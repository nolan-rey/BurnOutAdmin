-- ============================================================
--  SEED DATA — BurnOut API v2.0
--  Base : samy_burnoutapi (état du 2026-04-27)
--  Compatible : MariaDB 11.4 / phpMyAdmin
-- ============================================================
--
--  PRÉ-REQUIS : la BDD contient déjà
--    - exercices  : IDs 1-27  (is_default = 1)
--    - programmes : ID 1      (Programme Prise de Masse)
--
--  CE SCRIPT :
--    - Remet les AUTO_INCREMENT à 1 sur les tables vides
--    - Utilise uniquement INSERT ... VALUES avec IDs explicites
--    - Aucune sous-requête / aucune syntaxe avancée
--
--  TABLES REMPLIES :
--    ligues (3) · clients (15) · abonnements (12)
--    challenges (5) · challenge_participants (18)
--    resultats_challenge (5) · programmes (+3 = 4 total)
--    programme_assignations (10) · seances (8)
--    contenu_seance (22) · performance_client (10)
--    logs_acces (50) · seances_builder (3) · messages (5)
-- ============================================================

SET FOREIGN_KEY_CHECKS = 0;
SET NAMES utf8mb4;
START TRANSACTION;

-- ============================================================
--  RESET AUTO_INCREMENT (tables vides)
-- ============================================================

ALTER TABLE `ligues`                 AUTO_INCREMENT = 1;
ALTER TABLE `clients`                AUTO_INCREMENT = 1;
ALTER TABLE `abonnements`            AUTO_INCREMENT = 1;
ALTER TABLE `challenges`             AUTO_INCREMENT = 1;
ALTER TABLE `challenge_participants` AUTO_INCREMENT = 1;
ALTER TABLE `resultats_challenge`    AUTO_INCREMENT = 1;
ALTER TABLE `seances`                AUTO_INCREMENT = 1;
ALTER TABLE `contenu_seance`         AUTO_INCREMENT = 1;
ALTER TABLE `performance_client`     AUTO_INCREMENT = 1;
ALTER TABLE `logs_acces`             AUTO_INCREMENT = 1;
ALTER TABLE `messages`               AUTO_INCREMENT = 1;
ALTER TABLE `seances_builder`        AUTO_INCREMENT = 1;
-- programmes : AUTO_INCREMENT reste à 2 (ID 1 existant)


-- ============================================================
--  1. LIGUES
-- ============================================================

INSERT INTO `ligues` (`id_ligue`, `nom_ligue`, `description`) VALUES
(1, 'Ligue Alpha',    'Groupe competitif — membres confirmes et avances'),
(2, 'Ligue Beta',     'Groupe intermediaire — progression reguliere'),
(3, 'Ligue Debutants','Groupe accueil pour les nouveaux membres');


-- ============================================================
--  2. CLIENTS (15 membres)
--
--  ID  Nom                 Statut      NFC
--   1  Jean Dupont         actif       04:1A:2B:3C
--   2  Marie Martin        actif       04:2B:3C:4D
--   3  Thomas Leroy        actif       04:3C:4D:5E
--   4  Emma Dubois         actif       04:4D:5E:6F
--   5  Lucas Moreau        actif       04:5E:6F:7A
--   6  Pierre Bernard      actif       04:6F:7A:8B
--   7  Sarah Moulin        actif       04:7A:8B:9C
--   8  Sophie Petit        actif       NULL
--   9  Camille Roux        actif       NULL
--  10  Alexandre Bonnet    actif       NULL
--  11  Antoine Laurent     expire      04:8B:9C:AD
--  12  Julie Simon         expire      NULL
--  13  Nicolas Fontaine    en_attente  NULL
--  14  Chloe Garnier       en_attente  NULL
--  15  Maxime Girard       suspendu    04:9C:AD:BE
-- ============================================================

INSERT INTO `clients` (`id_client`, `prenom`, `nom`, `email`, `statut`, `nfc_uid`, `created_at`) VALUES
( 1, 'Jean',      'Dupont',   'jean.dupont@gmail.com',         'actif',      '04:1A:2B:3C', DATE_SUB(NOW(), INTERVAL 14 MONTH)),
( 2, 'Marie',     'Martin',   'marie.martin@outlook.com',      'actif',      '04:2B:3C:4D', DATE_SUB(NOW(), INTERVAL 11 MONTH)),
( 3, 'Thomas',    'Leroy',    'thomas.leroy@gmail.com',        'actif',      '04:3C:4D:5E', DATE_SUB(NOW(), INTERVAL 8  MONTH)),
( 4, 'Emma',      'Dubois',   'emma.dubois@gmail.com',         'actif',      '04:4D:5E:6F', DATE_SUB(NOW(), INTERVAL 6  MONTH)),
( 5, 'Lucas',     'Moreau',   'lucas.moreau@yahoo.fr',         'actif',      '04:5E:6F:7A', DATE_SUB(NOW(), INTERVAL 10 MONTH)),
( 6, 'Pierre',    'Bernard',  'pierre.bernard@hotmail.com',    'actif',      '04:6F:7A:8B', DATE_SUB(NOW(), INTERVAL 5  MONTH)),
( 7, 'Sarah',     'Moulin',   'sarah.moulin@gmail.com',        'actif',      '04:7A:8B:9C', DATE_SUB(NOW(), INTERVAL 3  MONTH)),
( 8, 'Sophie',    'Petit',    'sophie.petit@gmail.com',        'actif',      NULL,          DATE_SUB(NOW(), INTERVAL 7  MONTH)),
( 9, 'Camille',   'Roux',     'camille.roux@orange.fr',        'actif',      NULL,          DATE_SUB(NOW(), INTERVAL 2  MONTH)),
(10, 'Alexandre', 'Bonnet',   'alexandre.bonnet@gmail.com',    'actif',      NULL,          DATE_SUB(NOW(), INTERVAL 1  MONTH)),
(11, 'Antoine',   'Laurent',  'antoine.laurent@gmail.com',     'expire',     '04:8B:9C:AD', DATE_SUB(NOW(), INTERVAL 18 MONTH)),
(12, 'Julie',     'Simon',    'julie.simon@yahoo.fr',          'expire',     NULL,          DATE_SUB(NOW(), INTERVAL 15 MONTH)),
(13, 'Nicolas',   'Fontaine', 'nicolas.fontaine@gmail.com',    'en_attente', NULL,          DATE_SUB(NOW(), INTERVAL 5  DAY)),
(14, 'Chloe',     'Garnier',  'chloe.garnier@hotmail.com',     'en_attente', NULL,          DATE_SUB(NOW(), INTERVAL 2  DAY)),
(15, 'Maxime',    'Girard',   'maxime.girard@gmail.com',       'suspendu',   '04:9C:AD:BE', DATE_SUB(NOW(), INTERVAL 20 MONTH));


-- ============================================================
--  3. ABONNEMENTS
--
--  ID  Client  Type          Debut               Fin                   Renouvellement
--   1   1 (Jean)    annuel    -2 mois             +10 mois              oui
--   2   2 (Marie)   mensuel   -10 jours           +20 jours             oui
--   3   3 (Thomas)  trimestriel -1 mois           +2 mois               oui
--   4   4 (Emma)    annuel    -3 mois             +9 mois               oui
--   5   5 (Lucas)   mensuel   -15 jours           +15 jours             oui
--   6   6 (Pierre)  trimestriel -2 mois           +1 mois               non
--   7   7 (Sarah)   annuel    -1 mois             +11 mois              non
--   8   8 (Sophie)  trimestriel -6 semaines       +6 semaines           oui
--   9   9 (Camille) mensuel   -5 jours            +25 jours             non
--  10  10 (Alexandre) mensuel -25 jours           +5 jours  ← ALERTE   non
--  11  11 (Antoine) annuel    -14 mois            -2 mois   ← EXPIRE
--  12  12 (Julie)   mensuel   -4 mois             -3 mois   ← EXPIRE
-- ============================================================

INSERT INTO `abonnements` (`id_abonnement`, `id_client`, `type`, `date_debut`, `date_fin`, `auto_renouvellement`) VALUES
( 1,  1, 'annuel',       DATE_SUB(CURDATE(), INTERVAL 2  MONTH), DATE_ADD(CURDATE(), INTERVAL 10 MONTH), 1),
( 2,  2, 'mensuel',      DATE_SUB(CURDATE(), INTERVAL 10 DAY),   DATE_ADD(CURDATE(), INTERVAL 20 DAY),   1),
( 3,  3, 'trimestriel',  DATE_SUB(CURDATE(), INTERVAL 1  MONTH), DATE_ADD(CURDATE(), INTERVAL 2  MONTH), 1),
( 4,  4, 'annuel',       DATE_SUB(CURDATE(), INTERVAL 3  MONTH), DATE_ADD(CURDATE(), INTERVAL 9  MONTH), 1),
( 5,  5, 'mensuel',      DATE_SUB(CURDATE(), INTERVAL 15 DAY),   DATE_ADD(CURDATE(), INTERVAL 15 DAY),   1),
( 6,  6, 'trimestriel',  DATE_SUB(CURDATE(), INTERVAL 2  MONTH), DATE_ADD(CURDATE(), INTERVAL 1  MONTH), 0),
( 7,  7, 'annuel',       DATE_SUB(CURDATE(), INTERVAL 1  MONTH), DATE_ADD(CURDATE(), INTERVAL 11 MONTH), 0),
( 8,  8, 'trimestriel',  DATE_SUB(CURDATE(), INTERVAL 6  WEEK),  DATE_ADD(CURDATE(), INTERVAL 6  WEEK),  1),
( 9,  9, 'mensuel',      DATE_SUB(CURDATE(), INTERVAL 5  DAY),   DATE_ADD(CURDATE(), INTERVAL 25 DAY),   0),
(10, 10, 'mensuel',      DATE_SUB(CURDATE(), INTERVAL 25 DAY),   DATE_ADD(CURDATE(), INTERVAL 5  DAY),   0),
(11, 11, 'annuel',       DATE_SUB(CURDATE(), INTERVAL 14 MONTH), DATE_SUB(CURDATE(), INTERVAL 2  MONTH), 0),
(12, 12, 'mensuel',      DATE_SUB(CURDATE(), INTERVAL 4  MONTH), DATE_SUB(CURDATE(), INTERVAL 3  MONTH), 0);


-- ============================================================
--  4. CHALLENGES (5)
--
--  ID  Nom                           Type        Statut    Ligue
--   1  Challenge Cardio Avril        cardio      actif       1
--   2  Maximum Squat Mai             force       a_venir     2
--   3  Perte de poids Trimestre 1    poids       termine     2
--   4  30 jours de Planche           endurance   actif     NULL
--   5  Defi Burpees Mars             cardio      annule    NULL
-- ============================================================

INSERT INTO `challenges`
    (`id_challenge`, `nom`, `description`, `type`, `objectif`, `unite_objectif`,
     `statut`, `recompense_description`, `date_debut`, `date_fin`, `id_ligue`, `created_at`)
VALUES
(1, 'Challenge Cardio Avril',
 'Cumulez le plus de kilometres en course a pied ou velo sur le mois d avril.',
 'cardio', 100, 'km', 'actif', '1 mois d abonnement offert',
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-04-01'), '%Y-%m-%d 00:00:00'),
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-04-30'), '%Y-%m-%d 23:59:59'),
 1, DATE_SUB(NOW(), INTERVAL 35 DAY)),

(2, 'Maximum Squat Mai',
 'Atteignez votre nouveau record personnel au squat. 1 tentative maximum.',
 'force', 0, 'kg', 'a_venir', 'T-shirt exclusif Call of Phoenix + 1 seance coaching',
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-05-01'), '%Y-%m-%d 00:00:00'),
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-05-31'), '%Y-%m-%d 23:59:59'),
 2, DATE_SUB(NOW(), INTERVAL 10 DAY)),

(3, 'Perte de poids Trimestre 1',
 'Perdez le plus de poids sur 3 mois. Pesee hebdomadaire obligatoire.',
 'poids', 10, 'kg', 'termine', 'Analyse corporelle offerte + programme personnalise',
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-01-01'), '%Y-%m-%d 00:00:00'),
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-03-31'), '%Y-%m-%d 23:59:59'),
 2, DATE_SUB(NOW(), INTERVAL 120 DAY)),

(4, '30 jours de Planche',
 'Faites de la planche tous les jours pendant 30 jours. Progression en secondes.',
 'endurance', 300, 'secondes', 'actif', 'Waterbottle personnalise',
 DATE_SUB(NOW(), INTERVAL 10 DAY),
 DATE_ADD(NOW(), INTERVAL 20 DAY),
 NULL, DATE_SUB(NOW(), INTERVAL 15 DAY)),

(5, 'Defi Burpees Mars',
 'Challenge annule suite a maintenance de la salle.',
 'cardio', 500, 'repetitions', 'annule', 'Aucune (challenge annule)',
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-03-01'), '%Y-%m-%d 00:00:00'),
 DATE_FORMAT(CONCAT(YEAR(NOW()), '-03-31'), '%Y-%m-%d 23:59:59'),
 NULL, DATE_SUB(NOW(), INTERVAL 60 DAY));


-- ============================================================
--  5. CHALLENGE_PARTICIPANTS (classement leaderboard)
--
--  Challenge 1 — Cardio Avril (7 participants)
--  Challenge 3 — Perte poids T1, termine (6 participants)
--  Challenge 4 — 30j Planche (5 participants)
-- ============================================================

-- Challenge 1 : Cardio Avril
INSERT INTO `challenge_participants`
    (`id_participant`, `id_challenge`, `id_client`, `nom_client`, `valeur_actuelle`, `joined_at`)
VALUES
( 1, 1,  5, 'Moreau Lucas',   91.2, DATE_SUB(NOW(), INTERVAL 1  DAY)),
( 2, 1,  1, 'Dupont Jean',    87.5, DATE_SUB(NOW(), INTERVAL 3  DAY)),
( 3, 1,  2, 'Martin Marie',   72.0, DATE_SUB(NOW(), INTERVAL 4  DAY)),
( 4, 1,  3, 'Leroy Thomas',   65.3, DATE_SUB(NOW(), INTERVAL 2  DAY)),
( 5, 1,  4, 'Dubois Emma',    58.0, DATE_SUB(NOW(), INTERVAL 5  DAY)),
( 6, 1,  6, 'Bernard Pierre', 44.7, DATE_SUB(NOW(), INTERVAL 6  DAY)),
( 7, 1,  7, 'Moulin Sarah',   38.5, DATE_SUB(NOW(), INTERVAL 7  DAY)),

-- Challenge 3 : Perte de poids (termine)
( 8, 3,  1, 'Dupont Jean',    8.2, DATE_SUB(NOW(), INTERVAL 110 DAY)),
( 9, 3,  5, 'Moreau Lucas',   7.4, DATE_SUB(NOW(), INTERVAL 110 DAY)),
(10, 3,  3, 'Leroy Thomas',   6.8, DATE_SUB(NOW(), INTERVAL 110 DAY)),
(11, 3,  2, 'Martin Marie',   5.5, DATE_SUB(NOW(), INTERVAL 110 DAY)),
(12, 3,  6, 'Bernard Pierre', 4.9, DATE_SUB(NOW(), INTERVAL 110 DAY)),
(13, 3, 11, 'Laurent Antoine',3.1, DATE_SUB(NOW(), INTERVAL 110 DAY)),

-- Challenge 4 : 30 jours de Planche
(14, 4,  1, 'Dupont Jean',   245, DATE_SUB(NOW(), INTERVAL 9  DAY)),
(15, 4,  8, 'Petit Sophie',  210, DATE_SUB(NOW(), INTERVAL 8  DAY)),
(16, 4,  9, 'Roux Camille',  185, DATE_SUB(NOW(), INTERVAL 10 DAY)),
(17, 4,  7, 'Moulin Sarah',  160, DATE_SUB(NOW(), INTERVAL 9  DAY)),
(18, 4,  4, 'Dubois Emma',   130, DATE_SUB(NOW(), INTERVAL 7  DAY));


-- ============================================================
--  6. RESULTATS_CHALLENGE (preuves soumises)
-- ============================================================

INSERT INTO `resultats_challenge`
    (`id_resultat`, `id_challenge`, `id_user`, `valeur_resultat`, `preuve_url`, `date_soumission`)
VALUES
(1, 1, 5,  91.2, 'https://cdn.burnout.fr/preuves/strava_moreau.jpg',   NOW()),
(2, 1, 1,  87.5, 'https://cdn.burnout.fr/preuves/garmin_dupont.jpg',   DATE_SUB(NOW(), INTERVAL 1 DAY)),
(3, 1, 2,  72.0, NULL,                                                  DATE_SUB(NOW(), INTERVAL 2 DAY)),
(4, 3, 1,   8.2, 'https://cdn.burnout.fr/preuves/pesee_dupont.jpg',    DATE_SUB(NOW(), INTERVAL 25 DAY)),
(5, 3, 3,   6.8, NULL,                                                  DATE_SUB(NOW(), INTERVAL 30 DAY));


-- ============================================================
--  7. PROGRAMMES (+3 nouveaux, le ID=1 existe deja)
--
--  ID  Nom                           Type        Niveau
--   1  Programme Prise de Masse      mixte       intermediaire  (existant)
--   2  Cardio & Endurance Debutant   cardio      debutant
--   3  Force Avancee Powerlifting    force       avance
--   4  Souplesse & Mobilite          souplesse   debutant
-- ============================================================

INSERT INTO `programmes`
    (`id_programme`, `nom_programme`, `description`, `type`, `niveau`,
     `duree_semaines`, `seances_par_semaine`, `is_actif`, `id_createur`,
     `date_debut`, `date_fin`, `created_at`)
VALUES
(2, 'Cardio & Endurance Debutant',
 'Remise en forme progressive. 4 seances cardio par semaine, intensite croissante.',
 'cardio', 'debutant', 8, 4, 1, 1,
 DATE_SUB(CURDATE(), INTERVAL 1 MONTH), DATE_ADD(CURDATE(), INTERVAL 7 WEEK),
 DATE_SUB(NOW(), INTERVAL 6 WEEK)),

(3, 'Force Avancee Powerlifting',
 'Preparation competition. Squat, Bench, Deadlift. 16 semaines de periodisation.',
 'force', 'avance', 16, 4, 1, 1,
 DATE_SUB(CURDATE(), INTERVAL 4 WEEK), DATE_ADD(CURDATE(), INTERVAL 12 WEEK),
 DATE_SUB(NOW(), INTERVAL 5 WEEK)),

(4, 'Souplesse & Mobilite',
 'Programme recuperation et amelioration de la mobilite. Ideal en complement.',
 'souplesse', 'debutant', 6, 3, 1, 1,
 CURDATE(), DATE_ADD(CURDATE(), INTERVAL 6 WEEK),
 NOW());


-- ============================================================
--  8. PROGRAMME_ASSIGNATIONS (10 liens client <-> programme)
--
--  UUID format : aa[num]-0000-0000-0000-000000000000
-- ============================================================

INSERT INTO `programme_assignations`
    (`id_assignation`, `id_client`, `id_programme`, `date_debut`, `date_fin`, `date_assignation`)
VALUES
('aa000001-0000-0000-0000-000000000001',  1, 1, DATE_SUB(CURDATE(), INTERVAL 2  MONTH), DATE_ADD(CURDATE(), INTERVAL 1  MONTH), DATE_SUB(NOW(), INTERVAL 2  MONTH)),
('aa000002-0000-0000-0000-000000000002',  2, 2, DATE_SUB(CURDATE(), INTERVAL 3  WEEK),  DATE_ADD(CURDATE(), INTERVAL 5  WEEK),  DATE_SUB(NOW(), INTERVAL 3  WEEK)),
('aa000003-0000-0000-0000-000000000003',  3, 3, DATE_SUB(CURDATE(), INTERVAL 4  WEEK),  DATE_ADD(CURDATE(), INTERVAL 12 WEEK), DATE_SUB(NOW(), INTERVAL 4  WEEK)),
('aa000004-0000-0000-0000-000000000004',  4, 4, CURDATE(),                               DATE_ADD(CURDATE(), INTERVAL 6  WEEK),  NOW()),
('aa000005-0000-0000-0000-000000000005',  5, 1, DATE_SUB(CURDATE(), INTERVAL 1  MONTH), DATE_ADD(CURDATE(), INTERVAL 2  MONTH), DATE_SUB(NOW(), INTERVAL 1  MONTH)),
('aa000006-0000-0000-0000-000000000006',  6, 2, DATE_SUB(CURDATE(), INTERVAL 2  WEEK),  DATE_ADD(CURDATE(), INTERVAL 6  WEEK),  DATE_SUB(NOW(), INTERVAL 2  WEEK)),
('aa000007-0000-0000-0000-000000000007',  7, 3, DATE_SUB(CURDATE(), INTERVAL 3  WEEK),  DATE_ADD(CURDATE(), INTERVAL 13 WEEK), DATE_SUB(NOW(), INTERVAL 3  WEEK)),
('aa000008-0000-0000-0000-000000000008',  8, 4, CURDATE(),                               DATE_ADD(CURDATE(), INTERVAL 6  WEEK),  NOW()),
('aa000009-0000-0000-0000-000000000009',  9, 2, DATE_SUB(CURDATE(), INTERVAL 5  DAY),   DATE_ADD(CURDATE(), INTERVAL 7  WEEK),  DATE_SUB(NOW(), INTERVAL 5  DAY)),
('aa000010-0000-0000-0000-000000000010', 10, 1, DATE_SUB(CURDATE(), INTERVAL 1  WEEK),  DATE_ADD(CURDATE(), INTERVAL 11 WEEK), DATE_SUB(NOW(), INTERVAL 1  WEEK));


-- ============================================================
--  9. SEANCES planifiees (8 seances dans les programmes)
--
--  ID  Programme  Nom                         Realisee ?
--   1     1       Seance Pectoraux / Triceps   oui  (-6j)
--   2     1       Seance Dos / Biceps          oui  (-4j)
--   3     1       Seance Jambes                oui  (-2j)
--   4     1       Seance Epaules / Bras        non  (aujourd hui)
--   5     1       Seance Full Body Recup        non  (+2j)
--   6     3       Jour Squat                   oui  (-7j)
--   7     3       Jour Bench                   oui  (-5j)
--   8     3       Jour Deadlift                oui  (-3j)
-- ============================================================

INSERT INTO `seances`
    (`id_seance`, `id_programme`, `nom_seance`, `date_prevue`, `date_realisation`, `commentaire_client`)
VALUES
(1, 1, 'Seance Pectoraux / Triceps', DATE_SUB(CURDATE(), INTERVAL 6 DAY), DATE_SUB(NOW(), INTERVAL 6 DAY), 'Bonne seance, +2.5kg au bench'),
(2, 1, 'Seance Dos / Biceps',        DATE_SUB(CURDATE(), INTERVAL 4 DAY), DATE_SUB(NOW(), INTERVAL 4 DAY), 'Tractions difficiles, a retravailler'),
(3, 1, 'Seance Jambes',              DATE_SUB(CURDATE(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY), 'Courbatures garanties !'),
(4, 1, 'Seance Epaules / Bras',      CURDATE(),                            NULL,                            NULL),
(5, 1, 'Seance Full Body Recup',     DATE_ADD(CURDATE(), INTERVAL 2 DAY), NULL,                            NULL),
(6, 3, 'Jour Squat',                 DATE_SUB(CURDATE(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 7 DAY), 'PR: 160kg x1'),
(7, 3, 'Jour Bench',                 DATE_SUB(CURDATE(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 5 DAY), 'Technique a peaufiner'),
(8, 3, 'Jour Deadlift',              DATE_SUB(CURDATE(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY), 'Bonne session, dos bien gaine');


-- ============================================================
--  10. CONTENU_SEANCE (exercices planifies dans chaque seance)
--
--  IDs exercices (de la migration) :
--   1=Squat  2=Deadlift  3=Leg Press  4=Fente avant  5=Leg Curl
--   6=Developpe couche  7=Developpe incline  8=Developpe militaire
--   9=Dips  10=Pompes  11=Tractions  12=Rowing barre
--  13=Tirage poulie  14=Curl biceps  15=Extension triceps
--  16=Course  17=Velo  18=Rameur  19=Burpees  20=Corde
--  21=Planche  22=Crunch  23=Releve jambes  24=Russian twist
--  25=Etirement quad  26=Fentes souplesse  27=Rotation epaules
-- ============================================================

INSERT INTO `contenu_seance`
    (`id_contenu`, `id_seance`, `id_exercice`, `series_prevues`, `repetitions_prevues`, `charge_prevue_kg`, `temps_repos_sec`, `ordre_affichage`)
VALUES
-- Seance 1 : Pectoraux / Triceps
( 1, 1,  6, 4, '8',  80.0, 120, 1),   -- Developpe couche
( 2, 1,  7, 3, '10', 60.0,  90, 2),   -- Developpe incline
( 3, 1,  9, 3, '12',  0.0,  90, 3),   -- Dips
( 4, 1, 15, 3, '15', 25.0,  60, 4),   -- Extension triceps
( 5, 1, 10, 2, '20',  0.0,  60, 5),   -- Pompes

-- Seance 2 : Dos / Biceps
( 6, 2, 11, 4, '8',   0.0, 120, 1),   -- Tractions
( 7, 2, 12, 4, '8',  80.0, 120, 2),   -- Rowing barre
( 8, 2, 13, 3, '12', 60.0,  90, 3),   -- Tirage poulie haute
( 9, 2, 14, 3, '15', 14.0,  60, 4),   -- Curl biceps

-- Seance 3 : Jambes
(10, 3,  1, 5, '5',  100.0, 180, 1),  -- Squat
(11, 3,  3, 4, '10', 150.0, 120, 2),  -- Leg Press
(12, 3,  5, 3, '12',  40.0,  90, 3),  -- Leg Curl
(13, 3,  4, 3, '12',  20.0,  90, 4),  -- Fente avant

-- Seance 6 : Jour Squat (Powerlifting)
(14, 6,  1, 5, '3',  140.0, 300, 1),  -- Squat travail
(15, 6,  1, 1, '1',  160.0, 300, 2),  -- Squat max
(16, 6,  3, 3, '8',  180.0, 120, 3),  -- Leg Press complementaire

-- Seance 7 : Jour Bench (Powerlifting)
(17, 7,  6, 5, '3',  120.0, 300, 1),  -- Bench travail
(18, 7,  6, 1, '1',  140.0, 300, 2),  -- Bench max
(19, 7,  9, 3, '10',   0.0, 120, 3),  -- Dips complementaire

-- Seance 8 : Jour Deadlift (Powerlifting)
(20, 8,  2, 5, '3',  180.0, 300, 1),  -- Deadlift travail
(21, 8, 12, 4, '6',  100.0, 120, 2),  -- Rowing barre
(22, 8, 11, 3, '8',    0.0,  90, 3);  -- Tractions


-- ============================================================
--  11. PERFORMANCE_CLIENT (resultats reels — seances terminees)
-- ============================================================

INSERT INTO `performance_client`
    (`id_perf`, `id_contenu`, `series_faites`, `repetitions_faites`, `charge_faite_kg`, `ressenti_difficulte`)
VALUES
-- Seance 1
( 1,  1, 4, '8',  82.5, 7),   -- Developpe couche (+2.5kg)
( 2,  2, 3, '10', 60.0, 6),   -- Developpe incline
( 3,  3, 3, '10',  0.0, 8),   -- Dips (2 reps de moins)
-- Seance 2
( 4,  6, 4, '7',   0.0, 8),   -- Tractions (1 rep de moins)
( 5,  7, 4, '8',  80.0, 7),   -- Rowing barre
-- Seance 3
( 6, 10, 5, '5',  102.5, 8),  -- Squat (+2.5kg)
( 7, 11, 4, '10', 150.0, 7),  -- Leg Press
-- Seance 6
( 8, 14, 5, '3',  140.0, 7),  -- Squat travail
( 9, 15, 1, '1',  162.5, 9),  -- Squat PR (+2.5kg !)
-- Seance 8
(10, 20, 5, '3',  182.5, 8);  -- Deadlift (+2.5kg)


-- ============================================================
--  12. LOGS_ACCES NFC (50 entrees sur 7 jours)
--
--  Scenarios :
--   - autorise  : client actif avec abonnement valide
--   - refuse    : abonnement expire (client 11)
--   - refuse    : compte suspendu (client 15)
--   - refuse    : carte inconnue (uid fictif, id_client NULL)
-- ============================================================

INSERT INTO `logs_acces`
    (`id_log`, `event_id`, `uid_nfc`, `id_client`, `nom_client`,
     `resultat`, `raison`, `porte`, `source`, `timestamp_utc`)
VALUES
-- AUJOURD HUI
( 1,'ev000001-0000-0000-0000-000000000001','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1  HOUR,'%Y-%m-%d %H:00:00')),
( 2,'ev000002-0000-0000-0000-000000000002','04:2B:3C:4D', 2,'Martin Marie',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2  HOUR,'%Y-%m-%d %H:00:00')),
( 3,'ev000003-0000-0000-0000-000000000003','04:5E:6F:7A', 5,'Moreau Lucas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 3  HOUR,'%Y-%m-%d %H:00:00')),
( 4,'ev000004-0000-0000-0000-000000000004','04:4D:5E:6F', 4,'Dubois Emma',    'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 4  HOUR,'%Y-%m-%d %H:00:00')),
( 5,'ev000005-0000-0000-0000-000000000005','04:FF:FF:01', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 5  HOUR,'%Y-%m-%d %H:00:00')),
( 6,'ev000006-0000-0000-0000-000000000006','04:8B:9C:AD',11,'Laurent Antoine','refuse',  'Abonnement expire', 'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 6  HOUR,'%Y-%m-%d %H:00:00')),
( 7,'ev000007-0000-0000-0000-000000000007','04:7A:8B:9C', 7,'Moulin Sarah',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7  HOUR,'%Y-%m-%d %H:00:00')),
( 8,'ev000008-0000-0000-0000-000000000008','04:6F:7A:8B', 6,'Bernard Pierre', 'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 8  HOUR,'%Y-%m-%d %H:00:00')),
( 9,'ev000009-0000-0000-0000-000000000009','04:9C:AD:BE',15,'Girard Maxime',  'refuse',  'Compte suspendu',  'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 9  HOUR,'%Y-%m-%d %H:00:00')),
(10,'ev000010-0000-0000-0000-000000000010','04:3C:4D:5E', 3,'Leroy Thomas',   'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 10 HOUR,'%Y-%m-%d %H:00:00')),
-- HIER
(11,'ev000011-0000-0000-0000-000000000011','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 07:30:00')),
(12,'ev000012-0000-0000-0000-000000000012','04:4D:5E:6F', 4,'Dubois Emma',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 08:15:00')),
(13,'ev000013-0000-0000-0000-000000000013','04:2B:3C:4D', 2,'Martin Marie',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 09:00:00')),
(14,'ev000014-0000-0000-0000-000000000014','04:FF:FF:02', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 10:45:00')),
(15,'ev000015-0000-0000-0000-000000000015','04:5E:6F:7A', 5,'Moreau Lucas',   'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 11:00:00')),
(16,'ev000016-0000-0000-0000-000000000016','04:7A:8B:9C', 7,'Moulin Sarah',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 17:30:00')),
(17,'ev000017-0000-0000-0000-000000000017','04:8B:9C:AD',11,'Laurent Antoine','refuse',  'Abonnement expire', 'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 18:00:00')),
(18,'ev000018-0000-0000-0000-000000000018','04:6F:7A:8B', 6,'Bernard Pierre', 'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 18:30:00')),
(19,'ev000019-0000-0000-0000-000000000019','04:3C:4D:5E', 3,'Leroy Thomas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 19:00:00')),
(20,'ev000020-0000-0000-0000-000000000020','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 1 DAY,'%Y-%m-%d 20:00:00')),
-- IL Y A 2 JOURS
(21,'ev000021-0000-0000-0000-000000000021','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 07:00:00')),
(22,'ev000022-0000-0000-0000-000000000022','04:5E:6F:7A', 5,'Moreau Lucas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 08:00:00')),
(23,'ev000023-0000-0000-0000-000000000023','04:4D:5E:6F', 4,'Dubois Emma',    'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 09:30:00')),
(24,'ev000024-0000-0000-0000-000000000024','04:FF:FF:03', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 12:00:00')),
(25,'ev000025-0000-0000-0000-000000000025','04:2B:3C:4D', 2,'Martin Marie',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 18:00:00')),
(26,'ev000026-0000-0000-0000-000000000026','04:9C:AD:BE',15,'Girard Maxime',  'refuse',  'Compte suspendu',  'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 2 DAY,'%Y-%m-%d 19:15:00')),
-- IL Y A 3 JOURS
(27,'ev000027-0000-0000-0000-000000000027','04:7A:8B:9C', 7,'Moulin Sarah',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 3 DAY,'%Y-%m-%d 07:45:00')),
(28,'ev000028-0000-0000-0000-000000000028','04:3C:4D:5E', 3,'Leroy Thomas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 3 DAY,'%Y-%m-%d 08:30:00')),
(29,'ev000029-0000-0000-0000-000000000029','04:6F:7A:8B', 6,'Bernard Pierre', 'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 3 DAY,'%Y-%m-%d 09:00:00')),
(30,'ev000030-0000-0000-0000-000000000030','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 3 DAY,'%Y-%m-%d 17:00:00')),
(31,'ev000031-0000-0000-0000-000000000031','04:FF:FF:04', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 3 DAY,'%Y-%m-%d 20:30:00')),
-- IL Y A 4 JOURS
(32,'ev000032-0000-0000-0000-000000000032','04:2B:3C:4D', 2,'Martin Marie',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 4 DAY,'%Y-%m-%d 07:00:00')),
(33,'ev000033-0000-0000-0000-000000000033','04:5E:6F:7A', 5,'Moreau Lucas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 4 DAY,'%Y-%m-%d 08:00:00')),
(34,'ev000034-0000-0000-0000-000000000034','04:4D:5E:6F', 4,'Dubois Emma',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 4 DAY,'%Y-%m-%d 18:30:00')),
(35,'ev000035-0000-0000-0000-000000000035','04:8B:9C:AD',11,'Laurent Antoine','refuse',  'Abonnement expire', 'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 4 DAY,'%Y-%m-%d 19:00:00')),
-- IL Y A 5 JOURS
(36,'ev000036-0000-0000-0000-000000000036','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 5 DAY,'%Y-%m-%d 07:30:00')),
(37,'ev000037-0000-0000-0000-000000000037','04:7A:8B:9C', 7,'Moulin Sarah',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 5 DAY,'%Y-%m-%d 09:00:00')),
(38,'ev000038-0000-0000-0000-000000000038','04:3C:4D:5E', 3,'Leroy Thomas',   'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 5 DAY,'%Y-%m-%d 10:00:00')),
(39,'ev000039-0000-0000-0000-000000000039','04:6F:7A:8B', 6,'Bernard Pierre', 'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 5 DAY,'%Y-%m-%d 17:00:00')),
(40,'ev000040-0000-0000-0000-000000000040','04:FF:FF:05', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 5 DAY,'%Y-%m-%d 21:00:00')),
-- IL Y A 6 JOURS
(41,'ev000041-0000-0000-0000-000000000041','04:2B:3C:4D', 2,'Martin Marie',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 6 DAY,'%Y-%m-%d 08:00:00')),
(42,'ev000042-0000-0000-0000-000000000042','04:5E:6F:7A', 5,'Moreau Lucas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 6 DAY,'%Y-%m-%d 08:30:00')),
(43,'ev000043-0000-0000-0000-000000000043','04:4D:5E:6F', 4,'Dubois Emma',    'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 6 DAY,'%Y-%m-%d 09:00:00')),
(44,'ev000044-0000-0000-0000-000000000044','04:1A:2B:3C', 1,'Dupont Jean',    'autorise',NULL,                'Zone machines',    'rfid',DATE_FORMAT(NOW() - INTERVAL 6 DAY,'%Y-%m-%d 18:00:00')),
(45,'ev000045-0000-0000-0000-000000000045','04:9C:AD:BE',15,'Girard Maxime',  'refuse',  'Compte suspendu',  'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 6 DAY,'%Y-%m-%d 19:30:00')),
-- IL Y A 7 JOURS
(46,'ev000046-0000-0000-0000-000000000046','04:7A:8B:9C', 7,'Moulin Sarah',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7 DAY,'%Y-%m-%d 07:00:00')),
(47,'ev000047-0000-0000-0000-000000000047','04:3C:4D:5E', 3,'Leroy Thomas',   'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7 DAY,'%Y-%m-%d 08:15:00')),
(48,'ev000048-0000-0000-0000-000000000048','04:8B:9C:AD',11,'Laurent Antoine','refuse',  'Abonnement expire', 'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7 DAY,'%Y-%m-%d 09:00:00')),
(49,'ev000049-0000-0000-0000-000000000049','04:6F:7A:8B', 6,'Bernard Pierre', 'autorise',NULL,                'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7 DAY,'%Y-%m-%d 18:00:00')),
(50,'ev000050-0000-0000-0000-000000000050','04:FF:FF:06', NULL,'',             'refuse',  'Carte inconnue',   'Entree principale','rfid',DATE_FORMAT(NOW() - INTERVAL 7 DAY,'%Y-%m-%d 20:00:00'));


-- ============================================================
--  13. SEANCES_BUILDER (3 templates Program Builder)
--  JSON sans accents pour eviter les problemes d encodage
-- ============================================================

INSERT INTO `seances_builder`
    (`id_seance_builder`, `nom`, `description`, `exercise_count`, `category_count`, `data_json`)
VALUES
(1, 'Push Day', 'Poussee horizontale et verticale — pectoraux, epaules, triceps', 6, 3,
'{"id":"b1000001-0000-0000-0000-000000000001","name":"Push Day","order":1,"categories":[{"id":"b1000001-0000-0000-0000-000000000002","name":"Echauffement","order":1,"sub_categories":[{"id":"b1000001-0000-0000-0000-000000000003","name":"Mobilisation epaules","order":1,"sets":2,"rest_time":30,"type":"warmup","exercises":[{"id":"b1000001-0000-0000-0000-000000000004","name":"Rotation epaules","sets":2,"reps":15,"weight":0,"rpe":5,"order":1}]}]},{"id":"b1000001-0000-0000-0000-000000000005","name":"Pectoraux","order":2,"sub_categories":[{"id":"b1000001-0000-0000-0000-000000000006","name":"Developpe couche","order":1,"sets":4,"rest_time":120,"type":"strength","exercises":[{"id":"b1000001-0000-0000-0000-000000000007","name":"Developpe couche","sets":4,"reps":8,"weight":80,"rpe":8,"order":1},{"id":"b1000001-0000-0000-0000-000000000008","name":"Developpe incline","sets":3,"reps":10,"weight":60,"rpe":7,"order":2}]}]},{"id":"b1000001-0000-0000-0000-000000000009","name":"Triceps","order":3,"sub_categories":[{"id":"b1000001-0000-0000-0000-000000000010","name":"Isolation triceps","order":1,"sets":3,"rest_time":60,"type":"isolation","exercises":[{"id":"b1000001-0000-0000-0000-000000000011","name":"Extension triceps","sets":3,"reps":15,"weight":25,"rpe":7,"order":1},{"id":"b1000001-0000-0000-0000-000000000012","name":"Dips","sets":3,"reps":12,"weight":0,"rpe":8,"order":2}]}]}]}'),

(2, 'Pull Day', 'Tirage horizontal et vertical — dos, biceps', 4, 2,
'{"id":"b2000001-0000-0000-0000-000000000001","name":"Pull Day","order":2,"categories":[{"id":"b2000001-0000-0000-0000-000000000002","name":"Dos","order":1,"sub_categories":[{"id":"b2000001-0000-0000-0000-000000000003","name":"Tirage","order":1,"sets":4,"rest_time":120,"type":"strength","exercises":[{"id":"b2000001-0000-0000-0000-000000000004","name":"Tractions","sets":4,"reps":8,"weight":0,"rpe":8,"order":1},{"id":"b2000001-0000-0000-0000-000000000005","name":"Rowing barre","sets":4,"reps":8,"weight":80,"rpe":8,"order":2},{"id":"b2000001-0000-0000-0000-000000000006","name":"Tirage poulie haute","sets":3,"reps":12,"weight":60,"rpe":7,"order":3}]}]},{"id":"b2000001-0000-0000-0000-000000000007","name":"Biceps","order":2,"sub_categories":[{"id":"b2000001-0000-0000-0000-000000000008","name":"Isolation","order":1,"sets":3,"rest_time":60,"type":"isolation","exercises":[{"id":"b2000001-0000-0000-0000-000000000009","name":"Curl biceps","sets":3,"reps":12,"weight":14,"rpe":7,"order":1}]}]}]}'),

(3, 'Leg Day', 'Membres inferieurs — quadriceps, ischio, fessiers', 4, 2,
'{"id":"b3000001-0000-0000-0000-000000000001","name":"Leg Day","order":3,"categories":[{"id":"b3000001-0000-0000-0000-000000000002","name":"Membres inferieurs","order":1,"sub_categories":[{"id":"b3000001-0000-0000-0000-000000000003","name":"Compound","order":1,"sets":5,"rest_time":180,"type":"strength","exercises":[{"id":"b3000001-0000-0000-0000-000000000004","name":"Squat","sets":5,"reps":5,"weight":100,"rpe":8,"order":1},{"id":"b3000001-0000-0000-0000-000000000005","name":"Leg Press","sets":4,"reps":10,"weight":150,"rpe":7,"order":2},{"id":"b3000001-0000-0000-0000-000000000006","name":"Fente avant","sets":3,"reps":12,"weight":20,"rpe":7,"order":3}]}]},{"id":"b3000001-0000-0000-0000-000000000007","name":"Isolation","order":2,"sub_categories":[{"id":"b3000001-0000-0000-0000-000000000008","name":"Ischio-jambiers","order":1,"sets":3,"rest_time":90,"type":"isolation","exercises":[{"id":"b3000001-0000-0000-0000-000000000009","name":"Leg Curl","sets":3,"reps":12,"weight":40,"rpe":7,"order":1}]}]}]}');


-- ============================================================
--  14. MESSAGES
-- ============================================================

INSERT INTO `messages` (`id_message`, `id_expediteur`, `id_destinataire`, `contenu`, `date_envoi`, `est_lu`) VALUES
(1, 1, 2, 'Bonjour, votre prochain programme demarre lundi. Pret ?',                DATE_SUB(NOW(), INTERVAL 3 DAY), 1),
(2, 2, 1, 'Oui, hate de commencer ! A lundi.',                                      DATE_SUB(NOW(), INTERVAL 3 DAY), 1),
(3, 1, 3, 'Thomas, pensez a recuperer votre carte NFC a l accueil.',                DATE_SUB(NOW(), INTERVAL 1 DAY), 0),
(4, 1, 5, 'Lucas, votre abonnement expire dans 15 jours. Pensez a le renouveler.', DATE_SUB(NOW(), INTERVAL 2 DAY), 1),
(5, 1, 4, 'Emma, votre programme Souplesse commence cette semaine.',                 NOW(),                           0);


-- ============================================================
--  FIN
-- ============================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;


-- ============================================================
--  VERIFICATION (a coller separement dans phpMyAdmin)
-- ============================================================
/*

-- Decompte par table
SELECT 'clients'              AS tbl, COUNT(*) n FROM clients
UNION ALL SELECT 'abonnements',             COUNT(*) FROM abonnements
UNION ALL SELECT 'ligues',                  COUNT(*) FROM ligues
UNION ALL SELECT 'challenges',              COUNT(*) FROM challenges
UNION ALL SELECT 'challenge_participants',  COUNT(*) FROM challenge_participants
UNION ALL SELECT 'resultats_challenge',     COUNT(*) FROM resultats_challenge
UNION ALL SELECT 'programmes',              COUNT(*) FROM programmes
UNION ALL SELECT 'programme_assignations',  COUNT(*) FROM programme_assignations
UNION ALL SELECT 'seances',                 COUNT(*) FROM seances
UNION ALL SELECT 'contenu_seance',          COUNT(*) FROM contenu_seance
UNION ALL SELECT 'performance_client',      COUNT(*) FROM performance_client
UNION ALL SELECT 'logs_acces',              COUNT(*) FROM logs_acces
UNION ALL SELECT 'seances_builder',         COUNT(*) FROM seances_builder
UNION ALL SELECT 'exercices',               COUNT(*) FROM exercices
UNION ALL SELECT 'messages',               COUNT(*) FROM messages;

-- Clients + abonnements
SELECT c.id_client, c.prenom, c.nom, c.statut, c.nfc_uid,
       a.type, a.date_fin, DATEDIFF(a.date_fin, CURDATE()) AS jours_restants
FROM clients c
LEFT JOIN abonnements a ON a.id_client = c.id_client
ORDER BY c.statut, jours_restants;

-- Classement challenge Cardio
SELECT nom_client, valeur_actuelle,
       RANK() OVER (ORDER BY valeur_actuelle DESC) AS rang
FROM challenge_participants WHERE id_challenge = 1;

-- Logs du jour
SELECT id_log, timestamp_utc, nom_client, uid_nfc, resultat, raison
FROM logs_acces
WHERE DATE(timestamp_utc) = CURDATE()
ORDER BY timestamp_utc DESC;

-- Simulation dashboard
SELECT
    (SELECT COUNT(*) FROM clients     WHERE statut = 'actif')                                            AS clients_actifs,
    (SELECT COUNT(*) FROM logs_acces  WHERE DATE(timestamp_utc) = CURDATE())                             AS acces_aujourd_hui,
    (SELECT COUNT(*) FROM logs_acces  WHERE DATE(timestamp_utc) = CURDATE() AND resultat = 'refuse')     AS alertes,
    (SELECT COUNT(*) FROM challenges  WHERE statut = 'actif')                                            AS challenges_actifs,
    (SELECT COUNT(*) FROM programmes  WHERE is_actif = 1)                                                AS programmes_actifs,
    (SELECT COUNT(*) FROM abonnements WHERE date_fin BETWEEN CURDATE() AND DATE_ADD(CURDATE(),INTERVAL 30 DAY)) AS expirant_bientot;

*/
