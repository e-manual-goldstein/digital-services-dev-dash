# Epic COV — Coverlet Viewer

**Project:** Digital Services Dev Dash
**Code:** `COV`
**Scope:** Upload and explore **Coverlet** code-coverage run output in the browser — parse standard JSON reports, present results in a filterable table, and support in-session bulk actions (e.g. hide rows) without persisting uploads or user choices to disk.

**Depends on:** FND-002
**Blocks:** —

---

## Primary user story

> After a test run I have a Coverlet coverage JSON file. I want to open it in Dev Dash, scan the results in one table, narrow rows with column filters, and temporarily hide noise — without storing the file on the server or in SQLite.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [COV-001](#cov-001) | Done | Coverlet Viewer domain shell (nav and page) | FND-002 |
| [COV-002](#cov-002) | Done | Parse Coverlet JSON upload into in-memory session model | COV-001 |
| [COV-003](#cov-003) | Done | Coverage results table (single view) | COV-002 |
| [COV-004](#cov-004) | Done | Column-header filters on results table | COV-003 |
| [COV-005](#cov-005) | Done | Row selection, batch Hide, and Show hidden toggle | COV-004 |
| [COV-006](#cov-006) | Done | Collapsible **Method results** section | COV-005 |
| [COV-007](#cov-007) | Done | Results table column layout; drop **File** column | COV-006 |
| [COV-008](#cov-008) | Done | **Modules summary** collapsible report | COV-005 |
| [COV-009](#cov-009) | Done | **Class summary** by module (dropdown) | COV-008 |
| [COV-010](#cov-010) | Done | **Export** filtered working set (flat JSON) | COV-005 |

---

## Design notes

### Persistence

| Data | Stored? |
|------|---------|
| Uploaded JSON file | **No** — read once from browser upload, parse, discard file |
| Parsed coverage rows | **In memory only** for the current page session (component/state scoped) |
| Hidden row keys | **In memory only** — reset on navigation away or new upload |
| Column filter values | **In memory only** |

No new SQLite tables for COV v1.

### Input format (v1)

- **Coverlet JSON** — standard **summary** reports (`Classes` / `Methods` / `Summary`) and **legacy** reports (module → file → type → method with `Lines` / `Branches` maps). Parser accepts both in one upload.
- Parser lives in `DigitalDevServices.Services` (or `Model` DTOs + service) and returns a **flat list of row DTOs** suitable for binding (assembly/module, file, class, method, line/branch metrics — exact columns follow whatever the JSON exposes after flattening).
- Invalid or non-JSON files: clear error on the page; no partial table unless parse succeeds.

### UI layout

| Area | Behaviour |
|------|-----------|
| **Hub** | `/coverlet-viewer` — title **Coverlet Viewer**, short description, file upload (`InputFile`), optional summary stats after load |
| **Modules summary** | Collapsible section (default collapsed) — module name, class count, method count, aggregated line % for rows **not hidden** |
| **Class summary** | Collapsible section (default collapsed) — module dropdown (same module list as modules summary), then class / method count / line % per class |
| **Method results** | Main filterable table inside a collapsible section (default expanded) |
| **Table toolbar** | Above table, **right-aligned**: checkbox **Show hidden** — when unchecked, rows the user hid are omitted; when checked, hidden rows appear (visually distinct optional) |
| **Table columns** | Data columns from coverage model + leading **Select** column with checkbox per row for bulk actions |
| **Column filters** | Filter controls live **in the column headers** (e.g. text contains, numeric min/max, or enum where appropriate) — each column that is filterable exposes its control in `<thead>` |
| **Bulk actions** | Toolbar or bar above table when ≥1 row selected; v1 action: **Hide** — marks selected rows hidden (respects **Show hidden** toggle) |
| **Export** | **Export (N)** downloads flat JSON (`*.filtered.json`) — methods matching column filters and **not** hidden; not Coverlet schema (see COV-010) |

### Navigation

- Sidebar: **Coverlet Viewer** (alongside Log Viewer, Configuration viewer, Package viewer).
- Landing page card linking to `/coverlet-viewer`.

### Relationships

- **None** to `TrackedEnvironment` / `ApplicationInstance` in v1 — standalone tool.

### Out of scope (epic v1)

- Saving uploads or session state to SQLite / server disk
- Cobertura XML, OpenCover XML, or HTML report formats
- Multiple tables / drill-down by assembly (single flat table only)
- Export, merge, or diff of two coverage files
- Additional batch actions beyond **Hide** (architecture should allow more actions later)

---

## Tickets

### COV-001

| Field | Detail |
|-------|--------|
| **ID** | COV-001 |
| **Title** | Coverlet Viewer domain shell (nav and page) |
| **Status** | Done |
| **Description** | Introduce the **Coverlet Viewer** domain. **Route:** `/coverlet-viewer`. **Nav:** sidebar entry and optional home card. **Page:** `<PageTitle>` and `<h1>` **Coverlet Viewer**, short help text explaining JSON upload (parsing in COV-002). Placeholder upload area or disabled **Choose file** until COV-002. No SQLite. |
| **Test / demo** | Sidebar **Coverlet Viewer** → page loads → bookmark `/coverlet-viewer` works. |
| **Depends on** | FND-002 |
| **Implementation** | `/coverlet-viewer` hub in `Pages/CoverletViewer/Index.razor`; disabled `InputFile`; nav and home card. |

### COV-002

| Field | Detail |
|-------|--------|
| **ID** | COV-002 |
| **Title** | Parse Coverlet JSON upload into in-memory session model |
| **Status** | Done |
| **Description** | Wire **InputFile** on `/coverlet-viewer` to read uploaded JSON in the browser (or stream to server endpoint that returns parsed DTOs only — **do not** write the file to disk). Implement `ICoverletCoverageReportParser` (or equivalent) that deserializes standard Coverlet JSON and produces `CoverletCoverageRow` (or similar) read-only list + optional run summary (totals, line rate). Hold parsed result in page/component state until user uploads a new file or leaves the page. Show validation errors for empty file, invalid JSON, or unrecognized schema. |
| **Test / demo** | Upload a real `coverage.json` from a local Coverlet run → summary/count appears → no new rows in SQLite → refresh page clears state. `dotnet test --filter Coverlet` (parser unit tests with sample JSON under `samples/coverlet/`). |
| **Depends on** | COV-001 |
| **Implementation** | `ICoverletCoverageReportParser`, `CoverletCoverageReportParser`; in-memory state on `Index.razor`; `samples/coverlet/coverage.sample.json`; hub message size for large uploads. |

### COV-003

| Field | Detail |
|-------|--------|
| **ID** | COV-003 |
| **Title** | Coverage results table (single view) |
| **Status** | Done |
| **Description** | After successful parse (COV-002), render a single **sortable** (optional v1: default order from parser) table of all coverage rows. Columns reflect flattened model (e.g. assembly, file, class, method, line coverage %, covered/total lines — align to parser output). Include a **Select** column with checkbox per row (selection wiring completed in COV-005; column can be present but bulk bar optional until COV-005). Empty state when no file loaded. |
| **Test / demo** | Upload sample JSON → table populated → row count matches parser output. |
| **Depends on** | COV-002 |
| **Implementation** | `CoverletCoverageResultsTable` component; module/class/method and coverage metrics columns; per-row select checkboxes with session selection state on hub page. |

### COV-004

| Field | Detail |
|-------|--------|
| **ID** | COV-004 |
| **Title** | Column-header filters on results table |
| **Status** | Done |
| **Description** | Add **per-column filters in the table header** for the coverage results table. Each filterable column exposes its control in the header row (text filter for names/paths, numeric threshold for percentages/counts where sensible). Filters combine (AND). Updating a filter immediately narrows visible rows. Filters apply before hide logic (COV-005). Clear-all or per-column reset is acceptable. |
| **Test / demo** | Type in **File** header filter → only matching rows shown → combine with **Class** filter → intersection applied. |
| **Depends on** | COV-003 |
| **Implementation** | `CoverletCoverageFilterState`, `CoverletCoverageRowFilter`; filter inputs in table header row; **Clear filters** button; showing count. |

### COV-005

| Field | Detail |
|-------|--------|
| **ID** | COV-005 |
| **Title** | Row selection, batch Hide, and Show hidden toggle |
| **Status** | Done |
| **Description** | **Selection:** checkboxes in the leading column; header checkbox selects all **currently visible** rows (after filters). **Bulk actions:** when one or more rows selected, show action bar with **Hide** — selected rows are marked hidden in session state and disappear from the table unless **Show hidden** is on. **Show hidden:** checkbox above the table on the **right** — when enabled, include hidden rows in the table (styled or badged as hidden). Hidden state is in-memory only; new upload clears hidden set. Extensible pattern for future batch actions (enum or command list). |
| **Test / demo** | Select several rows → **Hide** → rows disappear → enable **Show hidden** → rows reappear → upload new file → hidden state cleared. |
| **Depends on** | COV-004 |
| **Implementation** | `CoverletCoverageRowVisibility`; header select-all-visible; **Hide** bulk action; **Show hidden** toggle; hidden row styling; state cleared on new upload. |

### COV-006

| Field | Detail |
|-------|--------|
| **ID** | COV-006 |
| **Title** | Collapsible method results section |
| **Status** | Done |
| **Description** | Wrap the main coverage results table in `CollapsibleSection` (default expanded). Title includes parsed method row count. |
| **Test / demo** | Upload JSON → expand/collapse **Method results** → table and filters remain functional. |
| **Depends on** | COV-005 |
| **Implementation** | `Index.razor` — `CollapsibleSection` around `CoverletCoverageResultsTable`. |

### COV-007

| Field | Detail |
|-------|--------|
| **ID** | COV-007 |
| **Title** | Results table column layout (remove File column) |
| **Status** | Done |
| **Description** | Fix column widths with `table-layout: fixed` and `colgroup`. Remove **File** column and file header filter from the method table (source file still parsed for legacy JSON). Truncate long module/class/method cells with `title` tooltip. |
| **Test / demo** | Wide assembly names → columns stay aligned → hover shows full name. |
| **Depends on** | COV-006 |
| **Implementation** | `site.css` coverlet table styles; `CoverletCoverageResultsTable.razor`; `CoverletCoverageFilterState` without file filter. |

### COV-008

| Field | Detail |
|-------|--------|
| **ID** | COV-008 |
| **Title** | Modules summary report |
| **Status** | Done |
| **Description** | Collapsible **Modules summary** (default collapsed). Table: module name, number of classes, number of methods, aggregated line coverage %. Counts and percentages use method rows that are **not hidden** (ignore **Show hidden** on the main table). |
| **Test / demo** | Hide methods in a module → modules summary updates → expand section → line % reflects visible rows only. |
| **Depends on** | COV-005 |
| **Implementation** | `CoverletCoverageSummaryBuilder`, `CoverletModuleSummaryRow`, `CoverletCoverageModulesSummaryTable`. |

### COV-009

| Field | Detail |
|-------|--------|
| **ID** | COV-009 |
| **Title** | Class summary by module |
| **Status** | Done |
| **Description** | Collapsible **Class summary** (default collapsed). Module `<select>` options match modules summary. Table: class name, method count, line coverage % for non-hidden rows in the selected module. |
| **Test / demo** | Change module dropdown → class rows update → hide a method → class and module summaries shrink accordingly. |
| **Depends on** | COV-008 |
| **Implementation** | `CoverletClassSummaryRow`, `CoverletCoverageClassSummaryTable`. |

### COV-010

| Field | Detail |
|-------|--------|
| **ID** | COV-010 |
| **Title** | Export filtered working set (flat JSON) |
| **Status** | Done |
| **Description** | After narrowing a large upload with column filters and **Hide**, **Export** downloads a compact JSON file for downstream automation (not a Coverlet round-trip). Export includes rows that pass current column filters and are **not** in the hidden set (independent of **Show hidden**). Document shape in `samples/coverlet/coverage.export.sample.json`. |
| **Test / demo** | Upload large JSON → filter → hide rows → **Export (N)** → browser saves `&lt;original&gt;.filtered.json` with `rowCount` and `rows[]`. |
| **Depends on** | COV-005 |
| **Implementation** | `CoverletCoverageExportBuilder`, `CoverletCoverageExportDocument`; `devDashDownload.downloadText` in `wwwroot/js/download.js`. |
