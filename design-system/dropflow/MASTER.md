# Design System Master File

> **LOGIC:** When building a specific page, first check `design-system/pages/[page-name].md`.
> If that file exists, its rules **override** this Master file.
> If not, strictly follow the rules below.

---

**Project:** DropFlow
**Generated:** 2026-07-05 20:04:27 (revised by hand — see note below)
**Category:** Logistics/Delivery (multi-tenant SaaS admin dashboard, not a marketing site)

> **Revision note:** the auto-search's initial "Style" and "Page Pattern" sections keyed off
> the word "workflow" and returned an Event/Conference landing-page pattern with
> Exaggerated-Minimalism styling (giant clamp() display type, "speaker grids", countdown CTAs).
> That's wrong for an authenticated dashboard app — those sections have been replaced below
> with the actual best match (`Data-Dense Dashboard` style, confirmed via `--domain style`
> and `--domain product` re-queries). Color palette, spacing, and shadow scale from the
> original run were correct and are kept as-is.

---

## Global Rules

### Color Palette

DropFlow already ships shadcn/ui default tokens (oklch, blue hue ~248) in
`src/index.css`. The palette below is the Logistics/Delivery recommendation —
**verified to already match** the app's current primary blue closely enough that
`--primary` does not need to change. Treat this table as the documented source of
truth for that existing color, not a repaint.

| Role | Hex | CSS Variable |
|------|-----|--------------|
| Primary | `#2563EB` | `--color-primary` (≈ current `--primary: oklch(0.59 0.21 248)` — keep) |
| On Primary | `#FFFFFF` | `--color-on-primary` |
| Secondary | `#3B82F6` | `--color-secondary` |
| Background | `#EFF6FF` (light) | `--color-background` |
| Foreground | `#1E40AF` | `--color-foreground` |
| Muted | `#E9EFF8` | `--color-muted` |
| Border | `#BFDBFE` | `--color-border` |
| Destructive | `#DC2626` | `--color-destructive` (already matches) |
| Ring | `#2563EB` | `--color-ring` |

**⚠️ `#EA580C` orange is NOT the shadcn `--accent` token.** In shadcn's convention,
`--accent`/`--accent-foreground` is a *neutral* hover/highlight surface (dropdown item
hover, calendar day hover, etc.) — currently a near-white muted gray. Overloading it with
saturated orange would break every component that leans on that neutral hover semantic.
Instead, introduce orange as a **new, separate semantic token** for delivery-domain meaning
only:

```css
/* add alongside existing tokens in :root / .dark */
--color-urgent: oklch(0.65 0.19 45);        /* ≈ #EA580C, WCAG-checked vs both surfaces */
--color-urgent-foreground: oklch(0.99 0 0);
```

Use `--color-urgent` for: urgent-delivery badges, "en retard" states, tracking/in-transit
accents. Do not use it as a general CTA color — the app's primary blue stays the CTA color
(buttons, links, active nav) to avoid competing accents.

**Zone accent system (recommendation — keep, formalize):** DropFlow's existing per-area
gradient headers are a legitimate and common SaaS pattern (distinguishing operator context
at a glance, e.g. Stripe/Linear-style privilege coloring), not visual noise. Formalize
rather than unify:

| Zone | Gradient | Meaning |
|------|----------|---------|
| Standard (Manager/Driver) | `from-sky-500 to-blue-600` | Primary brand — default for all tenant-facing pages |
| Super-Admin | `from-violet-600 to-indigo-700` | Signals elevated, cross-tenant privilege — deliberately distinct so no one mistakes admin screens for tenant screens |
| Settings hub | `from-slate-700 to-slate-900` | Neutral/utility — settings isn't "branded", it's configuration |

Keep these three exactly as-is. Do not introduce a fourth accent without a reason as strong
as "cross-tenant privilege."

### Typography

Current state: **no font is loaded** — `body { font-sans }` falls back to Tailwind's
default system-UI stack (`ui-sans-serif, system-ui, ...`). This is the single highest-
leverage typography fix available: a deliberate typeface reads as "designed"; the system
default reads as "unstyled default," which undercuts the trust goal directly.

- **Font:** Plus Jakarta Sans (single family, headings + body via weight, not two fonts)
- **Mood:** friendly, modern, professional, B2B SaaS, dashboards — modern alternative to Inter
- **Why not Fira Code/Fira Sans** (the initial auto-match): that pairing leans "developer
  tool / technical console." DropFlow is an operations dashboard used by non-technical
  managers/drivers/accountants — Plus Jakarta Sans keeps data-dense screens legible while
  reading as approachable rather than "engineering internal tool."
- **Google Fonts:** https://fonts.google.com/share?selection.family=Plus+Jakarta+Sans:wght@300;400;500;600;700

**CSS Import (add to top of `src/index.css`):**
```css
@import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;500;600;700&display=swap');
```

**Register in `@theme inline` block:**
```css
--font-sans: 'Plus Jakarta Sans', ui-sans-serif, system-ui, sans-serif;
```

**Weight scale:** 700–800 for page/section headings (hero titles), 600 for card titles/
button labels, 500 for nav items/labels, 400 for body text.

### Spacing Variables

| Token | Value | Usage |
|-------|-------|-------|
| `--space-xs` | `4px` / `0.25rem` | Tight gaps |
| `--space-sm` | `8px` / `0.5rem` | Icon gaps, inline spacing |
| `--space-md` | `16px` / `1rem` | Standard padding |
| `--space-lg` | `24px` / `1.5rem` | Section padding |
| `--space-xl` | `32px` / `2rem` | Large gaps |
| `--space-2xl` | `48px` / `3rem` | Section margins |
| `--space-3xl` | `64px` / `4rem` | Hero padding |

### Shadow Depths

| Level | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Subtle lift |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Cards, buttons |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Modals, dropdowns |
| `--shadow-xl` | `0 20px 25px rgba(0,0,0,0.15)` | Hero images, featured cards |

---

## Component Specs

### Buttons

Primary CTA stays brand blue (shadcn `<Button>` default variant) — orange is reserved for
the urgent/tracking semantic only, never for general buttons.

```css
/* Primary Button — maps to shadcn <Button> default variant, already uses --primary */
.btn-primary {
  background: var(--color-primary);
  color: var(--color-primary-foreground);
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 200ms ease;
  cursor: pointer;
}

.btn-primary:hover {
  opacity: 0.9;
  transform: translateY(-1px);
}

/* Secondary Button */
.btn-secondary {
  background: transparent;
  color: var(--color-primary);
  border: 2px solid var(--color-primary);
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 200ms ease;
  cursor: pointer;
}
```

### Cards

DropFlow's existing `rounded-2xl border shadow-sm` + `hover:-translate-y-1 hover:shadow-lg`
convention (seen in `DashboardPage.tsx`, `TeamPage.tsx`) is correct for the Data-Dense
Dashboard style below — formalize it, don't replace it:

```css
.card {
  background: var(--color-card);
  border: 1px solid var(--color-border);
  border-radius: 16px; /* rounded-2xl */
  padding: 24px;
  box-shadow: var(--shadow-sm);
  transition: all 200ms ease;
}

.card:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-4px); /* -translate-y-1 */
}
```

### Inputs

```css
.input {
  padding: 12px 16px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 16px;
  transition: border-color 200ms ease;
}

.input:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px color-mix(in oklch, var(--color-primary) 20%, transparent);
}
```

### Status & Semantic Colors

Delivery/route status already drives real meaning in the UI (kanban, badges, timelines) —
define these once as tokens instead of ad-hoc Tailwind classes scattered per component:

| Meaning | Token | Tailwind equivalent | Used for |
|---------|-------|---------------------|----------|
| Success / delivered / completed | `--color-success` | `emerald-500`/`emerald-600` | `DeliveryStatus.Delivered`, `RouteStatus.Completed` |
| Warning / in progress / pending | `--color-warning` | `amber-500` | `InProgress`, `Draft` awaiting action |
| Urgent / attention | `--color-urgent` | `orange-600` (see above) | Urgent delivery type, "en retard" |
| Danger / cancelled / failed | `--color-destructive` (existing) | `red-600` (already defined) | Cancelled, failed delivery, destructive actions |
| Neutral / unassigned | `--color-muted-foreground` (existing) | `slate-500` | Unassigned, no-status |

Per `color-not-only` (a11y rule): every status badge pairs its color with an icon or label —
never color alone. StatusBadge already does this; keep it as the canonical pattern for any
new status-like indicator.

### Icons — no emoji as content

Flagged in the current codebase and must be replaced with Lucide icons:

| Current | Location | Replace with |
|---------|----------|---------------|
| 👋 | Dashboard/driver greeting | `Hand` or drop it — a text greeting doesn't need an icon |
| 🥇🥈🥉 | Dashboard leaderboard / store ranking | `Trophy`/`Medal` (Lucide) recolored gold/silver/bronze via `text-amber-500` / `text-slate-400` / `text-amber-700`, or a numbered rank badge — not a raster medal glyph |

Lucide is already the icon set for the rest of the app — these are the only two known
exceptions.

### Modals

```css
.modal-overlay {
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
}

.modal {
  background: white;
  border-radius: 16px;
  padding: 32px;
  box-shadow: var(--shadow-xl);
  max-width: 500px;
  width: 90%;
}
```

### Plain `<button>` elements

The codebase has a documented, intentional exception in `frontend/DropFlow.Web/CLAUDE.md`:
the hero action pill (`bg-white/15 px-3 py-1.5 text-xs font-semibold text-white
hover:bg-white/25 rounded-xl`) deliberately uses a plain `<button>` instead of shadcn
`<Button>` to match the glass hero style. **Keep that exception** — but every plain
`<button>` anywhere in the app, including that one, must still get:

```css
button:focus-visible {
  outline: 2px solid var(--color-ring);
  outline-offset: 2px;
}
```

During the audit (Step 3), any plain `<button>` found *outside* the documented hero-pill
exception is a deviation to flag and convert to shadcn `<Button variant="ghost|link">` —
don't assume every plain button is intentional.

---

## Style Guidelines

**Style:** Data-Dense Dashboard (primary) + Minimalism/Flat Design (secondary, from the
Logistics/Delivery product-type recommendation)

**Keywords:** multiple charts/widgets, data tables, KPI cards, minimal padding, grid layout,
space-efficient, maximum data visibility, hover tooltips, row highlighting, smooth filter
animations

**Best For:** operational dashboards, admin panels, enterprise reporting — exactly DropFlow's
shape (KPI row + swim lanes + tables + wizards), confirmed via `--domain style` search.

**Key Effects:** 12-column grid, 8–12px component gaps, dense-but-readable type (12–14px for
table/meta text, 16px body), sticky table headers, compact card padding (16–24px), hover
tooltips on charts, row highlight on hover.

**Framework fit:** Recharts (already in stack) scores 9/10 for this style — no chart library
change needed.

### Page/App Pattern

Not a marketing landing page — this is an authenticated app shell. Pattern:

- **Shell:** persistent sidebar (nav, role-scoped items) + topbar (search/notifications/user
  menu) — `AppLayout.tsx`, already correct structurally, restyle only.
- **Content pattern per page type:**
  - **Dashboards** (Manager/Admin): KPI card row → charts/swim-lanes → recent-activity table.
  - **List pages** (Deliveries/Routes/Clients/Settings sub-pages): PageHeader (title + primary
    CTA) → filter bar → data table/cards → pagination.
  - **Wizards** (Route creation): step indicator → single-step form → sticky footer nav
    (Précédent/Suivant), matching the "multi-step state in Zustand, not component state"
    architecture already in place.
  - **Detail pages** (Delivery/Route/Client/Tenant detail): hero summary block → tabbed or
    sectioned content.
- **CTA placement:** one primary action per page, top-right of `PageHeader`; secondary actions
  as outline/ghost buttons beside it; destructive actions separated (own button group or
  overflow menu), never adjacent to primary CTA.

---

## Anti-Patterns (Do NOT Use)

- ❌ Oversized display type / clamp() hero headlines — this is a dashboard, not a landing page
- ❌ Countdown timers, "early bird" urgency patterns, testimonial/social-proof blocks — not applicable to an internal tool
- ❌ AI purple/pink decorative gradients (the violet/indigo admin gradient is a *semantic* zone marker, not decoration — keep it, but don't spread it elsewhere)
- ❌ Introducing a 4th brand color beyond primary blue / urgent orange / semantic status colors

### Additional Forbidden Patterns

- ❌ **Emojis as icons** — Use SVG icons (Heroicons, Lucide, Simple Icons)
- ❌ **Missing cursor:pointer** — All clickable elements must have cursor:pointer
- ❌ **Layout-shifting hovers** — Avoid scale transforms that shift layout
- ❌ **Low contrast text** — Maintain 4.5:1 minimum contrast ratio
- ❌ **Instant state changes** — Always use transitions (150-300ms)
- ❌ **Invisible focus states** — Focus states must be visible for a11y

---

## Pre-Delivery Checklist

Before delivering any UI code, verify:

- [ ] No emojis used as icons (use SVG instead)
- [ ] All icons from consistent icon set (Heroicons/Lucide)
- [ ] `cursor-pointer` on all clickable elements
- [ ] Hover states with smooth transitions (150-300ms)
- [ ] Light mode: text contrast 4.5:1 minimum
- [ ] Focus states visible for keyboard navigation
- [ ] `prefers-reduced-motion` respected
- [ ] Responsive: 375px, 768px, 1024px, 1440px
- [ ] No content hidden behind fixed navbars
- [ ] No horizontal scroll on mobile
