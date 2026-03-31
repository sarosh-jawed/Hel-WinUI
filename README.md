# Hel-WinUI

![CI](https://github.com/sarosh-jawed/Hel-WinUI/actions/workflows/ci.yml/badge.svg)

Desktop utility for classifying library "Monthly Missing Items" CSV exports into recipient-ready TXT lists, with run summaries and logs.

## About

Hel-WinUI is a staff-facing WinUI 3 desktop utility built for classifying monthly missing-items CSV exports for William Allen White Library.

The application supports:

- WAWL library scoping
- location-based filtering
- location-rule override routing
- call-number prefix routing
- Dewey range routing
- fallback from `items.effective_call_number` to `holdings.call_number`
- unassigned routing for unreadable or unmatched records
- recipient-ready TXT exports
- run summary generation
- structured preview and review before export

## Current workflow

1. Load the CSV export
2. Select WAWL locations
3. Preview assigned and unassigned routing
4. Review data-quality warnings
5. Generate TXT outputs and run summary

## Project structure

```text
Hel/
  Hel.App.WinUI/         WinUI 3 desktop app
  Hel.Application/       contracts and configuration models
  Hel.Domain/            core records and value objects
  Hel.Infrastructure/    CSV ingest, filtering, classification, export
  Hel.Tests/             xUnit test suite and fixtures

docs/
  TEST-CASES.md          manual validation cases
  SCREENSHOTS.md         screenshot naming guide
  screenshots/           current UI screenshots
