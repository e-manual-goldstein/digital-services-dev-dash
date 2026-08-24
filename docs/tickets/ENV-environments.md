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
| [ENV-004](#env-004) | Done | Environment details page (SQL copy, BuildNumber/TFS, WIP branch) | ENV-002, ENV-003 |
| [ENV-005](#env-005) | Done | Deployed applications table on environment details | ENV-004, APP-002 |
| [ENV-006](#env-006) | Done | Deployed application packages page (DLL list + versions) | ENV-005, APP-002 |
| [ENV-007](#env-007) | Done | Extensible `RemoteEnvironmentDetails` (overflow JSON properties) | ENV-001 |
| [ENV-008](#env-008) | Done | Environment favourites (local persistence + favourites table) | ENV-002 |
| [ENV-009](#env-009) | Todo | Environments list table columns (favourites + all environments) | ENV-008 |
| [ENV-010](#env-010) | Todo | Environment details — additional properties (expandable JSON) | ENV-007 |

---

## Design notes

### Local entity (`TrackedEnvironment`)

Persisted in SQLite — **no** name or SQL Server instance stored locally.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local DevDash PK |
| `RemoteId` | `int` | External team's environment id — unique |
| `IsFavourite` | `bool` | DevDash-only flag (ENV-008); default `false` |
| `DateLastUpdated` | `DateTimeOffset` | UTC — last successful API fetch for this record |

### Remote DTO (`RemoteEnvironmentDetails`)

Returned by the external Web API (shape may evolve). Known fields are first-class properties; additional JSON properties are captured without model changes (ENV-007).

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `int` | Remote environment id — maps to tracked `RemoteId` |
| `Code` | `string` | Short environment code |
| `Name` | `string` | e.g. `UAT-01` |
| `EnvironmentType` | `string` | e.g. `UAT`, `Production` |
| `AdditionalProperties` | `Dictionary<string, JsonElement>` | Overflow from API via `[JsonExtensionData]` — not serialized back to the API |

Promote fields from `AdditionalProperties` to typed properties when the UI or services need them reliably (e.g. SQL Server instance, build number).

### Combined view (`CachedEnvironment`)

What consumers (UI, other services) use:

| Field | Type | Notes |
|-------|------|--------|
| `LocalId` | `Guid` | From `TrackedEnvironment.Id` |
| `RemoteId` | `int` | From `Details.Id` |
| `IsFavourite` | `bool` | From `TrackedEnvironment` (local only) |
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
| `SetFavouriteAsync(localId, isFavourite)` | Persist favourite flag on local record (ENV-008) |
| `UntrackEnvironmentAsync(localId)` | Remove local link |

### Relationships

- One **TrackedEnvironment** has many **ApplicationInstance** records (see [APP-applications.md](APP-applications.md)) — FK uses local `Guid`.

### UI notes (ENV-002, ENV-008, ENV-009)

- Sidebar: **Environments**
- Page loads full environment list from remote API on open
- **Favourites** table at top (ENV-008): environments where `IsFavourite` is true
- **All environments** table below: remaining environments (non-favourites)
- Both tables share the same columns (ENV-009): **Code**, **Name**, **Type**, **Last updated**, favourite toggle, **Refresh**
- Default sort: ascending by remote id (`Details.Id` / `RemoteId`) — **remote id is not shown** as a column
- **Name** links to environment details (`/environments/{localId}`)
- **Refresh all** and per-row **Refresh** bypass cache and call the API
- Favourite toggle updates local SQLite only; does not call the remote API
- Local tracking records are created automatically when the catalog is loaded (for downstream FK use)

### Environment details (ENV-004+)

Route: `/environments/{localId}` (`CachedEnvironment.LocalId`).

**Header**

| Surface | Behaviour |
|---------|-----------|
| SQL Server instance | Display as `<code>`; **Copy** button writes the string to the clipboard and shows a success toast |
| BuildNumber | Text + hyperlink to TFS work item (new tab) when `Tfs:WorkItemUrlTemplate` is set |
| WIP branch | Plain text from `WipBranch` |
| Refresh | Per-environment refresh (same as list row refresh) |

**Additional properties** (ENV-010) — below the main details card.

| Surface | Behaviour |
|---------|-----------|
| Section | Reuse `CollapsibleSection`; title **Additional properties**; collapsed by default |
| Empty | Omit the section when `AdditionalProperties` is null or empty |
| Content | On expand, show `AdditionalProperties` as pretty-printed JSON (`JsonSerializer` with `WriteIndented`) in `<pre><code>` |
| Refresh | Content reflects the latest API response after per-environment **Refresh** |

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
| **Status** | Done |
| **Description** | Extended `RemoteEnvironmentDetails` and mock API samples with `BuildNumber` and `WipBranch`. Added `Tfs:WorkItemUrlTemplate` config and `TfsWorkItemLinkBuilder`. New `/environments/{localId}` detail page: SQL Server instance with clipboard copy + toast, BuildNumber as TFS hyperlink (new tab) when configured, WIP branch, and per-environment Refresh. Environments list links names to details. Unknown local id shows not-found message. Unit tests cover mock API fields and TFS URL building. |
| **Test / demo** | Run mock API + DevDash → **Environments** → click **UAT-01** → details show SQL instance, build `123456`, WIP branch `feature/123456-customer-portal`. Copy SQL → paste matches. Click BuildNumber → TFS URL opens with build number substituted. Refresh updates last-updated. Bogus Guid in URL shows not found. `dotnet test --filter "MockRemoteApiTests|TfsWorkItemLinkBuilderTests"` → pass. |
| **Depends on** | ENV-002, ENV-003 |

### ENV-005

| Field | Detail |
|-------|--------|
| **ID** | ENV-005 |
| **Title** | Deployed applications table on environment details |
| **Status** | Done |
| **Description** | Added `IsWebApp` to `DeployableApplication` (checkbox + Web badge on `/applications`) and `HomepageUrl` to `ApplicationInstance` (schema upgrade; APP-004 will collect it on the deployment form). Environment details page lists deployed applications for that environment: application name, homepage link when the app is a web app and a URL is set, and **Logs** (`/logs/{instanceId}`), **Configuration** (`/configuration/{instanceId}`), and **Packages** buttons. Empty state when no deployments exist. Unit tests cover `IsWebApp` and `HomepageUrl` persistence. |
| **Test / demo** | Register a web app (`IsWebApp`) and a non-web app via service/tests; upsert instances in UAT-01 with a homepage URL on the web app only. Open UAT-01 details → both apps appear; only the web app shows a homepage link. Logs / Configuration / Packages buttons navigate to the documented routes. Environment with no deployments shows empty-state message. `dotnet test --filter "DeployableApplicationServiceTests|ApplicationInstanceServiceTests"` → pass. |
| **Depends on** | ENV-004, APP-002 |

### ENV-006

| Field | Detail |
|-------|--------|
| **ID** | ENV-006 |
| **Title** | Deployed application packages page (DLL list + versions) |
| **Status** | Done |
| **Description** | Added `IDeployedPackageService` to recursively scan `ApplicationInstance.PhysicalPath` for `*.dll` files and return file name, file version, and assembly version. New page `/environments/{localId}/instances/{instanceId}/packages` with a packages table, back link to environment details, and clear messages for missing instance, missing path, missing folder, or unreadable files. Unit tests cover successful scan, missing path, missing folder, and unknown instance. |
| **Test / demo** | Point an instance `PhysicalPath` at a folder containing DLLs → open **Packages** from environment details → file names and versions appear. Missing path or folder → readable warning, no crash. `dotnet test --filter DeployedPackageServiceTests` → pass. |
| **Depends on** | ENV-005, APP-002 |

### ENV-007

| Field | Detail |
|-------|--------|
| **ID** | ENV-007 |
| **Title** | Extensible `RemoteEnvironmentDetails` (overflow JSON properties) |
| **Status** | Done |
| **Description** | Added `[JsonExtensionData]` and `Dictionary<string, JsonElement>? AdditionalProperties` on `RemoteEnvironmentDetails` so unmapped API JSON is preserved on deserialize. Added `TryGetAdditionalString` for simple display lookups. Mock API sample **UAT-01** includes overflow fields (`SqlServerInstance`, `BuildNumber`, `WipBranch`). Unit tests cover deserialization of known + overflow properties and promotion of a field to a typed subclass property. |
| **Test / demo** | `dotnet test --filter "RemoteEnvironmentDetailsTests|MockRemoteApiTests"` → pass. GET/POST mock environment for UAT-01 → overflow fields in `AdditionalProperties`. Deserialize same JSON to a type with `SqlServerInstance` property → binds to property, not overflow. |
| **Depends on** | ENV-001 |

### ENV-008

| Field | Detail |
|-------|--------|
| **ID** | ENV-008 |
| **Title** | Environment favourites (local persistence + favourites table) |
| **Status** | Done |
| **Description** | Added `IsFavourite` to `TrackedEnvironment` with SQLite schema upgrade. Extended `CachedEnvironment` and `IEnvironmentService.SetFavouriteAsync`. `/environments` shows a **Favourites** table at the top (star toggle, empty-state message) and **All environments** below for non-favourites. Favourite state is local-only and survives restart; memory cache is patched on toggle. |
| **Test / demo** | Open **Environments** → star two environments → they appear in Favourites → restart DevDash → still favourited → unstar one → moves to All environments only. `dotnet test --filter EnvironmentServiceTests` → pass. |
| **Depends on** | ENV-002 |

### ENV-009

| Field | Detail |
|-------|--------|
| **ID** | ENV-009 |
| **Title** | Environments list table columns (favourites + all environments) |
| **Status** | Todo |
| **Description** | Refactor `/environments` so **Favourites** and **All environments** tables share one row template / component. Columns in order: **Code** (`Details.Code`, `<code>`), **Name** (link to details), **Type** (`Details.EnvironmentType`), **Last updated** (`DateLastUpdated`, local time), **Favourite** (toggle button — filled star vs outline; calls `SetFavouriteAsync`), **Refresh** (per-row, existing behaviour). Remove the **Remote id** column from the UI. Default sort for both tables: ascending by `RemoteId` / `Details.Id`. Extract shared markup to avoid duplication (partial component or private render fragment). |
| **Test / demo** | **Environments** page shows Code, Name, Type, Last updated, favourite control, Refresh — no Remote id column. Rows sorted by remote id ascending. Favourite toggle works in both tables. **Refresh all** still works. |
| **Depends on** | ENV-008 |

### ENV-010

| Field | Detail |
|-------|--------|
| **ID** | ENV-010 |
| **Title** | Environment details — additional properties (expandable JSON) |
| **Status** | Todo |
| **Description** | On `/environments/{localId}`, below the card of named environment fields, add an expandable **Additional properties** section for `RemoteEnvironmentDetails.AdditionalProperties` (ENV-007). Reuse existing `CollapsibleSection` component; collapsed by default. When expanded, render the dictionary as pretty-printed JSON in a `<pre><code>` block (`System.Text.Json`, `WriteIndented`). Omit the section entirely when `AdditionalProperties` is null or empty. Optional small helper on the model or a static formatter keeps the Razor page thin. After **Refresh**, the JSON reflects the latest API payload. |
| **Test / demo** | Run mock API + DevDash → open **UAT-01** details → **Additional properties** section appears (mock includes `SqlServerInstance`, `BuildNumber`, `WipBranch` in overflow) → expand → indented JSON with those keys → **Refresh** → content still correct. Environment with no overflow fields → section not shown. |
| **Depends on** | ENV-007 |
