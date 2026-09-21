# Development Log

This log summarizes repository-visible development milestones. It does not claim download counts, active users, or adoption metrics.

## 2026-06-06 - Local Prototype

- Created the .NET 8 WPF application and transparent image-window model.
- Added multiple independent image windows, drag movement, wheel scaling, replacement, deletion, and per-image display layers.
- Added the Hub window and global `Ctrl + Shift + H` hotkey.
- Added automatic initial scaling for images larger than the desktop work area.

## 2026-06-07 - Persistence And Interaction

- Added layout restoration for image paths, positions, horizontal and vertical scales, and display layers.
- Added drag-and-drop import, system tray support, and image duplication.
- Added independent width and height scaling with modifier keys.
- Added click-through mode, opacity, rotation, flipping, clipboard import, URL import, and displayed-image count.
- Published the initial public repository and tagged `v1.0.0` and `v1.1.0`.
- Fixed a URL-import file-lock issue by closing the download stream before image validation.

## 2026-06-14 - OSS Readiness

- Integrated a named URL import library that stores cached local copies.
- Added a solution and automated tests for persistence, transform normalization, and import path handling.
- Added CI for formatting, build, tests, dependency audit, and publish artifacts.
- Expanded English and Japanese documentation.
- Added contribution, security, roadmap, changelog, issue-template, pull-request-template, and dependency-license documentation.
- Added actual application screenshots and release preparation for `v1.2.0`.

## 2026-06-29 - Startup Option

- Added a Hub checkbox for optional Windows startup registration.
- Implemented startup registration through the current-user `Run` registry key so administrator rights are not required.
- Added unit tests for startup command generation and enable/disable behavior without touching the real registry.

## 2026-09-21 - Grouping And Recovery

- Integrated group selection, shared movement and scaling, and persisted group membership from local development.
- Added debounced autosave, validated backups, single-instance activation, and off-screen recovery.
- Audited restoration, backup replacement, duplication failures, screen geometry, and group transforms.
- Preserved unavailable image registrations and added Hub actions to retry or relink them.
- Added regression coverage for missing and corrupt files, repeated backup recovery, invalid JSON contents, monitor gaps, grouped rescue, rotated scaling, and scale limits.
- Kept existing English model labels, startup registry behavior, and public tests while updating the English and Japanese guides.
- Prepared version 1.4.0 with an English Windows x64 executable and the existing CI/release workflow.
