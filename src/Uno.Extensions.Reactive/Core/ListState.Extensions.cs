﻿using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

static partial class ListState
{
	/// <summary>
	/// [DEPRECATED] Use UpdateMessageAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateMessageAsync")]
#endif
	public static ValueTask UpdateMessage<T>(this IListState<T> state, Action<MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> state.UpdateMessageAsync(updater, ct);

	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateAsync<T>(this IListState<T> state, Func<IImmutableList<T>, IImmutableList<T>> updater, CancellationToken ct = default)
		=> state.UpdateMessageAsync(
			m =>
			{
				var updatedValue = updater(m.CurrentData.SomeOrDefault() ?? ImmutableList<T>.Empty);
				var updatedData = updatedValue is null or {Count: 0} ? Option<IImmutableList<T>>.None() : Option.Some(updatedValue);

				m.Data(updatedData);
			},
			ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateAsync")]
#endif
	public static ValueTask Update<T>(this IListState<T> state, Func<IImmutableList<T>, IImmutableList<T>> updater, CancellationToken ct)
		=> UpdateAsync(state, updater, ct);

	/// <summary>
	/// Updates the value of a list state
	/// </summary>
	/// <typeparam name="T">Type of the items of the list state.</typeparam>
	/// <param name="state">The list state to update.</param>
	/// <param name="updater">The update method to apply to the current list.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateDataAsync<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, Option<IImmutableList<T>>> updater, CancellationToken ct = default)
		=> state.UpdateMessageAsync(m => m.Data(updater(m.CurrentData)), ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateDataAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateData<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, Option<IImmutableList<T>>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);

	/// <summary>
	/// Updates the value of a list state
	/// </summary>
	/// <typeparam name="T">Type of the items of the list state.</typeparam>
	/// <param name="state">The list state to update.</param>
	/// <param name="updater">The update method to apply to the current list.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateDataAsync<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, IImmutableList<T>> updater, CancellationToken ct = default)
		=> state.UpdateMessageAsync(m => m.Data(updater(m.CurrentData)), ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateDataAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateData<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, IImmutableList<T>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateDataAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateValue<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, Option<IImmutableList<T>>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateDataAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateValue<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, IImmutableList<T>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);


	#region Operators
	/// <summary>
	/// Adds an item into a list state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask InsertAsync<T>(this IListState<T> state, T item, CancellationToken ct = default)
		=> state.UpdateDataAsync(items => items.SomeOrDefault(ImmutableList<T>.Empty).Insert(0, item), ct);

	/// <summary>
	/// Adds an item into a list state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask AddAsync<T>(this IListState<T> state, T item, CancellationToken ct = default)
		=> state.UpdateDataAsync(items => items.SomeOrDefault(ImmutableList<T>.Empty).Add(item), ct);

	/// <summary>
	/// Removes all matching items from a list state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="match">Predicate to determine which items should be removed.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask RemoveAllAsync<T>(this IListState<T> state, Predicate<T> match, CancellationToken ct = default)
		=> state.UpdateDataAsync(itemsOpt => itemsOpt.Map(items => items.RemoveAll(match)), ct);

	/// <summary>
	/// Updates all matching items from a list state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="match">Predicate to determine which items should be removed.</param>
	/// <param name="updater">How to update items.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask UpdateAllAsync<T>(this IListState<T> state, Predicate<T> match, Func<T, T> updater, CancellationToken ct = default)
		=> state.UpdateDataAsync(
			itemsOpt => itemsOpt.Map(items =>
			{
				var updated = items;
				foreach (var item in items)
				{
					if (match(item))
					{
						updated = items.Replace(item, updater(item));
					}
				}
				return updated;
			}),
			ct);

	/// <summary>
	/// [DEPRECATED] Use .UpdateAllAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateAllAsync")]
#endif
	public static ValueTask UpdateAsync<T>(this IListState<T> state, Predicate<T> match, Func<T, T> updater, CancellationToken ct)
		=> UpdateAllAsync(state, match, updater, ct);

	/// <summary>
	/// [DEPRECATED] Use .ForEachAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ForEachAsync")]
#endif
	public static IDisposable Execute<T>(this IListState<T> state, AsyncAction<IImmutableList<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> ForEachAsync(state, action, caller, line);


	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IDisposable ForEachAsync<T>(this IListState<T> state, AsyncAction<IImmutableList<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> new StateForEach<IImmutableList<T>>(state, (list, ct) => action(list ?? ImmutableList<T>.Empty, ct), $"ForEachAsync defined in {caller} at line {line}.");
	#endregion

	/// <summary>
	/// Tries to select some items in a list state.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="selectedItems">The items to flag as selected.</param>
	/// <param name="ct">A token to abort the async operation.</param>
	/// <returns></returns>
	public static async ValueTask<bool> TrySelectAsync<T>(this IListState<T> state, IImmutableList<T> selectedItems, CancellationToken ct = default)
	{
		var comparer = ListFeed<T>.DefaultComparer.Entity;
		var success = false;

		await state.UpdateMessageAsync(msg =>
		{
			var items = msg.CurrentData.SomeOrDefault(ImmutableList<T>.Empty);
			if (SelectionInfo.TryCreateMultiple(items, selectedItems, out var selection, comparer))
			{
				success = true;
				msg.Selected(selection);
			}
		}, ct).ConfigureAwait(false);

		return success;
	}

	/// <summary>
	/// Tries to select a single item in a list state.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="selectedItem">The item to flag as selected.</param>
	/// <param name="ct">A token to abort the async operation.</param>
	/// <returns></returns>
	public static async ValueTask<bool> TrySelectAsync<T>(this IListState<T> state, T selectedItem, CancellationToken ct = default)
		where T : notnull
	{
		var comparer = ListFeed<T>.DefaultComparer.Entity;
		var success = false;

		await state.UpdateMessageAsync(msg =>
		{
			var items = msg.CurrentData.SomeOrDefault(ImmutableList<T>.Empty);
			if (SelectionInfo.TryCreateSingle(items, selectedItem, out var selection, comparer))
			{
				success = true;
				msg.Selected(selection);
			}
		}, ct).ConfigureAwait(false);

		return success;
	}

	/// <summary>
	/// Clear the selection info of a list state.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="ct">A token to abort the async operation.</param>
	/// <returns></returns>
	public static ValueTask ClearSelectionAsync<T>(this IListState<T> state, CancellationToken ct = default)
		where T : notnull
		=> state.UpdateMessageAsync(msg => msg.Selected(SelectionInfo.Empty), ct);

	/// <summary>
	/// [DEPRECATED] Use .ClearSelectionAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ClearSelectionAsync")]
#endif
	public static ValueTask ClearSelection<T>(this IListState<T> state, CancellationToken ct = default)
		where T : notnull
		=> ClearSelectionAsync(state, ct);
}
