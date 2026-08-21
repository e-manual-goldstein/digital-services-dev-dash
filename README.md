# Digital Services Dev Dash

A personal Blazor dashboard to improve day-to-day work — built **iteratively** as new ideas come up.

## Stack

- Blazor Server (.NET 10) — `DigitalDevServices.DevDash`
- Supporting projects: `DigitalDevServices.Data`, `.Model`, `.Services`, `.Plugins`
- Ticket-driven development ([agent methodology](E:\Goldstein\agent-methodology\instructions.md))

## Working with the agent

1. Add feature ideas to [docs/tickets/IDE-ideas.md](docs/tickets/IDE-ideas.md) and [docs/backlog.md](docs/backlog.md) → **Ideas**.
2. Say **Next** to implement the first ticket in [docs/backlog.md](docs/backlog.md) → **Active**.
3. Test locally, commit when satisfied, repeat.

Agents update epic docs and the backlog when a ticket ships. They do **not** commit unless you ask.

## Current state

- **Backlog:** [docs/backlog.md](docs/backlog.md)
- **Next ticket:** `ENV-003` — Mock remote environment Web API for local testing

## Run

```bash
dotnet run --project DigitalDevServices.DevDash
```

Or build the full solution:

```bash
dotnet build DigitalDevServices.DevDash/DigitalDevServices.DevDash.slnx
```
