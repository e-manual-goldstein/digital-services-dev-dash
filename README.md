# Digital Services Dev Dash

A personal Blazor dashboard to improve day-to-day work — built **iteratively** as new ideas come up.

## Stack

- Blazor Server (.NET 10) — `DigitalDevServices.DevDash`
- Supporting projects: `DigitalDevServices.Data`, `.Model`, `.Services`, `.Plugins`
- Local mock of external environment API — `DigitalDevServices.MockRemoteApi`
- Ticket-driven development ([agent methodology](E:\Goldstein\agent-methodology\instructions.md))

## Working with the agent

1. Add feature ideas to [docs/tickets/IDE-ideas.md](docs/tickets/IDE-ideas.md) and [docs/backlog.md](docs/backlog.md) → **Ideas**.
2. Say **Next** to implement the first ticket in [docs/backlog.md](docs/backlog.md) → **Active**.
3. Test locally, commit when satisfied, repeat.

Agents update epic docs and the backlog when a ticket ships. They do **not** commit unless you ask.

## Current state

- **Backlog:** [docs/backlog.md](docs/backlog.md)
- **Next ticket:** `ENV-002` — Environment management UI

## Run

**Terminal 1 — mock environment API** (required for Development):

```bash
dotnet run --project DigitalDevServices.MockRemoteApi
```

Runs at **http://localhost:5280**. See [DigitalDevServices.MockRemoteApi/README.md](DigitalDevServices.MockRemoteApi/README.md).

**Terminal 2 — DevDash:**

```bash
dotnet run --project DigitalDevServices.DevDash
```

Development config points DevDash at the mock API automatically.

Build the full solution:

```bash
dotnet build DigitalDevServices.DevDash.slnx
```
