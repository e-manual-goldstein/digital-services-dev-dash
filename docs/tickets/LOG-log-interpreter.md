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
| [LOG-001](#log-001) | Todo | LogFormatProfile per DeployableApplication | APP-001 |
| [LOG-002](#log-002) | Todo | Log file reader using ApplicationInstance paths | APP-002, LOG-001 |
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

- Path from **ApplicationInstance.LogPath** (file or directory — if directory, read newest file or tail all `.log` files; define v1 rule in LOG-002).
- Support tail/read-last-N-lines for large files.

### Viewer UX

1. Select **Environment** (or arrive via `/logs/{instanceId}` from environment details)
2. Select **ApplicationInstance** (filtered to that environment)
3. Stream or paginate parsed entries
4. Filters: minimum level (hide INFO and below), text contains, time range (future)

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
| **Status** | Todo |
| **Description** | Add `LogFormatProfile` entity and admin hooks on DeployableApplication. Ship at least one parser (plain text line + simple timestamp/level regex) and extensibility point for plugins. |
| **Test / demo** | Assign profile to deployable app → sample log lines parse to structured entries with level and message. |
| **Depends on** | APP-001 |

### LOG-002

| Field | Detail |
|-------|--------|
| **ID** | LOG-002 |
| **Title** | Log file reader using ApplicationInstance paths |
| **Status** | Todo |
| **Description** | Service: given ApplicationInstance, resolve `LogPath`, read file(s), apply DeployableApplication’s LogFormatProfile, return parsed entries (paginated/tail). Handle missing files with clear error. |
| **Test / demo** | Instance with log path → read last 100 lines → parsed entries returned with timestamps and levels. |
| **Depends on** | APP-002, LOG-001 |

### LOG-003

| Field | Detail |
|-------|--------|
| **ID** | LOG-003 |
| **Title** | Log viewer UI (environment → instance picker) |
| **Status** | Todo |
| **Description** | Blazor **Log Viewer** page at `/logs/{instanceId}` (deep link from environment details Logs button) and with cascade dropdowns Environment → ApplicationInstance as an alternate entry. Display parsed log entries in readable table (time, level badge, message). Load more / tail refresh button. Uses the instance's `LogPath` and the deployable app's LogFormatProfile. |
| **Test / demo** | From UAT-01 details, click **Logs** on an instance → formatted log lines from disk. Also: open **Log Viewer** → pick UAT-01 → pick app instance → same result. |
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
