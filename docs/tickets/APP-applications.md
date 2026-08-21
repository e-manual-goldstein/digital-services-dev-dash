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
| [APP-002](#app-002) | Todo | ApplicationInstance entity and persistence | APP-001, ENV-001, PIP-001 |
| [APP-003](#app-003) | Todo | DeployableApplication admin UI | APP-001 |
| [APP-004](#app-004) | Todo | ApplicationInstance admin UI | APP-002, ENV-002 |

---

## Design notes

### Entity (`DeployableApplication`)

Represents a compilable/deployable project in the wider codebase — the logical application, independent of environment.

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, unique — human name |
| `ProjectKey` | `string?` | Optional repo/project identifier |
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
| `DeployedAt` | |

Uniqueness (v1 suggestion): one **ApplicationInstance** per (`DeployableApplicationId`, `EnvironmentId`) — redeploy updates the same row.

### Relationships

- **DeployableApplication** 1→* **ApplicationInstance**
- **TrackedEnvironment** 1→* **ApplicationInstance**
- **PipelineFeed** 1→* **ApplicationInstance** (optional FK — see [PIP-pipeline-feeds.md](PIP-pipeline-feeds.md))

### UI notes

- Sidebar: **Applications** (deployable catalog) and **Deployments** or nested under Environments
- Environment detail: list application instances in that environment
- DeployableApplication detail: list instances across environments

### Out of scope (epic v1)

- Automatic deployment detection from Azure DevOps / file system scans
- Historical deployment audit trail (multiple past build numbers per slot)
- NuGet package inventory per instance

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
| **Status** | Todo |
| **Description** | Add `ApplicationInstance` with FKs to `DeployableApplication`, `Environment`, and `PipelineFeed`. Capture origin fields (build number, branch, deployed date) and environment fields (physical path, log path, SQL override). Service layer CRUD + query by environment or deployable app. |
| **Test / demo** | Register instance: App X in Partial16, build 1.2.3, branch `feature/123456-foo` → query by environment returns row with both origin and path fields. |
| **Depends on** | APP-001, ENV-001, PIP-001 |

### APP-003

| Field | Detail |
|-------|--------|
| **ID** | APP-003 |
| **Title** | DeployableApplication admin UI |
| **Status** | Todo |
| **Description** | Blazor CRUD for deployable applications. List, create, edit, delete (guard if instances exist). Nav link under **Applications**. |
| **Test / demo** | Add “Customer Portal API” → appears in list → edit → persists. |
| **Depends on** | APP-001 |

### APP-004

| Field | Detail |
|-------|--------|
| **ID** | APP-004 |
| **Title** | ApplicationInstance admin UI |
| **Status** | Todo |
| **Description** | Blazor UI to register/edit deployments: pick deployable app + environment, enter build number, pipeline feed, branch, deploy date, physical path, log path. Accessible from environment detail and/or standalone **Deployments** page. |
| **Test / demo** | From Partial16, add deployment for an app with build number and paths → visible on environment detail and application detail. |
| **Depends on** | APP-002, ENV-002 |
