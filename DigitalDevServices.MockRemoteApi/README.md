# Mock Remote Environment API

Rudimentary stand-in for the external team's environment Web API. Returns fixed sample data for local DevDash development.

## Run

```bash
dotnet run --project DigitalDevServices.MockRemoteApi
```

Default URL: **http://localhost:5280**

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/environments` | All sample environments |
| GET | `/api/environments/{id}` | One environment by remote id (404 if unknown) |

## Sample environments

| RemoteId | Name | SQL Server instance |
|----------|------|---------------------|
| 1 | Partial16 | `PARTIAL16\SQL2019` |
| 2 | Integration | `INT-SQL01\DEV` |
| 3 | UAT | `UAT-SQL01\STD` |
| 4 | Production | `PROD-SQL01\STD` |

## Point DevDash at the mock

With the mock API running, DevDash uses `RemoteEnvironmentApi:BaseUrl` from `appsettings.Development.json`:

```json
"RemoteEnvironmentApi": {
  "BaseUrl": "http://localhost:5280/"
}
```

Then run DevDash and call `IEnvironmentService.TrackEnvironmentAsync(1)` (or use the Environments UI once ENV-002 ships).
