# Desktop Image Pin

[日本語版 README](README.ja.md)

Desktop Image Pin is a lightweight Windows joke/utility app that lets you place images directly on your desktop without ordinary window frames or title bars.

Each image appears as its own transparent window and can be moved, resized, duplicated, replaced, layered, or removed independently.

## Features

- Display multiple images at the same time
- Transparent, borderless image windows
- Drag images to move them
- Mouse wheel to resize proportionally
- `Ctrl + Mouse Wheel` to resize width only
- `Alt + Mouse Wheel` to resize height only
- Set each image to Always on Top, Normal, or Back
- Replace, duplicate, or remove individual images
- Drag and drop multiple image files into the Hub or an existing image
- Restore image paths, positions, horizontal/vertical scales, and layer settings on the next launch
- System tray support
- `Ctrl + Shift + H` to show or hide the Hub
- Automatically fit oversized images within 90% of the desktop work area

Supported formats: PNG, JPEG, BMP, GIF, TIFF. Animated GIFs display their first frame.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK for building from source

## Run from Source

```powershell
dotnet run
```

## Build

```powershell
dotnet build -c Release
```

Create a self-contained single-file executable for Windows x64:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o publish/win-x64
```

## Controls

| Action | Control |
| --- | --- |
| Move an image | Left-drag |
| Resize proportionally | Mouse wheel |
| Resize width only | `Ctrl + Mouse Wheel` |
| Resize height only | `Alt + Mouse Wheel` |
| Open image menu | Right-click |
| Show/hide Hub | `Ctrl + Shift + H` |

## Saved Data

Layout data is stored in:

```text
%LocalAppData%\DesktopImagePin\images.json
```

The app stores image paths and layout settings only. It does not copy or upload image files.

## Notes

- Closing the Hub hides it; it does not exit the app.
- Exit from the Hub or system tray menu.
- A missing image file is skipped during startup restoration.
- Another application may already use `Ctrl + Shift + H`; the app will show a warning if registration fails.

## License

MIT License. See [LICENSE](LICENSE).
