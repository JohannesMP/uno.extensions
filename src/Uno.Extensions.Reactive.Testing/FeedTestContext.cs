﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Testing;

public class FeedTestContext : ISourceContextAware, IDisposable
{
	private static long _contextCount = 0;
	private long _contextId = Interlocked.Increment(ref _contextCount);

	private readonly string _name;
	private readonly SourceContext _sourceCtx;
			
	private SourceContext.CurrentSubscription _subscription;

	public FeedTestContext(TestContext testContext)
	{
		_name = testContext is null // On runtime test
			? $"runtime_tests.{_contextId:D4}"
			: $"{testContext.FullyQualifiedTestClassName}.{testContext.TestName}";

		_sourceCtx = SourceContext.GetOrCreate(this);
		_subscription = _sourceCtx.AsCurrent();

		testContext?.CancellationTokenSource.Token.Register(Dispose);

		// For tests we prefer to replay all vales
		FeedSubscription.IsInitialSyncValuesSkippingAllowed = false;
	}

	public FeedTestContext([CallerMemberName] string? name = null)
	{
		_name = name ?? throw new ArgumentNullException("Context must be named.");
		_sourceCtx = SourceContext.GetOrCreate(this);
		_subscription = _sourceCtx.AsCurrent();

		// For tests we prefer to replay all vales
		FeedSubscription.IsInitialSyncValuesSkippingAllowed = false;
	}

	public SourceContext SourceContext => _sourceCtx;

	public void RestoreCurrent()
	{
		ResignCurrent();
		_subscription = _sourceCtx.AsCurrent();
	}

	public void ResignCurrent()
		=> _subscription.Dispose();

	/// <inheritdoc />
	public void Dispose()
	{
		_subscription.Dispose();
		_sourceCtx.DisposeAsync().AsTask().Wait(1000);
	}

	public static implicit operator SourceContext(FeedTestContext context)
		=> context._sourceCtx;

	/// <inheritdoc />
	public override string ToString()
		=> $"[FeedContext] {_name}";
}
