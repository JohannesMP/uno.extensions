﻿namespace Uno.Extensions;

public static class UIElementExtensions
{
	public static IThemeService GetThemeService(this UIElement element, ILogger? logger = default) =>
		new ThemeService(element, new Dispatcher(element), logger);
}
