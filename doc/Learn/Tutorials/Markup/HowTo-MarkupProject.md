---
uid: Learn.Tutorials.HowToMarkupProject
---

# How to set up an C# Markup project

In this tutorial you'll learn how to set up an Uno Platform project to use C# Markup. 

1. Make sure your environment is set up properly by using [uno check](xref:UnoCheck.UsingUnoCheck).
1. You can create a Uno App by either using the Uno Platform Visual Studio extension or via the command line interface.

    # [**Visual Studio**](#tab/vs)

    #### Using Visual Studio 2022 Uno Platform Extension

    - Open the Visual Studio and select Extensions => Manage Extensions and Search (<kbd>Ctrl</kbd> + <kbd>E</kbd>) for unoplatform.

    - Make sure you have the latest version of Uno Extension (v5.0 or higher) installed.

        ![Screenshot displaying how to check the version of the Uno Extension wizard version in Visual Studio extension manager](../Assets/MarkupProject-VsixVersion.jpg)

    - Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> to create a new project and select "Uno Platform App".

    - Give your project an appropriate name (`MySampleProject` in this page).

    - When prompted Select *Blank* and click *Customize*

        ![Screenshot displaying the intro screen of the Uno Extension wizard in Visual Studio](../Assets/MarkupProject-StartupType.jpg)

    - In the *Markup* tab (4rd one), select C# Markup.

        ![Screenshot displaying how to pre-install C# Markup in the generated project](../Assets/MarkupProject-VsixMarkup.jpg)

    - Click *Create* on the bottom right corner.    

    # [**CLI**](#tab/cli)

    #### Using the command line interface

    - Run the following command, using an appropriate name (`MySampleProject` in this page).
    
        ```cmd
        dotnet new unoapp -preset blank -markup csharp -o MySampleProject
        ```

        Refer to [this](https://platform.uno/docs/articles/get-started-dotnet-new.html) article for more details
        on using the CLI interface of creating projects.

    - Launch the created solution, MySampleProject.sln, in  Visual Studio or Visual Studio Code.
