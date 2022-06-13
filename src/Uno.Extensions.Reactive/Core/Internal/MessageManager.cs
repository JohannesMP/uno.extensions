﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;
using _ChangeSet = System.Collections.Generic.IReadOnlyDictionary<Uno.Extensions.Reactive.MessageAxis, Uno.Extensions.Reactive.Core.MessageAxisUpdate>;

namespace Uno.Extensions.Reactive.Core;

internal partial class MessageManager<TResult> : MessageManager<Unit, TResult>
{
	public MessageManager(Action<Message<TResult>>? send = null)
		: base(send)
	{
	}
}

internal partial class MessageManager<TParent, TResult>
{
	public delegate MessageBuilder<TParent, TResult> Updater(CurrentMessage current);
	public delegate MessageBuilder<TParent, TResult> Updater<in TState>(CurrentMessage current, TState state);

	private static readonly Updater<Updater> _stateLessUpdater = (cm, u) => u(cm);

	private readonly object _gate = new();
	private readonly Action<Message<TResult>>? _send;

	private Message<TParent>? _parent;
	private UpdateTransaction? _pendingUpdate;

	public Message<TResult> Current => _local.result;
	// Locally, we only store a set of delegates that are upgrading the parent value into a local value.
	private (_ChangeSet defined, _ChangeSet applied, Message<TResult> result) _local;

	private bool _isFirstUpdate = true;

	public MessageManager(Action<Message<TResult>>? send = null)
	{
		_send = send;

		var initialMessage = Message<TResult>.Initial;
		var initialUpdates = new Dictionary<MessageAxis, MessageAxisUpdate>
		{
			// As the DataAxis always returns the local data over the parent one,
			// this default update ensure that the output will be undefined until explicitly set using Update
			{ MessageAxis.Data, new(MessageAxis.Data, new(Option<object>.Undefined())) }
		};
		_local = (initialUpdates, initialUpdates, initialMessage);
	}

	public bool Update(Updater updater, CancellationToken ct = default)
		=> Update(_stateLessUpdater, updater, ct);

	public bool Update<TState>(Updater<TState> updater, TState state, CancellationToken ct = default)
	{
		// Even if this method is sync, we force the caller to provide a ct to make sure that we don't send an update if cancelled
		if (ct.IsCancellationRequested)
		{
			return false;
		}

		lock (_gate)
		{
			var (parent, locallyDefinedChangeSet) = updater(new CurrentMessage(this), state).GetResult();

			if (ct.IsCancellationRequested)
			{
				return false;
			}

			// If we have any pending update transaction, we make sure to append its change set to the locally defined
			var changeSetToApply = _pendingUpdate?.TransientUpdates is { Count: > 0 } transientUpdates
				? locallyDefinedChangeSet.ToDictionary().SetItems(transientUpdates)
				: locallyDefinedChangeSet;

			// Finally apply the updates in order to get the new Local
			// Note: We append the _local.applied.Keys as if a transaction was removed, it's possible that some changes was removed
			var possiblyChangedAxes = changeSetToApply.Keys.Concat(_local.applied.Keys);
			if (parent is not null && parent != _parent) // Note: parent should not be null if updated !!!
			{
				possiblyChangedAxes = possiblyChangedAxes.Concat(parent.Changes);
			}

			var parentEntry = parent?.Current ?? MessageEntry<TParent>.Empty;
			var localEntry = _local.result.Current;
			var values = localEntry.Values.ToDictionary();
			var changes = new ChangeCollection();
			foreach (var axis in possiblyChangedAxes.Distinct())
			{
				var parentValue = parentEntry[axis];
				var currentValue = localEntry[axis];

				// Note: If we don't have any "change" to apply to the given axis,
				//		 it means that either that "change" has been removed (for instance a transient from an update transaction),
				//		 either the change is coming from the parent.
				//		 In all case we just need to propagate the value from the parent.
				var updated = (value: parentValue, changes: default(IChangeSet?));
				if (changeSetToApply.TryGetValue(axis, out var update))
				{
					updated = update.GetValue(parentValue, currentValue);
				}
				else if (!_local.applied.ContainsKey(axis)
					&& (parent?.Changes.Contains(axis, out var parentChanges) ?? false))
				{
					// If we don't have any local value (neither in the change set being applied, neither in the previously applied change set),
					// and the value has been updated on the parent, then it means that changes if the parent message are valid, so we can forward them.
					updated = (parentValue, parentChanges);
				}

				if (updated.value == MessageAxisValue.Unset)
				{
					values.Remove(axis);
				}
				else
				{
					values[axis] = updated.value;
				}

				if (!axis.AreEquals(currentValue, updated.value))
				{
					changes.Add(axis, updated.changes);
				}
			}

			_parent = parent;

			if (!_isFirstUpdate && changes is { Count: 0 })
			{
				return false; // Well even if some changes was made on the Parent and/or on Local, the resulting values are the same.
			}

			_isFirstUpdate = false;
			_local = (locallyDefinedChangeSet, changeSetToApply, new Message<TResult>(Current.Current, new MessageEntry<TResult>(values), changes)); 
			_send?.Invoke(Current);
			return true;
		}
	}

	private void Update()
		=> Update((m, _) => m.With(), default(object), CancellationToken.None);

	public UpdateTransaction BeginUpdate(CancellationToken ct)
	{
		lock (_gate)
		{
			var previousTransaction = _pendingUpdate;
			var transaction = new UpdateTransaction(this, ct);

			_pendingUpdate = transaction;
			if (previousTransaction is not null)
			{
				previousTransaction.Dispose();
				if (previousTransaction.TransientUpdates.Any())
				{
					Update(); // Make sure to clear the transient updates
				}
			}

			return transaction; 
		}
	}

	public UpdateTransaction BeginUpdate(CancellationToken ct, params MessageAxis[] preservePendingAxes)
	{
		lock (_gate)
		{
			var previousTransaction = _pendingUpdate;
			var existingTransientUpdates = previousTransaction
					?.TransientUpdates
					.Values
					.Where(u => preservePendingAxes.Contains(u.Axis))
					.ToDictionary(u => u.Axis)
				?? new();
			var transaction = new UpdateTransaction(this, existingTransientUpdates, ct);

			_pendingUpdate = transaction;
			if (previousTransaction is not null)
			{
				previousTransaction.Dispose();
				if (previousTransaction.TransientUpdates.Any() 
					&& previousTransaction.TransientUpdates.Count != transaction.TransientUpdates.Count)
				{
					Update(); // Make sure to clear the transient updates that was not preserved
				}
			}

			return transaction; 
		}
	}

	private void EndUpdate(UpdateTransaction transaction, Updater result)
		=> EndUpdate(transaction, _stateLessUpdater, result);

	private void EndUpdate<TState>(UpdateTransaction transaction, Updater<TState> result, TState state)
	{
		lock (_gate)
		{
			if (_pendingUpdate == transaction)
			{
				_pendingUpdate = null;
				Update((msg, @params) => @params.result(msg, @params.state), (result, state), CancellationToken.None);
			}
		}
	}

	private void EndUpdate(UpdateTransaction transaction)
	{
		lock (_gate)
		{
			if (_pendingUpdate == transaction)
			{
				_pendingUpdate = null;
				Update();
			}
		}
	}
}
