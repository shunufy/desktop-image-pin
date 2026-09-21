# Architecture

Desktop Image Pin is a single-process .NET 8 WPF application.

## Main Components

- `App` owns application lifetime, state restoration, tray integration, debounced autosave, and shutdown persistence.
- `ImageManager` owns registered `ImageItem` entries, including unavailable images, and coordinates image-window and group operations. Restoration suppresses save notifications until the collection has been populated.
- `ImageWindow` renders one transparent borderless window per image and handles direct mouse interaction.
- `HubWindow` displays the active collection and URL import library.
- `ImageStateStore` validates and serializes display state to local JSON. Only valid primary data is promoted to the backup.
- `GroupTransformCalculator` computes positions and local-axis scales, accounting for rotation and shared proportional limits.
- `DesktopWorkAreaProvider` maps monitor work areas into WPF coordinates; `WindowPlacementService` calculates one recovery offset per group.
- `ImageImportService` handles clipboard and bounded HTTP/HTTPS image imports.
- `UrlImportLibrary` stores named URL entries separately from active image layout state.
- `GlobalHotkeyService`, `TrayIconService`, `StartupService`, and `SingleInstanceService` isolate Windows integration. A named mutex and event reject duplicate launches and request activation of the existing Hub.

## Persistence Separation

Active desktop layout and reusable URL imports are intentionally separate:

- `images.json` stores registered images and their settings, even if their files are temporarily unavailable. Nullable `GroupId` values preserve compatibility with older layouts.
- `images.backup.json` stores the previous validated generation. Recovery never replaces this file with invalid primary data.
- `url-imports.json` stores named reusable URL sources and local cache paths.

This separation allows future layout profiles or cache management without coupling them to window rendering.

An unavailable entry keeps its settings but has no `ImageWindow`. The Hub can retry the same path, relink it through Change, or explicitly remove it. Group transforms also update unavailable members so their settings remain consistent when retried.

## Windows Interop

The app uses `RegisterHotKey`, extended window styles, `SetWindowPos`, and the current-user `Run` registry key for the global Hub shortcut, click-through behavior, bottommost placement, and optional Windows startup registration. These behaviors are Windows-specific and require manual verification in addition to unit tests.
