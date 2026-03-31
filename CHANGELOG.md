# Changelog
All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [1.0.0] - 2026-03-31
### Added
- WinUI 3 sidebar stepper workflow with Start, Load CSV, Select Locations, Preview Results, and Export & Finish pages.
- Shared wizard session state and deterministic step locking.
- Preview summary cards, recipient bucket previews, unassigned preview, and warning bars.
- Routing reason visibility in preview results.
- Export finish flow with generated-file listing and folder shortcuts.
- App icon assets and custom window icon support.
- Expanded automated test coverage, including golden-file export validation.
- GitHub Actions CI workflow for restore, build, and test.
- README screenshots and public-facing project documentation.
- Release bundle script for v1.0.0 packaging.

### Changed
- Default logs path now uses `%LOCALAPPDATA%\Hel\Logs`.
- Generated TXT files now end with `Thanks` only.
- Package display name and release metadata aligned for v1.0.0.

### Fixed
- CI release build no longer depends on publish-only settings.
- App package assets and store logo paths aligned with build output.
