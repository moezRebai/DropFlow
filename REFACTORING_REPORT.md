# DropFlow Backend — Code Quality & Refactoring Report

**Date:** 2026-07-05
**Scope:** `backend/` — DropFlow.Domain, DropFlow.Application, DropFlow.Infrastructure, DropFlow.Api
**Method:** Four parallel per-project reviewer agents, each auditing against the `.claude/skills/` rubrics (csharp-coding-standards, csharp-api-design, csharp-type-design-performance, csharp-concurrency-patterns, efcore-patterns, database-performance, microsoft-extensions-configuration, microsoft-extensions-dependency-injection, project-structure) and the DropFlow-specific rules in `CLAUDE.md`. CodeGraph used for exploration.
**Note:** No code was changed in this pass. This is an assessment only.

---

## Executive Summary

| Severity | Count | Themes |
|----------|-------|--------|
| 🔴 **Critical** | 3 | Exposed live secrets · cross-tenant file read · guaranteed cross-tenant reference collision |
| 🟠 **High** | 10 | Reference-number races · unprotected write endpoints · public paid-API endpoints · seeded default admin password · cross-tenant login · N+1 queries · case-sensitive search · unaudited route lifecycle · `ITenantEntity` gap |
| 🟡 **Medium** | 18 | Middleware env-gating · exception contract leak · N+1 / Cartesian query shapes · UTC DateTime handling · missing transactions · double-booking races · sync `SaveChanges` gap · retry strategy |
| 🟢 **Low** | 11 | Value-object modeling · dead code · mojibake comments · pagination gaps · `CancellationToken` propagation · config validation |

**The three items that need action before anything else** (details below): **C-1 exposed secrets** (rotate now), **C-2 cross-tenant file access**, and **C-3 delivery-reference collision** (blocks a second tenant from onboarding on any day the first tenant already created a delivery).

---

## 🔴 Critical

### C-1 — Live secrets committed / exposed in the working tree
**Project:** Api · **Files:** `secrets.txt` (repo root), `backend/DropFlow.Api/appsettings.Development.json:55-57`
**Rule:** CLAUDE.md "Never commit secrets"; `microsoft-extensions-configuration` (use user-secrets/env)

`secrets.txt` contains real, working credentials in plaintext — Gmail app password, the **Neon Postgres connection string with password**, the SuperAdmin password, and a Google Maps API key. The same Google Maps key is also hardcoded in `appsettings.Development.json`, which **is git-tracked** (`M` in git status). `secrets.txt` is untracked but **not in `.gitignore`** — one `git add .` from being committed permanently.

**Fix:**
1. **Rotate all four secrets now** — treat them as compromised (Neon DB password, Gmail app password, Google Maps key, SuperAdmin password).
2. Replace the Maps key value in `appsettings.Development.json` with a placeholder (`WILL_BE_OVERRIDDEN_BY_USER_SECRETS`, matching the other keys).
3. Delete `secrets.txt`; add `secrets.txt` and `appsettings.*.local.json`/`*.secret` patterns to `.gitignore`.
4. If any of these ever landed in git history, scrub with `git filter-repo`.

---

### C-2 — Cross-tenant file access via client-controlled path
**Project:** Api / Infrastructure · **Files:** `backend/DropFlow.Api/Controllers/FilesController.cs:23-31`, `backend/DropFlow.Infrastructure/Services/FileStorageService.cs:104-114` (delete path)
**Rule:** CLAUDE.md Multi-Tenant Rules; `csharp-api-design` (validate all input)

The catch-all route `{*relativePath}` (whose first segment is the tenantId, e.g. `/api/files/{tenantId}/deliveries/...`) is passed straight to `fileStorageService.GetFileAsync(relativePath)` with **no check that the tenantId segment matches the caller's `TenantId` claim** and **no traversal guard**. Any authenticated user of any tenant can read another tenant's signatures/photos by changing the first path segment, and possibly escape the upload root with `../`. Separately, `FileStorageService.DeleteFileAsync` is missing the `Path.GetFullPath` + `StartsWith(_basePath)` guard that `GetFileAsync` (line 93-95) already has.

**Fix:** Resolve `TenantId` from claims and build the physical path server-side from `{claimTenantId}/deliveries/{deliveryId}/{sanitizedFileName}` — never trust the client string. Reject `..`/rooted paths. Verify the target delivery belongs to the tenant. Apply the same canonicalization guard to `DeleteFileAsync`.

---

### C-3 — Delivery `Reference` unique index is global, not per-tenant → guaranteed cross-tenant collision
**Project:** Infrastructure / Application · **Files:** `backend/DropFlow.Infrastructure/Persistence/Configurations/DeliveryConfiguration.cs:17`, `backend/DropFlow.Infrastructure/Services/DeliveryReferenceService.cs:32-35`
**Rule:** CLAUDE.md Multi-Tenant Rules; `efcore-patterns`

References are `DL-yyyyMMdd-NNN` with a **per-tenant, per-day** sequence starting at `001`, but the unique index is on `Reference` **alone**. The existence check is tenant-scoped (`d.TenantId == tenantId && d.Reference == reference`), so it never sees the other tenant's row — then `SaveChanges` throws a unique-violation. **The second tenant to create their first delivery of any given day always fails**, and the raw exception surfaces to the UI.

**Fix:** Make the constraint composite (new migration):
```csharp
builder.HasIndex(d => new { d.TenantId, d.Reference }).IsUnique();
```
This also makes the collision-retry loop meaningful. See H-1 for the accompanying concurrency fix.

---

## 🟠 High

### H-1 — Delivery advisory lock is ineffective (released before the INSERT, and spans pooled connections)
**Project:** Infrastructure / Application · **Files:** `backend/DropFlow.Infrastructure/Services/DeliveryReferenceService.cs:25-54`, caller `backend/DropFlow.Application/Services/Deliveries/DeliveryService.cs:447-479`
**Rule:** `csharp-concurrency-patterns`; PostgreSQL/Neon pooled connections (CLAUDE.md); CLAUDE.md Known Issues

Two compounding defects:
1. **Lock doesn't span the write.** `pg_advisory_lock` is acquired and released *inside* `GenerateReferenceAsync`, but the `Deliveries.Add(...)` + `SaveChangesAsync()` happen later in `CreateDeliveryAsync`, after the lock is released. Two concurrent creates read the same `MAX(SequentialNumber)`, compute the same `001`, both pass the check, both insert.
2. **Lock spans separate pooled connections.** Each `ExecuteSqlRawAsync` opens/returns a connection to the pool, so the session-level `pg_advisory_lock` and its `pg_advisory_unlock` (and the intermediate reads) can run on **different physical connections** — worse under Neon's PgBouncer pooling. The lock never serializes anything and can leak.

Also, `SequentialNumber` is fetched by a *second* independent round-trip (`GetNextSequentialNumberAsync`, line 448) that can diverge from the number embedded in the reference.

**Fix:** Prefer the DB constraint as the source of truth (C-3) plus a retry-on-`DbUpdateException` loop, computing reference and sequence in **one pass**. If keeping an advisory lock, use a transaction-scoped lock on a single connection via the execution strategy:
```csharp
var strategy = context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () => {
    await using var tx = await context.Database.BeginTransactionAsync();
    await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockId);
    // read MAX(sequence) + build reference + Add + SaveChanges
    await tx.CommitAsync(); // xact lock auto-released, same connection
});
```

### H-2 — Route reference: no unique constraint, `Count()+1` race, and wrong prefix in the fallback
**Project:** Infrastructure / Application · **Files:** `backend/DropFlow.Infrastructure/Services/RouteReferenceService.cs:18-62`, `backend/DropFlow.Infrastructure/Persistence/Configurations/RouteConfiguration.cs:56`
**Rule:** `csharp-concurrency-patterns`; correctness

Three defects: (a) the index `HasIndex(rs => rs.Reference)` is **not** `.IsUnique()` — nothing prevents duplicate route references; (b) `sequentialNumber = CountAsync(...) + 1` with no lock/transaction is the classic race, and `Count+1` reuses numbers after a deletion; (c) the primary path emits prefix `RT-` (line 33) while the collision-recovery loop emits `FR-` (line 46) and the XML doc says `FR-` — so the "uniqueness recheck" checks a reference the primary path never produces and "succeeds" with an inconsistent value.

**Fix:** Add `HasIndex(r => new { r.TenantId, r.Reference }).IsUnique()`, unify the prefix, switch to `MAX(sequence)+1`, and generate inside the transaction/lock pattern from H-1.

### H-3 — Write endpoints unprotected by role — ReadOnly/Accountant/Driver can mutate data
**Project:** Api · **Files:** `backend/DropFlow.Api/Controllers/DeliveriesController.cs:14,30,39,58,74,86`, `backend/DropFlow.Api/Controllers/RoutesController.cs:129-145`
**Rule:** CLAUDE.md Role Hierarchy (ReadOnly = view-only; Accountant = invoices only)

These actions carry only the class-level `[Authorize]`, so **any authenticated role** can call them — including `CreateDelivery`, `UpdateDelivery`, `UpdateStatus`, `DuplicateDelivery`, `BulkUpdateStatus`, and route `Start`/`Complete`. ReadOnly and Accountant can create and mutate deliveries.

**Fix:** Add explicit `[Authorize(Roles = ...)]` per action (or policies like `RequireManager`). Decide the intended role for route `Start`/`Complete` (likely `Driver`). Consider a controller default that denies writes to view-only roles.

### H-4 — Public, unauthenticated endpoints that spend the paid Google API
**Project:** Api · **File:** `backend/DropFlow.Api/Controllers/GeocodingController.cs:10` (`[AllowAnonymous]` on the whole controller)
**Rule:** auth coverage; secrets/cost exposure

The entire controller is `[AllowAnonymous]` and exposes `TestGeocode`, `TestAutocomplete`, `TestPlaceDetails`, `TestFullAutocomplete` — debug endpoints that invoke the **billed** Google Geocoding/Places APIs. Anyone on the internet can drive Google billing and probe the service.

**Fix:** Remove these test endpoints from the deployed API, or gate behind `[Authorize(Roles="Admin")]` and register only in Development. At minimum apply the `auth` rate limiter.

### H-5 — Default admin password hardcoded and seeded in every environment
**Project:** Api · **Files:** `backend/DropFlow.Api/Extensions/DatabaseExtensions.cs:56`, `backend/DropFlow.Api/Program.cs:158`
**Rule:** `microsoft-extensions-configuration`; CLAUDE.md secrets

`var adminPassword = configuration["SuperAdmin:Password"] ?? "Admin@DropFlow123";` — a real fallback password (also in `secrets.txt`). `InitializeDatabaseAsync → SeedSuperAdminAsync` runs unconditionally (not just Development), so a production deploy missing the config key silently creates a super-admin with a publicly known password. The `LogWarning("CHANGE DEFAULT PASSWORD…")` does not mitigate it.

**Fix:** Remove the fallback; `ValidateOnStart`/throw if `SuperAdmin:Password` is unset. Bind SuperAdmin to a validated options class. Seed the super-admin only when explicitly enabled.

### H-6 — Login / password-reset resolve an arbitrary first user by email across tenants
**Project:** Application · **Files:** `backend/DropFlow.Application/Services/Users/AuthService.cs:191-192` (`LoginAsync`), `410-411` (`ResetPasswordAsync`)
**Rule:** correctness / multi-tenant auth

The same email can exist in multiple tenants (`GetUserTenantsAsync` acknowledges this), but `LoginAsync` and `ResetPasswordAsync` do `FirstOrDefaultAsync(u => u.Email == dto.Email)` with no tenant discriminator. Login authenticates against whichever row comes first (wrong-tenant login or spurious "bad password"); reset generates a token for one user but may apply against another → legitimate resets fail for non-first accounts.

**Fix:** Thread the selected `TenantId` (already surfaced by `GetUserTenantsAsync`) into the login/reset DTOs and filter by `Email && TenantId`.

### H-7 — `ClientService.GetClientsAsync` runs an N+1 (a stats query per client)
**Project:** Application · **Files:** `backend/DropFlow.Application/Services/ClientService.cs:562-567` → `MapToDtoAsync:705-713`
**Rule:** `database-performance` ("think in batches — avoid N+1")

After paging clients, the loop calls `MapToDtoAsync` per client, each running a separate `Deliveries.GroupBy` aggregate. A page of 20 = 21 queries.

**Fix:** Aggregate once — group deliveries by `ClientId` for the page's client IDs in a single query and join in the projection, or compute counts/revenue as a correlated sub-select inside one `.Select`.

### H-8 — `SearchClientsAsync` is case-sensitive on PostgreSQL (stale SQL Server assumption)
**Project:** Application · **File:** `backend/DropFlow.Application/Services/ClientService.cs:146-152`
**Rule:** CLAUDE.md PostgreSQL Specifics; correctness

A comment states "SQL Server est déjà CI par défaut" and deliberately drops `ToLower()`, but the project migrated to PostgreSQL where `LIKE`/`.Contains()` is **case-sensitive**. Client autocomplete silently misses results differing only in case. (`GetClientsAsync:542-547` uses `.ToLower()` — inconsistent.)

**Fix:** Use `EF.Functions.ILike(c.FirstName, $"%{term}%")` for case-insensitive search; remove the stale comment.

### H-9 — Route lifecycle mutations are never audited
**Project:** Application · **File:** `backend/DropFlow.Application/Services/Routes/RouteService.cs` (ctor `15-23`; `Create/Update/Delete/Confirm/Cancel`)
**Rule:** CLAUDE.md Code Generation Rule #8 ("audit critical actions")

`RouteService` injects no `IAuditService`, so none of its create/update/delete/confirm/cancel operations are audited — unlike `DeliveryService`/`ClientService`. `DeliveryService.BulkUpdateStatusAsync`/`BulkDeleteAsync` (lines 845, 882) are also unaudited despite mutating many rows.

**Fix:** Inject `IAuditService` into `RouteService` and log each state change; add audit calls to the bulk delivery operations.

### H-10 — Tenant entities carry `TenantId` without implementing `ITenantEntity` → not auto-stamped
**Project:** Domain / Infrastructure · **Files:** `backend/DropFlow.Domain/Entities/RouteTeam.cs:8`, `AuditLog.cs`, `UserInvitation.cs`, `TenantDepot.cs`, `ApplicationUser.cs`; configs `RouteTeamConfiguration.cs:27`, `AuditLogConfiguration.cs`, `UserInvitationConfiguration.cs`, `TenantDepotConfiguration.cs`
**Rule:** CLAUDE.md Multi-Tenant Rules ("all new entities with tenant scope MUST implement `ITenantEntity`")

`ApplyTenantId()` (`ApplicationDbContext.cs:132`) only stamps `ChangeTracker.Entries<ITenantEntity>()`. These entities have a `TenantId` column and a tenant query filter but don't implement the interface, so their `TenantId` is populated only if a caller remembers to set it — risking `TenantId = 0` rows (which then match the admin branch of the filter and can leak into the admin view). **`RouteTeam.TenantId` is provably dead**: `RouteTeam.Create` never sets it, and the query filter keys off `rsc.Driver.TenantId` (`ApplicationDbContext.cs:96`) — the column is always `0`.

**Fix:** Have each genuinely tenant-owned entity implement `ITenantEntity` (with a public `set`) so stamping is automatic. For `RouteTeam`, either implement it and set it in `Create`, or delete the redundant column and keep filtering through `Driver` — pick one consistently.

---

## 🟡 Medium

### M-1 — Synchronous `SaveChanges()` is not overridden → tenant stamping bypassed
`backend/DropFlow.Infrastructure/Persistence/ApplicationDbContext.cs:118-137` — only `SaveChangesAsync` is overridden. Any path (incl. some Identity flows) calling sync `SaveChanges()` skips `ApplyTenantId()`, inserting `ITenantEntity` rows with `TenantId = 0`. **Fix:** override sync `SaveChanges()` too, funneling both through a shared method. *(Rule: CLAUDE.md multi-tenant)*

### M-2 — Global exception handler bypasses the `ResponseResult<T>` contract and leaks messages in production
`backend/DropFlow.Api/Middleware/ExceptionHandlingMiddleware.cs:33-38,45-51` — returns a bespoke `ErrorResponse` (two error contracts for clients), and the `ArgumentException` branch sets `Detail = exception.Message` **unconditionally** (no `IsDevelopment()` guard, unlike the other branches). **Fix:** return a `ResponseResult`-shaped failure; guard `Detail` behind `IsDevelopment()`. *(Rule: CLAUDE.md ResponseResult)*

### M-3 — No transactions around multi-`SaveChanges` create flows
`DeliveryService.cs:478-497` (delivery then items), helpers `1197-1198`/`1233-1234`; `ClientService.cs:57-80` (client then address). A failure on the second write orphans the first. `RouteService.CreateAsync` and `DriverAppService.ValidateDeliveryAsync` already use `BeginTransactionAsync`. **Fix:** wrap each create in a transaction + `CommitAsync()`. *(Rule: correctness/atomicity)*

### M-4 — Vehicle/date double-booking is a check-then-act race
`backend/DropFlow.Application/Services/Routes/RouteService.cs:187-193` (mirror at `316-323`) — `AllAsync(...)` verifies no other route uses the vehicle that day, then creates one; two concurrent requests both pass under ReadCommitted. **Fix:** filtered unique index on active routes per `(TenantId, VehicleId, Date)` + handle the violation, or advisory lock. *(Rule: csharp-concurrency-patterns)*

### M-5 — Dashboard: ~10 sequential aggregates; revenue chart loads unbounded rows and buckets in memory
`backend/DropFlow.Application/Services/DashboardService.cs:39-89` (FetchStats), `313-329` (FetchRevenueChartData) — `FetchRevenueChartDataAsync` pulls every delivery in the period into memory then buckets with nested LINQ. **Fix:** push bucketing into SQL (`GROUP BY date_trunc`, `SUM`/`COUNT`); collapse status counts into one grouped query. *(Rule: database-performance; CLAUDE.md Known Issues)*

### M-6 — `GetAvailableDeliveriesForRouteAsync`: no pagination, no `AsNoTracking`, Cartesian tracked Include set
`backend/DropFlow.Application/Services/Deliveries/DeliveryService.cs:992-1037` — loads all matching deliveries with 6 `Include`s (incl. collection `Items`) as tracked entities, unbounded. **Fix:** `.AsNoTracking().AsSplitQuery()` (or project to DTO) + a cap. *(Rule: database-performance)*

### M-7 — Multiple collection `Include`s without `AsSplitQuery` (Cartesian explosion)
`DriverAppService.cs:48-65`, `539-548`; `DeliveryService.cs:34-43`; `RouteService.cs:1216-1229` — rows multiply as deliveries × items × team. `RouteService.GetByIdAsync:105` correctly uses `AsSplitQuery()`; these parallels don't and aren't `AsNoTracking`. **Fix:** add `.AsNoTracking().AsSplitQuery()`. *(Rule: database-performance)*

### M-8 — List projections carry dead `Include`s that EF discards
`DeliveryService.cs:153-168`, `259-268`; `RouteService.cs:27-32` — `.Include(...)` chains followed by a `.Select(...)` projection; EF ignores the includes (pure noise + misleading). `GetUnassignedDeliveriesAsync` also omits `.AsNoTracking()`. **Fix:** delete the `Include` chains on projected queries; add `AsNoTracking()`. *(Rule: database-performance)*

### M-9 — `DriverAvailabilityService.CheckMultipleAvailabilityAsync` is 3×N sequential queries
`backend/DropFlow.Application/Services/Drivers/DriverAvailabilityService.cs:76-89` → each `CheckAvailabilityAsync` runs 3 queries (13, 28, 47). **Fix:** batch — one `RouteTeams` query and one urgent-`Deliveries` query filtered by `driverIds.Contains(...)`, compute in memory. *(Rule: database-performance)*

### M-10 — No global UTC DateTime handling for Npgsql
Entity configs have no DateTime converter; `ScheduledDate`, `Route.Date`, `CreatedDate` etc. can arrive with `Kind=Unspecified` and throw against `timestamptz`. **Fix:** global UTC value converter in `OnModelCreating` (force `DateTime.SpecifyKind(v, Utc)`), or map to `timestamp without time zone`. *(Rule: CLAUDE.md PostgreSQL — Npgsql throws on Unspecified)*

### M-11 — `ScheduledDate == today` equality can silently miss dated deliveries
`DashboardService.cs:49,51,133` — if `ScheduledDate` carries a time component, `== today` (midnight) matches nothing; "today's deliveries" under-reports. **Fix:** half-open range `>= today && < today.AddDays(1)`. *(Rule: correctness)*

### M-12 — `AuditService.LogAsync` calls `SaveChangesAsync` on the shared scoped context
`backend/DropFlow.Infrastructure/Services/AuditService.cs:60-61` — if the caller has other pending tracked changes (mutate-then-audit is common), this commits them prematurely and partially. **Fix:** write audit rows on a separate context via `IDbContextFactory<ApplicationDbContext>`, or raw insert. *(Rule: database-performance/transactional correctness)*

### M-13 — No transient-failure retry (`EnableRetryOnFailure`) for Neon
`backend/DropFlow.Infrastructure/DependencyInjection.cs:26-29` — Neon serverless connections drop/cold-start; transient errors surface unhandled. **Fix:** `b.EnableRetryOnFailure()` (wrap the advisory-lock work from H-1 in `CreateExecutionStrategy`, which the retry strategy requires). *(Rule: efcore-patterns)*

### M-14 — Swagger served in all environments
`backend/DropFlow.Api/Program.cs:102-107` — `UseSwagger()`/`UseSwaggerUI()` called unconditionally despite comments claiming "Development only", exposing the full API surface + JWT scheme in production. **Fix:** wrap in `if (app.Environment.IsDevelopment())`. *(Rule: middleware env-gating)*

### M-15 — HTTPS redirection only in Development (inverted)
`backend/DropFlow.Api/Program.cs:114-117` — `UseHttpsRedirection()` runs only when `IsDevelopment()`; production has no redirect. **Fix:** enable in production (or document proxy TLS termination and use forwarded headers). *(Rule: HTTPS/middleware)*

### M-16 — `DbContext` injected directly into a controller (layering + info leak)
`backend/DropFlow.Api/Controllers/SystemController.cs:11-32` — reaches into EF directly, parses the connection string in the controller, and `GET /api/system/db-info` returns the DB host to **any authenticated user**. **Fix:** move behind an Application/Infrastructure service, restrict to `[Authorize(Roles="Admin")]`, don't leak host details. *(Rule: CLAUDE.md layering; thin controllers)*

### M-17 — Role whitespace bug — `"Admin, Manager"` silently excludes Manager
`backend/DropFlow.Api/Controllers/VehiclesController.cs:50` — ASP.NET Core splits `Roles` on `,` **without trimming**, so `" Manager"` never matches; `Delete` becomes Admin-only. **Fix:** remove the space; better, replace magic-string roles with the `Roles` constants or the `RequireManager` policy. *(Rule: csharp-coding-standards; consistent authz)*

### M-18 — Dead endpoint returns 200 while doing nothing
`backend/DropFlow.Api/Controllers/RoutesController.cs:157-165` — `RecalculateMetrics`'s body is entirely commented out and returns `Ok()`; callers believe recalculation happened. **Fix:** implement it or remove the endpoint (return `501`/remove rather than a misleading `200`). *(Rule: csharp-api-design; no dead code)*

---

## 🟢 Low

### L-1 — Raw exception messages leaked to the UI (inconsistent)
`DeliveryService.cs:145,518,640`; `RouteService.cs:276,427,1207,1338` — `ResponseResult.Failure($"… : {ex.Message}")` exposes internal error text; other handlers in the same files return generic messages. **Fix:** log `ex`, return a generic French message. *(Rule: CLAUDE.md — no raw exceptions to UI)*

### L-2 — `AuditChange` is a mutable class in `ValueObjects`
`backend/DropFlow.Domain/ValueObjects/AuditChange.cs:3-8` — a 3-field value object as a mutable class (reference equality, allocations). **Fix:** `public readonly record struct AuditChange(string Field, string? OldValue, string? NewValue);` *(Rule: csharp-coding-standards — value objects as `readonly record struct`)*

### L-3 — `Route` name collision in Domain
`backend/DropFlow.Domain/Maps/GoogleDirectionsResponse.cs:23` declares a `Route` class colliding with the `Route` entity (plus generic `Location`/`Distance`/`Step` at file scope). **Fix:** rename to `GoogleRoute`/`DirectionsRoute` or nest inside `GoogleDirectionsResponse` — and see L-5 (move out of Domain). *(Rule: csharp-coding-standards)*

### L-4 — Unused global-namespace `BaseEntity`; audit fields duplicated across entities
`backend/DropFlow.Domain/Common/BaseEntity.cs:1-11` declares no `namespace` and no entity inherits it; `Client`/`Store`/`Delivery`/`Route` each re-declare the six audit properties by hand. **Fix:** put it in `DropFlow.Domain.Common` and inherit, or delete it. *(Rule: project-structure; DRY)*

### L-5 — External-API DTOs and infra config leak into the Domain layer
`Domain/Maps/GoogleDirectionsResponse.cs`, `Maps/GeocodeAddress.cs`; `Domain/Configurations/EmailSettings.cs`, `SmtpSettings.cs`; `Domain/Emails/EmailRequest.cs`, `EmailAttachment.cs` — Google JSON contracts, SMTP settings, and email shapes are Infrastructure/Application concerns, dragging `System.Text.Json` attributes into Domain. **Fix:** move to Infrastructure/Application; keep Domain to entities + domain interfaces. *(Rule: CLAUDE.md dependency graph)*

### L-6 — Split-brain domain model (rich vs anemic entities)
Anemic public-setter bags (`Client`, `Store`, `Delivery`, `ClientAddress`, `DeliveryItem`, `TimeSlot`) vs rich factory + private-setter aggregates (`Driver`, `Route`, `Tenant`, `TenantDepot`, `Vehicle`, `ApplicationUser`) in the same layer. **Fix:** pick one convention; at minimum give `Client`/`Store`/`Delivery` private setters + `Create`/`Update` methods. *(Rule: csharp-coding-standards — immutability by default)*

### L-7 — Mojibake (non-UTF-8) French comments
`backend/DropFlow.Domain/Entities/Delivery.cs` (lines 38-63, 96-121, 144) and `Route.cs` (21, 144-171) render accents as `�`. Other French-comment files (e.g. `Tenant.cs`) are clean UTF-8. **Fix:** re-save these two files as UTF-8 and repair the comments. *(Rule: maintainability)*

### L-8 — No `CancellationToken` propagation anywhere in the API
All controllers (e.g. `RoutesController`, `TimeSlotsController`, `DeliveriesController`) — no action accepts/forwards a `CancellationToken`, so client aborts don't cancel DB/HTTP work. `GeocodingService.GeocodeAddressAsync` (`…/Geocoding/GeocodingService.cs:42`) also lacks the timeout CTS its siblings use. **Fix:** add `CancellationToken ct` to actions/services and thread through EF/HTTP. *(Rule: csharp-concurrency-patterns)*

### L-9 — Pagination gaps on list endpoints
`backend/DropFlow.Api/Controllers/TimeSlotsController.cs:16-22` (`GetAll`) takes no paging params and returns everything. **Fix:** accept a filter DTO with `PageNumber`/`PageSize`, page server-side with `.AsNoTracking()`. *(Rule: CLAUDE.md Code Generation Rule #5)*

### L-10 — Missing `ProducesResponseType` / non-uniform status codes and role gating
Most controllers return `IActionResult` with no `[ProducesResponseType]` (only `FilesController` declares them); creates mix `StatusCode(201)`/`CreatedAtAction`/`Ok`; some read actions are role-gated inconsistently (`TimeSlotsController.cs:28` `GetById` unrestricted while `GetAll` is `Admin,Manager`). **Fix:** annotate response types, standardize create → `CreatedAtAction`, delete → `NoContent`, apply deliberate read-role policies. *(Rule: csharp-api-design)*

### L-11 — Config read via `IConfiguration` indexers instead of validated options; misc smells
`AuthenticationExtensions.cs:41-43,62-63`, `CorsExtensions.cs:12,24`, `DependencyInjection.cs:28,31`, `DatabaseExtensions.cs:55-56` — JWT/CORS/SuperAdmin/EmailSettings read as loose strings with no `ValidateOnStart` (a short JWT `SecretKey` fails deep, not at startup). Plus: `NoTracking` is not the default `QueryTrackingBehavior` (relies on every call site); primitive-obsession GPS coords as loose `double?`/`decimal` across entities; magic-string states (`PlanType`, `Theme`, `EmailProvider`); French role value `Livreur` vs entity term `Driver` (`Constants/Roles.cs:7`); `int.Parse(User.FindFirst("TenantId")?.Value!)` (`UserManagementController.cs:59`) throws on a missing claim (use `TryParse`); tautology `if (route.Status == Confirmed) route.Status = Confirmed;` (`RouteService.cs:691-694`); `async` methods with no `await` — CS1998 (`AuthService.cs:489-507`, `DeliveryService.cs:1239-1281`); `LangVersion=latestmajor` floats across SDK upgrades. **Fix:** bind validated options POCOs (`ValidateDataAnnotations().ValidateOnStart()`), set `QueryTrackingBehavior.NoTracking` default, introduce a `GeoCoordinate` value struct, model states as enums, and clean the dead/no-await code. *(Rules: microsoft-extensions-configuration, efcore-patterns, csharp-coding-standards, csharp-type-design-performance)*

---

## ✅ Verified Correct (do not "fix" these)

The agents explicitly confirmed these are sound — noted here to prevent wasted rework:

- **Global query filters are correct.** They call the instance method `GetCurrentTenantId()`, re-evaluated per query — **not** the "captured-once-at-startup" bug. All 7 `ITenantEntity` types (Client, Delivery, Driver, Route, Store, TimeSlot, Vehicle) have a filter; owned/child types (ClientAddress, DeliveryItem, RouteTeam) filter through their parent.
- **`ApplyTenantId()` correctly targets only `Added` entities** and doesn't overwrite on `Modified` (the sync-`SaveChanges` gap in M-1 is the only hole).
- **JWT validation is fully configured** — `ValidateIssuer/Audience/Lifetime/IssuerSigningKey`, `ClockSkew = Zero`, `RequireHttpsMetadata = true`, token blacklist on `OnTokenValidated` (`AuthenticationExtensions.cs:56-91`).
- **Rate limiter is applied** to sensitive auth endpoints (`AuthController.cs:29,57,118`); middleware order (exception → routing → CORS → rate limiter → authn → authz) is sound; security headers + CSP are solid (`SecurityExtensions.cs:30-57`).
- **No `.Result`/`.Wait()` blocking calls** anywhere in the backend; controllers are `async` throughout.
- **No endpoint binds `TenantId` from client input** — the only references resolve from claims. (The one real tenant-leak vector is C-2, via a path segment, not a bound `TenantId`.)
- **Typed `HttpClient`s** via `AddHttpClient<>` — no raw `new HttpClient()`. **Decimal `HasPrecision(18,2)`** applied to money fields. **`AuditSeverity`** correctly kept in `Domain.Enums`; no shared enums misplaced.

---

## Suggested Remediation Order

1. **Immediately (security):** C-1 (rotate + remove secrets), C-2 (file-path tenant guard), H-4 (lock down public geocoding endpoints), H-5 (remove default admin password).
2. **Multi-tenant correctness:** C-3 + H-1 + H-2 (reference-number constraints & lock pattern — do these together), H-6 (cross-tenant login), H-10 + M-1 (`ITenantEntity` gap + sync `SaveChanges`).
3. **Authz & API contract:** H-3, M-2, M-16, M-17, M-18.
4. **Performance:** H-7, M-5, M-6, M-7, M-8, M-9 (query shape), M-13 (retry), M-10 (UTC handling).
5. **Consistency & hygiene:** H-8, H-9, M-3, M-4, M-11, M-12, and the Low tier.

Each fix should follow the existing patterns already present in the codebase (e.g. `RouteService.CreateAsync`'s transaction usage, `RouteService.GetByIdAsync`'s `AsSplitQuery`, `AdminController`'s `Roles` constants) — several findings are simply "apply the pattern the codebase already uses elsewhere, consistently."
