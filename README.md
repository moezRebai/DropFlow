# DropFlow

> Multi-tenant SaaS delivery management platform for logistics companies.

## Overview

DropFlow is a web-based platform that lets logistics companies manage their entire delivery operation — from client and order management through route planning, driver dispatch, and delivery validation. Each company runs in a fully isolated tenant, with role-based access control separating platform administrators, operations managers, and drivers. The application is built as a React + TypeScript single-page front-end communicating with an ASP.NET Core REST API backed by PostgreSQL.

## Features

### Fully Implemented

- **Multi-tenant isolation** — every data record is scoped to a tenant via EF Core global query filters; TenantId is resolved from JWT claims, never from client input
- **Authentication** — JWT-based login with ASP.NET Identity (password complexity rules, account lockout after 5 failed attempts)
- **Role-based authorization** — three roles (Admin, Manager, Livreur) enforced at the API and UI layer
- **User management** — invite users by email, assign roles, activate/deactivate, soft-delete and restore
- **Platform administration** — DropFlow super-admin can create, activate/deactivate, and manage all tenant companies and their users
- **Client management** — full CRUD with multiple delivery addresses per client; default address selection; address geocoding via Google Maps
- **Delivery management** — create, edit, duplicate, bulk status update, bulk soft-delete; Standard and Urgent delivery types; sequential reference numbers (DL-YYYYMMDD-NNNN)
- **Delivery kanban / list view** — filterable and sortable paginated list; status pipeline (ToBePlanned → Confirmed → InProgress → Delivered / Canceled)
- **Route management** — create route sheets, assign deliveries, manage driver teams (main driver + helpers), confirm/start/complete/cancel lifecycle
- **Route optimization** — Google Directions API integration for waypoint ordering; manual drag-and-drop reordering with metric recalculation
- **Route sheet PDF** — downloadable PDF generated with QuestPDF
- **Driver app API** — dedicated endpoints for the driver mobile experience: today's route, delivery detail (PII-limited view), delivery validation with signature and photo upload, route start/complete
- **Delivery validation** — signature capture, photo upload (base64), client-absent handling; files stored on the server filesystem
- **Depot management** — multi-depot support per tenant; default depot selection for route starting points
- **Company settings** — update company info, legal info, and logo per tenant
- **Dashboard** — KPI cards (unplanned deliveries, today's deliveries, monthly revenue, active routes), today's delivery list, at-risk delivery list, revenue/status/store charts
- **Audit logging** — every significant action is recorded with tenant, user, entity, and severity
- **Address autocomplete** — Google Places API integration for address input fields
- **Time slot management** — configurable delivery time windows
- **Store management** — CRUD for warehouse/store origins of deliveries
- **Vehicle management** — CRUD for delivery fleet
- **Driver management** — CRUD linked to Identity users; availability checking
- **Profile management** — users can update their profile, change password, and set UI preferences
- **Health check** — `/health` endpoint backed by EF Core database check
- **Request logging middleware** — structured request/response logging
- **Global exception handling** — consistent JSON error responses; stack traces hidden in production

### Partially Implemented

- **Route optimization algorithm** — the Google Directions API call and waypoint reordering work; the `RecalculateMetrics` endpoint is stubbed out (returns 200 with no data)
- **Dashboard stats** — KPI data is live; chart data makes multiple round-trips instead of a single optimized query (tracked as a known issue)
- **Driver mobile experience** — API endpoints are complete; the PWA front-end is not yet started (see Roadmap)

### Not Yet Implemented

- PWA mobile driver application
- Invoice PDF generation
- Invoice email delivery
- Stock / inventory module
- Advanced reporting and analytics
- SMS notifications (Twilio)

## Architecture

DropFlow follows a layered architecture with a clean separation of concerns across six projects.

```
DropFlow.sln
├── backend/
│   ├── DropFlow.Domain          # Entities, enums, interfaces, constants — no dependencies
│   ├── DropFlow.Application     # Service layer, DTOs, FluentValidation — depends on Domain
│   ├── DropFlow.Infrastructure  # EF Core, Identity, external services — depends on Application
│   └── DropFlow.Api             # ASP.NET Core Web API — depends on Application + Infrastructure
├── shared/
│   └── DropFlow.Shared          # DTOs shared between backend projects
├── frontend/
│   └── DropFlow.Web             # React + TypeScript SPA (Vite) — consumes the API over HTTP
└── mobile/
    └── DropFlow.Mobile          # .NET MAUI driver app
```

The `DropFlow.Api` and the `DropFlow.Web` front-end are separate processes. The React SPA communicates with the API exclusively over HTTP (Axios). There is no shared in-process state between them.

### Dependency Flow

```
React SPA  →  (HTTP)  →  Api  →  Application  →  Domain
                          ↓
                     Infrastructure  →  Application
```

### Key Design Decisions

- **No MediatR / CQRS** — services are injected directly via interfaces
- **`ResponseResult<T>`** — all service methods return a typed result wrapper instead of throwing exceptions to the UI
- **Typed API modules** — the React app groups API calls in per-domain modules under `src/api/`; an Axios interceptor handles JWT attachment and 401 token refresh
- **Multi-step wizards** — state is held in a centralized Zustand store, not individual component state
- **Cross-tab sync** — a `BroadcastChannel` propagates delivery change events across browser tabs

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 (SDK 10.x required) |
| API framework | ASP.NET Core 9 |
| Frontend framework | React 18 + TypeScript (Vite) |
| UI components / styling | shadcn/ui + Tailwind CSS v4 |
| Frontend data layer | TanStack Query + Zustand + Axios |
| ORM | Entity Framework Core 9 |
| Database | PostgreSQL (Neon) |
| Identity | ASP.NET Core Identity |
| Authentication | JWT Bearer tokens |
| Client-side auth storage | `localStorage` (Zustand persist) |
| PDF generation | QuestPDF 2025.x |
| Validation | FluentValidation 12.x |
| Mapping display strings | Humanizer.Core 3.x |
| External geocoding | Google Maps Geocoding API |
| External address search | Google Places API |
| External route optimization | Google Directions API |
| Email | SMTP (Gmail or any SMTP provider) |
| Logging | Microsoft.Extensions.Logging + Serilog.AspNetCore |
| API docs | Swagger / Swashbuckle (development only) |
| Seed data generation | Bogus 35.x |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (targets net9.0; SDK 10 required by `global.json`)
- PostgreSQL (a Neon branch or a local instance)
- Node.js 18+ and npm (for the React front-end)
- A Google Cloud project with the following APIs enabled:
  - Maps JavaScript API
  - Geocoding API
  - Directions API
  - Places API (New)
- An SMTP account for outbound email (Gmail app password, SendGrid, etc.)

### Configuration

The application uses `appsettings.json` for non-secret configuration and .NET User Secrets for all sensitive values. **Never commit real credentials to `appsettings.json`.**

Initialize user secrets for the API project:

```bash
cd src/DropFlow.Api
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-256-bit-secret-key-here"
dotnet user-secrets set "SuperAdmin:Password" "your-admin-password"
dotnet user-secrets set "EmailSettings:Smtp:Password" "your-smtp-app-password"
dotnet user-secrets set "Google:MapsApiKey" "your-google-maps-api-key"
```

See the [Configuration Reference](#configuration-reference) section below for all available keys.

### Database Setup

Apply the EF Core migration to create the database schema:

```bash
cd src/DropFlow.Api
dotnet ef database update --project ../DropFlow.Infrastructure
```

The application automatically seeds initial data (roles, DropFlow admin user, and sample tenant data) on first startup via `InitializeDatabaseAsync()`.

### Running the Application

Run two processes: the API and the React front-end.

**Terminal 1 — API:**
```bash
cd backend/DropFlow.Api
dotnet run
# Listens on https://localhost:7001
```

**Terminal 2 — React front-end:**
```bash
cd frontend/DropFlow.Web
npm install
npm run dev
# Serves on http://localhost:3000 (must match AllowedOrigins for CORS)
```

Swagger UI is available at `https://localhost:7001/swagger` in the Development environment.

## Configuration Reference

All keys below are read from `appsettings.json` or overridden by environment variables / user secrets. Keys marked **Required** must be set before the application will start or function correctly.

| Key | Description | Required | Secret |
|-----|-------------|----------|--------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string (Npgsql) | Yes | No |
| `JwtSettings:SecretKey` | HMAC-SHA256 signing key for JWT tokens (min. 32 characters) | Yes | **Yes** |
| `JwtSettings:Issuer` | JWT issuer claim value | Yes | No |
| `JwtSettings:Audience` | JWT audience claim value | Yes | No |
| `JwtSettings:ExpirationHours` | Token lifetime in hours (default: 8) | Yes | No |
| `SuperAdmin:Email` | Email address for the initial DropFlow platform admin | Yes | No |
| `SuperAdmin:Password` | Password for the initial platform admin | Yes | **Yes** |
| `AppUrl` | Base URL of the API (used in email links) | Yes | No |
| `BlazorClientUrl` | Base URL of the web front-end (legacy config key name) | Yes | No |
| `AllowedOrigins` | JSON array of origins permitted by CORS (development) | Yes | No |
| `ProductionUrl` | Single origin permitted by CORS in production | No | No |
| `FileStorage:BasePath` | Absolute path on the server where uploaded files are stored | Yes | No |
| `EmailSettings:Provider` | Email provider type (currently only `Smtp` is implemented) | Yes | No |
| `EmailSettings:Smtp:Host` | SMTP server hostname | Yes | No |
| `EmailSettings:Smtp:Port` | SMTP server port (typically 587 for TLS) | Yes | No |
| `EmailSettings:Smtp:EnableSsl` | Enable STARTTLS (`true`/`false`) | Yes | No |
| `EmailSettings:Smtp:Username` | SMTP authentication username | Yes | No |
| `EmailSettings:Smtp:Password` | SMTP authentication password or app password | Yes | **Yes** |
| `EmailSettings:Smtp:FromEmail` | Sender email address shown to recipients | Yes | No |
| `EmailSettings:Smtp:FromName` | Sender display name shown to recipients | Yes | No |
| `EmailSettings:DefaultSubjects:UserInvitation` | Default subject for invitation emails | No | No |
| `EmailSettings:DefaultSubjects:PasswordReset` | Default subject for password reset emails | No | No |
| `EmailSettings:DefaultSubjects:DeliveryNote` | Default subject for delivery note emails | No | No |
| `EmailSettings:DefaultSubjects:Invoice` | Default subject for invoice emails | No | No |
| `Google:MapsApiKey` | Google Maps API key (Geocoding, Directions, Places APIs enabled) | Yes | **Yes** |
| `Logging:LogLevel:Default` | Minimum log level for all categories | No | No |
| `Logging:LogLevel:Microsoft.AspNetCore` | Log level for ASP.NET Core internals | No | No |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore` | Log level for EF Core internals | No | No |
| `Logging:LogLevel:DropFlow` | Log level for application code | No | No |

## Multi-Tenant Architecture

### Tenant Isolation Model

Every data entity that belongs to a tenant implements `ITenantEntity`:

```csharp
public interface ITenantEntity
{
    int TenantId { get; set; }
}
```

The DropFlow platform admin uses the reserved `TenantId = 0` and can see all tenant data.

### How It Works

**Query filtering** — `ApplicationDbContext.OnModelCreating` registers a global query filter for every tenant entity. The filter calls `GetCurrentTenantId()` at query time, which reads the `TenantId` claim from the current HTTP request's JWT:

```csharp
modelBuilder.Entity<Delivery>().HasQueryFilter(d =>
    GetCurrentTenantId() == TenantIds.DropFlowAdmin ||
    d.TenantId == GetCurrentTenantId()
);
```

This filter is automatically applied to all LINQ queries on those entities — including eager loads via `Include()`.

**Write isolation** — `SaveChangesAsync` calls `ApplyTenantId()` before writing, which stamps `TenantId` on all newly added entities. Client-supplied `TenantId` fields in DTOs are ignored entirely.

**Claim resolution** — `TenantService.GetTenantId()` extracts the tenant from the authenticated user's JWT claims. Services call this method to scope business logic, aggregate queries, and audit records.

> **Important:** `DbContext.FindAsync()` bypasses global query filters in EF Core. All lookups by primary key on tenant-filtered entities must use `FirstOrDefaultAsync(e => e.Id == id)` to maintain isolation. See Known Issues for tracked cases.

## Authentication & Roles

### Login Flow

1. The user navigates to `/login` in the web app
2. If the account exists in multiple tenants, the app calls `GET /api/auth/tenants?email=` to list options
3. The user selects a tenant and submits credentials via `POST /api/auth/login`
4. The API returns a signed JWT containing: `UserId`, `Email`, `FullName`, `Role`, `TenantId`, `IsActive`, `TenantName`
5. The web app stores the JWT in `localStorage` (via the Zustand auth store)
6. Subsequent API calls attach the token as `Authorization: Bearer <token>`

### Roles

| Role | Constant | Access |
|------|----------|--------|
| `Admin` | `Roles.Admin` | Full access to all tenant data; can manage users, settings, routes, deliveries, clients, vehicles, stores, drivers |
| `Manager` | `Roles.Manager` | Same operational access as Admin within the tenant; cannot manage platform-level settings |
| `Livreur` | `Roles.Livreur` | Read access to their own assigned routes and deliveries via the driver API; can validate deliveries |

### Authorization Policies

| Policy | Applies to |
|--------|-----------|
| `RequireAdmin` | Admin role only |
| `RequireManager` | Manager or Admin role |
| `ActiveUser` | Any authenticated user with `IsActive = true` claim |
| `ActiveManager` | Manager or Admin with `IsActive = true` |
| `SameTenant` | Custom handler validating user TenantId matches route `{tenantId}` parameter |

### User Invitation Flow

Managers and Admins invite users by email via `POST /api/usermanagement/invite`. The system sends an email containing a time-limited token (72 hours). The recipient clicks the link, lands on `/accept-invitation`, sets a password, and is immediately logged in with a JWT.

## API Endpoints

Base URL: `https://localhost:7001` (development)

All endpoints require `Authorization: Bearer <token>` unless marked **[Public]**.

### Auth — `/api/auth`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/register` | Public | Register a new tenant (creates company + Manager user) |
| POST | `/login` | Public | Authenticate and receive JWT |
| POST | `/accept-invitation` | Public | Complete invitation flow and set password |
| POST | `/forgot-password` | Public | Request password reset email |
| POST | `/reset-password` | Public | Apply password reset token |
| GET | `/tenants?email=` | Public | List tenants for an email address (for multi-tenant login picker) |

### Admin — `/api/admin` _(Admin role only)_

| Method | Path | Description |
|--------|------|-------------|
| GET | `/tenants` | List all tenants |
| GET | `/tenants/{tenantId}` | Tenant detail with users and stats |
| POST | `/tenants/{tenantId}/activate` | Activate a tenant |
| POST | `/tenants/{tenantId}/deactivate` | Deactivate a tenant |
| PUT | `/tenants/{tenantId}/plan` | Update tenant plan |
| DELETE | `/tenants/{tenantId}` | Soft-delete a tenant |
| GET | `/tenants/{tenantId}/users` | List all users in a tenant |
| POST | `/tenants/{tenantId}/users/{userId}/activate` | Activate a user |
| POST | `/tenants/{tenantId}/users/{userId}/deactivate` | Deactivate a user |
| GET | `/stats` | Global platform statistics |
| GET | `/audit` | Audit log with filters (tenantId, userId, action, severity, date range, pagination) |
| GET | `/users` | All users across all tenants (paginated, filtered) |
| GET | `/users/stats` | Global user statistics |
| POST | `/users/{userId}/activate` | Activate user globally |
| POST | `/users/{userId}/deactivate` | Deactivate user globally |

### Clients — `/api/clients` _(Manager, Admin)_

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Paginated client list with filters |
| GET | `/search?query=` | Client autocomplete search |
| GET | `/{id}` | Client detail with addresses |
| GET | `/{id}/addresses` | Client address list |
| GET | `/{id}/deliveries` | Client delivery history |
| POST | `/` | Create client |
| POST | `/{id}/addresses` | Add address to client |
| PUT | `/{id}` | Update client |
| PUT | `/{clientId}/addresses/{addressId}` | Update address |
| PUT | `/{clientId}/addresses/{addressId}/set-default` | Set default address |
| DELETE | `/{id}` | Soft-delete client |
| DELETE | `/{clientId}/addresses/{addressId}` | Delete address |

### Deliveries — `/api/deliveries`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Any | Paginated delivery list with filters |
| GET | `/{id}` | Any | Delivery detail |
| GET | `/stats` | Any | Delivery aggregate stats |
| GET | `/unassigned?date=` | Manager, Admin | Deliveries not yet in a route for a date |
| GET | `/available-for-route?date=&currentRouteId=` | Manager, Admin | Deliveries eligible for route assignment |
| POST | `/` | Any | Create delivery |
| POST | `/{id}/duplicate` | Any | Duplicate delivery |
| POST | `/batch/status` | Any | Bulk status update |
| POST | `/batch/delete` | Manager, Admin | Bulk soft-delete |
| PUT | `/{id}` | Any | Update delivery |
| PATCH | `/{id}/status` | Any | Update single delivery status |
| DELETE | `/{id}` | Manager, Admin | Delete delivery |

### Routes — `/api/routes` _(Manager, Admin)_

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Paginated route list with filters |
| GET | `/{id}` | Route detail with team and deliveries |
| GET | `/{id}/download-sheet` | Download route sheet PDF |
| POST | `/` | Create route |
| POST | `/optimize` | Optimize delivery order via Google Directions |
| POST | `/recalculate-path` | Recalculate metrics after manual reorder |
| POST | `/{id}/teamMember` | Add driver to route team |
| POST | `/{id}/deliveries/{deliveryId}` | Assign delivery to route |
| POST | `/{id}/confirm` | Confirm route (locks deliveries) |
| POST | `/{id}/start` | Start route execution |
| POST | `/{id}/complete` | Mark route complete |
| POST | `/{id}/cancel` | Cancel route |
| PUT | `/{id}` | Update route |
| PUT | `/{id}/sequence` | Update delivery order in route |
| DELETE | `/{id}` | Delete route |
| DELETE | `/{id}/team/{driverId}` | Remove driver from team |
| DELETE | `/{id}/deliveries/{deliveryId}` | Remove delivery from route |

### Driver App — `/api/driver`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/route/today` | Today's assigned route for the authenticated driver |
| GET | `/deliveries/{id}` | Delivery detail (PII-limited: no internal notes or pricing) |
| POST | `/deliveries/{id}/validate` | Validate delivery with signature + optional photo |
| POST | `/route/{id}/start` | Start a route |
| POST | `/route/{id}/complete` | Complete a route |

### Tenant Settings — `/api/tenants`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/current` | Any | Current tenant info |
| PUT | `/company-info` | Manager, Admin | Update company details |
| PUT | `/legal-info` | Manager, Admin | Update legal details |
| PUT | `/logo` | Manager, Admin | Upload company logo (base64) |
| DELETE | `/logo` | Manager, Admin | Remove company logo |
| GET | `/depots/all` | Any | All active depots (for dropdowns) |
| GET | `/depots` | Any | Paginated depot list |
| GET | `/depots/{id}` | Any | Depot detail |
| POST | `/depots` | Manager, Admin | Create depot |
| PUT | `/depots/{id}` | Manager, Admin | Update depot |
| DELETE | `/depots/{id}` | Admin | Delete depot |
| POST | `/depots/{id}/set-default` | Manager, Admin | Set default depot |
| POST | `/depots/{id}/toggle-status` | Manager, Admin | Activate / deactivate depot |

### User Management — `/api/usermanagement` _(Manager, Admin)_

| Method | Path | Description |
|--------|------|-------------|
| GET | `/users` | List tenant users |
| POST | `/invite` | Invite user by email |
| POST | `/{userId}/activate` | Activate user |
| POST | `/{userId}/deactivate` | Deactivate user |
| POST | `/{userId}/restore` | Restore soft-deleted user |
| PUT | `/users/{userId}/role` | Change user role |
| DELETE | `/users/{userId}` | Soft-delete user |

### Other Endpoints

| Controller | Base | Auth | Description |
|------------|------|------|-------------|
| Stores | `/api/stores` | Manager, Admin | Store CRUD with filters |
| Vehicles | `/api/vehicles` | Manager, Admin | Vehicle CRUD with filters |
| Drivers | `/api/drivers` | Manager, Admin | Driver CRUD, availability check |
| TimeSlots | `/api/timeslots` | Manager, Admin | Delivery time window CRUD |
| Profile | `/api/profile` | Any | Get/update profile, change password, preferences |
| Dashboard | `/api/dashboard` | Manager, Admin | KPI stats, charts, notifications, events |
| Files | `/api/files/{*relativePath}` | Any | Serve uploaded delivery files (signatures, photos) |
| Geocoding | `/api/geocoding` | **Public** | Google Maps geocoding and Places autocomplete (dev/test endpoints) |

> **Note:** `GeocodingController` is marked `[AllowAnonymous]` and is intended for development testing only. It should be protected or removed before production deployment.

## Known Issues

The following issues were identified during code review and are tracked for remediation. Issues are ordered by severity.

### Critical (must fix before production)

1. **Cross-tenant delete via `FindAsync`** — `DeliveryService.DeleteDeliveryAsync` and `UpdateStatusAsync` use `context.Deliveries.FindAsync(id)`, which bypasses EF Core global query filters. An authenticated user can delete or update the status of any tenant's delivery by ID. **Fix:** replace with `FirstOrDefaultAsync(d => d.Id == id)`.

2. **Client-supplied FK IDs not validated** — `CreateDelivery` and `UpdateDelivery` accept `ClientId` and `ClientAddressId` from the request body and write them directly as foreign keys without verifying they belong to the current tenant. **Fix:** validate ownership via a filtered `AnyAsync` call before accepting the ID.

3. **Path traversal in file serving** — `FileStorageService.GetFileAsync` constructs an absolute path with `Path.Combine(_basePath, relativePath)` without verifying the result stays within `_basePath`. **Fix:** check that the resolved path starts with `_basePath` before reading.

4. **Real credentials in `appsettings.json`** — the Gmail SMTP password and Google Maps API key are committed as live values. **Immediate action required:** revoke both credentials, rotate them, and move them to user secrets / environment variables.

5. **Reference number race condition** — `DeliveryReferenceService` uses a random number, checks for existence, and returns — with no database lock between the check and the subsequent insert. Concurrent requests can generate duplicate reference numbers. **Fix:** add a `UNIQUE` index on `(TenantId, Reference)` and use an atomic sequential counter.

6. **Route reference prefix bug** — `RouteReferenceService` generates the initial reference with the prefix `FR-` instead of `RT-`. The fallback retry path uses `RT-` correctly, making the initial path always wrong. **Fix:** change `"FR-{dateString}-{sequentialNumber:D3}"` to `"RT-{dateString}-{sequentialNumber:D3}"`.

### High

7. **`DashboardService` captures `TenantId` at construction** — `private readonly int _tenantId = tenantService.GetTenantId()` runs at DI resolution time and throws if resolved outside an HTTP context.

8. **`GetStatsAsync` loads all deliveries into memory** — the delivery stats method calls `ToListAsync()` on all tenant deliveries and aggregates in C#. Should use server-side `GroupBy` / `Sum`.

9. **Google Maps API key appears in application logs** — `GeocodingService` logs the full request URL including the API key at `Information` level.

10. **`TenantService.GetCurrentUser()` is synchronous** — blocks a thread pool thread on a database call inside an async pipeline.

11. **`NullReferenceException` in `GetUnassignedDeliveriesAsync`** — `d.UrgentDriver.User.FullName` is accessed without a null guard in the projection (line 221); the urgent driver is optional.

12. **Failed login returns HTTP 200** — `AuthController.Login` always returns `Ok(result)` regardless of authentication outcome.

### Medium

13. **Missing `AsNoTracking()`** on the main delivery list query and several other read-only list queries.

14. **No `pageSize` maximum enforcement** on admin audit log and user list endpoints; callers can request unlimited rows.

15. **`DateTime.Today` used for UTC date comparisons** in `DashboardService`; mismatches by the server UTC offset during cross-midnight periods.

16. **No rate limiting** on `/api/auth/login`, `/api/auth/forgot-password`, or `/api/auth/register`.

17. **No server-side token revocation** — deactivating a user does not invalidate their existing JWT; they retain access for up to 8 hours.

18. **Audit log `Changes` field stores PII** — emails and roles appear in the `Changes` JSON blob of audit log entries.

19. **`GeocodingController` is unauthenticated** — the geocoding test endpoints are `[AllowAnonymous]` and should be restricted or removed in production.

20. **No geocoding result cache** — every delivery creation triggers a live Google Maps Geocoding API call even for previously geocoded addresses.

## Roadmap

The following features are specified but not yet implemented:

- **PWA mobile driver application** — native mobile experience for the `Livreur` role; the server-side API (`/api/driver/...`) is ready
- **Invoice PDF generation** — generate printable invoices with QuestPDF
- **Invoice email delivery** — send invoices to clients automatically
- **Stock / inventory module** — track goods associated with deliveries
- **Advanced reporting and analytics** — historical data, trend charts, export
- **SMS notifications** — delivery status updates via Twilio
- **Push notifications** — real-time driver alerts
- **Token revocation** — immediate effect when a user is deactivated

## Contributing

1. Fork the repository and create a feature branch from `main`
2. Follow the commit message convention: `[Module] Action: short description`
   - Example: `[Deliveries] Fix: cross-tenant delete via FindAsync`
   - Example: `[Routes] Add: recalculate metrics endpoint`
3. Business logic belongs in the Application layer, not in controllers or UI components
4. New entities must implement `ITenantEntity` and have a corresponding global query filter registered in `ApplicationDbContext.OnModelCreating`
5. New list endpoints must support `PageNumber` + `PageSize` pagination
6. All service methods must return `ResponseResult<T>` — do not throw exceptions to callers
7. Run existing tests before opening a pull request

## License

Proprietary — all rights reserved.
