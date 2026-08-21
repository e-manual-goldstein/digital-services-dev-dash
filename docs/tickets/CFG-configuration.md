# Epic CFG — Shared Configuration Settings

**Project:** Digital Services Dev Dash
**Code:** `CFG`
**Scope:** Read and compare shared configuration settings (connection strings, feature toggles, API secrets, etc.) across deployed applications — by setting name within an environment, or for one application across environments.

**Depends on:** APP-002, APP-004
**Blocks:** —

---

## Primary user story

> Every team configures apps differently, but I constantly need to answer “what is the connection string for X?” or “does this flag differ between Partial16 and Production?” I want to pick a setting by name and compare it across apps or environments in seconds.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [CFG-001](#cfg-001) | Todo | Configuration setting model and storage | APP-002 |
| [CFG-002](#cfg-002) | Todo | Import settings from deployed application locations | CFG-001, APP-004 |
| [CFG-003](#cfg-003) | Todo | Settings browser UI (view all settings for an instance) | CFG-002 |
| [CFG-004](#cfg-004) | Todo | Compare setting by name across apps in one environment | CFG-002, ENV-002 |
| [CFG-005](#cfg-005) | Todo | Compare setting by name for one app across environments | CFG-002, APP-004 |

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
| **Status** | Todo |
| **Description** | Add `ConfigurationSetting` entity, repository, upsert-by-key service. Link to `ApplicationInstance`. |
| **Test / demo** | Upsert two keys for an instance → list by instance → update value → `CapturedAt` changes. |
| **Depends on** | APP-002 |

### CFG-002

| Field | Detail |
|-------|--------|
| **ID** | CFG-002 |
| **Title** | Import settings from deployed application locations |
| **Status** | Todo |
| **Description** | Service to read config files from `ApplicationInstance.PhysicalPath` (JSON v1 minimum). Flatten nested keys with `:` separator. Store via CFG-001 upsert. Handle missing path gracefully. |
| **Test / demo** | Point instance at folder with sample `appsettings.json` → refresh → keys appear in DB. |
| **Depends on** | CFG-001, APP-004 |

### CFG-003

| Field | Detail |
|-------|--------|
| **ID** | CFG-003 |
| **Title** | Settings browser UI (view all settings for an instance) |
| **Status** | Todo |
| **Description** | Blazor page: pick environment → application instance → searchable table of all settings. Refresh button. Secret masking. |
| **Test / demo** | Select Partial16 + app instance → see settings → refresh → updated values. |
| **Depends on** | CFG-002 |

### CFG-004

| Field | Detail |
|-------|--------|
| **ID** | CFG-004 |
| **Title** | Compare setting by name across apps in one environment |
| **Status** | Todo |
| **Description** | Blazor compare view: pick environment + setting key (autocomplete from known keys) → grid of each deployed app and its value. Highlight differences. |
| **Test / demo** | Pick `ConnectionStrings:Default` in Partial16 → see all apps’ values side by side. |
| **Depends on** | CFG-002, ENV-002 |

### CFG-005

| Field | Detail |
|-------|--------|
| **ID** | CFG-005 |
| **Title** | Compare setting by name for one app across environments |
| **Status** | Todo |
| **Description** | Blazor compare view: pick DeployableApplication + setting key → grid of each environment where deployed and value. Highlight differences. |
| **Test / demo** | Pick “Customer Portal API” + `FeatureFlags:Beta` → see values in Partial16 vs other envs. |
| **Depends on** | CFG-002, APP-004 |
