# 📘 Spécification API Complète — CallOfPhoenix × BurnOut Admin

**Base URL :** `http://98.66.235.57`  
**Version cible :** 2.0  
**Stack serveur :** PHP 8.4 · Slim Framework · Firebase Auth · MariaDB 11.4  
**Date :** 2026-04-24  
**Auteur :** BurnOut Admin — Spécification technique complète

---

## Table des matières

1. [Vue d'ensemble et conventions](#1-vue-densemble-et-conventions)
2. [Authentification](#2-authentification)
3. [Clients & Abonnements](#3-clients--abonnements)
4. [Programmes (Templates)](#4-programmes-templates)
5. [Assignation de programmes aux clients](#5-assignation-de-programmes-aux-clients)
6. [Challenges & Classement](#6-challenges--classement)
7. [Séances & Program Builder](#7-séances--program-builder)
8. [Bibliothèque d'exercices](#8-bibliothèque-dexercices)
9. [Logs d'accès NFC](#9-logs-daccès-nfc)
10. [Dashboard & Statistiques](#10-dashboard--statistiques)
11. [Schéma de base de données](#11-schéma-de-base-de-données)
12. [Codes d'erreur standard](#12-codes-derreur-standard)
13. [Pagination & Filtres](#13-pagination--filtres)
14. [Plan d'implémentation prioritisé](#14-plan-dimplémentation-prioritisé)
15. [Intégration côté app — mapping JSON ↔ C#](#15-intégration-côté-app--mapping-json--c)

---

## 1. Vue d'ensemble et conventions

### Format des réponses

Toutes les réponses sont en `application/json`. Deux types de réponses :

**Réponse ressource unique :**
```json
{ "success": true, "data": { /* objet */ } }
```

**Réponse liste :**
```json
{ "success": true, "data": [ /* tableau */ ], "total": 42, "page": 1, "per_page": 50 }
```

**Réponse création (POST) :**
```json
{ "success": true, "id": 7, "data": { /* objet créé complet */ } }
```

**Réponse erreur :**
```json
{ "success": false, "error": "CODE_ERREUR", "message": "Description lisible par l'utilisateur." }
```

### Authentification

Toutes les routes marquées 🔒 nécessitent :
```
Authorization: Bearer <token_jwt_firebase>
```

Le token expire après **1 heure**. L'app vérifie localement l'expiration et redemande la connexion automatiquement.

### Formats de données

| Type | Format | Exemple |
|------|--------|---------|
| Dates | ISO 8601 UTC | `"2026-04-24T18:42:00Z"` |
| Dates simples | `YYYY-MM-DD` | `"2026-04-24"` |
| IDs ressources | Entier auto-incrémenté | `1`, `42` |
| IDs séances/exercices | UUID v4 | `"550e8400-e29b-41d4-a716-446655440000"` |
| Booléens | `true` / `false` | `true` |
| Valeurs décimales | Nombre JSON | `42.5` |

### Valeurs d'enum (constantes de chaîne)

> ⚠️ Les valeurs d'enum sont en **snake_case minuscule** côté API. L'app fait le mapping vers ses enums C#.

---

## 2. Authentification

### Routes existantes ✅

#### `POST /users/login` — Connexion Firebase
```
POST /users/login
Content-Type: application/json
```

**Body :**
```json
{
  "email": "admin@burnout.fr",
  "password": "motdepasse"
}
```

**Réponse `200` :**
```json
{
  "success": true,
  "token": "eyJhbGciOiJSUzI1NiIs..."
}
```

**Erreurs :**
```json
// 400 — champs manquants
{ "success": false, "error": "MISSING_FIELDS", "message": "Email et mot de passe requis." }

// 401 — mauvais identifiants
{ "success": false, "error": "INVALID_CREDENTIALS", "message": "Identifiants incorrects." }
```

---

#### `GET /users` — Liste des utilisateurs Firebase 🔒
```
GET /users
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "users": [
    {
      "uid": "T3Ibaxq1aMcipHNYBBhBgZcfF3l2",
      "email": "admin@burnout.fr",
      "displayName": "Admin BurnOut"
    }
  ]
}
```

---

### Routes à créer 🆕

#### `POST /users/register` — Création de compte
```
POST /users/register
Content-Type: application/json
```

**Body :**
```json
{
  "email": "nouveau@burnout.fr",
  "password": "motdepasse",
  "display_name": "Jean Dupont"
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "uid": "AbCdEfGhIjKlMn1234",
  "email": "nouveau@burnout.fr"
}
```

**Erreurs :**
```json
// 409 — email déjà utilisé
{ "success": false, "error": "EMAIL_ALREADY_EXISTS", "message": "Un compte avec cet email existe déjà." }

// 400 — mot de passe trop faible
{ "success": false, "error": "WEAK_PASSWORD", "message": "Le mot de passe doit contenir au moins 6 caractères." }
```

> **Côté app** : `ApiAuthService.RegisterAsync()` appelle `/users/register` puis `LoginAsync()` automatiquement.

---

#### `GET /users/me` — Profil de l'utilisateur connecté 🔒
```
GET /users/me
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "uid": "T3Ibaxq1aMcipHNYBBhBgZcfF3l2",
    "email": "admin@burnout.fr",
    "display_name": "Admin BurnOut",
    "role": "admin",
    "created_at": "2026-01-01T00:00:00Z"
  }
}
```

> **Côté app** : Utilisé pour afficher le nom complet dans la sidebar et la page Paramètres. Le `display_name` remplacera le fallback par initiales d'email actuel.

---

#### `POST /users/refresh` — Renouveler le token 🔒
```
POST /users/refresh
Content-Type: application/json
```

**Body :**
```json
{
  "refresh_token": "token_de_refresh_firebase"
}
```

**Réponse `200` :**
```json
{
  "success": true,
  "token": "eyJhbGciOiJSUzI1NiIs...",
  "expires_in": 3600
}
```

> **Côté app** : Le token Firebase fournit un `refresh_token` à la connexion initiale. Le stocker dans `Preferences` et l'utiliser pour renouveler sans reconnexion manuelle.

---

#### `POST /users/logout` — Révocation du token côté serveur 🔒
```
POST /users/logout
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

> **Côté app** : `IApiAuthService.Logout()` efface déjà les `Preferences` localement. Cet endpoint invalide le token côté Firebase pour la sécurité.

---

## 3. Clients & Abonnements

> ❌ **Absent — Priorité P0 (bloquante)** — Actuellement 100% mocké dans l'app.

### Modèle Client (JSON API)

```json
{
  "id": 1,
  "prenom": "Jean",
  "nom": "Dupont",
  "email": "jean.dupont@email.com",
  "statut": "actif",
  "nfc_uid": "04:A3:5B:12",
  "abonnement": {
    "type": "annuel",
    "date_debut": "2026-01-01",
    "date_fin": "2026-12-31",
    "auto_renouvellement": true
  },
  "created_at": "2025-12-01T10:00:00Z"
}
```

**Mapping vers `Client.cs` :**
| Champ JSON | Propriété C# | Type |
|------------|-------------|------|
| `id` | `Client.Id` | `int` |
| `prenom` | `Client.FirstName` | `string` |
| `nom` | `Client.LastName` | `string` |
| `email` | `Client.Email` | `string` |
| `statut` | `Client.Status` | `string` |
| `nfc_uid` | `Client.NfcUid` | `string?` |
| `abonnement` | `Client.Subscription` | `Subscription?` |
| `abonnement.type` | `Subscription.Type` | `string` |
| `abonnement.date_debut` | `Subscription.StartDate` | `DateTime` |
| `abonnement.date_fin` | `Subscription.EndDate` | `DateTime` |
| `abonnement.auto_renouvellement` | `Subscription.AutoRenewal` | `bool` |

**Valeurs `statut` :** `actif`, `expire`, `en_attente`, `suspendu`  
**Valeurs `abonnement.type` :** `mensuel`, `trimestriel`, `annuel`

---

### `GET /clients` — Liste des clients 🔒
```
GET /clients
Authorization: Bearer <token>
```

**Paramètres de filtre (optionnels) :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `statut` | string | Filtrer par statut : `actif`, `expire`, `en_attente`, `suspendu` |
| `search` | string | Recherche sur nom, prénom, email (LIKE %terme%) |
| `expiration_avant` | date | Abonnements expirant avant cette date (`YYYY-MM-DD`) |
| `page` | int | Numéro de page (défaut : 1) |
| `per_page` | int | Résultats par page (défaut : 50, max : 200) |

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "prenom": "Jean",
      "nom": "Dupont",
      "email": "jean.dupont@email.com",
      "statut": "actif",
      "nfc_uid": "04:A3:5B:12",
      "abonnement": {
        "type": "annuel",
        "date_debut": "2026-01-01",
        "date_fin": "2026-12-31",
        "auto_renouvellement": true
      },
      "created_at": "2025-12-01T10:00:00Z"
    }
  ],
  "total": 87,
  "page": 1,
  "per_page": 50
}
```

---

### `GET /clients/{id}` — Détail d'un client 🔒
```
GET /clients/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "prenom": "Jean",
    "nom": "Dupont",
    "email": "jean.dupont@email.com",
    "statut": "actif",
    "nfc_uid": "04:A3:5B:12",
    "abonnement": {
      "type": "annuel",
      "date_debut": "2026-01-01",
      "date_fin": "2026-12-31",
      "auto_renouvellement": true
    },
    "created_at": "2025-12-01T10:00:00Z"
  }
}
```

**Erreurs :**
```json
// 404
{ "success": false, "error": "CLIENT_NOT_FOUND", "message": "Client introuvable." }
```

---

### `POST /clients` — Créer un client 🔒
```
POST /clients
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "prenom": "Marie",
  "nom": "Martin",
  "email": "marie.martin@email.com",
  "statut": "en_attente",
  "nfc_uid": null
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 88,
  "data": {
    "id": 88,
    "prenom": "Marie",
    "nom": "Martin",
    "email": "marie.martin@email.com",
    "statut": "en_attente",
    "nfc_uid": null,
    "abonnement": null,
    "created_at": "2026-04-24T10:00:00Z"
  }
}
```

**Erreurs :**
```json
// 409 — email déjà pris
{ "success": false, "error": "EMAIL_ALREADY_EXISTS", "message": "Un client avec cet email existe déjà." }

// 400 — champs manquants
{ "success": false, "error": "MISSING_FIELDS", "message": "Les champs prenom, nom et email sont obligatoires." }
```

---

### `PUT /clients/{id}` — Modifier un client 🔒
```
PUT /clients/1
Authorization: Bearer <token>
Content-Type: application/json
```

**Body (tous les champs sont optionnels — PATCH sémantique) :**
```json
{
  "prenom": "Jean-Pierre",
  "nom": "Dupont",
  "email": "jp.dupont@email.com",
  "statut": "actif"
}
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "prenom": "Jean-Pierre",
    "nom": "Dupont",
    "email": "jp.dupont@email.com",
    "statut": "actif",
    "nfc_uid": "04:A3:5B:12",
    "abonnement": { /* inchangé */ },
    "created_at": "2025-12-01T10:00:00Z"
  }
}
```

---

### `DELETE /clients/{id}` — Supprimer un client 🔒
```
DELETE /clients/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

> ⚠️ Supprimer en cascade : logs NFC associés, participations aux challenges, programmes assignés.

---

### `PUT /clients/{id}/nfc` — Associer / dissocier une carte NFC 🔒
```
PUT /clients/1/nfc
Authorization: Bearer <token>
Content-Type: application/json
```

**Body pour associer :**
```json
{ "nfc_uid": "04:A3:5B:12" }
```

**Body pour dissocier :**
```json
{ "nfc_uid": null }
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "nfc_uid": "04:A3:5B:12"
  }
}
```

**Erreurs :**
```json
// 409 — UID déjà attribué à un autre client
{ "success": false, "error": "NFC_UID_ALREADY_ASSIGNED", "message": "Cette carte est déjà attribuée au client ID 5." }
```

---

### `GET /clients/nfc/{uid}` — Identifier un client par son UID NFC 🔒
```
GET /clients/nfc/04:A3:5B:12
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "prenom": "Jean",
    "nom": "Dupont",
    "statut": "actif",
    "abonnement": {
      "type": "annuel",
      "date_fin": "2026-12-31",
      "auto_renouvellement": true
    }
  }
}
```

**Réponse `404` :**
```json
{ "success": false, "error": "NFC_UID_NOT_FOUND", "message": "Aucun client associé à cet UID NFC." }
```

> **Utilisé par l'orchestrateur NFC** (`NfcOrchestrator.cs`) pour valider un accès en temps réel.

---

### `POST /clients/{id}/abonnement` — Créer ou renouveler un abonnement 🔒
```
POST /clients/1/abonnement
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "type": "annuel",
  "date_debut": "2026-05-01",
  "date_fin": "2027-05-01",
  "auto_renouvellement": true
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "data": {
    "type": "annuel",
    "date_debut": "2026-05-01",
    "date_fin": "2027-05-01",
    "auto_renouvellement": true
  }
}
```

---

### `PUT /clients/{id}/abonnement` — Modifier un abonnement existant 🔒
```
PUT /clients/1/abonnement
Authorization: Bearer <token>
Content-Type: application/json
```

**Body (champs optionnels) :**
```json
{
  "type": "mensuel",
  "date_fin": "2026-06-01",
  "auto_renouvellement": false
}
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "type": "mensuel",
    "date_debut": "2026-05-01",
    "date_fin": "2026-06-01",
    "auto_renouvellement": false
  }
}
```

**Erreur :**
```json
// 404 — pas d'abonnement existant
{ "success": false, "error": "SUBSCRIPTION_NOT_FOUND", "message": "Ce client n'a pas d'abonnement actif." }
```

---

## 4. Programmes (Templates)

> ⚠️ **Partiellement existant — Modèle incompatible — Priorité P1**  
> Les routes existent mais le modèle actuel n'a pas `type`, `level`, `sessions_per_week`, `is_actif`.

### Modèle Programme (JSON API)

```json
{
  "id_programme": 1,
  "nom_programme": "Programme Force Intermédiaire",
  "description": "12 semaines de force, 3 séances/semaine",
  "type": "force",
  "niveau": "intermediaire",
  "duree_semaines": 12,
  "seances_par_semaine": 3,
  "is_actif": true,
  "id_createur": 1,
  "created_at": "2026-01-15T09:00:00Z"
}
```

**Mapping vers `Programme.cs` :**
| Champ JSON | Propriété C# | Type |
|------------|-------------|------|
| `id_programme` | `Programme.Id` | `int` |
| `nom_programme` | `Programme.Name` | `string` |
| `description` | `Programme.Description` | `string` |
| `type` | `Programme.Type` | `ProgrammeType` |
| `niveau` | `Programme.Level` | `ProgrammeLevel` |
| `duree_semaines` | `Programme.DurationWeeks` | `int` |
| `seances_par_semaine` | `Programme.SessionsPerWeek` | `int` |
| `is_actif` | `Programme.IsActive` | `bool` |
| `created_at` | `Programme.CreatedAt` | `DateTime` |

**Valeurs `type` :** `force`, `cardio`, `souplesse`, `mixte`  
**Mapping C# :** `force` → `ProgrammeType.Strength` · `cardio` → `ProgrammeType.Cardio` · `souplesse` → `ProgrammeType.Flexibility` · `mixte` → `ProgrammeType.Mixed`

**Valeurs `niveau` :** `debutant`, `intermediaire`, `avance`  
**Mapping C# :** `debutant` → `ProgrammeLevel.Beginner` · `intermediaire` → `ProgrammeLevel.Intermediate` · `avance` → `ProgrammeLevel.Advanced`

---

### `GET /programmes` — Liste des programmes 🔒 *(à enrichir)*
```
GET /programmes
Authorization: Bearer <token>
```

**Paramètres optionnels :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `type` | string | Filtrer : `force`, `cardio`, `souplesse`, `mixte` |
| `niveau` | string | Filtrer : `debutant`, `intermediaire`, `avance` |
| `is_actif` | bool | `true` = programmes actifs uniquement |
| `search` | string | Recherche sur le nom |

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id_programme": 1,
      "nom_programme": "Programme Force Intermédiaire",
      "description": "12 semaines de force, 3 séances/semaine",
      "type": "force",
      "niveau": "intermediaire",
      "duree_semaines": 12,
      "seances_par_semaine": 3,
      "is_actif": true,
      "id_createur": 1,
      "created_at": "2026-01-15T09:00:00Z"
    }
  ],
  "total": 15
}
```

---

### `GET /programmes/{id}` — Détail d'un programme 🔒 *(nouveau)*
```
GET /programmes/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "id_programme": 1,
    "nom_programme": "Programme Force Intermédiaire",
    "description": "12 semaines de force, 3 séances/semaine",
    "type": "force",
    "niveau": "intermediaire",
    "duree_semaines": 12,
    "seances_par_semaine": 3,
    "is_actif": true,
    "id_createur": 1,
    "created_at": "2026-01-15T09:00:00Z"
  }
}
```

---

### `POST /programmes` — Créer un programme 🔒 *(à enrichir)*
```
POST /programmes
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "nom_programme": "Nouveau Programme Cardio",
  "description": "Programme cardio 8 semaines",
  "type": "cardio",
  "niveau": "debutant",
  "duree_semaines": 8,
  "seances_par_semaine": 4,
  "is_actif": true,
  "id_createur": 1
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 16,
  "data": {
    "id_programme": 16,
    "nom_programme": "Nouveau Programme Cardio",
    "description": "Programme cardio 8 semaines",
    "type": "cardio",
    "niveau": "debutant",
    "duree_semaines": 8,
    "seances_par_semaine": 4,
    "is_actif": true,
    "id_createur": 1,
    "created_at": "2026-04-24T10:00:00Z"
  }
}
```

---

### `PUT /programmes/{id}` — Modifier un programme 🔒 *(à enrichir)*
```
PUT /programmes/1
Authorization: Bearer <token>
Content-Type: application/json
```

**Body (champs optionnels) :**
```json
{
  "nom_programme": "Programme Force Avancé",
  "description": "Nouvelle description",
  "type": "force",
  "niveau": "avance",
  "duree_semaines": 16,
  "seances_par_semaine": 4,
  "is_actif": true
}
```

**Réponse `200` :**
```json
{ "success": true, "data": { /* programme mis à jour complet */ } }
```

---

### `DELETE /programmes/{id}` — Supprimer un programme 🔒
```
DELETE /programmes/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

---

## 5. Assignation de programmes aux clients

> ❌ **Absent — Priorité P1**

### Modèle Assignation (JSON API)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "id_client": 1,
  "id_programme": 3,
  "nom_programme": "Programme Force Intermédiaire",
  "date_assignation": "2026-04-24T10:00:00Z",
  "date_debut": "2026-05-01",
  "date_fin": "2026-08-01"
}
```

**Mapping vers `ClientProgramAssignment.cs` :**
| Champ JSON | Propriété C# | Type |
|------------|-------------|------|
| `id` | `ClientProgramAssignment.Id` | `Guid` |
| `id_client` | `ClientProgramAssignment.ClientId` | `int` |
| `nom_programme` | `ClientProgramAssignment.ProgramName` | `string` |
| `date_assignation` | `ClientProgramAssignment.AssignedAt` | `DateTime` |

---

### `GET /clients/{id}/programmes` — Programmes assignés au client 🔒
```
GET /clients/1/programmes
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "id_client": 1,
      "id_programme": 3,
      "nom_programme": "Programme Force Intermédiaire",
      "date_assignation": "2026-04-24T10:00:00Z",
      "date_debut": "2026-05-01",
      "date_fin": "2026-08-01"
    }
  ]
}
```

---

### `POST /clients/{id}/programmes` — Assigner un programme à un client 🔒
```
POST /clients/1/programmes
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "id_programme": 3,
  "date_debut": "2026-05-01",
  "date_fin": "2026-08-01"
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "id_client": 1,
    "id_programme": 3,
    "nom_programme": "Programme Force Intermédiaire",
    "date_assignation": "2026-04-24T10:00:00Z",
    "date_debut": "2026-05-01",
    "date_fin": "2026-08-01"
  }
}
```

---

### `DELETE /clients/{id}/programmes/{assignment_id}` — Désassigner un programme 🔒
```
DELETE /clients/1/programmes/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

---

## 6. Challenges & Classement

> ❌ **Absent — Priorité P1** — Actuellement en SQLite local.

### Modèle Challenge (JSON API)

```json
{
  "id": 1,
  "nom": "Challenge Cardio Avril",
  "description": "Cumulez le plus de kilomètres en course à pied",
  "type": "cardio",
  "date_debut": "2026-04-01T00:00:00Z",
  "date_fin": "2026-04-30T23:59:59Z",
  "objectif": 100,
  "unite_objectif": "km",
  "statut": "actif",
  "recompense_description": "1 mois offert",
  "created_at": "2026-03-20T10:00:00Z"
}
```

**Mapping vers `Challenge.cs` :**
| Champ JSON | Propriété C# | Type |
|------------|-------------|------|
| `id` | `Challenge.Id` | `int` |
| `nom` | `Challenge.Name` | `string` |
| `description` | `Challenge.Description` | `string` |
| `type` | `Challenge.Type` | `ChallengeType` |
| `date_debut` | `Challenge.StartDate` | `DateTime` |
| `date_fin` | `Challenge.EndDate` | `DateTime` |
| `objectif` | `Challenge.TargetGoal` | `int` |
| `unite_objectif` | `Challenge.GoalUnit` | `string` |
| `statut` | `Challenge.Status` | `ChallengeStatus` |
| `recompense_description` | `Challenge.RewardDescription` | `string` |
| `created_at` | `Challenge.CreatedAt` | `DateTime` |

**Valeurs `type` :** `general`, `cardio`, `force`, `endurance`, `poids`, `flexibilite`  
**Mapping C# :** `general` → `ChallengeType.General` · `cardio` → `ChallengeType.Cardio` · `force` → `ChallengeType.Force` · `endurance` → `ChallengeType.Endurance` · `poids` → `ChallengeType.Poids` · `flexibilite` → `ChallengeType.Flexibilite`

**Valeurs `statut` :** `a_venir`, `actif`, `termine`, `annule`  
**Mapping C# :** `a_venir` → `ChallengeStatus.Upcoming` · `actif` → `ChallengeStatus.Active` · `termine` → `ChallengeStatus.Completed` · `annule` → `ChallengeStatus.Cancelled`

---

### `GET /challenges` — Liste des challenges 🔒
```
GET /challenges
Authorization: Bearer <token>
```

**Paramètres optionnels :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `statut` | string | `a_venir`, `actif`, `termine`, `annule` |
| `type` | string | `general`, `cardio`, `force`, `endurance`, `poids`, `flexibilite` |

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nom": "Challenge Cardio Avril",
      "description": "Cumulez le plus de kilomètres en course à pied",
      "type": "cardio",
      "date_debut": "2026-04-01T00:00:00Z",
      "date_fin": "2026-04-30T23:59:59Z",
      "objectif": 100,
      "unite_objectif": "km",
      "statut": "actif",
      "recompense_description": "1 mois offert",
      "created_at": "2026-03-20T10:00:00Z"
    }
  ],
  "total": 8
}
```

---

### `GET /challenges/{id}` — Détail d'un challenge 🔒
```
GET /challenges/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": { /* objet challenge complet */ }
}
```

---

### `POST /challenges` — Créer un challenge 🔒
```
POST /challenges
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "nom": "Challenge Force Mai",
  "description": "Max de séries de pompes en mai",
  "type": "force",
  "date_debut": "2026-05-01T00:00:00Z",
  "date_fin": "2026-05-31T23:59:59Z",
  "objectif": 5000,
  "unite_objectif": "répétitions",
  "statut": "a_venir",
  "recompense_description": "T-shirt exclusif"
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 9,
  "data": { /* challenge créé complet */ }
}
```

---

### `PUT /challenges/{id}` — Modifier un challenge 🔒
```
PUT /challenges/1
Authorization: Bearer <token>
Content-Type: application/json
```

**Body (champs optionnels) :**
```json
{
  "nom": "Challenge Cardio Avril — Édition Spéciale",
  "statut": "termine"
}
```

**Réponse `200` :**
```json
{ "success": true, "data": { /* challenge mis à jour complet */ } }
```

---

### `DELETE /challenges/{id}` — Supprimer un challenge 🔒
```
DELETE /challenges/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

> ⚠️ Supprimer en cascade tous les participants du challenge.

---

### `GET /challenges/{id}/participants` — Classement des participants 🔒
```
GET /challenges/1/participants
Authorization: Bearer <token>
```

**Réponse `200` (triée par `valeur_actuelle` décroissant) :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "id_challenge": 1,
      "id_client": 3,
      "nom_client": "Jean Dupont",
      "valeur_actuelle": 87.5,
      "rang": 1,
      "joined_at": "2026-04-02T08:30:00Z"
    },
    {
      "id": 2,
      "id_challenge": 1,
      "id_client": 7,
      "nom_client": "Marie Martin",
      "valeur_actuelle": 72.0,
      "rang": 2,
      "joined_at": "2026-04-03T09:00:00Z"
    }
  ]
}
```

**Mapping vers `ChallengeParticipant.cs` :**
| Champ JSON | Propriété C# |
|------------|-------------|
| `id` | `ChallengeParticipant.Id` |
| `id_challenge` | `ChallengeParticipant.ChallengeId` |
| `id_client` | `ChallengeParticipant.ClientId` |
| `nom_client` | `ChallengeParticipant.ClientName` |
| `valeur_actuelle` | `ChallengeParticipant.CurrentValue` |
| `joined_at` | `ChallengeParticipant.JoinedAt` |

---

### `POST /challenges/{id}/participants` — Inscrire / mettre à jour un participant 🔒

Cet endpoint fait un **upsert** : si le client est déjà inscrit, sa valeur est mise à jour ; sinon, il est inscrit.

```
POST /challenges/1/participants
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "id_client": 3,
  "valeur": 42.5
}
```

**Réponse `200` ou `201` :**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "id_challenge": 1,
    "id_client": 3,
    "nom_client": "Jean Dupont",
    "valeur_actuelle": 42.5,
    "rang": 1,
    "joined_at": "2026-04-02T08:30:00Z"
  }
}
```

**Erreurs :**
```json
// 404 — client introuvable
{ "success": false, "error": "CLIENT_NOT_FOUND", "message": "Client introuvable." }
```

---

### `DELETE /challenges/{id}/participants/{participant_id}` — Retirer un participant 🔒
```
DELETE /challenges/1/participants/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

---

## 7. Séances & Program Builder

> ❌ **Absent — Priorité P2** — Actuellement en SQLite local via `SqliteSessionLibraryService`.

### Structure d'une séance (SessionModel complet)

Le `data_json` contient le `SessionModel` C# sérialisé en JSON :

```json
{
  "id": "uuid",
  "name": "Push Day",
  "order": 1,
  "categories": [
    {
      "id": "uuid",
      "name": "Échauffement",
      "order": 1,
      "sub_categories": [
        {
          "id": "uuid",
          "name": "Rotation épaules",
          "order": 1,
          "sets": 2,
          "rest_time": 30,
          "type": "warmup",
          "exercises": [
            {
              "id": "uuid",
              "name": "Rotation épaules",
              "sets": 2,
              "reps": 15,
              "weight": 0.0,
              "rpe": 5.0,
              "order": 1
            }
          ]
        }
      ]
    }
  ]
}
```

### Modèle SavedSession (JSON API)

```json
{
  "id": 1,
  "nom": "Push Day",
  "description": "Poussée horizontale et verticale",
  "exercise_count": 8,
  "category_count": 3,
  "data_json": { /* SessionModel sérialisé — voir ci-dessus */ },
  "created_at": "2026-04-10T14:00:00Z"
}
```

**Mapping vers `SavedSessionEntry.cs` :**
| Champ JSON | Propriété C# |
|------------|-------------|
| `id` | `SavedSessionEntry.Id` |
| `nom` | `SavedSessionEntry.Name` |
| `description` | `SavedSessionEntry.Description` |
| `exercise_count` | `SavedSessionEntry.ExerciseCount` |
| `category_count` | `SavedSessionEntry.CategoryCount` |
| `data_json` | `SavedSessionEntry.DataJson` (string JSON) |
| `created_at` | `SavedSessionEntry.CreatedAt` |

---

### `GET /seances` — Liste des séances sauvegardées 🔒
```
GET /seances
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nom": "Push Day",
      "description": "Poussée horizontale et verticale",
      "exercise_count": 8,
      "category_count": 3,
      "data_json": { /* SessionModel complet */ },
      "created_at": "2026-04-10T14:00:00Z"
    }
  ],
  "total": 12
}
```

---

### `GET /seances/{id}` — Détail d'une séance 🔒
```
GET /seances/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": { /* séance complète avec data_json */ }
}
```

---

### `POST /seances` — Sauvegarder une séance 🔒
```
POST /seances
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "nom": "Push Day",
  "description": "Poussée horizontale et verticale",
  "exercise_count": 8,
  "category_count": 3,
  "data_json": { /* SessionModel C# sérialisé */ }
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 13,
  "data": { /* séance créée complète */ }
}
```

---

### `DELETE /seances/{id}` — Supprimer une séance 🔒
```
DELETE /seances/1
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

---

## 8. Bibliothèque d'exercices

> ❌ **Absent — Priorité P2** — Actuellement en SQLite local via `SqliteExerciseLibraryService`. Plus de 100 exercices.

### Modèle Exercice (JSON API)

```json
{
  "id": 1,
  "nom": "Squat",
  "categorie": "Force",
  "groupe_musculaire": "Quadriceps",
  "tags": ["jambes", "compound"],
  "is_default": true,
  "created_at": "2026-01-01T00:00:00Z"
}
```

**Mapping vers `ExerciseLibraryItem.cs` :**
| Champ JSON | Propriété C# |
|------------|-------------|
| `id` | `ExerciseLibraryItem.DbId` |
| `nom` | `ExerciseLibraryItem.Name` |
| `categorie` | `ExerciseLibraryItem.Category` |
| `groupe_musculaire` | `ExerciseLibraryItem.MuscleGroup` |
| `tags` | `ExerciseLibraryItem.Tags` |

---

### `GET /exercices` — Liste des exercices 🔒
```
GET /exercices
Authorization: Bearer <token>
```

**Paramètres optionnels :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `categorie` | string | Filtrer par catégorie |
| `groupe_musculaire` | string | Filtrer par groupe musculaire |
| `search` | string | Recherche dans le nom |
| `is_default` | bool | `true` = exercices par défaut uniquement |

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nom": "Squat",
      "categorie": "Force",
      "groupe_musculaire": "Quadriceps",
      "tags": ["jambes", "compound"],
      "is_default": true,
      "created_at": "2026-01-01T00:00:00Z"
    },
    {
      "id": 2,
      "nom": "Développé couché",
      "categorie": "Force",
      "groupe_musculaire": "Pectoraux",
      "tags": ["poitrine", "compound"],
      "is_default": true,
      "created_at": "2026-01-01T00:00:00Z"
    }
  ],
  "total": 112
}
```

---

### `POST /exercices` — Ajouter un exercice personnalisé 🔒
```
POST /exercices
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "nom": "Fentes bulgares",
  "categorie": "Force",
  "groupe_musculaire": "Quadriceps",
  "tags": ["jambes", "unilatéral"]
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 113,
  "data": {
    "id": 113,
    "nom": "Fentes bulgares",
    "categorie": "Force",
    "groupe_musculaire": "Quadriceps",
    "tags": ["jambes", "unilatéral"],
    "is_default": false,
    "created_at": "2026-04-24T10:00:00Z"
  }
}
```

---

### `DELETE /exercices/{id}` — Supprimer un exercice personnalisé 🔒
```
DELETE /exercices/113
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{ "success": true }
```

**Erreur :**
```json
// 403 — tentative de supprimer un exercice par défaut
{ "success": false, "error": "CANNOT_DELETE_DEFAULT", "message": "Les exercices par défaut ne peuvent pas être supprimés." }
```

---

## 9. Logs d'accès NFC

> ❌ **Absent — Priorité P1** — Actuellement en SQLite local via `SqliteNfcLogRepository`. Reçus en temps réel via MQTT.

### Modèle Log NFC (JSON API)

```json
{
  "id": 1,
  "event_id": "a3f2c1d0-e29b-41d4-a716-446655440000",
  "uid_nfc": "04:A3:5B:12",
  "id_client": 3,
  "nom_client": "Jean Dupont",
  "resultat": "autorise",
  "raison": null,
  "porte": "Entrée principale",
  "source": "rfid",
  "timestamp": "2026-04-24T18:42:00Z"
}
```

**Mapping vers `NfcLog.cs` :**
| Champ JSON | Propriété C# |
|------------|-------------|
| `id` | `NfcLog.Id` |
| `event_id` | `NfcLog.EventId` |
| `uid_nfc` | `NfcLog.Uid` |
| `id_client` | `NfcLog.ClientId` |
| `nom_client` | `NfcLog.ClientName` |
| `resultat` | `NfcLog.Result` (`NfcAccessResult`) |
| `raison` | `NfcLog.Reason` |
| `porte` | `NfcLog.Door` |
| `source` | `NfcLog.Source` |
| `timestamp` | `NfcLog.TimestampUtc` |

**Valeurs `resultat` :** `en_attente`, `autorise`, `refuse`  
**Mapping C# :** `en_attente` → `NfcAccessResult.Pending` (0) · `autorise` → `NfcAccessResult.Authorized` (1) · `refuse` → `NfcAccessResult.Denied` (2)

---

### `GET /acces` — Liste des logs d'accès 🔒
```
GET /acces
Authorization: Bearer <token>
```

**Paramètres optionnels :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `date` | date | Filtrer par date (`YYYY-MM-DD`) |
| `id_client` | int | Logs d'un client spécifique |
| `resultat` | string | `autorise`, `refuse`, `en_attente` |
| `porte` | string | Filtrer par porte/lecteur |
| `page` | int | Page (défaut : 1) |
| `per_page` | int | Taille de page (défaut : 100, max : 500) |

**Réponse `200` :**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "event_id": "a3f2c1d0-e29b-41d4-a716-446655440000",
      "uid_nfc": "04:A3:5B:12",
      "id_client": 3,
      "nom_client": "Jean Dupont",
      "resultat": "autorise",
      "raison": null,
      "porte": "Entrée principale",
      "source": "rfid",
      "timestamp": "2026-04-24T18:42:00Z"
    }
  ],
  "total": 1247,
  "page": 1,
  "per_page": 100
}
```

---

### `POST /acces` — Enregistrer un accès NFC 🔒

> Appelé par `NfcOrchestrator` à chaque scan validé.

```
POST /acces
Authorization: Bearer <token>
Content-Type: application/json
```

**Body :**
```json
{
  "event_id": "a3f2c1d0-e29b-41d4-a716-446655440000",
  "uid_nfc": "04:A3:5B:12",
  "id_client": 3,
  "nom_client": "Jean Dupont",
  "resultat": "autorise",
  "raison": null,
  "porte": "Entrée principale",
  "source": "rfid",
  "timestamp": "2026-04-24T18:42:00Z"
}
```

**Réponse `201` :**
```json
{
  "success": true,
  "id": 1248,
  "data": { /* log créé complet */ }
}
```

> **Idempotence** : si `event_id` existe déjà, retourner `200` avec le log existant (évite les doublons si l'orchestrateur retry).

---

### `GET /acces/today` — Accès du jour 🔒
```
GET /acces/today
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": [ /* mêmes objets que GET /acces */ ],
  "total": 23
}
```

---

### `GET /acces/stats` — Statistiques d'accès 🔒
```
GET /acces/stats
Authorization: Bearer <token>
```

**Paramètres optionnels :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| `days` | int | Période en jours (défaut : 7) |

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "today": 23,
    "this_week": 152,
    "authorized_rate": 0.94,
    "by_day": [
      { "date": "2026-04-24", "count": 23, "authorized": 22, "refused": 1 },
      { "date": "2026-04-23", "count": 31, "authorized": 29, "refused": 2 }
    ]
  }
}
```

---

## 10. Dashboard & Statistiques

> ❌ **Absent — Priorité P2** — Actuellement 100% mocké via `MockDashboardService`.

### `GET /dashboard` — Statistiques globales 🔒
```
GET /dashboard
Authorization: Bearer <token>
```

**Réponse `200` :**
```json
{
  "success": true,
  "data": {
    "clients_actifs": 45,
    "acces_aujourd_hui": 23,
    "alertes": 2,
    "challenges_actifs": 3,
    "programmes_actifs": 8,
    "abonnements_expirant_bientot": 5,
    "statut_systeme": "online",
    "acces_recents": [
      {
        "heure": "18:42",
        "nom_client": "Jean Dupont",
        "porte": "Entrée principale",
        "autorise": true
      },
      {
        "heure": "18:35",
        "nom_client": "Carte inconnue",
        "porte": "Entrée principale",
        "autorise": false
      }
    ]
  }
}
```

**Mapping vers `DashboardStats.cs` :**
| Champ JSON | Propriété C# |
|------------|-------------|
| `clients_actifs` | `DashboardStats.ActiveClientsCount` |
| `acces_aujourd_hui` | `DashboardStats.TodayNfcAccessCount` |
| `alertes` | `DashboardStats.AlertsCount` |
| `challenges_actifs` | `DashboardStats.ActiveChallengesCount` |
| `programmes_actifs` | `DashboardStats.ActiveProgrammesCount` |
| `abonnements_expirant_bientot` | `DashboardStats.ExpiringSubscriptionsCount` |
| `statut_systeme` | `DashboardStats.SystemStatus` |
| `acces_recents[].heure` | `DashboardNfcEntry.Time` |
| `acces_recents[].nom_client` | `DashboardNfcEntry.ClientName` |
| `acces_recents[].porte` | `DashboardNfcEntry.Door` |
| `acces_recents[].autorise` | `DashboardNfcEntry.IsAuthorized` |

**Valeurs `statut_systeme` :** `online`, `maintenance`, `offline`  
**Mapping C# :** `online` → `SystemStatus.Online` · `maintenance` → `SystemStatus.Maintenance` · `offline` → `SystemStatus.Offline`

> **Logique `alertes`** : compter les abonnements expirés + clients sans carte NFC actifs + challenges à activer.  
> **`abonnements_expirant_bientot`** : abonnements dont `date_fin` ≤ aujourd'hui + 30 jours.

---

## 11. Schéma de base de données

### Tables SQL (MariaDB)

```sql
-- ── CLIENTS ──────────────────────────────────────────────────────────
CREATE TABLE clients (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    prenom        VARCHAR(100) NOT NULL,
    nom           VARCHAR(100) NOT NULL,
    email         VARCHAR(255) NOT NULL UNIQUE,
    statut        ENUM('actif','expire','en_attente','suspendu') NOT NULL DEFAULT 'en_attente',
    nfc_uid       VARCHAR(50) NULL UNIQUE,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_statut (statut),
    INDEX idx_nfc_uid (nfc_uid),
    INDEX idx_email (email)
);

-- ── ABONNEMENTS ──────────────────────────────────────────────────────
CREATE TABLE abonnements (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    id_client           INT NOT NULL,
    type                ENUM('mensuel','trimestriel','annuel') NOT NULL,
    date_debut          DATE NOT NULL,
    date_fin            DATE NOT NULL,
    auto_renouvellement TINYINT(1) NOT NULL DEFAULT 0,
    created_at          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_client) REFERENCES clients(id) ON DELETE CASCADE,
    INDEX idx_id_client (id_client),
    INDEX idx_date_fin (date_fin)
);

-- ── PROGRAMMES (templates) ───────────────────────────────────────────
-- Modification de la table existante (ajouter colonnes manquantes)
ALTER TABLE programmes
    ADD COLUMN type              ENUM('force','cardio','souplesse','mixte') NOT NULL DEFAULT 'mixte' AFTER description,
    ADD COLUMN niveau            ENUM('debutant','intermediaire','avance')  NOT NULL DEFAULT 'intermediaire' AFTER type,
    ADD COLUMN duree_semaines    INT NOT NULL DEFAULT 8 AFTER niveau,
    ADD COLUMN seances_par_semaine INT NOT NULL DEFAULT 3 AFTER duree_semaines,
    ADD COLUMN is_actif          TINYINT(1) NOT NULL DEFAULT 1 AFTER seances_par_semaine,
    ADD COLUMN created_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER is_actif;

-- Supprimer les colonnes id_client et date_debut/fin du template (les mettre sur l'assignation)
-- ALTER TABLE programmes DROP COLUMN id_client, DROP COLUMN date_debut, DROP COLUMN date_fin;
-- ⚠️ Migration : vérifier qu'aucune donnée existante n'est perdue avant de supprimer

-- ── ASSIGNATIONS PROGRAMMES ─────────────────────────────────────────
CREATE TABLE programme_assignations (
    id              CHAR(36) NOT NULL PRIMARY KEY,  -- UUID
    id_client       INT NOT NULL,
    id_programme    INT NOT NULL,
    date_assignation DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    date_debut      DATE NOT NULL,
    date_fin        DATE NULL,
    FOREIGN KEY (id_client)    REFERENCES clients(id)    ON DELETE CASCADE,
    FOREIGN KEY (id_programme) REFERENCES programmes(id_programme) ON DELETE CASCADE,
    INDEX idx_id_client (id_client),
    INDEX idx_id_programme (id_programme)
);

-- ── CHALLENGES ──────────────────────────────────────────────────────
CREATE TABLE challenges (
    id                    INT AUTO_INCREMENT PRIMARY KEY,
    nom                   VARCHAR(200) NOT NULL,
    description           TEXT,
    type                  ENUM('general','cardio','force','endurance','poids','flexibilite') NOT NULL DEFAULT 'general',
    date_debut            DATETIME NOT NULL,
    date_fin              DATETIME NOT NULL,
    objectif              INT NOT NULL DEFAULT 0,
    unite_objectif        VARCHAR(50) NOT NULL DEFAULT '',
    statut                ENUM('a_venir','actif','termine','annule') NOT NULL DEFAULT 'a_venir',
    recompense_description TEXT,
    created_at            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_statut (statut),
    INDEX idx_type (type),
    INDEX idx_dates (date_debut, date_fin)
);

-- ── PARTICIPANTS CHALLENGES ──────────────────────────────────────────
CREATE TABLE challenge_participants (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    id_challenge    INT NOT NULL,
    id_client       INT NOT NULL,
    nom_client      VARCHAR(200) NOT NULL,  -- dénormalisé pour affichage rapide
    valeur_actuelle DOUBLE NOT NULL DEFAULT 0,
    joined_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_challenge_client (id_challenge, id_client),
    FOREIGN KEY (id_challenge) REFERENCES challenges(id) ON DELETE CASCADE,
    FOREIGN KEY (id_client)    REFERENCES clients(id)    ON DELETE CASCADE,
    INDEX idx_challenge_valeur (id_challenge, valeur_actuelle DESC)
);

-- ── SÉANCES (Program Builder) ────────────────────────────────────────
CREATE TABLE seances (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nom             VARCHAR(200) NOT NULL,
    description     TEXT,
    exercise_count  INT NOT NULL DEFAULT 0,
    category_count  INT NOT NULL DEFAULT 0,
    data_json       JSON NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_nom (nom)
);

-- ── BIBLIOTHÈQUE D'EXERCICES ─────────────────────────────────────────
CREATE TABLE exercices (
    id                INT AUTO_INCREMENT PRIMARY KEY,
    nom               VARCHAR(200) NOT NULL,
    categorie         VARCHAR(100) NOT NULL DEFAULT '',
    groupe_musculaire VARCHAR(100) NOT NULL DEFAULT '',
    tags              JSON NOT NULL DEFAULT '[]',
    is_default        TINYINT(1) NOT NULL DEFAULT 0,
    created_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_categorie (categorie),
    INDEX idx_groupe_musculaire (groupe_musculaire),
    FULLTEXT INDEX ft_nom (nom)
);

-- ── LOGS D'ACCÈS NFC ────────────────────────────────────────────────
CREATE TABLE acces_nfc (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    event_id        CHAR(36) NOT NULL UNIQUE,  -- UUID, idempotence
    uid_nfc         VARCHAR(50) NOT NULL,
    id_client       INT NULL,
    nom_client      VARCHAR(200) NOT NULL DEFAULT '',
    resultat        ENUM('en_attente','autorise','refuse') NOT NULL DEFAULT 'en_attente',
    raison          VARCHAR(500) NULL,
    porte           VARCHAR(100) NULL,
    source          VARCHAR(100) NOT NULL DEFAULT 'rfid',
    timestamp_utc   DATETIME NOT NULL,
    FOREIGN KEY (id_client) REFERENCES clients(id) ON DELETE SET NULL,
    INDEX idx_uid_nfc (uid_nfc),
    INDEX idx_id_client (id_client),
    INDEX idx_timestamp (timestamp_utc),
    INDEX idx_resultat (resultat)
);
```

---

## 12. Codes d'erreur standard

### Codes HTTP

| Code | Signification |
|------|---------------|
| `200` | Succès |
| `201` | Ressource créée |
| `400` | Données invalides / champs manquants |
| `401` | Token absent, invalide ou expiré |
| `403` | Accès interdit (droits insuffisants) |
| `404` | Ressource introuvable |
| `405` | Méthode HTTP non supportée |
| `409` | Conflit (doublon, contrainte d'unicité) |
| `500` | Erreur serveur interne |

### Codes d'erreur métier

| Code erreur | Contexte |
|-------------|----------|
| `MISSING_FIELDS` | Champs obligatoires absents dans le body |
| `INVALID_CREDENTIALS` | Email/mot de passe incorrect (login) |
| `TOKEN_EXPIRED` | Token JWT expiré |
| `TOKEN_INVALID` | Token JWT invalide ou malformé |
| `EMAIL_ALREADY_EXISTS` | Email déjà utilisé (register/create client) |
| `WEAK_PASSWORD` | Mot de passe trop faible |
| `CLIENT_NOT_FOUND` | Client introuvable |
| `NFC_UID_NOT_FOUND` | UID NFC non associé |
| `NFC_UID_ALREADY_ASSIGNED` | UID NFC déjà attribué à un autre client |
| `SUBSCRIPTION_NOT_FOUND` | Aucun abonnement actif pour ce client |
| `PROGRAMME_NOT_FOUND` | Programme introuvable |
| `CHALLENGE_NOT_FOUND` | Challenge introuvable |
| `PARTICIPANT_NOT_FOUND` | Participant introuvable |
| `SEANCE_NOT_FOUND` | Séance introuvable |
| `EXERCICE_NOT_FOUND` | Exercice introuvable |
| `CANNOT_DELETE_DEFAULT` | Tentative de suppression d'une ressource par défaut |

### Format d'erreur standard

```json
{
  "success": false,
  "error": "CLIENT_NOT_FOUND",
  "message": "Aucun client avec l'identifiant 999."
}
```

---

## 13. Pagination & Filtres

### Paramètres de pagination (routes de liste)

| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| `page` | int | 1 | Numéro de page (base 1) |
| `per_page` | int | 50 | Résultats par page |

### En-têtes de pagination dans la réponse

```json
{
  "success": true,
  "data": [ /* ... */ ],
  "total": 245,
  "page": 1,
  "per_page": 50,
  "total_pages": 5
}
```

### Exemple de requête paginée

```
GET /clients?page=2&per_page=25&statut=actif&search=dupont
```

---

## 14. Plan d'implémentation prioritisé

### 🔴 Phase 1 — P0 : Bloquant (à livrer en premier)

Ces routes débloquent les fonctionnalités principales de l'app. Sans elles, l'app reste sur des mocks.

| Route | Méthode | Description |
|-------|---------|-------------|
| `/users/register` | POST | 🆕 Inscription Firebase |
| `/users/me` | GET | 🆕 Profil connecté |
| `/clients` | GET | 🆕 Liste clients |
| `/clients/{id}` | GET | 🆕 Détail client |
| `/clients` | POST | 🆕 Créer client |
| `/clients/{id}` | PUT | 🆕 Modifier client |
| `/clients/{id}` | DELETE | 🆕 Supprimer client |
| `/clients/{id}/nfc` | PUT | 🆕 Associer NFC |
| `/clients/nfc/{uid}` | GET | 🆕 Lookup NFC |
| `/clients/{id}/abonnement` | POST | 🆕 Créer abonnement |
| `/clients/{id}/abonnement` | PUT | 🆕 Modifier abonnement |

**Modifications sur l'app après Phase 1 :**
- Remplacer `MockClientService` par `ApiClientService` dans `MauiProgram.cs`
- Mapper JSON → `Client` + `Subscription` dans le nouveau service

---

### 🟠 Phase 2 — P1 : Important

| Route | Méthode | Description |
|-------|---------|-------------|
| `/programmes` | GET | ✏️ Enrichir modèle (type, niveau, is_actif, etc.) |
| `/programmes/{id}` | GET | 🆕 Détail programme |
| `/programmes` | POST | ✏️ Body enrichi |
| `/programmes/{id}` | PUT | ✏️ Body enrichi |
| `/clients/{id}/programmes` | GET | 🆕 Programmes assignés |
| `/clients/{id}/programmes` | POST | 🆕 Assigner programme |
| `/clients/{id}/programmes/{id}` | DELETE | 🆕 Désassigner |
| `/challenges` | GET | 🆕 |
| `/challenges/{id}` | GET | 🆕 |
| `/challenges` | POST | 🆕 |
| `/challenges/{id}` | PUT | 🆕 |
| `/challenges/{id}` | DELETE | 🆕 |
| `/challenges/{id}/participants` | GET | 🆕 Classement |
| `/challenges/{id}/participants` | POST | 🆕 Upsert participant |
| `/challenges/{id}/participants/{id}` | DELETE | 🆕 |
| `/acces` | GET | 🆕 Logs NFC |
| `/acces` | POST | 🆕 Enregistrer accès |
| `/acces/today` | GET | 🆕 Accès du jour |

**Modifications sur l'app après Phase 2 :**
- Remplacer `SqliteChallengeService` par `ApiChallengeService`
- Remplacer `MockChallengeService` par `ApiChallengeService`
- Ajouter sync bidirectionnelle NFC : envoyer logs à l'API après réception MQTT
- `ApiProgrammeService.MapToProgramme()` : mapper `type`, `niveau`, `duree_semaines`, `seances_par_semaine`, `is_actif`

---

### 🟡 Phase 3 — P2 : Enrichissement

| Route | Méthode | Description |
|-------|---------|-------------|
| `/seances` | GET | 🆕 Bibliothèque séances |
| `/seances/{id}` | GET | 🆕 |
| `/seances` | POST | 🆕 |
| `/seances/{id}` | DELETE | 🆕 |
| `/exercices` | GET | 🆕 Bibliothèque exercices |
| `/exercices` | POST | 🆕 |
| `/exercices/{id}` | DELETE | 🆕 |
| `/dashboard` | GET | 🆕 Stats globales |
| `/acces/stats` | GET | 🆕 Stats accès NFC |
| `/users/refresh` | POST | 🆕 Refresh token |
| `/users/logout` | POST | 🆕 Révocation token |

**Modifications sur l'app après Phase 3 :**
- Remplacer `SqliteExerciseLibraryService` par `ApiExerciseLibraryService` (avec cache SQLite)
- Remplacer `SqliteSessionLibraryService` par `ApiSessionLibraryService` (avec cache SQLite)
- Remplacer `MockDashboardService` par `ApiDashboardService`

---

## 15. Intégration côté app — mapping JSON ↔ C#

### Services à créer / modifier

#### `ApiClientService.cs` (nouveau — remplace `MockClientService`)

```csharp
// Services/Api/ApiClientService.cs
public class ApiClientService : IClientService
{
    private readonly ApiHttpClient _api;

    public async Task<List<Client>> GetClientsAsync(string? statut = null, string? search = null)
    {
        var path = "/clients";
        var query = new List<string>();
        if (statut != null) query.Add($"statut={statut}");
        if (search != null) query.Add($"search={Uri.EscapeDataString(search)}");
        if (query.Count > 0) path += "?" + string.Join("&", query);

        var resp = await _api.GetAsync<ClientListResponseDto>(path);
        return resp?.Data?.Select(MapToClient).ToList() ?? [];
    }

    private static Client MapToClient(ClientDto dto) => new Client
    {
        Id        = dto.Id,
        FirstName = dto.Prenom,
        LastName  = dto.Nom,
        Email     = dto.Email,
        Status    = dto.Statut,
        NfcUid    = dto.NfcUid,
        Subscription = dto.Abonnement is null ? null : new Subscription
        {
            Type        = dto.Abonnement.Type,
            StartDate   = DateTime.Parse(dto.Abonnement.DateDebut),
            EndDate     = DateTime.Parse(dto.Abonnement.DateFin),
            AutoRenewal = dto.Abonnement.AutoRenouvellement
        }
    };
}
```

#### `ApiChallengeService.cs` (nouveau — remplace `SqliteChallengeService`)

```csharp
// Services/Api/ApiChallengeService.cs
public class ApiChallengeService : IChallengeService
{
    private static ChallengeType MapType(string t) => t switch
    {
        "cardio"      => ChallengeType.Cardio,
        "force"       => ChallengeType.Force,
        "endurance"   => ChallengeType.Endurance,
        "poids"       => ChallengeType.Poids,
        "flexibilite" => ChallengeType.Flexibilite,
        _             => ChallengeType.General
    };

    private static ChallengeStatus MapStatus(string s) => s switch
    {
        "actif"   => ChallengeStatus.Active,
        "termine" => ChallengeStatus.Completed,
        "annule"  => ChallengeStatus.Cancelled,
        _         => ChallengeStatus.Upcoming
    };
}
```

#### Enrichissement de `ApiProgrammeService.cs` (existant)

```csharp
// Remplacer MapToProgramme() par la version complète :
private static Programme MapToProgramme(ProgrammeDtoV2 dto) => new Programme
{
    Id              = dto.IdProgramme,
    Name            = dto.NomProgramme,
    Description     = dto.Description ?? string.Empty,
    Type            = dto.Type switch
    {
        "force"    => ProgrammeType.Strength,
        "cardio"   => ProgrammeType.Cardio,
        "souplesse"=> ProgrammeType.Flexibility,
        _          => ProgrammeType.Mixed
    },
    Level = dto.Niveau switch
    {
        "debutant"      => ProgrammeLevel.Beginner,
        "avance"        => ProgrammeLevel.Advanced,
        _               => ProgrammeLevel.Intermediate
    },
    DurationWeeks   = dto.DureeSemaines,
    SessionsPerWeek = dto.SeancesParSemaine,
    IsActive        = dto.IsActif,
    CreatedAt       = dto.CreatedAt
};
```

### DTOs à créer

```
Services/Api/Dto/
├── ClientDto.cs             (Client + abonnement imbriqué)
├── ClientListResponseDto.cs (wrapper { success, data[], total, page, per_page })
├── ChallengeDto.cs
├── ChallengeListResponseDto.cs
├── ChallengeParticipantDto.cs
├── NfcLogDto.cs
├── NfcLogListResponseDto.cs
├── DashboardDto.cs
├── SeanceDto.cs
├── ExerciceDto.cs
└── ProgrammeDtoV2.cs        (version enrichie de ProgrammeDto)
```

### Configuration MauiProgram.cs — changement de services

```csharp
// Phase 1 : Remplacer les mocks
builder.Services.AddSingleton<IClientService, ApiClientService>();  // était MockClientService

// Phase 2
builder.Services.AddSingleton<IChallengeService, ApiChallengeService>();  // était SqliteChallengeService

// Phase 3
builder.Services.AddSingleton<IDashboardService, ApiDashboardService>(); // était MockDashboardService
```

---

## Récapitulatif — toutes les routes par endpoint

| Route | Méthode | Auth | Statut | Priorité |
|-------|---------|------|--------|----------|
| `/` | GET | ❌ | ✅ Existant | — |
| `/test-db` | GET | ❌ | ✅ Existant | — |
| `/users/login` | POST | ❌ | ✅ Existant | — |
| `/users` | GET | 🔒 | ✅ Existant | — |
| `/users/register` | POST | ❌ | 🆕 À créer | P0 |
| `/users/me` | GET | 🔒 | 🆕 À créer | P0 |
| `/users/refresh` | POST | ❌ | 🆕 À créer | P2 |
| `/users/logout` | POST | 🔒 | 🆕 À créer | P2 |
| `/clients` | GET | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}` | GET | 🔒 | 🆕 À créer | P0 |
| `/clients` | POST | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}` | PUT | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}` | DELETE | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}/nfc` | PUT | 🔒 | 🆕 À créer | P0 |
| `/clients/nfc/{uid}` | GET | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}/abonnement` | POST | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}/abonnement` | PUT | 🔒 | 🆕 À créer | P0 |
| `/clients/{id}/programmes` | GET | 🔒 | 🆕 À créer | P1 |
| `/clients/{id}/programmes` | POST | 🔒 | 🆕 À créer | P1 |
| `/clients/{id}/programmes/{id}` | DELETE | 🔒 | 🆕 À créer | P1 |
| `/programmes` | GET | 🔒 | ✏️ À enrichir | P1 |
| `/programmes/{id}` | GET | 🔒 | 🆕 À créer | P1 |
| `/programmes` | POST | 🔒 | ✏️ À enrichir | P1 |
| `/programmes/{id}` | PUT | 🔒 | ✏️ À enrichir | P1 |
| `/programmes/{id}` | DELETE | 🔒 | ✅ Existant | — |
| `/challenges` | GET | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}` | GET | 🔒 | 🆕 À créer | P1 |
| `/challenges` | POST | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}` | PUT | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}` | DELETE | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}/participants` | GET | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}/participants` | POST | 🔒 | 🆕 À créer | P1 |
| `/challenges/{id}/participants/{id}` | DELETE | 🔒 | 🆕 À créer | P1 |
| `/acces` | GET | 🔒 | 🆕 À créer | P1 |
| `/acces` | POST | 🔒 | 🆕 À créer | P1 |
| `/acces/today` | GET | 🔒 | 🆕 À créer | P1 |
| `/acces/stats` | GET | 🔒 | 🆕 À créer | P2 |
| `/seances` | GET | 🔒 | 🆕 À créer | P2 |
| `/seances/{id}` | GET | 🔒 | 🆕 À créer | P2 |
| `/seances` | POST | 🔒 | 🆕 À créer | P2 |
| `/seances/{id}` | DELETE | 🔒 | 🆕 À créer | P2 |
| `/exercices` | GET | 🔒 | 🆕 À créer | P2 |
| `/exercices` | POST | 🔒 | 🆕 À créer | P2 |
| `/exercices/{id}` | DELETE | 🔒 | 🆕 À créer | P2 |
| `/dashboard` | GET | 🔒 | 🆕 À créer | P2 |

**Total :** 4 existants · 2 à enrichir · **42 à créer**

---

*Document généré le 2026-04-24 — Source of truth pour l'intégration API BurnOut Admin v2.0*
