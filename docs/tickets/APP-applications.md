# Epic APP — Deployed Applications

**Project:** Digital Services Dev Dash
**Code:** `APP`
**Scope:** Model the distinction between a **DeployableApplication** (a compilable project in the codebase) and an **ApplicationInstance** (a specific build deployed into a specific environment). Capture deployment-origin metadata (branch, build number, deploy date) separately from environment-specific runtime properties (paths, log locations).

**Depends on:** ENV-001, PIP-001
**Blocks:** CFG, LOG

---

## Primary user story

> The same application exists in many environments at different versions, in different folders, with different log paths. I need to register what can be deployed, then record what actually is deployed where — including which build number and branch feed produced that deployment.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [APP-001](#app-001) | Done | DeployableApplication entity and persistence | ENV-001 |
| [APP-002](#app-002) | Done | ApplicationInstance entity and persistence | APP-001, ENV-001, PIP-001 |
| [APP-003](#app-003) | Done | DeployableApplication admin UI | APP-001 |
| [APP-004](#app-004) | Done | ApplicationInstance admin UI | APP-002, ENV-004, ENV-005 |
| [APP-005](#app-005) | Done | `PathToLogFiles` template on DeployableApplication | APP-001, APP-003 |

---

## Design notes

### Entity (`DeployableApplication`)

Represents a compilable/deployable project in the wider codebase — the logical application, independent of environment.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, unique — human name |
| `ProjectKey` | `string?` | Optional repo/project identifier |
| `IsWebApp` | `bool` | `true` when this app has a browser homepage (ENV-005) |
| `PathToLogFiles` | `string?` | Optional template for resolving `ApplicationInstance.LogPath` per environment (APP-005) |
| `Notes` | `string?` | |
| `CreatedAt` | `DateTimeOffset` | UTC |

### Entity (`ApplicationInstance`)

A specific deployment of a **DeployableApplication** in a specific **TrackedEnvironment** (local Guid).

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `DeployableApplicationId` | `Guid` | FK |
| `EnvironmentId` | `Guid` | FK → `TrackedEnvironment.Id` (local) |
| `BuildNumber` | `string` | Version identifier from pipeline output |
| `PipelineFeedId` | `Guid?` | FK — pipeline feed selected for this deployment (manual v1) |
| `SourceBranch` | `string?` | Git branch used for the build |
| `DeployedAt` | `DateTimeOffset?` | When deployed |
| `PhysicalPath` | `string?` | Deploy location on the environment |
| `LogPath` | `string?` | Log file or directory location |
| `HomepageUrl` | `string?` | Browser homepage for this instance (web apps only) |
| `SqlServerInstance` | `string?` | Override if instance differs from environment default |
| `Notes` | `string?` | |
| `CreatedAt` | `DateTimeOffset` | UTC |
| `UpdatedAt` | `DateTimeOffset?` | |

**Origin vs environment properties:**

| Origin (from pipeline / build) | Environment-specific |
|--------------------------------|----------------------|
| `BuildNumber` | `PhysicalPath` |
| `PipelineFeedId` / feed (manual) | `LogPath` |
| `SourceBranch` | `SqlServerInstance` (override) |
| `DeployedAt` | `HomepageUrl` |

Uniqueness (v1 suggestion): one **ApplicationInstance** per (`DeployableApplicationId`, `EnvironmentId`) — redeploy updates the same row.

### Log path templates (APP-005)

`DeployableApplication.PathToLogFiles` stores a template (e.g. `{MachineName}\{EnvironmentCode}\{AppName}\Logs`). `ILogPathTemplateService.Resolve` substitutes tokens from `LogPathTemplateContext`.

**When resolution runs:**

| Trigger | Behaviour |
|---------|-----------|
| **ENV-015** registration / add-deployment pre-fill | Resolve when remote + cached environment data is available; user confirms **Save** → store on `ApplicationInstance.LogPath` |
| **LOG-003** **View Logs** | If `LogPath` is already set → use it. If missing and template exists but tokens are unavailable → **`RefreshEnvironmentAsync`**, then resolve and **persist** `LogPath` (see [LOG-log-interpreter.md](LOG-log-interpreter.md)) |
| **Manual edit** (APP-004) | User may type or override `LogPath` directly on the deployment form |

Unknown `{tokens}` are left unchanged. Resolved value is stored on `ApplicationInstance.LogPath`; the log reader (LOG-002) reads that stored path, not the template.

### Relationships

- **DeployableApplication** 1→* **ApplicationInstance**
- **TrackedEnvironment** 1→* **ApplicationInstance**
- **PipelineFeed** 1→* **ApplicationInstance** (optional FK — see [PIP-pipeline-feeds.md](PIP-pipeline-feeds.md))

### UI notes

- Sidebar: **Applications** (deployable catalog) and **Deployments** or nested under Environments
- Environment detail (ENV-004 / ENV-005): list application instances in that environment, with logs / homepage / config / packages actions
- DeployableApplication detail: list instances across environments
- APP-004: add/edit deployments from the environment details page (environment pre-selected)

### Out of scope (epic v1)

- Automatic deployment detection from Azure DevOps / file system scans
- Historical deployment audit trail (multiple past build numbers per slot)
- NuGet gallery / feed package inventory (filesystem DLL listing is [ENV-006](ENV-environments.md#env-006))

---

## Tickets

### APP-001

| Field | Detail |
|-------|--------|
| **ID** | APP-001 |
| **Title** | DeployableApplication entity and persistence |
| **Status** | Done |
| **Description** | Added `DeployableApplication` entity to SQLite (`Id`, unique `Name`, `ProjectKey`, `Notes`, `CreatedAt`). `IDeployableApplicationService` provides list/get/create/update/delete with duplicate name rejection. Schema upgrade adds table on existing databases. Wired into DevDash host; unit tests in `DeployableApplicationServiceTests`. |
| **Test / demo** | `dotnet test --filter DeployableApplicationServiceTests` → pass. Create app → list → update name → delete. |
| **Depends on** | ENV-001 |

### APP-002

| Field | Detail |
|-------|--------|
| **ID** | APP-002 |
| **Title** | ApplicationInstance entity and persistence |
| **Status** | Done |
| **Description** | Added `ApplicationInstance` with FKs to `DeployableApplication`, `TrackedEnvironment`, and optional `PipelineFeed`. Origin fields (`BuildNumber`, `SourceBranch`, `DeployedAt`, feed) and environment fields (`PhysicalPath`, `LogPath`, `SqlServerInstance`). Unique slot per app+environment; `UpsertAsync` updates existing row. `IApplicationInstanceService` CRUD + query by environment or deployable app. Delete guard on deployable applications when instances exist. Schema upgrade for existing DBs. Unit tests in `ApplicationInstanceServiceTests`. |
| **Test / demo** | `dotnet test --filter ApplicationInstanceServiceTests` → pass. Register instance: App X in UAT-01, build 1.2.3, branch `feature/123456-foo` → query by environment returns row with origin and path fields. |
| **Depends on** | APP-001, ENV-001, PIP-001 |

### APP-003

| Field | Detail |
|-------|--------|
| **ID** | APP-003 |
| **Title** | DeployableApplication admin UI |
| **Status** | Done |
| **Description** | Blazor CRUD at `/applications`: list, add/edit form, delete with confirm step. Delete disabled when application instances exist; service guard surfaces errors. Nav link and home card enabled. |
| **Test / demo** | Run DevDash → **Applications** → add “Customer Portal API” → appears in list → edit → persists. Delete blocked if deployments exist. |
| **Depends on** | APP-001 |

### APP-004

| Field | Detail |
|-------|--------|
| **ID** | APP-004 |
| **Title** | ApplicationInstance admin UI |
| **Status** | Done |
| **Description** | Added add/edit deployment UI on the environment details page (`/environments/{localId}`): **Add deployment** opens a form with application picker (apps not yet deployed in this environment), build number, pipeline feed, source branch, deployed date, physical path, log path, homepage URL (when the app is a web app), SQL Server override, and notes. **Edit** on each row updates the existing slot. Saved deployments appear immediately in the deployed applications table with build number, homepage link, and action buttons. |
| **Test / demo** | **Applications** → register a web app and a non-web app → **Environments** → **UAT-01** → **Add deployment** → fill form with build number, paths, and homepage URL for the web app → row appears with homepage link. Edit updates values. Non-web app has no homepage field or link. |
| **Depends on** | APP-002, ENV-004, ENV-005 |

### APP-005

| Field | Detail |
|-------|--------|
| **ID** | APP-005 |
| **Title** | `PathToLogFiles` template on DeployableApplication |
| **Status** | Done |
| **Description** | Added optional `PathToLogFiles` on `DeployableApplication` (SQLite schema upgrade). `ILogPathTemplateService` resolves templates using `{AppName}`, `{EnvironmentCode}`, `{EnvironmentName}`, `{MachineName}`, `{ApplicationPoolName}`, `{VirtualPath}`, and `{PhysicalPath}` (case-insensitive); unknown tokens are left unchanged and reported in `UnknownTokens`. Persisted via create/update on `IDeployableApplicationService`. `/applications` form includes template field with token list and sample preview. |
| **Test / demo** | `dotnet test --filter LogPathTemplateServiceTests` → pass. `/applications` → edit app → set `{MachineName}\{EnvironmentCode}\{AppName}\Logs` → save → reload shows template and example preview. |
| **Depends on** | APP-001, APP-003 |
