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

WPF tests use temporary generated images. They include native image-window creation, scaling, and closure; pixel-position comparisons allow DPI-dependent rounding. Tests do not use the user's saved layout or modify the real startup registry. Temporary state paths and injected work-area/display providers isolate recovery tests.

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
