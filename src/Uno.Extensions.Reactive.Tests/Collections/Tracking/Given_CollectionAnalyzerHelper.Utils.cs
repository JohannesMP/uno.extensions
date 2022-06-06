﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Tests._Utils;

namespace Uno.Extensions.Reactive.Tests.Collections.Tracking;

partial class Given_CollectionAnalyzer
{
	private static CollectionTrackerTester<MyClass> FromObj(params MyClass[] items)
	=> new(ImmutableList.Create(items), null);

	private static CollectionTrackerTester<int> FromInt(params int[] items)
		=> new(ImmutableList.Create(items), null);
}

internal class CollectionTrackerTester<T>
{
	private readonly ImmutableList<T> _previous;
	private ImmutableList<T>? _updated;
	private IEqualityComparer<T>? _itemComparer;
	private IEqualityComparer<T>? _itemVersionComparer;

	public CollectionTrackerTester(ImmutableList<T> previous, ImmutableList<T>? updated)
	{
		_previous = previous;
		_updated = updated;
	}

	public CollectionTrackerTester<T> To(params T[] updated)
	{
		_updated = ImmutableList.Create(updated);

		return this;
	}

	public CollectionTrackerTester<T> With(IEqualityComparer<T>? itemComparer = null, IEqualityComparer<T>? itemVersionComparer = null)
	{
		_itemComparer = itemComparer;
		_itemVersionComparer = itemVersionComparer;

		return this;
	}

	public void ShouldBeEmpty()
	{
		ShouldBe();
	}

	public void ShouldBe(params NotifyCollectionChangedEventArgs[] expected)
	{
		if (_updated is null)
		{
			Assert.Fail("To collection has not been set.");
		}

		var s1 = new ObservableCollectionSnapshot<T>(_previous);
		var s2 = new ObservableCollectionSnapshot<T>(_updated);

		var visitor = new TestVisitor();
		var tracker = new CollectionAnalyzer<T>(new ItemComparer<T>(_itemComparer, _itemVersionComparer));
		var changes = tracker.GetUpdater(s1, s2, visitor);

		IEnumerable<NotifyCollectionChangedEventArgs> GetCollectionChanges()
		{
			// Note: we use reflexion here since it's only for debug output

			var node = changes
				.GetType()
				.GetField("_head", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				?.GetValue(changes) as CollectionUpdater.Update;

			while(node != null)
			{
				var args = default(RichNotifyCollectionChangedEventArgs);
				try
				{
					args = node.Event;
				}
				catch (Exception) { }

				if (args != null)
				{
					yield return args;
				}
				node = node.Next;
			}
		}

		Console.WriteLine($"Expected: \r\n{expected.ToOutputString()}");
		Console.WriteLine();
		Console.WriteLine($"Actual: \r\n{GetCollectionChanges().ToOutputString()}");

		var handler = new Handler(expected);
		changes.DequeueChanges(handler);

		Assert.AreEqual(expected.Length, handler.EventsCount);

		var previousDuplicates = _previous.Count - _previous.Distinct(_itemComparer ?? EqualityComparer<T>.Default).Count();
		var updatedDuplicates = _updated.Count - _updated.Distinct(_itemComparer ?? EqualityComparer<T>.Default).Count();

		var added = _updated.Except(_previous, _itemComparer ?? EqualityComparer<T>.Default).ToArray();
		var removed = _previous.Except(_updated, _itemComparer ?? EqualityComparer<T>.Default).ToArray();

		var kept1 = _previous.Except(removed).ToArray(); // either updated or moved (or nothing at all)
		var kept2 = _updated.Except(added).ToArray();

		Assert.AreEqual(kept1.Length, kept2.Length);

		var notUpdated = kept1.Except(kept2, _itemComparer ?? EqualityComparer<T>.Default).ToArray(); // moved (or nothing at all): items for which the vistiro should not have been invoked
		var updated = kept1.Except(notUpdated).Join(kept2, l => l, r => r, (originalItem, updatedItem) => (originalItem, updatedItem), _itemComparer ?? EqualityComparer<T>.Default).ToArray();

		Console.WriteLine($@"
Detected changes using Linq: 
Added ({added.Length}): 
	{string.Join("\r\n\t", added)}
Removed ({removed.Length}): 
	{string.Join("\r\n\t", removed)}
Updated ({updated.Length}): 
	{string.Join("\r\n\t", updated.Select(items => $"{items.Item1} => {items.Item2}"))}
Moved or untouched ({notUpdated.Length}):
	{string.Join("\r\n\t", notUpdated)}");

		if (updated.Length + added.Length + removed.Length != visitor.Pending
			&& updated.Length + added.Length + removed.Length + Math.Abs(updatedDuplicates - previousDuplicates) != visitor.Pending
			&& updated.Length + added.Length + removed.Length + previousDuplicates != visitor.Pending
			&& updated.Length + added.Length + removed.Length + updatedDuplicates != visitor.Pending)
		{
			Assert.Fail($"Did not invoke the visitor for each item! (actual: {visitor.Pending})");
		}

		visitor.AssertAllRaised();
	}

	private class Handler : CollectionUpdater.IHandler
	{
		private readonly NotifyCollectionChangedEventArgs[] _expected;
		private int _expectedIndex = 0;

		public int EventsCount { get; private set; }

		public int Added { get; private set; }

		public int Removed { get; private set; }

		public int Replaced { get; private set; }

		public Handler(NotifyCollectionChangedEventArgs[] expected) => _expected = expected;

		public void Raise(RichNotifyCollectionChangedEventArgs arg)
		{
			EventsCount++;
			Assert.IsTrue(new NotifyCollectionChangedComparer(MyClassComparer.Instance).Equals(_expected[_expectedIndex++], arg));

			switch (arg.Action)
			{
				case NotifyCollectionChangedAction.Add:
					Added += arg.NewItems!.Count;
					break;

				case NotifyCollectionChangedAction.Remove:
					Removed += arg.OldItems!.Count;
					break;

				case NotifyCollectionChangedAction.Replace:
					Replaced += arg.NewItems!.Count;
					break;

				case NotifyCollectionChangedAction.Reset:
					throw new InvalidOperationException("Tracker should not generate Reset");
			}
		}

		public void ApplySilently(RichNotifyCollectionChangedEventArgs arg)
			=> throw new InvalidOperationException("Tracker should not generate silent event since we never handle them in visitor.");
	}
}

internal class TestVisitor : ICollectionUpdaterVisitor
{
	private int _pending, _added, _equals, _replaced, _remove, _reset;

	public int Pending => _pending;
	public int Added => _added;
	public int Same => _equals;
	public int Replaced => _replaced;
	public int Removed => _remove;
	public int Reseted => _reset;

	public void AssertAllRaised()
	{
		Assert.AreEqual(Pending, Added + Same + Replaced + Removed + Reseted);
	}

	public void AddItem(object item, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _added);
		}
	}

	public void SameItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Append(Did);

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _equals);
		};
	}

	public bool ReplaceItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		return false;

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _replaced);
		}
	}

	public void RemoveItem(object item, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _remove);
		}
	}

	public void Reset(IList oldItems, IList newItems, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _reset);
		}
	}
}


internal class MyClass
{
	public int Version { get; }

	public int Value { get; }

	private MyClass(int value, int version)
	{
		Value = value;
		Version = version;
	}

	public override int GetHashCode() => Value;

	public override bool Equals(object? obj) => obj is MyClass c && Value == c.Value && Version == c.Version;

	public static implicit operator MyClass(int value) => new MyClass(value, 0);

	public static implicit operator MyClass((int value, int version) values) => new MyClass(values.value, values.version);

	public static explicit operator int(MyClass obj) => obj.Value;

	public override string ToString() => Version > 0 ? $"{Value}v{Version}" : Value.ToString();
}

internal static class Given_CollectionAnalyzer_Extensions
{
	public static void ShouldBe(
		this ICollection<NotifyCollectionChangedEventArgs> changes,
		params NotifyCollectionChangedEventArgs[] expected)
	{
		Console.WriteLine($"Expected: \r\n{expected.ToOutputString()}");
		Console.WriteLine();
		Console.WriteLine($"Actual: \r\n{changes.ToOutputString()}");

		CollectionAssert.AreEqual(expected, changes.ToArray(), new NotifyCollectionChangedComparer(MyClassComparer.Instance));
	}

	public static string ToOutputString(this IEnumerable<NotifyCollectionChangedEventArgs> args)
		=> "\t" + string.Join("\r\n\t", args.Select((arg, i) => $"{i:00} - {ToOutputString(arg)}"));

	public static string ToOutputString(this NotifyCollectionChangedEventArgs arg) =>
		$"{arg.Action}: " +
		$"@ {arg.OldStartingIndex} => [{string.Join(", ", arg.OldItems?.Cast<object>().Select(MyClassComparer.GetData).Select(d => d.version > 0 ? $"{d.value}v{d.version}" : $"{d.value}") ?? new string[0])}] / " +
		$"@ {arg.NewStartingIndex} => [{string.Join(", ", arg.NewItems?.Cast<object>().Select(MyClassComparer.GetData).Select(d => d.version > 0 ? $"{d.value}v{d.version}" : $"{d.value}") ?? new string[0])}]";
}

internal class MyClassComparer : IEqualityComparer<object?>
{
	public static MyClassComparer Instance { get; } = new MyClassComparer();

	public new bool Equals(object? left, object? right) => GetData(left).Equals(GetData(right));

	public int GetHashCode(object? obj)
	{
		var (value, version) = GetData(obj);
		return value + version;
	}

	public static (int value, int version) GetData(object? item)
	{
		if (item is MyClass c)
		{
			return (c.Value, c.Version);
		}
		else if (item is int i)
		{
			return (i, 0);
		}
		else if (item is ValueTuple<int, int> v)
		{
			return v;
		}

		throw new InvalidOperationException();
	}
}
