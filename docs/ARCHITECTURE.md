# Architecture

Desktop Image Pin is a single-process .NET 8 WPF application.

## Main Components

- `App` owns application lifetime, state restoration, tray integration, and shutdown persistence.
- `ImageManager` owns the active `ImageItem` collection and coordinates image-window operations.
- `ImageWindow` renders one transparent borderless window per image and handles direct mouse interaction.
- `HubWindow` displays the active collection and URL import library.
- `ImageStateStore` serializes display state to local JSON.
- `ImageImportService` handles clipboard and bounded HTTP/HTTPS image imports.
- `UrlImportLibrary` stores named URL entries separately from active image layout state.
- `GlobalHotkeyService` and `TrayIconService` isolate Windows integration.

## Persistence Separation

Active desktop layout and reusable URL imports are intentionally separate:

- `images.json` stores what is currently displayed and how.
- `url-imports.json` stores named reusable URL sources and local cache paths.

This separation allows future layout profiles or cache management without coupling them to window rendering.

## Windows Interop

The app uses `RegisterHotKey`, extended window styles, and `SetWindowPos` for the global Hub shortcut, click-through behavior, and bottommost placement. These behaviors are Windows-specific and require manual verification in addition to unit tests.
