﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.UI;

public partial class FeedView
{
	internal class RefreshCommand : IAsyncCommand
	{
		private static readonly PropertyChangedEventArgs _isExecutingChanged = new(nameof(IsExecuting));

		/// <inheritdoc />
		public event EventHandler? CanExecuteChanged;

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;

		private readonly FeedView _view;
		private bool _isExecuting;

		public RefreshCommand(FeedView view)
		{
			_view = view;
		}

		/// <inheritdoc />
		public bool IsExecuting
		{
			get => _isExecuting;
			internal set
			{
				if (value != _isExecuting)
				{
					_isExecuting = value;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					PropertyChanged?.Invoke(this, _isExecutingChanged);
				}
			}
		}

		/// <inheritdoc />
		public bool CanExecute(object? parameter)
			=> !IsExecuting;

		/// <inheritdoc />
		public void Execute(object? parameter)
		{
			if (_view._subscription is { } subscription)
			{
				IsExecuting = true;
				// We must make sure to run on a background thread to send requests
				_ = Task.Run(() =>
				{
					try
					{
						subscription.RequestRefresh(() => IsExecuting = false);
					}
					catch (Exception error)
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn(error, "Failed to send a refresh request");
						}
						IsExecuting = false;
					}
				});
			}
		}
	}
}
