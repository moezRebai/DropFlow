# DROPFLOW MOBILE — Spécification Technique Complète
# Document destiné à Claude Code pour génération du projet .NET MAUI

---

## 1. CONTEXTE & OBJECTIF

DropFlow est une application SaaS multi-tenant de gestion de livraisons de meubles pour entreprises de logistique. Le backend est un ASP.NET Core 9 Web API avec JWT, EF Core 9, SQL Server 2022, architecture Clean Architecture (DDD).

**Objectif** : Créer une application mobile Android interne (.NET MAUI) dédiée aux livreurs, distribuée par APK signée (hors Google Play Store).

**L'app permet au livreur de** :
- Se connecter avec ses identifiants
- Consulter sa feuille de route du jour (tournée + livraisons ordonnées)
- Voir le détail de chaque livraison (client, adresse, produits, notes)
- Appeler le client directement (intent téléphone)
- Naviguer vers le client (Google Maps intent)
- Valider une livraison (signature tactile + photo + commentaire)
- Démarrer et terminer sa tournée
- Voir un message d'erreur global en cas de perte de connexion

---

## 2. STACK TECHNIQUE

```
Plateforme : Android uniquement
Framework  : .NET 9 MAUI
Architecture : MVVM avec CommunityToolkit.Mvvm
Signature  : SkiaSharp (pad tactile avec lissage quadratique)
HTTP       : HttpClient natif
Auth       : JWT stocké dans SecureStorage
Offline    : Détection connectivité + message erreur (PAS de cache SQLite)
```

### NuGet Packages requis :
- `CommunityToolkit.Mvvm` (8.4.0+) — [ObservableProperty], [RelayCommand], [QueryProperty]
- `CommunityToolkit.Maui` (10.0.0+)
- `SkiaSharp.Views.Maui.Controls` (3.116.1+) — Signature pad
- `System.Text.Json` (9.0.0)
- `System.IdentityModel.Tokens.Jwt` — Parsing JWT pour vérifier expiration

---

## 3. ARCHITECTURE DU PROJET

```
DropFlow.Mobile/
├── DropFlow.Mobile.csproj           # Android only, net9.0-android
├── MauiProgram.cs                   # DI registration
├── App.xaml(.cs)                    # Entry point, converters globaux
├── AppShell.xaml(.cs)               # Shell navigation, routes
│
├── Models/
│   └── ApiModels.cs                 # Tous les DTOs (miroir du backend)
│
├── Services/
│   ├── ApiService.cs                # HTTP client centralisé (toutes les requêtes)
│   ├── AuthStorageService.cs        # JWT + UserInfo dans SecureStorage
│   └── ConnectivityService.cs       # Détection réseau temps réel
│
├── ViewModels/
│   ├── BaseViewModel.cs             # Loading, erreurs, connectivity, session expiry
│   ├── LoginViewModel.cs            # Login flow
│   ├── RouteViewModel.cs            # Écran principal : tournée du jour
│   ├── DeliveryDetailViewModel.cs   # Détail livraison
│   └── ValidationViewModel.cs       # Signature, photo, commentaire
│
├── Views/
│   ├── LoginPage.xaml(.cs)
│   ├── RoutePage.xaml(.cs)
│   ├── DeliveryDetailPage.xaml(.cs)
│   └── ValidationPage.xaml(.cs)
│
├── Controls/
│   └── SignaturePadView.cs          # Custom SkiaSharp signature control
│
├── Converters/
│   └── ValueConverters.cs           # InvertBool, StatusColor, Currency, TimeFormat, etc.
│
└── Resources/Styles/
    ├── Colors.xaml                   # Thème DropFlow
    └── Styles.xaml                   # Styles globaux
```

### Pattern MVVM :
- Tous les ViewModels utilisent `CommunityToolkit.Mvvm` (source generators)
- `[ObservableProperty]` pour les propriétés bindées
- `[RelayCommand]` pour les actions
- `[QueryProperty]` pour les paramètres de navigation Shell
- Navigation via `Shell.Current.GoToAsync()` avec paramètres typés
- DI natif MAUI : tous les services et VMs enregistrés dans `MauiProgram.cs`

---

## 4. API ENDPOINTS CONSOMMÉS

Le backend expose ces endpoints spécifiques livreur (prefix `/api/driver/`) :

### 4.1 Authentification
```
POST /api/auth/login
Body: { "email": string, "password": string, "tenantId": int }
Response 200: { "success": true, "token": "eyJ...", "user": { "id", "email", "firstName", "lastName", "role", "tenantId", "tenantName" } }
Response 400: { "success": false, "message": "Email ou mot de passe incorrect" }
```

### 4.2 Tournée du jour
```
GET /api/driver/route/today
Headers: Authorization: Bearer {jwt}
Response 200: {
  "hasRoute": true/false,
  "message": "Aucune tournée prévue aujourd'hui" (si hasRoute=false),
  "route": {
    "routeId": int,
    "reference": "T-20260115-001",
    "date": "2026-01-15",
    "vehicleName": "Renault Master 20m³",
    "departureAddress": "Entrepôt principal",
    "startTime": "08:00:00",
    "estimatedEndTime": "17:00:00",
    "status": int (0=Draft, 1=Confirmed, 2=InProgress, 3=Completed, 4=Cancelled),
    "statusDisplay": "Confirmée",
    "totalDeliveries": 8,
    "totalDistanceKm": 45.3,
    "totalDurationMinutes": 195,
    "teamMembers": ["Jean Dupont", "Marc Lefebvre"],
    "deliveries": [ ... liste ordonnée par sequenceOrder ... ]
  }
}
```

### 4.3 Livraison (item dans la liste)
```json
{
  "id": 5492,
  "sequenceOrder": 1,
  "reference": "DL-20260115-0042",
  "clientName": "Claudine LOUIS",
  "city": "BETHENY",
  "zipCode": "51450",
  "timeSlotName": "10:00 - 12:00",
  "estimatedArrivalTime": "08:15:00",
  "status": 2,
  "statusDisplay": "En cours",
  "withAssembly": true,
  "totalPackages": 2,
  "hasClientPayment": true,
  "isClientAbsent": false,
  "isValidated": false
}
```

### 4.4 Détail livraison
```
GET /api/driver/deliveries/{id}
Headers: Authorization: Bearer {jwt}
```

### 4.5 Validation livraison
```
POST /api/driver/deliveries/{id}/validate
Headers: Authorization: Bearer {jwt}
Body: {
  "signatureBase64": "iVBORw0KGgo..." (PNG base64, obligatoire sauf si isClientAbsent),
  "photoBase64": "..." (PNG/JPEG base64, optionnel),
  "comment": "RAS, livraison effectuée" (optionnel),
  "isClientAbsent": false
}
Response 200: { "message": "Livraison validée avec succès" }
Response 400: { "message": "Signature obligatoire sauf en cas de client absent" }
```

### 4.6 Démarrer tournée
```
POST /api/driver/route/{id}/start
```
→ Passe la Route de Confirmed → InProgress
→ Passe TOUTES les livraisons Confirmed → InProgress

### 4.7 Terminer tournée
```
POST /api/driver/route/{id}/complete
```
→ Passe la Route → Completed

---

## 5. ÉCRANS & COMPORTEMENTS

### 5.1 LoginPage
- Logo texte "📦 DropFlow" + "Espace Livreur"
- Champ email, mot de passe (toggle), bouton connexion
- Si token valide au lancement → redirect vers //route

### 5.2 RoutePage (écran principal)
- Header : nom livreur + entreprise + logout
- Résumé tournée : référence, statut, métriques, progression
- Bouton Démarrer (si Confirmed) / Terminer (si InProgress)
- Liste livraisons (CollectionView) avec tap → détail

### 5.3 DeliveryDetailPage
- Sections : Client, Adresse, Détails, Paiement, Produits, Notes
- Appel client direct, navigation Google Maps
- Bouton valider en bas (si livraison validable)

### 5.4 ValidationPage
- Toggle client absent
- Signature pad SkiaSharp (si client présent)
- Photo optionnelle (MediaPicker)
- Commentaire optionnel
- Bouton confirmer

---

## 6. SIGNATURE PAD (SkiaSharp Custom Control)

- `ContentView` + `SKCanvasView`
- Interpolation quadratique (lissage), anti-aliasing
- Export PNG base64 600×300px avec auto-scaling
- Event `SignatureChanged` après chaque trait
- Méthode `Clear()`

---

## 7. GESTION DES ERREURS

- `BaseViewModel.ExecuteAsync()` gère : `NoConnectivityException`, `SessionExpiredException`, `HttpRequestException`, `TaskCanceledException`
- Auto-logout si 401
- Bannière orange si hors ligne

---

## 8. THÈME

- Primary: #1565C0, Accent: #00BCD4
- Boutons 52-56px, CornerRadius 12, font 16-18px
- Cards : Frame blanc, shadow, radius 12

---

## 9. NAVIGATION (Shell)

```
//login → LoginPage
//route → RoutePage
  delivery → DeliveryDetailPage
    delivery/validation → ValidationPage
```

---

## 10. SÉCURITÉ

- JWT dans SecureStorage (Android Keystore)
- Vérification expiration avant chaque appel
- Certificats auto-signés acceptés en DEBUG uniquement
- Permissions : INTERNET, ACCESS_NETWORK_STATE, CAMERA

---

## 11. BUILD APK

```bash
# Keystore (une fois)
keytool -genkey -v -keystore dropflow.keystore -alias dropflow -keyalg RSA -keysize 2048 -validity 10000

# Build release
dotnet publish -f net9.0-android -c Release \
  /p:AndroidKeyStore=true \
  /p:AndroidSigningKeyStore=dropflow.keystore \
  /p:AndroidSigningKeyAlias=dropflow \
  /p:AndroidSigningKeyPass=VotreMotDePasse \
  /p:AndroidSigningStorePass=VotreMotDePasse
```

---

## 12. BACKEND ENDPOINTS À IMPLÉMENTER

Ces endpoints driver doivent être ajoutés au backend ASP.NET Core :
- `GET /api/driver/route/today`
- `GET /api/driver/deliveries/{id}`
- `POST /api/driver/deliveries/{id}/validate`
- `POST /api/driver/route/{id}/start`
- `POST /api/driver/route/{id}/complete`

Chaque endpoint vérifie que l'utilisateur authentifié a le rôle `Driver` et n'accède qu'à ses propres données (TenantId + DriverId).
