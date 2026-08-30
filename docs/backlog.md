# Digital Services Dev Dash — Backlog

Ordered list of **open** tickets across all epics. When a ticket is completed, add it to **Recently completed** and remove it from **Active**. That section shows **only the latest** completed ticket — replace the row when a new one lands (previous rows drop off). If you complete **multiple tickets in one batch** (same session/commit), list every ticket from that batch in the table instead.

**Source epics:** [tickets/README.md](tickets/README.md)

**Workflow:** [`E:\Goldstein\agent-methodology\instructions.md`](../../agent-methodology/instructions.md)

## Recently completed

| TicketId | Epic | Description |
|----------|------|-------------|
| ~~LOG-010~~ | [LOG](tickets/LOG-log-interpreter.md) | **View exception** modal for error rows (stack trace + inner exceptions) |

## Active (recommended order)

| TicketId | Epic | Description |
|----------|------|-------------|
| *(none)* | | |

## Epic progress

In-progress epics only. **100%** completed epics move to [Completed epics](#completed-epics-100).

| Epic | Description | Tickets Completed | Tickets Shelved | Total Tickets | Progress |
|------|-------------|-------------------|-----------------|---------------|----------|
| [Pipeline Feeds (PIP)](tickets/PIP-pipeline-feeds.md) | Named pipeline feeds (no branch matching v1) | 2 | 1 | 3 | 🟩🟩🟩🟩🟩🟩🟨🟨🟨⬜ 67% |
| [Configuration (CFG)](tickets/CFG-configuration.md) | Read and compare shared settings | 3 | 2 | 5 | 🟩🟩🟩🟩🟩🟩🟨🟨🟨🟨 60% |

*Progress bar: 10 squares — 🟩 completed, 🟨 shelved, ⬜ open; percentage = completed only.*

## Shelved

| TicketId | Epic | Description | Notes |
|----------|------|-------------|-------|
| PIP-002 | [PIP](tickets/PIP-pipeline-feeds.md) | Resolve feed from branch name on ApplicationInstance | Shelved — branch rules enforced elsewhere; no pattern matching in DevDash yet |
| CFG-004 | [CFG](tickets/CFG-configuration.md) | Compare setting by name across apps in one environment | Shelved — compare views deprioritized; per-instance browse (CFG-003) sufficient for now |
| CFG-005 | [CFG](tickets/CFG-configuration.md) | Compare setting by name for one app across environments | Shelved — compare views deprioritized; per-instance browse (CFG-003) sufficient for now |

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
| [Environments (ENV)](tickets/ENV-environments.md) | Remote API + local tracking + environment details hub | ENV-001 – ENV-017 |
| [Foundation (FND)](tickets/FND-foundation.md) | Blazor skeleton and layout | FND-001 – FND-002 |
| [Applications (APP)](tickets/APP-applications.md) | Deployable app vs instance | APP-001 – APP-005 |
| [Log Interpreter (LOG)](tickets/LOG-log-interpreter.md) | Adaptable log viewer | LOG-001 – LOG-010 |

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
        bool IsFavourite
        DateTimeOffset DateLastUpdated
    }
    PipelineFeed {
        Guid Id
        string Name
    }
    DeployableApplication {
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
