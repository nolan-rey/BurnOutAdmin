# API BurnOut / Call of Phoenix — Spécification unifiée (mobile + admin)

> Doc unique destinée à **Samy** (mainteneur API PHP Slim Skeleton + MySQL).
> Source consolidée de 2 specs distinctes :
> - **Mobile** (Flutter, `burnout_application`) — utilisé par les **pratiquants**
> - **Admin** (MAUI, `BurnOutAdmin`) — utilisé par **coaches / admins**
>
> Objectif : **UNE seule API** qui sert les 2 clients sans duplication.
>
> Toute divergence est explicitement signalée et **une recommandation est proposée**.
> Les routes/champs nouveaux (non encore implémentés côté API) sont marqués 🆕.

## Légende

| Symbole | Signification |
|---|---|
| 📱 | utilisé par l'app **mobile** |
| 🖥 | utilisé par l'app **admin** |
| 📱🖥 | utilisé par **les deux** |
| 🔒 | nécessite header `Authorization: Bearer <jwt>` |
| 🆕 | route ou champ **nouveau** (pas encore implémenté côté API) |
| ⚠️ **DÉCISION REQUISE** | conflit mobile/admin à trancher par Samy |
| ℹ️ Divergence | différence de schéma déjà résolue par recommandation |

---

## Sommaire

- [0. Décisions critiques à prendre par Samy (à lire en premier)](#0-décisions-critiques-à-prendre-par-samy)
- [1. Conventions générales](#1-conventions-générales)
- [2. Authentification](#2-authentification)
- [3. Utilisateurs (admin/coach)](#3-utilisateurs-admincoach)
- [4. Clients](#4-clients)
- [5. Coaches](#5-coaches)
- [6. Événements](#6-événements)
- [7. Défis (challenges)](#7-défis-challenges)
- [8. Exercices](#8-exercices)
- [9. Programmes](#9-programmes)
- [10. Séances (builder + assignations + réalisations)](#10-séances-builder--assignations--réalisations)
- [11. Posts (feed) & commentaires](#11-posts-feed--commentaires)
- [12. Gamification (rang, points, classement, totem)](#12-gamification-rang-points-classement-totem)
- [13. Accès NFC](#13-accès-nfc)
- [14. Dashboard (admin)](#14-dashboard-admin)
- [15. Statut API](#15-statut-api)
- [16. Schéma BDD unifié (MySQL)](#16-schéma-bdd-unifié-mysql)
- [17. Récapitulatif des clés d'ID renvoyées à la création](#17-récapitulatif-des-clés-did-renvoyées-à-la-création)
- [18. Ordre d'implémentation recommandé pour Samy](#18-ordre-dimplémentation-recommandé-pour-samy)
- [19. Notes finales & pièges à éviter](#19-notes-finales--pièges-à-éviter)

---

## 0. Décisions critiques à prendre par Samy

> **À LIRE EN PREMIER** — chacun de ces points bloque l'une des 2 apps si non tranché.
> Recommandation : valider rapidement les 6 décisions avant d'écrire le code.

### 0.1 ⚠️ **DÉCISION REQUISE** — Format de body sur `/users/login` et `/users/register`

| Client | Content-Type | Body |
|---|---|---|
| 📱 Mobile (Flutter) | `application/x-www-form-urlencoded` | `email=foo@x.com&password=...` |
| 🖥 Admin (MAUI) | `application/json` | `{"email":"foo@x.com","password":"..."}` |

**Recommandation** : **accepter LES DEUX** côté Slim. La méthode `$request->getParsedBody()` retourne déjà un array peuplé que le body soit form-encoded OU JSON, à condition d'enregistrer le `BodyParsingMiddleware`. Aucune ligne supplémentaire à écrire si déjà configuré.

**Alternative si refus** : harmoniser sur JSON (plus standard), Flutter doit être patché (`ApiService.login` → `Content-Type: application/json`, `jsonEncode({...})`).

**Impact si refusé** : un des 2 clients ne pourra pas se logger (HTTP 400 sur parsing).

---

### 0.2 ⚠️ **DÉCISION REQUISE** — Base URL unique

| Client | URL |
|---|---|
| 📱 Mobile | `http://98.66.235.57` |
| 🖥 Admin | `http://apiburnout.duckdns.org/` |

**Recommandation** : choisir **`http://apiburnout.duckdns.org/`** (DNS, plus durable que l'IP). Migrer mobile via `.env`.

**Impact si non harmonisé** : tolérable court terme (les 2 URL pointent vers la même API), mais une seule des 2 sera mise en HTTPS plus tard.

**À prévoir** : HTTPS via Let's Encrypt sur le DNS, et redirection 301 depuis l'IP.

---

### 0.3 ⚠️ **DÉCISION REQUISE** — Mécanisme de refresh JWT

| Client | Comportement |
|---|---|
| 📱 Mobile | Utilise **Firebase Auth** : refresh via `securetoken.googleapis.com/v1/token?key=...` puis rejoue la requête avec nouveau JWT |
| 🖥 Admin | Pas de refresh : sur 401 → déconnexion forcée |

**Recommandation** :
1. Conserver **Firebase Auth** comme source de vérité (les 2 clients peuvent l'utiliser).
2. Côté API : valider le JWT Firebase via la `firebase/jwt` lib PHP ou via une introspection upstream (le mobile fournit déjà ce pattern).
3. Admin doit **adopter** le flux Firebase (ou alors un endpoint `/auth/refresh` natif renvoyant un JWT custom doit être ajouté, et le mobile bascule dessus).

**Décision proposée** : **Firebase pour les 2**. Admin a déjà un `RegisterRequestDto` qui produit un `uid` Firebase — la cohérence est plus simple.

---

### 0.4 ⚠️ **DÉCISION REQUISE** — Naming convention des champs

#### Programme
| Mobile | Admin |
|---|---|
| `nom` | `nom_programme` |

**Décision proposée** : **`nom_programme`** (plus explicite, cohérent avec les autres champs admin `id_createur`, `is_actif`, `seances_par_semaine`). Mobile devra adapter `lib/models/programme.dart`.

**Alternative** : le serveur renvoie **les deux clés** dans chaque réponse JSON (`{"nom": "...", "nom_programme": "..."}`), permettant la migration progressive. Coût : duplication mineure.

#### Exercice
| Mobile | Admin |
|---|---|
| `video_url`, `image_url` | `url_video`, (pas d'image) |

**Décision proposée** : **`video_url` + `image_url`** (mobile a raison, plus standard et permet une future image). L'admin C# sera mis à jour côté DTO (`UrlVideo` → `VideoUrl`, ajout `ImageUrl`).

#### ID de création
| Mobile | Admin |
|---|---|
| `{"id_event": 12}`, `{"id_post": 30}` (clé explicite par entité) | `{"id": 12}` ou variante `{"id_<entity>": 12}` |

**Décision proposée** : **`{"id_<entity>": <int>}` explicite partout** (`id_client`, `id_programme`, `id_exercice`, `id_seance_builder`, ...). Admin C# accepte déjà les deux variantes via `[JsonPropertyName]` multi-clés.

---

### 0.5 ⚠️ **DÉCISION REQUISE** — Superset des entités

**User/Client** :
- 📱 Mobile attend ~30 champs (`firebase_uid`, `phone`, `address`, `city`, `postal_code`, `country`, `gender`, `birth_date`, `height`, `weight`, `blood_type`, `image_url`, `membership_level`, `member_since`, `favorite_sport`, `personal_physical_fatigue`, `personal_mental_fatigue`, `sleep_quality`, `sleep_quantity`, `diet_quality`, `diet_quantity`, `tobacco_consumption`, `alcohol_consumption`, `movement_limitations`, `medical_condition`, `specific_medical_treatment`, `max_heart_rate`, `rest_heart_rate`, `weekly_visits`, `calories_burned`, `emergency_contact_name`, `emergency_contact_phone`, ...).
- 🖥 Admin attend ~10 champs + objet `abonnement` nested.

**Décision proposée** : **renvoyer le superset complet**. L'admin ignore les champs qu'il n'utilise pas (le DTO C# `[JsonExtensionData]` capture le reste sans warning).

**Challenge** :
- 📱 Mobile a `today_task`, `image_url`, `participant` (participation courante de l'utilisateur).
- 🖥 Admin a `id_ligue`, `participants_count`.

**Décision proposée** : le serveur renvoie **tous les champs**. `participant` est calculé pour l'utilisateur courant (mobile). `participants_count` est un `COUNT(*)` sur la table participants.

---

### 0.6 Abonnement de Client (admin-only)

L'admin gère les abonnements via une struct `abonnement` nested dans `Client`. Le mobile ne lit pas cet objet.

**Décision proposée** : le serveur renvoie systématiquement `abonnement: {...}` (ou `null`) dans tous les `Client`. Mobile l'ignore, admin l'utilise.

---

## 1. Conventions générales

### 1.1 Base URL
`http://apiburnout.duckdns.org/` (à confirmer par décision 0.2)

### 1.2 Format requête
- Par défaut : **JSON** (`Content-Type: application/json`).
- Exception (mobile) : `/users/login` + `/users/register` en `application/x-www-form-urlencoded` (cf. décision 0.1).

### 1.3 Authentification
- Header : `Authorization: Bearer <jwt>`.
- Le serveur **déduit `id_client` du token** (jamais du body de requête).
- Sur **401** : mobile tente un refresh Firebase puis rejoue. Admin déconnecte.

### 1.4 Format réponse

| Type | Forme |
|---|---|
| Lecture (liste ou objet) | `{ "success": true, "data": <...> }` |
| Lecture paginée | `{ "success": true, "data": [...], "page": 1, "per_page": 50, "total": 120 }` |
| Login / register | `{ "success": true, "token": "<jwt>", "refreshToken": "<...>" }` (cf. décision 0.5) |
| Création | `{ "success": true, "id_<entity>": <int|uuid> }` |
| Mise à jour / suppression OK | `{ "success": true }` |
| Erreur | `{ "success": false, "error": "<message>" }` + HTTP 4xx/5xx |

### 1.5 Codes HTTP

| Code | Signification |
|---|---|
| 200 | OK (lecture, update) |
| 201 | Créé |
| 204 | Vide (delete OK) |
| 400 | Body invalide / paramètre manquant |
| 401 | Non authentifié ou token expiré |
| 403 | Authentifié mais interdit |
| 404 | Ressource introuvable |
| 409 | Conflit (déjà inscrit, déjà validé, etc.) |
| 500 | Erreur serveur |

### 1.6 Formats date

| Usage | Format |
|---|---|
| Date pure | `YYYY-MM-DD` |
| Datetime ISO (recommandé) | `YYYY-MM-DDTHH:MM:SSZ` (UTC) |
| Datetime MySQL (legacy challenges) | `YYYY-MM-DD HH:MM:SS` |
| Heure pure | `HH:MM` ou `HH:MM:SS` |

> Préférence : **ISO 8601 UTC**. `/challenges` actuel utilise le format MySQL — toléré mais à harmoniser.

### 1.7 Enums normalisés (toujours en minuscules avec underscores)

| Champ | Valeurs autorisées |
|---|---|
| `statut` (client) | `actif`, `inactif`, `expire`, `en_attente`, `suspendu` |
| `type` (challenge) | `cardio`, `force`, `endurance`, `poids`, `souplesse`, `general`, `quotidien` |
| `statut` (challenge) | `actif`, `a_venir`, `termine`, `annule` |
| `type` (programme) | `cardio`, `force`, `souplesse`, `mixte`, `musculation` |
| `niveau` | `debutant`, `intermediaire`, `avance` |
| `type` (abonnement) | `mensuel`, `trimestriel`, `annuel` |
| `resultat` (accès NFC) | `autorise`, `refuse`, `en_attente` |
| `action` (log accès) | `entree`, `sortie` |
| `statut` (réalisation séance) | `prevue`, `realisee`, `manquee`, `annulee` |
| `tier` (badge) | `bronze`, `silver`, `gold`, `platinum`, `diamond`, `master`, `legend` |
| `division` | `1`, `2`, `3`, `4` (int) |
| `totem_slug` | 8 slugs (à confirmer entre mobile/admin) ex. `wolf`, `eagle`, `bear`, `lion`, `dragon`, `phoenix`, `tiger`, `shark` |
| `role` (register) | `coach`, `admin`, `client` |

---

## 2. Authentification

### 2.1 POST /users/login  📱🖥

⚠️ **DÉCISION REQUISE** (cf. 0.1) — accepter form-encoded **et** JSON, ou choisir JSON unique.

**Auth** : aucune

**Body (form-encoded — mobile)** :
```
email=foo@x.com&password=secret
```

**Body (JSON — admin)** :
```json
{ "email": "foo@x.com", "password": "secret" }
```

**Réponse 200** (union mobile + admin) :
```json
{
  "success": true,
  "token": "<jwt>",
  "refreshToken": "<firebase_refresh_token>",
  "error": null
}
```

**Notes** :
- `refreshToken` requis par mobile (Firebase). Admin ne le lit pas — mais le renvoyer ne casse rien.
- Admin stocke le token avec expiration locale 1h ; mobile rafraîchit dynamiquement.

---

### 2.2 POST /users/register  📱🖥

⚠️ **DÉCISION REQUISE** — Mobile attend **auto-login** (token retourné), Admin attend juste un `uid`.

**Auth** : aucune

**Body (form-encoded — mobile)** :
```
email=...&password=...&prenom=...&nom=...
```

**Body (JSON — admin)** :
```json
{
  "email": "...",
  "password": "...",
  "prenom": "...",
  "nom": "...",
  "role": "coach",
  "specialite": "Musculation",
  "code_verification": "ABC123"
}
```
`role` ∈ {`coach`, `admin`, `client`} — défaut `client`. `specialite` nullable. `code_verification` requis pour `role=coach` / `role=admin`.

**Réponse 201** (union) :
```json
{
  "success": true,
  "uid": "<firebase-uid>",
  "token": "<jwt>",
  "refreshToken": "<firebase_refresh>",
  "error": null
}
```

**Recommandation** : renvoyer **toujours** `uid` + `token` + `refreshToken`. Mobile utilise token+refresh, admin n'utilise que `uid` puis enchaîne avec `/users/login`.

ℹ️ **Divergence résolue** : le superset des champs satisfait les deux.

---

### 2.3 POST /users/logout  🔒  📱🖥

**Body** : `{}` (ou vide)

**Réponse 200** : `{ "success": true }`

---

### 2.4 GET /users/me  🔒  📱

**Réponse 200** : `{ "success": true, "data": <ClientComplet> }` (cf. §4 pour la structure complète).

> 🖥 Admin n'appelle pas cette route ; il identifie l'utilisateur courant en cherchant son email dans `GET /users` (workaround). Le serveur peut le supporter aussi.

---

### 2.5 PUT /users/me  🔒  📱

**Body** : tous les champs `ClientComplet` optionnels (sauf `id_client`, `firebase_uid`, `statut` qui sont en lecture seule pour le client lui-même).

**Réponse 200** : `{ "success": true }`

---

## 3. Utilisateurs (admin/coach)

### 3.1 GET /users  🔒  🖥 (📱 fallback)

**Réponse 200** :
```json
{
  "success": true,
  "data": [ <ClientComplet>, ... ]
}
```

ℹ️ **Note admin** : utilisé aussi pour résoudre l'`id` SQL de l'utilisateur connecté en cherchant par email. Le DTO admin accepte `id_user`, `uid`, `displayName`, `role` en plus.

**Recommandation serveur** : renvoyer `id_client` + `id_user` (alias) + `email` + `role` minimum, plus le superset défini en §4.

---

### 3.2 GET /users/{id}  🔒  🖥

**Réponse 200** : `{ "success": true, "data": <ClientComplet> }`.

ℹ️ Sert de **fallback** côté admin quand `GET /clients/{id}` échoue.

---

### 3.3 PUT /users/{id}  🔒  🖥

Deux usages distincts par l'admin :

**Usage A — mise à jour statut seul** :
```json
{ "statut": "suspendu" }
```

**Usage B — mise à jour complète** :
```json
{
  "prenom": "...",
  "nom": "...",
  "email": "...",
  "statut": "actif",
  "nfc_uid": null
}
```

**Recommandation** : accepter **n'importe quel sous-ensemble** des champs `ClientComplet`. Les champs absents ne sont pas modifiés.

**Réponse 200** : `{ "success": true }`

---

## 4. Clients

### 4.1 Schéma `ClientComplet` unifié

> Le serveur renvoie systématiquement le **superset complet**. Mobile et admin n'en consomment qu'un sous-ensemble.

| Champ JSON | Type SQL | Obligatoire | Source | Notes |
|---|---|---|---|---|
| `id_client` | INT PK | oui | 📱🖥 | alias accepté : `id`, `id_user` |
| `firebase_uid` | VARCHAR(128) | oui | 📱 | UID Firebase Auth |
| `prenom` | VARCHAR(100) | oui | 📱🖥 | |
| `nom` | VARCHAR(100) | oui | 📱🖥 | |
| `email` | VARCHAR(255) UNIQUE | oui | 📱🖥 | |
| `phone` | VARCHAR(30) | non | 📱 | |
| `address` | VARCHAR(255) | non | 📱 | |
| `city` | VARCHAR(100) | non | 📱 | |
| `postal_code` | VARCHAR(20) | non | 📱 | |
| `country` | VARCHAR(100) | non | 📱 | |
| `gender` | ENUM('M','F','autre') | non | 📱 | |
| `birth_date` | DATE | non | 📱 | |
| `height` | DECIMAL(5,2) | non | 📱 | cm |
| `weight` | DECIMAL(5,2) | non | 📱 | kg |
| `blood_type` | VARCHAR(5) | non | 📱 | |
| `image_url` | VARCHAR(500) | non | 📱 | avatar |
| `membership_level` | VARCHAR(50) | non | 📱 | ex. `gold`, `silver` |
| `member_since` | DATE | non | 📱 | |
| `favorite_sport` | VARCHAR(100) | non | 📱 | |
| `personal_physical_fatigue` | TINYINT | non | 📱 | 0-10 |
| `personal_mental_fatigue` | TINYINT | non | 📱 | 0-10 |
| `sleep_quality` | TINYINT | non | 📱 | 0-10 |
| `sleep_quantity` | DECIMAL(4,2) | non | 📱 | heures |
| `diet_quality` | TINYINT | non | 📱 | 0-10 |
| `diet_quantity` | TINYINT | non | 📱 | 0-10 |
| `tobacco_consumption` | VARCHAR(50) | non | 📱 | |
| `alcohol_consumption` | VARCHAR(50) | non | 📱 | |
| `movement_limitations` | TEXT | non | 📱 | |
| `medical_condition` | TEXT | non | 📱 | |
| `specific_medical_treatment` | TEXT | non | 📱 | |
| `max_heart_rate` | SMALLINT | non | 📱 | bpm |
| `rest_heart_rate` | SMALLINT | non | 📱 | bpm |
| `weekly_visits` | SMALLINT | non | 📱 | calculé |
| `calories_burned` | INT | non | 📱 | calculé |
| `statut` | ENUM | oui | 📱🖥 | cf. §1.7 |
| `nfc_uid` | VARCHAR(50) | non | 📱🖥 | UID badge NFC, UNIQUE |
| `emergency_contact_name` | VARCHAR(200) | non | 📱 | |
| `emergency_contact_phone` | VARCHAR(30) | non | 📱 | |
| `totem_rang` | INT NULL | non | 🖥 | FK logique vers rank_badges |
| `abonnement` | OBJET | non | 🖥 | cf. §4.2 (peut être `null`) |
| `created_at` | TIMESTAMP | auto | 📱🖥 | ISO UTC |

### 4.2 Schéma `abonnement` (nested dans Client, admin-only)

```json
{
  "id_abonnement": 5,
  "type": "mensuel",
  "date_debut": "2026-01-10",
  "date_fin": "2026-02-10",
  "auto_renouvellement": 1
}
```

| Champ | Type SQL | Valeurs |
|---|---|---|
| `id_abonnement` | INT PK | auto |
| `type` | ENUM | `mensuel`, `trimestriel`, `annuel` |
| `date_debut` | DATE | |
| `date_fin` | DATE | |
| `auto_renouvellement` | TINYINT(1) | 0 ou 1 |

---

### 4.3 GET /clients  🔒  🖥

**Query** :
- `page` (int, défaut 1)
- `per_page` (int, défaut 50)
- `statut` (enum)
- `search` (string : nom, prénom, email)

**Réponse 200** :
```json
{
  "success": true,
  "data": [ <ClientComplet>, ... ],
  "page": 1,
  "per_page": 50,
  "total": 120
}
```

---

### 4.4 GET /clients/{id}  🔒  📱🖥

**Réponse 200** : `{ "success": true, "data": <ClientComplet> }`.

ℹ️ Admin fait fallback automatique sur `GET /users/{id}` si 4xx.

---

### 4.5 GET /clients/{id}/profile  🔒  📱

**Réponse 200** : `{ "success": true, "data": <ClientPublic + stats + derniers posts> }`.

Sous-ensemble public du client + `points`, `current_streak`, `totem_rang`, dernières publications.

---

### 4.6 GET /clients/nfc/{uid}  🔒  📱🖥

**Réponse 200** : `{ "success": true, "data": <ClientComplet> }` — 404 si UID inconnu.

---

### 4.7 PUT /clients/{id}/nfc  🔒  📱🖥

**Body** : `{ "nfc_uid": "04A1B2C3" }` ou `{ "nfc_uid": null }` (désassociation).

**Réponse 200** : `{ "success": true, "data": <ClientComplet> }`.

---

### 4.8 POST /clients  🔒  🖥

**Body** :
```json
{
  "prenom": "Jean",
  "nom": "Dupont",
  "email": "jean@example.com",
  "statut": "actif",
  "nfc_uid": null,
  "abonnement": {
    "type": "mensuel",
    "date_debut": "2026-01-10",
    "date_fin": "2026-02-10",
    "auto_renouvellement": 1
  }
}
```

**Réponse 201** :
```json
{ "success": true, "id_client": 42, "error": null }
```
Alias accepté (admin) : `id`.

---

### 4.9 PUT /clients/{id}/abonnement  🔒  🖥

**Body** :
```json
{
  "type": "mensuel",
  "date_debut": "2026-01-10",
  "date_fin": "2026-02-10",
  "auto_renouvellement": 1
}
```

**Réponse 200** : `{ "success": true }`

---

### 4.10 DELETE /clients/{id}  🔒  🖥

**Réponse 200** : corps vide ou `{ "success": true }`.

---

### 4.11 🆕 GET /clients/{id}/history  🔒  🖥

Liste des séances assignées à un client (réalisées + à venir).

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "assignation_id": "uuid",
      "seance_builder_id": 12,
      "nom_seance": "Pectoraux + Triceps",
      "date_prevue": "2026-06-12T10:00:00Z",
      "date_realisation": "2026-06-12T10:45:00Z",
      "statut": "realisee"
    }
  ]
}
```
`statut` ∈ `prevue`, `realisee`, `manquee`, `annulee`.

---

### 4.12 🆕 GET /clients/{id}/performances/{builderId}  🔒  🖥

Performances d'un client pour une séance template précise.

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "date_realisation": "2026-06-12T10:45:00Z",
      "exercice_id": 1,
      "exercice_nom": "Développé couché",
      "series": [
        { "reps": 10, "poids_kg": 60.0 },
        { "reps": 8,  "poids_kg": 65.0 }
      ]
    }
  ]
}
```

---

### 4.13 🆕 GET /clients/{id}/session-feedback  🔒  🖥

**Query** : `date` (ISO 8601 UTC, obligatoire), `assignation` (UUID, optionnel).

**Réponse 200** :
```json
{
  "success": true,
  "data": {
    "rpe": 8,
    "commentaire": "Très intense, fatigué sur la fin",
    "duree_minutes": 55
  }
}
```
Si aucun feedback : `{ "success": true, "data": null }`.

---

### 4.14 🆕 GET /clients/{id}/stats  🔒  🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": {
    "points": 1340,
    "seances_total": 42,
    "streak": 7
  }
}
```

ℹ️ Note : équivalent admin de `GET /me/stats` côté mobile. Mêmes données sous-jacentes, juste un autre point d'accès (admin consulte les stats d'un client tiers).

---

### 4.15 🆕 PUT /clients/{id}/totem  🔒  🖥

**Body** : `{ "totem_rang": 12 }` (int ou `null` pour reset).

**Réponse 200** : `{ "success": true, "data": { "totem_rang": 12 } }`.

---

## 5. Coaches  📱

> Routes mobile-only. L'admin gère les coaches via les routes `/users` (un coach étant un User avec `role=coach`).

### 5.1 Schéma `Coach`

| Champ | Type | Notes |
|---|---|---|
| `id_coach` | INT PK | |
| `name` | VARCHAR(200) | "Prénom Nom" |
| `specialty` | VARCHAR(100) | |
| `image_url` | VARCHAR(500) | |
| `availability` | VARCHAR(200) | texte libre |
| `is_available` | TINYINT(1) | |
| `is_pro` | TINYINT(1) | |
| `bio` | TEXT | |

### 5.2 GET /coaches  🔒  📱
**Query** : `available=true` (filtre).

### 5.3 GET /coaches/{id}  🔒  📱

### 5.4 GET /me/coaches  🔒  📱
Liste des coaches associés au client courant.

### 5.5 POST /me/coaches  🔒  📱
**Body** : `{ "id_coach": 3, "is_primary": true }`

### 5.6 DELETE /me/coaches/{idCoach}  🔒  📱

---

## 6. Événements  📱

> Routes mobile-only.

### 6.1 Schéma `Event`

| Champ | Type | Notes |
|---|---|---|
| `id_event` | INT PK | |
| `title` | VARCHAR(255) | |
| `category` | VARCHAR(100) | |
| `description` | TEXT | |
| `date` | DATE | |
| `start_time` | TIME | |
| `end_time` | TIME | |
| `location` | VARCHAR(255) | |
| `location_detail` | VARCHAR(255) | |
| `image_url` | VARCHAR(500) | |
| `price` | DECIMAL(8,2) | |
| `max_participants` | INT | |
| `current_participants` | INT | computed |
| `intensity` | VARCHAR(50) | |
| `level` | VARCHAR(50) | |
| `coach` | OBJET Coach \| null | embed |
| `participants` | ARRAY de `{image_url}` | preview avatars |

### 6.2 GET /events  🔒  📱
**Query** : `category`, `date`, `upcoming=true`.

### 6.3 GET /events/{id}  🔒  📱

### 6.4 POST /events  🔒  📱
**Body** : `title`, `category`, `description`, `date`, `start_time`, `end_time`, `location`, `location_detail`, `image_url`, `price`, `max_participants`, `intensity`, `level`, `id_coach`.
**Réponse 201** : `{ "success": true, "id_event": 12 }`.

### 6.5 POST /events/{id}/register  🔒  📱
**Réponse 200** : `{ "success": true }`. **409** si déjà inscrit ou complet.

### 6.6 DELETE /events/{id}/register  🔒  📱

### 6.7 GET /me/events  🔒  📱

---

## 7. Défis (challenges)  📱🖥

### 7.1 Schéma `Challenge` unifié

| Champ JSON | Type SQL | Source | Notes |
|---|---|---|---|
| `id_challenge` | INT PK | 📱🖥 | |
| `nom` | VARCHAR(255) | 📱🖥 | |
| `description` | TEXT | 📱🖥 | |
| `type` | ENUM | 📱🖥 | `cardio`, `force`, `endurance`, `poids`, `souplesse`, `general`, `quotidien` |
| `objectif` | DECIMAL(10,2) | 📱🖥 | |
| `unite_objectif` | VARCHAR(20) | 📱🖥 | ex. `km`, `kg`, `reps` |
| `statut` | ENUM | 📱🖥 | `actif`, `a_venir`, `termine`, `annule` |
| `recompense_description` | VARCHAR(500) | 📱🖥 | |
| `image_url` | VARCHAR(500) | 📱 | |
| `today_task` | VARCHAR(500) | 📱 | description tâche du jour (type quotidien) |
| `date_debut` | DATETIME | 📱🖥 | format MySQL legacy ou ISO |
| `date_fin` | DATETIME | 📱🖥 | |
| `id_ligue` | INT NULL | 🖥 | FK vers `ligues` (admin) |
| `participants_count` | INT (computed) | 🖥 | `COUNT(*)` participants |
| `participant` | OBJET \| null | 📱 | participation du client courant |
| `created_at` | TIMESTAMP | 📱🖥 | |

**Sous-objet `participant`** (mobile-only, présent dans la réponse pour `/challenges` et `/challenges/{id}` quand un utilisateur connecté est inscrit) :
```json
{
  "id_participant": 22,
  "valeur_actuelle": 47.5,
  "completed_days": 3
}
```

---

### 7.2 GET /challenges  🔒  📱🖥
**Query** : `statut`, `type`.
**Réponse 200** :
```json
{
  "success": true,
  "data": [ <Challenge>, ... ]
}
```
Tableau brut accepté en fallback côté admin.

---

### 7.3 GET /challenges/{id}  🔒  📱🖥

---

### 7.4 POST /challenges  🔒  📱🖥

**Body** (union mobile + admin) :
```json
{
  "nom": "100km en mai",
  "description": "...",
  "type": "cardio",
  "objectif": 100,
  "unite_objectif": "km",
  "statut": "a_venir",
  "recompense_description": "Badge or",
  "image_url": null,
  "today_task": null,
  "date_debut": "2026-05-01 00:00:00",
  "date_fin": "2026-05-31 23:59:59",
  "id_ligue": null
}
```

⚠️ **DÉCISION REQUISE** — Format dates : admin envoie `yyyy-MM-dd HH:mm:ss` (MySQL). Mobile peut envoyer ISO. Le serveur doit accepter **les deux** (PHP `DateTime::createFromFormat` ou `strtotime`).

**Réponse 201** : `{ "success": true, "id_challenge": 3 }`.

---

### 7.5 PUT /challenges/{id}  🔒  📱🖥
**Body** : mêmes champs que POST (optionnels).
**Réponse 200** : `{ "success": true }`.

### 7.6 DELETE /challenges/{id}  🔒  📱🖥
Corps vide accepté.

---

### 7.7 GET /challenges/{id}/participants  🔒  📱🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "id_participant": 22,
      "id_challenge": 3,
      "id_client": 7,
      "prenom": "Jean",
      "nom": "Dupont",
      "nom_client": "Jean Dupont",
      "image_url": "https://...",
      "valeur_actuelle": 47.5,
      "completed_days": 3,
      "joined_at": "2026-05-02T10:00:00Z"
    }
  ]
}
```
ℹ️ **Divergence résolue** : mobile lit `prenom`/`nom`/`image_url`/`completed_days`. Admin lit `nom_client`/`joined_at`. Le serveur renvoie l'union.

---

### 7.8 POST /challenges/{id}/participants  🔒  📱🖥

⚠️ **DÉCISION REQUISE** — Body :
- 📱 Mobile envoie `{}` (id_client déduit du token).
- 🖥 Admin envoie `{ "id_client": 7, "nom_client": "Jean Dupont" }` (admin inscrit un autre client).

**Recommandation côté serveur** : accepter **les deux** :
- Si body vide → utiliser `id_client` du token (cas mobile : auto-inscription).
- Si body contient `id_client` → l'utiliser tel quel (cas admin : inscription tierce, vérifier que le caller a `role=admin|coach`).

**Réponse 200** : `{ "success": true }`.

---

### 7.9 GET /challenges/{id}/me  🔒  📱

Renvoie la participation du client courant à ce challenge, ou `null` si non inscrit.

**Réponse 200** : `{ "success": true, "data": <participant> | null }`.

---

### 7.10 POST /challenges/{id}/validate  🔒  📱

Valide la journée courante (pour les challenges `type=quotidien`).

**Réponse 200** : `{ "success": true }`. **409** si déjà validé aujourd'hui.

---

### 7.11 DELETE /challenge-participants/{participantId}  🔒  📱🖥

⚠️ **Attention nommage** : la route est sur `/challenge-participants/...` (sans le challenge id), pas `/challenges/{id}/participants/{pid}`.

Corps vide accepté.

---

### 7.12 PUT /challenge-participants/{participantId}/progress  🔒  📱🖥

⚠️ **DÉCISION REQUISE** — Body :
- 📱 Mobile : `{ "valeur_actuelle": 47.5, "completed_days": 3 }`
- 🖥 Admin : `{ "valeur_actuelle": 47.5 }`

**Recommandation** : accepter **les deux**, `completed_days` optionnel (incrémenté automatiquement par le serveur sur validation quotidienne si absent).

**Réponse 200** : `{ "success": true }`.

---

### 7.13 GET /me/challenges  🔒  📱
Liste des challenges auxquels le client courant participe.

---

## 8. Exercices  📱🖥

### 8.1 Schéma `Exercice` unifié

| Champ JSON | Type SQL | Obligatoire | Source | Notes |
|---|---|---|---|---|
| `id_exercice` | INT PK | oui | 📱🖥 | alias accepté : `id` |
| `nom` | VARCHAR(255) | oui | 📱🖥 | |
| `description` | TEXT | non | 📱🖥 | |
| `categorie` | VARCHAR(100) | non | 📱🖥 | |
| `groupe_musculaire` | VARCHAR(100) | non | 🖥 | ignoré par mobile |
| `image_url` | VARCHAR(500) | non | 📱 | ⚠️ admin n'envoie pas, à ajouter côté DTO admin |
| `video_url` | VARCHAR(500) | non | 📱🖥 | ⚠️ **DÉCISION** : admin utilise `url_video`, mobile `video_url`. Recommandation : **`video_url`** (cf. 0.4) |
| `is_default` | TINYINT(1) | non | 🖥 | exo "système" vs custom |
| `tags` | JSON | non | 🖥 | ex. `["compound","barbell"]` |
| `created_at` | TIMESTAMP | auto | 📱🖥 | |

ℹ️ **Compat** : DTO admin C# accepte `is_default` en bool, int 0/1, ou string. DTO mobile lit uniquement `video_url`. Renvoyer `video_url` (canonical) + `url_video` en alias temporaire si admin tarde à migrer.

### 8.2 GET /exercices  🔒  📱🖥
**Query** : `categorie`, `search`.
**Réponse 200** :
```json
{
  "success": true,
  "data": [ <Exercice>, ... ]
}
```

### 8.3 POST /exercices  🔒  📱🖥

**Body** :
```json
{
  "nom": "Mon exo perso",
  "description": null,
  "categorie": "Musculation",
  "groupe_musculaire": "Pectoraux",
  "image_url": null,
  "video_url": null
}
```
**Réponse 201** : `{ "success": true, "id_exercice": 28 }`.

### 8.4 DELETE /exercices/{id}  🔒  📱🖥
Corps vide accepté.

---

## 9. Programmes  📱🖥

### 9.1 Schéma `Programme` unifié

| Champ JSON | Type SQL | Source | Notes |
|---|---|---|---|
| `id_programme` | INT PK | 📱🖥 | |
| `nom_programme` | VARCHAR(255) | 📱🖥 | ⚠️ **DÉCISION 0.4** : mobile utilise `nom`. Recommandation : **`nom_programme`** canonique + alias `nom` temporaire. |
| `description` | TEXT | 📱🖥 | |
| `type` | ENUM | 📱🖥 | `cardio`, `force`, `souplesse`, `mixte`, `musculation` |
| `niveau` | ENUM | 📱🖥 | `debutant`, `intermediaire`, `avance` |
| `duree_semaines` | TINYINT | 📱🖥 | |
| `seances_par_semaine` | TINYINT | 🖥 | |
| `image_url` | VARCHAR(500) | 📱 | |
| `is_actif` | TINYINT(1) | 🖥 | bool ou int |
| `id_client` | INT NULL | 🖥 | programme dédié à un client si non-null |
| `id_createur` | INT | 🖥 | id du coach/admin créateur |
| `date_debut` | DATE | 🖥 | |
| `date_fin` | DATE | 🖥 | |
| `created_at` | TIMESTAMP | 📱🖥 | |

---

### 9.2 GET /programmes  🔒  📱🖥
**Query** : `type`, `niveau`, `search`.
**Réponse 200** :
```json
{
  "success": true,
  "data": [ <Programme>, ... ]
}
```
Tableau brut accepté en fallback admin.

### 9.3 GET /programmes/{id}  🔒  📱🖥

### 9.4 POST /programmes  🔒  📱🖥

**Body union** :
```json
{
  "nom_programme": "Prise de masse",
  "description": "...",
  "type": "force",
  "niveau": "intermediaire",
  "duree_semaines": 8,
  "seances_par_semaine": 3,
  "image_url": null,
  "id_client": null,
  "id_createur": 12,
  "date_debut": "2026-01-10",
  "date_fin": "2026-03-10"
}
```
**Réponse 201** : `{ "success": true, "id_programme": 18 }`.

ℹ️ Admin résout `id_createur` côté client via `GET /users` (lookup email). Le serveur pourrait le déduire du token si absent.

### 9.5 PUT /programmes/{id}  🔒  📱🖥

**Body** : sous-ensemble des champs.
```json
{
  "nom_programme": "...",
  "description": "...",
  "type": "force",
  "niveau": "intermediaire",
  "is_actif": 1
}
```
**Réponse 200** : `{ "success": true }`.

### 9.6 DELETE /programmes/{id}  🔒  📱🖥
Corps vide accepté.

---

### 9.7 GET /programmes/{id}/seances  🔒  📱🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "id_seance": 33,
      "id_programme": 7,
      "id_seance_builder": 12,
      "nom": "Séance 1 - Pectoraux",
      "description": "...",
      "ordre": 1,
      "exercise_count": 6,
      "category_count": 2,
      "data_json": "{...}",
      "created_at": "2026-01-10T08:00:00Z"
    }
  ]
}
```
Alias accepté admin : `id` ou `id_seance`.

### 9.8 POST /programmes/{id}/seances  🔒  🖥

**Body** :
```json
{
  "id_seance_builder": 12,
  "nom": "Séance 1 - Pectoraux",
  "description": "...",
  "ordre": 1,
  "exercise_count": 6,
  "category_count": 2,
  "data_json": "{...JSON sérialisé du template...}"
}
```
**Réponse 201** : `{ "success": true, "id_seance": 33 }`.

### 9.9 PUT /programmes/{id}/seances/{seanceId}  🔒  🖥

**Body** (tous nullable) :
```json
{ "ordre": 2, "nom": null, "description": null }
```
**Réponse 200** : `{ "success": true }`.

### 9.10 DELETE /programmes/{id}/seances/{seanceId}  🔒  🖥
Corps vide accepté.

---

### 9.11 POST /programme-assignations  🔒  📱🖥

**Body union** :
```json
{
  "id_programme": 18,
  "id_client": 7,
  "date_debut": "2026-01-10",
  "date_fin": "2026-04-10"
}
```
📱 Mobile envoie `id_programme + id_client + date_debut` (auto-assignation possible).
🖥 Admin envoie tous les champs (assignation tiers).

**Réponse 201** :
```json
{
  "success": true,
  "id_assignation": "550e8400-e29b-41d4-a716-446655440000"
}
```
⚠️ `id_assignation` est un **UUID string** (CHAR(36)).

### 9.12 GET /programme-assignations/client/{idClient}  🔒  🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "id_assignation": "uuid",
      "id_client": 7,
      "id_programme": 18,
      "date_debut": "2026-01-10",
      "date_fin": "2026-04-10",
      "date_assignation": "2026-01-09T14:00:00Z",
      "programme": {
        "id_programme": 18,
        "nom_programme": "Prise de masse",
        "description": "...",
        "type": "force",
        "niveau": "intermediaire"
      }
    }
  ]
}
```

### 9.13 GET /me/programmes  🔒  📱
Liste des programmes assignés au client courant.

---

## 10. Séances (builder + assignations + réalisations)

### 10.1 Schéma `SeanceBuilder` unifié

Le `SeanceBuilder` (= "template" de séance) est l'unité partagée entre mobile et admin.

**Vue mobile (riche, structurée par blocs)** :
```json
{
  "id_seance_builder": 12,
  "nom": "Pectoraux + Triceps",
  "description": "...",
  "blocs": [
    {
      "name": "Échauffement",
      "exercices": [
        {
          "id_exercice": 1,
          "nom": "Développé couché",
          "series_count": 4,
          "reps_per_serie": 10,
          "reps_varied": false,
          "recup_seconds": 90,
          "recup_inter_seconds": 30,
          "tempo": "2-0-1-0",
          "amplitude": "complete",
          "rir": 2,
          "rpe": 7,
          "duree_secondes": null,
          "poids_label": "60kg",
          "image_url": "...",
          "video_url": "...",
          "description": "...",
          "set_details": [
            { "reps": 10, "weight": 60.0, "recup": 90, "tempo": "2-0-1-0" }
          ],
          "factors_info": {}
        }
      ]
    }
  ],
  "exercise_count": 6,
  "category_count": 2,
  "created_at": "2026-01-10T08:00:00Z"
}
```

**Vue admin (raccourcie, JSON sérialisé)** :
```json
{
  "id_seance_builder": 12,
  "nom": "Pectoraux + Triceps",
  "description": "...",
  "exercise_count": 6,
  "category_count": 2,
  "data_json": "{...JSON sérialisé du SessionModel...}",
  "created_at": "2026-01-10T08:00:00Z"
}
```

⚠️ **DÉCISION REQUISE** — La structure mobile (`blocs[]`) et l'admin (`data_json` opaque) sont **incompatibles** en l'état.

**Recommandation** : le serveur **stocke les deux représentations** et expose les deux dans la réponse :
- `blocs[]` : représentation structurée (lue par mobile).
- `data_json` : la **même chose** sérialisée en string (lue par admin).
- `exercise_count`, `category_count` : champs dérivés.

À l'écriture (POST/PUT) : accepter **l'un OU l'autre** :
- Si `blocs[]` fourni → serveur calcule `data_json = JSON.stringify(blocs)`.
- Si `data_json` fourni seul → serveur le parse pour reconstituer `blocs[]`.

ℹ️ Ce contrat unifie le pattern "session library" admin avec le "seances builder" mobile.

---

### 10.2 GET /seances-builder  🔒  📱🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": [ <SeanceBuilder>, ... ]
}
```
Tableau brut accepté en fallback admin.

---

### 10.3 GET /seances-builder/{id}  🔒  📱🖥

⚠️ **DÉCISION REQUISE** — Admin attend la réponse **BRUTE** (`SeanceBuilderDto` directement, pas d'enveloppe). Mobile attend `{ "success": true, "data": ... }`.

**Recommandation** : **harmoniser sur l'enveloppe** `{success, data}`. Admin doit être patché (`ApiSessionLibraryService.cs:202` à modifier).

**Réponse 200** : `{ "success": true, "data": <SeanceBuilder> }`.

ℹ️ Si `data_json = "{}"` ou vide → l'admin considère l'endpoint non implémenté (fallback cache local).

---

### 10.4 POST /seances-builder  🔒  📱🖥

**Body union** (cf. 10.1) :
```json
{
  "nom": "Pectoraux + Triceps",
  "description": "...",
  "blocs": [ /* mobile */ ],
  "exercise_count": 6,
  "category_count": 2,
  "data_json": "{...}"  // admin
}
```

**Réponse 201** :
```json
{ "success": true, "id_seance_builder": 12 }
```

---

### 10.5 PUT /seances-builder/{id}  🔒  🖥

**Body** : mêmes champs que POST.
**Réponse 200** : `{ "success": true, "id_seance_builder": 12 }`.

---

### 10.6 DELETE /seances-builder/{id}  🔒  📱🖥
Corps vide accepté.

---

### 10.7 GET /seances/{id}/contenu  🔒  📱

Renvoie le contenu détaillé d'une **séance instanciée** (issue d'un programme assigné) — incluant blocs + exercices + détails.

**Réponse 200** : `{ "success": true, "data": <SeanceComplete> }` (même structure que `SeanceBuilder` mais avec données spécifiques à l'instance).

---

### 10.8 GET /me/seances  🔒  📱

Liste des **assignations** de séances au client courant avec leur contenu.

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "id_assignation": "uuid",
      "id_seance_builder": 12,
      "date_prevue": "2026-06-12T10:00:00Z",
      "ordre_jour": 1,
      "seance": { /* SeanceBuilder complet */ }
    }
  ]
}
```

### 10.9 GET /me/seances/week-count  🔒  📱
**Réponse 200** : `{ "success": true, "data": { "count": 3 } }`.

---

### 10.10 POST /me/seances/rpe  🔒  📱

Soumet le feedback RPE de fin de séance.

**Body complexe** :
```json
{
  "id_assignation": "uuid",
  "session_title": "Pectoraux + Triceps",
  "duration_sec": 3300,
  "exos_validated": [1, 2, 3],
  "exos_skipped": [4],
  "mood_index": 4,
  "mood_emoji": "😅",
  "rpe_general": 8,
  "rpe_details": {
    "scores": { "effort": 8, "technique": 7 },
    "comments": { "effort": "Bien", "technique": "À revoir le dos" },
    "category_scores": { "haut_du_corps": 8 },
    "general": 8
  },
  "comments": {
    "specifique": "Très bonne séance",
    "modifications": [
      {
        "bloc": "Échauffement",
        "exercice": "Pompes",
        "serie": 1,
        "facteur": "reps",
        "valeur": 12
      }
    ]
  }
}
```

**Réponse 200** : `{ "success": true, "id_realisation": "uuid" }`.

> ℹ️ Côté serveur : créer une ligne dans `seance_realisations` + N lignes `seance_performances` + 1 ligne `seance_feedback` (cf. §16).

---

### 10.11 GET /seances-assignations/{id}/realized-today  🔒  📱
**Réponse 200** : `{ "success": true, "data": { "realized": true } }`.

### 10.12 PUT /seances-assignations/{id}/complete  🔒  📱
**Body** : `{ "commentaire": "..." }`.
**Réponse 200** : `{ "success": true }`.

### 10.13 PUT /seances-assignations/{id}/move  🔒  📱
**Body** : `{ "date_prevue": "2026-06-15T10:00:00Z", "ordre_jour": 2 }`.

### 10.14 DELETE /seances-assignations/{id}  🔒  📱
Corps vide accepté.

### 10.15 PUT /me/seances/reorder  🔒  📱
**Body** : `{ "order": ["uuid1", "uuid2", "uuid3"] }`.
**Réponse 200** : `{ "success": true }`.

---

## 11. Posts (feed) & commentaires  📱

> Routes mobile-only (feed social).

### 11.1 Schéma `Post`

| Champ | Type | Notes |
|---|---|---|
| `id_post` | INT PK | |
| `user` | OBJET `{id_client, prenom, nom, image_url}` | auteur |
| `content` | TEXT | |
| `image_url` | VARCHAR(500) | |
| `rpe` | TINYINT | 1-10 |
| `mood` | VARCHAR(50) | |
| `duration_min` | INT | |
| `likes_count` | INT (computed) | |
| `created_at` | TIMESTAMP | |

### 11.2 GET /posts  🔒  📱
**Query** : `limit=20`, `offset=0`.

### 11.3 POST /posts  🔒  📱

**Body** :
```json
{
  "content": "Belle séance aujourd'hui !",
  "rpe": 8,
  "rpe_general": 8,
  "rpe_details": { /* idem POST /me/seances/rpe */ },
  "rpe_visibility": { "hidden": ["technique", "fatigue"] },
  "mood": "energique",
  "duration_min": 55,
  "image_url": null
}
```
**Réponse 201** : `{ "success": true, "id_post": 30 }`.

### 11.4 DELETE /posts/{id}  🔒  📱

### 11.5 POST /posts/{id}/like  🔒  📱
**Body** : `{}`.
**Réponse 200** : `{ "success": true, "data": { "likes_count": 6 } }`.

### 11.6 DELETE /posts/{id}/like  🔒  📱

### 11.7 PUT /posts/{id}/rpe  🔒  📱
**Body partiel** : `{ "rpe_visibility": {...}, "rpe_details": {...} }`.

### 11.8 GET /posts/{id}/comments  🔒  📱
**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "id_comment": 1,
      "id_post": 30,
      "content": "Bravo !",
      "user": {"id_client": 7, "prenom": "Alice", "nom": "X", "image_url": "..."},
      "created_at": "2026-06-12T11:00:00Z"
    }
  ]
}
```

### 11.9 POST /posts/{id}/comments  🔒  📱
**Body** : `{ "content": "Bravo !" }`.
**Réponse 201** : `{ "success": true, "data": <comment> }`.

### 11.10 GET /me/post-likes  🔒  📱
**Query** : `ids=1,2,3`.
**Réponse 200** : `{ "success": true, "data": [1, 3] }` (sous-liste des IDs likés).

### 11.11 GET /me/rpe-visibility  🔒  📱
**Réponse 200** : `{ "success": true, "data": { "hidden": ["technique"] } }` ou `null`.

### 11.12 PUT /me/rpe-visibility  🔒  📱
**Body** : `{ "hidden": ["technique"] }`.

---

## 12. Gamification (rang, points, classement, totem)

### 12.1 GET /rank-badges  🔒  📱🖥

> **Route partagée** : une seule implémentation, utilisée par les deux clients.

Liste **complète** des badges/rangs/totems (~224 entrées : 7 tiers × 4 divisions × 8 totems).

**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "tier": "bronze",
      "division": 3,
      "totem_slug": "wolf",
      "url": "https://cdn.../badges/bronze-3-wolf.png",
      "min_points": 0
    }
  ]
}
```
Indexée côté client par `<tier>/<division>/<totem_slug>`.

ℹ️ **Confirmer** les 8 slugs totems alignés mobile/admin (mobile : wolf, eagle, bear, lion, dragon, phoenix, tiger, shark — à valider).

---

### 12.2 GET /me/stats  🔒  📱

**Réponse 200** :
```json
{
  "success": true,
  "data": {
    "points": 1340,
    "current_streak": 7,
    "last_session_date": "2026-06-12",
    "total_seances": 42,
    "totem_rang": 12
  }
}
```

ℹ️ Équivalent admin : `GET /clients/{id}/stats` (§4.14). Mêmes données sous-jacentes.

---

### 12.3 GET /me/points  🔒  📱

**Query** : `limit=50`.
**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "delta": +10,
      "reason": "seance_realisee",
      "metadata": { "id_realisation": "uuid" },
      "created_at": "2026-06-12T11:00:00Z"
    }
  ]
}
```

### 12.4 POST /me/points  🔒  📱

**Body** : `{ "delta": 10, "reason": "challenge_validated", "metadata": {...} }`.
**Réponse 201** : `{ "success": true }`.

### 12.5 GET /leaderboard  🔒  📱

**Query** : `top=50`.
**Réponse 200** :
```json
{
  "success": true,
  "data": [
    {
      "rank": 1,
      "id_client": 7,
      "prenom": "Jean",
      "nom": "Dupont",
      "image_url": "...",
      "points": 1340,
      "current_streak": 7,
      "last_session_date": "2026-06-12",
      "totem_rang": 12
    }
  ]
}
```

---

## 13. Accès NFC  📱🖥

### 13.1 POST /logs-acces  🔒  📱

Crée un log d'accès (entrée/sortie badge).

**Body** : `{ "badge_uid": "04A1B2C3", "action": "entree" }`.
`action` ∈ `entree`, `sortie`.

**Réponse 200** : `{ "success": true, "data": { "acces_accorde": true } }`.

---

### 13.2 GET /logs-acces  🔒  📱🖥

**Query** : `limit` (int, défaut 200 admin / non spécifié mobile), `page` (int).

**Réponse 200** (union mobile + admin) :
```json
{
  "success": true,
  "data": [
    {
      "id_log": 1234,
      "event_id": "evt-uuid",
      "badge_uid": "04A1B2C3",
      "uid_nfc": "04A1B2C3",
      "id_client": 7,
      "nom_client": "Jean Dupont",
      "action": "entree",
      "resultat": "autorise",
      "acces_accorde": true,
      "raison": null,
      "porte": "Entrée principale",
      "source": "esp32",
      "timestamp": "2026-06-09T08:32:00Z",
      "timestamp_utc": "2026-06-09T08:32:00Z"
    }
  ],
  "total": 1234,
  "page": 1,
  "limit": 200
}
```

ℹ️ **Divergence résolue** : mobile lit `badge_uid`/`action`/`acces_accorde`/`timestamp`. Admin lit `uid_nfc`/`resultat`/`timestamp_utc`/`nom_client`. Renvoyer les deux jeux (aliases redondants).

---

### 13.3 GET /logs-acces/today  🔒  📱🖥

Même schéma que `/logs-acces`, filtré sur la journée courante.

---

## 14. Dashboard (admin)

### 14.1 GET /dashboard/stats  🔒  🖥

**Réponse 200** :
```json
{
  "success": true,
  "data": {
    "clients_actifs": 42,
    "acces_aujourd_hui": 18,
    "alertes_actives": 2,
    "challenges_actifs": 3,
    "programmes_actifs": 7,
    "abonnements_expirant_bientot": 4,
    "seances_jour": 12,
    "derniers_acces": [
      {
        "timestamp_utc": "2026-06-09T08:32:00Z",
        "nom_client": "Jean Dupont",
        "porte": "Entrée principale",
        "resultat": "autorise"
      }
    ]
  }
}
```

ℹ️ Si endpoint absent, admin reconstruit les stats à partir des autres endpoints (`/users`, `/logs-acces/today`, `/challenges`, `/programmes`). Préférable pour les perfs.

---

## 15. Statut API

### 15.1 GET /  (non authentifié)
**Réponse 200** : `{ "status": "ok" }`.

---

## 16. Schéma BDD unifié (MySQL)

> Schéma **complet** consolidant les besoins des deux clients.
> Pour chaque table, indique l'origine (📱 mobile / 🖥 admin / 📱🖥 both / 🆕 nouvelle).

### 16.1 `clients` (= `users`)  📱🖥

> Une seule table — les champs admin (statut, abonnement_id) et mobile (firebase_uid, height...) coexistent.

```sql
CREATE TABLE clients (
  id_client INT AUTO_INCREMENT PRIMARY KEY,
  firebase_uid VARCHAR(128) UNIQUE NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NULL,         -- si auth locale en plus de Firebase
  prenom VARCHAR(100) NOT NULL,
  nom VARCHAR(100) NOT NULL,
  role ENUM('client','coach','admin') DEFAULT 'client',
  phone VARCHAR(30) NULL,
  address VARCHAR(255) NULL,
  city VARCHAR(100) NULL,
  postal_code VARCHAR(20) NULL,
  country VARCHAR(100) NULL,
  gender ENUM('M','F','autre') NULL,
  birth_date DATE NULL,
  height DECIMAL(5,2) NULL,
  weight DECIMAL(5,2) NULL,
  blood_type VARCHAR(5) NULL,
  image_url VARCHAR(500) NULL,
  membership_level VARCHAR(50) NULL,
  member_since DATE NULL,
  favorite_sport VARCHAR(100) NULL,
  personal_physical_fatigue TINYINT NULL,
  personal_mental_fatigue TINYINT NULL,
  sleep_quality TINYINT NULL,
  sleep_quantity DECIMAL(4,2) NULL,
  diet_quality TINYINT NULL,
  diet_quantity TINYINT NULL,
  tobacco_consumption VARCHAR(50) NULL,
  alcohol_consumption VARCHAR(50) NULL,
  movement_limitations TEXT NULL,
  medical_condition TEXT NULL,
  specific_medical_treatment TEXT NULL,
  max_heart_rate SMALLINT NULL,
  rest_heart_rate SMALLINT NULL,
  weekly_visits SMALLINT DEFAULT 0,
  calories_burned INT DEFAULT 0,
  statut ENUM('actif','inactif','expire','en_attente','suspendu') DEFAULT 'en_attente',
  nfc_uid VARCHAR(50) NULL UNIQUE,
  emergency_contact_name VARCHAR(200) NULL,
  emergency_contact_phone VARCHAR(30) NULL,
  totem_rang INT NULL,
  points INT DEFAULT 0,                    -- dénormalisé pour /leaderboard
  current_streak INT DEFAULT 0,
  last_session_date DATE NULL,
  total_seances INT DEFAULT 0,
  specialite VARCHAR(100) NULL,            -- pour role=coach
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX idx_email (email),
  INDEX idx_firebase_uid (firebase_uid),
  INDEX idx_nfc_uid (nfc_uid),
  INDEX idx_statut (statut)
);
```

### 16.2 `abonnements`  🖥

```sql
CREATE TABLE abonnements (
  id_abonnement INT AUTO_INCREMENT PRIMARY KEY,
  id_client INT NOT NULL,
  type ENUM('mensuel','trimestriel','annuel') NOT NULL,
  date_debut DATE NOT NULL,
  date_fin DATE NOT NULL,
  auto_renouvellement TINYINT(1) DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  INDEX idx_client (id_client),
  INDEX idx_date_fin (date_fin)
);
```

### 16.3 `coaches`  📱

> Vue/dénormalisation des `clients` avec `role=coach`, ou table dédiée. Recommandation : **table dédiée** avec FK vers `clients`.

```sql
CREATE TABLE coaches (
  id_coach INT AUTO_INCREMENT PRIMARY KEY,
  id_client INT NULL,                      -- FK vers le User correspondant
  name VARCHAR(200) NOT NULL,
  specialty VARCHAR(100) NULL,
  image_url VARCHAR(500) NULL,
  availability VARCHAR(200) NULL,
  is_available TINYINT(1) DEFAULT 1,
  is_pro TINYINT(1) DEFAULT 0,
  bio TEXT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE SET NULL
);
```

### 16.4 `client_coaches`  📱  🆕

> Liaison client ↔ coach.

```sql
CREATE TABLE client_coaches (
  id_client INT NOT NULL,
  id_coach INT NOT NULL,
  is_primary TINYINT(1) DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_client, id_coach),
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  FOREIGN KEY (id_coach) REFERENCES coaches(id_coach) ON DELETE CASCADE
);
```

### 16.5 `events`  📱  🆕

```sql
CREATE TABLE events (
  id_event INT AUTO_INCREMENT PRIMARY KEY,
  title VARCHAR(255) NOT NULL,
  category VARCHAR(100) NULL,
  description TEXT NULL,
  date DATE NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  location VARCHAR(255) NULL,
  location_detail VARCHAR(255) NULL,
  image_url VARCHAR(500) NULL,
  price DECIMAL(8,2) DEFAULT 0,
  max_participants INT DEFAULT 0,
  intensity VARCHAR(50) NULL,
  level VARCHAR(50) NULL,
  id_coach INT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_coach) REFERENCES coaches(id_coach) ON DELETE SET NULL,
  INDEX idx_date (date)
);
```

### 16.6 `event_participants`  📱  🆕

```sql
CREATE TABLE event_participants (
  id_event INT NOT NULL,
  id_client INT NOT NULL,
  registered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_event, id_client),
  FOREIGN KEY (id_event) REFERENCES events(id_event) ON DELETE CASCADE,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE
);
```

### 16.7 `challenges`  📱🖥

```sql
CREATE TABLE challenges (
  id_challenge INT AUTO_INCREMENT PRIMARY KEY,
  nom VARCHAR(255) NOT NULL,
  description TEXT NULL,
  type ENUM('cardio','force','endurance','poids','souplesse','general','quotidien') NOT NULL,
  objectif DECIMAL(10,2) NOT NULL,
  unite_objectif VARCHAR(20) NULL,
  statut ENUM('actif','a_venir','termine','annule') NOT NULL DEFAULT 'a_venir',
  recompense_description VARCHAR(500) NULL,
  image_url VARCHAR(500) NULL,
  today_task VARCHAR(500) NULL,
  date_debut DATETIME NOT NULL,
  date_fin DATETIME NOT NULL,
  id_ligue INT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_statut (statut),
  INDEX idx_type (type)
);
```

### 16.8 `challenge_participants`  📱🖥

```sql
CREATE TABLE challenge_participants (
  id_participant INT AUTO_INCREMENT PRIMARY KEY,
  id_challenge INT NOT NULL,
  id_client INT NOT NULL,
  valeur_actuelle DECIMAL(10,2) DEFAULT 0,
  completed_days INT DEFAULT 0,
  last_validated_at DATETIME NULL,
  joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uk_challenge_client (id_challenge, id_client),
  FOREIGN KEY (id_challenge) REFERENCES challenges(id_challenge) ON DELETE CASCADE,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE
);
```

### 16.9 `exercices`  📱🖥

```sql
CREATE TABLE exercices (
  id_exercice INT AUTO_INCREMENT PRIMARY KEY,
  nom VARCHAR(255) NOT NULL,
  description TEXT NULL,
  categorie VARCHAR(100) NULL,
  groupe_musculaire VARCHAR(100) NULL,
  image_url VARCHAR(500) NULL,
  video_url VARCHAR(500) NULL,
  is_default TINYINT(1) DEFAULT 0,
  tags JSON NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_categorie (categorie)
);
```

### 16.10 `programmes`  📱🖥

```sql
CREATE TABLE programmes (
  id_programme INT AUTO_INCREMENT PRIMARY KEY,
  nom_programme VARCHAR(255) NOT NULL,
  description TEXT NULL,
  type ENUM('cardio','force','souplesse','mixte','musculation') NOT NULL,
  niveau ENUM('debutant','intermediaire','avance') NOT NULL,
  duree_semaines TINYINT DEFAULT 4,
  seances_par_semaine TINYINT DEFAULT 3,
  image_url VARCHAR(500) NULL,
  is_actif TINYINT(1) DEFAULT 1,
  id_client INT NULL,                      -- programme dédié
  id_createur INT NOT NULL,
  date_debut DATE NULL,
  date_fin DATE NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE SET NULL,
  FOREIGN KEY (id_createur) REFERENCES clients(id_client) ON DELETE CASCADE,
  INDEX idx_is_actif (is_actif)
);
```

### 16.11 `seances_builder`  📱🖥

> Table des **templates** de séances (réutilisables dans plusieurs programmes).

```sql
CREATE TABLE seances_builder (
  id_seance_builder INT AUTO_INCREMENT PRIMARY KEY,
  id_createur INT NULL,
  nom VARCHAR(255) NOT NULL,
  description TEXT NULL,
  exercise_count INT DEFAULT 0,
  category_count INT DEFAULT 0,
  data_json LONGTEXT NULL,                 -- structure complète sérialisée
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (id_createur) REFERENCES clients(id_client) ON DELETE SET NULL
);
```

### 16.12 `seance_blocks`  📱  🆕

> Représentation **structurée** mobile (alternative à `data_json` seul).

```sql
CREATE TABLE seance_blocks (
  id_block INT AUTO_INCREMENT PRIMARY KEY,
  id_seance_builder INT NOT NULL,
  name VARCHAR(100) NOT NULL,
  ordre TINYINT NOT NULL,
  FOREIGN KEY (id_seance_builder) REFERENCES seances_builder(id_seance_builder) ON DELETE CASCADE
);
```

### 16.13 `seance_block_exercices`  📱  🆕

```sql
CREATE TABLE seance_block_exercices (
  id_block_exercice INT AUTO_INCREMENT PRIMARY KEY,
  id_block INT NOT NULL,
  id_exercice INT NOT NULL,
  ordre TINYINT NOT NULL,
  series_count INT NULL,
  reps_per_serie INT NULL,
  reps_varied TINYINT(1) DEFAULT 0,
  recup_seconds INT NULL,
  recup_inter_seconds INT NULL,
  tempo VARCHAR(20) NULL,
  amplitude VARCHAR(50) NULL,
  rir TINYINT NULL,
  rpe TINYINT NULL,
  duree_secondes INT NULL,
  poids_label VARCHAR(50) NULL,
  set_details JSON NULL,                   -- [{reps, weight, recup, tempo}]
  factors_info JSON NULL,
  description TEXT NULL,
  FOREIGN KEY (id_block) REFERENCES seance_blocks(id_block) ON DELETE CASCADE,
  FOREIGN KEY (id_exercice) REFERENCES exercices(id_exercice) ON DELETE CASCADE
);
```

### 16.14 `programme_seances`  🖥

> Liaison **séance template ↔ programme** avec ordre.

```sql
CREATE TABLE programme_seances (
  id_seance INT AUTO_INCREMENT PRIMARY KEY,
  id_programme INT NOT NULL,
  id_seance_builder INT NOT NULL,
  nom VARCHAR(255) NOT NULL,
  description TEXT NULL,
  ordre INT NOT NULL,
  exercise_count INT DEFAULT 0,
  category_count INT DEFAULT 0,
  data_json LONGTEXT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_programme) REFERENCES programmes(id_programme) ON DELETE CASCADE,
  FOREIGN KEY (id_seance_builder) REFERENCES seances_builder(id_seance_builder) ON DELETE CASCADE
);
```

### 16.15 `programme_assignations`  📱🖥

```sql
CREATE TABLE programme_assignations (
  id_assignation CHAR(36) PRIMARY KEY,     -- UUID
  id_programme INT NOT NULL,
  id_client INT NOT NULL,
  date_debut DATE NOT NULL,
  date_fin DATE NULL,
  date_assignation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_programme) REFERENCES programmes(id_programme) ON DELETE CASCADE,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  INDEX idx_client (id_client)
);
```

### 16.16 `seance_builder_assignations`  📱  🆕

> Assignations individuelles d'une séance template à un client (planning).

```sql
CREATE TABLE seance_builder_assignations (
  id_assignation CHAR(36) PRIMARY KEY,
  id_client INT NOT NULL,
  id_seance_builder INT NOT NULL,
  id_programme_assignation CHAR(36) NULL,  -- FK vers programme_assignations
  date_prevue DATETIME NOT NULL,
  ordre_jour TINYINT DEFAULT 1,
  commentaire TEXT NULL,
  completed_at DATETIME NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  FOREIGN KEY (id_seance_builder) REFERENCES seances_builder(id_seance_builder) ON DELETE CASCADE,
  FOREIGN KEY (id_programme_assignation) REFERENCES programme_assignations(id_assignation) ON DELETE SET NULL,
  UNIQUE KEY uk_client_seance_date (id_client, id_seance_builder, date_prevue),
  INDEX idx_client_date (id_client, date_prevue)
);
```

### 16.17 `seance_realisations`  📱🖥  🆕

```sql
CREATE TABLE seance_realisations (
  id_realisation CHAR(36) PRIMARY KEY,
  id_client INT NOT NULL,
  id_assignation CHAR(36) NULL,
  id_seance_builder INT NOT NULL,
  nom_seance VARCHAR(255) NOT NULL,        -- dénormalisé pour historique
  date_prevue DATETIME NULL,
  date_realisation DATETIME NULL,
  duration_sec INT NULL,
  statut ENUM('prevue','realisee','manquee','annulee') NOT NULL DEFAULT 'prevue',
  mood_index TINYINT NULL,
  mood_emoji VARCHAR(10) NULL,
  rpe_general TINYINT NULL,
  rpe_details JSON NULL,
  comments JSON NULL,
  exos_validated JSON NULL,
  exos_skipped JSON NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  FOREIGN KEY (id_seance_builder) REFERENCES seances_builder(id_seance_builder),
  FOREIGN KEY (id_assignation) REFERENCES seance_builder_assignations(id_assignation) ON DELETE SET NULL,
  INDEX idx_client_date (id_client, date_realisation)
);
```

### 16.18 `seance_performances`  🖥  🆕

```sql
CREATE TABLE seance_performances (
  id_performance INT AUTO_INCREMENT PRIMARY KEY,
  id_realisation CHAR(36) NOT NULL,
  id_exercice INT NOT NULL,
  serie_num INT NOT NULL,
  reps INT NOT NULL,
  poids_kg DECIMAL(6,2) NULL,
  FOREIGN KEY (id_realisation) REFERENCES seance_realisations(id_realisation) ON DELETE CASCADE,
  FOREIGN KEY (id_exercice) REFERENCES exercices(id_exercice),
  INDEX idx_realisation (id_realisation)
);
```

### 16.19 `seance_feedback`  🖥  🆕

```sql
CREATE TABLE seance_feedback (
  id_feedback INT AUTO_INCREMENT PRIMARY KEY,
  id_realisation CHAR(36) NOT NULL UNIQUE,
  rpe TINYINT NOT NULL,
  commentaire TEXT NULL,
  duree_minutes INT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_realisation) REFERENCES seance_realisations(id_realisation) ON DELETE CASCADE
);
```

### 16.20 `posts`  📱  🆕

```sql
CREATE TABLE posts (
  id_post INT AUTO_INCREMENT PRIMARY KEY,
  id_client INT NOT NULL,
  content TEXT NOT NULL,
  image_url VARCHAR(500) NULL,
  rpe TINYINT NULL,
  rpe_general TINYINT NULL,
  rpe_details JSON NULL,
  rpe_visibility JSON NULL,
  mood VARCHAR(50) NULL,
  duration_min INT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  INDEX idx_created (created_at DESC)
);
```

### 16.21 `post_likes`  📱  🆕

```sql
CREATE TABLE post_likes (
  id_post INT NOT NULL,
  id_client INT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id_post, id_client),
  FOREIGN KEY (id_post) REFERENCES posts(id_post) ON DELETE CASCADE,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE
);
```

### 16.22 `post_comments`  📱  🆕

```sql
CREATE TABLE post_comments (
  id_comment INT AUTO_INCREMENT PRIMARY KEY,
  id_post INT NOT NULL,
  id_client INT NOT NULL,
  content TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_post) REFERENCES posts(id_post) ON DELETE CASCADE,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE
);
```

### 16.23 `client_points_log`  📱  🆕

```sql
CREATE TABLE client_points_log (
  id_log INT AUTO_INCREMENT PRIMARY KEY,
  id_client INT NOT NULL,
  delta INT NOT NULL,
  reason VARCHAR(100) NOT NULL,
  metadata JSON NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE,
  INDEX idx_client_date (id_client, created_at DESC)
);
```

### 16.24 `rank_badges`  📱🖥  🆕

```sql
CREATE TABLE rank_badges (
  id INT AUTO_INCREMENT PRIMARY KEY,
  tier ENUM('bronze','silver','gold','platinum','diamond','master','legend') NOT NULL,
  division INT NOT NULL,
  totem_slug VARCHAR(50) NOT NULL,
  url VARCHAR(500) NOT NULL,
  min_points INT NOT NULL DEFAULT 0,
  UNIQUE KEY uk_tier_div_totem (tier, division, totem_slug)
);
```

### 16.25 `logs_acces`  📱🖥

```sql
CREATE TABLE logs_acces (
  id_log INT AUTO_INCREMENT PRIMARY KEY,
  event_id CHAR(36) NULL,
  badge_uid VARCHAR(50) NOT NULL,
  id_client INT NULL,
  action ENUM('entree','sortie') NULL,
  resultat ENUM('autorise','refuse','en_attente') NOT NULL,
  acces_accorde TINYINT(1) NOT NULL,
  raison VARCHAR(255) NULL,
  porte VARCHAR(100) NULL,
  source VARCHAR(50) NULL,                 -- 'esp32', 'web', 'mobile'
  timestamp_utc DATETIME NOT NULL,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE SET NULL,
  INDEX idx_timestamp (timestamp_utc DESC),
  INDEX idx_client (id_client),
  INDEX idx_badge (badge_uid)
);
```

### 16.26 `rpe_visibility_user`  📱  🆕

> Optionnel : préférences de visibilité RPE par utilisateur.

```sql
CREATE TABLE rpe_visibility_user (
  id_client INT PRIMARY KEY,
  hidden JSON NOT NULL,                    -- ["technique", "fatigue"]
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id_client) ON DELETE CASCADE
);
```

---

### 16.27 Récap : tables à AJOUTER (nouvelles vs schéma admin actuel)

> Le schéma admin actuel suppose `users`/`clients`, `abonnements`, `programmes`, `seances_builder`, `programme_seances`, `programme_assignations`, `challenges`, `challenge_participants`, `exercices`, `logs_acces` déjà en place.

| Table | Origine | Statut |
|---|---|---|
| `coaches` | 📱 | 🆕 |
| `client_coaches` | 📱 | 🆕 |
| `events` | 📱 | 🆕 |
| `event_participants` | 📱 | 🆕 |
| `seance_blocks` | 📱 | 🆕 (optionnel si `data_json` seul) |
| `seance_block_exercices` | 📱 | 🆕 (optionnel) |
| `seance_builder_assignations` | 📱 | 🆕 |
| `seance_realisations` | 📱🖥 | 🆕 |
| `seance_performances` | 🖥 | 🆕 |
| `seance_feedback` | 🖥 | 🆕 |
| `posts` | 📱 | 🆕 |
| `post_likes` | 📱 | 🆕 |
| `post_comments` | 📱 | 🆕 |
| `client_points_log` | 📱 | 🆕 |
| `rank_badges` | 📱🖥 | 🆕 |
| `rpe_visibility_user` | 📱 | 🆕 (optionnel) |

### 16.28 Récap : tables à MODIFIER (ajout colonnes)

```sql
-- clients : ajouter tous les champs mobile
ALTER TABLE clients
  ADD COLUMN firebase_uid VARCHAR(128) UNIQUE NULL,
  ADD COLUMN phone VARCHAR(30) NULL,
  ADD COLUMN address VARCHAR(255) NULL,
  ADD COLUMN city VARCHAR(100) NULL,
  ADD COLUMN postal_code VARCHAR(20) NULL,
  ADD COLUMN country VARCHAR(100) NULL,
  ADD COLUMN gender ENUM('M','F','autre') NULL,
  ADD COLUMN birth_date DATE NULL,
  ADD COLUMN height DECIMAL(5,2) NULL,
  ADD COLUMN weight DECIMAL(5,2) NULL,
  ADD COLUMN blood_type VARCHAR(5) NULL,
  ADD COLUMN image_url VARCHAR(500) NULL,
  ADD COLUMN membership_level VARCHAR(50) NULL,
  ADD COLUMN member_since DATE NULL,
  ADD COLUMN favorite_sport VARCHAR(100) NULL,
  ADD COLUMN personal_physical_fatigue TINYINT NULL,
  ADD COLUMN personal_mental_fatigue TINYINT NULL,
  ADD COLUMN sleep_quality TINYINT NULL,
  ADD COLUMN sleep_quantity DECIMAL(4,2) NULL,
  ADD COLUMN diet_quality TINYINT NULL,
  ADD COLUMN diet_quantity TINYINT NULL,
  ADD COLUMN tobacco_consumption VARCHAR(50) NULL,
  ADD COLUMN alcohol_consumption VARCHAR(50) NULL,
  ADD COLUMN movement_limitations TEXT NULL,
  ADD COLUMN medical_condition TEXT NULL,
  ADD COLUMN specific_medical_treatment TEXT NULL,
  ADD COLUMN max_heart_rate SMALLINT NULL,
  ADD COLUMN rest_heart_rate SMALLINT NULL,
  ADD COLUMN weekly_visits SMALLINT DEFAULT 0,
  ADD COLUMN calories_burned INT DEFAULT 0,
  ADD COLUMN emergency_contact_name VARCHAR(200) NULL,
  ADD COLUMN emergency_contact_phone VARCHAR(30) NULL,
  ADD COLUMN totem_rang INT NULL,
  ADD COLUMN points INT DEFAULT 0,
  ADD COLUMN current_streak INT DEFAULT 0,
  ADD COLUMN last_session_date DATE NULL,
  ADD COLUMN total_seances INT DEFAULT 0;

-- exercices : ajout image + renommage video_url
ALTER TABLE exercices
  ADD COLUMN image_url VARCHAR(500) NULL,
  CHANGE COLUMN url_video video_url VARCHAR(500) NULL;

-- challenges : ajout champs mobile
ALTER TABLE challenges
  ADD COLUMN image_url VARCHAR(500) NULL,
  ADD COLUMN today_task VARCHAR(500) NULL;

-- challenge_participants : ajout completed_days
ALTER TABLE challenge_participants
  ADD COLUMN completed_days INT DEFAULT 0,
  ADD COLUMN last_validated_at DATETIME NULL;

-- programmes : nom_programme déjà OK admin ; ajouter image_url
ALTER TABLE programmes
  ADD COLUMN image_url VARCHAR(500) NULL;
```

---

## 17. Récapitulatif des clés d'ID renvoyées à la création

| Route | Clé renvoyée | Type | Alias acceptés |
|---|---|---|---|
| POST /users/register | `uid` (Firebase) + `id_client` | string + int | — |
| POST /clients | `id_client` | int | `id` |
| POST /events | `id_event` | int | — |
| POST /challenges | `id_challenge` | int | — |
| POST /exercices | `id_exercice` | int | `id` |
| POST /programmes | `id_programme` | int | `id` |
| POST /programmes/{id}/seances | `id_seance` | int | `id` |
| POST /seances-builder | `id_seance_builder` | int | — |
| POST /programme-assignations | `id_assignation` | **string UUID (CHAR(36))** | — |
| POST /me/seances/rpe | `id_realisation` | **string UUID** | — |
| POST /posts | `id_post` | int | — |
| POST /posts/{id}/comments | `id_comment` (data wrapper) | int | — |
| POST /posts/{id}/like | `likes_count` (data wrapper) | int | — |
| POST /me/points | — (success only) | — | — |

---

## 18. Ordre d'implémentation recommandé pour Samy

### Phase 1 — Bloquant (décisions + auth)
1. Valider les **6 décisions** §0 (1 réunion 30 min).
2. Routes auth : `POST /users/login`, `POST /users/register`, `POST /users/logout`, `GET /users/me`, `PUT /users/me`.
3. Migration `clients` (cf. §16.28).

### Phase 2 — Entités de base (CRUD simple)
4. `/clients` (CRUD + abonnement + nfc).
5. `/exercices` (CRUD).
6. `/coaches` (CRUD).

### Phase 3 — Métier coeur
7. `/programmes` (CRUD).
8. `/programmes/{id}/seances` (CRUD).
9. `/seances-builder` (CRUD avec décision §10.3 sur l'enveloppe).
10. `/challenges` (CRUD + participants).
11. `/events` (CRUD + register/unregister).
12. `/programme-assignations` (CRUD).

### Phase 4 — Utilisation client final (mobile)
13. `/me/seances`, `/me/seances/week-count`, `/me/seances/reorder`.
14. `/seances-assignations/{id}/...` (move, complete, realized-today, delete).
15. `/me/seances/rpe` (point critique, body complexe).
16. `/posts` + `/posts/{id}/like` + `/posts/{id}/comments`.
17. `/me/coaches` (CRUD).
18. Gamification : `/rank-badges`, `/me/stats`, `/me/points`, `/leaderboard`.
19. `/logs-acces` (POST + GET + GET today).

### Phase 5 — Fiche client admin (6 nouveaux endpoints 🆕)
20. `GET /clients/{id}/history`.
21. `GET /clients/{id}/performances/{builderId}`.
22. `GET /clients/{id}/session-feedback`.
23. `GET /clients/{id}/stats`.
24. `PUT /clients/{id}/totem`.
25. `/rank-badges` déjà couvert en phase 4.

### Phase 6 — Confort
26. `/dashboard/stats` (admin peut reconstruire sinon).
27. `/clients/{id}/profile` (vue publique mobile).

---

## 19. Notes finales & pièges à éviter

### 19.1 Types et UUIDs
- **UUIDs** en `CHAR(36)` (pas BINARY(16)) — admin envoie/lit des strings UUID standard.
- **Enums MySQL** en **minuscules avec underscores** (alignement avec les 2 clients).

### 19.2 Format de réponse
- **Toujours renvoyer `{success, data, error?}`** — même quand `data` est vide (`null` ou `[]`).
- Cas particuliers où admin lit une réponse BRUTE (à corriger) :
  - `GET /seances-builder/{id}` (cf. §10.3).
  - Tableau brut en fallback sur `GET /programmes` et `GET /exercices` — toléré mais à harmoniser.

### 19.3 SQL specifics
- `NULLS LAST` n'existe **pas** en MySQL → `ORDER BY col IS NULL, col DESC`.
- `Prefer: return=representation` n'est **pas** standard côté Slim — émettre directement la ligne créée dans le body de la réponse.

### 19.4 Idempotence et contraintes uniques
- `(id_client, id_seance_builder, date_prevue) UNIQUE` sur `seance_builder_assignations`.
- `(id_challenge, id_client) UNIQUE` sur `challenge_participants`.
- `(id_event, id_client)` PK composé sur `event_participants`.
- `(id_post, id_client)` PK composé sur `post_likes`.

### 19.5 Cascade FK obligatoires
- `seances_builder` → `seance_blocks` → `seance_block_exercices` : CASCADE.
- `seance_realisations` → `seance_performances` + `seance_feedback` : CASCADE.
- `challenges` → `challenge_participants` : CASCADE.
- `events` → `event_participants` : CASCADE.
- `posts` → `post_likes` + `post_comments` : CASCADE.
- `clients` → `client_points_log` + `seance_realisations` : CASCADE.

### 19.6 Format dates
- **Préférer ISO UTC** : `2026-06-12T10:00:00Z`.
- **Exception challenges** : mobile accepte `yyyy-MM-dd HH:mm:ss`. Recommandation : **supporter les deux** côté API (`DateTime::createFromFormat('Y-m-d H:i:s', ...)` puis fallback `strtotime`).

### 19.7 Robustesse des DTOs
- Le client admin C# accepte plusieurs noms pour le même champ (ex: `id` OU `id_exercice`, `id` OU `id_seance`, `nom_programme` OU `nom`). **Fixer un seul nom canonique par champ côté serveur** pour éviter la dette technique.
- Préférer la canonicalisation suivante :
  - **ID** : `id_<entity>` partout.
  - **Champ "nom"** : `nom_<entity>` quand possible (sauf très court : `nom` accepté pour Challenge).
  - **URL vidéo** : `video_url` (pas `url_video`).
  - **URL image** : `image_url`.

### 19.8 Auth
- **401** = token invalide/expiré → mobile rafraîchit Firebase, admin déconnecte.
- **404 sur `/users/register`** : admin interprète comme "inscription pas déployée" → renvoyer 405 plutôt si la route existe mais que l'inscription est temporairement désactivée.
- Le serveur **doit déduire `id_client` du token JWT** — ne jamais faire confiance à un `id_client` dans le body de requête venant du mobile.
- Pour les routes admin qui agissent au nom d'un client tiers (ex: `POST /challenges/{id}/participants` avec `id_client`), **vérifier le rôle** du caller (`role=admin|coach`) avant d'autoriser.

### 19.9 Pagination
- `rank_badges` (~224 lignes) : **pas** de pagination, cache mémoire client.
- `posts`, `logs-acces`, `clients` : pagination obligatoire (`limit` / `offset` ou `page` / `per_page`).
- Standardiser sur **`page` + `per_page`** (déjà utilisé par mobile pour `/clients`) ; conserver `limit` en alias temporaire pour admin.

### 19.10 Champ `data_json` des séances
- Stocker en `LONGTEXT` (et pas `JSON` MySQL) si l'on veut éviter la validation stricte.
- Représentation **double** (structurée + sérialisée) au choix selon l'architecture (cf. §10.1).

### 19.11 NFC
- Routes `POST /logs-acces` peuvent être appelées par un ESP32 (source=`esp32`) ou par l'app mobile (source=`mobile`).
- Le serveur valide la correspondance `badge_uid` ↔ `clients.nfc_uid` pour calculer `acces_accorde`.

### 19.12 Slugs totems (à confirmer)
Mobile et admin doivent s'accorder sur les **8 slugs canoniques**. Proposition :
`wolf`, `eagle`, `bear`, `lion`, `dragon`, `phoenix`, `tiger`, `shark`.

À vérifier dans `BurnOutAdmin/Models/Totems.cs` et `lib/models/totem.dart` avant de seeder `rank_badges`.

---

**FIN DU DOCUMENT** — toute question : ping `@nolan` ou ouvrir une issue avec le tag `api-unification`.
