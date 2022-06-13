﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public static class ListFeed<T>
{
	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Uno.Extensions.Reactive.Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Create(Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider)
		=> Feed<IImmutableList<T>>.Create(sourceProvider).AsListFeed();

	/// <summary>
	/// Gets or create a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Create(Func<IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider)
		=> Feed<IImmutableList<T>>.Create(sourceProvider).AsListFeed();

	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Async(AsyncFunc<Option<IImmutableList<T>>> valueProvider, Signal? refresh = null)
		=> Feed<IImmutableList<T>>.Async(valueProvider, refresh).AsListFeed();

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> Async(AsyncFunc<IImmutableList<T>> valueProvider, Signal? refresh = null)
		=> Feed<IImmutableList<T>>.Async(valueProvider, refresh).AsListFeed();

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<Option<IImmutableList<T>>>> enumerableProvider)
		=> Feed<IImmutableList<T>>.AsyncEnumerable(enumerableProvider).AsListFeed();

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IListFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<IImmutableList<T>>> enumerableProvider)
		=> Feed<IImmutableList<T>>.AsyncEnumerable(enumerableProvider).AsListFeed();

	public static IListFeed<T> Paginated<TCursor>(TCursor firstPage, AsyncFunc<TCursor, Page<TCursor, T>> getPage)
		=> AttachedProperty.GetOrCreate(getPage.Target ?? getPage.Method, firstPage, getPage, (_, fp, gp) => new PaginatedListFeed<TCursor,T>(fp, gp));

	public static IListFeed<T> PaginatedBy(AsyncFunc<PageInfo, IImmutableList<T>> getPage)
		=> AttachedProperty.GetOrCreate(getPage, gp => new PaginatedListFeed<uint?, T>(firstPage: 0, PaginatedByIndex(gp)));

	private static AsyncFunc<uint?, Page<uint?, T>> PaginatedByIndex(AsyncFunc<PageInfo, IImmutableList<T>> getPage) => async (pageNumber, ct)
		=> await getPage(new PageInfo { PageNumber = pageNumber!.Value }, ct) is { Count: > 0 } page
			? new Page<uint?, T>(page, pageNumber + 1)
			: Page<uint?, T>.Empty;

	public struct PageInfo
	{
		public uint PageNumber { get; init; }

		public uint DesiredCount { get; init; }
	}
}
