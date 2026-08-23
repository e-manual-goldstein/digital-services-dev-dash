# Epic catalogue — Digital Services Dev Dash

Ticket-driven development for a personal/work Blazor dashboard. Workflow: see [`E:\Goldstein\agent-methodology\instructions.md`](../../../agent-methodology/instructions.md) (or copy that folder into this repo).

**Control panel:** [backlog.md](../backlog.md)

## Project naming

| Layer | Name |
|-------|------|
| Repo folder | `DigitalServicesDevDash` |
| Solution / projects | `DigitalDevServices.*` |
| Blazor host | `DigitalDevServices.DevDash` |
| UI title | **Digital Services Dev Dash** |

## Status vocabulary

| Status | Meaning |
|--------|---------|
| **Todo** | Not started; eligible for Active queue |
| **In Progress** | Agent or human is actively working on it |
| **Done** | Shipped and verified |
| **Blocked** | Cannot proceed — document blocker |
| **Shelved** | Paused / rejected approach |
| **Cancelled** | Will not implement |
| **Idea** | Unprioritized — in backlog Ideas section |

## Epics

| Code | File | Description |
|------|------|-------------|
| **FND** | [FND-foundation.md](FND-foundation.md) | Solution skeleton, Blazor host, layout shell |
| **ENV** | [ENV-environments.md](ENV-environments.md) | Named environments, SQLite, SQL Server instance, details hub |
| **APP** | [APP-applications.md](APP-applications.md) | DeployableApplication vs ApplicationInstance |
| **PIP** | [PIP-pipeline-feeds.md](PIP-pipeline-feeds.md) | Named pipeline feeds (WIP / Branch / NuGet), BuildNumber |
| **CFG** | [CFG-configuration.md](CFG-configuration.md) | Shared settings read and compare |
| **LOG** | [LOG-log-interpreter.md](LOG-log-interpreter.md) | Adaptable log viewer per application |
| **IDE** | [IDE-ideas.md](IDE-ideas.md) | Unprioritized feature ideas |

## Ticket ID format

`<EPIC>-<NNN>` — e.g. `FND-001`, `IDE-003`.

## Session rhythm

1. Say **Next** (or name a ticket).
2. Agent implements the first **Active** ticket, updates epic + backlog, gives manual test steps.
3. You test, commit when ready, say **Next** again.

Agents do **not** commit unless you ask.
