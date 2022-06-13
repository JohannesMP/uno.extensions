﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.UI;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// An helper class use to data-bind a <see cref="IListFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed partial class BindableListFeed<T> : ISignal<IMessage>, IListState<T>, IInput<IImmutableList<T>>
{
	private readonly BindableCollection _items;
	private readonly IListState<T> _state;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="propertyName">The name of the property backed by the object.</param>
	/// <param name="source">The source data stream.</param>
	/// <param name="ctx">The context of the owner.</param>
	public BindableListFeed(string propertyName, IListFeed<T> source, SourceContext ctx)
	{
		PropertyName = propertyName;

		_state = ctx.GetOrCreateListState(source);
		_items = CreateBindableCollection(ctx);
	}

	/// <inheritdoc />
	public string PropertyName { get; }

	/// <inheritdoc />
	async IAsyncEnumerable<IMessage> ISignal<IMessage>.GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct)
	{
		// This is the GetSource implementation dedicated to data binding!
		// Instead of being an IFeed<IIMutableList<T>> we are instead exposing an IFeed<ICollectionView>.
		// WARNING: **The ICollectionView is mutable**

		var collectionViewForCurrentThread = _items.GetForCurrentThread();
		var localMsg = new MessageManager<IImmutableList<T>, ICollectionView>();

		await foreach (var parentMsg in _state.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(
					(current, @params) => current
						.With(@params.parentMsg)
						.Data(@params.parentMsg.Current.Data.Map(_ => @params.collectionViewForCurrentThread)),
					(parentMsg, collectionViewForCurrentThread)))
			{
				yield return localMsg.Current;
			}
		}
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		// TODO Uno: Should the source be per thread? This is actually not used for bindings.

		return _state.GetSource(context, ct);
	}

	/// <inheritdoc />
	ValueTask IListState<T>.UpdateMessage(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessage(updater, ct);

	/// <inheritdoc />
	ValueTask IState<IImmutableList<T>>.UpdateMessage(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessage(updater, ct);


	private BindableCollection CreateBindableCollection(SourceContext ctx)
	{
		var currentCount = 0;
		//var currentPage = default(PaginationInfo?);
		//var pageLoadTask = default(TaskCompletionSource<Unit>?);
		//var pageLoadTokens = default(TokenCollection<PageToken>?);
		var pageTokens = new TokenCompletionHandler<PageToken>();

		var requests = new RequestSource();
		var pagination = new PaginationService(LoadMore);
		var services = new SingletonServiceProvider(pagination);
		var collection = BindableCollection.Create<T>(services: services);

		if (ctx.Token.CanBeCanceled)
		{
			ctx.Token.Register(pagination.Dispose);
			ctx.Token.Register(requests.Dispose);
			ctx.Token.Register(() => _ = services.DisposeAsync());
		}

		// Note: we have to listen for collection changes on bindable _items to update the state.
		// https://github.com/unoplatform/uno.extensions/issues/370

		_state.GetSource(ctx.CreateChild(requests), ctx.Token).ForEachAsync(
			msg =>
			{
				if (ctx.Token.IsCancellationRequested)
				{
					return;
				}

				if (msg.Changes.Contains(MessageAxis.Data, out var changes))
				{
					var items = msg.Current.Data.SomeOrDefault(ImmutableList<T>.Empty);
					currentCount = items.Count;

					collection.Switch(new ImmutableObservableCollection<T>(items), changes as CollectionChangeSet);
				}

				// We update the HasMoreItems **before** completing the pendingPage, so LV won't try to request a new page.
				//if (msg.Changes.Contains("HasMoreItems"))
				//{

				//}

				// ON NE DOIT ACTIVER LE HAS MORE ITEMS QUE SI ON OBTIENT UN PAGEINFO
				//pagination.HasMoreItems = true;

				if (!msg.Current.IsTransient
					&& msg.Current.GetPaginationInfo() is { IsLoadingMoreItems: false } page)
				{
					pageTokens.Received(page.Tokens);
				}
				//currentPage = msg.Current.GetPaginationInfo();

				//if (!msg.Current.IsTransient
				//	&& pageLoadTokens is {} tokens
				//	&& currentPage is {IsLoadingMoreItems: false} page
				//	&& tokens.IsLower(page.Tokens))
				//{
				//	pageLoadTask!.TrySetResult(default);
				//	pageLoadTokens = null;
				//	pageLoadTask = null;
				//}
			},
			ctx.Token);

		async ValueTask<uint> LoadMore(uint count, CancellationToken ct)
		{
			try
			{
				var originalCount = currentCount;

				await pageTokens.WaitFor(requests.RequestMoreItems(count), ct);

				// We set the task first to avoid concurrency issue with the ForEachAsync callback
				//pageLoadTask = new TaskCompletionSource<Unit>();
				//pageLoadTokens = requests.RequestMoreItems(count);

				//if (pageLoadTokens.IsEmpty)
				//{
				//	return 0;
				//}

				//if (currentPage is not null && !pageLoadTokens.IsLower(currentPage.Tokens))
				//{
				//	// If the page was not loaded synchronously, lets wait for the tokens
				//	await pageLoadTask.Task;
				//}

				var resultCount = currentCount;

				return (uint)Math.Max(0, resultCount - originalCount);
			}
			finally
			{
				//pageLoadTask = default;
				//pageLoadTokens = default;
			}
		}

		return collection;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _state.DisposeAsync();
	}
}
