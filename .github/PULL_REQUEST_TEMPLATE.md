## Summary

Describe the user-visible or maintenance change.

## Validation

- [ ] `dotnet format DesktopImagePin.sln --verify-no-changes --no-restore`
- [ ] `dotnet build DesktopImagePin.sln -c Release --no-restore -warnaserror`
- [ ] `dotnet test DesktopImagePin.sln -c Release --no-build`
- [ ] Manual Windows behavior checked when applicable

## Documentation

- [ ] README or supporting docs updated when behavior changed
- [ ] `CHANGELOG.md` updated for user-visible changes

## Privacy

- [ ] No personal paths, private images, tokens, cache files, or saved local state are included
