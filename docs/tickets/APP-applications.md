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
| [APP-005](#app-005) | Todo | `PathToLogFiles` template on DeployableApplication | APP-001, APP-003 |

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
| **Status** | Todo |
| **Description** | Add optional `PathToLogFiles` on `DeployableApplication` — a path **template** (not a literal path) stored in SQLite and editable on `/applications`. Provide `ILogPathTemplateService` (or similar) that resolves a template to a concrete path using token substitution in the same style as .NET composite formatting, e.g. `{MachineName}\{EnvironmentCode}\{AppName}\Logs`. **v1 tokens** (document in epic): `AppName`, `EnvironmentCode`, `EnvironmentName`, `MachineName`, `ApplicationPoolName`, `VirtualPath` (IIS path), `PhysicalPath`. Unknown tokens left unchanged or surfaced as a validation warning at resolve time (pick one rule and test it). Expose resolved preview in UI when helpful. Persist via schema upgrade; include in create/update service methods. Does **not** auto-write `ApplicationInstance.LogPath` until a deployment is saved — resolution is invoked when pre-filling or saving an instance (see ENV-015). |
| **Test / demo** | `dotnet test --filter LogPathTemplateServiceTests` → pass. Template `{MachineName}\{EnvironmentCode}\{AppName}\Logs` with `MachineName=UAT-01-APP`, `EnvironmentCode=UAT-01`, `AppName=portal` → `UAT-01-APP\UAT-01\portal\Logs`. `/applications` → edit app → set template → save → reload shows value. |
| **Depends on** | APP-001, APP-003 |
