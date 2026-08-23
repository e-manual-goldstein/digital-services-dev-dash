# Epic ENV — Environments

**Project:** Digital Services Dev Dash
**Code:** `ENV`
**Scope:** Track deployment environments sourced from an external team's Web API. Local SQLite stores only DevDash identifiers and cache metadata; names, SQL Server instances, BuildNumber, WIP branch, and other display data are fetched remotely and cached in memory. The environment details page is the hub for inspecting a single environment and jumping to logs, configuration, homepage, and published packages.

**Depends on:** FND-002
**Blocks:** APP-004 (registers deployments from environment details), CFG, LOG

---

## Primary user story

> Environments are owned by another team — I can't edit their SQL Server mappings directly. I need to link their environment ids into DevDash, see up-to-date details from their API, and refresh when I need to — without hammering their endpoints on every page load.

> From the Environments list I want to open one environment and see everything I actually use day to day: copy the SQL Server instance, jump to the TFS work item for this environment's BuildNumber, see which WIP branch produced that build, and work with each deployed application (logs, homepage, config, published DLLs).

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [ENV-001](#env-001) | Done | SQLite bootstrap, tracked environment entity, API cache | FND-002 |
| [ENV-003](#env-003) | Done | Mock remote environment Web API for local testing | ENV-001 |
| [ENV-002](#env-002) | Done | Environment management UI | ENV-001 |
| [ENV-004](#env-004) | Todo | Environment details page (SQL copy, BuildNumber/TFS, WIP branch) | ENV-002, ENV-003 |
| [ENV-005](#env-005) | Todo | Deployed applications table on environment details | ENV-004, APP-002 |
| [ENV-006](#env-006) | Todo | Deployed application packages page (DLL list + versions) | ENV-005, APP-002 |

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
| `Name` | `string` | e.g. `UAT-01` |
| `SqlServerInstance` | `string` | Dedicated SQL Server for this environment |
| `BuildNumber` | `string?` | Current environment build — also the TFS work item id |
| `WipBranch` | `string?` | Git / WIP branch used to produce that build |

These environment-level origin fields come from the **remote API**. They describe the environment as a whole (the work item / branch that produced the current environment build). They are distinct from `ApplicationInstance.BuildNumber` / `SourceBranch`, which remain per-app local records.

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

### TFS work item links

`BuildNumber` doubles as a hyperlink to the TFS work item that represents that build. URL is a configurable template; `{BuildNumber}` is replaced with the remote value. If the template is empty or `BuildNumber` is missing, show the build number as plain text.

```json
"Tfs": {
  "WorkItemUrlTemplate": "https://tfs.example.com/DefaultCollection/DigitalServices/_workitems/edit/{BuildNumber}"
}
```

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

Rudimentary standalone ASP.NET Minimal API returning fixed sample environments (e.g. `UAT-01`) at the same paths as `RemoteEnvironmentApi`. Run locally and set DevDash `RemoteEnvironmentApi:BaseUrl` to the mock host URL. ENV-004 extends samples with `BuildNumber` and `WipBranch`.

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
- Environment **name** is a link to the details page (ENV-004)

### Environment details (ENV-004+)

Route: `/environments/{localId}` (`CachedEnvironment.LocalId`).

**Header**

| Surface | Behaviour |
|---------|-----------|
| SQL Server instance | Display as `<code>`; **Copy** button writes the string to the clipboard and shows a success toast |
| BuildNumber | Text + hyperlink to TFS work item (new tab) when `Tfs:WorkItemUrlTemplate` is set |
| WIP branch | Plain text from `WipBranch` |
| Refresh | Per-environment refresh (same as list row refresh) |

**Deployed applications table** (ENV-005) — `IApplicationInstanceService.GetByEnvironmentIdAsync`. Empty state until deployments exist (APP-004).

| Row action | Behaviour |
|------------|-----------|
| Logs | Navigate to `/logs/{instanceId}` — log viewer uses that instance's `LogPath` and the deployable app's LogFormatProfile (LOG-003) |
| Homepage | If the deployable app `IsWebApp` and the instance has `HomepageUrl`, show an external hyperlink (new tab). Otherwise omit |
| Configuration | Navigate to `/configuration/{instanceId}` — settings browser for that instance (CFG-003) |
| Packages | Navigate to `/environments/{localId}/instances/{instanceId}/packages` (ENV-006) |

Logs and configuration destinations are implemented in LOG-003 and CFG-003. ENV-005 still wires the buttons to those routes.

**Packages page** (ENV-006) — read-only scan of `ApplicationInstance.PhysicalPath` for `*.dll` (recursive). Show file name plus file version and assembly version (`FileVersionInfo` / `AssemblyName`). Not a NuGet feed inventory.

### Supporting fields (local, not remote)

| Field | On | Notes |
|-------|----|-------|
| `IsWebApp` | `DeployableApplication` | Added in ENV-005; checkbox on Applications admin |
| `HomepageUrl` | `ApplicationInstance` | Environment-specific URL; form field in APP-004 |

### Out of scope (epic v1)

- Writing back to the external team's systems
- Background scheduled refresh jobs
- Health checks against SQL Server instances
- NuGet gallery / feed package lookup (filesystem DLL listing only)
- Editing BuildNumber / WIP branch locally (remote API owns them)

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
| **Description** | Added `DigitalDevServices.MockRemoteApi` Minimal API on `http://localhost:5280` with four sample environments (`UAT-01`, Integration, UAT, Production). Endpoints match `RemoteEnvironmentApi` paths. DevDash `appsettings.Development.json` points at the mock by default. Unit tests verify list and get-by-id responses. |
| **Test / demo** | Terminal 1: `dotnet run --project DigitalDevServices.MockRemoteApi`. Terminal 2: `curl http://localhost:5280/api/environments/1` → UAT-01 JSON. Run DevDash in Development → environment API calls hit the mock. |
| **Depends on** | ENV-001 |

### ENV-002

| Field | Detail |
|-------|--------|
| **ID** | ENV-002 |
| **Title** | Environment management UI |
| **Status** | Done |
| **Description** | Added `/environments` page that loads the full catalog from the remote Web API on open. Table shows name, remote id, SQL Server instance, and last updated. **Refresh all** and per-row **Refresh** bypass cache. Local tracking records sync automatically in the background. Nav link and home card enabled. |
| **Test / demo** | Run mock API + DevDash → track remote id `1` → UAT-01 appears → Refresh updates timestamp → restart DevDash → row persists. |
| **Depends on** | ENV-001 |

### ENV-004

| Field | Detail |
|-------|--------|
| **ID** | ENV-004 |
| **Title** | Environment details page (SQL copy, BuildNumber/TFS, WIP branch) |
| **Status** | Todo |
| **Description** | Add `BuildNumber` and `WipBranch` to `RemoteEnvironmentDetails` and the ENV-003 mock sample environments (and mock API tests). Add configurable `Tfs:WorkItemUrlTemplate`. New Blazor page `/environments/{localId}` loaded via `GetTrackedEnvironmentAsync`: environment name, remote id, last updated, SQL Server instance with a **Copy** button (clipboard + toast), BuildNumber as a TFS work item hyperlink (new tab) when the template is set, and WIP branch. Refresh on the detail page. On the Environments list, the name becomes a link to details. Unknown id → not-found message. |
| **Test / demo** | Run mock API + DevDash → **Environments** → click **UAT-01** → details show SQL instance, build number, and WIP branch from the mock. Copy SQL → paste matches. Click BuildNumber → TFS URL opens with that build number substituted (or plain text if template is empty). Refresh updates last-updated. Direct URL with a bogus Guid shows not found. `dotnet test --filter MockRemoteApiTests` still passes and asserts the new fields. |
| **Depends on** | ENV-002, ENV-003 |

### ENV-005

| Field | Detail |
|-------|--------|
| **ID** | ENV-005 |
| **Title** | Deployed applications table on environment details |
| **Status** | Todo |
| **Description** | On the environment details page, list `ApplicationInstance` rows for that environment. Add `IsWebApp` to `DeployableApplication` (checkbox on `/applications`) and `HomepageUrl` to `ApplicationInstance` (schema upgrade; APP-004 collects it on the form). Table columns: application name; homepage hyperlink when `IsWebApp` and `HomepageUrl` are set (new tab); **Logs** (`/logs/{instanceId}`), **Configuration** (`/configuration/{instanceId}`), and **Packages** (`/environments/{localId}/instances/{instanceId}/packages`). Empty state when the environment has no deployments. Logs/config pages land in LOG-003 / CFG-003; packages page in ENV-006 — still wire the buttons now. |
| **Test / demo** | Seed or (once APP-004 exists) register two apps in UAT-01, one web with a homepage URL and one not. Details table shows both; only the web app has a homepage link. Logs / Configuration / Packages buttons navigate to the documented routes. Empty environment shows an empty-state message. `dotnet test` covers the new `IsWebApp` / `HomepageUrl` persistence. |
| **Depends on** | ENV-004, APP-002 |

### ENV-006

| Field | Detail |
|-------|--------|
| **ID** | ENV-006 |
| **Title** | Deployed application packages page (DLL list + versions) |
| **Status** | Todo |
| **Description** | New page `/environments/{localId}/instances/{instanceId}/packages` reached from the ENV-005 **Packages** button. Service scans `ApplicationInstance.PhysicalPath` for `*.dll` (recursive) and returns file name, file version, and assembly version. Table on the page; back link to environment details. Clear errors when path is missing, the folder does not exist, or files cannot be read. Not a NuGet inventory. |
| **Test / demo** | Point an instance `PhysicalPath` at a folder that contains at least one DLL (copy the test assembly into a temp directory in unit tests). Open Packages from environment details → DLL name and versions appear. Missing path / missing folder → readable error, not a crash. `dotnet test --filter` for the new package-scan tests → pass. |
| **Depends on** | ENV-005, APP-002 |
