# Contributing

Thank you for considering a contribution to Desktop Image Pin.

## Before Starting

- Check existing issues and pull requests.
- Open an issue before a large behavioral or architectural change.
- Keep changes focused; avoid unrelated refactors.
- Do not commit personal image files, saved layouts, downloaded URL caches, or build outputs.

## Development Setup

Requirements: Windows 10/11 and the .NET 8 SDK.

```powershell
git clone https://github.com/shunufy/desktop-image-pin.git
cd desktop-image-pin
dotnet restore DesktopImagePin.sln --locked-mode
dotnet build DesktopImagePin.sln -c Release --no-restore
dotnet test DesktopImagePin.sln -c Release --no-build
```

## Pull Requests

1. Create a branch from `main`.
2. Add tests for testable behavior.
3. Run build, tests, and formatting checks.
4. Describe user-visible changes and manual Windows verification.
5. Update documentation and `CHANGELOG.md` when behavior changes.

```powershell
dotnet format DesktopImagePin.sln --verify-no-changes --no-restore
dotnet build DesktopImagePin.sln -c Release --no-restore
dotnet test DesktopImagePin.sln -c Release --no-build
```

By contributing, you agree that your contribution is licensed under the repository's MIT License.
