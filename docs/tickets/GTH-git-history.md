# Epic GTH — Git History

**Project:** Digital Services Dev Dash
**Code:** `GTH`
**Scope:** Track Azure DevOps repository migrations — current location, prior homes, and derived last-known prior URL — so file history research can jump to pre-migration repos without manual lookup each time.

**Depends on:** FND-002
**Blocks:** —

---

## Primary user story

> Code was extracted from a central monolithic repository into many Azure DevOps repos without preserving git history. Repositories often contain multiple components that were migrated at different times. I need to record where each component lives today and where it lived before, so I can quickly open the right prior location when researching a file's history.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [GTH-001](#gth-001) | Done | Git History domain — entities, persistence, list and detail UI | FND-002 |
| [GTH-002](#gth-002) | Done | Artifact components — per-component migration history | GTH-001 |

---

## Design notes

### `GitRepository`

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local PK |
| `Name` | `string` | Unique display name |
| `CreatedAt` | `DateTimeOffset` | UTC audit |
| `ArtifactComponents` | `ICollection<ArtifactComponent>` | Logical components within the repo |

### `ArtifactComponent`

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local PK |
| `GitRepositoryId` | `Guid` | FK |
| `Name` | `string` | Unique per repository |
| `DateMigrated` | `DateTimeOffset` | When the component arrived at its current location |
| `CurrentLocationUrl` | `string` | Absolute http/https URL to the current Azure DevOps repo/path |
| `CreatedAt` | `DateTimeOffset` | UTC audit |
| `PreviousLocations` | `ICollection<HistoricGitRepoRecord>` | Prior homes, newest first in UI |

**Derived:** `LastLocationUrl` — URL from the `PreviousLocations` entry with the latest `DateMigrated` (`ArtifactComponentDisplay.GetLastLocationUrl`).

### `HistoricGitRepoRecord`

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local PK |
| `ArtifactComponentId` | `Guid` | FK |
| `Name` | `string` | Label for the prior repo (e.g. monolith path or interim repo name) |
| `LastLocationUrl` | `string` | Absolute http/https URL |
| `DateMigrated` | `DateTimeOffset` | When code left this location |

### UI

| Route | Purpose |
|-------|---------|
| `/git-history` | Table of repositories with component count |
| `/git-history/{repositoryId}` | Repository summary + components table |
| `/git-history/{repositoryId}/components/{componentId}` | Component detail + previous locations table |

Nav: **Git history** in sidebar and home card. URLs render via `ExternalUrlLink` (`target="_blank"`).

### Data migration

Existing repository-level migration data (from GTH-001) is migrated automatically on startup: one `ArtifactComponent` is created per repository from legacy `CurrentLocationUrl` / `DateMigrated` fields, and historic records are re-linked to that component.

### Out of scope (epic v1)

- Azure DevOps API integration or automatic discovery
- Linking repositories to `DeployableApplication`
- Git operations (clone, blame, log)

---

## Tickets

### GTH-001

| Field | Detail |
|-------|--------|
| **ID** | GTH-001 |
| **Title** | Git History domain — entities, persistence, list and detail UI |
| **Status** | Done |
| **Description** | Introduce **Git History** as a first-class domain. Initial model tracked migration at repository level; superseded by GTH-002. |
| **Depends on** | FND-002 |

### GTH-002

| Field | Detail |
|-------|--------|
| **ID** | GTH-002 |
| **Title** | Artifact components — per-component migration history |
| **Status** | Done |
| **Description** | Refactor so each `GitRepository` has a collection of `ArtifactComponent` entities. Migration fields (`DateMigrated`, `CurrentLocationUrl`, `PreviousLocations`) move to the component. **Service:** component CRUD; historic records keyed by `ArtifactComponentId`. **UI:** three-level navigation (repos → components → previous locations). **Migration:** legacy repo-level data converted to a default component per repository. |
| **Test / demo** | Add a repository with two components, each with previous locations → list shows component count → component detail shows derived last location → `dotnet test --filter GitRepositoryServiceTests` → pass. |
| **Depends on** | GTH-001 |
| **Implementation** | `ArtifactComponent`, `ArtifactComponentDisplay`, updated `GitRepositoryService`, `Pages/GitHistory/ComponentDetail.razor`, SQLite migration in `DevDashDataServiceCollectionExtensions`. |
