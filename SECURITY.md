# Security Policy

## Supported Versions

Security fixes are applied to the latest release and the `main` branch.

## Reporting A Vulnerability

Please use GitHub's private security advisory feature instead of a public issue. Include:

- affected version and Windows version
- reproduction steps
- expected and actual behavior
- whether local files, cached URL images, or saved paths are involved

Do not upload private images, credentials, access tokens, personal paths, or `%LocalAppData%\DesktopImagePin` data publicly.

## Security Boundaries

- The app reads image files explicitly selected, dropped, pasted, restored, or downloaded by the user.
- URL imports accept HTTP/HTTPS only, enforce a 25 MB limit, and validate downloaded data as an image.
- State is stored locally; the app has no telemetry, account system, or cloud synchronization.
