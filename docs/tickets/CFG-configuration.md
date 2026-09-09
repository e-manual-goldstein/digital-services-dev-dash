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
| [CFG-006](#cfg-006) | Done | Rename section to Configuration Viewer | CFG-003 |
| [CFG-007](#cfg-007) | Done | Import web.config, app.config, and exe.config | CFG-002 |
| [CFG-008](#cfg-008) | Done | Compare configuration between two instances of same app | CFG-003, PKG-004 |
| [CFG-009](#cfg-009) | Done | Compare configuration between two apps in same environment | CFG-003, PKG-005 |
| [CFG-010](#cfg-010) | Open | Connection strings in separate collapsible table | CFG-003 |
| [CFG-011](#cfg-011) | Open | Pinned configuration keys | CFG-003, ENV-003 |

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

- Read from paths on **ApplicationInstance** (`PhysicalPath`) — support JSON (`appsettings*.json`) and XML (`web.config`, `app.config`, `{ApplicationName}.exe.config`).
- **Precedence** (later overrides earlier): `appsettings.json` → other `appsettings*.json` (alphabetical) → `web.config` → `app.config` → `{appName}.exe.config` candidates → other `*.exe.config` (alphabetical).
- XML `connectionStrings` import as `ConnectionStrings:{name}`; `appSettings` keys stored as-is.
- Manual “Refresh settings” per instance or bulk per environment.
- Future: scheduled refresh, diff since last capture.

### Comparison modes

| Mode | User selects | Result |
|------|--------------|--------|
| **Same app, two instances** (CFG-008) | DeployableApplication + Instance A + Instance B | Grid: setting key → value in A vs B; highlight mismatches; connection strings sectioned separately (CFG-010) |
| **Same environment, two apps** (CFG-009) | Environment + App A + App B | Grid: setting key → value in each app; highlight mismatches; connection strings sectioned separately (CFG-010) |
| **Across environment** (shelved CFG-004) | Environment + setting key | Table: each ApplicationInstance in env → value |
| **Across environments** (shelved CFG-005) | DeployableApplication + setting key | Table: each Environment where app is deployed → value |

CFG-008 and CFG-009 supersede the shelved key-by-key compare tickets (CFG-004/CFG-005) with full side-by-side instance diff views, mirroring [Package viewer](PKG-packages.md) compare flows.

### Pinned keys (CFG-011)

Global pinned configuration keys (stored in SQLite, similar to `TrackedEnvironment.IsFavourite` / `DisplayOrder`). A pinned key appears at the top of any settings table when the current instance has a value for that key. Pin/unpin from the browse view.

### Connection strings presentation (CFG-010)

Keys matching `ConnectionStrings:*` (or imported from XML `connectionStrings`) are shown in a dedicated collapsible table, separate from general app settings, on browse and compare views.

### UI notes

- Sidebar: **Configuration viewer**
- Sub-views: **Browse** (instance → all keys), **Compare instances** (CFG-008), **Compare applications** (CFG-009)
- Browse is also reachable as `/configuration/{instanceId}` from the environment details **Viewer** button (ENV-005)
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

### CFG-006

| Field | Detail |
|-------|--------|
| **ID** | CFG-006 |
| **Title** | Rename section to Configuration Viewer |
| **Status** | Done |
| **Description** | Rebranded the **Configuration** area as **Configuration viewer** everywhere user-facing: sidebar nav label, page titles (`<PageTitle>` and `<h1>`), landing page card, environment details action column (**Viewer** button with `title="Configuration viewer"`), and breadcrumbs/back links. Route paths (`/configuration`) unchanged for bookmark compatibility. |
| **Test / demo** | Sidebar shows **Configuration Viewer** → `/configuration` page title matches → home card updated → deep link from environment still works. |
| **Depends on** | CFG-003 |

### CFG-007

| Field | Detail |
|-------|--------|
| **ID** | CFG-007 |
| **Title** | Import web.config, app.config, and exe.config |
| **Status** | Done |
| **Description** | Extended `IConfigurationImportService` to import **`web.config`**, **`app.config`**, and **`{appName}.exe.config`** alongside `appsettings*.json`. `XmlConfigurationFlattener` flattens `appSettings` keys and `connectionStrings` names into the existing `Key` / `Value` model with `Source` filename. `ConfigurationFileDiscovery` documents merge precedence. Sample XML configs under `samples/config/`. Missing XML files are skipped; JSON-only apps unchanged. |
| **Test / demo** | Instance with `PhysicalPath` containing `web.config` → **Refresh settings** → `appSettings` keys appear → `app.config` / `{appName}.exe.config` samples import → source column shows file name. `dotnet test --filter ConfigurationImport` → pass. |
| **Depends on** | CFG-002 |

### CFG-008

| Field | Detail |
|-------|--------|
| **ID** | CFG-008 |
| **Title** | Compare configuration between two instances of same app |
| **Status** | Done |
| **Description** | Add a **Compare instances** flow to Configuration viewer, mirroring [PKG-004](PKG-packages.md). **Hub:** `/configuration/compare` — pick deployable application, then Instance A and Instance B (must be different instances; typically different environments). **Result view:** `/configuration/compare/{leftInstanceId}/{rightInstanceId}` — side-by-side grid of all captured setting keys with values from each instance; highlight keys where values differ; show keys present in only one instance. Reuse secret masking from browse. Respect pinned-key ordering (CFG-011) and connection-string sectioning (CFG-010) when those tickets land; if implemented first, structure compare UI so sectioning can be added without rework. **Service:** comparison query over `ConfigurationSetting` rows for two `ApplicationInstanceId`s, keyed by `Key`. |
| **Test / demo** | Register same app in UAT-01 and SYS-02 → import settings with differing values → **Compare instances** → pick both → diff highlights mismatches → equal keys not highlighted. |
| **Depends on** | CFG-003, PKG-004 |
| **Implementation** | `/configuration/compare` picker and `/configuration/compare/{leftId}/{rightId}` results; `ConfigurationSettingComparer`, `CompareInstancesAsync`, `ConfigurationComparisonContent`; hub link on configuration viewer index. |

### CFG-009

| Field | Detail |
|-------|--------|
| **ID** | CFG-009 |
| **Title** | Compare configuration between two apps in same environment |
| **Status** | Done |
| **Description** | Add a **Compare applications** flow to Configuration viewer, mirroring [PKG-005](PKG-packages.md). **Hub:** `/configuration/compare/apps` — pick environment, then Application A and Application B (must be different deployable applications deployed in that environment). **Result view:** same route pattern as PKG-005 compare or dedicated config compare URL with both instance IDs resolved from environment + app selection. Side-by-side grid of setting keys → value in App A vs App B; highlight differences; keys only in one app shown clearly. Same masking, pinning, and connection-string rules as CFG-008/CFG-010/CFG-011. |
| **Test / demo** | Two apps deployed in UAT-01 with overlapping and distinct keys → compare → shared keys show both values → differing values highlighted. |
| **Depends on** | CFG-003, PKG-005 |
| **Implementation** | `/configuration/compare/apps` picker; shared `/configuration/compare/{leftId}/{rightId}` results and `ConfigurationComparisonContent`; `CompareInstancesAsync` accepts same-environment cross-app pairs; hub links for both compare modes. |

### CFG-010

| Field | Detail |
|-------|--------|
| **ID** | CFG-010 |
| **Title** | Connection strings in separate collapsible table |
| **Status** | Open |
| **Description** | On the configuration **browse** view (`/configuration/{instanceId}`) and on compare result views (CFG-008/CFG-009), separate keys whose name starts with `ConnectionStrings:` (case-insensitive) into a dedicated **Connection strings** collapsible section (expanded by default). General app settings remain in the main searchable table. Compare views apply the same split for both sides. Empty connection-string section hidden or shows “No connection strings captured.” |
| **Test / demo** | Import `samples/config` → browse instance → `ConnectionStrings:Default` appears only under **Connection strings** → other keys in main table → collapse section hides connection strings. |
| **Depends on** | CFG-003 |

### CFG-011

| Field | Detail |
|-------|--------|
| **ID** | CFG-011 |
| **Title** | Pinned configuration keys |
| **Status** | Open |
| **Description** | Allow users to **pin** frequently checked configuration keys so they always appear at the top of settings tables when the current instance has a value. **Model:** global pinned-key registry in SQLite (e.g. `PinnedConfigurationKey`: `Key`, `DisplayOrder`, `CreatedAt`) — same UX pattern as environment favourites (`TrackedEnvironment.IsFavourite`, `DisplayOrder`). **UI:** pin/unpin control on browse table rows (and optionally from compare); pinned keys sorted first (by `DisplayOrder`, then key name) in browse and compare tables; unpinned keys follow. Keys pinned but absent from the current instance are omitted (not shown as empty rows). **Service:** `IPinnedConfigurationKeyService` CRUD + merge into display ordering. |
| **Test / demo** | Pin `ConnectionStrings:Default` and `FeatureFlags:NewCheckout` → browse instance with both keys → pinned rows appear at top in display order → unpin one → it returns to alphabetical position in main table. |
| **Depends on** | CFG-003, ENV-003 |

