# Epic PIP — Pipeline Feeds

**Project:** Digital Services Dev Dash
**Code:** `PIP`
**Scope:** Model **Pipeline Feeds** (also called WIP Feed, Branch Feed, or NuGet Feed): named feeds that group packages built from related pipeline work so deployments can be tied to a shared **BuildNumber** and originating feed.

**Depends on:** ENV-001
**Blocks:** APP-002 (ApplicationInstance links deployment to feed)

---

## Primary user story

> Deployments are driven by a BuildNumber produced from a pipeline on a branch. Related builds share a pipeline feed so packages across repositories can be consumed together. I need to register those feeds and link deployments to them — without re-implementing branch naming rules that already exist elsewhere.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [PIP-001](#pip-001) | Done | PipelineFeed entity and persistence | ENV-001 |
| [PIP-002](#pip-002) | Shelved | Resolve feed from branch name on ApplicationInstance | PIP-001, APP-002 |
| [PIP-003](#pip-003) | Done | Pipeline feed admin UI | PIP-001 |

---

## Design notes

### Entity (`PipelineFeed`)

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, unique — e.g. `Feature 123456`, `UAT-01 WIP` |
| `Description` | `string?` | Optional notes (repos, purpose, etc.) |
| `CreatedAt` | `DateTimeOffset` | UTC |

**No `BranchNamePattern` in v1.** Branch naming rules are enforced outside DevDash; this app records feeds by name and links deployments manually (or via explicit selection), not by parsing branch strings.

### Concepts

| Term | Meaning |
|------|---------|
| **Pipeline Feed** | Named NuGet/package pool for related pipeline output |
| **BuildNumber** | Pipeline output version string used to deploy |
| **Branch Feed** | Same as pipeline feed — a deployment can reference which feed it came from |

`ApplicationInstance.SourceBranch` is stored as metadata; **automatic** feed resolution from branch name is out of scope for now (see shelved PIP-002).

### Relationships

- **PipelineFeed** 1→* **ApplicationInstance** (deployments attributed to a feed via `PipelineFeedId`)

### UI notes

- Sidebar: **Pipeline Feeds**
- List: name, description, deployment count (once APP-002 exists)
- Create/edit: name and description only

### Out of scope (epic v1)

- Branch name pattern matching / auto-resolution from `SourceBranch` (shelved — PIP-002)
- Live Azure DevOps pipeline integration
- Automatic NuGet feed URL discovery
- Package listing inside a feed

---

## Tickets

### PIP-001

| Field | Detail |
|-------|--------|
| **ID** | PIP-001 |
| **Title** | PipelineFeed entity and persistence |
| **Status** | Done |
| **Description** | Added `PipelineFeed` entity to SQLite (`Id`, unique `Name`, `Description`, `CreatedAt`). `IPipelineFeedService` provides list/get/create/update/delete with duplicate name rejection (case-insensitive). Schema upgrade adds `PipelineFeeds` table on existing databases. Wired into DevDash host; unit tests in `PipelineFeedServiceTests`. |
| **Test / demo** | `dotnet test --filter PipelineFeedServiceTests` → pass. Create feed `Feature 123456` via service → read by name → duplicate rejected. |
| **Depends on** | ENV-001 |

### PIP-002

| Field | Detail |
|-------|--------|
| **ID** | PIP-002 |
| **Title** | Resolve feed from branch name on ApplicationInstance |
| **Status** | Shelved |
| **Description** | When saving an ApplicationInstance with `SourceBranch`, auto-resolve `PipelineFeedId` via branch pattern matcher. |
| **Test / demo** | *(not implemented)* |
| **Depends on** | PIP-001, APP-002 |

Shelved — branch naming rules are enforced elsewhere; DevDash does not need pattern matching logic yet. Revisit if automatic feed suggestion from `SourceBranch` becomes valuable.

### PIP-003

| Field | Detail |
|-------|--------|
| **ID** | PIP-003 |
| **Title** | Pipeline feed admin UI |
| **Status** | Done |
| **Description** | Blazor CRUD at `/pipeline-feeds`: list with deployment count, add/edit name and description, delete with confirm step. Nav link and home card enabled. Feeds available for APP-004 instance dropdown. |
| **Test / demo** | Run DevDash → **Pipeline feeds** → create feed → appears in list → edit description → persists after restart. |
| **Depends on** | PIP-001 |
