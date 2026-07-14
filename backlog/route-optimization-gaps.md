# Route Optimization — Gap Analysis & Backlog

**Status:** 🔄 Partially Implemented (per CLAUDE.md)
**Investigated:** 2026-07-12
**Scope:** `backend/DropFlow.Application/Services/Routes/RouteService.cs`, `backend/.../GeocodingService.cs`, `RoutesController.cs`, `frontend/DropFlow.Web/src/features/routes/wizard/Step2Deliveries.tsx`, `Step4Optimize.tsx`, `RouteDetailPage.tsx`

This document tracks what's missing to consider route optimization "done." Findings come from direct source inspection (CodeGraph index was unavailable during this investigation — verify file:line references before acting, code may have moved).

---

## Critical

### 1. No real optimization algorithm — 100% delegated to Google's black box
- **File:** `RouteService.cs:835` (`OptimizeRouteAsync`), line `956` (the actual Google call)
- **Issue:** There is no local optimization logic (no TSP heuristic, nearest-neighbor, 2-opt, etc.). The method delegates entirely to Google Directions `optimize:true`.
- **Impact:** If the Google API key is missing, quota is exceeded, or the call fails for any reason, there is **no fallback** — the method returns a failure result straight to the UI. Route optimization is 100% dependent on external API availability.
- **Fix:** Decide if a local fallback heuristic is worth building, or explicitly accept the Google-only dependency and harden the failure path (see #6).

### 2. Google's optimizer has zero awareness of business constraints
- **File:** `RouteService.cs` — `OptimizeRouteAsync` (835), `RecalculateRouteMetricsAsync` (~1141)
- **Issue:** Google's `optimizeWaypoints` only minimizes travel distance/time. Nothing in either method passes or validates:
  - Delivery `TimeSlot` windows
  - Driver availability/working hours
  - Vehicle capacity (`Vehicle.MaxDeliveries`)
- **Confirmed:** `Vehicle.MaxDeliveries` is checked *only* in `AddDeliveryAsync` (line 580) — never during optimization or recalculation.
- **Impact:** A manager can "optimize" 40 deliveries onto a 10-capacity vehicle with no warning at any point in the flow.
- **Fix:** Either enforce these constraints server-side before/after calling Google, or explicitly document that DropFlow's "optimization" is pure distance/time only (a product decision, not just a bug).

### 3. No handling of Google's 25-waypoint API limit
- **File:** `RouteService.cs:945-956` (initial optimize), `1123-1133` (recalculate); `Step2Deliveries.tsx` (frontend selection)
- **Issue:** No pre-check on `deliveries.Count` anywhere in the stack before building the waypoints string. If the limit is exceeded, Google returns `MAX_WAYPOINTS_EXCEEDED`.
- **Compounding bug:** `OptimizeRouteAsync`'s multi-delivery branch (lines 964-969) **never checks `googleDirectionsResponse.Status`** — it only checks whether `Routes` is null/empty. So instead of surfacing the real cause, the user sees the generic, misleading message *"Aucun itinéraire trouvé. Vérifiez les adresses."*
- **Note:** `RecalculateRouteMetricsAsync` (line 1141) *does* check `Status != "OK"` — the two near-identical methods have diverged and now have inconsistent error handling.
- **Fix:**
  1. Cap delivery selection at 25 in `Step2Deliveries.tsx` with a clear UI message.
  2. Add the same cap server-side as a defense-in-depth check.
  3. Make `OptimizeRouteAsync` check `.Status` the same way `RecalculateRouteMetricsAsync` does, and surface Google's actual error/status code to the frontend.

### 4. `RecalculateMetricsInternalAsync` is a disguised stub
- **File:** `RouteService.cs:1364-1384`
- **Issue:** Hardcodes `totalDistance: 0` with the comment `// à calculer avec Google Maps` (line 1376). It never actually calls Google Directions.
- **Trigger:** Invoked from `AddDeliveryAsync` / `RemoveDeliveryAsync` when editing an existing **Draft** route's delivery list.
- **Impact:** Editing deliveries on an existing route silently zeroes `TotalDistance`. Duration is only a naive sum of `EstimatedDurationMinutes` — not real travel time. This looks like working code but silently produces wrong data.
- **Fix:** Implement the real Google Directions call here, matching the pattern used in `OptimizeRouteAsync`/`RecalculateRouteMetricsAsync`.

---

## High

### 5. Two dead/broken endpoints
- **`POST /api/routes/{id}/recalculate`** — `RoutesController.cs:157-165`. Fully commented out; unconditionally returns `Ok()` and does nothing.
- **`PUT /api/routes/{id}/sequence`** → `RouteService.UpdateSequenceAsync` (`RouteService.cs:629-659`). Never called from the frontend (verified: no `sequence`/`updateSequence` call site in `src/api/routes.ts` or any component). Even if wired up, it only persists `SequenceOrder` — it never recalculates distance/duration/`EstimatedArrivalTime`, so using it today would silently desync route metrics from the displayed sequence.
- **Fix:** Decide to either finish these endpoints properly or delete them — leaving dead/broken code that looks functional is a trap for future changes.

### 6. No resilience around the Google Directions API
- **File:** `GeocodingService.cs` (entire file, notably lines 109, 169)
- **Issue:** Single `HttpClient.GetAsync` call with a 30s `CancellationTokenSource`. No retry, no backoff, no circuit breaker (no Polly), no explicit handling of `OVER_QUERY_LIMIT` or HTTP 429.
- **Impact:** Any transient Google API hiccup fails the whole optimization/recalculation immediately with no retry.
- **Fix:** Add a retry/backoff policy (e.g. Polly) around Directions/Geocoding calls, with explicit handling for rate-limit responses.

### 7. No re-optimization path once a route leaves Draft
- **File:** `RouteService.cs` — `AddDeliveryAsync`, `RemoveDeliveryAsync`, `UpdateSequenceAsync` all gate on `Status == Draft`
- **Issue:** There is no supported flow to re-optimize a `Confirmed`/`InProgress` route — e.g. after a delay, a vehicle breakdown, or a last-minute delivery swap.
- **Impact:** Once a route is confirmed, dispatchers have no way to adapt it through the optimization system; any change has to happen outside the app or by cancelling/recreating the route.
- **Fix:** Either build a supported re-optimize flow for non-Draft statuses, or explicitly document this as an intentional lock (product decision).

---

## Medium

### 8. No caching/dedup of Directions requests
- **File:** `GeocodingService.cs`, `RouteService.cs` (all optimize/recalculate call sites)
- **Issue:** Every optimize or recalculate call re-hits the paid Google API, even for an unchanged waypoint set.
- **Impact:** Unnecessary API billing, slower response times. (Note: this overlaps with previously-tracked code review issue #11 — geocoding result caching — which was deferred.)
- **Fix:** Cache Directions responses keyed by waypoint set (with reasonable TTL), at minimum for repeated recalculation calls within the same editing session.

### 9. Inconsistent default service duration fallback
- **File:** `RouteService.cs` — `15` min in `OptimizeRouteAsync` (line 915) and `RecalculateRouteMetricsAsync` (lines 1037, 1188) vs. `30` min in `RecalculateMetricsInternalAsync` (line 1373)
- **Issue:** Same conceptual value (default delivery service duration when `EstimatedDurationMinutes` isn't set) has two different hardcoded values across near-identical methods.
- **Fix:** Extract to a single named constant/config value and use consistently.

### 10. Frontend: silent failure on manual-reorder recalculation
- **File:** `Step4Optimize.tsx`
- **Issue:**
  - Drag-and-drop reorder **is** correctly wired to recalculation (`handleDragEnd` → `recalculateMutation.mutate` → `POST /api/routes/recalculate-path`, lines 177-188).
  - The optimize button has a loading state (lines 227-238), but the drag-reorder recalculation does not — `recalculateMutation.isPending` is never surfaced in the UI (no spinner/disabled state), so a user can drag again mid-request.
  - The optimize mutation has an error state/toast (lines 286-291); `recalculateMutation` has **no `onError` handler at all** — a failed recalculate-after-drag fails silently, leaving stale metrics displayed to the user as if they were current.
- **Fix:** Add pending/disabled state and an error toast to the recalculate mutation, matching the optimize mutation's UX.

### 11. Frontend: no delivery-count guard before reaching the waypoint limit
- **File:** `Step2Deliveries.tsx`
- **Issue:** No cap on the number of selected deliveries, and no use of the selected vehicle's `MaxDeliveries` to constrain selection.
- **Impact:** Nothing stops a manager from selecting well beyond Google's 25-waypoint limit before reaching Step4, where it then fails with a misleading error (see #3).
- **Fix:** Enforce both the vehicle capacity and the 25-waypoint cap at selection time, with inline UI feedback.

### 12. No re-sequencing UI outside the wizard
- **File:** `RouteDetailPage.tsx` (~lines 505-573)
- **Issue:** Sequence is rendered **read-only** on the route detail page. There's no re-optimize/re-sequence entry point once a route exists outside the creation/edit wizard.
- **Fix:** Related to #7 — depends on the product decision about whether non-Draft routes should be re-optimizable at all.

---

## Low

### 13. Zero test coverage
- **Issue:** No test project exists anywhere in the repo (confirmed via filesystem search). `OptimizeRouteAsync`, `RecalculateRouteMetricsAsync`, and `RecalculateMetricsInternalAsync` are entirely untested.
- **Fix:** Add integration tests per `testcontainers-integration-tests` skill conventions, at minimum covering: single delivery, multi-delivery optimize, manual reorder + recalculate, waypoint-limit exceeded, Google API failure.

---

## Verified still correct (documented gotchas that hold up)

- **`optimize:true` for initial optimization / `optimize:false` for manual reorder** — correctly implemented (`RouteService.cs:956` vs `:1133`; `Step4Optimize.tsx:139-175`).
- **Single-delivery separate `GetDirectionsAsync` path** — correctly implemented (`RouteService.cs:868-943`), and is actually the most robust of the three optimization code paths (properly validates `Routes`/`Legs`).

---

## Suggested fix order

1. **#3** — Waypoint limit cap + fix `OptimizeRouteAsync` status check (small, high-value, prevents confusing user-facing errors)
2. **#4** — Implement real Google call in `RecalculateMetricsInternalAsync` (silently wrong data is the worst kind of bug)
3. **#2** — Decide and enforce (or explicitly punt on) business constraints (capacity/time windows) — this is a product decision, get alignment first
4. **#5** — Clean up the two dead endpoints
5. **#6** — Add retry/backoff to the Google client
6. **#10, #11** — Frontend UX fixes (loading/error state, selection guard)
7. **#7, #12** — Re-optimization flow for non-Draft routes (larger scope, needs design)
8. **#8, #9** — Caching + constant cleanup
9. **#13** — Test coverage, ideally added alongside each fix above rather than as a separate pass
