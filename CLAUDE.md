# CLAUDE.md — DropFlow Project Context for Claude Code

This file is read automatically by Claude Code at the start of every session.
It provides persistent context about the project so you don't need to re-explain the architecture.

---

## Project: DropFlow

Multi-tenant SaaS delivery management platform for logistics companies.

**Stack:**
- .NET Core 9 / ASP.NET Core (REST API)
- React 18 + TypeScript frontend — Vite, Tailwind CSS v4, shadcn/ui (see `frontend/DropFlow.Web/CLAUDE.md`)
- Entity Framework Core (PostgreSQL via Neon)
- JWT Auth + LocalStorage
- Google Maps API

---

## Skills Available (.claude/skills/)

Claude Code has access to the following skills, located in `.claude/skills/`.
Each skill encodes production-grade .NET patterns. Read the relevant SKILL.md
**before** writing code for any task that matches its trigger.

| Skill | Trigger / When to consult |
|-------|---------------------------|
| `modern-csharp-coding-standards` | Writing or refactoring C# code — records, pattern matching, nullable types, value objects |
| `efcore-patterns` | Adding entities, configurations, migrations, query optimization |
| `database-performance` | Slow queries, N+1 issues, AsNoTracking, read/write separation, indexing |
| `csharp-concurrency-patterns` | Threading, async/await, race conditions, locks, channels |
| `dependency-injection-patterns` | DI registration, scope management, keyed services, IServiceCollection extensions |
| `microsoft-extensions-configuration` | IOptions pattern, appsettings, secrets, environment-specific config |
| `testcontainers-integration-tests` | Integration tests with Docker (PostgreSQL, Redis, etc.) |
| `playwright-blazor-testing` | E2E UI tests with Playwright — page objects, async assertions (applies to the React app too) |
| `dotnet-project-structure` | Solution layout, Directory.Build.props, layered architecture decisions |

### Skills Routing — Quick Reference

- **New entity / table / migration** → `efcore-patterns` + multi-tenant rules below
- **List query is slow / pagination** → `database-performance`
- **Race condition (e.g. reference number generation)** → `csharp-concurrency-patterns`
- **New service registration in DI** → `dependency-injection-patterns`
- **New appsettings key or secret** → `microsoft-extensions-configuration`
- **Writing C# code in general** → `modern-csharp-coding-standards`
- **Integration test against DB** → `testcontainers-integration-tests` (PostgreSQL container)
- **E2E test of a frontend page** → `playwright-blazor-testing`

---

## Skills Overrides — DropFlow Conventions Take Precedence

The generic .NET skills come from a third-party library. Where they conflict
with DropFlow's established architecture, **DropFlow rules win**:

| Generic skill says | DropFlow rule (override) |
|--------------------|--------------------------|
| (Frontend conventions) | Frontend is **React** (`frontend/DropFlow.Web`) — API calls live in per-domain modules under `src/api/`; follow that app's own `CLAUDE.md` |
| "No AutoMapper" | ✅ Aligned — DropFlow does not use AutoMapper either |
| (No mention of multi-tenancy) | **Always apply DropFlow multi-tenant rules** (see section below) |
| (No mention of MediatR) | **Do NOT use MediatR / CQRS** — DropFlow uses direct services |
| (No mention of i18n) | All UI labels in **French**; entity properties and code in English |

When a skill suggests a pattern that conflicts with DropFlow conventions,
note the conflict explicitly in your reasoning, then follow DropFlow.

---

## Architecture Principles

**DO follow these patterns:**
- Services are injected via interfaces, registered as Scoped in DI
- All API responses use `ResponseResult<T>` wrapper
- Multi-step wizards hold state in a centralized state object (not component-level)
- EF queries on lists always use `.AsNoTracking()` + server-side pagination
- **Frontend is React** (`frontend/DropFlow.Web`) — follow that app's `CLAUDE.md` for frontend conventions (per-domain `src/api/` modules, TanStack Query, Zustand, React Hook Form + Zod, shadcn/ui, French UI labels)

**DO NOT use:**
- MediatR or CQRS pattern (decision was made to use direct services)
- Raw exceptions thrown to the UI (use ResponseResult error handling)
- Hardcoded tenant IDs or user IDs anywhere
- Blocking `.Result` or `.Wait()` on async calls

---

## Multi-Tenant Rules

Every entity that belongs to a tenant implements `ITenantEntity`:
```csharp
public interface ITenantEntity
{
    int TenantId { get; set; }
}
```

EF Core global query filters are applied in `ApplicationDbContext.OnModelCreating`.
`TenantId` is automatically set on `SaveChanges` via the `ApplyTenantId()` method.

**Critical rules:**
- **Never accept TenantId from client input.** Always resolve from authenticated claims.
- **Never disable global query filters** without explicit `TenantId` validation in the query.
- **Super Admin uses TenantId = 0** — there is no FK between `ApplicationUser` and `Tenant` to support this. Don't try to "fix" this with a foreign key.
- **All new entities with tenant scope MUST implement `ITenantEntity`.**
- When writing a new query, verify in your mind: "Could this leak data across tenants?" If yes, stop and reconsider.

---

## PostgreSQL Specifics

DropFlow uses PostgreSQL (hosted on Neon). Keep these in mind:

- **Provider**: `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Identifier casing**: PostgreSQL folds unquoted identifiers to lowercase. EF Core configurations should rely on snake_case or explicit `[Table("...")]` / `[Column("...")]` if mixing with raw SQL.
- **Decimal precision**: always specify `HasPrecision(18, 2)` on monetary fields — PostgreSQL is stricter than SQL Server about `decimal`/`numeric` precision.
- **DateTime**: prefer `DateTime` with `Kind = Utc`. Npgsql v6+ throws on `Unspecified` kind by default.
- **Full-text search**: use PostgreSQL's `tsvector` / `to_tsquery` via `EF.Functions.ToTsVector()` rather than `EF.Functions.Contains()` (SQL Server-only).
- **Migrations on Neon**: Neon uses branched databases — test migrations on a branch before applying to main.
- **Connection pooling**: Neon recommends the pooled connection string (`-pooler` suffix) for serverless workloads.

---

## Role Hierarchy

| Role | Access Level |
|------|-------------|
| `Admin` | Full platform access for their tenant |
| `Manager` | Deliveries, clients, invoices, routes, reports |
| `Driver` | Only their assigned deliveries (read + status update) |
| `Accountant` | Invoices, payments, reports only |
| `ReadOnly` | View-only access |

---

## Solution Structure

```
DropFlow/
├── backend/
│   ├── DropFlow.Domain/          # Entities, AuditSeverity enum, interfaces
│   ├── DropFlow.Application/     # Services, validators, business logic
│   ├── DropFlow.Infrastructure/  # EF Core, repositories, external services
│   └── DropFlow.Api/             # ASP.NET Core Web API entry point
├── frontend/
│   └── DropFlow.Web/             # React + TypeScript (Vite, Tailwind, shadcn/ui)
├── mobile/
│   └── DropFlow.Mobile/          # Driver mobile app (MAUI)
├── shared/
│   └── DropFlow.Shared/          # DTOs + shared enums (no dependencies)
├── .claude/
│   └── skills/                   # .NET skills library (see Skills section)
└── DropFlow.sln
```

### Dependency Graph

```
shared/DropFlow.Shared        (no dependencies)
  ← backend/DropFlow.Domain   → Shared
    ← DropFlow.Application    → Domain + Shared
      ← DropFlow.Infrastructure → Application + Domain
        ← DropFlow.Api        → Infrastructure + Shared
frontend/DropFlow.Web         → consumes REST API over HTTP (own TS types; does NOT reference the .NET Shared project)
mobile/DropFlow.Mobile        → Shared only
```

### Shared Enums (in DropFlow.Shared.Enums)
`DeliveryStatus`, `DeliveryType`, `RouteStatus`, `TeamMemberRole`
`AuditSeverity` stays in `DropFlow.Domain.Enums` (backend-only).

---

## Key File Locations

| What | Where |
|------|-------|
| DbContext | `backend/DropFlow.Infrastructure/Data/ApplicationDbContext.cs` |
| Tenant service | `backend/DropFlow.Infrastructure/Services/TenantService.cs` |
| API entry point | `backend/DropFlow.Api/Program.cs` |
| Domain entities | `backend/DropFlow.Domain/Entities/` |
| Application services | `backend/DropFlow.Application/Services/` |
| Shared DTOs | `shared/DropFlow.Shared/` |
| Shared enums | `shared/DropFlow.Shared/Enums/` |
| Frontend API layer | `frontend/DropFlow.Web/src/api/` |
| Frontend pages | `frontend/DropFlow.Web/src/features/` |
| Mobile app | `mobile/DropFlow.Mobile/` |
| Skills library | `.claude/skills/` |

---

## Current Implementation Status

### ✅ Fully Implemented
- Multi-tenant authentication (JWT + Identity)
- Role-based authorization
- Admin platform management
- Client management (CRUD + multi-address)
- Delivery management (CRUD + status tracking + kanban)
- Route creation wizard with Google Maps integration
- Depot selection and delivery assignment to routes
- Audit logging

### 🔄 Partially Implemented / In Progress
- Route optimization (wizard exists, optimization algorithm partial)
- Cross-tab change detection for route invalidation
- Dashboard KPI stats

### ❌ Not Yet Implemented
- PWA mobile driver application
- Invoice PDF generation (QuestPDF)
- Invoice email sending
- Stock / inventory module
- Advanced reporting & analytics
- SMS notifications (Twilio)

---

## Known Issues (from code review)

> Update this section as fixes are applied.

- [ ] Reference number generation (DL-YYYYMMDD-NNN) may have race condition under concurrent load
      → consult `csharp-concurrency-patterns` when fixing
- [ ] Some components may be missing IDisposable for EventBus subscriptions
- [ ] Dashboard stats may use multiple queries instead of single optimized query
      → consult `database-performance` when fixing

---

## Route / Tournée Module — Specific Gotchas

The route module has accumulated some non-obvious rules from past debugging:

- **Google Directions API**: use `optimize:true` for the *initial* optimization; use `optimize:false` when recalculating after manual reordering (otherwise you lose user-chosen order).
- **Single-delivery routes**: require a separate `GetDirectionsAsync` call path. Without it, Google Maps isn't called and metrics return zero.
- **Driver availability filter**: exclude drivers only from `Confirmed`, `InProgress`, `Completed` routes — **not from `Draft`**. Drivers must remain selectable while editing draft routes.
- **Delivery availability filter**: include deliveries that are either unassigned OR belong to a `Draft` route, to support both creation and editing.
- **Cross-tab change detection**: the React app uses a `BroadcastChannel` (`dropflow_deliveries`) via the `useDeliveryBroadcast` hook to invalidate delivery caches across browser tabs.

---

## Code Generation Rules

When I ask you to generate new code for DropFlow, always:

1. **Read the relevant skill first** — for any task matching a skill trigger above, consult that SKILL.md before writing code
2. **Follow existing patterns** — look at a similar existing service/component before writing a new one
3. **Use the correct layer** — business logic in Application services, not in UI components
4. **Include tenant isolation** — new entities must implement `ITenantEntity`, new services must respect tenant scope
5. **Use pagination** — any new list query must support `PageNumber` + `PageSize` parameters
6. **Add validation** — new DTOs must have DataAnnotations or FluentValidation rules
7. **Handle errors** — return `ResponseResult<T>`, catch and log exceptions, never swallow silently
8. **Audit critical actions** — call `IAuditService` for create / update / delete on sensitive entities
9. **Write tests** — at minimum, one test verifying tenant isolation for new entities

---

## Communication Style — User Preference

- Deliver **production-ready code** — minimize explanations unless asked
- Match existing patterns exactly; do not introduce alternative architectures
- Backend first, then frontend; specs before implementation for non-trivial features
- Fix root causes, not symptoms ("stop bricolaging")
- Multi-file solutions include a step-by-step installation guide and testing scenarios

---

## Commit Message Convention

```
[Module] Action: short description

Examples:
[Auth] Fix: JWT token expiry not validated on refresh
[Deliveries] Add: bulk status update endpoint
[Routes] Fix: cross-tab invalidation EventBus memory leak
[Invoices] Add: PDF generation with QuestPDF
```
