# V6 Gameplay Deep Test — 2026-05-12 23h00

**Tester** : Sonnet QA agent (Claude Opus 4.7)
**Cible** : `/Users/mike/Work/crowd-defense` (Unity 6 LTS, V6 post-R6-PARITY-V4 sprint)
**Méthode** : UnityMCP HTTP @ `http://127.0.0.1:8080/mcp`
**Durée** : ~25 min

---

## TL;DR — Test session **BLOCKED**

**Tous les 7 tests gameplay : SKIPPED** — Cause unique : UnityMCP bridge WebSocket dead-loop côté Unity Editor.

L'instance Unity `crowd-defense@144c22fa9f095059` est listée par le MCP server, mais **tout appel tools/call retourne** :
```
"Unity session not ready for '<tool>' (ping not answered); please retry"
```

Le `Editor.log` Unity montre des centaines de cycles `[WebSocket] Connection closed: The remote party closed the WebSocket connection without completing the close handshake.` + `Keep-alive failed:` qui se répètent toutes les 2-5s. Unity-side client reconnecte (nouveau `session_id` toutes les minutes) mais ferme aussitôt. Aucun tool call ne survit assez longtemps pour aboutir.

---

## Test 0 — Bridge health check (NEW, blocker)

**Status** : **FAIL** — bridge WebSocket en boucle de déconnexion.

Détails :
- MCP server `/health` répond OK (version 9.6.8, `{"status":"healthy"}`).
- `mcpforunity://instances` liste 1 instance : `crowd-defense@144c22fa9f095059` (Unity `6000.3.15f1`).
- `set_active_instance` réussit (renvoie `{"success":true}`).
- `manage_editor action=telemetry_ping` réussit (renvoie `{"success":true,"message":"telemetry ping queued"}`) mais l'inverse n'est pas réversible : `telemetry_status` retourne `{"telemetry_enabled":true}` sans données utiles.
- **Tous** les autres tool calls (`read_console`, `execute_code`, `find_gameobjects`, `resources/read editor/state`, `resources/read project/info`) retournent `{"error":"Unity session not ready ... (ping not answered); please retry"}` ou `{"error":"TimeoutError"}`.
- `refresh_unity` lui-même timeout après 60s avec `{"timeout":true,"wait_seconds":60.0}`.
- Multiple sessions HTTP MCP créées (3 SID différents) — même résultat.
- 16 `UnityShaderCompiler` processus + 6 `AssetImportWorker` actifs au début, **toujours** 16 + 6 après 5+ minutes d'attente → asset import n'est pas la cause (sinon ça baisserait).

**Symptômes Editor.log** (extrait représentatif, répété > 50 fois) :
```
MCP-FOR-UNITY: [WebSocket] Unexpected receive error: WebSocket is not initialised
MCP-FOR-UNITY: [WebSocket] Receive loop error: The remote party closed ...
MCP-FOR-UNITY: [WebSocket] Connection closed: The remote party closed ...
MCP-FOR-UNITY: [WebSocket] Keep-alive failed: The remote party closed ...
```

Source côté Unity package : `Library/PackageCache/com.coplaydev.unity-mcp@13fb3ee12774/Editor/Services/Transport/Transports/WebSocketTransportClient.cs:763` (`HandleSocketClosureAsync`).

**Hypothèse root cause** : MCPForUnity Editor package en mauvais état après un domain reload pendant que le MCP server était live. Le client Unity ouvre le socket, le serveur l'accepte (instance listée), mais à la première frame envoyée, fermeture sans handshake → boucle de reconnexion stérile. Possiblement un mismatch versions client/server (server v9.6.8, package commit `13fb3ee12774`).

**Reproduction** : 100% (toute la durée du test, ~25 min sans amélioration).

**Action requise pour débloquer** :
1. **Restart Unity Editor** (Cmd+Q puis relaunch) — purgera l'état client WebSocket en boucle. À tenter en premier.
2. Si KO après restart : `rm -rf Library/PackageCache/com.coplaydev.unity-mcp*` + Refresh Packages.
3. Si KO encore : version-pin du package MCPForUnity ou rollback à un commit antérieur.

---

## Tests 1-7 — **SKIPPED, blocker bridge**

Chaque test ci-dessous nécessite l'API MCP fonctionnelle. Aucun test n'a pu être exécuté.

| # | Test | Status | Blocker |
|---|------|--------|---------|
| 1 | Screenshot Play mode initial state | SKIP | `execute_code` timeout |
| 2 | Tower placement via reflection | SKIP | `execute_code` + `find_gameobjects` timeout |
| 3 | Wave spawn + tower combat | SKIP | `manage_editor action=play` + reflection timeout |
| 4 | Multi-wave progression | SKIP | dépend de test 3 |
| 5 | Boss wave | SKIP | dépend de test 3 |
| 6 | Tower upgrade L1→L2→L3 | SKIP | dépend de test 2 |
| 7 | Economy gold gain on kill | SKIP | dépend de test 3 |

**Screenshots produits** : 0 (le dossier `/Users/mike/Work/crowd-defense/.claude/audit/screenshots/` a été créé mais reste vide).

---

## Analyse fallback (code-only inspection)

Sans accès runtime, voici une vérification statique des APIs annoncées dans la mission, pour identifier ce qui aurait pu PASS/FAIL si le bridge fonctionnait :

### API surface confirmée

`/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/PlacementController.cs:13` — `PlacementController : MonoSingleton<PlacementController>` :
- `Instance` (via MonoSingleton) ✅
- `PlacedTowers` (IReadOnlyList<Tower>) ✅
- `SelectedTower` ✅
- `OnTowerPlaced`, `OnTowerUpgraded`, `OnTowerSold`, `OnHoverPlacementCell`, `OnEmptyBuildableTileClick` events ✅
- `SelectTowerForPlacement(TowerType?)` ✅
- `RemoveTower(Tower)`, `UnregisterTower(Tower)`, `RestoreTowers(List<PlacedTowerEntry>)` ✅
- **MANQUE** : pas de méthode publique `PlaceTowerAt(cell)` directe — l'API attend un workflow `SelectTowerForPlacement(type)` → clic sur tile vide (event `OnEmptyBuildableTileClick`). **Pour le test 2 il aurait fallu** trigger ces deux étapes par reflection, ou trouver la méthode interne `TryPlaceTower(cell)`.

`/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/WaveManager.cs:13` — `WaveManager : MonoSingleton<WaveManager>` :
- `Instance` ✅
- `ActiveEnemies`, `CurrentWaveIdx`, `WaveDisplayNumber`, `TotalWaves`, `IsWaveActive`, `IsWaitingForPlayerStart`, `PendingSpawnCount`, `SpawnTimerMs`, `SpawnIntervalMs`, `SkipWindowSecondsRemaining`, `StreakCount`, `NextWaveDisplayNumber`, `StreakRewardMul`, `EndlessGoldMul` properties ✅
- `OnWaveStart(int)`, `OnWaveCleared(int)`, `OnAllWavesCompleted()`, `OnBreakStateChanged()` events ✅
- Recherche `StartNextWave` : présent (référencé dans les properties, méthode `private` mais accessible via reflection)
- **OK pour tests 3-4-5** : surface API suffisante.

`/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/Economy.cs:14` — `Economy : MonoSingleton<Economy>` :
- `Gold` (int, getter only) ✅
- `AddGold(int)`, `AddGoldFromKill(int)`, `AddGoldFromKill(int, Vector3)` ✅
- `OnGoldChanged(int)` event ✅
- **OK pour test 7** : surface API suffisante.

### Architecture V6 mappée

Sans pouvoir spawner d'entités, voici l'état des assets de gameplay (statique) :

- **64 fichiers Systems/** dont `LevelRunner.cs` (846 LOC), `WaveManager.cs` (553 LOC), `Economy.cs`, `PlacementController.cs`, `Achievements.cs`, `BossSystem.cs`, `DoctrineSystem.cs`, `DailyChallenge.cs`, `EndlessMode.cs`, `EventManager.cs`, `EnemyPool.cs`.
- **25 fichiers Entities/** dont `Tower.cs` (498 LOC, partiel : `.Combat`, `.Anim`, `.Effects`, `.Placement`, `.Upgrade`), `Enemy.cs` (436 LOC, partiel : `.Init`, `.Anim`, `.Behaviors`, `.Combat`, `.Lifecycle`, `.Movement`, `.Stats`, `.Update`), `Hero.cs`, `Castle.cs` (`.HP`, `.VFX`), `Projectile.cs`, `HeroProjectile.cs`, `MineExplosive.cs`, `TreasureTile.cs`, `EnemyBossBehaviors.cs`.
- **83 fichiers UI/** dont `BossUI`, `BossHpBarController`, `BossIntroBannerController`, `WaveBannerController`, `WaveClearedController`, `WavePreviewController`, `WaveTipsController`, `LevelSummaryController`, `EndScreenController`, `CutsceneController`, `AchievementsPanel`, `BestiaryPanel`, `DailyChallengeModal`, `HelpOverlayController`.

**Verdict statique** : V6 a la surface fonctionnelle nécessaire pour tous les 7 tests demandés. Le code est en place ; **seul le bridge bloque la validation runtime**.

---

## Top 5 issues bloquantes (estimées, non confirmées par runtime)

> Disclaimer : sans tests live, ces items sont déduits de l'inspection code + Editor.log warnings.

1. **🔴 BLOCKER — UnityMCP bridge dead** : WebSocket loop côté Unity Editor empêche tout test runtime. **Restart Unity Editor obligatoire** pour reprendre la session de test.

2. **🟠 AudioMixer not assigned** : Editor.log montre 3 warnings répétés au scene load :
   - `[AudioMixerController] AudioMixer not assigned — cannot set Music_Volume.`
   - `[AudioMixerController] AudioMixer not assigned — cannot set UI_Volume.`
   - `[AudioMixerController] AudioMixer not assigned — cannot set <SFX> Volume.`
   Source : `Assets/Scripts/Systems/AudioMixerController.cs:37`, déclenché par `SettingsRegistry.ApplyAudio()` au démarrage. **Bloque le SO-AUDIO axis (cf. STATUS.md)** — l'AudioMixer SO n'est pas wired sur le `MasterMixer` Inspector field.

3. **🟠 Placement API gap** : Pas de méthode publique `PlaceTowerAt(cell, type)` sur `PlacementController` ; l'API force le workflow `SelectTowerForPlacement` → clic. Pour QA automation (test 2 et 6), il faudrait soit exposer une méthode test-only, soit accepter le reflection.

4. **🟡 V4 parity gap déclaré ~75%** : selon STATUS.md, manquent encore "80 niveaux complets, upgrade L3 hybride, iOS/Steam builds". Test 6 (upgrade L3) **risque FAIL** si L3 pas câblé sur tous towers.

5. **🟡 Shader compile / Asset import jamais finis** : 16 ShaderCompiler + 6 AssetImportWorker actifs 5+ min sans diminuer suggère que Unity est encore en cours d'import massif (probable suite à refresh ou pivot de package récent). Test session devrait attendre l'import complet avant runtime test.

---

## Recommandations Mike

**Immédiat (pour reprendre QA)** :
1. Quitter Unity Editor (Cmd+Q sur Unity.app, PID 21801).
2. Attendre 10s, relancer Unity Hub → ouvrir `/Users/mike/Work/crowd-defense`.
3. Laisser asset import finir complètement (vérifier que les shader compilers passent à 0 ou 1).
4. Re-lancer ce script de QA — la connexion WebSocket sera fraîche.

**Pendant que le bridge n'est pas debloqué** :
- Le déploiement WebGL `/v6/` live (`https://michaelchevallier.github.io/crowd-defense/v6/`) reste **testable manuellement** via Chrome / Chrome MCP. Cette piste n'a pas été tentée ici car la mission explicite était UnityMCP, mais elle resterait disponible en fallback.

**Mid-term** :
- Documenter la procédure de récupération bridge dans `STATUS.md` ou `CLAUDE.md` (section "Pièges UnityMCP").
- Pinner la version du package `com.coplaydev.unity-mcp` à un commit stable connu pour éviter régression silencieuse.
- Exposer dans `PlacementController` une méthode `internal TryPlaceTowerAt(Vector2Int cell, TowerType type)` pour les tests automation (sans casser le workflow user).

---

## Conclusion

**Mission gameplay deep test : NOT EXECUTABLE** dans la session courante. Le blocker est entièrement infrastructurel (bridge UnityMCP), pas gameplay. **Aucune information sur la parité V6 runtime n'a pu être collectée**.

**Status sprint R6-PARITY-V4 "95% complete"** : ne peut **ni être confirmé ni infirmé** par cette session. Le code source montre la surface API en place pour tous les 7 tests mais le runtime reste boîte noire tant que MCPForUnity ne réspond pas.

**Recommandation finale** : refaire la session après restart Unity Editor. Estimé 25 min réelles de tests post-restart (les 7 tests + screenshots + analyse).
