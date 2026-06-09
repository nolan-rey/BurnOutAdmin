# Routes API pour BurnOutAdmin scolaire

> Doc à destination de l'équipe API PHP Slim.
> Base URL : `http://apiburnout.duckdns.org/`
> Auth : Bearer JWT (sauf `/users/login` + `/users/register`)
> Format réponse : `{ "success": bool, "data": ..., "error"?: string }`
>
> Source : extraction exhaustive des appels HTTP dans `BurnOutAdmin/Services/Api/Api*Service.cs`
> (méthodes `_api.GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync` du wrapper `ApiHttpClient`).

## État des routes

Routes documentées dans ce fichier : **46**
- Déjà appelées par le code (= doivent exister) : **40**
- Nouvelles routes nécessaires (= à ajouter) : **6**

Notes techniques sur le client :
- `ApiHttpClient` ajoute automatiquement le header `Authorization: Bearer <token>` sur **toutes** les requêtes (sauf login/register qui passent par un `HttpClient` brut dans `ApiAuthService`).
- Si l'API répond **401**, le client lève `UnauthorizedAccessException` et l'app force la déconnexion.
- Les bodies sont sérialisés en JSON via `System.Text.Json` (camelCase Ignoré : tous les noms de champs sont en `snake_case` via `[JsonPropertyName]`).

---

## Routes existantes (déjà utilisées)

> Ces endpoints sont appelés par le projet C# actuel.
> S'ils sont déjà implémentés côté API, RAS — sinon, à corriger.

### Authentification (`ApiAuthService.cs`)

#### POST /users/login
- **Auth** : aucune
- **Body** (`LoginRequestDto`) :
  ```json
  { "email": "user@example.com", "password": "..." }
  ```
- **Réponse 200** (`LoginResponseDto`) :
  ```json
  { "success": true, "token": "<jwt>", "error": null }
  ```
- **Référence** : `ApiAuthService.cs:68`
- **Notes** : le token est stocké côté client avec une expiration locale de 1h. L'app ne décode pas le JWT.

#### POST /users/register
- **Auth** : aucune
- **Body** (`RegisterRequestDto`) :
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
  `role` ∈ {`coach`, `admin`}. `specialite` nullable.
- **Réponse 200** (`RegisterResponseDto`) :
  ```json
  { "success": true, "uid": "<firebase-uid>", "error": null }
  ```
- **Référence** : `ApiAuthService.cs:90`
- **Notes** : un 404 ou 405 sur cette route est interprété côté UI comme "inscription pas encore disponible". Après succès, l'app enchaîne automatiquement avec `/users/login`.

---

### Utilisateurs / Clients (`ApiClientService.cs`)

#### GET /users
- **Auth** : Bearer JWT
- **Réponse 200** (`ClientListResponseDto`, même format que `GET /clients` v2) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id": 12,
        "prenom": "Jean",
        "nom": "Dupont",
        "email": "jean@example.com",
        "statut": "actif",
        "nfc_uid": null,
        "created_at": "2026-01-10T08:00:00Z",
        "abonnement": {
          "id_abonnement": 5,
          "type": "mensuel",
          "date_debut": "2026-01-10",
          "date_fin": "2026-02-10",
          "auto_renouvellement": 1
        }
      }
    ]
  }
  ```
- **Référence** : `ApiClientService.cs:40` (+ `ApiProgrammeService.cs:48` pour la résolution d'`id_createur`)
- **Notes** : utilisé pour la liste complète admin/coach/client ; sert aussi à résoudre l'`id` SQL de l'utilisateur connecté en cherchant par email. Le DTO `UserDto` accepte aussi `id_user`, `uid`, `displayName`, `role` si présents.

#### GET /clients/{id}
- **Auth** : Bearer JWT
- **Réponse 200** (`ClientResponseDto`) : `{ "success": true, "data": ClientDto }` (même schéma que les entrées de `GET /users`).
- **Référence** : `ApiClientService.cs:77`
- **Notes** : appel principal pour la fiche client. Fallback automatique sur `GET /users/{id}` si 4xx.

#### GET /users/{id}
- **Auth** : Bearer JWT
- **Réponse 200** (`UserResponseDto`) : `{ "success": true, "data": UserDto }`. Champs Firebase (`uid`, `displayName`) + SQL (`id_user`/`id`, `prenom`, `nom`, `role`, `statut`, `nfc_uid`, `abonnement`).
- **Référence** : `ApiClientService.cs:87`
- **Notes** : fallback utilisé quand `GET /clients/{id}` échoue.

#### POST /clients
- **Auth** : Bearer JWT
- **Body** (`CreateClientDto`) :
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
  `statut` ∈ {`actif`, `inactif`, `expire`, `en_attente`, `suspendu`}.
  `type` ∈ {`annuel`, `mensuel`, `trimestriel`}.
- **Réponse 200** (`CreateClientResponseDto`) :
  ```json
  { "success": true, "id": 42, "error": null }
  ```
  L'app accepte aussi `id_client` en variante.
- **Référence** : `ApiClientService.cs:124`

#### PUT /users/{id} (mise à jour statut seul)
- **Auth** : Bearer JWT
- **Body** (`UpdateStatutDto`) : `{ "statut": "suspendu" }`
- **Réponse 200** (`ApiSuccessDto`) : `{ "success": true }`
- **Référence** : `ApiClientService.cs:149`
- **Notes** : appel "mini" pour basculer le statut depuis la liste clients.

#### PUT /users/{id} (mise à jour client complète)
- **Auth** : Bearer JWT
- **Body** (`UpdateClientDto`) :
  ```json
  {
    "prenom": "...",
    "nom": "...",
    "email": "...",
    "statut": "actif",
    "nfc_uid": null
  }
  ```
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiClientService.cs:179`

#### PUT /clients/{id}/abonnement
- **Auth** : Bearer JWT
- **Body** (`CreateAbonnementDto`) :
  ```json
  {
    "type": "mensuel",
    "date_debut": "2026-01-10",
    "date_fin": "2026-02-10",
    "auto_renouvellement": 1
  }
  ```
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiClientService.cs:199`
- **Notes** : appelé en cascade après `PUT /users/{id}` quand le client a un abonnement. Erreur silencieuse côté C# si endpoint absent.

#### DELETE /clients/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiClientService.cs:243`

---

### Programmes (`ApiProgrammeService.cs`)

#### GET /programmes
- **Auth** : Bearer JWT
- **Réponse 200** (`ProgrammeListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id_programme": 7,
        "nom_programme": "Prise de masse",
        "description": "...",
        "type": "force",
        "niveau": "intermediaire",
        "duree_semaines": 8,
        "seances_par_semaine": 3,
        "is_actif": true,
        "id_client": null,
        "id_createur": 12,
        "date_debut": "2026-01-10",
        "date_fin": "2026-03-10",
        "created_at": "2026-01-10T08:00:00Z"
      }
    ]
  }
  ```
  `type` ∈ {`cardio`, `force`, `souplesse`, `mixte`}. `niveau` ∈ {`debutant`, `intermediaire`, `avance`}. `is_actif` accepté en bool OU int 0/1.
- **Référence** : `ApiProgrammeService.cs:73` (fallback tableau brut `:89`)
- **Notes** : le client tente d'abord le format enveloppé `{success, data}`, puis tombe sur un tableau brut `[...]` en fallback.

#### POST /programmes
- **Auth** : Bearer JWT
- **Body** (`CreateProgrammeDto`) :
  ```json
  {
    "nom_programme": "Prise de masse",
    "description": "...",
    "type": "force",
    "niveau": "intermediaire",
    "duree_semaines": 8,
    "seances_par_semaine": 3,
    "id_client": null,
    "id_createur": 12,
    "date_debut": "2026-01-10",
    "date_fin": "2026-03-10"
  }
  ```
- **Réponse 200** (`CreateProgrammeResponseDto`) : `{ "success": true, "id": 18, "message": null, "error": null }` (accepte aussi `id_programme`).
- **Référence** : `ApiProgrammeService.cs:146`
- **Notes** : `id_createur` est résolu côté client via `GET /users` en cherchant l'email connecté ; obligatoire pour que le POST réussisse.

#### PUT /programmes/{id}
- **Auth** : Bearer JWT
- **Body** (`UpdateProgrammeDto`) :
  ```json
  {
    "nom_programme": "...",
    "description": "...",
    "type": "force",
    "niveau": "intermediaire",
    "is_actif": 1
  }
  ```
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiProgrammeService.cs:180`

#### DELETE /programmes/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiProgrammeService.cs:195`

---

### Séances d'un programme (`ApiProgrammeSeanceService.cs`)

#### GET /programmes/{id}/seances
- **Auth** : Bearer JWT
- **Réponse 200** (`ProgrammeSeanceListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id": 33,
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
  L'app accepte aussi `id_seance` en variante.
- **Référence** : `ApiProgrammeSeanceService.cs:34`

#### POST /programmes/{id}/seances
- **Auth** : Bearer JWT
- **Body** (`CreateProgrammeSeanceDto`) :
  ```json
  {
    "id_seance_builder": 12,
    "nom": "Séance 1 - Pectoraux",
    "description": "...",
    "ordre": 1,
    "exercise_count": 6,
    "category_count": 2,
    "data_json": "{...json sérialisé du template seances_builder...}"
  }
  ```
- **Réponse 200** (`CreateProgrammeSeanceResponseDto`) : `{ "success": true, "id": 33, "error": null }` (accepte `id_seance`).
- **Référence** : `ApiProgrammeSeanceService.cs:78`
- **Notes** : le `data_json` complet est transmis pour que le serveur n'ait pas à relire le template à chaque attachement.

#### PUT /programmes/{id}/seances/{seanceId}
- **Auth** : Bearer JWT
- **Body** (`UpdateProgrammeSeanceDto`) — tous nullable :
  ```json
  { "ordre": 2, "nom": null, "description": null }
  ```
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiProgrammeSeanceService.cs:123`
- **Notes** : appelé principalement pour réordonner les séances (drag & drop).

#### DELETE /programmes/{id}/seances/{seanceId}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiProgrammeSeanceService.cs:142`

---

### Bibliothèque de séances templates (`ApiSessionLibraryService.cs`)

#### GET /seances-builder
- **Auth** : Bearer JWT
- **Réponse 200** (`SeanceBuilderListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id_seance_builder": 12,
        "nom": "Pectoraux + Triceps",
        "description": "...",
        "exercise_count": 6,
        "category_count": 2,
        "data_json": "{...}",
        "created_at": "2026-01-10T08:00:00Z"
      }
    ]
  }
  ```
- **Référence** : `ApiSessionLibraryService.cs:42` (fallback tableau brut `:58`)
- **Notes** : `data_json` peut être absent dans la liste — l'app a un cache local de fallback dans MAUI Preferences.

#### GET /seances-builder/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : `SeanceBuilderDto` BRUT (pas d'enveloppe `{success, data}` ici — le code désérialise directement). Le champ critique est `data_json`.
- **Référence** : `ApiSessionLibraryService.cs:202`
- **Notes** : utilisé pour recharger le `SessionModel` complet à l'édition. Si `data_json` vaut `"{}"` ou est vide, l'app considère l'endpoint comme non implémenté et retourne null.

#### POST /seances-builder
- **Auth** : Bearer JWT
- **Body** (`CreateSeanceBuilderDto`) :
  ```json
  {
    "nom": "Pectoraux + Triceps",
    "description": "...",
    "exercise_count": 6,
    "category_count": 2,
    "data_json": "{...JSON sérialisé du SessionModel...}"
  }
  ```
- **Réponse 200** (`CreateSeanceBuilderResponseDto`) : `{ "success": true, "id_seance_builder": 12, "error": null }`
- **Référence** : `ApiSessionLibraryService.cs:97`

#### PUT /seances-builder/{id}
- **Auth** : Bearer JWT
- **Body** (`UpdateSeanceBuilderDto`) : mêmes champs que `CreateSeanceBuilderDto`.
- **Réponse 200** : `{ "success": true, "id_seance_builder": 12 }` (mêmes contraintes que POST).
- **Référence** : `ApiSessionLibraryService.cs:163`

#### DELETE /seances-builder/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiSessionLibraryService.cs:182`

---

### Assignations programme ↔ client (`ApiProgramAssignmentService.cs`)

#### POST /programme-assignations
- **Auth** : Bearer JWT
- **Body** (`CreateAssignationDto`) :
  ```json
  {
    "id_client": 7,
    "id_programme": 18,
    "date_debut": "2026-01-10",
    "date_fin": "2026-04-10"
  }
  ```
- **Réponse 200** (`CreateAssignationResponseDto`) :
  ```json
  { "success": true, "id_assignation": "550e8400-e29b-41d4-a716-446655440000", "error": null }
  ```
  `id_assignation` est un **UUID string** (CHAR(36) côté SQL).
- **Référence** : `ApiProgramAssignmentService.cs:45`

#### GET /programme-assignations/client/{idClient}
- **Auth** : Bearer JWT
- **Réponse 200** (`AssignationListResponseDto`) :
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
- **Référence** : `ApiProgramAssignmentService.cs:72`

---

### Challenges (`ApiChallengeService.cs`)

#### GET /challenges
- **Auth** : Bearer JWT
- **Réponse 200** (`ChallengeListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id_challenge": 3,
        "nom": "100km en mai",
        "description": "...",
        "type": "cardio",
        "objectif": 100,
        "unite_objectif": "km",
        "statut": "actif",
        "recompense_description": "Badge or",
        "date_debut": "2026-05-01 00:00:00",
        "date_fin": "2026-05-31 23:59:59",
        "id_ligue": null,
        "created_at": "2026-04-25T08:00:00Z",
        "participants_count": 12
      }
    ]
  }
  ```
  `type` ∈ {`cardio`, `force`, `endurance`, `poids`, `souplesse`, `general`}.
  `statut` ∈ {`actif`, `a_venir`, `termine`, `annule`}.
- **Référence** : `ApiChallengeService.cs:46` (fallback tableau brut `:62`)

#### GET /challenges/{id}
- **Auth** : Bearer JWT
- **Réponse 200** (`ChallengeResponseDto`) : `{ "success": true, "data": ChallengeDto }`.
- **Référence** : `ApiChallengeService.cs:82`

#### POST /challenges
- **Auth** : Bearer JWT
- **Body** (`CreateChallengeDto`) :
  ```json
  {
    "nom": "100km en mai",
    "description": "...",
    "type": "cardio",
    "objectif": 100,
    "unite_objectif": "km",
    "statut": "a_venir",
    "recompense_description": "Badge or",
    "date_debut": "2026-05-01 00:00:00",
    "date_fin": "2026-05-31 23:59:59"
  }
  ```
  Dates au format `yyyy-MM-dd HH:mm:ss` (pas ISO 8601).
- **Réponse 200** (`CreateChallengeResponseDto`) : `{ "success": true, "id_challenge": 3, "error": null }`
- **Référence** : `ApiChallengeService.cs:110`

#### PUT /challenges/{id}
- **Auth** : Bearer JWT
- **Body** (`UpdateChallengeDto`) : mêmes champs que `CreateChallengeDto`.
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiChallengeService.cs:142`

#### DELETE /challenges/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiChallengeService.cs:156`

#### GET /challenges/{id}/participants
- **Auth** : Bearer JWT
- **Réponse 200** (`ParticipantListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id_participant": 22,
        "id_challenge": 3,
        "id_client": 7,
        "nom_client": "Jean Dupont",
        "valeur_actuelle": 47.5,
        "joined_at": "2026-05-02T10:00:00Z"
      }
    ]
  }
  ```
- **Référence** : `ApiChallengeService.cs:182`

#### POST /challenges/{id}/participants
- **Auth** : Bearer JWT
- **Body** (`AddParticipantDto`) :
  ```json
  { "id_client": 7, "nom_client": "Jean Dupont" }
  ```
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiChallengeService.cs:206`

#### DELETE /challenge-participants/{participantId}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiChallengeService.cs:224`
- **Notes** : ATTENTION au nommage — la route est sur **`/challenge-participants/...`** (singulier au début, sans le challenge id dans l'URL), pas sur `/challenges/{id}/participants/{pid}`.

#### PUT /challenge-participants/{participantId}/progress
- **Auth** : Bearer JWT
- **Body** (`UpdateProgressDto`) : `{ "valeur_actuelle": 47.5 }`
- **Réponse 200** : `{ "success": true }`
- **Référence** : `ApiChallengeService.cs:239`
- **Notes** : mêmes remarques de nommage que le DELETE.

---

### Dashboard (`ApiDashboardService.cs`)

#### GET /dashboard/stats
- **Auth** : Bearer JWT
- **Réponse 200** (`DashboardResponseDto`) :
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
  `resultat` ∈ {`autorise`, `refuse`, `en_attente`}.
- **Référence** : `ApiDashboardService.cs:41`
- **Notes** : si cet endpoint échoue, l'app reconstruit les stats à partir des autres endpoints (`/users`, `/logs-acces/today`, `/challenges`, `/programmes`) — donc pas critique, mais préférable pour les perfs.

---

### Bibliothèque d'exercices (`ApiExerciseLibraryService.cs`)

#### GET /exercices
- **Auth** : Bearer JWT
- **Réponse 200** (`ExerciceListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id": 1,
        "nom": "Développé couché",
        "description": "...",
        "url_video": null,
        "categorie": "Musculation",
        "groupe_musculaire": "Pectoraux",
        "is_default": 1,
        "tags": "[\"compound\",\"barbell\"]",
        "created_at": "2026-01-01T00:00:00Z"
      }
    ]
  }
  ```
  L'app accepte `id` ou `id_exercice`. `is_default` accepté en bool, int 0/1, ou string "0"/"1"/"true"/"false". `tags` accepté en null, string JSON, ou array JSON.
- **Référence** : `ApiExerciseLibraryService.cs:37` (fallback tableau brut `:53`)

#### POST /exercices
- **Auth** : Bearer JWT
- **Body** (`CreateExerciceDto`) :
  ```json
  {
    "nom": "Mon exo perso",
    "description": null,
    "categorie": "Musculation",
    "groupe_musculaire": "Pectoraux"
  }
  ```
- **Réponse 200** (`CreateExerciceResponseDto`) : `{ "success": true, "id": 28, "error": null }` (accepte aussi `id_exercice`).
- **Référence** : `ApiExerciseLibraryService.cs:90`

#### DELETE /exercices/{id}
- **Auth** : Bearer JWT
- **Réponse 200** : corps vide accepté.
- **Référence** : `ApiExerciseLibraryService.cs:123`

---

### Logs NFC (`ApiNfcService.cs`)

#### GET /logs-acces?limit=200
- **Auth** : Bearer JWT
- **Query** : `limit` (int, défaut 200 côté client)
- **Réponse 200** (`NfcLogListResponseDto`) :
  ```json
  {
    "success": true,
    "data": [
      {
        "id_log": 1234,
        "event_id": "evt-uuid",
        "uid_nfc": "04AABBCCDD",
        "id_client": 7,
        "nom_client": "Jean Dupont",
        "resultat": "autorise",
        "raison": null,
        "porte": "Entrée principale",
        "source": "esp32",
        "timestamp_utc": "2026-06-09T08:32:00Z"
      }
    ],
    "total": 1234,
    "page": 1,
    "limit": 200
  }
  ```
- **Référence** : `ApiNfcService.cs:34` (via `FetchApiLogsAsync`)

#### GET /logs-acces/today
- **Auth** : Bearer JWT
- **Réponse 200** : même schéma que `/logs-acces`, filtré sur la journée courante (timezone serveur).
- **Référence** : `ApiNfcService.cs:37`

---

## Routes nouvelles à ajouter

> Ces 6 endpoints sont attendus par les services qui viennent d'être ajoutés (ClientProfile).
> Ils ne sont **PAS** encore appelés en prod côté école car les services sont en STUB,
> mais dès qu'ils seront implémentés côté API, les fonctionnalités correspondantes s'activeront.

### GET /clients/{id}/history
- **Description** : liste des séances assignées à un client (réalisées + à venir).
- **Auth** : Bearer JWT
- **Réponse 200 attendue** :
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
  `statut` ∈ {`prevue`, `realisee`, `manquee`, `annulee`}.
- **Usage** : alimenter l'onglet "Historique" de la fiche client.
- **Référence C#** : `ApiClientHistoryService.cs:25` (méthode `GetClientSeancesAsync`).

### GET /clients/{id}/performances/{builderId}
- **Description** : performances d'un client pour une séance template précise (toutes les fois où il a réalisé cette séance, avec poids/reps/séries).
- **Auth** : Bearer JWT
- **Réponse 200 attendue** :
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
- **Usage** : graphique de progression sur une séance donnée.
- **Référence C#** : `ApiClientHistoryService.cs:32` (méthode `GetPerformancesForSessionAsync`).

### GET /clients/{id}/session-feedback?date={iso}&assignation={uuid?}
- **Description** : feedback RPE de fin de séance pour un client à une date donnée (optionnellement filtré sur une assignation précise).
- **Auth** : Bearer JWT
- **Query** : `date` (ISO 8601 UTC, obligatoire), `assignation` (UUID, optionnel).
- **Réponse 200 attendue** :
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
- **Usage** : afficher le ressenti du client sous chaque séance dans l'historique.
- **Référence C#** : `ApiClientHistoryService.cs:39` (méthode `GetSessionFeedbackAsync`).

### GET /clients/{id}/stats
- **Description** : points et stats agrégées du client (utilisé pour calculer le rang/totem).
- **Auth** : Bearer JWT
- **Réponse 200 attendue** :
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
  Pour l'instant, seul `points` est consommé côté client — les autres champs sont optionnels.
- **Usage** : déterminer le tier/division du client dans le système totem.
- **Référence C#** : `ApiClientStatsService.cs:24`.

### GET /rank-badges
- **Description** : liste **complète** de tous les badges/rangs/totems disponibles (cacheable côté serveur, ~224 entrées : 7 tiers × 4 divisions × 8 totems).
- **Auth** : Bearer JWT
- **Réponse 200 attendue** :
  ```json
  {
    "success": true,
    "data": [
      {
        "tier": "bronze",
        "division": 3,
        "totem_slug": "wolf",
        "url": "https://cdn.../badges/bronze-3-wolf.png"
      }
    ]
  }
  ```
  `tier` ∈ {`bronze`, `silver`, `gold`, `platinum`, `diamond`, `master`, `legend`} (insensible à la casse côté client).
  `totem_slug` ∈ 8 slugs alignés mobile (wolf, eagle, bear, lion, dragon, phoenix, tiger, shark — à confirmer avec l'app mobile).
- **Usage** : table chargée une seule fois au démarrage (cache mémoire client), évite N+1.
- **Référence C#** : `ApiRankBadgeService.cs:45`.

### PUT /clients/{id}/totem
- **Description** : met à jour le totem (slug ou rang) choisi par/pour un client.
- **Auth** : Bearer JWT
- **Body** :
  ```json
  { "totem_rang": 12 }
  ```
  `totem_rang` est un entier OU `null` (pour réinitialiser).
- **Réponse 200 attendue** :
  ```json
  { "success": true, "data": { "totem_rang": 12 } }
  ```
  L'app n'utilise actuellement que `success`, le `data` est optionnel.
- **Usage** : sauvegarder le choix de totem depuis la fiche client.
- **Référence C#** : `ApiClientService.cs:218` (méthode `UpdateClientTotemAsync`).

---

## Schéma BDD attendu côté MySQL (nouvelles tables/colonnes)

> Tables minimales pour que les 6 nouveaux endpoints fonctionnent.
> Le reste du schéma (users, clients, programmes, seances_builder, exercices, challenges, etc.) est supposé déjà en place.

### Table `rank_badges` (nouvelle)

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

### Colonne `totem_rang` sur `clients` (ajout)

```sql
ALTER TABLE clients
  ADD COLUMN totem_rang INT NULL;
-- Pas de FK stricte vers rank_badges (le client choisit un totem,
-- mais le tier/division est dérivé de ses points → recalculé dynamiquement).
```

### Table `seance_assignations` / réalisations (à confirmer)

Pour `/clients/{id}/history`, il faut une table qui lie :
- un client (`id_client`)
- une séance template (`id_seance_builder` OU `id_seance` d'un programme)
- une date prévue + une date de réalisation (nullable)
- un statut

Exemple minimal :

```sql
CREATE TABLE seance_realisations (
  id_realisation CHAR(36) PRIMARY KEY,                 -- UUID
  id_client INT NOT NULL,
  id_assignation CHAR(36) NULL,                        -- FK vers programme_assignations
  id_seance_builder INT NOT NULL,
  nom_seance VARCHAR(255) NOT NULL,                    -- dénormalisé pour l'historique
  date_prevue DATETIME NULL,
  date_realisation DATETIME NULL,
  statut ENUM('prevue','realisee','manquee','annulee') NOT NULL DEFAULT 'prevue',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_client) REFERENCES clients(id) ON DELETE CASCADE,
  FOREIGN KEY (id_seance_builder) REFERENCES seances_builder(id_seance_builder),
  INDEX idx_client_date (id_client, date_realisation)
);
```

### Table `seance_performances` (pour /performances/{builderId})

```sql
CREATE TABLE seance_performances (
  id_performance INT AUTO_INCREMENT PRIMARY KEY,
  id_realisation CHAR(36) NOT NULL,
  id_exercice INT NOT NULL,
  serie_num INT NOT NULL,
  reps INT NOT NULL,
  poids_kg DECIMAL(6,2) NULL,
  FOREIGN KEY (id_realisation) REFERENCES seance_realisations(id_realisation) ON DELETE CASCADE,
  FOREIGN KEY (id_exercice) REFERENCES exercices(id),
  INDEX idx_realisation (id_realisation)
);
```

### Table `seance_feedback` (pour /session-feedback)

```sql
CREATE TABLE seance_feedback (
  id_feedback INT AUTO_INCREMENT PRIMARY KEY,
  id_realisation CHAR(36) NOT NULL UNIQUE,
  rpe TINYINT NOT NULL,                                 -- 1-10
  commentaire TEXT NULL,
  duree_minutes INT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (id_realisation) REFERENCES seance_realisations(id_realisation) ON DELETE CASCADE
);
```

### Stats client (`/clients/{id}/stats`)

Pas besoin de nouvelle table — `points` peut être :
- soit une colonne dénormalisée `points` sur `clients` (rapide mais à recalculer),
- soit calculé à la volée par agrégation sur `seance_realisations` (statut = 'realisee').

À la discrétion du collègue selon les perfs voulues.

---

## Notes générales

- **Format réponse** : toutes les routes existantes utilisent déjà le format `{success, data, error?}`. Conserver ce contrat pour les nouvelles.
- **Pagination** : pour `rank_badges` (~224 lignes), la pagination n'est **pas** requise — le client cache tout en mémoire au premier appel.
- **Dates en réponse** : préférer ISO 8601 UTC (`2026-06-12T10:00:00Z`). Note : `/challenges` utilise actuellement le format MySQL `yyyy-MM-dd HH:mm:ss` — l'app le parse en `DateTime.TryParse` donc OK, mais ISO serait plus propre.
- **IDs** : `INT AUTO_INCREMENT` pour les entités principales, `CHAR(36)` UUID pour les assignations et réalisations de séances.
- **Codes HTTP** : 401 = token invalide/expiré (déclenche un logout côté client). 404 = traité comme "endpoint pas encore déployé" sur `/users/register`. Tout autre code non-2xx lève une `ApiException` affichée à l'utilisateur.
- **Casse des enums** : les valeurs d'enum (`statut`, `type`, `niveau`, `resultat`, `tier`, etc.) sont attendues en **minuscules avec underscores** côté API. L'app fait le mapping vers les libellés affichés.
- **Robustesse** : certains DTOs côté client acceptent plusieurs noms de champ pour le même concept (ex: `id` OU `id_exercice`, `id` OU `id_client`). Idéalement, fixer un seul nom canonique par champ côté serveur.
