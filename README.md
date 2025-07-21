# Archival Notice

As outlined in issue #136 we were looking for maintainers, but nobody stepped forward. Thus it has been unmaintained for a very long period of time, 
and we decided to archive the repository.

Alternative: [ilspy-vscode](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode) has more features than AvaloniaILSpy ever had and
is available on all platforms thanks to VS Code. See [feature comparison](https://github.com/icsharpcode/ilspy-vscode/wiki/Feature-Comparison) for what is 
not there compared to the WPF/Windows version of ILSpy. All future xplat work is focused on the VS Code extension.

We do not intend to resurrect a "copy of ILSpy with UI framework XYZ" as this project has proven how hard it is to keep a copy up to date.


# AvaloniaILSpy ![Build AvaloniaILSpy](https://github.com/icsharpcode/AvaloniaILSpy/workflows/Build%20AvaloniaILSpy/badge.svg?branch=master)

This is cross-platform version of [ILSpy](https://github.com/icsharpcode/ILSpy) built with [Avalonia](https://github.com/AvaloniaUI/Avalonia).

![](https://github.com/icsharpcode/AvaloniaILSpy/raw/master/preview.png)

Supported Features 
-------
 * Decompilation
 * Analyze Window
 * Search for types/methods/properties (substring)
 * Hyperlink-based type/method/property navigation
 * Extensible via MEF Extensibility (Check out TestPlugin folder). Note: This is not compatible with ILSpy Plugins.
 * Check out [feature support status](https://github.com/icsharpcode/AvaloniaILSpy/issues/1)

# Download

## Stable Release

https://github.com/icsharpcode/AvaloniaILSpy/releases

## Bleeding-edge Builds
Grab artifacts from the [latest master CI build](https://github.com/icsharpcode/AvaloniaILSpy/actions?query=workflow%3A%22Build+AvaloniaILSpy%22+branch%3Amaster+is%3Asuccess).
This includes Linux, Mac and Windows.

How to run on Linux: 
- just open it
- if you have trouble, please try to grant it the rights to execute `chmod a+x ILSpy`
- you could also run it in command line by `./ILSpy`

How to run on Mac:
- just move the app into `/Applications` folder and open it
- run `xattr -rd com.apple.quarantine /Applications/ILSpy.app`
- if you see `“ILSpy” cannot be opened because the developer cannot be verified.`, please open up `System Preferences` -> `Security & Privacy` -> `General` -> `Open Anyway` 
- if you see the error `The application ILSpy can't be opened' error on launch`, you could `chmod +x "/Applications/ILSpy.app/Contents/MacOS/ILSpy"`

# Build from sources

1. Install dotnet 6 or above from https://dotnet.microsoft.com/en-us/download/dotnet
2. Clone repository : `git clone https://github.com/icsharpcode/AvaloniaILSpy.git`.
3. Run build script: `dotnet tool restore` and `dotnet cake`
4. Artifacts will be located in subdirectory `artifacts`.
