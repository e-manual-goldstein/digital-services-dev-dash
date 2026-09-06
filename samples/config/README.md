# Sample configuration files

Use these for manual testing of configuration import (CFG-002, CFG-007) and the configuration viewer (CFG-003).

Point an **ApplicationInstance** `PhysicalPath` at this folder (use the full path on your machine, e.g. `E:\Goldstein\DigitalServicesDevDash\samples\config`).

## Files

- `appsettings.json` — base settings with nested `ConnectionStrings`, `FeatureFlags`, and `Logging`
- `appsettings.Production.json` — overrides `ConnectionStrings:Default` and `FeatureFlags:NewCheckout`
- `web.config` — `appSettings` keys and `connectionStrings` for IIS-style deployments
- `app.config` — desktop/service `appSettings` and `connectionStrings`
- `CustomerPortalAPI.exe.config` — example `{appName}.exe.config` sidecar settings

## Import precedence

Later files override earlier keys with the same name:

1. `appsettings.json`
2. Other `appsettings*.json` files (alphabetical)
3. `web.config`
4. `app.config`
5. `{ApplicationName}.exe.config` candidate names, then any other `*.exe.config` in the folder (alphabetical)

XML `appSettings` keys are stored as-is. XML `connectionStrings` are stored as `ConnectionStrings:{name}`.
