﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Uno.Extensions.Collections.Facades.Slice;
using Uno.Extensions.Collections.Tracking;

namespace Uno.Extensions.Reactive.Utils;

internal static class ListExtensions
{
	public static CollectionAnalyzer.IndexOf GetIndexOf(this IList list, IEqualityComparer? comparer)
	{
		if (comparer is null)
		{
			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf;
				case List<object?> impl:
					return impl.IndexOf;
				case Array array:
					return (value, index, count) => Array.IndexOf(array, value, index, count);
				default:
					return (value, index, count) =>
					{
						for (var i = index; i < index + count; i++)
						{
							if (object.Equals(list[i], value))
							{
								return i;
							}
						}

						return -1;
					};
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<object?> immutable:
				{
					var typedComparer = comparer.ToEqualityComparer<object?>();
					return (value, index, count) => immutable.IndexOf(value, index, count, typedComparer);
				}
				default:
					return (value, index, count) =>
					{
						for (var i = index; i < index + count; i++)
						{
							if (comparer.Equals(list[i], value))
							{
								return i;
							}
						}

						return -1;
					};
			}
		}
	}

	public static CollectionAnalyzer.IndexOf GetIndexOf<T>(this IList list, IEqualityComparer<T>? comparer)
	{
		if (comparer is null)
		{
			switch (list)
			{
				case IImmutableList<T> immutable:
					return (value, index, count) => value is T t ? immutable.IndexOf(t, index, count) : -1;
				case List<T> impl:
					return (value, index, count) => value is T t ? impl.IndexOf(t, index, count) : -1;
				case T[] array:
					return (value, index, count) => Array.IndexOf(array, value, index, count);
				default:
					return (value, index, count) =>
					{
						for (var i = index; i < index + count; i++)
						{
							if (object.Equals(list[i], value))
							{
								return i;
							}
						}

						return -1;
					};
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<T> immutable:
					return (value, index, count) => value is T t ? immutable.IndexOf(t, index, count, comparer) : -1;
				default:
				{
					var untypedComparer = comparer.ToEqualityComparer();
					return (value, index, count) =>
					{
						for (var i = index; i < index + count; i++)
						{
							if (untypedComparer.Equals(list[i], value))
							{
								return i;
							}
						}

						return -1;
					};
				}
			}
		}
	}

	public static int IndexOf(this IList list, object? value, IEqualityComparer? comparer)
		=> list.IndexOf(value, 0, comparer);

	public static int IndexOf(this IList list, object? value, int index, IEqualityComparer? comparer)
		=> list.IndexOf(value, index, list.Count - index, comparer);

	public static int IndexOf(this IList list, object? value, int index, int count, IEqualityComparer? comparer)
	{
		if (comparer is null)
		{
			if (index is 0 && count == list.Count)
			{
				return list.IndexOf(value);
			}

			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index, count);
				case List<object?> impl:
					return impl.IndexOf(value, index, count);
				case Array array:
					return Array.IndexOf(array, value, index, count);
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (object.Equals(list[i], value))
						{
							return i;
						}
					}

					return -1;
				}
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index, count, comparer.ToEqualityComparer<object?>());
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (comparer.Equals(list[i], value))
						{
							return i;
						}
					}
					return -1;
				}
			}
		}
	}

	public static int IndexOf<T>(this IList<T> list, T value, IEqualityComparer<T>? comparer)
		=> list.IndexOf(value, 0, comparer);

	public static int IndexOf<T>(this IList<T> list, T value, int index, IEqualityComparer<T>? comparer)
		=> list.IndexOf(value, index, list.Count - index, comparer);

	public static int IndexOf<T>(this IList<T> list, T value, int index, int count, IEqualityComparer<T>? comparer)
	{
		if (comparer is null)
		{
			if (index is 0)
			{
				return list.IndexOf(value);
			}

			switch (list)
			{
				case IImmutableList<T> immutable:
					return immutable.IndexOf(value, index, count);
				case List<T> impl:
					return impl.IndexOf(value, index, count);
				case T[] array:
					return Array.IndexOf(array, value, index, count);
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (object.Equals(list[i], value))
						{
							return i;
						}
					}

					return -1;
				}
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<T> immutable:
					return immutable.IndexOf(value, index, count, comparer);
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (comparer.Equals(list[i], value))
						{
							return i;
						}
					}
					return -1;
				}
			}
		}
	}

	public static IList Slice(this IList list, int index, int count)
		=> new SliceList(list, index, count);
}
