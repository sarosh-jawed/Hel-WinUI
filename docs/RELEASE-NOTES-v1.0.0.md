# Hel v1.0.0

Hel is a WinUI 3 desktop utility for classifying library monthly missing-items CSV exports into recipient-ready TXT lists, with preview, run summaries, and logs.

## What it does

- loads a monthly missing-items CSV
- limits processing to William Allen White Library records
- lets staff filter by WAWL location
- applies location and call-number routing rules
- previews assigned and unassigned results before export
- generates recipient-ready TXT files and a run summary

## Included release assets

- `Hel-v1.0.0-x64.msix`
- `Hel-v1.0.0-win-x64.zip`
- `SHA256SUMS.txt`
- optional public certificate file if needed for local trust

## Install notes

### Option 1 - MSIX install
Install the `.msix` package directly.

If Windows reports the publisher is not trusted, import the included public certificate (`.cer`) into the current user's **Trusted People** store, then retry installation.

### Option 2 - ZIP release bundle
The ZIP bundle contains the MSIX installer, install notes, config override example, and checksums.

## Config override

The app ships with `config.json` inside the app package.

A machine-specific override can be added here:

`%LOCALAPPDATA%\Hel\config.local.json`

This is useful for local rule changes or path overrides without modifying the packaged defaults.

## Defaults

- logs: `%LOCALAPPDATA%\Hel\Logs`
- output: user-selected folder

## Known limitations

- current release is focused on William Allen White Library routing rules
- CSV schema must match the configured required column names
- release package is x64 only