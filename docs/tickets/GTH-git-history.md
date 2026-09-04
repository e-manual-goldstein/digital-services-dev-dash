# Epic GTH — Git History

**Project:** Digital Services Dev Dash
**Code:** `GTH`
**Scope:** Track Azure DevOps repository migrations — current location, prior homes, and derived last-known prior URL — so file history research can jump to pre-migration repos without manual lookup each time.

**Depends on:** FND-002
**Blocks:** —

---

## Primary user story

> Code was extracted from a central monolithic repository into many Azure DevOps repos without preserving git history. I need to record where each repo lives today and where it lived before, so I can quickly open the right prior location when researching a file's history.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [GTH-001](#gth-001) | Done | Git History domain — entities, persistence, list and detail UI | FND-002 |

---

## Design notes

### `GitRepository`

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local PK |
| `Name` | `string` | Unique display name |
| `DateMigrated` | `DateTimeOffset` | When the repo arrived at its current location |
| `CurrentLocationUrl` | `string` | Absolute http/https URL to the current Azure DevOps repo |
| `CreatedAt` | `DateTimeOffset` | UTC audit |
| `PreviousLocations` | `ICollection<HistoricGitRepoRecord>` | Prior homes, newest first in UI |

**Derived:** `LastLocationUrl` — URL from the `PreviousLocations` entry with the latest `DateMigrated` (`GitRepositoryDisplay.GetLastLocationUrl`).

### `HistoricGitRepoRecord`

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | Local PK |
| `GitRepositoryId` | `Guid` | FK |
| `Name` | `string` | Label for the prior repo (e.g. monolith path or interim repo name) |
| `LastLocationUrl` | `string` | Absolute http/https URL |
| `DateMigrated` | `DateTimeOffset` | When code left this location |

### UI

| Route | Purpose |
|-------|---------|
| `/git-history` | Table of repositories with clickable current/last location links |
| `/git-history/{repositoryId}` | Repository summary + full previous locations table |

Nav: **Git history** in sidebar and home card. URLs render via `ExternalUrlLink` (`target="_blank"`).

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
| **Description** | Introduce **Git History** as a first-class domain. **Entities:** `GitRepository` and `HistoricGitRepoRecord` in SQLite with cascade delete on previous locations. **Service:** `IGitRepositoryService` for repository CRUD and historic-record CRUD; validate unique repository names and http/https URLs. **Derived last location:** most recent `HistoricGitRepoRecord` by `DateMigrated`. **UI:** `/git-history` list table; `/git-history/{id}` detail with previous locations table; add/edit/delete on both levels; all URLs clickable. **Nav:** sidebar and home card. |
| **Test / demo** | Add a repository with two previous locations → list shows current and derived last location links → detail page shows ordered previous locations table → edit and delete work. `dotnet test --filter GitRepositoryServiceTests` → pass. |
| **Depends on** | FND-002 |
| **Implementation** | `GitRepository`, `HistoricGitRepoRecord`, `GitRepositoryDisplay`, `GitRepositoryService`, `Pages/GitHistory/`, `ExternalUrlLink` component. |
