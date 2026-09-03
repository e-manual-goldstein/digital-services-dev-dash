# Epic PKG — Deployed Packages

**Project:** Digital Services Dev Dash
**Code:** `PKG`
**Scope:** First-class **Packages** domain for inspecting DLL versions deployed to application instances — decoupled from the Environments epic, with manifest support, build-number-based package resolution, and comparison views.

**Depends on:** APP-002, ENV-006
**Blocks:** —

---

## Primary user story

> I need to know exactly which DLL versions are on a server, whether they came from the right build artefact, and how two deployments differ — without treating packages as a buried sub-page under Environments.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [PKG-001](#pkg-001) | Done | Packages as first-class domain (nav and routes) | ENV-006 |
| [PKG-002](#pkg-002) | Done | Consume deployment manifest file when present | PKG-001 |
| [PKG-003](#pkg-003) | Open | Resolve deployment package by build number for instance view | PKG-001, ENV-016 |
| [PKG-004](#pkg-004) | Done | Compare DLL versions between two instances of same app | PKG-001 |
| [PKG-005](#pkg-005) | Open | Compare DLL versions between two apps in same environment | PKG-001 |

---

## Design notes

### Current state (PKG-001)

Package viewer hub at `/package-viewer` with environment → instance picker. Instance view at `/package-viewer/{instanceId}` — filesystem scan of `ApplicationInstance.PhysicalPath` for `*.dll` (recursive), showing file name, file version, and assembly version. Linked from nav, home, and environment details. Legacy `/packages` and `/environments/.../packages` routes redirect to the canonical paths.

### Target domain

| Concept | Notes |
|---------|--------|
| **Packages hub** | Top-level nav entry **Package viewer** (like Log Viewer and Configuration) |
| **Instance packages** | Deep link from environment details preserved; canonical route `/package-viewer/{instanceId}` with redirect from legacy paths |
| **Manifest** | `manifest.csv` in the deployment root (`PhysicalPath`): quoted CSV `[representative file path],[version]`; header row skipped (PKG-002). Falls back to recursive `*.dll` scan when absent or unusable. |
| **Build artefact** | For a given `ApplicationInstance`, `BuildNumber` may identify the deployment package used to deploy that build — integrate with remote/build APIs where available (ENV-016 / build version details) |
| **Compare** | Side-by-side diff of DLL name → version across two selected targets |

### Comparison modes

| Mode | User selects | Result |
|------|--------------|--------|
| **Same app, two instances** | DeployableApplication + Instance A + Instance B | Grid: DLL → version in A vs B; highlight mismatches |
| **Same environment, two apps** | Environment + App A + App B | Grid: DLL → version in each app; highlight mismatches |

### Relationships

- **ApplicationInstance** — scan target (`PhysicalPath`, `BuildNumber`)
- **DeployableApplication** — groups instances for same-app comparison
- **TrackedEnvironment** — scopes same-environment comparison

### Out of scope (epic v1)

- NuGet gallery / feed browsing
- Downloading or replacing DLLs on the server
- Historical package inventory (multiple past builds per slot)

---

## Tickets

### PKG-001

| Field | Detail |
|-------|--------|
| **ID** | PKG-001 |
| **Title** | Packages as first-class domain (nav and routes) |
| **Status** | Done |
| **Description** | Promote **Packages** from an Environments sub-route to its own domain alongside Log Viewer and Configuration. **Nav:** add top-level **Packages** link in `NavMenu` and a home card on the landing page. **Routes:** introduce a packages hub (environment → instance picker, mirroring Log Viewer / Configuration patterns) and an instance packages view at a domain-centric URL (e.g. `/packages` and `/packages/{instanceId}`). Keep backward compatibility: existing `/environments/{localId}/instances/{instanceId}/packages` should redirect or remain as an alias. Reuse existing `IDeployedPackageService` scan logic from ENV-006. **Out of scope:** manifest, build resolution, compare views. |
| **Test / demo** | Sidebar **Packages** → pick environment and instance → DLL table loads → environment details **Packages** button still works → deep link bookmarkable. |
| **Depends on** | ENV-006 |
| **Implementation** | Hub at `/package-viewer`; instance view at `/package-viewer/{instanceId}`; `PackageViewerContent` shared component; legacy routes redirect with `replace: true`; nav and home card added. Pages live under `Pages/PackageViewer/` (not `Packages/`, which is gitignored). |

### PKG-002

| Field | Detail |
|-------|--------|
| **ID** | PKG-002 |
| **Title** | Consume deployment manifest file when present |
| **Status** | Done |
| **Description** | When inspecting packages for an `ApplicationInstance`, detect and parse **`manifest.csv`** in the deployment root (`PhysicalPath`). Format: quoted CSV columns `[representative file path],[version]`; skip the first (header) line. **Behaviour:** if a valid manifest is found with at least one entry, use it as the authoritative package list; fall back to recursive `*.dll` filesystem scan when manifest is absent, unreadable, or empty. Surface manifest file name and parse warnings in the UI. |
| **Test / demo** | Instance with `manifest.csv` in deploy root → packages table shows manifest entries → instance without manifest → filesystem scan unchanged. `dotnet test --filter Manifest` → pass. |
| **Depends on** | PKG-001 |
| **Implementation** | `DeploymentManifestParser` reads `manifest.csv`; `DeployedPackageScanResult` includes `Source`, `ManifestFileName`, and `Warnings`; packages view shows manifest source and warnings. |

### PKG-003

| Field | Detail |
|-------|--------|
| **ID** | PKG-003 |
| **Title** | Resolve deployment package by build number for instance view |
| **Status** | Open |
| **Description** | When viewing packages for a deployed application instance, use **`ApplicationInstance.BuildNumber`** to retrieve the deployment package artefact where possible (remote API, build storage, or manifest keyed by build). Display which build the package list corresponds to; when build-based resolution succeeds, prefer package contents from that artefact over a blind directory scan. When build number is missing or lookup fails, fall back to `PhysicalPath` scan with a clear UI message. Coordinate with ENV-016 deployment/build details and PKG-002 manifest support. |
| **Test / demo** | Instance with known build number → packages view shows build label and resolved package list → instance without build number → scan fallback with explanatory text. |
| **Depends on** | PKG-001, ENV-016 |

### PKG-004

| Field | Detail |
|-------|--------|
| **ID** | PKG-004 |
| **Title** | Compare DLL versions between two instances of same app |
| **Status** | Done |
| **Description** | Add a **compare** workflow: user picks a **DeployableApplication**, then two **ApplicationInstance** rows (typically different environments). Show a table of DLL / assembly name with version columns for Instance A and Instance B; highlight rows where versions differ or a DLL exists on only one side. Reuse package resolution from PKG-001 (and PKG-002/003 when available). Entry point: Packages hub or deployable application context. |
| **Test / demo** | Pick same app in UAT-01 and Integration → compare → mismatched DLL versions highlighted → identical versions shown neutrally. |
| **Depends on** | PKG-001 |
| **Implementation** | `/package-viewer/compare` picker and `/package-viewer/compare/{leftId}/{rightId}` results; `DeployedPackageComparer`, `CompareInstancesAsync`, `PackageViewerComparisonContent`; hub link on package viewer index. |

### PKG-005

| Field | Detail |
|-------|--------|
| **ID** | PKG-005 |
| **Title** | Compare DLL versions between two apps in same environment |
| **Status** | Open |
| **Description** | Add a **compare** workflow: user picks an **environment**, then two **DeployableApplication** instances deployed in that environment. Show DLL / assembly name with version columns for App A and App B; highlight differences. Useful for verifying shared dependencies (e.g. two web apps on the same server should run the same `Common.dll` version). Reuse package resolution and comparison grid from PKG-004 where possible. |
| **Test / demo** | Pick two apps in UAT-01 → compare → shared DLLs with same version align → version skew highlighted. |
| **Depends on** | PKG-001 |
