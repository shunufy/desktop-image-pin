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
- group membership, spacing, rotated screen-axis scaling, and shared size limits
- off-screen recovery across separate work areas, including monitor gaps and negative coordinates
- backup recovery followed by saving and another corrupt-primary recovery
- invalid state contents, such as null entries and out-of-range values
- retention, retry, relinking, and explicit removal of unavailable registrations
- duplication after the source file is removed
- restoration notification suppression and later change notifications
- startup registry behavior and single-instance activation signaling
- complete URL-download deadlines, stream failures, size limits, and partial-file cleanup
- Hub actions at minimum width, group controls after image-window changes, and selection of 1,000 rows
- recycling virtualized Hub rows without modifying opacity or triggering saves
- recovery geometry at 100%, 125%, 150%, 175%, and 200% system scaling, 1,024-member oversized groups, and scaling a 2,048-member rotated group

WPF tests use temporary generated images. They create native image-window handles without showing or activating the windows, then check scaling and closure; pixel-position comparisons allow DPI-dependent rounding. Hub tests measure and exercise the actual controls without creating a native Hub handle, registering its hotkey, or starting the application. Temporary state paths, an in-memory startup store, and injected work-area/display providers isolate the tests from the user's saved layout, URL library, clipboard, and startup registry.

The scaled geometry tests simulate different system DPI values; they do not replace physical mixed-DPI monitor dragging. Large-group tests cover model transforms, recovery geometry, and virtualized Hub controls, not the memory/rendering cost of thousands of simultaneously visible native windows.

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
13. Select multiple rows with Ctrl/Shift, group them, and move/scale from different members.
14. Rotate one member by 90 degrees and verify horizontal/vertical scaling follows screen axes.
15. Disconnect an image drive, restart, and verify unavailable registrations survive saving; reconnect and Retry.
16. Move a group off-screen and use Recover Off-screen without changing its relative spacing.
17. Verify monitor changes and dragging between monitors with different DPI settings.
18. Launch the executable again and verify the existing Hub opens instead of another app instance.
19. Wait for autosave after an edit, then restart and verify the result.
