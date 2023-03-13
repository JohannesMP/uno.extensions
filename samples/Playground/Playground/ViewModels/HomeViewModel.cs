﻿
using System.Globalization;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Localization;

namespace Playground.ViewModels;

public class HomeViewModel
{
	public string? Platform { get; }

	public string UseMock { get; }

	private readonly IWritableOptions<LocalizationSettings> _localization;
	public HomeViewModel(
		IOptions<AppInfo> appInfo,
		IOptions<LocalizationConfiguration> configuration,
		IWritableOptions<LocalizationSettings> localization,
		IStringLocalizer localizer)
	{
		_localization = localization;
		Platform = appInfo.Value.Platform;
		SupportedCultures = configuration.Value?.Cultures?.AsCultures() ?? new[] { "en-US".AsCulture()! }; 

		var language = localizer[_localization.Value?.CurrentCulture ?? "en"];

		UseMock = (appInfo.Value?.Mock ?? false) ? "Mock ENABLED" : "Mock DISABLED";
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture {
		get => SupportedCultures.FirstOrDefault(x=>x.Name == _localization.Value?.CurrentCulture)?? SupportedCultures.First();
		set
		{
			_ = _localization.UpdateAsync(settings => settings with { CurrentCulture = value.Name });
		}
	}

}
