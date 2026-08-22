# Digital Services Dev Dash

A personal Blazor dashboard to improve day-to-day work — built **iteratively** as new ideas come up.

## Stack

- Blazor Server (.NET 10) — `DigitalDevServices.DevDash`
- Supporting projects: `DigitalDevServices.Data`, `.Model`, `.Services`, `.Plugins`
- Local mock of external environment API — `DigitalDevServices.MockRemoteApi`

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
