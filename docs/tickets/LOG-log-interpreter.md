# Epic LOG — Log Interpreter

**Project:** Digital Services Dev Dash
**Code:** `LOG`
**Scope:** A universally adaptable log viewer: pick an environment and application instance, read logs from environment-specific paths, parse using a format profile defined per **DeployableApplication**, and present human-readable output with filtering.

**Depends on:** APP-002, APP-004
**Blocks:** —

---

## Primary user story

> Logging is a mess across teams, but each deployable application should log the same way in every environment. I want to pick an environment, pick the running instance of an app, and read its logs in a sane format — with filters to hide noise like INFO lines.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [LOG-001](#log-001) | Done | LogFormatProfile per DeployableApplication | APP-001 |
| [LOG-002](#log-002) | Done | Log file reader using ApplicationInstance paths | APP-002, LOG-001 |
| [LOG-003](#log-003) | Done | Log viewer UI (environment → instance picker) | LOG-002, ENV-005, APP-004 |
| [LOG-004](#log-004) | Todo | Log filtering (level, text search) | LOG-003 |
| [LOG-005](#log-005) | Todo | Log file selection when path is a directory | LOG-003 |
| [LOG-006](#log-006) | Todo | Raw log content debug panel | LOG-003 |
| [LOG-007](#log-007) | Todo | Override log format profile on viewer | LOG-003 |

---

## Design notes

### Entity (`LogFormatProfile`)

One profile per **DeployableApplication** — same format across all environments.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `DeployableApplicationId` | `Guid` | FK, unique |
| `FormatName` | `string` | e.g. `SerilogJson`, `NLogXml`, `PlainText` |
| `ParserConfig` | `string` | JSON config: timestamp pattern, level token, message layout, multiline rules |
| `Notes` | `string?` | |
| `UpdatedAt` | `DateTimeOffset?` | |

**Parsed log entry (in-memory DTO):**

| Field | Type |
|-------|------|
| `Timestamp` | `DateTimeOffset?` |
| `Level` | `string?` — DEBUG, INFO, WARN, ERROR, etc. |
| `Message` | `string` |
| `RawLine` | `string` |
| `Properties` | `Dictionary<string,string>?` |

### Log source

- **Resolved path** on **ApplicationInstance.LogPath** (file or directory — if directory, read newest `*.log` today; **LOG-005** adds explicit file picker).
- **Template** on **DeployableApplication.PathToLogFiles** (APP-005) — used to *produce* `LogPath` when it is missing or incomplete; not re-evaluated on every log read when a usable stored path already exists.
- **Lazy environment refresh (LOG-003):** when the user opens logs and `LogPath` cannot be determined from the instance plus cached environment data, call **`RefreshEnvironmentAsync`** for that environment (full remote fetch: `GetEnvironment` + `GetDeploymentDetailsForEnvironment`), build a `LogPathTemplateContext` from refreshed `RemoteEnvironmentDetails` + instance fields, resolve via `ILogPathTemplateService`, **persist** the result on `ApplicationInstance.LogPath`, then read logs. Do **not** refresh on every visit when `LogPath` is already set and valid.
- Support tail/read-last-N-lines for large files (LOG-002).

### Log path resolution (deployable app → instance)

| Step | When | Behaviour |
|------|------|-----------|
| 1 | User clicks **View Logs** (or navigates to `/log-viewer/{instanceId}`) | If `ApplicationInstance.LogPath` is set and usable → read logs immediately |
| 2 | `LogPath` empty but deployable app has `PathToLogFiles` | Build `LogPathTemplateContext` from instance + cached `RemoteEnvironmentDetails` (and matching web app row when relevant) |
| 3 | Required template tokens still missing | **`RefreshEnvironmentAsync`** → retry context build → resolve template → save `LogPath` on instance → read logs |
| 4 | No template and no `LogPath` | Show clear error — user must configure path on deployable app or instance |

**Not in scope:** re-resolving the template on every log view when a stored `LogPath` already exists (even if remote data later changes). User can re-save the deployment or trigger environment **Refresh** manually if paths drift.

Token sources (see APP-005): `{AppName}`, `{EnvironmentCode}`, `{EnvironmentName}`, `{MachineName}`, `{ApplicationPoolName}`, `{VirtualPath}`, `{PhysicalPath}`.

### Viewer UX

1. Select **Environment** (or arrive via `/log-viewer/{instanceId}` from environment details **View Logs**)
2. **LOG-003 pre-read:** run log path resolution flow above (may show brief “Refreshing environment…” when a remote fetch is needed)
3. Select **ApplicationInstance** when using cascade entry (filtered to that environment)
4. **LOG-005:** when `LogPath` is a directory, pick which log file to view (default: newest `*.log` by write time)
5. Stream or paginate parsed entries; **LOG-007** optional format override dropdown for debugging parsers
6. **LOG-006:** collapsible panel showing raw unformatted file content (tail read) for parser debugging
7. Filters: minimum level (hide INFO and below), text contains, time range (future — LOG-004)

### Sample logs and preview UI

Prototype sample files live in [`samples/logs/`](../../samples/logs/README.md) (Serilog JSON, plain text, NLog multiline, log4net pattern). The **Log preview** page at `/log-viewer/preview` loads these files, parses them with format-specific parsers, and lets you step through entries one at a time. Parser code in `DigitalDevServices.Services.Logs` is the starting point for LOG-001.

### Plugin angle

- Parser implementations may live in `DigitalDevServices.Plugins` — profile selects parser by `FormatName`.

### Out of scope (epic v1)

- Live tail / SignalR streaming
- Cross-instance log aggregation
- Log shipping to external SIEM

---

## Tickets

### LOG-001

| Field | Detail |
|-------|--------|
| **ID** | LOG-001 |
| **Title** | LogFormatProfile per DeployableApplication |
| **Status** | Done |
| **Description** | Added `LogFormatProfile` entity (one per deployable app: `FormatName`, `ParserConfig`, `Notes`, `UpdatedAt`) with SQLite schema upgrade. `ILogFormatProfileService` upserts/deletes profiles; `ILogParsingService` parses content using the assigned profile via `LogParserRegistry` (Serilog JSON, plain text, NLog multiline, log4net pattern). Applications admin UI: log format dropdown and notes on add/edit; log format column in list. `ILogParserPlugin` marker in Plugins for future extensions. Unit tests cover profile persistence and parsing via assigned profile. |
| **Test / demo** | **Applications** → edit app → set log format to **Plain text** → save → badge appears in list. `dotnet test --filter LogFormatProfileServiceTests` → pass. Assign profile → `ParseForDeployableApplicationAsync` returns structured entries with level and message. |
| **Depends on** | APP-001 |

### LOG-002

| Field | Detail |
|-------|--------|
| **ID** | LOG-002 |
| **Title** | Log file reader using ApplicationInstance paths |
| **Status** | Done |
| **Description** | `ILogReaderService.ReadAsync` resolves `ApplicationInstance.LogPath` (file path, or newest `*.log` in a directory), reads the last N lines (default 100, max 10,000; large files tail the last 10 MB), and parses content using the deployable app's `LogFormatProfile` via `ILogParsingService`. Returns `LogReadResult` with entries, source file path, raw line count, or a clear error (missing path, missing file, read failure, missing profile). |
| **Test / demo** | `dotnet test --filter LogReaderServiceTests` → pass. Instance with `LogPath` pointing at a log file + Plain text profile → `ReadAsync(instanceId, 100)` returns parsed entries with timestamps and levels. |
| **Depends on** | APP-002, LOG-001 |

### LOG-003

| Field | Detail |
|-------|--------|
| **ID** | LOG-003 |
| **Title** | Log viewer UI (environment → instance picker) |
| **Status** | Done |
| **Description** | Blazor **Log Viewer** at `/log-viewer` (environment → application instance picker) and `/log-viewer/{instanceId}` (deep link from environment details **Logs**). `ILogPathResolutionService.EnsureLogPathAsync` resolves `PathToLogFiles` from cached environment data when `LogPath` is empty; calls `RefreshEnvironmentAsync` once when template tokens are missing, persists resolved `LogPath` on the instance, then reads via `ILogReaderService`. Skips refresh when `LogPath` is already set. Table UI shows timestamp, level badge, and message; **Refresh tail** and **Load more** (up to 10,000 lines). Nav and home card added. Unit tests in `LogPathResolutionServiceTests`. |
| **Test / demo** | Instance with `LogPath` set → **Logs** from UAT-01 details → entries appear with no environment refresh. Instance with empty `LogPath` but app template and stale cache → **Logs** → environment refresh → path saved → logs appear. `/log-viewer` picker reaches same viewer. `dotnet test --filter "LogPathResolutionServiceTests|LogReaderServiceTests"` → pass. |
| **Depends on** | LOG-002, ENV-005, APP-004 |

### LOG-004

| Field | Detail |
|-------|--------|
| **ID** | LOG-004 |
| **Title** | Log filtering (level, text search) |
| **Status** | Todo |
| **Description** | Add filters to log viewer: minimum level dropdown (e.g. hide INFO), free-text search on message, clear filters. Client-side filter on loaded page; server-side filter for reload. |
| **Test / demo** | Load logs → set “Warning and above” → INFO lines hidden → search “timeout” → matching rows only. |
| **Depends on** | LOG-003 |

### LOG-005

| Field | Detail |
|-------|--------|
| **ID** | LOG-005 |
| **Title** | Log file selection when path is a directory |
| **Status** | Todo |
| **Description** | Today `LogPathResolver` picks the newest `*.log` in a directory with no user choice. When `ApplicationInstance.LogPath` resolves to a directory (or contains multiple log files), expose a **Log file** dropdown on `/log-viewer/{instanceId}` listing available files (at minimum `*.log` in that directory; show file name, size, and last modified). Default selection remains the newest file by write time (current behaviour). Extend `ILogReaderService` (or add a companion service) with `ListLogFilesAsync` and allow `ReadAsync` to accept an optional explicit file path. Selection is per-view session (component state or query string) — do not persist to `ApplicationInstance.LogPath`. When `LogPath` is already a single file, hide the dropdown or show it disabled with one item. |
| **Test / demo** | Instance with `LogPath` = directory containing `app-2026-08-25.log` and `app-2026-08-26.log` → viewer opens on newest → switch dropdown to older file → table and source path update. `dotnet test` covers file listing and read with explicit path. |
| **Depends on** | LOG-003 |

### LOG-006

| Field | Detail |
|-------|--------|
| **ID** | LOG-006 |
| **Title** | Raw log content debug panel |
| **Status** | Todo |
| **Description** | Add a **`CollapsibleSection`** on the log viewer page titled e.g. **Raw log content**, collapsed by default, showing the unformatted text returned by the tail read (same bytes/lines used for parsing, before any format interpretation). Extend `LogReadResult` with a `RawContent` (or equivalent) field populated by `LogFileTailReader`. Use existing monospace / `pre-wrap` styling (see `site.css` `.log-entry-raw`). Panel updates when the user refreshes, loads more lines, or selects a different file (**LOG-005**). Purpose: debug parser and path issues while the LOG epic is still evolving. |
| **Test / demo** | Open viewer → expand **Raw log content** → see exact tail text from disk → **Refresh tail** → panel matches new read. Unit test asserts `LogReadResult` includes raw content on success. |
| **Depends on** | LOG-003 |

### LOG-007

| Field | Detail |
|-------|--------|
| **ID** | LOG-007 |
| **Title** | Override log format profile on viewer |
| **Status** | Todo |
| **Description** | Add a **Parse as** dropdown on `/log-viewer/{instanceId}` listing all supported formats from `LogFormatNames` / `ILogParsingService.GetSupportedFormatNames()` (Serilog JSON, plain text, NLog multiline, log4net pattern). Default selection is the deployable application’s assigned `LogFormatProfile` when one exists; otherwise no format pre-selected. Changing the dropdown re-parses the currently loaded raw content using `ILogParsingService.ParseWithFormat` without requiring a deployable-app profile — useful when the assigned profile is wrong or missing. Show the assigned profile as read-only context (e.g. “Configured: Plain text”) beside the override control. Extend `ILogReaderService.ReadAsync` with an optional `formatName` override, or re-parse client-side via a dedicated endpoint/service method that accepts content + format name. Override is per-view session only (not saved to `LogFormatProfile`). |
| **Test / demo** | Load a Serilog JSON file with assigned profile **Plain text** → garbled table → set **Parse as** to **Serilog JSON** → entries parse correctly. Change format back and forth without re-reading from disk. `dotnet test` covers read/parse with format override. |
| **Depends on** | LOG-003 |
