# Sample configuration files

Use these for manual testing of configuration import (CFG-002) and the settings browser (CFG-003).

Point an **ApplicationInstance** `PhysicalPath` at this folder (use the full path on your machine, e.g. `E:\Goldstein\DigitalServicesDevDash\samples\config`).

Files:

- `appsettings.json` — base settings with nested `ConnectionStrings`, `FeatureFlags`, and `Logging`
- `appsettings.Production.json` — overrides `ConnectionStrings:Default` and `FeatureFlags:NewCheckout`

Import reads `appsettings.json` first, then other `appsettings*.json` files in alphabetical order so later files override earlier keys.
