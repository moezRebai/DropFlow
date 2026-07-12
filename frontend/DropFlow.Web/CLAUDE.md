# CLAUDE.md — DropFlow React Frontend

Lire ce fichier **en entier** au début de chaque session avant d'écrire du code.
Il contient le contexte projet, les conventions, et l'état d'avancement de la migration.

---

## Contexte

Frontend **React + TypeScript** de DropFlow. Il remplace l'ancien frontend Blazor Server
(supprimé du dépôt) ; c'est désormais le seul frontend web. L'API backend (.NET 9) est inchangée.

**API base URL** : définie dans `.env.local` → `VITE_API_URL`

---

## Stack (décisions figées)

| Rôle | Outil | Version |
|---|---|---|
| Bundler | Vite | latest |
| Framework | React | 18+ |
| Langage | TypeScript | strict mode |
| Composants | shadcn/ui | latest |
| Styles | Tailwind CSS | v4 |
| Routing | React Router | v6 |
| Server state | TanStack Query | v5 |
| Client state | Zustand | latest |
| Formulaires | React Hook Form + Zod | latest |
| HTTP | Axios | latest |
| Maps | @vis.gl/react-google-maps | latest |
| Charts | Recharts | latest |
| Drag & drop | @dnd-kit/core | latest |
| Icônes | Lucide React | latest |
| Types API | openapi-typescript (généré depuis swagger) | — |

Ne pas introduire de nouvelles librairies sans justification explicite.

---

## Structure des dossiers

```
frontend/DropFlow.Web/
├── public/
├── src/
│   ├── api/                  # Un fichier par domaine métier
│   │   ├── client.ts         # Instance Axios + intercepteurs JWT
│   │   ├── auth.ts
│   │   ├── deliveries.ts
│   │   ├── routes.ts
│   │   ├── clients.ts
│   │   ├── drivers.ts
│   │   ├── vehicles.ts
│   │   ├── stores.ts
│   │   ├── dashboard.ts
│   │   └── admin.ts
│   ├── components/
│   │   ├── ui/               # shadcn/ui (ne pas modifier manuellement)
│   │   └── shared/           # Composants réutilisables métier
│   │       ├── DataTable/
│   │       ├── PageHeader/
│   │       ├── StatusBadge/
│   │       └── ConfirmDialog/
│   ├── features/             # Un dossier par module métier
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── deliveries/
│   │   ├── routes/
│   │   │   └── wizard/       # 5 étapes du wizard
│   │   ├── clients/
│   │   ├── drivers/
│   │   ├── vehicles/
│   │   ├── stores/
│   │   ├── settings/
│   │   ├── profile/
│   │   └── admin/
│   ├── hooks/
│   ├── layouts/
│   │   ├── AppLayout.tsx
│   │   └── AuthLayout.tsx
│   ├── lib/
│   │   ├── axios.ts
│   │   └── queryClient.ts
│   ├── router/
│   │   ├── index.tsx
│   │   └── ProtectedRoute.tsx
│   ├── store/
│   │   └── authStore.ts      # Zustand : token, user, rôle
│   ├── types/
│   │   └── api.d.ts          # Généré par openapi-typescript
│   └── main.tsx
├── .env.local
├── vite.config.ts
├── tailwind.config.ts
└── tsconfig.json
```

---

## Conventions de code

- **Composants** : PascalCase, un composant par fichier
- **Hooks** : préfixe `use`, ex. `useDeliveries.ts`
- **API calls** : toujours via les fonctions dans `src/api/`, jamais d'Axios direct dans les composants
- **Typage** : tout ce qui vient de l'API utilise les types générés dans `src/types/api.d.ts`
- **Formulaires** : React Hook Form + Zod schema dans le même fichier que le formulaire
- **Queries** : clés TanStack Query centralisées dans `src/api/<domaine>.ts` (ex. `queryKeys.deliveries`)
- **Rôles** : vérifier via `authStore` — ne jamais hardcoder un rôle en string
- **Labels UI** : en **français** (comme le Blazor existant)
- **Pas de commentaires** sauf si la logique est non évidente

---

## Auth & Sécurité

- JWT stocké dans `localStorage` (même stratégie que Blazor)
- Intercepteur Axios : ajoute `Authorization: Bearer <token>` à chaque requête
- Intercepteur 401 : appelle `/auth/refresh`, rejoue la requête, ou redirige vers Login si refresh expiré
- `ProtectedRoute` : vérifie le token + le rôle requis, redirige sinon

**Rôles** (même hiérarchie que le backend) :
`Admin` > `Manager` > `Driver` | `Accountant` | `ReadOnly`

---

## Correspondance pages Blazor → React

### Légende
- `[ ]` Non commencé
- `[~]` En cours
- `[x]` Terminé et validé

### Auth
- `[~]` Login → `features/auth/LoginPage.tsx`
- `[~]` Register → `features/auth/RegisterPage.tsx`
- `[~]` AcceptInvitation → `features/auth/AcceptInvitationPage.tsx`
- `[~]` ForgotPassword → `features/auth/ForgotPasswordPage.tsx`
- `[~]` ResetPassword → `features/auth/ResetPasswordPage.tsx`
- `[~]` AccessDenied → `features/auth/AccessDeniedPage.tsx`

### Dashboard
- `[~]` ManagerDashboard (KPIs + sparklines, PulseBar, swim lanes, risk cards, charts) → `features/dashboard/DashboardPage.tsx`
- `[ ]` AdminDashboard → `features/admin/AdminDashboardPage.tsx`

### Clients
- `[~]` Clients list (paginée, recherche debouncée, delete) → `features/clients/ClientsPage.tsx`
- `[~]` Client detail panel (adresses + historique livraisons) → `features/clients/ClientDetailDialog.tsx`
- `[~]` Create / Edit client form → `features/clients/ClientFormDialog.tsx`
- `[~]` Address create / edit form → `features/clients/AddressFormDialog.tsx`

### Livraisons
- `[~]` Deliveries list + filtres + vue cartes → `features/deliveries/DeliveriesPage.tsx`
- `[~]` Create / Edit delivery form → `features/deliveries/CreateDeliveryPage.tsx`
- `[~]` Delivery detail → `features/deliveries/DeliveryDetailPage.tsx`
- `[ ]` Personal deliveries (rôle Driver) → `features/deliveries/PersonalDeliveriesPage.tsx` (placeholder)

### Routes / Tournées
- `[~]` Routes list → `features/routes/RoutesPage.tsx`
- `[~]` Route details → `features/routes/RouteDetailPage.tsx`
- `[~]` Edit Draft route (wizard pré-rempli) → `features/routes/EditRoutePage.tsx`
- `[~]` Wizard Step 1 — Info véhicule → `features/routes/wizard/Step1Info.tsx`
- `[~]` Wizard Step 2 — Sélection livraisons → `features/routes/wizard/Step2Deliveries.tsx`
- `[~]` Wizard Step 3 — Équipe → `features/routes/wizard/Step3Team.tsx`
- `[~]` Wizard Step 4 — Optimisation Google Maps → `features/routes/wizard/Step4Optimize.tsx`
- `[~]` Wizard Step 5 — Validation → `features/routes/wizard/Step5Validation.tsx`
- `[~]` Cancel route dialog → intégré dans RoutesPage.tsx (CancelConfirmModal) et RouteDetailPage.tsx

### Paramètres
- `[~]` Company info + legal → `features/settings/company/CompanySettings.tsx`
- `[~]` Depots CRUD (inline dans CompanySettings, onglet Dépôts) → `features/settings/company/CompanySettings.tsx`
- `[~]` Drivers list + dialog → `features/settings/drivers/DriversPage.tsx`
- `[~]` Vehicles list + dialog → `features/settings/vehicles/VehiclesPage.tsx`
- `[~]` Stores list + dialog → `features/settings/stores/StoresPage.tsx`
- `[~]` Time slots list + dialog → `features/settings/timeslots/TimeSlotsPage.tsx`
- `[~]` Team + invite → `features/settings/team/TeamPage.tsx`

### Profil
- `[~]` General tab + Sécurité → `features/profile/ProfilePage.tsx` (implémenté, validation navigateur en cours)

### Admin (Super Admin uniquement)
- `[ ]` Tenants list → `features/admin/TenantsPage.tsx`
- `[ ]` Tenant details → `features/admin/TenantDetailPage.tsx`
- `[ ]` Users management → `features/admin/UsersPage.tsx`
- `[ ]` Audit logs + filtres → `features/admin/AuditLogsPage.tsx`
- `[ ]` Change user role dialog → `features/admin/ChangeRoleDialog.tsx`
- `[ ]` Update tenant plan dialog → `features/admin/UpdatePlanDialog.tsx`

### Autres
- `[ ]` Help center → `features/help/HelpPage.tsx`
- `[ ]` Error page → `ErrorPage.tsx`

---

## Règle de validation d'une page

Une page est marquée `[x]` uniquement si :
1. Toutes les fonctionnalités de la page Blazor correspondante sont présentes
2. Les appels API fonctionnent (vérifié dans le navigateur, pas seulement en compilation)
3. La gestion d'erreur est en place (états loading / error / empty)
4. Les permissions par rôle sont respectées
5. Le responsive mobile est acceptable

---

## Gotchas techniques à connaître

- **Google Maps** : utiliser `optimize: true` pour l'optimisation initiale, `optimize: false` pour recalcul après réordonnancement manuel (même règle que Blazor)
- **Route wizard** : l'état du wizard est dans un Zustand store dédié `wizardStore.ts`, pas dans le state des composants
- **JWT refresh** : l'intercepteur doit éviter les boucles infinies (si le refresh lui-même retourne 401, logout immédiat)
- **Pagination** : toujours passer `page` et `pageSize` (max 500 côté serveur via `PaginatedFilter`)
- **TenantId** : jamais envoyé par le frontend — toujours résolu par le backend depuis le JWT
- **Dates** : l'API retourne des `DateTime` UTC — convertir en local pour l'affichage
- **shadcn CLI sur Windows** : crée `@\components\ui\` au lieu de `src\components\ui\` — déplacer manuellement après chaque `npx shadcn add`
- **Double scrollbar** : `html, body { height: 100%; overflow: hidden; }` + `#root { height: 100%; display: flex; flex-direction: column; }` dans `index.css`. Les pages gèrent leur propre scroll via `overflow-auto` sur le conteneur principal.
- **Formulaires longs** : utiliser sticky header + `flex-1 overflow-y-auto` sur le body — pas de sticky footer (conflits de positionnement)
- **Erreurs API** : le backend renvoie les erreurs dans `errors[]` (tableau), pas dans `message`. Toujours utiliser `err.response?.data?.errors?.[0] ?? err.response?.data?.message ?? err.message` pour afficher le message réel.
- **"Enseigne"** = Store dans le domaine métier (ce que le Blazor appelle "Enseigne", le backend appelle `Store`). Label UI = "Enseigne".
- **Créneau d'intervention** (`timeSlotId`) : optionnel pour TOUS les types de livraison (pas seulement Urgent). L'Urgent requiert seulement `urgentDriverId`.
- **Durée estimée** : dropdown avec valeurs prédéfinies (5, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 300, 360, 420, 480 min) — pas un champ texte libre.
- **Tournée** (`routeId`) : non modifiable depuis le formulaire livraison (pas dans `CreateDeliveryDto` / `UpdateDeliveryDto`). S'affiche en read-only si la livraison est déjà affectée à une route.
- **Sheet built-in close button** : ajouter `[&>button]:hidden` au `className` de `SheetContent` pour masquer le bouton X généré par shadcn et utiliser un bouton custom dans le header.
- **Modales imbriquées au-dessus d'un Sheet** : Sheet est à `z-50`. Les modales custom overlay sont à `z-50`. Pour une modale qui s'ouvre depuis un Sheet (ex. `AddressFormDialog`), utiliser `z-[60]`.
- **Hero action pill** : ne pas utiliser shadcn `<Button>`. Utiliser un `<button>` plain avec `bg-white/15 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/25 rounded-xl` pour rester cohérent avec le style glass du hero.
- **VIP badge sur fond blanc** : `bg-gradient-to-r from-amber-400 to-yellow-500 text-white shadow-sm rounded-full`.
- **VIP badge sur fond coloré (hero/header bleu)** : `bg-yellow-300/25 text-yellow-200 ring-1 ring-yellow-300/40 rounded-full`.
- **StatChip gold variant (hero)** : `bg-yellow-300/25 text-yellow-200 ring-1 ring-yellow-300/40` — pas `bg-amber-400/30 text-amber-100` (trop terne).
- **NotificationsPopover** : utiliser des `div` plain (pas `DropdownMenuItem`) pour les items de notification — sinon le clic sur le X de dismiss ferme tout le dropdown.
- **AppLayout notifications** : `refetchInterval: 60_000`, badge rouge sur la cloche, popover `w-80`, dismiss individuel via mutation `markRead`.
- **PUT 204 No Content** : `PUT /api/routes/{id}` retourne 204 sans body. Ne pas vérifier `result.succeeded` — axios lève une exception sur les vraies erreurs, donc `await routesApi.update(...)` suffit sans inspecter la réponse.
- **`durationToNextMinutes` sémantique** : malgré son nom, ce champ stocke le temps de trajet POUR ARRIVER à cette livraison depuis la précédente (ou depuis le dépôt pour la première). `durationToNextMinutes[0]` = dépôt → livraison 0. Calcul timeline : `arrival[i] = cursor + durationToNextMinutes[i]`, `departure[i] = arrival[i] + estimatedDuration[i]`.
- **Wizard edit mode** : `wizardStore` contient `editRouteId: number | null` et `editRouteReference: string`. `EditRoutePage` pré-remplit le store puis rend `<RouteWizard />`. Step5Validation détecte le mode édition via `!!wizard.editRouteId` et appelle `PUT` au lieu de `POST`.
- **TimeSpan pour l'API** : `startTime` en C# est un `TimeSpan`. Le wizard stocke `"HH:MM"`, mais l'API attend `"HH:MM:SS"`. Toujours ajouter `":00"` : `` `${wizard.startTime}:00` ``.
- **Timeline helpers partagés** : `computeTimeline()` et `minutesToTime()` sont exportés depuis `wizardStore.ts` — les utiliser dans Step4Optimize et Step5Validation (pas de duplication).
- **Babel JSX if/else** : les branches `if/else` sans accolades provoquent une erreur Babel en mode JSX strict. Toujours utiliser `{ }` : `if (cond) { ... } else { ... }`.
- **Google Maps — partitionnement des librairies dynamiques** : chaque classe vit dans une librairie nommée spécifique. Ne pas supposer qu'elles sont toutes dans `google.maps` :
  - `useMapsLibrary('maps')` → `Polyline`, `Polygon`, `Map`
  - `useMapsLibrary('marker')` → `AdvancedMarkerElement`, `Marker` (legacy)
  - `useMapsLibrary('core')` → `LatLng`, `LatLngBounds`, `SymbolPath`
  - `useMapsLibrary('geocoding')` → `Geocoder`
  - `useMapsLibrary('places')` → `Autocomplete`
- **`AdvancedMarkerElement`** : nécessite `mapId` sur le composant `<Map>`. Cleanup via `m.map = null` (pas `m.setMap(null)`).
- **`LatLngBoundsLiteral`** : utiliser l'objet plain `{ north, south, east, west }` dans `map.fitBounds()` — évite d'avoir besoin de la librairie `core` juste pour `new LatLngBounds()`.
- **Autocomplete Places dans les formulaires** : utiliser `Controller` de React Hook Form + `setValue(..., { shouldValidate: true })` dans le callback `place_changed`. Stocker `onPlaceSelect` / `onChange` dans `useRef` pour éviter les re-souscriptions à chaque render.
- **Nettoyage Autocomplete** : `google.maps.event.clearInstanceListeners(ac)` dans le `return` du `useEffect` (pas de méthode `.remove()` sur `Autocomplete`).
- **Coordonnées dépôt** : `departureLatitude` / `departureLongitude` ne sont pas stockées en base côté backend. Utiliser `useMapsLibrary('geocoding')` + `Geocoder` comme fallback pour résoudre l'adresse en coordonnées GPS avant de dessiner la carte.
- **`durationToNextMinutes` sémantique (rappel)** : malgré son nom "toNext", ce champ stocke le temps de trajet POUR ARRIVER à cette livraison (depuis la précédente ou depuis le dépôt). Afficher la distance/durée sur TOUTES les livraisons ayant `distanceToNextMeters != null` — pas uniquement `idx < length - 1`.
- **Map hauteur dans layout flex** : `flex flex-col h-full` sur le conteneur `RouteMapSection` + `flex-1 min-h-0` sur la `div` inner du `<Map>` pour que la carte remplisse la hauteur disponible sans déborder.

---

## Fichiers API créés (src/api/)

| Fichier | Contenu |
|---------|---------|
| `client.ts` | Instance Axios + intercepteurs JWT + helpers typés |
| `auth.ts` | Login, register, forgot/reset password, invitation |
| `deliveries.ts` | CRUD livraisons, enums DeliveryStatus/DeliveryType, STATUS_LABELS/COLORS, queryKeys |
| `clients.ts` | CRUD complet clients + adresses. Types : `ClientDto`, `ClientAddressDto`, `ClientDeliveryDto`, `PagedResult<T>`, `ClientFilterDto`. Backward-compat : `clientKeys.search` + `clientsApi.search` pour l'autocomplete livraisons |
| `stores.ts` | CRUD complet enseignes (+ getLookup conservé pour backward-compat livraisons) |
| `drivers.ts` | CRUD complet chauffeurs (+ getActive conservé pour backward-compat) |
| `vehicles.ts` | CRUD complet véhicules |
| `timeslots.ts` | CRUD complet créneaux (liste non paginée) |
| `routes.ts` | CRUD tournées, enums RouteStatus/TeamMemberRole, STATUS_LABELS/COLORS, optimizePath, recalculatePath, confirm/start/complete/cancel, `update(id, UpdateRouteDto)` — DTOs : `UpdateRouteDto`, `UpdateDeliverySequenceDto` |
| `dashboard.ts` | Stats KPI, livraisons du jour, risques, notifications, charts revenus/status/enseignes |
| `profile.ts` | Lecture profil, mise à jour, changement de mot de passe |
| `settings.ts` | Infos société (TenantsController) + CRUD dépôts (getAllDepots pour le wizard) |
| `team.ts` | Gestion membres équipe : invite, changeRole, activate/deactivate, delete |

---

## Avancement global

Dernière mise à jour : 2026-05-25 (Google Maps Places autocomplete sur stores + dépôts ; carte interactive sur RouteDetailPage avec géocodage fallback).

**Phase 1** `[x]` Scaffolding + infrastructure  
**Phase 2** `[~]` Auth + Layout — implémenté (notifications bell + user popover modernisés)  
**Phase 3** `[~]` Modules métier :
  - Livraisons `[~]`, Dashboard `[~]`, Profil `[~]`, Clients `[~]`
  - Paramètres `[~]` (6 sous-pages : Company, Dépôts, Drivers, Vehicles, Stores, TimeSlots, Team)
  - Routes/Tournées `[~]` (list, detail, wizard 5 étapes avec optimisation Google Maps + DnD, édition brouillon via EditRoutePage)
  - Reste : Admin (4 pages)
