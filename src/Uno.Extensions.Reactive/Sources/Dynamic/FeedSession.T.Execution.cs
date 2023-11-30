﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Sources;

internal sealed partial class FeedSession<TResult>
{
	internal sealed class Execution : FeedExecution
	{
		public readonly object StateGate = new();
		private readonly TaskCompletionSource<Unit> _task = new();

		private readonly FeedSession<TResult> _session;

		public States State = States.Loading;
		private Queue<Action<IMessageBuilder>>? _updatesQueue;

		public enum States
		{
			// When loading the main content, updates are only put in queue in the MessageManager and they will be processed when the main load action pushes a messages.
			Loading,

			// The main content has been loaded and is still active (no new session has been created).
			// Queuing an update will be processed immediately.
			Loaded,

			// A new sessions has been created, updates are forbidden (ignored).
			Completed,
		}

		public Execution(FeedSession<TResult> session, IReadOnlyCollection<ExecuteRequest> requests)
			: base(session, requests)
		{
			_session = session;
			Enable();
		}

		/// <inheritdoc />
		public override void Enqueue(Action<IMessageBuilder> updater)
		{
			lock (StateGate)
			{
				switch (State)
				{
					case States.Loading:
						(_updatesQueue ??= new()).Enqueue(updater);
						break;

					case States.Loaded: // Sync update
						_session._message.Update(static (m, u) => m.With().Apply(u), updater, default);
						break;

					default:
					case States.Completed:
						this.Log().Info("Update is being ignored as the execution is not longer active.");
						break;
				}
			}
		}

		private async void Enable()
		{
			try
			{
				// Note: we DO NOT register to the Token for cancellation, as the _task is all about tracking the effective completion of the execution,
				// including the "finalization" in case of cancellation.

				await EnableCore().ConfigureAwait(false);
			}
			catch (Exception error)
			{
				// Impossible case ... the EnableCore should not throw in any way!
				this.Log().Error(error, "Unexpected error while enabling the execution.");
			}
			finally
			{
				_task.SetResult(default);
			}
		}

		private async Task EnableCore()
		{
			// Note: We DO NOT register the 'message' update transaction into ct.Register,
			//		 so the next execution will be abel to preserve the pending progress axis (it's expected to be started before this is being disposed).
			using var message = _session._message.BeginUpdate(preservePendingAxes: Requests.Select(req => req.AsyncAxis).Distinct().ToArray());

			// Once we have created our message update transaction (and we kept the transient axes), we make sure to set us as the current execution and wait for previous one to full complete.
			if (Interlocked.Exchange(ref _session._currentExecution, this) is { } previous)
			{
				await previous.DisposeAsync().ConfigureAwait(false);
				previous = null; // avoids leak
			}

			using var srcCtxCurrentSub = _session.Context.AsCurrent();
			using var currentSub = FeedExecution.SetCurrent(this);

			ValueTask<Option<TResult>> task = default;
			try
			{
				await NotifyBegin().ConfigureAwait(false);

				task = _session._mainAsyncAction(Token);
			}
			catch (OperationCanceledException) when (Token.IsCancellationRequested)
			{
				await NotifyEnd(FeedExecutionResult.Cancelled).ConfigureAwait(false);

				lock (StateGate)
				{
					// Even if we have been cancelled (because a new execution request has been queued),
					// it's still our responsibility, once we reached a state where we can allow it, to start the next execution.

					State = States.Completed;
					if (_session.TryStartNext())
					{
						return;
					}
				}
				if (Interlocked.CompareExchange(ref _session._currentExecution, null, this) == this)
				{
					// Concurrency support: if a request has been queued since we checked, as the _currentExecution was not null,
					// the RequestLoad won't have create a new execution.
					_session.TryStartNext();
				}

				return; // No commit here! - TODO: _updatesQueue are dropped here
			}
			catch (Exception error)
			{
				await NotifyEnd(FeedExecutionResult.Failed).ConfigureAwait(false);

				lock (StateGate)
				{
					State = States.Completed;
					if (_session.TryStartNext())
					{
						return; // If a new execution has been started, there is no need to commit our transaction, it has been disposed.
					}

					var parentMsg = _session.Feeds.GetParent();
					var updates = Interlocked.Exchange(ref _updatesQueue, null);

					message.Commit(
						static (m, @params) => m.With(@params.parentMsg).Apply(@params.updates).Error(@params.error),
						(parentMsg, error, updates));
				}

				// Finally we remove us from the current execution (if we are still the current ^^).
				if (Interlocked.CompareExchange(ref _session._currentExecution, null, this) == this)
				{
					// Concurrency support: if a request has been queued since we checked, as the _currentExecution was not null,
					// the RequestLoad won't have create a new execution.
					_session.TryStartNext();
				}

				return;
			}

			// If we are not yet and the 'dataTask' is really async, we need to send a new message flagged as transient
			// Note: This check is not "atomic", but it's valid as it only enables a fast path.
			if (Requests.Any(req => !req.IsAsync(message.Local.Current)))// !message.Local.Current.IsTransient)
			{
				// As lot of async methods are actually not really async but only re-scheduled,
				// we try to avoid the transient state by delaying a bit the message.
				for (var i = 0; !task.IsCompleted && !Token.IsCancellationRequested && i < 5; i++)
				{
					await Task.Yield();
				}

				if (Token.IsCancellationRequested)
				{
					await NotifyEnd(FeedExecutionResult.Cancelled).ConfigureAwait(false);
					lock (StateGate)
					{
						// Even if we have been cancelled (because a new execution request has been queued),
						// it's still our responsibility, once we reached a state where we can allow it, to start the next execution.

						State = States.Completed;
						if (_session.TryStartNext())
						{
							return;
						}
					}

					// Finally we remove us from the current execution (if we are still the current ^^).
					if (Interlocked.CompareExchange(ref _session._currentExecution, null, this) == this)
					{
						// Concurrency support: if a request has been queued since we checked, as the _currentExecution was not null,
						// the RequestLoad won't have create a new execution.
						_session.TryStartNext();
					}

					return; // No commit here! - TODO: _updatesQueue are dropped here
				}

				// The 'valueProvider' is not completed yet, so we need to flag the current value as transient.
				// Note: We also provide the parentMsg which will be applied
				if (!task.IsCompleted)
				{
					var parentMsg = _session.Feeds.GetParent();
					var updates = Interlocked.Exchange(ref _updatesQueue, null);

					message.Update(
						static (msg, @params) =>
						{
							var builder = msg.With(@params.parentMsg);
							builder.Apply(@params.updates);
							foreach (var request in @params.Requests)
							{
								builder.SetTransient(request.AsyncAxis, request.AsyncValue);
							}
							return builder;
						},
						(parentMsg, updates, Requests),
						Token);
				}
			}

			try
			{
				var data = await task.ConfigureAwait(false);

				await NotifyEnd(FeedExecutionResult.Success).ConfigureAwait(false);

				lock (StateGate)
				{
					State = States.Loaded;
					if (_session.TryStartNext())
					{
						return; // If a new execution has been started, there is no need to commit our transaction, it has been disposed.
					}

					var parentMsg = _session.Feeds.GetParent();
					var updates = Interlocked.Exchange(ref _updatesQueue, null);

					message.Commit(
						static (msg, @params) => msg.With(@params.parentMsg).Apply(@params.updates).Data(@params.data).Error(null),
						(parentMsg, data, updates));
				}
			}
			catch (OperationCanceledException) when (Token.IsCancellationRequested)
			{
				await NotifyEnd(FeedExecutionResult.Cancelled).ConfigureAwait(false);

				lock (StateGate)
				{
					State = States.Completed;
					// Note: if we were cancelled it's either because the a new execution has started, either because the session is ending, so no need to TryStartNext() here. 
				}

				// No commit here! - TODO: _updatesQueue are dropped here
			}
			catch (Exception error)
			{
				await NotifyEnd(FeedExecutionResult.Failed).ConfigureAwait(false);

				lock (StateGate)
				{
					State = States.Loaded;
					if (_session.TryStartNext())
					{
						return;
					}

					var parentMsg = _session.Feeds.GetParent();
					var updates = Interlocked.Exchange(ref _updatesQueue, null);

					message.Commit(
						static (msg, @params) => msg.With(@params.parentMsg).Apply(@params.updates).Error(@params.error),
						(parentMsg, error, updates));
				}
			}
			finally
			{
				// Finally we remove us from the current execution (if we are still the current ^^).
				if (Interlocked.CompareExchange(ref _session._currentExecution, null, this) == this)
				{
					// Concurrency support: if a request has been queued since we checked, as the _currentExecution was not null,
					// the RequestLoad won't have create a new execution.
					_session.TryStartNext();
				}
			}
		}

		private async ValueTask NotifyBegin()
		{
			foreach (var dependency in _session.Dependencies)
			{
				try
				{
					await dependency.OnExecuting(this, Token).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					this.Log().Error(e, $"Dependency {dependency} failed its loading.");
				}
			}
		}

		private async ValueTask NotifyEnd(FeedExecutionResult result)
		{
			foreach (var dependency in _session.Dependencies)
			{
				try
				{
					// Note: No CT for the end notification (the current Token might already be cancelled).
					await dependency.OnExecuted(this, result, default).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					this.Log().Error(e, $"Dependency {dependency} failed its loaded.");
				}
			}
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			// This will abort the Token
			await base.DisposeAsync().ConfigureAwait(false);

			lock (StateGate)
			{
				State = States.Completed;
			}

			// Then wait for the end of the execution (so we are sure that dependencies are notified of the end of the loading)
			// Note: This execution won't commit anything if a new execution has been queued (cf. WillCommit).
			await _task.Task.ConfigureAwait(false);
		}
	}
}
