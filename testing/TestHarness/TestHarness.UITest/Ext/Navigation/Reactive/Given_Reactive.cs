﻿namespace TestHarness.UITest;

public class Given_Reactive : NavigationTestBase
{
	[Test]
	public void When_ReactiveXAML()
	{
		InitTestSection(TestSections.Navigation_Reactive);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageButton");
		var screenBefore = TakeScreenshot("When_PageNavigationXAML_Before");
		App.WaitThenTap("OnePageToTwoPageButton");
		App.WaitThenTap("TwoPageToThreePageButton");
		App.WaitThenTap("ThreePageToFourPageButton");
		App.WaitThenTap("FourPageToFivePageButton");
		App.WaitThenTap("FivePageToSixPageButton");
		App.WaitThenTap("SixPageBackButton");
		App.WaitThenTap("FivePageBackButton");
		App.WaitThenTap("FourPageBackButton");
		App.WaitThenTap("ThreePageBackButton");
		App.WaitThenTap("TwoPageBackButton");


		App.WaitElement("OnePageToTwoPageButton");
		var screenAfter = TakeScreenshot("When_PageNavigationXAML_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_ReactiveCodebehind()
	{
		InitTestSection(TestSections.Navigation_Reactive);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageCodebehindButton");
		var screenBefore = TakeScreenshot("When_PageNavigationCodebehind_Before");
		App.WaitThenTap("OnePageToTwoPageCodebehindButton");
		App.WaitThenTap("TwoPageToThreePageCodebehindButton");
		App.WaitThenTap("ThreePageToFourPageCodebehindButton");
		App.WaitThenTap("FourPageToFivePageCodebehindButton");
		App.WaitThenTap("FivePageToSixPageCodebehindButton");
		App.WaitThenTap("SixPageBackCodebehindButton");
		App.WaitThenTap("FivePageBackCodebehindButton");
		App.WaitThenTap("FourPageBackCodebehindButton");
		App.WaitThenTap("ThreePageBackCodebehindButton");
		App.WaitThenTap("TwoPageBackCodebehindButton");


		App.WaitElement("OnePageToTwoPageCodebehindButton");
		var screenAfter = TakeScreenshot("When_PageNavigationCodebehind_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));
	}

	[Test]
	public void When_ReactiveViewModel()
	{
		InitTestSection(TestSections.Navigation_Reactive);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton");
		App.WaitThenTap("ThreePageToFourPageViewModelButton");
		App.WaitThenTap("FourPageToFivePageViewModelButton");
		App.WaitThenTap("FivePageToSixPageViewModelButton");
		App.WaitThenTap("SixPageBackViewModelButton");
		App.WaitThenTap("FivePageBackViewModelButton");
		App.WaitThenTap("FourPageBackViewModelButton");
		App.WaitThenTap("ThreePageBackViewModelButton");
		App.WaitThenTap("TwoPageBackViewModelButton");


		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenAfter = TakeScreenshot("When_PageNavigationViewModel_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}

	[Test]
	public void When_ReactiveDependsOnWithData()
	{
		InitTestSection(TestSections.Navigation_Reactive);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenBefore = TakeScreenshot("When_PageNavigationViewModel_Before");

		// Iterate through pages 1 to 3 - this caused an issue
		// where NavigationCacheMode is set to required
		// see https://github.com/unoplatform/uno.extensions/issues/2097
		App.WaitThenTap("OnePageToTwoPageViewModelButton");
		App.WaitThenTap("TwoPageToThreePageViewModelButton");
		App.WaitThenTap("ThreePageBackViewModelButton");
		App.WaitThenTap("TwoPageBackViewModelButton");
		App.WaitElement("OnePageToTwoPageViewModelButton");


		App.WaitThenTap("OnePageToThreePageDataButton");
		App.WaitElement("ThreePageWidgetNameTextBlock");
		var text = App.GetText("ThreePageWidgetNameTextBlock");
		Assert.AreEqual("From One", text);

		App.WaitThenTap("ThreePageBackViewModelButton");
		text = App.GetText("TwoPageWidgetNameTextBlock");
		Assert.AreEqual("Adapted model", text);


		App.WaitThenTap("TwoPageBackViewModelButton");


		App.WaitElement("OnePageToTwoPageViewModelButton");
		var screenAfter = TakeScreenshot("When_PageNavigationViewModel_After");
		ImageAssert.AreEqual(screenBefore, screenAfter, tolerance: PixelTolerance.Exclusive(Constants.DefaultPixelTolerance));

	}
}
