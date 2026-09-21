# Changelog

All notable changes are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [1.4.0] - 2026-09-21

### Added

- Group and ungroup selected images from the Hub, with shared dragging and proportional or per-axis scaling.
- Persist group membership alongside existing layout settings.
- Autosave after 1.5 seconds without changes, with a validated backup generation.
- Single-instance handling that activates the existing Hub on a second launch.
- Per-monitor off-screen recovery at startup and from the Hub, preserving group spacing.
- Unavailable-image registrations with Retry and Change actions and a separate unavailable count.
- Regression tests for recovery, grouping, scale limits, monitor gaps, and unavailable files.

### Fixed

- Preserve missing and unreadable image registrations across saves and suppress save notifications during restoration.
- Keep valid backups when saving after recovery from a corrupt primary file.
- Validate saved state contents, including null entries and invalid numeric values, before restoration.
- Report duplication failures without allowing an exception to terminate the app.
- Apply axis-specific scaling in screen coordinates for rotated images and preserve aspect ratios at scale limits.
- Close image streams even when decoding fails so damaged files can be replaced and retried.

## [1.3.0] - 2026-06-29

### Added

- Hub option to start Desktop Image Pin automatically when Windows starts.
- Tests for startup registry command generation and enable/disable behavior.

## [1.2.0] - 2026-06-14

### Added

- Named URL import library with local caching, display, refresh, and removal actions.
- Automated tests for model behavior, persistence, and URL import path handling.
- GitHub Actions checks for formatting, build, tests, package audit, and Windows x64 publishing.
- OSS maintenance documents, issue templates, pull request template, roadmap, development log, and screenshots.

### Changed

- Enabled list virtualization in the Hub for better large-collection responsiveness.
- Expanded English and Japanese documentation.
- Clarified dependency and bundled-asset licensing.

### Fixed

- Closed URL download streams before image validation to prevent file-lock errors.

## [1.1.0] - 2026-06-07

- Added per-image click-through mode.
- Added opacity controls from 10% to 100%.
- Added 90-degree rotation and horizontal/vertical flipping from the Hub.
- Added clipboard image import and HTTP/HTTPS image URL import.
- Added persistent imported-image storage with a 25 MB URL download limit.
- Added a live displayed-image count in the Hub.

## [1.0.0] - 2026-06-07

- Initial public release.
- Multiple transparent desktop image windows.
- Move, proportional resize, width-only resize, and height-only resize.
- Per-image layering, replacement, duplication, and removal.
- Multi-file drag and drop.
- Layout persistence and restoration.
- System tray integration and global Hub hotkey.

[Unreleased]: https://github.com/shunufy/desktop-image-pin/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/shunufy/desktop-image-pin/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/shunufy/desktop-image-pin/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/shunufy/desktop-image-pin/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/shunufy/desktop-image-pin/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/shunufy/desktop-image-pin/releases/tag/v1.0.0
