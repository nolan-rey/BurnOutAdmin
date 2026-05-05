# 📊 Analyse des écarts — API CallOfPhoenix × BurnOut Admin
**Date :** 2026-04-23  
**Version API actuelle :** 1.0  
**Auteur :** Analyse technique BurnOut Admin

---

## 🎯 Résumé exécutif

L'API actuelle ne couvre qu'une **infime partie** des besoins de l'application admin.  
Sur **8 fonctionnalités majeures**, seules **2 sont partiellement couvertes** (Authentification + Programmes), et avec des **modèles de données incompatibles** pour les Programmes.

| Fonctionnalité | Statut API | Priorité |
|---|---|---|
| 🔐 Authentification | ✅ Partiel (login seul) | P0 |
| 👥 Clients + Abonnements | ❌ Absent | P0 |
| 📋 Programmes | ⚠️ Partiel (modèle incompatible) | P1 |
| 🏆 Challenges + Classement | ❌ Absent | P1 |
| 🏗️ Séances / Program Builder | ❌ Absent | P2 |
| 🏋️ Bibliothèque d'exercices | ❌ Absent | P2 |
| 📡 Logs d'accès NFC | ❌ Absent | P1 |
| 📊 Dashboard / Statistiques | ❌ Absent | P2 |

---

## 🔐 1. Authentification

### Ce qui existe
- `POST /users/login` → token JWT Firebase ✅
- `GET /users` → liste des utilisateurs Firebase ✅ (usage limité)

### Ce qui manque

#### 1.1 Refresh token
Le token Firebase expire après **1 heure**. Sans endpoint de renouvellement, l'utilisateur doit se reconnecter toutes les heures.

**Route à ajouter :**
```
POST /users/refresh
Body: { "refresh_token": "..." }
Réponse: { "success": true, "token": "nouveau_jwt" }
```

#### 1.2 Profil utilisateur connecté
L'app a besoin du nom/rôle de l'utilisateur connecté pour l'afficher dans le shell.

**Route à ajouter :**
```
GET /users/me
Header: Authorization: Bearer <token>
Réponse: {
  "uid": "...",
  "email": "admin@burnout.fr",
  "displayName": "Jean Dupont",
  "role": "admin"
}
```

#### 1.3 Logout (révocation côté serveur)
```
POST /users/logout
Header: Authorization: Bearer <token>
Réponse: { "success": true }
```

---

## 👥 2. Clients + Abonnements ❌ ABSENT — PRIORITÉ MAXIMALE

C'est la fonctionnalité **la plus utilisée** de l'application. Actuellement 100% mockée.

### Modèle de données attendu (Client)

```json
{
  "id": 1,
  "prenom": "Jean",
  "nom": "Dupont",
  "email": "jean.dupont@email.com",
  "statut": "Actif",
  "nfc_uid": "04:A3:5B:12",
  "abonnement": {
    "type": "Annuel",
    "date_debut": "2025-01-01",
    "date_fin": "2025-12-31",
    "auto_renouvellement": true
  },
  "created_at": "2024-12-01T10:00:00Z"
}
```

**Valeurs possibles pour `statut` :** `Actif`, `Expiré`, `En attente`, `Suspendu`  
**Valeurs possibles pour `abonnement.type` :** `Mensuel`, `Trimestriel`, `Annuel`

### Routes à ajouter

```
GET    /clients                    → Liste tous les clients (avec abonnement inclus)
GET    /clients/{id}               → Détail d'un client
POST   /clients                    → Créer un client
PUT    /clients/{id}               → Modifier un client (nom, email, statut)
DELETE /clients/{id}               → Supprimer un client
PUT    /clients/{id}/nfc           → Associer/dissocier une carte NFC
                                     Body: { "nfc_uid": "04:A3:5B:12" }  ou null pour dissocier
POST   /clients/{id}/abonnement    → Créer ou renouveler un abonnement
PUT    /clients/{id}/abonnement    → Modifier un abonnement existant
```

### Filtres suggérés pour GET /clients

```
GET /clients?statut=Actif
GET /clients?expiration_avant=2025-12-31
GET /clients?search=dupont
```

---

## 📋 3. Programmes — Incompatibilité de modèle

### Problème actuel

L'API et l'application ont une **vision différente** de ce qu'est un programme :

| | API actuelle | Application |
|---|---|---|
| **Concept** | Programme assigné à un client spécifique | Template de programme (bibliothèque) |
| `id_client` | Obligatoire (lié à 1 client) | Pas de client dans le modèle template |
| `type` | ❌ Absent | Strength / Cardio / Flexibility / Mixed |
| `level` | ❌ Absent | Beginner / Intermediate / Advanced |
| `sessions_per_week` | ❌ Absent | Nombre de séances/semaine |
| `is_active` | ❌ Absent (déduit de date_fin) | Boolean explicite |
| `created_at` | ❌ Absent | Date de création |

### Modifications à apporter à l'API

#### 3.1 Séparer "template" et "assignation"

**Concept recommandé :**
- `/programmes` → bibliothèque de templates (sans `id_client`)
- `/clients/{id}/programmes` → programmes assignés à un client

#### 3.2 Enrichir le modèle Programme

**Nouveau modèle suggéré pour `GET /programmes` :**
```json
{
  "id_programme": 1,
  "nom_programme": "Programme Force Intermédiaire",
  "description": "Programme 3j/semaine axé force",
  "type": "force",
  "niveau": "intermediaire",
  "duree_semaines": 12,
  "seances_par_semaine": 3,
  "is_actif": true,
  "id_createur": 1,
  "created_at": "2026-01-15T09:00:00Z"
}
```

**Valeurs pour `type` :** `general`, `cardio`, `force`, `endurance`, `souplesse`, `mixte`  
**Valeurs pour `niveau` :** `debutant`, `intermediaire`, `avance`

#### 3.3 Routes d'assignation à ajouter

```
GET  /clients/{id}/programmes          → Programmes assignés au client
POST /clients/{id}/programmes          → Assigner un programme à un client
                                         Body: { "id_programme": 3, "date_debut": "2026-05-01", "date_fin": "2026-08-01" }
DELETE /clients/{id}/programmes/{id}   → Désassigner un programme
```

---

## 🏆 4. Challenges + Classement ❌ ABSENT — PRIORITÉ HAUTE

L'application dispose d'une fonctionnalité complète de gestion des challenges avec classement podium. Tout est actuellement en SQLite local.

### Modèle de données attendu

```json
{
  "id": 1,
  "nom": "Challenge Cardio Avril",
  "description": "Cumulez le plus de kilomètres en course à pied",
  "type": "cardio",
  "date_debut": "2026-04-01",
  "date_fin": "2026-04-30",
  "objectif": 100,
  "unite_objectif": "km",
  "statut": "actif",
  "recompense_description": "1 mois offert",
  "created_at": "2026-03-20T10:00:00Z"
}
```

**Valeurs pour `type` :** `general`, `cardio`, `force`, `endurance`, `poids`, `flexibilite`  
**Valeurs pour `statut` :** `a_venir`, `actif`, `termine`, `annule`

### Routes à ajouter

```
GET    /challenges                             → Liste tous les challenges
GET    /challenges/{id}                        → Détail d'un challenge
POST   /challenges                             → Créer un challenge
PUT    /challenges/{id}                        → Modifier un challenge
DELETE /challenges/{id}                        → Supprimer un challenge (cascade participants)

GET    /challenges/{id}/participants           → Classement des participants (trié par valeur desc)
POST   /challenges/{id}/participants           → Inscrire/mettre à jour un participant
                                                Body: { "id_client": 3, "valeur": 42.5 }
                                                (upsert : crée ou met à jour si déjà inscrit)
DELETE /challenges/{id}/participants/{id}      → Retirer un participant
```

### Modèle participant

```json
{
  "id": 1,
  "id_challenge": 1,
  "id_client": 3,
  "nom_client": "Jean Dupont",
  "valeur_actuelle": 42.5,
  "rang": 1,
  "joined_at": "2026-04-02T08:30:00Z"
}
```

---

## 🏗️ 5. Séances / Program Builder ❌ ABSENT — PRIORITÉ MOYENNE

Le Program Builder permet de créer des séances structurées (catégories → sous-catégories → exercices) et de les sauvegarder. Actuellement tout en SQLite local.

### Structure d'une séance

```json
{
  "id": "uuid",
  "nom": "Push Day",
  "description": "Poussée horizontale et verticale",
  "exercise_count": 8,
  "category_count": 3,
  "data": { /* SessionModel sérialisé complet */ },
  "created_at": "2026-04-10T14:00:00Z"
}
```

### Routes à ajouter

```
GET    /seances                  → Liste des séances sauvegardées
POST   /seances                  → Sauvegarder une séance
                                   Body: { "nom": "...", "description": "...", "data": {...} }
DELETE /seances/{id}             → Supprimer une séance
```

---

## 🏋️ 6. Bibliothèque d'exercices ❌ ABSENT — PRIORITÉ MOYENNE

Plus de 100 exercices sont stockés en SQLite local. Il faudra synchroniser avec l'API pour partager la bibliothèque entre les clients (coach/admin).

### Modèle d'exercice

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

### Routes à ajouter

```
GET    /exercices                     → Liste tous les exercices (filtre ?categorie=Force)
POST   /exercices                     → Ajouter un exercice personnalisé
DELETE /exercices/{id}               → Supprimer un exercice personnalisé
```

---

## 📡 7. Logs d'accès NFC ❌ ABSENT — PRIORITÉ HAUTE

L'app enregistre les accès NFC en SQLite local et les reçoit via MQTT. La synchronisation avec l'API permettrait d'accéder aux logs depuis n'importe quel appareil et d'avoir un historique centralisé.

### Modèle de log d'accès

```json
{
  "id": 1,
  "event_id": "uuid-unique-par-event",
  "uid_nfc": "04:A3:5B:12",
  "id_client": 3,
  "nom_client": "Jean Dupont",
  "resultat": "autorise",
  "raison": null,
  "porte": "Entrée principale",
  "source": "rfid",
  "timestamp": "2026-04-23T18:42:00Z"
}
```

**Valeurs pour `resultat` :** `en_attente`, `autorise`, `refuse`

### Routes à ajouter

```
GET  /acces                           → Liste des logs (filtre ?date=2026-04-23&id_client=3)
POST /acces                           → Enregistrer un accès (appelé par l'orchestrateur)
GET  /acces/today                     → Accès du jour uniquement
GET  /acces/stats                     → Stats : nombre d'accès par jour (pour dashboard)
```

---

## 📊 8. Dashboard / Statistiques ❌ ABSENT — PRIORITÉ BASSE

Le dashboard affiche des métriques agrégées. Actuellement calculées côté client depuis les mocks.

### Route à ajouter

```
GET /dashboard
Header: Authorization: Bearer <token>

Réponse:
{
  "clients_actifs": 45,
  "acces_aujourd_hui": 23,
  "alertes": 2,
  "challenges_actifs": 3,
  "programmes_actifs": 8,
  "abonnements_expirant_bientot": 5,
  "statut_systeme": "online"
}
```

**Valeurs pour `statut_systeme` :** `online`, `maintenance`, `offline`

---

## 🔄 9. Améliorations transversales

### 9.1 Pagination
Pour les listes longues (clients, logs), ajouter la pagination :
```
GET /clients?page=1&per_page=50
Réponse headers: X-Total-Count: 245, X-Page: 1, X-Per-Page: 50
```

### 9.2 Format des dates
Utiliser **ISO 8601** de façon cohérente : `"2026-04-23T18:42:00Z"` (UTC)  
Actuellement : `"2026-01-01"` (date seule sans timezone) — peut causer des décalages.

### 9.3 Réponses de création
Les `POST` qui créent une ressource devraient retourner la ressource créée (avec son `id`) :
```json
// Actuellement : { "success": true }
// Recommandé  : { "success": true, "id": 42 }
```
Sans l'`id`, l'application doit refaire un `GET` pour connaître l'id de la ressource créée.

### 9.4 Messages d'erreur localisés
```json
{
  "success": false,
  "error": "EMAIL_ALREADY_EXISTS",
  "message": "Un client avec cet email existe déjà."
}
```
Utiliser des codes d'erreur constants (`EMAIL_ALREADY_EXISTS`) + message humain pour l'affichage.

### 9.5 Vérification de disponibilité NFC UID
```
GET /clients/nfc/{uid}
→ 200 : { "id_client": 3, "nom": "Jean Dupont" }
→ 404 : { "success": false, "error": "NFC_UID_NOT_FOUND" }
```
Utilisé par le lecteur RFID pour identifier le client à l'entrée.

---

## 📐 10. Récapitulatif des routes à créer

### Routes priorité P0 (bloquantes)
```
POST /users/refresh
GET  /users/me
GET  /clients
GET  /clients/{id}
POST /clients
PUT  /clients/{id}
DELETE /clients/{id}
PUT  /clients/{id}/nfc
POST /clients/{id}/abonnement
PUT  /clients/{id}/abonnement
GET  /clients/nfc/{uid}
```

### Routes priorité P1 (importantes)
```
GET    /challenges
GET    /challenges/{id}
POST   /challenges
PUT    /challenges/{id}
DELETE /challenges/{id}
GET    /challenges/{id}/participants
POST   /challenges/{id}/participants
DELETE /challenges/{id}/participants/{participant_id}
GET    /acces
POST   /acces
GET    /acces/today
```

### Routes priorité P2 (enrichissement)
```
GET  /programmes/{id}            (actuellement manquant)
GET  /clients/{id}/programmes
POST /clients/{id}/programmes
DELETE /clients/{id}/programmes/{programme_id}
GET  /exercices
POST /exercices
DELETE /exercices/{id}
GET  /seances
POST /seances
DELETE /seances/{id}
GET  /dashboard
POST /users/logout
```

---

## 🏗️ État de l'intégration côté App (BurnOut Admin)

| Service | Implémentation actuelle | Cible |
|---|---|---|
| Auth | `ApiAuthService` ✅ | Stable — ajouter refresh |
| HTTP Client | `ApiHttpClient` ✅ | Stable |
| Programmes | `ApiProgrammeService` ✅ | Enrichir quand API évolue |
| Clients | `MockClientService` ⏳ | → `ApiClientService` |
| Challenges | `SqliteChallengeService` ⏳ | → `ApiChallengeService` |
| Logs NFC | `SqliteNfcLogRepository` ⏳ | → Sync bidirectionnelle API+SQLite |
| Dashboard | `MockDashboardService` ⏳ | → `ApiDashboardService` |
| Exercices | `SqliteExerciseLibraryService` ⏳ | → `ApiExerciseService` |
| Séances | `SqliteSessionLibraryService` ⏳ | → `ApiSessionService` |

---

*Document généré le 2026-04-23 — à mettre à jour à chaque évolution de l'API.*
