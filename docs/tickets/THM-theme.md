# Epic THM — Theme & Visual Design

**Project:** Digital Services Dev Dash
**Code:** `THM`
**Scope:** Establish a project-wide colour scheme and visual language so the dashboard does not rely on default Bootstrap blue styling. Apply consistently across the landing page, navigation, buttons, links, and badges.

**Depends on:** FND-002
**Blocks:** —

---

## Primary user story

> The app still looks like a stock Bootstrap template — primary buttons are bright blue everywhere. I want a deliberate, cohesive colour palette that fits a professional internal dashboard and can be extended as new features ship.

---

## Ticket summary

| ID | Status | Title | Depends on |
|----|--------|-------|------------|
| [THM-001](#thm-001) | Open | Global colour scheme and non-blue primary actions | FND-002 |

---

## Design notes

### Approach

- Define design tokens as CSS custom properties in `site.css` (or a dedicated `theme.css`) — primary, secondary, accent, surface, border, text, success/warning/danger variants.
- Override Bootstrap CSS variables (`--bs-primary`, `--bs-link-color`, etc.) where appropriate so `btn-primary`, nav links, and form focus rings inherit the palette without per-page hacks.
- **Landing page** (`AdminHome` / home cards): action buttons must use the new scheme — not default blue.
- Document token names and intended usage in this epic's design notes (no separate design doc unless needed).
- Accessibility: maintain sufficient contrast for text on buttons and badges (WCAG AA target for body text and interactive controls).

### Out of scope (epic v1)

- Dark mode toggle
- Per-user theme preferences
- Radzen component theming beyond what Bootstrap overrides cover
- Custom icon set or typography overhaul

---

## Tickets

### THM-001

| Field | Detail |
|-------|--------|
| **ID** | THM-001 |
| **Title** | Global colour scheme and non-blue primary actions |
| **Status** | Open |
| **Description** | Introduce a **global colour scheme** for Digital Services Dev Dash. Replace the default Bootstrap blue primary button styling site-wide with a defined palette (CSS variables + Bootstrap overrides). **Landing page:** home / admin cards and their action buttons must use the new primary/secondary styles — buttons should not appear as default blue. **Scope:** shared styles in `wwwroot/css/site.css` (or extracted theme file referenced from `_Host.cshtml`); update high-traffic surfaces: landing page, nav menu active links, primary CTAs on Log Viewer / Configuration pickers, environment details actions, and modal primary buttons. **Out of scope:** redesigning layout or component structure; dark mode. **Deliverable:** documented token list in this ticket or design notes; visual pass on landing page confirms non-blue buttons. |
| **Test / demo** | Run DevDash → landing page buttons are not default Bootstrap blue → navigate to Environments, Log viewer picker, Configuration picker → primary buttons match palette → inspect CSS variables in dev tools. |
| **Depends on** | FND-002 |
