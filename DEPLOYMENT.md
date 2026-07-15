# Déploiement DropFlow.Api (Plesk / IIS)

Ce document décrit la procédure de déploiement de l'API vers l'hébergement Plesk
(adaptivewebhosting.com). Il ne contient **aucun secret** — les valeurs réelles
vivent dans le coffre-fort (Bitwarden), voir section [Secrets](#secrets).

## Comment la config est chargée

ASP.NET Core empile 3 sources, chacune écrasant la précédente :

1. `appsettings.json` — valeurs par défaut communes à tous les environnements
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` — overrides par environnement
   (`Development` en local, `Production` sur Plesk)
3. Variables d'environnement — priorité la plus haute, définies dans `web.config`

Aucun des fichiers `appsettings*.json` ne doit contenir de secret réel — ils
portent des placeholders (`WILL_BE_OVERRIDDEN_BY_USER_SECRETS` en dev,
`WILL_BE_OVERRIDDEN_BY_ENV_VAR` en prod). Les vraies valeurs sont injectées :
- en local, via `dotnet user-secrets`
- en prod, via `<environmentVariables>` dans le `web.config` déployé

`DependencyInjection.cs` choisit aussi la base de données selon l'environnement :
`DefaultConnection` (Postgres local) en `Development`, `NeonConnection` partout
ailleurs — dev et prod utilisent des bases séparées.

## Procédure de publish

```
dotnet publish backend/DropFlow.Api/DropFlow.Api.csproj -c Release -o publish/api
```

Le dossier `publish/api/` est ignoré par git (`.gitignore`). C'est là, et
seulement là, qu'on peut mettre les vraies valeurs de secrets — jamais dans
`backend/DropFlow.Api/web.config` (celui-là est suivi par git).

Le `web.config` source contient déjà `ASPNETCORE_ENVIRONMENT=Production` (sans
secret, safe à committer) — ça survit à chaque republish grâce à la
transformation du SDK .NET.

## Remplir `publish/api/web.config`

Après le publish, éditer `publish/api/web.config` et ajouter dans
`<aspNetCore><environmentVariables>` (valeurs à récupérer dans le coffre-fort) :

| Variable | Correspond à | Notes |
|---|---|---|
| `ConnectionStrings__NeonConnection` | `ConnectionStrings:NeonConnection` | chaîne de connexion complète |
| `JwtSettings__SecretKey` | `JwtSettings:SecretKey` | rotation = déconnecte tous les utilisateurs actifs |
| `SuperAdmin__Password` | `SuperAdmin:Password` | ne sert que si la base est recréée (seed initial) — voir [Rotation](#rotation-des-secrets) |
| `EmailSettings__Smtp__Username` | `EmailSettings:Smtp:Username` | login SMTP Brevo |
| `EmailSettings__Smtp__Password` | `EmailSettings:Smtp:Password` | clé SMTP Brevo (pas le mot de passe du compte) |
| `EmailSettings__Smtp__FromEmail` | `EmailSettings:Smtp:FromEmail` | doit être un expéditeur vérifié dans Brevo |
| `Google__MapsApiKey` | `Google:MapsApiKey` | à restreindre par domaine (referrer) dans Google Cloud Console |

## Déploiement sur Plesk

1. Uploader le contenu de `publish/api/` (File Manager ou FTP) vers le
   répertoire du site.
2. Vérifier que `web.config` déployé contient bien toutes les variables
   ci-dessus avec les vraies valeurs.
3. Redémarrer le pool applicatif (Plesk → Websites & Domains → [domaine] → IIS
   → Restart) pour que les nouvelles variables soient prises en compte.

## Vérification post-déploiement

- Se connecter avec un compte existant (JWT + DB OK)
- Tester `POST /api/auth/forgot-password` avec un compte réel → email reçu
  via Brevo
- Tester l'invitation d'un utilisateur → email d'invitation reçu
- Consulter les logs de démarrage : une ligne `Database connected: Neon
  (host=..., environment=Production)` confirme la bonne base de données

## Secrets

Les valeurs réelles (chaîne Neon, clé JWT, mot de passe SuperAdmin, clé SMTP
Brevo, clé Google Maps) sont stockées dans le coffre-fort (Bitwarden — secure
notes, une par secret). Ne jamais les committer, ni dans `appsettings*.json`
ni dans `backend/DropFlow.Api/web.config`.

## Rotation des secrets

À faire périodiquement, ou immédiatement si une valeur a fuité :

1. **Neon DB** : dashboard Neon → reset password sur le rôle `neondb_owner` →
   mettre à jour `ConnectionStrings__NeonConnection` dans `web.config` +
   coffre-fort → redéployer.
2. **Clé JWT** : générer une nouvelle valeur aléatoire (64 octets, base64) →
   mettre à jour `JwtSettings__SecretKey` dans `web.config` + coffre-fort →
   redéployer. ⚠️ Déconnecte tous les utilisateurs actifs.
3. **Mot de passe SuperAdmin** : le seed (`DatabaseExtensions.cs`) ne crée le
   compte que s'il n'existe pas déjà — changer `SuperAdmin__Password` dans
   `web.config` **ne change pas** le mot de passe d'un compte existant. Il
   faut se connecter avec l'ancien mot de passe et utiliser
   `PUT /api/profile/change-password`, puis mettre à jour `web.config` +
   coffre-fort pour que le seed reste cohérent en cas de recréation de base.
4. **Clé SMTP Brevo / clé Google Maps** : pas de rotation périodique
   nécessaire sauf compromission — pour Google Maps, préférer une restriction
   par domaine (referrer) à une régénération.
