# 📘 Documentation API — CallOfPhoenix
**Base URL :** `http://98.66.235.57`
**Version :** 1.0
**Stack :** PHP 8.4 · Slim Framework · Firebase Auth · MariaDB 11.4

---

## 🔐 Authentification

L'API utilise **Firebase Authentication**. La majorité des routes sont protégées et nécessitent un token JWT dans chaque requête.

### Obtenir un token

**`POST /users/login`**

Retourne un token JWT à utiliser dans toutes les requêtes protégées.

**Request**
```
POST http://98.66.235.57/users/login
Content-Type: application/json
```

**Body**
```json
{
    "email": "test@test.com",
    "password": "samydz"
}
```

**Réponse succès `200`**
```json
{
    "success": true,
    "token": "eyJhbGciOiJSUzI1NiIs..."
}
```

**Réponse erreur `400`** — champs manquants
```json
{
    "success": false,
    "error": "Email and password required"
}
```

**Réponse erreur `401`** — mauvais identifiants
```json
{
    "success": false,
    "error": "Invalid credentials"
}
```

---

## 🔑 Utiliser le token

Pour toutes les routes protégées (🔒), ajoute ce header à chaque requête :

```
Authorization: Bearer <ton_token_jwt>
```

> ⚠️ Le token Firebase **expire après 1 heure**. Il faudra rappeler `/users/login` pour en obtenir un nouveau.

---

## 📡 Routes

### Statut de l'API

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| GET | `/` | ❌ | Vérifie que l'API tourne |
| GET | `/test-db` | ❌ | Vérifie la connexion BDD |

**`GET /`** — Réponse `200`
```json
{
    "status": "CallOfPhoenix API is running 🔥"
}
```

---

### 👤 Utilisateurs

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| POST | `/users/login` | ❌ | Connexion Firebase, retourne un token JWT |
| GET | `/users` | 🔒 | Liste tous les utilisateurs Firebase |

**`GET /users`** — Réponse `200`
```json
{
    "success": true,
    "users": [
        {
            "uid": "T3Ibaxq1aMcipHNYBBhBgZcfF3l2",
            "email": "test@test.com",
            "displayName": null
        }
    ]
}
```

---

### 📋 Programmes

Toutes les routes `/programmes` nécessitent un token JWT valide.

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| GET | `/programmes` | 🔒 | Liste tous les programmes |
| POST | `/programmes` | 🔒 | Crée un nouveau programme |
| PUT | `/programmes/{id}` | 🔒 | Modifie un programme existant |
| DELETE | `/programmes/{id}` | 🔒 | Supprime un programme |

---

**`GET /programmes`** — Réponse `200`
```json
[
    {
        "id_programme": 1,
        "nom_programme": "Programme Alpha",
        "description": "Description du programme",
        "id_client": 3,
        "id_createur": 1,
        "date_debut": "2026-01-01",
        "date_fin": "2026-06-01"
    }
]
```

---

**`POST /programmes`**

```
POST http://98.66.235.57/programmes
Authorization: Bearer <token>
Content-Type: application/json
```

**Body**
```json
{
    "nom_programme": "Nouveau Programme",
    "description": "Description ici",
    "id_client": 3,
    "id_createur": 1,
    "date_debut": "2026-04-01",
    "date_fin": "2026-12-31"
}
```

**Réponse `200`**
```json
{
    "success": true
}
```

---

**`PUT /programmes/{id}`**

```
PUT http://98.66.235.57/programmes/1
Authorization: Bearer <token>
Content-Type: application/json
```

**Body**
```json
{
    "nom_programme": "Nom modifié",
    "description": "Nouvelle description"
}
```

**Réponse `200`**
```json
{
    "success": true
}
```

---

**`DELETE /programmes/{id}`**

```
DELETE http://98.66.235.57/programmes/1
Authorization: Bearer <token>
```

**Réponse `200`**
```json
{
    "success": true
}
```

---

## ❌ Codes d'erreur

| Code | Signification |
|------|---------------|
| `400` | Champs manquants dans le body |
| `401` | Token absent, invalide ou expiré |
| `405` | Mauvaise méthode HTTP (ex: GET au lieu de POST) |
| `500` | Erreur serveur / BDD |

---

## 🛠️ Exemple complet avec Postman

1. **Login** → `POST /users/login` avec email + password → copie le `token`
2. **Ajouter le token** → Onglet *Authorization* → Type : *Bearer Token* → colle le token
3. **Appeler une route protégée** → ex: `GET /programmes`

---

## 🌐 CORS

L'API accepte les requêtes depuis **n'importe quelle origine** (`*`).
Les méthodes autorisées : `GET, POST, PUT, DELETE, OPTIONS`
Les headers autorisés : `Content-Type, Authorization`

Pas de configuration supplémentaire nécessaire pour les applis mobile ou web.
