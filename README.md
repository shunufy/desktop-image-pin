# Desktop Image Pin

[![CI](https://github.com/shunufy/desktop-image-pin/actions/workflows/ci.yml/badge.svg)](https://github.com/shunufy/desktop-image-pin/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

[日本語 README](README.ja.md)

Desktop Image Pin places images directly on the Windows desktop as independent, transparent, borderless windows. It is built for people who want reference art, stream decorations, visual reminders, cutouts, or playful desktop layouts without the chrome and single-canvas workflow of a conventional image viewer.

Unlike a normal viewer, every image can be moved, resized, layered, made click-through, transformed, duplicated, and restored independently. A central Hub keeps large collections manageable while the images themselves remain visually unobtrusive.

![Desktop Image Pin Hub](docs/images/hub.png)

The screenshot shows an earlier release. Version 1.4 also includes grouping, retry, and off-screen recovery controls.

## Who It Helps

- Creators who keep visual references visible while working
- Streamers and presenters who arrange transparent image overlays
- Users who want persistent desktop decorations or reminders
- Anyone who needs many independently positioned images instead of one viewer window

## Features

- Display multiple images as transparent, borderless windows
- Move each image with left-drag
- Group selected images to move and resize them together while preserving their spacing
- Resize proportionally with the mouse wheel
- Resize width only with `Ctrl + Mouse Wheel`
- Resize height only with `Alt + Mouse Wheel`
- Automatically fit oversized images within 90% of the desktop work area
- Set each image to Always on Top, Normal, or Back
- Enable click-through mode per image
- Adjust opacity from 10% to 100%
- Rotate by 90 degrees and flip horizontally or vertically
- Replace, duplicate, or remove images independently
- Drag and drop multiple files into the Hub or an image window
- Import from the clipboard or an HTTP/HTTPS URL
- Save named URL imports locally and reuse them without downloading on every launch
- Restore paths, positions, independent X/Y scales, layers, opacity, transforms, and click-through state
- Autosave after about 1.5 seconds without changes, with one validated backup generation
- Keep missing or unreadable images registered so they can be retried or relinked
- Recover off-screen images across monitor work areas without collapsing group layouts
- Use the system tray while the Hub is hidden
- Reopen the existing Hub when launching an already-running instance
- Start automatically when Windows starts
- Toggle the Hub globally with `Ctrl + Shift + H`
- View the current displayed-image count

## Supported Formats

| Format | Extensions | Notes |
| --- | --- | --- |
| PNG | `.png` | Transparency supported |
| JPEG | `.jpg`, `.jpeg` | Static images |
| BMP | `.bmp` | Static images |
| GIF | `.gif` | First frame only |
| TIFF | `.tif`, `.tiff` | Static images |

URL downloads are limited to 25 MB and are validated as images before use.

## Install

### Release executable

1. Download `DesktopImagePin.exe` from the [latest GitHub Release](https://github.com/shunufy/desktop-image-pin/releases/latest).
2. Place it in a folder you control.
3. Exit any older running version, then double-click the executable.

The release is a self-contained Windows x64 executable; installing the .NET runtime separately is not required.

### Run from source

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

```powershell
git clone https://github.com/shunufy/desktop-image-pin.git
cd desktop-image-pin
dotnet restore DesktopImagePin.sln --locked-mode
dotnet run --project DesktopImagePin.csproj
```

## Controls

| Action | Control |
| --- | --- |
| Move an image | Left-drag |
| Open image menu | Right-click |
| Resize proportionally | Mouse wheel |
| Resize width only | `Ctrl + Mouse Wheel` |
| Resize height only | `Alt + Mouse Wheel` |
| Add multiple local images | Drag and drop into the Hub |
| Select multiple images | `Ctrl` or `Shift` while selecting rows in the Hub |
| Group selected images | Select at least two rows, then **Group** |
| Ungroup | Select a group member and choose **Ungroup**, or use its right-click menu |
| Recover off-screen images | **Recover Off-screen** in the Hub |
| Retry an unavailable image | **Retry** on its Hub row, or **Change** to choose a new file |
| Show or hide the Hub | `Ctrl + Shift + H` |
| Start with Windows | Enable **Start Desktop Image Pin when Windows starts** in the Hub |
| Restore a click-through image | Disable click-through from the Hub |
| Exit completely | Hub **Exit** button or tray menu |

The Hub contains two tabs:

- **Images** manages registered images, including unavailable files, and their groups, transforms, layer, opacity, replacement, and removal. The displayed-image count excludes unavailable entries.
- **Imports** stores named URL images in a local cache. **Display** reuses the cached file, while **Refresh** downloads it again.

## Grouping And Recovery

Dragging any group member moves the entire group. Wheel scaling changes both image sizes and spacing; `Ctrl` and `Alt` still select the horizontal and vertical screen axes, including for rotated images. Proportional scaling stops at the first member's size limit without changing aspect ratios. Duplicates start ungrouped.

Group membership survives a restart. Off-screen recovery moves a group by one shared offset. Groups larger than a monitor retain their arrangement while at least one member is made reachable.

If a drive is disconnected or an image cannot be decoded, the Hub keeps its registration and settings as **Unavailable**. Use **Retry** after the file becomes available, **Change** to relink it, or **Remove** to explicitly delete the registration. A failed duplication reports an error without closing the application.

## Saved Data

Desktop Image Pin stores data only on the local machine:

```text
%LocalAppData%\DesktopImagePin\images.json
%LocalAppData%\DesktopImagePin\images.backup.json
%LocalAppData%\DesktopImagePin\url-imports.json
%LocalAppData%\DesktopImagePin\ImportedImages\
```

Changes are saved after about 1.5 seconds of inactivity and on normal exit. Restoration does not trigger autosaving while items are still being loaded. `images.json` and `images.backup.json` contain local file paths and display settings; remove personal paths before sharing either file.

Saved JSON is checked for valid entries and values before use. Invalid primary data falls back to the backup, and a corrupt primary is never promoted over a valid backup. Older layouts without group metadata remain compatible.

The optional startup setting writes one current-user registry value:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\DesktopImagePin
```

After moving the executable, enable the startup checkbox in the new copy to register its current location.

## Build And Test

```powershell
dotnet restore DesktopImagePin.sln
dotnet format DesktopImagePin.sln --verify-no-changes --no-restore
dotnet build DesktopImagePin.sln -c Release --no-restore
dotnet test DesktopImagePin.sln -c Release --no-build
```

Publish a self-contained Windows x64 executable:

```powershell
dotnet publish DesktopImagePin.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o publish/win-x64
```

## Project Status

The project is maintained as a small Windows utility. Maintenance priorities are reliability, understandable behavior, safe local persistence, and keeping the dependency surface small. Planned work is tracked in [ROADMAP.md](ROADMAP.md) and GitHub Issues.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Testing](docs/TESTING.md)
- [Development log](DEVELOPMENT_LOG.md)
- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)

## License

Desktop Image Pin is available under the [MIT License](LICENSE).
