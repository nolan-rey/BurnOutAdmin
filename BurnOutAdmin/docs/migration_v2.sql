-- ============================================================
--  SCRIPT DE MIGRATION — BurnOut API v1.0 → v2.0
--  Base de données : samy_burnoutapi
--  Serveur : mysql-samy.alwaysdata.net (MariaDB 11.4)
--  Généré le : 2026-04-24
-- ============================================================
--
--  INSTRUCTIONS :
--  1. Faire un dump de sauvegarde AVANT d'exécuter ce script.
--  2. Exécuter dans phpMyAdmin → onglet SQL, ou via CLI :
--       mysql -h mysql-samy.alwaysdata.net -u samy -p samy_burnoutapi < migration_v2.sql
--  3. Vérifier les résultats avec les requêtes de contrôle en bas de fichier.
--
--  CE QUE FAIT CE SCRIPT :
--  - Convertit toutes les tables de MyISAM à InnoDB (FK support)
--  - Crée : clients, abonnements, challenge_participants,
--            programme_assignations, seances_builder
--  - Modifie : challenges, exercices, programmes, logs_acces, seances
--  - Ajoute toutes les clés étrangères
--  - Migre les données existantes sans perte
-- ============================================================

SET SQL_MODE          = "NO_AUTO_VALUE_ON_ZERO";
SET FOREIGN_KEY_CHECKS = 0;
SET time_zone         = "+00:00";
SET NAMES utf8mb4;
START TRANSACTION;


-- ============================================================
--  ÉTAPE 0 — Conversion MyISAM → InnoDB
--  (obligatoire pour les clés étrangères)
-- ============================================================

ALTER TABLE `challenges`          ENGINE = InnoDB;
ALTER TABLE `contenu_seance`      ENGINE = InnoDB;
ALTER TABLE `exercices`           ENGINE = InnoDB;
ALTER TABLE `ligues`              ENGINE = InnoDB;
ALTER TABLE `logs_acces`          ENGINE = InnoDB;
ALTER TABLE `messages`            ENGINE = InnoDB;
ALTER TABLE `performance_client`  ENGINE = InnoDB;
ALTER TABLE `programmes`          ENGINE = InnoDB;
ALTER TABLE `resultats_challenge` ENGINE = InnoDB;
ALTER TABLE `seances`             ENGINE = InnoDB;


-- ============================================================
--  ÉTAPE 1 — Table CLIENTS (nouvelle)
--  Les membres de la salle (≠ utilisateurs Firebase admin)
-- ============================================================

CREATE TABLE IF NOT EXISTS `clients` (
    `id_client`  INT(11)      NOT NULL AUTO_INCREMENT,
    `prenom`     VARCHAR(100) NOT NULL,
    `nom`        VARCHAR(100) NOT NULL,
    `email`      VARCHAR(255) NOT NULL,
    `statut`     ENUM('actif','expire','en_attente','suspendu')
                              NOT NULL DEFAULT 'en_attente',
    `nfc_uid`    VARCHAR(50)  DEFAULT NULL COMMENT 'UID carte NFC/RFID',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                              ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (`id_client`),
    UNIQUE  KEY `uq_clients_email`   (`email`),
    UNIQUE  KEY `uq_clients_nfc_uid` (`nfc_uid`),
    KEY         `idx_clients_statut` (`statut`)

) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Membres de la salle de sport';


-- ============================================================
--  ÉTAPE 2 — Table ABONNEMENTS (nouvelle)
--  Un client peut avoir un abonnement actif à la fois.
-- ============================================================

CREATE TABLE IF NOT EXISTS `abonnements` (
    `id_abonnement`       INT(11)  NOT NULL AUTO_INCREMENT,
    `id_client`           INT(11)  NOT NULL,
    `type`                ENUM('mensuel','trimestriel','annuel')
                                   NOT NULL,
    `date_debut`          DATE     NOT NULL,
    `date_fin`            DATE     NOT NULL,
    `auto_renouvellement` TINYINT(1) NOT NULL DEFAULT 0,
    `created_at`          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`id_abonnement`),
    KEY `idx_abo_id_client` (`id_client`),
    KEY `idx_abo_date_fin`  (`date_fin`),

    CONSTRAINT `fk_abo_client`
        FOREIGN KEY (`id_client`)
        REFERENCES `clients` (`id_client`)
        ON DELETE CASCADE
        ON UPDATE CASCADE

) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Abonnements des membres';


-- ============================================================
--  ÉTAPE 3 — Mise à jour table CHALLENGES
--  - Renomme : titre → nom
--  - Change  : DATE → DATETIME pour date_debut / date_fin
--  - Rend    : id_ligue nullable (challenge sans ligue possible)
--  - Ajoute  : type, objectif, unite_objectif, statut,
--              recompense_description, created_at
-- ============================================================

-- 3.1 Renommer titre → nom et agrandir la taille
ALTER TABLE `challenges`
    CHANGE `titre` `nom` VARCHAR(200) NOT NULL;

-- 3.2 Passer les dates en DATETIME (les dates existantes deviennent YYYY-MM-DD 00:00:00)
ALTER TABLE `challenges`
    MODIFY `date_debut` DATETIME NOT NULL,
    MODIFY `date_fin`   DATETIME NOT NULL;

-- 3.3 Rendre id_ligue nullable (un challenge peut exister sans ligue)
ALTER TABLE `challenges`
    MODIFY `id_ligue` INT(11) DEFAULT NULL;

-- 3.4 Ajouter les colonnes manquantes
ALTER TABLE `challenges`
    ADD COLUMN `type`
        ENUM('general','cardio','force','endurance','poids','flexibilite')
        NOT NULL DEFAULT 'general'
        AFTER `description`,
    ADD COLUMN `objectif`
        INT(11) NOT NULL DEFAULT 0
        AFTER `type`,
    ADD COLUMN `unite_objectif`
        VARCHAR(50) NOT NULL DEFAULT ''
        AFTER `objectif`,
    ADD COLUMN `statut`
        ENUM('a_venir','actif','termine','annule')
        NOT NULL DEFAULT 'a_venir'
        AFTER `unite_objectif`,
    ADD COLUMN `recompense_description`
        TEXT DEFAULT NULL
        AFTER `statut`,
    ADD COLUMN `created_at`
        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        AFTER `recompense_description`;

-- 3.5 Index sur les colonnes fréquemment filtrées
ALTER TABLE `challenges`
    ADD KEY `idx_ch_statut` (`statut`),
    ADD KEY `idx_ch_type`   (`type`),
    ADD KEY `idx_ch_dates`  (`date_debut`, `date_fin`);


-- ============================================================
--  ÉTAPE 4 — Table CHALLENGE_PARTICIPANTS (nouvelle)
--  Classement en temps réel (upsert par id_challenge + id_client)
--  ≠ resultats_challenge (qui conserve les preuves soumises)
-- ============================================================

CREATE TABLE IF NOT EXISTS `challenge_participants` (
    `id_participant`  INT(11)      NOT NULL AUTO_INCREMENT,
    `id_challenge`    INT(11)      NOT NULL,
    `id_client`       INT(11)      NOT NULL,
    `nom_client`      VARCHAR(200) NOT NULL DEFAULT ''
                      COMMENT 'Dénormalisé pour affichage rapide',
    `valeur_actuelle` DOUBLE       NOT NULL DEFAULT 0,
    `joined_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`id_participant`),
    UNIQUE KEY `uq_cp_challenge_client` (`id_challenge`, `id_client`),
    KEY        `idx_cp_classement`      (`id_challenge`, `valeur_actuelle`),

    CONSTRAINT `fk_cp_challenge`
        FOREIGN KEY (`id_challenge`)
        REFERENCES `challenges` (`id_challenge`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `fk_cp_client`
        FOREIGN KEY (`id_client`)
        REFERENCES `clients` (`id_client`)
        ON DELETE CASCADE
        ON UPDATE CASCADE

) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Classement des participants aux challenges (leaderboard)';


-- ============================================================
--  ÉTAPE 5 — Mise à jour table EXERCICES
--  - Renomme : nom_exercice → nom
--  - Ajoute  : categorie, groupe_musculaire, tags (JSON),
--              is_default, created_at
-- ============================================================

-- 5.1 Renommer nom_exercice → nom
ALTER TABLE `exercices`
    CHANGE `nom_exercice` `nom` VARCHAR(200) NOT NULL;

-- 5.2 Ajouter les colonnes de classification
ALTER TABLE `exercices`
    ADD COLUMN `categorie`
        VARCHAR(100) NOT NULL DEFAULT ''
        AFTER `description`,
    ADD COLUMN `groupe_musculaire`
        VARCHAR(100) NOT NULL DEFAULT ''
        AFTER `categorie`,
    ADD COLUMN `tags`
        JSON NOT NULL DEFAULT ('[]')
        AFTER `groupe_musculaire`,
    ADD COLUMN `is_default`
        TINYINT(1) NOT NULL DEFAULT 0
        COMMENT '1 = exercice livré avec l''app, non supprimable'
        AFTER `tags`,
    ADD COLUMN `created_at`
        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        AFTER `is_default`;

-- 5.3 Index pour la recherche et les filtres
ALTER TABLE `exercices`
    ADD KEY `idx_ex_categorie`         (`categorie`),
    ADD KEY `idx_ex_groupe_musculaire` (`groupe_musculaire`),
    ADD KEY `idx_ex_is_default`        (`is_default`);

-- 5.4 Marquer les exercices déjà présents comme exercices par défaut
UPDATE `exercices` SET `is_default` = 1;


-- ============================================================
--  ÉTAPE 6 — Mise à jour table PROGRAMMES
--  - Garde   : id_client (assignation directe legacy), id_createur,
--              date_debut, date_fin (intervalles du programme)
--  - Ajoute  : type, niveau, duree_semaines, seances_par_semaine,
--              is_actif, created_at
-- ============================================================

-- 6.1 Ajouter les colonnes de description du template
ALTER TABLE `programmes`
    ADD COLUMN `type`
        ENUM('force','cardio','souplesse','mixte')
        NOT NULL DEFAULT 'mixte'
        AFTER `description`,
    ADD COLUMN `niveau`
        ENUM('debutant','intermediaire','avance')
        NOT NULL DEFAULT 'intermediaire'
        AFTER `type`,
    ADD COLUMN `duree_semaines`
        INT(11) NOT NULL DEFAULT 8
        AFTER `niveau`,
    ADD COLUMN `seances_par_semaine`
        INT(11) NOT NULL DEFAULT 3
        AFTER `duree_semaines`,
    ADD COLUMN `is_actif`
        TINYINT(1) NOT NULL DEFAULT 1
        AFTER `seances_par_semaine`,
    ADD COLUMN `created_at`
        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        AFTER `is_actif`;

-- 6.2 Calculer is_actif depuis date_fin pour les programmes existants
UPDATE `programmes`
SET `is_actif` = CASE
    WHEN `date_fin` IS NULL THEN 1
    WHEN `date_fin` >= CURDATE() THEN 1
    ELSE 0
END;

-- 6.3 Calculer duree_semaines depuis date_debut/date_fin pour l'existant
UPDATE `programmes`
SET `duree_semaines` = GREATEST(1, CEIL(DATEDIFF(`date_fin`, `date_debut`) / 7.0))
WHERE `date_debut` IS NOT NULL AND `date_fin` IS NOT NULL;

-- 6.4 Index
ALTER TABLE `programmes`
    ADD KEY `idx_prog_type`    (`type`),
    ADD KEY `idx_prog_niveau`  (`niveau`),
    ADD KEY `idx_prog_is_actif`(`is_actif`);


-- ============================================================
--  ÉTAPE 7 — Table PROGRAMME_ASSIGNATIONS (nouvelle)
--  Lie un programme (template) à un client spécifique.
--  Remplace le champ id_client direct sur programmes
--  pour une gestion multi-assignations propre.
-- ============================================================

CREATE TABLE IF NOT EXISTS `programme_assignations` (
    `id_assignation`   CHAR(36)  NOT NULL COMMENT 'UUID généré côté PHP',
    `id_client`        INT(11)   NOT NULL,
    `id_programme`     INT(11)   NOT NULL,
    `date_assignation` DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `date_debut`       DATE      NOT NULL,
    `date_fin`         DATE      DEFAULT NULL,

    PRIMARY KEY (`id_assignation`),
    KEY `idx_pa_id_client`    (`id_client`),
    KEY `idx_pa_id_programme` (`id_programme`),

    CONSTRAINT `fk_pa_client`
        FOREIGN KEY (`id_client`)
        REFERENCES `clients` (`id_client`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `fk_pa_programme`
        FOREIGN KEY (`id_programme`)
        REFERENCES `programmes` (`id_programme`)
        ON DELETE CASCADE
        ON UPDATE CASCADE

) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Assignation d''un programme template à un client';


-- ============================================================
--  ÉTAPE 8 — Mise à jour table LOGS_ACCES
--  Migration délicate : changement de noms de colonnes
--  + migration ENUM AUTORISE/REFUSE → autorise/refuse
--
--  Ancienne structure :
--    nfc_tag_lu, id_user, statut_acces ENUM('AUTORISE','REFUSE'),
--    motif_refus, date_acces
--
--  Nouvelle structure :
--    uid_nfc, id_client, resultat ENUM('en_attente','autorise','refuse'),
--    raison, timestamp_utc + event_id, nom_client, porte, source
-- ============================================================

-- 8.1 Renommer les colonnes existantes
--     (CHANGE conserve les données)
ALTER TABLE `logs_acces`
    CHANGE `nfc_tag_lu`  `uid_nfc`       VARCHAR(50)  DEFAULT NULL,
    CHANGE `id_user`     `id_client`     INT(11)      DEFAULT NULL,
    CHANGE `motif_refus` `raison`        VARCHAR(500) DEFAULT NULL,
    CHANGE `date_acces`  `timestamp_utc` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- 8.2 Migrer l'ENUM en VARCHAR temporairement pour changer les valeurs
--     (ne pas passer directement de ENUM à ENUM avec valeurs différentes)
ALTER TABLE `logs_acces`
    CHANGE `statut_acces` `resultat` VARCHAR(20) NOT NULL DEFAULT 'en_attente';

-- 8.3 Mettre à jour les valeurs uppercase → lowercase
UPDATE `logs_acces` SET `resultat` = 'autorise'  WHERE `resultat` = 'AUTORISE';
UPDATE `logs_acces` SET `resultat` = 'refuse'    WHERE `resultat` = 'REFUSE';
-- Corriger toute valeur résiduelle invalide
UPDATE `logs_acces` SET `resultat` = 'en_attente' WHERE `resultat` NOT IN ('autorise','refuse','en_attente');

-- 8.4 Repasser en ENUM strict
ALTER TABLE `logs_acces`
    MODIFY `resultat` ENUM('en_attente','autorise','refuse') NOT NULL DEFAULT 'en_attente';

-- 8.5 Ajouter les nouvelles colonnes
ALTER TABLE `logs_acces`
    ADD COLUMN `event_id`   CHAR(36)     DEFAULT NULL
        COMMENT 'UUID unique par événement NFC (idempotence MQTT)'
        AFTER `id_log`,
    ADD COLUMN `nom_client` VARCHAR(200) NOT NULL DEFAULT ''
        COMMENT 'Dénormalisé pour affichage rapide'
        AFTER `id_client`,
    ADD COLUMN `porte`      VARCHAR(100) DEFAULT NULL
        COMMENT 'Identifiant du lecteur / point d''accès'
        AFTER `raison`,
    ADD COLUMN `source`     VARCHAR(100) NOT NULL DEFAULT 'rfid'
        COMMENT 'rfid | mqtt | manual'
        AFTER `porte`;

-- 8.6 Index et clé unique sur event_id (idempotence)
ALTER TABLE `logs_acces`
    ADD UNIQUE KEY `uq_log_event_id`      (`event_id`),
    ADD KEY       `idx_log_uid_nfc`       (`uid_nfc`),
    ADD KEY       `idx_log_id_client`     (`id_client`),
    ADD KEY       `idx_log_timestamp_utc` (`timestamp_utc`),
    ADD KEY       `idx_log_resultat`      (`resultat`);


-- ============================================================
--  ÉTAPE 9 — Table SEANCES_BUILDER (nouvelle)
--  Templates de séances créées dans le Program Builder.
--  Distincte de `seances` (sessions planifiées dans un programme).
-- ============================================================

CREATE TABLE IF NOT EXISTS `seances_builder` (
    `id_seance_builder` INT(11)      NOT NULL AUTO_INCREMENT,
    `nom`               VARCHAR(200) NOT NULL,
    `description`       TEXT         DEFAULT NULL,
    `exercise_count`    INT(11)      NOT NULL DEFAULT 0,
    `category_count`    INT(11)      NOT NULL DEFAULT 0,
    `data_json`         JSON         NOT NULL
        COMMENT 'SessionModel C# sérialisé (catégories → sous-catégories → exercices)',
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`id_seance_builder`),
    KEY `idx_sb_nom` (`nom`)

) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Templates de séances (Program Builder)';


-- ============================================================
--  ÉTAPE 10 — Mise à jour table SEANCES
--  - Ajoute : created_at
--  - Ajoute : FK vers programmes
-- ============================================================

ALTER TABLE `seances`
    ADD COLUMN `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        AFTER `date_realisation`;

ALTER TABLE `seances`
    ADD CONSTRAINT `fk_seances_programme`
        FOREIGN KEY (`id_programme`)
        REFERENCES `programmes` (`id_programme`)
        ON DELETE CASCADE
        ON UPDATE CASCADE;


-- ============================================================
--  ÉTAPE 11 — Clés étrangères sur CONTENU_SEANCE
-- ============================================================

ALTER TABLE `contenu_seance`
    ADD CONSTRAINT `fk_cs_seance`
        FOREIGN KEY (`id_seance`)
        REFERENCES `seances` (`id_seance`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    ADD CONSTRAINT `fk_cs_exercice`
        FOREIGN KEY (`id_exercice`)
        REFERENCES `exercices` (`id_exercice`)
        ON DELETE CASCADE
        ON UPDATE CASCADE;


-- ============================================================
--  ÉTAPE 12 — Clés étrangères sur PERFORMANCE_CLIENT
-- ============================================================

ALTER TABLE `performance_client`
    ADD CONSTRAINT `fk_pc_contenu`
        FOREIGN KEY (`id_contenu`)
        REFERENCES `contenu_seance` (`id_contenu`)
        ON DELETE CASCADE
        ON UPDATE CASCADE;


-- ============================================================
--  ÉTAPE 13 — Clés étrangères sur RESULTATS_CHALLENGE
--  (table conservée telle quelle pour les preuves soumises)
-- ============================================================

ALTER TABLE `resultats_challenge`
    ADD CONSTRAINT `fk_rc_challenge`
        FOREIGN KEY (`id_challenge`)
        REFERENCES `challenges` (`id_challenge`)
        ON DELETE CASCADE
        ON UPDATE CASCADE;

--  Note : resultats_challenge.id_user est un INT référençant un utilisateur
--  Firebase. Firebase gère les UIDs comme des VARCHAR(128), mais cette colonne
--  reste INT pour compatibilité avec l'existant.
--  À adapter si un tableau users local est ajouté ultérieurement.


-- ============================================================
--  ÉTAPE 14 — Clé étrangère CHALLENGES → LIGUES (optionnelle)
-- ============================================================

ALTER TABLE `challenges`
    ADD CONSTRAINT `fk_ch_ligue`
        FOREIGN KEY (`id_ligue`)
        REFERENCES `ligues` (`id_ligue`)
        ON DELETE SET NULL
        ON UPDATE CASCADE;


-- ============================================================
--  ÉTAPE 15 — FK sur LOGS_ACCES → CLIENTS
--  (après migration des colonnes en étape 8)
-- ============================================================

ALTER TABLE `logs_acces`
    ADD CONSTRAINT `fk_log_client`
        FOREIGN KEY (`id_client`)
        REFERENCES `clients` (`id_client`)
        ON DELETE SET NULL
        ON UPDATE CASCADE;


-- ============================================================
--  ÉTAPE 16 — Données de référence
--  Quelques exercices par défaut pour initialiser la bibliothèque
--  (is_default = 1 → non supprimables via l'API)
-- ============================================================

INSERT IGNORE INTO `exercices`
    (`nom`, `description`, `url_video`, `categorie`, `groupe_musculaire`, `tags`, `is_default`)
VALUES
    -- Force / Membres inférieurs
    ('Squat',               'Squat classique, barre sur le dos',             NULL, 'Force',    'Quadriceps',  '["jambes","compound","barre"]',         1),
    ('Deadlift',            'Soulevé de terre',                              NULL, 'Force',    'Ischio-jambiers','["dos","jambes","compound","barre"]', 1),
    ('Leg Press',           'Presse à cuisses',                              NULL, 'Force',    'Quadriceps',  '["jambes","machine"]',                  1),
    ('Fente avant',         'Fentes avec haltères ou barre',                 NULL, 'Force',    'Quadriceps',  '["jambes","unilatéral"]',               1),
    ('Leg Curl',            'Curl des ischio-jambiers couché',               NULL, 'Force',    'Ischio-jambiers','["jambes","machine","isolation"]',    1),
    -- Force / Membres supérieurs — poussée
    ('Développé couché',    'Bench press, barre ou haltères',                NULL, 'Force',    'Pectoraux',   '["poitrine","compound","barre"]',        1),
    ('Développé incliné',   'Incline bench press',                           NULL, 'Force',    'Pectoraux',   '["poitrine","compound"]',               1),
    ('Développé militaire', 'Overhead press debout ou assis',                NULL, 'Force',    'Épaules',     '["épaules","compound","barre"]',         1),
    ('Dips',                'Dips aux barres parallèles',                    NULL, 'Force',    'Triceps',     '["poitrine","triceps","poids du corps"]',1),
    ('Pompes',              'Push-ups au sol',                               NULL, 'Force',    'Pectoraux',   '["poitrine","poids du corps"]',          1),
    -- Force / Membres supérieurs — tirage
    ('Tractions',           'Pull-ups pronation',                            NULL, 'Force',    'Dos',         '["dos","biceps","poids du corps"]',      1),
    ('Rowing barre',        'Barbell row penché',                            NULL, 'Force',    'Dos',         '["dos","compound","barre"]',             1),
    ('Tirage poulie haute', 'Lat pulldown machine',                          NULL, 'Force',    'Dos',         '["dos","machine"]',                     1),
    ('Curl biceps',         'Biceps curl haltères ou barre',                 NULL, 'Force',    'Biceps',      '["bras","isolation"]',                  1),
    ('Extension triceps',   'Triceps pushdown à la poulie',                  NULL, 'Force',    'Triceps',     '["bras","isolation","machine"]',         1),
    -- Cardio
    ('Course à pied',       'Running sur tapis ou extérieur',                NULL, 'Cardio',   'Cardio',      '["cardio","endurance"]',                1),
    ('Vélo stationnaire',   'Cycling sur vélo d''appartement',               NULL, 'Cardio',   'Cardio',      '["cardio","endurance","jambes"]',        1),
    ('Rameur',              'Rowing machine',                                NULL, 'Cardio',   'Cardio',      '["cardio","full body"]',                1),
    ('Burpees',             'Exercice cardio full body',                     NULL, 'Cardio',   'Cardio',      '["cardio","poids du corps","hiit"]',     1),
    ('Corde à sauter',      'Jump rope',                                     NULL, 'Cardio',   'Cardio',      '["cardio","coordination"]',             1),
    -- Gainage / Core
    ('Planche',             'Plank statique',                                NULL, 'Gainage',  'Abdominaux',  '["core","gainage","isométrique"]',       1),
    ('Crunch',              'Crunch abdominaux au sol',                      NULL, 'Gainage',  'Abdominaux',  '["core","isolation"]',                  1),
    ('Relevé de jambes',    'Hanging leg raise',                             NULL, 'Gainage',  'Abdominaux',  '["core","suspension"]',                 1),
    ('Russian twist',       'Rotation du tronc avec médecine ball',          NULL, 'Gainage',  'Obliques',    '["core","rotation"]',                   1),
    -- Souplesse / Mobilité
    ('Étirement quadriceps','Étirement debout du quadriceps',                NULL, 'Souplesse','Quadriceps',  '["étirement","mobilité"]',              1),
    ('Fentes de souplesse', 'Hip flexor stretch en fente',                   NULL, 'Souplesse','Hip flexors', '["étirement","mobilité","hanche"]',     1),
    ('Rotation épaules',    'Rotation des épaules pour échauffement',        NULL, 'Souplesse','Épaules',     '["mobilité","échauffement"]',           1);


-- ============================================================
--  FIN DE MIGRATION
-- ============================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;


-- ============================================================
--  REQUÊTES DE VÉRIFICATION
--  (à exécuter manuellement après la migration pour contrôler)
-- ============================================================

/*

-- 1. Vérifier les tables et leurs moteurs
SELECT TABLE_NAME, ENGINE, TABLE_ROWS
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'samy_burnoutapi'
ORDER BY TABLE_NAME;

-- 2. Vérifier toutes les clés étrangères
SELECT
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'samy_burnoutapi'
  AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

-- 3. Vérifier les colonnes de chaque table modifiée
DESCRIBE clients;
DESCRIBE abonnements;
DESCRIBE challenges;
DESCRIBE challenge_participants;
DESCRIBE exercices;
DESCRIBE programmes;
DESCRIBE programme_assignations;
DESCRIBE logs_acces;
DESCRIBE seances_builder;
DESCRIBE seances;
DESCRIBE contenu_seance;

-- 4. Vérifier que les exercices par défaut ont bien été insérés
SELECT COUNT(*) AS total_exercices, SUM(is_default) AS exercices_defaut FROM exercices;

-- 5. Vérifier la migration de l'ENUM logs_acces
SELECT DISTINCT resultat FROM logs_acces;
-- Doit retourner uniquement : en_attente | autorise | refuse

-- 6. Vérifier is_actif sur les programmes existants
SELECT id_programme, nom_programme, date_fin, is_actif FROM programmes;

*/


-- ============================================================
--  RÉSUMÉ DES CHANGEMENTS
-- ============================================================
/*

  NOUVELLES TABLES (5)
  ─────────────────────────────────────────────────────────────
  clients                 Membres de la salle (email, statut, nfc_uid)
  abonnements             Abonnements des membres (mensuel/trimestriel/annuel)
  challenge_participants  Classement leaderboard des challenges
  programme_assignations  Lien programme template ↔ client
  seances_builder         Templates Program Builder (data_json)

  TABLES MODIFIÉES (6)
  ─────────────────────────────────────────────────────────────
  challenges     titre→nom, dates→DATETIME, id_ligue nullable,
                 +type +objectif +unite_objectif +statut
                 +recompense_description +created_at
  exercices      nom_exercice→nom,
                 +categorie +groupe_musculaire +tags +is_default +created_at
  programmes     +type +niveau +duree_semaines +seances_par_semaine
                 +is_actif +created_at
  logs_acces     nfc_tag_lu→uid_nfc, id_user→id_client,
                 statut_acces→resultat (AUTORISE/REFUSE→autorise/refuse),
                 motif_refus→raison, date_acces→timestamp_utc,
                 +event_id +nom_client +porte +source
  seances        +created_at, FK→programmes
  contenu_seance FK→seances, FK→exercices

  TABLES CONSERVÉES SANS MODIFICATION (4)
  ─────────────────────────────────────────────────────────────
  ligues              Inchangée (reliée aux challenges)
  messages            Inchangée (adapter si messagerie Firebase)
  performance_client  Inchangée (historique perfs + FK ajoutée)
  resultats_challenge Inchangée (preuves challenges + FK ajoutée)

  CLÉS ÉTRANGÈRES AJOUTÉES (10)
  ─────────────────────────────────────────────────────────────
  abonnements.id_client          → clients.id_client (CASCADE)
  challenge_participants.id_challenge → challenges.id_challenge (CASCADE)
  challenge_participants.id_client    → clients.id_client (CASCADE)
  programme_assignations.id_client    → clients.id_client (CASCADE)
  programme_assignations.id_programme → programmes.id_programme (CASCADE)
  logs_acces.id_client           → clients.id_client (SET NULL)
  seances.id_programme           → programmes.id_programme (CASCADE)
  contenu_seance.id_seance       → seances.id_seance (CASCADE)
  contenu_seance.id_exercice     → exercices.id_exercice (CASCADE)
  performance_client.id_contenu  → contenu_seance.id_contenu (CASCADE)
  resultats_challenge.id_challenge → challenges.id_challenge (CASCADE)
  challenges.id_ligue            → ligues.id_ligue (SET NULL)

*/
