# Epic ENV — Environments

**Project:** Digital Services Dev Dash
**Code:** `ENV`
**Scope:** Track named deployment environments (e.g. `Partial16`) in a local SQLite database. Each environment owns a dedicated SQL Server instance and acts as the container for deployed application instances.

**Depends on:** FND-002
**Blocks:** APP (ApplicationInstance), CFG, LOG

---

## Primary user story

> I work across many inconsistently configured environments. I need a single place to register each environment by name, record its SQL Server instance, and see what is deployed there — without relying on spreadsheets or tribal knowledge.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [ENV-001](#env-001) | Todo | SQLite bootstrap and Environment entity | FND-002 |
| [ENV-002](#env-002) | Todo | Environment management UI | ENV-001 |

---

## Design notes

### Entity (`Environment`)

| Field | Type | Notes |
|-------|------|--------|
| `Id` | `Guid` | PK |
| `Name` | `string` | Required, unique — e.g. `Partial16` |
| `SqlServerInstance` | `string` | Dedicated SQL Server for this environment |
| `Notes` | `string?` | Optional free text |
| `CreatedAt` | `DateTimeOffset` | UTC |
| `UpdatedAt` | `DateTimeOffset?` | Last mutation |

**Validation (v1):** non-empty `Name`; `SqlServerInstance` required.

### Storage

- Local **SQLite** database owned by DevDash (path under user app data or configurable in `appsettings`).
- Implemented in `DigitalDevServices.Data` with EF Core (or existing data conventions).

### Relationships

- One **Environment** has many **ApplicationInstance** records (see [APP-applications.md](APP-applications.md)).
- Environment-specific properties of an application (physical deploy path, log file location, etc.) live on **ApplicationInstance**, not on **Environment**.

### UI notes

- Sidebar section: **Environments**
- List view: name, SQL Server instance, count of deployed applications (once APP-002 exists)
- Detail/edit: name, SQL Server instance, notes

### Out of scope (epic v1)

- Remote environment discovery / auto-provisioning
- Health checks against SQL Server instances
- Environment grouping or hierarchy

---

## Tickets

### ENV-001

| Field | Detail |
|-------|--------|
| **ID** | ENV-001 |
| **Title** | SQLite bootstrap and Environment entity |
| **Status** | Todo |
| **Description** | Add local SQLite database to `DigitalDevServices.Data`: connection string config, `DbContext`, migrations/EnsureCreated. Add `Environment` entity, repository/service, and CRUD operations (no UI yet). |
| **Test / demo** | Unit test or manual: create environment → read back by name; duplicate name rejected. DB file created on first run. |
| **Depends on** | FND-002 |

### ENV-002

| Field | Detail |
|-------|--------|
| **ID** | ENV-002 |
| **Title** | Environment management UI |
| **Status** | Todo |
| **Description** | Blazor pages: list environments, create, edit. Fields: name, SQL Server instance, notes. Empty state when none exist. Nav link under **Environments**. |
| **Test / demo** | Add `Partial16` with SQL instance → appears in list → edit name → persists after restart. |
| **Depends on** | ENV-001 |
