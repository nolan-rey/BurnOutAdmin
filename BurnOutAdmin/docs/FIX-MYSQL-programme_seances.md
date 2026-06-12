# Fix MySQL — Table `programme_seances` manquante

> **Erreur reproductible** :
> ```
> GET /programmes/{id}/seances
> → 500 SERVER_ERROR
> "SQLSTATE[42S02]: Base table or view not found:
>  1146 Table 'samy_burnoutapi.programme_seances' doesn't exist"
> ```

## 🎯 Cause

L'API PHP attend une table MySQL `programme_seances` qui n'existe pas dans
la base `samy_burnoutapi`.

## 🔍 Vérification rapide à faire d'abord côté Samy

```sql
-- Lister toutes les tables qui contiennent "seance"
SHOW TABLES FROM samy_burnoutapi LIKE '%seance%';
```

3 résultats possibles :

| Résultat | Diagnostic | Action |
|---|---|---|
| `seances` | La table existe sous un autre nom | Modifier le code PHP : remplacer `programme_seances` par `seances` dans les requêtes des routes `/programmes/{id}/seances/*` |
| Rien | La table n'a jamais été créée | Exécuter le `CREATE TABLE` ci-dessous |
| `programme_seances` | La table existe mais MySQL la voit pas (cache ?) | `FLUSH TABLES;` puis retester |

---

## 📐 Schéma attendu — `CREATE TABLE programme_seances`

> Compatible avec le DTO `ProgrammeSeanceDto` du projet C# admin
> (cf. `BurnOutAdmin/Services/Api/Dto/ProgrammeSeanceDto.cs`).

```sql
CREATE TABLE programme_seances (
    id_seance         INT AUTO_INCREMENT PRIMARY KEY,
    id_programme      INT NOT NULL,
    id_seance_builder INT NULL,
    nom               VARCHAR(255) NOT NULL,
    description       TEXT NULL,
    ordre             INT NOT NULL DEFAULT 0,
    exercise_count    INT NOT NULL DEFAULT 0,
    category_count    INT NOT NULL DEFAULT 0,
    data_json         JSON NULL,
    created_at        DATETIME DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_programme (id_programme),
    INDEX idx_template  (id_seance_builder),

    CONSTRAINT fk_ps_programme
        FOREIGN KEY (id_programme)
        REFERENCES programmes(id_programme)
        ON DELETE CASCADE,

    CONSTRAINT fk_ps_template
        FOREIGN KEY (id_seance_builder)
        REFERENCES seances_builder(id_seance_builder)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### Notes sur les colonnes

| Colonne | Pourquoi |
|---|---|
| `id_seance` | PK auto-increment, exposée en JSON sous `"id"` ou `"id_seance"` |
| `id_programme` | FK obligatoire vers le programme parent |
| `id_seance_builder` | FK optionnelle vers le template d'origine (séance peut exister sans template ou template supprimé sans casser la séance) |
| `nom` | Nom de la séance attachée |
| `description` | Description libre, peut être vide |
| `ordre` | Position dans le programme (1, 2, 3...) |
| `exercise_count` / `category_count` | Stats dénormalisées (perf : évite de recompter à chaque GET) |
| `data_json` | Snapshot JSON du template au moment de l'attachement (pour figer la séance même si le template évolue ensuite) |
| `created_at` | Timestamp création |

### FK CASCADE et SET NULL

- **CASCADE sur `programmes`** : si on supprime un programme, toutes ses séances attachées disparaissent.
- **SET NULL sur `seances_builder`** : si on supprime un template, les séances déjà attachées aux programmes conservent leur snapshot `data_json` mais perdent le lien vers le template d'origine.

---

## 🔌 Routes API attendues (rappel)

Les 4 routes utilisées par l'app admin sur cette table :

### GET /programmes/{idProgramme}/seances
- Liste les séances d'un programme, triées par `ordre` ascendant
- Réponse : `{"success": true, "data": [<séance>]}`

### POST /programmes/{idProgramme}/seances
- Crée une séance dans le programme (depuis un template)
- Body :
  ```json
  {
    "id_seance_builder": 12,
    "nom": "Séance 1 - Pectoraux",
    "description": "...",
    "ordre": 1,
    "exercise_count": 6,
    "category_count": 2,
    "data_json": "{...snapshot du template...}"
  }
  ```
- Réponse : `{"success": true, "id": 33}` ou `{"success": true, "id_seance": 33}`

### PUT /programmes/{idProgramme}/seances/{seanceId}
- Met à jour partiellement (réorder principalement)
- Body : `{"ordre": 2, "nom": null, "description": null}` (champs nullable)
- Réponse : `{"success": true}`

### DELETE /programmes/{idProgramme}/seances/{seanceId}
- Supprime la séance du programme
- Réponse : corps vide accepté

---

## 🚀 Quick fix — étapes pour Samy

1. Ouvrir phpMyAdmin / un client MySQL sur la base `samy_burnoutapi`
2. Exécuter d'abord la vérification :
   ```sql
   SHOW TABLES LIKE '%seance%';
   ```
3. **Si la table n'existe pas** → exécuter le `CREATE TABLE` ci-dessus
4. **Si la table existe sous un autre nom** → renommer :
   ```sql
   RENAME TABLE seances TO programme_seances;
   ```
   ou modifier le code PHP des routes `/programmes/{id}/seances/*` pour utiliser le nom correct
5. Tester : `curl -H 'Authorization: Bearer <token>' http://apiburnout.duckdns.org/programmes/17/seances`
   → doit retourner `{"success":true,"data":[]}` (liste vide) au lieu d'une 500

---

## 📂 Référence côté projet C# (pour debug futur)

- DTO : `BurnOutAdmin/Services/Api/Dto/ProgrammeSeanceDto.cs`
- Service : `BurnOutAdmin/Services/Api/ApiProgrammeSeanceService.cs`
- Routes utilisées (commentaire en haut du service) :
  - GET `/programmes/{id}/seances`
  - POST `/programmes/{id}/seances`
  - PUT `/programmes/{id}/seances/{seanceId}`
  - DELETE `/programmes/{id}/seances/{seanceId}`
