# Epic FND — Foundation

**Project:** Digital Services Dev Dash
**Code:** `FND`
**Scope:** Blazor Server solution skeleton — buildable project, home page, nav shell, and conventions for adding features iteratively.

**Depends on:** —
**Blocks:** All feature epics

---

## Primary user story

> I need a Blazor app I can run locally that loads fast, looks like a real dashboard, and gives me a place to land new tools as I think of them — without fighting broken dependencies or stale template cruft.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [FND-001](#fnd-001) | Done | Blazor Server solution skeleton and home page | — |
| [FND-002](#fnd-002) | Todo | Dashboard layout shell (nav, branding, empty sections) | FND-001 |

---

## Design notes

### Naming

| Layer | Name |
|-------|------|
| Repo folder | `DigitalServicesDevDash` |
| Solution / projects | `DigitalDevServices.*` |
| Blazor host | `DigitalDevServices.DevDash` |
| UI title | **Digital Services Dev Dash** |

### Stack

- **.NET 10**
- **Blazor Server** (interactive UI without separate WASM host)
- **Bootstrap** (default Blazor template styling; keep it simple for v1)
- **Radzen.Blazor** (UI components)

### Solution layout

```
DigitalServicesDevDash/
  DigitalDevServices.DevDash/
    DigitalDevServices.DevDash.slnx
    DigitalDevServices.DevDash.csproj    # Blazor Server host
  DigitalDevServices.Data/
  DigitalDevServices.Model/
  DigitalDevServices.Services/
  DigitalDevServices.Services.Test/
  DigitalDevServices.Plugins/
  docs/
    backlog.md
    tickets/
```

The solution was bootstrapped from an unrelated prior template; project names and references have been migrated to `DigitalDevServices.*`.

### Out of scope (foundation epic)

- Authentication / multi-user
- Database or external API integrations (Data/Services projects are placeholders for now)

---

## Tickets

### FND-001

| Field | Detail |
|-------|--------|
| **ID** | FND-001 |
| **Title** | Blazor Server solution skeleton and home page |
| **Status** | Done |
| **Description** | Migrated template to `DigitalDevServices.*` projects with Blazor Server host at `DigitalDevServices.DevDash`. Solution builds cleanly. Home/admin landing at `/` and `/admin`. |
| **Test / demo** | `dotnet build DigitalDevServices.DevDash/DigitalDevServices.DevDash.slnx` → 0 errors. `dotnet run --project DigitalDevServices.DevDash` → browser shows admin home. |
| **Depends on** | — |

### FND-002

| Field | Detail |
|-------|--------|
| **ID** | FND-002 |
| **Title** | Dashboard layout shell (nav, branding, empty sections) |
| **Status** | Todo |
| **Description** | Trim `MainLayout` + `NavMenu` to match DevDash scope — remove orphan links left from the template (pages that no longer exist). Keep branding **Digital Services Dev Dash**. Home empty state: “Add features via backlog tickets.” Responsive nav toggle. |
| **Test / demo** | Run app → sidebar shows only valid nav links; home shows welcome + backlog hint; resize window → mobile nav toggles. No dead links. |
| **Depends on** | FND-001 |
