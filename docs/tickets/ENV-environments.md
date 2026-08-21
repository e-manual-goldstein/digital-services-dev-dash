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
| [ENV-002](#env-002) | Todo | Environment management UI | ENV-001 |

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

When `BaseUrl` is empty, API calls fail with a clear configuration error (until configured).

### Storage

- SQLite database: `%LocalAppData%/DigitalDevServices/DevDash/devdash.db` (override via `ConnectionStrings:DevDashDatabase`).
- Implemented in `DigitalDevServices.Data` (`DevDashDbContext`).

### Service surface (`IEnvironmentService`)

| Method | Purpose |
|--------|---------|
| `TrackEnvironmentAsync(remoteId)` | Create local link + initial API fetch |
| `GetTrackedEnvironmentsAsync()` | All tracked envs with cached/fresh details |
| `GetTrackedEnvironmentAsync(localId)` | Single env |
| `RefreshEnvironmentAsync(localId)` | Force API refresh |
| `UntrackEnvironmentAsync(localId)` | Remove local link |

### Relationships

- One **TrackedEnvironment** has many **ApplicationInstance** records (see [APP-applications.md](APP-applications.md)) — FK uses local `Guid`.

### UI notes (ENV-002)

- Sidebar: **Environments**
- List: name + SQL instance from cached API data, `DateLastUpdated`, refresh button per row
- Track environment by remote id (or pick from API list when available)
- No editing of name/SQL Server — read-only from remote source

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

### ENV-002

| Field | Detail |
|-------|--------|
| **ID** | ENV-002 |
| **Title** | Environment management UI |
| **Status** | Todo |
| **Description** | Blazor pages: list tracked environments with API-sourced name and SQL instance, show `DateLastUpdated`, per-row **Refresh** button. Track new environment by remote id. Empty state when none tracked. Nav link enables **Environments** in sidebar. |
| **Test / demo** | Track remote id → list shows API name/SQL → Refresh updates timestamp → data persists after restart. |
| **Depends on** | ENV-001 |
