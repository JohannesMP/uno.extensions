namespace MyExtensionsApp;

public sealed class AppResources : ResourceDictionary
{
	public AppResources()
	{
		// Load WinUI Resources
		this.Build(r => r.Merged(
			new XamlControlsResources()));
#if useMaterial

		// Load Material Resources
		this.Build(r => r.Merged(
			new MaterialTheme()
				.ColorOverrideDictionary(new Styles.ColorPaletteOverride())
				.FontOverrideDictionary(new Styles.MaterialFontsOverride())));

		// Load Uno.UI.Toolkit Resources
		this.Build(r => r.Merged(
			new ToolkitResources(),
			new MaterialToolkitResources()));
#elif (useRecommendedAppTemplate)

		// Load Uno.UI.Toolkit Resources
		this.Build(r => r.Merged(
			new ToolkitResources()));
#endif
	}
}
