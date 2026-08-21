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


| ID                  | Status | Title                                                  | Depends on |
| ------------------- | ------ | ------------------------------------------------------ | ---------- |
| [FND-001](#fnd-001) | Done   | Blazor Server solution skeleton and home page          | —          |
| [FND-002](#fnd-002) | Done   | Dashboard layout shell (nav, branding, empty sections) | FND-001    |


---



## Design notes



### Naming


| Layer               | Name                          |
| ------------------- | ----------------------------- |
| Repo folder         | `DigitalServicesDevDash`      |
| Solution / projects | `DigitalDevServices.*`        |
| Blazor host         | `DigitalDevServices.DevDash`  |
| UI title            | **Digital Services Dev Dash** |




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
- Domain data (environments, applications, etc.) — see ENV, APP, and related epics

---



## Tickets



### FND-001


| Field           | Detail                                                                                                                                                                         |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **ID**          | FND-001                                                                                                                                                                        |
| **Title**       | Blazor Server solution skeleton and home page                                                                                                                                  |
| **Status**      | Done                                                                                                                                                                           |
| **Description** | Migrated template to `DigitalDevServices.`* projects with Blazor Server host at `DigitalDevServices.DevDash`. Solution builds cleanly. Home/admin landing at `/` and `/admin`. |
| **Test / demo** | `dotnet build DigitalDevServices.DevDash/DigitalDevServices.DevDash.slnx` → 0 errors. `dotnet run --project DigitalDevServices.DevDash` → browser shows admin home.            |
| **Depends on**  | —                                                                                                                                                                              |




### FND-002


| Field           | Detail                                                                                                                                                                                                                                                    |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **ID**          | FND-002                                                                                                                                                                                                                                                   |
| **Title**       | Dashboard layout shell (nav, branding, empty sections)                                                                                                                                                                                                    |
| **Status**      | Done                                                                                                                                                                                                                                                      |
| **Description** | Trimmed `MainLayout` and `NavMenu`: branding **Digital Services Dev Dash**, Home nav only (no dead links), muted **Planned** section for upcoming epics. Home welcome + backlog hint and roadmap placeholder cards. Removed template Portal page and main About link. Responsive nav toggle retained. |
| **Test / demo** | Run app → sidebar shows Home + planned labels (non-links); home shows welcome and backlog hint; narrow viewport → hamburger toggles nav. |
| **Depends on**  | FND-001                                                                                                                                                                                                                                                   |


