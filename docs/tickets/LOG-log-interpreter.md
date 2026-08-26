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
| [LOG-003](#log-003) | Todo | Log viewer UI (environment → instance picker) | LOG-002, ENV-005, APP-004 |
| [LOG-004](#log-004) | Todo | Log filtering (level, text search) | LOG-003 |

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

- **Resolved path** on **ApplicationInstance.LogPath** (file or directory — if directory, read newest `*.log`; see LOG-002).
- **Template** on **DeployableApplication.PathToLogFiles** (APP-005) — used to *produce* `LogPath` when it is missing or incomplete; not re-evaluated on every log read when a usable stored path already exists.
- **Lazy environment refresh (LOG-003):** when the user opens logs and `LogPath` cannot be determined from the instance plus cached environment data, call **`RefreshEnvironmentAsync`** for that environment (full remote fetch: `GetEnvironment` + `GetDeploymentDetailsForEnvironment`), build a `LogPathTemplateContext` from refreshed `RemoteEnvironmentDetails` + instance fields, resolve via `ILogPathTemplateService`, **persist** the result on `ApplicationInstance.LogPath`, then read logs. Do **not** refresh on every visit when `LogPath` is already set and valid.
- Support tail/read-last-N-lines for large files (LOG-002).

### Log path resolution (deployable app → instance)

| Step | When | Behaviour |
|------|------|-----------|
| 1 | User clicks **View Logs** (or navigates to `/logs/{instanceId}`) | If `ApplicationInstance.LogPath` is set and usable → read logs immediately |
| 2 | `LogPath` empty but deployable app has `PathToLogFiles` | Build `LogPathTemplateContext` from instance + cached `RemoteEnvironmentDetails` (and matching web app row when relevant) |
| 3 | Required template tokens still missing | **`RefreshEnvironmentAsync`** → retry context build → resolve template → save `LogPath` on instance → read logs |
| 4 | No template and no `LogPath` | Show clear error — user must configure path on deployable app or instance |

**Not in scope:** re-resolving the template on every log view when a stored `LogPath` already exists (even if remote data later changes). User can re-save the deployment or trigger environment **Refresh** manually if paths drift.

Token sources (see APP-005): `{AppName}`, `{EnvironmentCode}`, `{EnvironmentName}`, `{MachineName}`, `{ApplicationPoolName}`, `{VirtualPath}`, `{PhysicalPath}`.

### Viewer UX

1. Select **Environment** (or arrive via `/logs/{instanceId}` from environment details **View Logs**)
2. **LOG-003 pre-read:** run log path resolution flow above (may show brief “Refreshing environment…” when a remote fetch is needed)
3. Select **ApplicationInstance** when using cascade entry (filtered to that environment)
4. Stream or paginate parsed entries
5. Filters: minimum level (hide INFO and below), text contains, time range (future — LOG-004)

### Sample logs and preview UI

Prototype sample files live in [`samples/logs/`](../../samples/logs/README.md) (Serilog JSON, plain text, NLog multiline, log4net pattern). The **Log preview** page at `/logs/preview` loads these files, parses them with format-specific parsers, and lets you step through entries one at a time. Parser code in `DigitalDevServices.Services.Logs` is the starting point for LOG-001.

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
| **Status** | Todo |
| **Description** | Blazor **Log Viewer** page at `/logs/{instanceId}` (deep link from environment details **View Logs**) and cascade dropdowns Environment → ApplicationInstance as an alternate entry. **Before reading logs:** if `ApplicationInstance.LogPath` is missing or template tokens cannot be satisfied from cached environment data, call `RefreshEnvironmentAsync`, resolve `DeployableApplication.PathToLogFiles` via `ILogPathTemplateService` + `LogPathTemplateContext`, persist resolved `LogPath` on the instance, then proceed. If `LogPath` is already set, use it directly (no refresh). Display parsed entries in a table (time, level badge, message). Load more / tail refresh button. Uses `ILogReaderService` (stored `LogPath`) and the deployable app's `LogFormatProfile`. Show loading state during environment refresh; surface clear errors when path cannot be resolved. |
| **Test / demo** | Instance with `LogPath` set → **View Logs** → logs appear with no environment refresh. Instance with empty `LogPath` but app template `{MachineName}\{EnvironmentCode}\{AppName}\Logs` and stale/missing cache → **View Logs** → brief refresh → path resolved and saved → logs appear. From UAT-01 details and from **Log Viewer** picker → same behaviour. `dotnet test --filter "LogReaderServiceTests|LogPathResolution*"` → pass (add tests for resolution helper when implemented). |
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
