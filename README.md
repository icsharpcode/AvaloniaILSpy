# AvaloniaILSpy [![appveyor](https://ci.appveyor.com/api/projects/status/github/icsharpcode/AvaloniaILSpy?svg=true)](https://ci.appveyor.com/project/icsharpcode/avaloniailspy) 

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
| Version | Installers (for x 64) |
|---------|------------|
|**Windows**|[Download](https://ci.appveyor.com/api/projects/icsharpcode/avaloniailspy/artifacts/artifacts%2Fzips%2FILSpy-win7-x64-Release.zip?branch=master)|
|**macOS**|[Download](https://ci.appveyor.com/api/projects/icsharpcode/avaloniailspy/artifacts/artifacts%2Fzips%2FILSpy-osx-x64-Release.zip?branch=master)|
|**Linux**|[Download](https://ci.appveyor.com/api/projects/icsharpcode/avaloniailspy/artifacts/artifacts%2Fzips%2FILSpy-linux-x64-Release.zip?branch=master)|

How to run on Linux: 
- make sure you have installed `ttf-ms-fonts` package
- grant it the rights to execute `chmod a+x ILSpy`
- run  `./ILSpy`

How to run on Mac:
- just move the app into `/Applications` folder and open it
- if you see the error `The application ILSpy can't be opened' error on launch`, you could `chmod +x "/Applications/ILSpy.app/Contents/MacOS/ILSpy"`

# Build from sources

1. Clone repository with submodules: `git clone --recurse-submodules`.
2. Run build script: `build.ps1` on Windows and `./build.sh` on Linux and Mac OS.
3. Artifacts will be located in subdirectory `artifacts`.
