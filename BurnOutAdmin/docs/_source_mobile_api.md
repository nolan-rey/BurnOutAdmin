# API REST — Spécification complète (burnout_application)

> ⚠️ Doc SOURCE conservée à titre de référence — ne pas modifier.
> Le contrat unifié à implémenter par Samy se trouve dans `API-UNIFIED-FOR-SAMY.md`.

Spécification **prête à implémenter** : pour chaque route → rôle, paramètres,
**body de requête** et **body de réponse** avec les noms de champs exacts.

> Les noms de champs JSON ci-dessous sont **contractuels** : l'app Flutter les lit
> tels quels (cf. `lib/models/*.dart`, `lib/services/api_service.dart`). Toute
> implémentation serveur doit les respecter à la lettre.

---

## Conventions générales

- **Base URL** : `http://98.66.235.57` (surchargeable via `.env` → `API_BASE_URL`).
- **Format entrée** : JSON (`Content-Type: application/json`), **sauf**
  `POST /users/login` et `POST /users/register` qui sont
  **form-encoded** (`application/x-www-form-urlencoded`, Slim `getParsedBody`).
- **Format sortie** : toujours JSON.
- **Auth** : JWT Bearer obtenu au login.
  - Routes protégées (🔒) : header `Authorization: Bearer <token>`.
  - Le serveur déduit l'`id_client` courant **depuis le token** (jamais du body).
  - Sur `401`, le client tente un refresh Firebase puis rejoue la requête.
- **Enveloppe de réponse** :
  - Lecture (liste/objet) : `{ "success": true, "data": <...> }`.
  - Login/register : `{ "success": true, "token": "<jwt>", "refreshToken": "<...>" }`.
  - Création : `{ "id_event": 12 }`, `{ "id_post": 30 }`, etc. (clé = `id_<ressource>`).
  - Erreur : `{ "error": "message" }` + code HTTP ≥ 400.
- **Codes HTTP** : `200` OK · `201` créé · `204` vide · `400` invalide ·
  `401` non auth · `403` interdit · `404` introuvable · `409` conflit · `500` serveur.
- **Dates** : `YYYY-MM-DD` (date seule), `YYYY-MM-DDTHH:MM:SS` (timestamp),
  `HH:MM` ou `HH:MM:SS` (heures).
- **Firebase Auth REST** (appelé directement par le client, hors API métier) :
  - `POST https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=<KEY>`
    → `{ idToken, refreshToken, ... }`
  - `POST https://securetoken.googleapis.com/v1/token?key=<KEY>`
    (`grant_type=refresh_token&refresh_token=<...>`) → `{ id_token, ... }`

---
[Contenu complet conservé tel que fourni par l'utilisateur — voir le fichier source dans le projet burnout_application]
