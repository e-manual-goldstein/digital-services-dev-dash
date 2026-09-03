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
| [LOG-004](#log-004) | Done | Log filtering (level, text search) | LOG-003 |
| [LOG-005](#log-005) | Done | Log file selection when path is a directory | LOG-003 |
| [LOG-006](#log-006) | Done | Raw log content debug panel | LOG-003 |
| [LOG-007](#log-007) | Done | Override log format profile on viewer | LOG-003 |
| [LOG-008](#log-008) | Done | Custom regex log parser (Entry / EntryStart) | LOG-001, LOG-007 |
| [LOG-009](#log-009) | Done | Per-entry raw log modal on viewer table | LOG-003 |
| [LOG-010](#log-010) | Done | Structured exception detail modal for error rows | LOG-003, LOG-009 |
| [LOG-011](#log-011) | Done | Raw / JSON / XML format toggle for text viewers | LOG-006, LOG-009 |
| [LOG-012](#log-012) | Done | Live tail with file watch and auto-scroll | LOG-003, LOG-005 |
| [LOG-013](#log-013) | Done | Toolbar layout — Log file and Parse as side by side | LOG-005, LOG-007 |
| [LOG-014](#log-014) | Done | Raw log content modal (replace collapsible panel) | LOG-006, LOG-009 |
| [LOG-015](#log-015) | Open | Table header filter dropdowns (level and search) | LOG-004 |
| [LOG-016](#log-016) | Open | Viewport-filling scrollable log table | LOG-012, LOG-015 |

---

## Design notes

### Entity (`LogFormatProfile`)

One profile per **DeployableApplication** — same format across all environments.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `DeployableApplicationId` | `Guid` | FK, unique |
| `FormatName` | `string` | e.g. `SerilogJson`, `NLogXml`, `PlainText` |
| `ParserConfig` | `string` | JSON config — built-in formats use `{}`; **LOG-008** stores custom regex pattern(s) here when `FormatName` is `CustomRegex` |
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

### Sample log files

Fixture files in [`samples/logs/`](../../samples/logs/README.md) (Serilog JSON, plain text, NLog multiline, log4net pattern) support parser unit tests. The prototype **Log preview** page has been removed now that the log viewer is complete.

### Plugin angle

- Parser implementations may live in `DigitalDevServices.Plugins` — profile selects parser by `FormatName`.
- **LOG-008:** a single built-in `CustomRegex` parser reads per-app patterns from `LogFormatProfile.ParserConfig` rather than requiring a new C# class per format.

### Custom regex parser (LOG-008)

Built-in parsers (`PlainText`, `NLogMultiline`, `Log4NetPattern`, etc.) all reduce log text to the same `ParsedLogEntry` shape using regex capture groups — mainly `timestamp`, `level`, and `message`, with optional extras (e.g. `logger`) mapped to `Properties`.

**Goal:** let a user define their own regex without shipping code.

| Mode | Behaviour | Built-in analogue |
|------|-----------|-------------------|
| **Entry** | Each line that matches the pattern is one complete entry | `PlainTextLogParser` |
| **EntryStart** | A matching line starts a new entry; subsequent non-matching lines are appended to `message` / `RawText` until the next match | `NLogMultilineLogParser` |

**`ParserConfig` JSON** (when `FormatName` = `CustomRegex`):

| Field | Type | Notes |
|-------|------|--------|
| `mode` | `"Entry"` \| `"EntryStart"` | Required |
| `pattern` | `string` | .NET regex applied per line (Entry) or to detect entry starts (EntryStart). Must be valid and compile within a timeout |
| `timestampFormat` | `string?` | Optional `DateTimeOffset.TryParseExact` format when `timestamp` group is present; otherwise `TryParse` |
| `multiline` | `bool` | Optional alias — `true` ≡ `EntryStart` |

**Named capture groups** (convention, same as built-in parsers):

| Group | Maps to | Required |
|-------|---------|----------|
| `message` | `ParsedLogEntry.Message` | Yes (on matching lines) |
| `timestamp` | `ParsedLogEntry.Timestamp` | No |
| `level` | `ParsedLogEntry.Level` | No |
| *any other* | `ParsedLogEntry.Properties[key]` | No |

**Parsing pipeline changes:**

- `ILogParsingService` / `LogParsingService` passes `LogFormatProfile.ParserConfig` when `FormatName` is `CustomRegex` (extend `ILogEntryParser` or add a dedicated `ICustomRegexLogParser` invoked from the registry).
- Validate pattern on profile save (compile + smoke match optional); reject patterns that fail compilation or exceed regex timeout.
- `LogFormatNames` adds `CustomRegex`; display name **Custom regex**.

**UI (Applications admin):**

- Log format dropdown includes **Custom regex**.
- When selected, show **Mode** (`Entry` / `EntryStart`), **Pattern** textarea, optional **Timestamp format**, and short help listing required/optional named groups with copy-paste examples derived from `PlainText` / `NLogMultiline` patterns.
- Persist config JSON in `ParserConfig` on save (today `ParserConfig` is stored but not surfaced in the form).

**Viewer:**

- Assigned `CustomRegex` profile works in log viewer via deployable-app parse path.

**Out of scope (LOG-008):**

- Arbitrary code/script parsers
- Per-environment pattern overrides (pattern remains per **DeployableApplication** via `LogFormatProfile`)
- Regex debugger with highlighted captures (future enhancement)

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
| **Status** | Done |
| **Description** | **Minimum level** dropdown (All, Debug+, Info+, Warning+, Error+) and **Search message** text box on `/log-viewer/{instanceId}`. **Clear filters** resets both. `LogEntryFilter` applies level and case-insensitive message search client-side on loaded entries; filters persist across refresh, load more, file change, and format override without re-reading from disk. Shows “X of Y entries” when filters are active and a dedicated empty state when nothing matches. Unit tests in `LogEntryFilterTests`. |
| **Test / demo** | Load logs → set **Warning and above** → INFO hidden → search `timeout` → matching rows only → **Clear filters** restores full list. `dotnet test --filter LogEntryFilterTests` → pass. |
| **Depends on** | LOG-003 |

### LOG-005

| Field | Detail |
|-------|--------|
| **ID** | LOG-005 |
| **Title** | Log file selection when path is a directory |
| **Status** | Done |
| **Description** | When `ApplicationInstance.LogPath` is a directory, `ILogReaderService.ListLogFilesAsync` lists `*.log` files (newest first) with file name, size, and last modified. `ReadAsync` accepts an optional explicit `logFilePath` (validated to stay within the configured file or directory). Log viewer page shows a **Log file** dropdown when multiple files exist; default remains the newest file. Selection is session-only. `AvailableLogFile`, `LogFileListResult` model types; path validation in `LogPathResolver`. Unit tests cover listing, explicit read, and rejection of paths outside the configured directory. |
| **Test / demo** | Instance with `LogPath` = directory containing two `.log` files → viewer opens on newest → switch dropdown to older file → table updates. `dotnet test --filter LogReaderServiceTests` → pass. |
| **Depends on** | LOG-003 |

### LOG-006

| Field | Detail |
|-------|--------|
| **ID** | LOG-006 |
| **Title** | Raw log content debug panel |
| **Status** | Done |
| **Description** | Added `RawContent` to `LogReadResult`, populated from the tail read in `LogReaderService` (including when parsing fails after a successful read). Log viewer page shows a collapsed-by-default **`CollapsibleSection`** titled **Raw log content** with monospace `pre-wrap` styling (`.log-entry-raw`). Panel updates on refresh, load more, and file selection. |
| **Test / demo** | Open viewer → expand **Raw log content** → see exact tail text → **Refresh tail** → panel updates. `dotnet test --filter LogReaderServiceTests` asserts `RawContent` on success and parse-failure paths. |
| **Depends on** | LOG-003 |

### LOG-007

| Field | Detail |
|-------|--------|
| **ID** | LOG-007 |
| **Title** | Override log format profile on viewer |
| **Status** | Done |
| **Description** | Add a **Parse as** dropdown on `/log-viewer/{instanceId}` listing all supported formats from `LogFormatNames` / `ILogParsingService.GetSupportedFormatNames()` (Serilog JSON, plain text, NLog multiline, log4net pattern). Default selection is the deployable application’s assigned `LogFormatProfile` when one exists; otherwise no format pre-selected. Changing the dropdown re-parses the currently loaded raw content using `ILogParsingService.ParseWithFormat` without requiring a deployable-app profile — useful when the assigned profile is wrong or missing. Show the assigned profile as read-only context (e.g. “Configured: Plain text”) beside the override control. Extend `ILogReaderService.ReadAsync` with an optional `formatName` override, or re-parse client-side via a dedicated endpoint/service method that accepts content + format name. Override is per-view session only (not saved to `LogFormatProfile`). |
| **Test / demo** | Load a Serilog JSON file with assigned profile **Plain text** → garbled table → set **Parse as** to **Serilog JSON** → entries parse correctly. Change format back and forth without re-reading from disk. `dotnet test` covers read/parse with format override. |
| **Depends on** | LOG-003 |

### LOG-008

| Field | Detail |
|-------|--------|
| **ID** | LOG-008 |
| **Title** | Custom regex log parser (Entry / EntryStart) |
| **Status** | Done |
| **Description** | Added **Custom regex** log format (`FormatName` = `CustomRegex`) driven by `LogFormatProfile.ParserConfig` JSON (`mode`, `pattern`, optional `timestampFormat`). `CustomRegexLogParser` supports **Entry** (one line per match) and **EntryStart** (multiline continuation) using named capture groups (`message` required; `timestamp`, `level`, others → `Properties`). `CustomRegexParserConfigValidator` validates pattern compile, timeout, and required groups on profile save. **Applications** admin shows mode, pattern, and timestamp format fields when **Custom regex** is selected. Log viewer uses saved `ParserConfig` when parsing with a custom profile or **Parse as** override. Unit tests in `CustomRegexLogParserTests`. |
| **Test / demo** | **Applications** → **Custom regex** → Entry mode with Plain-text-style pattern → save → logs parse in viewer. EntryStart mode with NLog-style header → stack lines fold into message. Invalid pattern → error on save. `dotnet test --filter CustomRegexLogParserTests` → pass. |
| **Depends on** | LOG-001, LOG-007 |

### LOG-009

| Field | Detail |
|-------|--------|
| **ID** | LOG-009 |
| **Title** | Per-entry raw log modal on viewer table |
| **Status** | Done |
| **Description** | On `/log-viewer/{instanceId}`, add a **View raw entry** action to each row in the parsed log table. Clicking opens a floating modal dialog showing the unformatted source text for that entry (`ParsedLogEntry.RawText`) in monospace `pre-wrap` styling — the exact line(s) from the log file before field extraction, including JSON blobs, stack traces, and continuation lines for multiline parsers. Modal title should identify the entry briefly (e.g. timestamp + level when available). Provide a clear **Close** control (and standard dismiss: backdrop click, Escape). Only one modal open at a time. Reuse Bootstrap modal patterns already available in the app; extract a small shared component if that keeps `Viewer.razor` readable. This is per-entry detail; the existing **Raw log content** panel (LOG-006) remains the tail-of-file debug view. No backend changes required unless `RawText` is missing for some formats — in that case ensure parsers populate it. |
| **Test / demo** | Open log viewer with mixed entries (plain text + multiline exception) → **View raw entry** on a row → modal shows full raw text → Close dismisses → open another row → previous modal replaced. Keyboard: Escape closes. Mobile: modal scrolls when content is long. |
| **Depends on** | LOG-003 |
| **Implementation** | Shared `TextDetailModal` component (Bootstrap modal, scrollable body, Close / backdrop / Escape). Log viewer table **View raw entry** button per row; `LogEntryDisplay.FormatModalTitle` for modal title. |

### LOG-010

| Field | Detail |
|-------|--------|
| **ID** | LOG-010 |
| **Title** | Structured exception detail modal for error rows |
| **Status** | Done |
| **Description** | Enhance error rendering on `/log-viewer/{instanceId}` so **Error** rows (and **Fatal** / **Critical**, matching existing level badge rules) can show structured exception detail in a modal — separate from **View raw entry** (LOG-009). **Model:** extend `ParsedLogEntry` with optional structured exception data, e.g. `ParsedLogException` with `Type`, `Message`, `StackTrace`, and recursive `InnerException` (or `IReadOnlyList<ParsedLogException>` for a flattened chain). Keep a short summary in the table `Message` column; do not duplicate the full stack in the grid. **Parsing:** update built-in parsers to populate exception detail instead of only appending stack text to `Message` — Serilog JSON (`error.*`, legacy `@x`), NLog/log4net multiline continuation blocks, plain text / custom regex where stack lines are already grouped. Add a shared helper to parse .NET exception text into nested inner exceptions (recognise `Inner exception`, `--- End of inner exception stack trace ---`, chained `at …` blocks). When structure cannot be inferred, fall back to a single block with full text from `Message` / `RawText`. **UI:** on qualifying error rows where exception detail exists, show **View exception** (or **View error**) beside **View raw entry**. Modal reuses the LOG-009 Bootstrap modal pattern: title with timestamp + level; body shows the outer exception message, stack trace, then each inner exception unwound (type, message, stack) with clear visual separation (headings or indented sections). Monospace `pre-wrap` for stack frames. Dismiss via Close, backdrop, Escape. **Out of scope:** fixing the underlying log format; live symbolication; copying to clipboard (optional nice-to-have). |
| **Test / demo** | Serilog error line with `error.stack_trace` → table shows short message → **View exception** → modal shows outer + inner chain. NLog multiline error with stack → same. Non-error row → no button. Error row without parseable exception → button hidden or disabled with tooltip. `dotnet test` covers exception text parser and at least one format integration. |
| **Depends on** | LOG-003, LOG-009 |
| **Implementation** | `ParsedLogException` on `ParsedLogEntry`; `DotNetExceptionTextParser`, `LogEntryExceptionSplitter`, `SerilogExceptionExtractor`; parsers updated (Serilog, NLog, log4net, custom regex EntryStart). `ExceptionDetailModal` + **View exception** on error rows in viewer. Tests in `DotNetExceptionTextParserTests`. |

### LOG-011

| Field | Detail |
|-------|--------|
| **ID** | LOG-011 |
| **Title** | Raw / JSON / XML format toggle for text viewers |
| **Status** | Done |
| **Description** | Several UI surfaces show unformatted text that is often JSON (single object, NDJSON lines) or XML. Add a reusable **FormattedTextViewer** (or similar) shared component with a **Raw** / **JSON** / **XML** radio-button group above the content area. **Raw** shows the source string unchanged (current behaviour). **JSON** attempts to parse and pretty-print (indented); **XML** attempts to parse and pretty-print with declaration/indentation. When the selected format cannot be parsed, show the raw text and a short inline hint (e.g. “Not valid JSON”) rather than failing silently. For multi-line content where each line is a separate JSON object (common in log tails), pretty-print line-by-line; leave non-JSON lines as-is or prefix unchanged. Default selection: **Raw**, or auto-select JSON/XML when the entire body parses successfully on first render (optional nice-to-have). Preserve existing monospace / scroll styling (reuse `.log-entry-raw` / `.log-exception-stack` patterns). Selection is per-component instance (session-only; not persisted). **Adopt in:** log viewer **Raw log content** panel (LOG-006), **View raw entry** modal (`TextDetailModal` / LOG-009). **Out of scope for v1:** exception stack traces (`ExceptionDetailModal` — not JSON/XML), configuration value cells, server-side re-fetch. **Implementation:** extract formatting into a small service or static helper (`IFormattedTextService` / `FormattedTextFormatter`) with unit tests for JSON/XML success, invalid input, and NDJSON tails; component lives under `Shared/Components`. |
| **Test / demo** | Open log viewer on Serilog JSON tail → **Raw log content** → switch to **JSON** → indented structure → switch to **XML** on XML sample → shows formatted tree → invalid JSON shows hint and raw text. **View raw entry** on a single JSON line → same radio group in modal. `dotnet test --filter FormattedText` → pass. |
| **Depends on** | LOG-006, LOG-009 |
| **Implementation** | `IFormattedTextService` / `FormattedTextService`; shared `FormattedTextViewer` component (Raw/JSON/XML radio group, auto-detect on load). Adopted in log viewer **Raw log content** panel and `TextDetailModal`. Tests in `FormattedTextServiceTests`. |

### LOG-012

| Field | Detail |
|-------|--------|
| **ID** | LOG-012 |
| **Title** | Live tail with file watch and auto-scroll |
| **Status** | Done |
| **Description** | While the user is on `/log-viewer/{instanceId}` viewing a **specific log file**, keep a file-watching mechanism active so new log lines are detected and the parsed entry table updates automatically — without requiring **Refresh tail**. **Watching lifecycle:** start when the viewer has loaded a resolved file path; stop when the user navigates away, changes instance, selects a different file from the dropdown, or the component is disposed. If `LogPath` is a directory, watch the currently selected file only (LOG-005). **Detection:** prefer `FileSystemWatcher` (or equivalent) on the server for the resolved path, with a sensible fallback (e.g. polling file length / last-write time on a short interval) when the path is UNC or watching is unsupported. On change, read only **new** content since the last tail position (track byte offset or line cursor); append newly parsed entries to the in-memory list rather than re-reading the entire tail on every event. Re-apply active filters (LOG-004) and **Parse as** override (LOG-007) to new entries. Cap in-memory growth: continue to respect the current `_maxLines` / load-more window, or trim oldest displayed entries when the window is full (document chosen behaviour). **UI:** add **Auto-scroll** checkbox (default **on**) near the table or toolbar. When enabled and new entries arrive, scroll the table container to the bottom so the newest rows are visible; when disabled, preserve the user's scroll position. If the user has scrolled up manually, optionally pause auto-scroll until they return to the bottom or re-check the box (nice-to-have). Show a subtle **Live** / **Watching** indicator while active; surface read/watch errors without tearing down the page. **Backend:** extend `ILogReaderService` (or add `ILogTailWatcherService`) with incremental read/watch APIs; ensure thread-safe coordination if multiple users watch the same path. **Out of scope:** watching multiple files simultaneously; push notifications outside the log viewer page; editing or deleting log files. |
| **Test / demo** | Open viewer on a tailing log file → append lines from another process → table updates within a few seconds → auto-scroll shows newest row. Uncheck **Auto-scroll** → append more lines → scroll position unchanged. Switch log file → watch moves to new file. Navigate away → watcher stops (no leaked handles). `dotnet test` covers incremental read / offset tracking. |
| **Depends on** | LOG-003, LOG-005 |

### LOG-013

| Field | Detail |
|-------|--------|
| **ID** | LOG-013 |
| **Title** | Toolbar layout — Log file and Parse as side by side |
| **Status** | Done |
| **Description** | On `/log-viewer/{instanceId}`, place the **Log file** picker (when path is a directory, LOG-005) and **Parse as** dropdown **side by side** in one toolbar row (responsive: stack on narrow viewports). Remove the redundant **Source:** line at the top of the page showing the resolved log file path — keep **Log path:** when it differs from the instance's configured template path. Consolidate cards/sections so controls are not spread across multiple full-width cards when a single toolbar row suffices. |
| **Test / demo** | Open viewer on directory log path → Log file + Parse as appear on one row → Source line absent → Log path still shown when applicable. |
| **Depends on** | LOG-005, LOG-007 |

### LOG-014

| Field | Detail |
|-------|--------|
| **ID** | LOG-014 |
| **Title** | Raw log content modal (replace collapsible panel) |
| **Status** | Done |
| **Description** | Remove the **Raw log content** collapsible section (LOG-006) from the main page body. Add a **View raw log** (or similar) button beside **Parse as** that opens the full current tail raw text in a modal dialog — reuse `TextDetailModal` or extend it. Modal shows the same `_rawContent` string as today (monospace, scrollable). LOG-011 formatted JSON/XML toggle may apply inside this modal when that ticket ships; until then, raw text only. |
| **Test / demo** | Open log viewer → no inline raw panel → click **View raw log** beside Parse as → modal shows tail content → Close dismisses. |
| **Depends on** | LOG-006, LOG-009 |

### LOG-015

| Field | Detail |
|-------|--------|
| **ID** | LOG-015 |
| **Title** | Table header filter dropdowns (level and search) |
| **Status** | Open |
| **Description** | Remove the separate filter card containing **Minimum level** and **Search message**. Embed filters in the **log entries table header**: e.g. level filter as a dropdown in the **Level** column header, search as a compact input or dropdown in the **Message** column header (or a combined filter control in the header row). Preserve LOG-004 filter behaviour client-side. **Clear filters** remains accessible (header chip, reset icon, or small link). |
| **Test / demo** | Open viewer with mixed levels → filter via header dropdown → search from header → table filters without separate section above. |
| **Depends on** | LOG-004 |

### LOG-016

| Field | Detail |
|-------|--------|
| **ID** | LOG-016 |
| **Title** | Viewport-filling scrollable log table |
| **Status** | Open |
| **Description** | The log viewer page itself must **not scroll** — only the entries table scrolls vertically. Layout: fixed header/toolbar area; table container **fills** from below the toolbar to the bottom of the viewport (`100vh` minus nav/header). Table body scrolls inside that region (extend `.log-viewer-table-scroll` from LOG-012). Back link, title, toolbar, and live-tail controls stay visible. Test with live tail (LOG-012) and auto-scroll. |
| **Test / demo** | Resize browser → page body has no vertical scrollbar → table scrolls internally → live tail auto-scroll still works → filters in header remain visible. |
| **Depends on** | LOG-012, LOG-015 |

