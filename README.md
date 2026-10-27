# Survie : jeu de survie 3D (Unity 6, Android)

Jeu de survie à la troisième personne. Le joueur récolte du bois et de la pierre, fabrique des outils,
gère sa vie, sa faim, sa soif et son énergie, combat des ennemis contrôlés par une IA et construit un abri.
Chaque membre de l'équipe utilise un capteur différent du téléphone.

## Ouvrir le projet

1. Unity Hub → **Add** → dossier `SurvivalGame` (Unity **6000.6.4f1**, module Android installé).
2. Ouvrir la scène `Assets/Scenes/Survie.unity` et appuyer sur **Play**.
3. Pour régénérer toute la scène (objets, prefabs, matériaux, NavMesh) : menu **Survie → Construire la scène du jeu**.
   ⚠️ Ça écrase la scène `Survie.unity`. Si vous faites des changements à la main, faites-les dans les **prefabs** (`Assets/Prefabs`) ou dans le **code**, ou alors arrêtez d'utiliser le builder.

## Répartition

| Membre | Module | Capteur | Dossier |
|---|---|---|---|
| P1 | Personnage, caméra, récolte | Gyroscope : tourner le téléphone oriente la caméra | `Assets/Scripts/P1_Personnage` |
| P2 | Objets et inventaire | Proximité : passer la main devant le haut du téléphone ouvre/ferme le sac | `Assets/Scripts/P2_Inventaire` |
| P3 | Équipement et craft | Magnétomètre : boussole craftable, indique le vrai nord et la direction de l'abri | `Assets/Scripts/P3_Craft` |
| P4 | Survie, ennemis, combat | Accéléromètre : secouer le téléphone pour attaquer / frapper un arbre ou un rocher | `Assets/Scripts/P4_Survie_Combat` |
| P5 | Construction, sauvegarde, menus | Lumière ambiante : pièce sombre = nuit, ennemis plus dangereux, il faut se réfugier dans l'abri | `Assets/Scripts/P5_Construction_Sauvegarde` |
| Bonus | Podomètre | Les pas réels remontent la barre de faim | `Assets/Scripts/Bonus_Podometre` |

`Assets/Scripts/Core` est partagé (GameManager, entrées, UI de base) : **prévenez l'équipe avant de le modifier**.

### Fichiers par membre

- **P1** : `GyroCameraInput` (capteur), `ThirdPersonCamera`, `PlayerController`, `PlayerActions` (bouton Action), `ResourceNode` (arbre/rocher/buisson), `ControlsUI`, `VirtualJoystick`, `TouchLookArea`
- **P2** : `ProximityInventoryToggle` (capteur), `Items` (liste des objets et recettes), `Inventory`, `InventoryUI`
- **P3** : `MagnetometerCompass` (capteur), `CompassUI`, `CraftingUI`, `EquipmentVisual`
- **P4** : `ShakeDetector` (capteur), `PlayerStats`, `StatsHUD`, `EnemyAI`, `EnemySpawner`, `WaterSource`
- **P5** : `AmbientLightSensor` (capteur), `DayNightCycle`, `BuildSystem`, `BuildPiece`, `BuildUI`, `CampfireFlicker`, `SaveSystem`, `MenuUI`
- **Bonus** : `Pedometer`

Dans la scène, chaque membre a son propre GameObject (`P1 - Personnage (Gyroscope)`, `P2 - Inventaire (Proximite)`, ...).

## Commandes

Chaque capteur a un **bouton tactile de secours** (le jeu reste jouable sans le capteur).

| Action | Téléphone | Secours tactile | Clavier (Editor) |
|---|---|---|---|
| Se déplacer | | Joystick à gauche | ZQSD / WASD / flèches |
| Caméra | Tourner le téléphone (gyroscope) | Glisser à droite | Glisser la souris à droite |
| Action (couper, miner, cueillir, boire, attaquer) | Secouer (accéléromètre) | Bouton Action | E ou F |
| Sac | Main devant le capteur de proximité | Bouton Sac | Tab ou I |
| Sprint / Saut | | Boutons Sprint / Saut | Shift / Espace |
| Simuler la lumière (sombre / éclairée) | Capteur de lumière | | L |
| Simuler 10 pas | Podomètre | | P |
| Pause | | Bouton II | Échap |

Le menu principal affiche la liste des capteurs détectés sur le téléphone (`[OK]` ou `[--]`).

## Gameplay

- **Ressources** : arbre → bois, rocher → pierre, buisson → baies, lac → boire. La hache / la pioche donnent 3× plus.
- **Craft** : Hache (3 bois, 2 pierre), Pioche (3 bois, 3 pierre), Épée (2 bois, 5 pierre), Boussole (1 bois, 3 pierre).
- **Construction** (grille de 3 m) : Sol, Mur, Mur avec porte, Toit, Feu de camp. Sous un toit = à l'abri (les ennemis ne t'attaquent plus, la faim et la soif baissent 2× moins vite). Près d'un feu = soin plus rapide et ennemis tenus à distance.
- **Nuit** : plus d'ennemis, qui voient plus loin, courent plus vite et frappent plus fort.
- **Sauvegarde** : automatique toutes les 60 s, quand on quitte l'application et via le menu Pause. Le menu principal propose « Continuer ».

## Test automatique

`Assets/Scripts/Tests/PlaytestDriver.cs` joue une partie complète en mode Play : déplacement, récolte, boire et manger, craft, construction et abri, ennemi, nuit par le capteur de lumière, podomètre, pause, sauvegarde/chargement et mort. Il ne s'exécute que dans l'Editor et seulement si la variable `SURVIE_PLAYTEST=1` est définie, donc jamais pendant une partie normale ni dans le build Android. Unity doit être fermé avant de lancer la commande :

```bash
SURVIE_PLAYTEST=1 /Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -executeMethod PlaytestRunner.Run -logFile Logs/playtest.log
```

Le rapport et des captures d'écran sont écrits dans `Logs/Playtest/` (`rapport.txt` commence par `RESULTAT : TOUT EST OK` si tout passe).

## Build Android

File → Build Profiles → Android → **Switch Platform** (déjà fait), brancher le téléphone (mode développeur + débogage USB), puis **Build And Run**.
Réglages déjà configurés : paysage, IL2CPP, ARM64, permission `ACTIVITY_RECOGNITION` ajoutée automatiquement pour le podomètre.

⚠️ Testez les capteurs **tôt sur vos vrais téléphones** : certains modèles n'ont pas de gyroscope, de magnétomètre ou de capteur de lumière.
Si la caméra tourne dans le mauvais sens avec le gyroscope : menu Pause → « Inverser gyroscope ».
