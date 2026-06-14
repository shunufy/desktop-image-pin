# Third-Party Notices

## Runtime

Desktop Image Pin has no third-party NuGet package dependencies at runtime.

It is built with .NET 8, WPF, and Windows Forms components from Microsoft:

- [.NET Runtime](https://github.com/dotnet/runtime) - MIT
- [WPF](https://github.com/dotnet/wpf) - MIT
- [Windows Forms](https://github.com/dotnet/winforms) - MIT

The self-contained release includes .NET runtime components under their applicable Microsoft and .NET Foundation notices.

## Test Dependencies

NuGet package metadata was checked for the direct and transitive test dependencies:

- Apache-2.0: `xunit`, `xunit.runner.visualstudio`, `xunit.abstractions`, `xunit.analyzers`, `xunit.assert`, `xunit.core`, `xunit.extensibility.core`, and `xunit.extensibility.execution`
- MIT: `Microsoft.NET.Test.Sdk`, `Microsoft.CodeCoverage`, `Microsoft.TestPlatform.ObjectModel`, `Microsoft.TestPlatform.TestHost`, `Newtonsoft.Json`, `System.Collections.Immutable`, and `System.Reflection.Metadata`

These packages are development-only and are not bundled into `DesktopImagePin.exe`. Exact resolved versions are recorded in `packages.lock.json` files.

## Assets

- No third-party images are bundled with the application.
- No custom font files are bundled; the UI uses Windows/WPF system fonts.
- No third-party icon file is bundled. The tray icon is extracted from the running executable, with the Windows system application icon as fallback.
- Documentation screenshots and their simple sample images were created for this repository.
