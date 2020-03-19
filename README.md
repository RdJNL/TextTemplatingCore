# T4 Text Templating with .NET Core
The software in this repository allows you to run a T4 template with the .NET Core 3.1 runtime. This means you can load .NET Core 3.1 assemblies and use .NET Core 3.1 libraries. My team uses it to generate C# code for types in a Core assembly (using Reflection). We also use it to process a JSON file using .NET Core's System.Text.Json library.

__Author:__ RdJNL

## Latest release
Version 1.0.0 can be downloaded [here](https://github.com/RdJNL/TextTemplatingCore/releases/download/v1.0.0/TextTemplatingCore_v1.0.0.zip).

## Requirements
- .NET Core 3.1
- Visual Studio 2019 (for the VS extension)
- .NET Framework 4.8 (for the TextTransformCore executable)

## How to use it

### Visual Studio 2019 extension
- The extension is provided as a VSIX file. Run this file to install the extension to Visual Studio. You may need to use an elevated command prompt to run the file as admin.
- Once the extension is installed, open your solution in Visual Studio.
- Select the `.tt` file in the Solution Explorer, right click it and click properties.
- In the properties window, set _Custom Tool_ to `TextTemplatingFileGeneratorCore`.
- Right click the file in the Solution Explorer and click _Run Custom Tool_.

### TextTransformCore executable
To use the executable, simply run it from the command line. Provide as first and only argument the path to the template file. If the path has spaces in it, makes sure to surround it with double quotes (`"`).

## Limitations
There are several limitations to this approach:
- The hostspecific setting __must__ be false.
- No debugging.
- No template parameters.
- The VS extension cannot be used in database projects (this seems to be a bug in Visual Studio).

## Extra feature
This processor has one feature that Visual Studio's processor does not have:
- From a T4 template, you can access the `TemplateFile` string property to get the absolute path to the template file.

## How does it work?
The following steps are followed to process the T4 template:
- .NET Framework 4.8 code (either the VS extension or the TextTransformCore executable) uses Visual Studio's T4 template processor to preprocess the template into C# code. This C# code is saved to a temporary file.
- The .NET Framework code runs a .NET Core 3.1 executable which compiles the C# code and runs it. The result is once again saved to a temporary file.
- The .NET Framework code loads the content of the temporary file and either passes it to Visual Studio (the VS extension) or saves it to the output file (the TextTransformCore executable).

## License
The license for all contents of the repository can be found in the _LICENSE_ file.
