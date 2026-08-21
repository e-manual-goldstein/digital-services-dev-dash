# Epic PIP — Pipeline Feeds

**Project:** Digital Services Dev Dash
**Code:** `PIP`
**Scope:** Model **Pipeline Feeds** (also called WIP Feed, Branch Feed, or NuGet Feed): a branch name pattern that groups packages built from matching branches so pipelines across repositories can consume each other's outputs and produce a shared **BuildNumber** for deployment.

**Depends on:** ENV-001
**Blocks:** APP-002 (ApplicationInstance links deployment to feed)

---

## Primary user story

> Deployments are driven by a BuildNumber produced from a pipeline on a branch. Branches with the same feed pattern share NuGet packages across repos — so a deployment effectively originated from a particular branch feed. I need to define and manage those feeds and tie them to deployments.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [PIP-001](#pip-001) | Todo | PipelineFeed entity and branch pattern matching | ENV-001 |
| [PIP-002](#pip-002) | Todo | Resolve feed from branch name on ApplicationInstance | PIP-001, APP-002 |
| [PIP-003](#pip-003) | Todo | Pipeline feed admin UI | PIP-001 |

---

## Design notes

### Entity (`PipelineFeed`)

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required — display name, e.g. `Feature 123456` |
| `BranchNamePattern` | `string` | Required — e.g. `feature/123456-*` or regex/glob (define matching rules in implementation) |
| `Description` | `string?` | What repos/packages participate |
| `CreatedAt` | `DateTimeOffset` | UTC |

**Matching (v1):** document chosen strategy in code — glob prefix match is likely sufficient for patterns like `feature/123456-my-special-branch` sharing feed `feature/123456-*`.

### Concepts

| Term | Meaning |
|------|---------|
| **Pipeline Feed** | Logical NuGet/package pool keyed by branch pattern |
| **BuildNumber** | Pipeline output version string used to deploy |
| **Branch Feed** | Same as pipeline feed — deployment can be said to originate from the feed matched by `SourceBranch` |

When an **ApplicationInstance** is saved with a `SourceBranch`, DevDash can resolve (or suggest) the matching **PipelineFeed**.

### Relationships

- **PipelineFeed** 1→* **ApplicationInstance** (deployments attributed to a feed)

### UI notes

- Sidebar: **Pipeline Feeds**
- List: name, branch pattern, deployment count
- Create/edit: name, pattern, description with examples

### Out of scope (epic v1)

- Live Azure DevOps pipeline integration
- Automatic NuGet feed URL discovery
- Package listing inside a feed

---

## Tickets

### PIP-001

| Field | Detail |
|-------|--------|
| **ID** | PIP-001 |
| **Title** | PipelineFeed entity and branch pattern matching |
| **Status** | Todo |
| **Description** | Add `PipelineFeed` to SQLite schema with CRUD service. Implement branch name → feed matcher (glob or prefix rules). Unit tests for pattern matching edge cases. |
| **Test / demo** | Feed pattern `feature/123456-*` matches `feature/123456-my-special-branch`; does not match `feature/999999-other`. |
| **Depends on** | ENV-001 |

### PIP-002

| Field | Detail |
|-------|--------|
| **ID** | PIP-002 |
| **Title** | Resolve feed from branch name on ApplicationInstance |
| **Status** | Todo |
| **Description** | When saving an ApplicationInstance with `SourceBranch`, auto-resolve `PipelineFeedId` via matcher (allow manual override). Expose feed name on instance queries. |
| **Test / demo** | Save instance with branch `feature/123456-foo` → `PipelineFeedId` set to matching feed; changing branch re-resolves. |
| **Depends on** | PIP-001, APP-002 |

### PIP-003

| Field | Detail |
|-------|--------|
| **ID** | PIP-003 |
| **Title** | Pipeline feed admin UI |
| **Status** | Todo |
| **Description** | Blazor CRUD for pipeline feeds. Show example branches that would match each pattern (test input field). Nav link under **Pipeline Feeds**. |
| **Test / demo** | Create feed → test matcher UI shows match for sample branch → feed appears in ApplicationInstance dropdown. |
| **Depends on** | PIP-001 |
