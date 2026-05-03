# CLAUDE.md — DropFlow Project Context for Claude Code

This file is read automatically by Claude Code at the start of every session.
It provides persistent context about the project so you don't need to re-explain the architecture.

---

## Project: DropFlow

Multi-tenant SaaS delivery management platform for logistics companies.

**Stack:**
- .NET Core 9 / ASP.NET Core
- Blazor Server + MudBlazor
- Entity Framework Core (SQL Server)
- JWT Auth + LocalStorage
- Google Maps API

---

## Architecture Principles

**DO follow these patterns:**
- Services are injected via interfaces, registered as Scoped in DI
- Frontend Blazor services inherit from `BaseApiService`
- All API responses use `ResponseResult<T>` wrapper
- User feedback uses MudBlazor `ISnackbar` service
- Multi-step wizards hold state in a centralized state object (not component-level)
- EF queries on lists always use `.AsNoTracking()` + server-side pagination
- JS interop only in `OnAfterRenderAsync(firstRender)`, never in `OnInitializedAsync`
- Components that subscribe to EventBus implement `IDisposable`

**DO NOT use:**
- MediatR or CQRS pattern (decision was made to use direct services)
- Raw exceptions thrown to the UI (use ResponseResult error handling)
- Hardcoded tenant IDs or user IDs anywhere
- Blocking `.Result` or `.Wait()` on async calls in Blazor

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
**Never accept TenantId from client input.** Always resolve from authenticated claims.

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

## Key File Locations

| What | Where |
|------|-------|
| DbContext | `src/DropFlow.Infrastructure/Data/ApplicationDbContext.cs` |
| Tenant service | `src/DropFlow.Infrastructure/Services/TenantService.cs` |
| Auth config | `src/DropFlow.Web/Program.cs` |
| Base API service | `src/DropFlow.Web/Services/BaseApiService.cs` |
| Blazor pages | `src/DropFlow.Web/Pages/` |
| Domain entities | `src/DropFlow.Domain/Entities/` |
| DTOs | `src/DropFlow.Application/DTOs/` |

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
- [ ] Some components may be missing IDisposable for EventBus subscriptions
- [ ] Dashboard stats may use multiple queries instead of single optimized query

---

## Code Generation Rules

When I ask you to generate new code for DropFlow, always:

1. **Follow existing patterns** — look at a similar existing service/component before writing a new one
2. **Use the correct layer** — business logic in Application services, not in Blazor components
3. **Include tenant isolation** — new entities must implement `ITenantEntity`, new services must respect tenant scope
4. **Use pagination** — any new list query must support `PageNumber` + `PageSize` parameters
5. **Add validation** — new DTOs must have DataAnnotations or FluentValidation rules
6. **Handle errors** — return `ResponseResult<T>`, catch and log exceptions, never swallow silently
7. **Write tests** — at minimum, one test verifying tenant isolation for new entities

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
