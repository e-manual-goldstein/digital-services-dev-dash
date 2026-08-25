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
| [ENV-009](#env-009) | Done | Environments list table columns (favourites + all environments) | ENV-008 |
| [ENV-010](#env-010) | Done | Environment details — additional properties (expandable JSON) | ENV-007 |
| [ENV-011](#env-011) | Done | `Servers` on `RemoteEnvironmentDetails` (model + details UI) | ENV-007 |
| [ENV-012](#env-012) | Done | `EnvironmentUrls` — model + register `ApplicationInstance` from URL | ENV-007, APP-002 |
| [ENV-013](#env-013) | Done | `WebSites` / `WebApplications` — model + register instances from IIS paths | ENV-012, APP-002 |
| [ENV-014](#env-014) | Done | `WindowsServices` on `RemoteEnvironmentDetails` (model + details UI) | ENV-007 |
| [ENV-015](#env-015) | Todo | Register from remote data → pre-filled application/deployment forms | ENV-012, ENV-013, APP-003, APP-004, APP-005 |
| [ENV-016](#env-016) | Done | Fetch deployment/build details on environment refresh | ENV-004, ENV-007 |
| [ENV-017](#env-017) | Todo | `GetBuildVersionDetails` — version control metadata for a build | ENV-016 |

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
| `Servers` | `EnvironmentServer[]` | Infrastructure servers (ENV-011) |
| `EnvironmentUrls` | `EnvironmentUrl[]` | Named application URLs (ENV-012) |
| `WebSites` | `EnvironmentWebSite[]` | IIS sites and web applications (ENV-013) |
| `WindowsServices` | `EnvironmentWindowsService[]` | Windows services on environment machines (ENV-014) |
| `AdditionalProperties` | `Dictionary<string, JsonElement>` | Overflow from API via `[JsonExtensionData]` — not serialized back to the API |

Promote fields from `AdditionalProperties` to typed properties when the UI or services need them reliably. Once promoted, they no longer appear in the overflow dictionary.

### Remote deployment details (`RemoteEnvironmentDeploymentDetails`, ENV-016)

Returned by **`GetDeploymentDetailsForEnvironment`** (POST, same body as `GetEnvironment`). Wrapped in `RemoteApiResponse<RemoteEnvironmentDeploymentDetails>` like other endpoints. Cached on `CachedEnvironment.DeploymentDetails` when an environment is refreshed.

| Field | Type | Notes |
|-------|------|--------|
| `Builds` | `EnvironmentBuild[]` | All builds for the environment |
| `BuildsFull` | `EnvironmentBuild[]` | Full builds subset |
| `BuildsLast` | `EnvironmentBuild[]` | Most recent builds |
| `BuildsSuccessful` | `EnvironmentBuild[]` | Successful builds only |
| `AdditionalProperties` | `Dictionary<string, JsonElement>?` | Overflow via `[JsonExtensionData]` on the root DTO |

**`EnvironmentBuild`** (element type for all four arrays)

| Field | Type | Notes |
|-------|------|--------|
| `BuildNumber` | `int` | TFS work item id — stringify for `Tfs:WorkItemUrlTemplate` |
| `Color` | `string?` | Display hint (e.g. status colour from API) |
| `DeploymentType` | `string?` | |
| `Name` | `string?` | Build / deployment label |
| `Parameters` | `EnvironmentBuildParameter[]` | Key/value metadata (may include branch, etc.) |
| `Result` | `string?` | Outcome / status text |
| `AdditionalProperties` | `Dictionary<string, JsonElement>?` | Overflow via `[JsonExtensionData]` |

**`EnvironmentBuildParameter`** (nested under `EnvironmentBuild`)

| Field | Type | Notes |
|-------|------|--------|
| `Name` | `string?` | Parameter key |
| `NameAsLabel` | `string?` | Human-readable label for UI |
| `Value` | `string?` | Parameter value |

Example response shape (inner `Result` only):

```json
{
  "Result": {
    "Builds": [
      {
        "BuildNumber": 123456,
        "Color": "green",
        "DeploymentType": "Full",
        "Name": "Customer Portal",
        "Parameters": [
          { "Name": "WipBranch", "NameAsLabel": "WIP branch", "Value": "feature/123456-customer-portal" }
        ],
        "Result": "Succeeded"
      }
    ],
    "BuildsFull": [],
    "BuildsLast": [],
    "BuildsSuccessful": []
  }
}
```

**Header build link (ENV-004 + ENV-016):** derive the primary build for the TFS hyperlink from the first entry in `BuildsSuccessful`, else `BuildsLast`, else `BuildsFull`, else `Builds`. Use `BuildNumber` (as string) with `TfsWorkItemLinkBuilder`. Optional WIP branch in header: first `Parameters` entry where `Name` is `WipBranch` (case-insensitive) on that primary build, if present.

**UI (ENV-016):** below the main details card (and before **Additional properties**), show up to four **`CollapsibleSection`** + table blocks when non-empty — **Builds (n)**, **Builds full (n)**, **Builds last (n)**, **Builds successful (n)**. Table columns: build number (TFS link when configured), name, deployment type, result, colour; expand row or secondary column for parameters (`NameAsLabel` / `Value`). Omit sections when the array is null or empty. Build number link or **Version details** action triggers **ENV-017** fetch for that row.

### Remote build version details (`RemoteBuildVersionDetails`, ENV-017)

Returned by **`GetBuildVersionDetails`** (POST). On-demand per build number — not part of environment refresh. Wrapped in `RemoteApiResponse<RemoteBuildVersionDetails>`.

**Request (`GetBuildVersionDetailsRequest`)**

```json
{
  "BuildNumber": "123456",
  "IncludeVersionControlLog": true
}
```

| Field | Type | Notes |
|-------|------|--------|
| `BuildNumber` | `string` | String form of `EnvironmentBuild.BuildNumber` |
| `IncludeVersionControlLog` | `bool` | Always `true` in v1 |

**Response (`RemoteBuildVersionDetails`)**

| Field | Type | Notes |
|-------|------|--------|
| `BuildNumber` | `int` | Echo of requested build |
| `FromShaId` | `string?` | Source commit SHA |
| `Project` | `string?` | Project / repo identifier |
| `SourceBranch` | `string?` | Branch the build came from |
| `AdditionalProperties` | `Dictionary<string, JsonElement>?` | Overflow via `[JsonExtensionData]` — may include version-control log payload when `IncludeVersionControlLog` is true |

Example inner `Result`:

```json
{
  "BuildNumber": 123456,
  "FromShaId": "a1b2c3d4e5f6",
  "Project": "DigitalServices/CustomerPortal",
  "SourceBranch": "feature/123456-customer-portal"
}
```

**UI (ENV-017):** when the user requests version details for a build (from an ENV-016 build table row), call the API and show **Build version details** — `FromShaId`, `Project`, `SourceBranch` in the main card or an expandable row / nested collapsible; if version-control log fields appear in `AdditionalProperties`, show them in a collapsible JSON block (same pattern as ENV-010). Optional short-lived in-memory cache keyed by `BuildNumber`. Do not prefetch for every build on environment refresh.

### Remote child DTOs (ENV-011 – ENV-014)

**`EnvironmentServer`** (`Servers[]`)

| Field | Type | Notes |
|-------|------|--------|
| `ComponentDescription` | `string?` | |
| `ComponentIdenifier` | `string?` | API spelling — use `[JsonPropertyName]` if JSON key differs |
| `ComponentName` | `string?` | |
| `ComponentResourceNameResolved` | `string?` | |
| `Name` | `string?` | JSON property `name` (lowercase) |
| `ServerType` | `string?` | |

**`EnvironmentUrl`** (`EnvironmentUrls[]`)

| Field | Type | Notes |
|-------|------|--------|
| `ApplicationName` | `string` | Maps to `DeployableApplication.Name` when registering instances |
| `Url` | `string` | Maps to `ApplicationInstance.HomepageUrl`; implies web app |

**`EnvironmentWebSite`** (`WebSites[]`)

| Field | Type | Notes |
|-------|------|--------|
| `Name` | `string?` | IIS site name |
| `MachineName` | `string?` | Host running the site |
| `WebApplications` | `EnvironmentWebApplication[]` | IIS applications under the site |

**`EnvironmentWebApplication`** (nested under `WebSite`)

| Field | Type | Notes |
|-------|------|--------|
| `ApplicationPoolName` | `string?` | |
| `Path` | `string?` | Virtual path — candidate deployable app name |
| `PhysicalPath` | `string?` | Maps to `ApplicationInstance.PhysicalPath` |

**`EnvironmentWindowsService`** (`WindowsServices[]`)

| Field | Type | Notes |
|-------|------|--------|
| `MachineName` | `string?` | |
| `DisplayName` | `string?` | |
| `BinaryPathName` | `string?` | Installed service binary path |

### Registering deployments from remote data (ENV-012, ENV-013, ENV-015)

Remote environment payloads can seed local **`ApplicationInstance`** rows (and **`DeployableApplication`** records when no name match exists).

**Current behaviour (ENV-012 / ENV-013):** **Register** on environment details immediately finds or creates `DeployableApplication` and upserts `ApplicationInstance` via `IRemoteEnvironmentRegistrationService`.

**Planned behaviour (ENV-015):** **Register** navigates to guided creation instead of saving immediately — pre-filled **Add application** (`/applications`) when no matching deployable app exists, then pre-filled **Add deployment** on the originating environment details page. **Update** (when already registered) opens the existing deployment edit form with refreshed remote values. `IRemoteEnvironmentRegistrationService` may remain for tests/programmatic use or be narrowed to shared field-mapping helpers.

| Source | Deployable app prefill | Instance prefill |
|--------|------------------------|------------------|
| `EnvironmentUrl` | `Name` ← `ApplicationName`; `IsWebApp` ← `true` | `HomepageUrl` ← `Url`; `LogPath` ← resolve `DeployableApplication.PathToLogFiles` when set (APP-005) |
| `WebApplication` (+ parent `WebSite`) | `Name` ← `ResolveDeployableApplicationName()`; `IsWebApp` ← `true` | `PhysicalPath` ← `PhysicalPath`; `LogPath` ← resolve template with `MachineName` from site, `VirtualPath` ← `Path`, `ApplicationPoolName`, etc. |

Use existing `IApplicationInstanceService.UpsertAsync` / deployable-app services on final **Save**. Origin fields (`BuildNumber`, feed, branch) remain manual unless also mapped from `AdditionalProperties` later. One instance per (`DeployableApplicationId`, `EnvironmentId`) — save updates the same row.

**Prefill transport (implementation hint):** scoped `IRegistrationPrefillState` (or query-string for simple fields) carrying environment local id, source type, and mapped field bag; return URL after application create so the user lands back on deployment form.

### Remote collections UI (ENV-011 – ENV-014)

On environment details, each non-empty remote array is shown as its own **`CollapsibleSection`** (same component as ENV-010), **collapsed by default**. Omit the section when the array is null or empty. Section title includes the collection name and row count (e.g. **Servers (3)**).

Inside each expanded section: a Bootstrap **`table`** (`table-striped`, `table-hover`, `align-middle`) — not a bare list.

| Collection | Section title | Table content |
|------------|---------------|---------------|
| `Servers` | **Servers** | One row per server — columns per ENV-011 |
| `EnvironmentUrls` | **Environment URLs** | One row per URL — application name, URL link, **Register** / **Update** (ENV-015: navigate to pre-filled forms) |
| `WebSites` | **Web sites** | One nested **`CollapsibleSection` per site** (title = `{Name} - {MachineName}`) with a **Web applications** table (pool, path, physical path, **Register** / **Update**) |
| `WindowsServices` | **Windows services** | One row per service — machine, display name, binary path |

Place these sections after the main details card and **Additional properties** (ENV-010), before **Deployed applications**. Consider a shared wrapper component (e.g. `EnvironmentDetailsCollectionSection`) that renders `CollapsibleSection` + table header/body to keep `Detail.razor` thin.

### Combined view (`CachedEnvironment`)

What consumers (UI, other services) use:

| Field | Type | Notes |
|-------|------|--------|
| `LocalId` | `Guid` | From `TrackedEnvironment.Id` |
| `RemoteId` | `int` | From `Details.Id` |
| `IsFavourite` | `bool` | From `TrackedEnvironment` (local only) |
| `Details` | `RemoteEnvironmentDetails` | From API (`GetEnvironment`) |
| `DeploymentDetails` | `RemoteEnvironmentDeploymentDetails?` | From API (`GetDeploymentDetailsForEnvironment`) on refresh; null when not yet fetched |
| `DateLastUpdated` | `DateTimeOffset` | From local record |
| `IsFromCache` | `bool` | `true` when served from memory without an API call |

### Caching

| Setting | Config key | Default |
|---------|------------|---------|
| Cache lifetime | `EnvironmentCache:CacheLifetime` | `24:00:00` (24 hours) |

- Remote details cached in **`IMemoryCache`** per local environment id.
- **`RefreshEnvironmentAsync(remoteId)`** bypasses cache, calls **`GetEnvironment`** and **`GetDeploymentDetailsForEnvironment`** (same POST body: `{ EnvironmentCode }`), updates `DateLastUpdated`, and refreshes the memory entry.
- Normal reads use cache when still valid; otherwise fetch from API (both endpoints when a full fetch is required).

### TFS work item links

`BuildNumber` for the environment header TFS link comes from the **primary build** in `CachedEnvironment.DeploymentDetails` (see [Remote deployment details](#remote-deployment-details-remoteenvironmentdeploymentdetails-env-016)). URL is a configurable template; `{BuildNumber}` is replaced with the string form of `EnvironmentBuild.BuildNumber`. If the template is empty or no primary build exists, show plain text or omit the link.

```json
"Tfs": {
  "WorkItemUrlTemplate": "https://tfs.example.com/DefaultCollection/DigitalServices/_workitems/edit/{BuildNumber}"
}
```

### External API configuration

```json
"RemoteEnvironmentApi": {
  "BaseUrl": "https://their-api.example/",
  "GetEnvironmentPath": "api/environments",
  "GetDeploymentDetailsForEnvironmentPath": "api/environments/deployment-details",
  "GetBuildVersionDetailsPath": "api/builds/version-details",
  "ListEnvironmentsPath": "api/environments"
}
```

| Endpoint | Method | Request body | Response (`Result`) |
|----------|--------|--------------|------------------------|
| List environments | GET `ListEnvironmentsPath` | — | `RemoteEnvironmentDetails[]` |
| Get environment | POST `GetEnvironmentPath` | `GetEnvironmentRequest` (`EnvironmentCode`) | `RemoteEnvironmentDetails` |
| Get deployment details | POST `GetDeploymentDetailsForEnvironmentPath` | `GetEnvironmentRequest` (`EnvironmentCode`) | `RemoteEnvironmentDeploymentDetails` |
| Get build version details | POST `GetBuildVersionDetailsPath` | `GetBuildVersionDetailsRequest` (`BuildNumber`, `IncludeVersionControlLog`) | `RemoteBuildVersionDetails` |

`GetEnvironment` and `GetDeploymentDetailsForEnvironment` share the `{ "EnvironmentCode": "UAT-01" }` body. `GetBuildVersionDetails` uses `{ "BuildNumber": "123456", "IncludeVersionControlLog": true }`. All responses use the shared `RemoteApiResponse<T>` wrapper.

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
| BuildNumber | Primary build from `DeploymentDetails` — text + TFS hyperlink when `Tfs:WorkItemUrlTemplate` is set (see ENV-016 primary-build rule) |
| WIP branch | Optional — from primary build `Parameters` where `Name` is `WipBranch`, if present |
| Refresh | Per-environment refresh (same as list row refresh); fetches both `GetEnvironment` and `GetDeploymentDetailsForEnvironment` |

**Build collections** (ENV-016) — collapsible tables for `Builds`, `BuildsFull`, `BuildsLast`, `BuildsSuccessful` when non-empty. See [Remote deployment details](#remote-deployment-details-remoteenvironmentdeploymentdetails-env-016).

**Additional properties** (ENV-010) — below build sections.

| Surface | Behaviour |
|---------|-----------|
| Section | Reuse `CollapsibleSection`; title **Additional properties**; collapsed by default |
| Empty | Omit the section when `AdditionalProperties` is null or empty |
| Content | On expand, show `AdditionalProperties` as pretty-printed JSON (`JsonSerializer` with `WriteIndented`) in `<pre><code>` |
| Refresh | Content reflects the latest API response after per-environment **Refresh** |

**Remote collections** (ENV-011 – ENV-014) — each non-empty array is a **`CollapsibleSection`** containing a **table** (collapsed by default; section omitted when empty). See [Remote collections UI](#remote-collections-ui-env-011--env-014) design notes.

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
| **Status** | Done |
| **Description** | Refactored `/environments` with shared `EnvironmentListTable` and `EnvironmentListRow` components. Columns: **Code**, **Name** (link), **Type**, **Last updated**, favourite toggle, **Refresh**. Removed **Remote id** column; both tables sort by `RemoteId` ascending. |
| **Test / demo** | **Environments** page shows Code, Name, Type, Last updated, favourite control, Refresh — no Remote id column. Rows sorted by remote id ascending. Favourite toggle works in both tables. **Refresh all** still works. |
| **Depends on** | ENV-008 |

### ENV-010

| Field | Detail |
|-------|--------|
| **ID** | ENV-010 |
| **Title** | Environment details — additional properties (expandable JSON) |
| **Status** | Done |
| **Description** | On `/environments/{localId}`, below the named-fields card, added collapsible **Additional properties** via `CollapsibleSection` when `AdditionalProperties` is non-empty. `RemoteEnvironmentDetails.FormatAdditionalPropertiesJson()` pretty-prints overflow JSON for display in `<pre><code>`. Section omitted when empty. |
| **Test / demo** | Run mock API + DevDash → open **UAT-01** details → expand **Additional properties** → indented JSON with `SqlServerInstance`, `BuildNumber`, `WipBranch` → **Refresh** → content updates. Environment with no overflow → section hidden. `dotnet test --filter RemoteEnvironmentDetailsTests` → pass. |
| **Depends on** | ENV-007 |

### ENV-011

| Field | Detail |
|-------|--------|
| **ID** | ENV-011 |
| **Title** | `Servers` on `RemoteEnvironmentDetails` (model + details UI) |
| **Status** | Done |
| **Description** | Added `EnvironmentServer` DTO and `Servers` on `RemoteEnvironmentDetails` (including JSON `name` mapping). Mock API UAT-01 includes two sample servers. `EnvironmentDetailsCollectionSection` reusable wrapper: `CollapsibleSection` + table with count in title. Environment details shows **Servers (n)** when non-empty. Unit tests cover deserialization and mock API round-trip. |
| **Test / demo** | Mock API + DevDash → **UAT-01** details → expand **Servers (2)** → table shows rows → **Integration** omits section. `dotnet test --filter "RemoteEnvironmentDetailsTests|MockRemoteApiTests"` → pass. |
| **Depends on** | ENV-007 |

### ENV-012

| Field | Detail |
|-------|--------|
| **ID** | ENV-012 |
| **Title** | `EnvironmentUrls` — model + register `ApplicationInstance` from URL |
| **Status** | Done |
| **Description** | Added `EnvironmentUrl` DTO and `EnvironmentUrls` on `RemoteEnvironmentDetails`. `IRemoteEnvironmentRegistrationService.RegisterFromEnvironmentUrlAsync` finds or creates `DeployableApplication` (`IsWebApp = true`) and upserts `ApplicationInstance` with `HomepageUrl`. `EnvironmentUrlsSection` UI: collapsible table with external links, **Register** / **Update**, and **Registered** badge. Mock UAT-01 includes sample URLs. |
| **Test / demo** | Expand **Environment URLs (2)** → **Register** → row appears in **Deployed applications** with homepage → **Update** changes URL. `dotnet test --filter RemoteEnvironmentRegistrationServiceTests` → pass. |
| **Depends on** | ENV-007, APP-002 |

### ENV-013

| Field | Detail |
|-------|--------|
| **ID** | ENV-013 |
| **Title** | `WebSites` / `WebApplications` — model + register instances from IIS paths |
| **Status** | Done |
| **Description** | Added `EnvironmentWebSite` and `EnvironmentWebApplication` DTOs and `WebSites` on `RemoteEnvironmentDetails`. `ResolveDeployableApplicationName()` uses the last path segment, falling back to `ApplicationPoolName`. `RegisterFromWebApplicationAsync` reuses the ENV-012 registration flow and upserts `PhysicalPath`. `WebSitesSection` UI: outer **Web sites (n)** collapsible with nested per-machine sections and register/update actions. Mock UAT-01 includes sample IIS data. |
| **Test / demo** | Expand **Web sites (1)** → expand **UAT-01-APP** → **Register** on `/portal` → **Deployed applications** shows `portal` with physical path. `dotnet test --filter "RemoteEnvironmentRegistrationServiceTests|RemoteEnvironmentDetailsTests|MockRemoteApiTests"` → pass. |
| **Depends on** | ENV-012, APP-002 |

### ENV-014

| Field | Detail |
|-------|--------|
| **ID** | ENV-014 |
| **Title** | `WindowsServices` on `RemoteEnvironmentDetails` (model + details UI) |
| **Status** | Done |
| **Description** | Added `EnvironmentWindowsService` DTO and `WindowsServices` on `RemoteEnvironmentDetails`. Mock UAT-01 includes two sample services. Environment details shows **Windows services (n)** via `EnvironmentDetailsCollectionSection` — machine name, display name, binary path. |
| **Test / demo** | **UAT-01** details → expand **Windows services (2)** → table rows visible → **Integration** omits section. `dotnet test --filter "RemoteEnvironmentDetailsTests|MockRemoteApiTests"` → pass. |
| **Depends on** | ENV-007 |

### ENV-015

| Field | Detail |
|-------|--------|
| **ID** | ENV-015 |
| **Title** | Register from remote data → pre-filled application/deployment forms |
| **Status** | Todo |
| **Description** | Replace immediate register-on-click (ENV-012 / ENV-013) with a guided flow. **Register** on `EnvironmentUrlsSection` / `WebSitesSection`: if no matching `DeployableApplication` by name, navigate to `/applications` with the add form open and fields pre-filled from the remote row (name, `IsWebApp`, optional `PathToLogFiles` suggestion); after **Save**, return to `/environments/{localId}` with **Add deployment** open and instance fields pre-filled (`HomepageUrl`, `PhysicalPath`, resolved `LogPath` via APP-005, environment pre-selected). If the deployable app already exists but no instance in this environment → skip straight to pre-filled **Add deployment**. If instance already exists → **Update** opens **Edit deployment** with remote values pre-filled (user confirms **Save**). Remove auto-save notifications from register buttons. Shared mapping helper from `EnvironmentUrl` / `EnvironmentWebApplication` + parent `EnvironmentWebSite` + `RemoteEnvironmentDetails`. |
| **Test / demo** | **UAT-01** → **Environment URLs** → **Register** on Customer Portal (new app) → `/applications` form shows name + web app checked → **Save** → returns to UAT-01 deployment form with homepage URL filled → **Save** → row in **Deployed applications**. **Web sites** → **Register** on `/portal` with `PathToLogFiles` template on app → log path field shows resolved path. Existing instance → **Update** opens edit form, does not duplicate rows. |
| **Depends on** | ENV-012, ENV-013, APP-003, APP-004, APP-005 |

### ENV-016

| Field | Detail |
|-------|--------|
| **ID** | ENV-016 |
| **Title** | Fetch deployment/build details on environment refresh |
| **Status** | Done |
| **Description** | Add a second remote call when an environment is refreshed (or on cache-miss full fetch): **`GetDeploymentDetailsForEnvironment`** — POST with the same body as **`GetEnvironment`** (`GetEnvironmentRequest` / `EnvironmentCode`). Response: `RemoteApiResponse<RemoteEnvironmentDeploymentDetails>`. **Models:** `RemoteEnvironmentDeploymentDetails` with four arrays — `Builds`, `BuildsFull`, `BuildsLast`, `BuildsSuccessful` — each `EnvironmentBuild[]`. **`EnvironmentBuild`:** `BuildNumber` (`int`), `Color`, `DeploymentType`, `Name`, `Parameters` (`EnvironmentBuildParameter[]`: `Name`, `NameAsLabel`, `Value`), `Result`, plus `[JsonExtensionData]` / `AdditionalProperties`. Root DTO also supports `[JsonExtensionData]`. Extend `IRemoteEnvironmentApiClient`, `HttpRemoteEnvironmentApiClient`, `RemoteEnvironmentApiOptions.GetDeploymentDetailsForEnvironmentPath`, and `appsettings.json`. `EnvironmentService.RefreshEnvironmentAsync` stores result on `CachedEnvironment.DeploymentDetails`. **Mock API:** new POST route with sample builds for **UAT-01** (include a `WipBranch` parameter on at least one build). **UI:** header primary build + optional WIP branch per design notes; four collapsible build tables when arrays are non-empty. Remove `BuildNumber` / `WipBranch` from mock UAT-01 `AdditionalProperties` on `RemoteEnvironmentDetails`. Catalog list (`GetEnvironmentsAsync`) unchanged until per-environment refresh. |
| **Test / demo** | Mock API + DevDash → **UAT-01** → **Refresh** → header shows build `123456` TFS link and WIP branch from parameters → expand **Builds successful (1)** (or relevant section) → table shows name, type, result. `dotnet test --filter "MockRemoteApiTests|EnvironmentServiceTests|RemoteEnvironmentDeploymentDetailsTests"` → pass. |
| **Depends on** | ENV-004, ENV-007 |

### ENV-017

| Field | Detail |
|-------|--------|
| **ID** | ENV-017 |
| **Title** | `GetBuildVersionDetails` — version control metadata for a build |
| **Status** | Todo |
| **Description** | Add **`GetBuildVersionDetails`** — POST with `GetBuildVersionDetailsRequest`: `BuildNumber` (string), `IncludeVersionControlLog: true`. Response: `RemoteApiResponse<RemoteBuildVersionDetails>` with `BuildNumber` (`int`), `FromShaId`, `Project`, `SourceBranch`, and `[JsonExtensionData]` overflow (version-control log fields may land here). Extend `IRemoteEnvironmentApiClient`, `HttpRemoteEnvironmentApiClient`, `RemoteEnvironmentApiOptions.GetBuildVersionDetailsPath`, and `appsettings.json`. Service method (e.g. on API client or thin `IBuildVersionDetailsService`) — on-demand only, not on environment refresh. **Mock API:** POST route returning sample data for build `123456`. **UI:** from ENV-016 build tables, user action (e.g. **Version details** or build-number drill-down) fetches and displays `FromShaId`, `Project`, `SourceBranch`; optional collapsible JSON for overflow when log is returned. Short-lived cache per build number optional. |
| **Test / demo** | **UAT-01** → refresh (ENV-016) → open version details for build `123456` → shows SHA, project, branch. `dotnet test --filter "MockRemoteApiTests|RemoteBuildVersionDetailsTests"` → pass. |
| **Depends on** | ENV-016 |
