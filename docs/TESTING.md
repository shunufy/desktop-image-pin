# Testing

## Automated

Run from the repository root:

```powershell
dotnet restore DesktopImagePin.sln --locked-mode
dotnet format DesktopImagePin.sln --verify-no-changes --no-restore
dotnet build DesktopImagePin.sln -c Release --no-restore
dotnet test DesktopImagePin.sln -c Release --no-build
```

Automated tests currently cover:

- rotation normalization and transform descriptions
- display-layer descriptions
- JSON state save/load round trips
- malformed state-file handling
- URL extension and media-type mapping
- managed import-directory boundary checks
- named URL library loading and cache-preserving removal

## Manual Windows Checklist

1. Add PNG, JPEG, BMP, GIF, and TIFF images.
2. Add an oversized image and confirm it initially fits the work area.
3. Move and resize an image proportionally.
4. Resize width and height independently.
5. Change layer, opacity, rotation, flips, and click-through.
6. Duplicate, replace, and remove an image.
7. Add multiple images by drag and drop.
8. Import from clipboard and URL.
9. Save a named URL import, restart, and display it without refreshing.
10. Restart and confirm layout restoration.
11. Toggle the Hub with `Ctrl + Shift + H`.
12. Hide the Hub and reopen it from the tray.
