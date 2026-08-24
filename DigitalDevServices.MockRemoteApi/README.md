# Mock Remote Environment API

Rudimentary stand-in for the Web API. Returns fixed sample data for local DevDash development.

## Run

```bash
dotnet run --project DigitalDevServices.MockRemoteApi
```

Default URL: **http://localhost:5280**

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/environments` | All sample environments |
| POST | `/api/environments` | One environment by code — body: `{ "EnvironmentCode": "UAT-01" }` (404 if unknown) |

## Sample environments

| Id | Code | Name | EnvironmentType |
|----|------|------|-----------------|
| 1 | UAT-01 | UAT-01 | UAT |
| 2 | INT | Integration | Integration |
| 3 | UAT | UAT | UAT |
| 4 | PROD | Production | Production |

## Point DevDash at the mock

With the mock API running, DevDash uses `RemoteEnvironmentApi:BaseUrl` from `appsettings.Development.json`:

```json
"RemoteEnvironmentApi": {
  "BaseUrl": "http://localhost:5280/",
  "UseNtlmAuthentication": false
}
```

Then run DevDash and open the **Environments** page.
