# Epic CFG — Shared Configuration Settings

**Project:** Digital Services Dev Dash
**Code:** `CFG`
**Scope:** Read and compare shared configuration settings (connection strings, feature toggles, API secrets, etc.) across deployed applications — by setting name within an environment, or for one application across environments.

**Depends on:** APP-002, APP-004
**Blocks:** —

---

## Primary user story

> Every team configures apps differently, but I constantly need to answer “what is the connection string for X?” or “does this flag differ between UAT-01 and SYS-02?” I want to pick a setting by name and compare it across apps or environments in seconds.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [CFG-001](#cfg-001) | Done | Configuration setting model and storage | APP-002 |
| [CFG-002](#cfg-002) | Done | Import settings from deployed application locations | CFG-001, APP-004 |
| [CFG-003](#cfg-003) | Done | Settings browser UI (view all settings for an instance) | CFG-002, ENV-005 |
| [CFG-004](#cfg-004) | Shelved | Compare setting by name across apps in one environment | CFG-002, ENV-002 |
| [CFG-005](#cfg-005) | Shelved | Compare setting by name for one app across environments | CFG-002, APP-004 |

---

## Design notes

### Entity (`ConfigurationSetting`)

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `ApplicationInstanceId` | `Guid` | FK |
| `Key` | `string` | Setting name — e.g. `ConnectionStrings:Default`, `FeatureFlags:NewCheckout` |
| `Value` | `string` | Stored value (mask secrets in UI when key suggests secret) |
| `Source` | `string?` | Where read from — e.g. `appsettings.json`, `web.config` |
| `CapturedAt` | `DateTimeOffset` | When imported/refreshed |

Uniqueness: one row per (`ApplicationInstanceId`, `Key`) — refresh replaces value.

### Import strategy (v1)

- Read from paths on **ApplicationInstance** (`PhysicalPath`) — support common formats: JSON (`appsettings*.json`), XML (`web.config`), key-value env files.
- Manual “Refresh settings” per instance or bulk per environment.
- Future: scheduled refresh, diff since last capture.

### Comparison modes

| Mode | User selects | Result |
|------|--------------|--------|
| **Across environment** | Environment + setting key | Table: each ApplicationInstance in env → value |
| **Across environments** | DeployableApplication + setting key | Table: each Environment where app is deployed → value |

### UI notes

- Sidebar: **Configuration**
- Sub-views: **Browse** (instance → all keys), **Compare in environment**, **Compare across environments**
- Browse is also reachable as `/configuration/{instanceId}` from the environment details **Configuration** button (ENV-005)
- Mask values when key matches `*Secret*`, `*Password*`, `*Key*` (configurable list)

### Out of scope (epic v1)

- Edit/write settings back to deployed files
- Azure App Configuration / Key Vault integration
- Encrypted value decryption

---

## Tickets

### CFG-001

| Field | Detail |
|-------|--------|
| **ID** | CFG-001 |
| **Title** | Configuration setting model and storage |
| **Status** | Done |
| **Description** | Added `ConfigurationSetting` entity (FK to `ApplicationInstance`, unique per instance+key: `Key`, `Value`, `Source`, `CapturedAt`) with SQLite schema upgrade. `IConfigurationSettingService` lists settings by instance and upserts by key (refresh updates value and `CapturedAt`). Registered in DI via `AddConfigurationServices`. |
| **Test / demo** | `dotnet test --filter ConfigurationSettingServiceTests` → pass. Upsert two keys for an instance → list by instance → update value → `CapturedAt` advances. |
| **Depends on** | APP-002 |

### CFG-002

| Field | Detail |
|-------|--------|
| **ID** | CFG-002 |
| **Title** | Import settings from deployed application locations |
| **Status** | Done |
| **Description** | `IConfigurationImportService.RefreshAsync` reads `appsettings*.json` from `ApplicationInstance.PhysicalPath`, flattens nested JSON keys with `:` separators (`JsonConfigurationFlattener`), merges files (`appsettings.json` first, then overrides from environment-specific files), and stores via `IConfigurationSettingService.UpsertManyAsync`. Missing path, missing folder, and parse/read errors return a result with `ErrorMessage` (same pattern as package scan). Sample files in `samples/config/`. |
| **Test / demo** | `dotnet test --filter ConfigurationImportServiceTests` → pass. Point instance `PhysicalPath` at `samples/config` → call `RefreshAsync` → keys like `ConnectionStrings:Default` stored with source filename. |
| **Depends on** | CFG-001, APP-004 |

### CFG-003

| Field | Detail |
|-------|--------|
| **ID** | CFG-003 |
| **Title** | Settings browser UI (view all settings for an instance) |
| **Status** | Done |
| **Description** | Blazor **Configuration** section: `/configuration` with environment → application instance pickers; `/configuration/{instanceId}` browse view (deep link from environment details **Configuration** button) with searchable settings table, **Refresh settings** (imports from `PhysicalPath`), secret masking for keys containing Secret/Password/Key (toggle to reveal), source file and captured timestamp columns. Nav and home card updated. |
| **Test / demo** | Environment details → **Configuration** on an instance → settings table. Set `PhysicalPath` to `samples/config` → **Refresh settings** → keys appear. Sidebar **Configuration** → pick environment and app → **Browse**. Masked values for keys like `Api:ClientSecret`. |
| **Depends on** | CFG-002, ENV-005 |

### CFG-004

| Field | Detail |
|-------|--------|
| **ID** | CFG-004 |
| **Title** | Compare setting by name across apps in one environment |
| **Status** | Shelved |
| **Description** | Blazor compare view: pick environment + setting key (autocomplete from known keys) → grid of each deployed app and its value. Highlight differences. |
| **Test / demo** | Pick `ConnectionStrings:Default` in UAT-01 → see all apps’ values side by side. |
| **Depends on** | CFG-002, ENV-002 |

Shelved — compare views deprioritized; per-instance browse (CFG-003) sufficient for now. Revisit when cross-app comparison within an environment is needed.

### CFG-005

| Field | Detail |
|-------|--------|
| **ID** | CFG-005 |
| **Title** | Compare setting by name for one app across environments |
| **Status** | Shelved |
| **Description** | Blazor compare view: pick DeployableApplication + setting key → grid of each environment where deployed and value. Highlight differences. |
| **Test / demo** | Pick “Customer Portal API” + `FeatureFlags:Beta` → see values in UAT-01 vs other envs. |
| **Depends on** | CFG-002, APP-004 |

Shelved — compare views deprioritized; per-instance browse (CFG-003) sufficient for now. Revisit when cross-environment comparison for one app is needed.
