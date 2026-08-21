# Digital Services Dev Dash — Backlog

Ordered list of **open** tickets across all epics. When a ticket is completed, add it to **Recently completed** and remove it from **Active**.

**Source epics:** [tickets/README.md](tickets/README.md)

**Workflow:** [`E:\Goldstein\agent-methodology\instructions.md`](../../agent-methodology/instructions.md)

## Recently completed

| TicketId | Epic | Description |
|----------|------|-------------|
| ~~PIP-001~~ | [PIP](tickets/PIP-pipeline-feeds.md) | PipelineFeed entity and persistence |

## Active (recommended order)

| TicketId | Epic | Description |
|----------|------|-------------|
| APP-001 | [APP](tickets/APP-applications.md) | DeployableApplication entity and persistence |
| APP-002 | [APP](tickets/APP-applications.md) | ApplicationInstance entity and persistence |
| APP-003 | [APP](tickets/APP-applications.md) | DeployableApplication admin UI |
| PIP-003 | [PIP](tickets/PIP-pipeline-feeds.md) | Pipeline feed admin UI |
| APP-004 | [APP](tickets/APP-applications.md) | ApplicationInstance admin UI |
| LOG-001 | [LOG](tickets/LOG-log-interpreter.md) | LogFormatProfile per DeployableApplication |
| CFG-001 | [CFG](tickets/CFG-configuration.md) | Configuration setting model and storage |
| CFG-002 | [CFG](tickets/CFG-configuration.md) | Import settings from deployed application locations |
| LOG-002 | [LOG](tickets/LOG-log-interpreter.md) | Log file reader using ApplicationInstance paths |
| CFG-003 | [CFG](tickets/CFG-configuration.md) | Settings browser UI (view all settings for an instance) |
| CFG-004 | [CFG](tickets/CFG-configuration.md) | Compare setting by name across apps in one environment |
| CFG-005 | [CFG](tickets/CFG-configuration.md) | Compare setting by name for one app across environments |
| LOG-003 | [LOG](tickets/LOG-log-interpreter.md) | Log viewer UI (environment → instance picker) |
| LOG-004 | [LOG](tickets/LOG-log-interpreter.md) | Log filtering (level, text search) |

## Epic progress

In-progress epics only. **100%** completed epics move to [Completed epics](#completed-epics-100).

| Epic | Description | Tickets Completed | Tickets Shelved | Total Tickets | Progress |
|------|-------------|-------------------|-----------------|---------------|----------|
| [Applications (APP)](tickets/APP-applications.md) | Deployable app vs instance | 0 | 0 | 4 | ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ 0% |
| [Pipeline Feeds (PIP)](tickets/PIP-pipeline-feeds.md) | Named pipeline feeds (no branch matching v1) | 1 | 1 | 3 | 🟩🟩🟩🟨🟨🟨⬜⬜⬜⬜ 33% |
| [Configuration (CFG)](tickets/CFG-configuration.md) | Read and compare shared settings | 0 | 0 | 5 | ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ 0% |
| [Log Interpreter (LOG)](tickets/LOG-log-interpreter.md) | Adaptable log viewer | 0 | 0 | 4 | ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜ 0% |

*Progress bar: 10 squares — 🟩 completed, 🟨 shelved, ⬜ open; percentage = completed only.*

## Shelved

| TicketId | Epic | Description | Notes |
|----------|------|-------------|-------|
| PIP-002 | [PIP](tickets/PIP-pipeline-feeds.md) | Resolve feed from branch name on ApplicationInstance | Shelved — branch rules enforced elsewhere; no pattern matching in DevDash yet |

## Cancelled

| TicketId | Epic | Description | Notes |
|----------|------|-------------|-------|
| *(none)* | | | |

## Ideas

Unprioritized — not in the active queue. See [IDE-ideas.md](tickets/IDE-ideas.md).

| TicketId | Epic | Description |
|----------|------|-------------|
| *(add ideas as you think of them)* | [IDE](tickets/IDE-ideas.md) | |

---

## Completed epics (100%)

| Epic | Description | Completed |
|------|-------------|-----------|
| [Foundation (FND)](tickets/FND-foundation.md) | Blazor skeleton and layout | FND-001 – FND-002 |
| [Environments (ENV)](tickets/ENV-environments.md) | Remote API + local tracking + UI | ENV-001 – ENV-003 |

## Domain model (overview)

```mermaid
erDiagram
    TrackedEnvironment ||--o{ ApplicationInstance : contains
    DeployableApplication ||--o{ ApplicationInstance : deployed_as
    PipelineFeed ||--o{ ApplicationInstance : originates_from
    DeployableApplication ||--o| LogFormatProfile : log_format
    ApplicationInstance ||--o{ ConfigurationSetting : has
    TrackedEnvironment {
        Guid Id
        int RemoteId
        DateTimeOffset DateLastUpdated
    }
    PipelineFeed {
        Guid Id
        string Name
    }
```

| Epic | Core entities |
|------|---------------|
| ENV | `TrackedEnvironment` (+ remote `RemoteEnvironmentDetails`) |
| APP | `DeployableApplication`, `ApplicationInstance` |
| PIP | `PipelineFeed` |
| CFG | `ConfigurationSetting` |
| LOG | `LogFormatProfile` |
