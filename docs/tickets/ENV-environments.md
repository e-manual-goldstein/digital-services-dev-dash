# Epic ENV — Environments

**Project:** Digital Services Dev Dash
**Code:** `ENV`
**Scope:** Track deployment environments sourced from an external team's Web API. Local SQLite stores only DevDash identifiers and cache metadata; names, SQL Server instances, and other display data are fetched remotely and cached in memory.

**Depends on:** FND-002
**Blocks:** APP (ApplicationInstance), CFG, LOG

---

## Primary user story

> Environments are owned by another team — I can't edit their SQL Server mappings directly. I need to link their environment ids into DevDash, see up-to-date details from their API, and refresh when I need to — without hammering their endpoints on every page load.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [ENV-001](#env-001) | Done | SQLite bootstrap, tracked environment entity, API cache | FND-002 |
| [ENV-003](#env-003) | Done | Mock remote environment Web API for local testing | ENV-001 |
| [ENV-002](#env-002) | Done | Environment management UI | ENV-001 |

---

## Design notes

### Local entity (`TrackedEnvironment`)

Persisted in SQLite — **no** name or SQL Server instance stored locally.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local DevDash PK |
| `RemoteId` | `int` | External team's environment id — unique |
| `DateLastUpdated` | `DateTimeOffset` | UTC — last successful API fetch for this record |

### Remote DTO (`RemoteEnvironmentDetails`)

Returned by the external Web API (shape may evolve):

| Field | Type | Notes |
|-------|------|--------|
| `RemoteId` | `int` | Same as tracked `RemoteId` |
| `Name` | `string` | e.g. `Partial16` |
| `SqlServerInstance` | `string` | Dedicated SQL Server for this environment |

Additional API fields can be added to this DTO as needed.

### Combined view (`CachedEnvironment`)

What consumers (UI, other services) use:

| Field | Type | Notes |
|-------|------|--------|
| `LocalId` | `Guid` | From `TrackedEnvironment.Id` |
| `RemoteId` | `int` | |
| `Details` | `RemoteEnvironmentDetails` | From API |
| `DateLastUpdated` | `DateTimeOffset` | From local record |
| `IsFromCache` | `bool` | `true` when served from memory without an API call |

### Caching

| Setting | Config key | Default |
|---------|------------|---------|
| Cache lifetime | `EnvironmentCache:CacheLifetime` | `24:00:00` (24 hours) |

- Remote details cached in **`IMemoryCache`** per local environment id.
- **`RefreshEnvironmentAsync(localId)`** bypasses cache, calls the API, updates `DateLastUpdated`, and refreshes the memory entry.
- Normal reads use cache when still valid; otherwise fetch from API.

### External API configuration

```json
"RemoteEnvironmentApi": {
  "BaseUrl": "https://their-api.example/",
  "GetEnvironmentPath": "api/environments/{id}",
  "ListEnvironmentsPath": "api/environments"
}
```

When `BaseUrl` is empty, API calls fail with a clear configuration error (until configured). Use **ENV-003** mock API for local development.

### Mock API (ENV-003)

Rudimentary standalone ASP.NET Minimal API returning fixed sample environments (e.g. `Partial16`) at the same paths as `RemoteEnvironmentApi`. Run locally and set DevDash `RemoteEnvironmentApi:BaseUrl` to the mock host URL.

### Storage

- SQLite database: `%LocalAppData%/DigitalDevServices/DevDash/devdash.db` (override via `ConnectionStrings:DevDashDatabase`).
- Implemented in `DigitalDevServices.Data` (`DevDashDbContext`).

### Service surface (`IEnvironmentService`)

| Method | Purpose |
|--------|---------|
| `GetEnvironmentsAsync(forceRefresh?)` | Load full catalog from remote API (cached); sync local tracking records |
| `GetTrackedEnvironmentAsync(localId)` | Single env by local id |
| `RefreshEnvironmentAsync(remoteId)` | Force API refresh for one environment |
| `UntrackEnvironmentAsync(localId)` | Remove local link |

### Relationships

- One **TrackedEnvironment** has many **ApplicationInstance** records (see [APP-applications.md](APP-applications.md)) — FK uses local `Guid`.

### UI notes (ENV-002)

- Sidebar: **Environments**
- Page loads full environment list from remote API on open
- Table: name, remote id, SQL Server instance, last updated
- **Refresh all** and per-row **Refresh** bypass cache and call the API
- Local tracking records are created automatically when the catalog is loaded (for downstream FK use)
- No manual “track by id” step

### Out of scope (epic v1)

- Writing back to the external team's systems
- Background scheduled refresh jobs
- Health checks against SQL Server instances

---

## Tickets

### ENV-001

| Field | Detail |
|-------|--------|
| **ID** | ENV-001 |
| **Title** | SQLite bootstrap, tracked environment entity, API cache |
| **Status** | Done |
| **Description** | Added `DevDashDbContext` with `TrackedEnvironment` (Guid, RemoteId, DateLastUpdated). `IEnvironmentService` tracks environments by remote id, fetches `RemoteEnvironmentDetails` via configurable `IRemoteEnvironmentApiClient`, caches in memory with configurable lifetime (default 24h), and supports `RefreshEnvironmentAsync` for manual refresh. Wired into DevDash host; unit tests cover persistence, cache, and refresh. |
| **Test / demo** | `dotnet test` → EnvironmentServiceTests pass. Configure `RemoteEnvironmentApi:BaseUrl`, run app → DB file created under LocalAppData. |
| **Depends on** | FND-002 |

### ENV-003

| Field | Detail |
|-------|--------|
| **ID** | ENV-003 |
| **Title** | Mock remote environment Web API for local testing |
| **Status** | Done |
| **Description** | Added `DigitalDevServices.MockRemoteApi` Minimal API on `http://localhost:5280` with four sample environments (`Partial16`, Integration, UAT, Production). Endpoints match `RemoteEnvironmentApi` paths. DevDash `appsettings.Development.json` points at the mock by default. Unit tests verify list and get-by-id responses. |
| **Test / demo** | Terminal 1: `dotnet run --project DigitalDevServices.MockRemoteApi`. Terminal 2: `curl http://localhost:5280/api/environments/1` → Partial16 JSON. Run DevDash in Development → environment API calls hit the mock. |
| **Depends on** | ENV-001 |

### ENV-002

| Field | Detail |
|-------|--------|
| **ID** | ENV-002 |
| **Title** | Environment management UI |
| **Status** | Done |
| **Description** | Added `/environments` page that loads the full catalog from the remote Web API on open. Table shows name, remote id, SQL Server instance, and last updated. **Refresh all** and per-row **Refresh** bypass cache. Local tracking records sync automatically in the background. Nav link and home card enabled. |
| **Test / demo** | Run mock API + DevDash → track remote id `1` → Partial16 appears → Refresh updates timestamp → restart DevDash → row persists. |
| **Depends on** | ENV-001 |
