# Hel Conventions

## Solution & Project Naming
- Solution: Hel
- Projects:
  - Hel.Domain
  - Hel.Application
  - Hel.Infrastructure
  - Hel.App.WinUI

## Namespace Rules
- Namespace matches project name.
- Folders map to namespaces (e.g., Hel.Application.Services).
- Avoid `using Hel.Application;` in the UI project unless required (prevents conflicts with WinUI `Application` type).

## Dependency Direction (Clean Architecture)
- Hel.Domain: no dependencies
- Hel.Application → references Hel.Domain
- Hel.Infrastructure → references Hel.Application + Hel.Domain
- Hel.App.WinUI → references Hel.Application + Hel.Infrastructure + Hel.Domain
- No reverse references allowed.

## Logging
- Prefer `ILogger<T>` per class.
- High-level categories used in logs should start with `Hel.`.
- Log style:
  - Start of operation: “Starting …”
  - Completion: “Completed …”
  - Use structured logging for variables.

## Output Folder Strategy
Default output root:
- `%LOCALAPPDATA%\Hel\Output\YYYY-MM\`
- Final folder naming derived from run month (configurable later).

## File/Code Style
- PascalCase: classes, methods, public properties.
- Private fields: `_camelCase`.
- One public type per file.
- No abbreviations unless widely understood (CSV, UI, DI).
