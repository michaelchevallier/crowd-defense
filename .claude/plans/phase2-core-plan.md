# Phase 2 Core — Plan de migration

**Sprint** : MIGRATE Phase 2 (post-POC Phase 1 livré 2026-05-11)
**Auteur** : Opus orchestrator
**Date** : 2026-05-11
**Estimé total** : 3-4 semaines, 25-40 tickets `MIGRATE-CORE-XX`
**Cible fin Phase 2** : tout le contenu gameplay porté (12 towers, ~30 enemies, ~80 levels, 4 specs D1) + synergies + polish. À l'issue, jeu jouable end-to-end sur W1-1 à W10-8 en WebGL.

---

## 0. Préalable — état Phase 1 acquis

| Élément | État |
|---|---|
| 4 SO classes (`TowerType`, `EnemyType`, `WaveDef`, `LevelData`) | ✅ basique (stats core uniquement) |
| 5 SO instances (Archer + Basic/Runner/Brute + W1-1) | ✅ |
| Systems (`GridCoords`, `GridData`, `PathManager` BFS, `WaveManager`, `Economy`, `PlacementController`, `LevelRunner`, `MapRenderer`) | ✅ POC-grade |
| Entities (`Tower`, `Projectile`, `Enemy`, `Castle`) | ✅ POC-grade |
| HUD UI Toolkit (Gold/Wave/HP pills + GameOver/Victory) | ✅ |
| WebGL build + deploy gh-pages /v6/ | ✅ |
| `TOWER_DAMAGE_MUL=1.6` hardcoded dans `Tower.cs` ligne 10 | ⚠️ à remonter Phase 2 (MIGRATE-CORE-03) |
| `BalanceConfig` global SO | ❌ inexistant |
| Multi-portal / multi-castle | ❌ POC = single P × single C uniquement (cf `PathManager.cs:42-43`) |
| Behaviors towers (cluster/slow/buffAura/coinPull) | ❌ inexistants |
| Behaviors enemies (flyer/stealth/shieldHP) | ❌ inexistants |
| Synergies | ❌ inexistantes |

**SO `TowerType` actuel** = 13 champs core uniquement. Manque (à ajouter selon source Phaser `Tower.js:9-119`) : `behavior` enum, `clusterCount`, `cooldownMs`, `slowMul`, `slowDurationMs`, `buffMul`, `coinMul`, `pullSlow`, `flyerOnly`, `flyerDmgMul`, `canHitFlyers`, `parabolic`, `armorBreakOnHit`, `unlockWorld` (présent), `icon` (manque), `synergies[]`, `placementRule` (anti-double magnet D1-01), `signature` (D1-03), `branches{}` (D1-03).

**SO `EnemyType` actuel** = 8 champs core. Manque : `isFlyer`, `flyHeight`, `ignorePath`, `isStealth`, `stealthCycleMs`, `stealthOpacity`, `shieldHP`, `isBoss`, `isMidBoss`, `isApocalypseBoss`, `bossAuraColor`, `bossName`, `chargeMs/chargeCooldownMs/chargeMul` (brigand), `summonsMinions/summonCooldownMs/summonType` (warlord/dragon/cosmic/kraken), `aoeBlastMs/aoeBlastRadius/aoeBlastDamage` (corsair/dragon), `isFiery` (imp), `immuneToFlyerBonus` (dragon), `walkAnim`, `asset` (mesh ref).

---

## Section 1 — Priorisation et ordering

5 sprints internes, ordering basé sur **dépendances gameplay + complexité technique** :

### Sprint 2.A — Foundation (1-1.5 sem, ~8-10 tickets)

**Objectif** : étendre les 4 SO classes pour accueillir TOUTES les variantes Phaser, instancier les 12 towers + ~30 enemies en SO, créer `BalanceConfig` singleton, supporter multi-portal × multi-castle. Sans behavior runtime — juste data + plumbing.

**Critères de fin** :
- 12 `.asset` Towers présents dans `Assets/ScriptableObjects/Towers/` avec tous les champs Phaser portés.
- 30 `.asset` Enemies présents dans `Assets/ScriptableObjects/Enemies/`.
- 1 `BalanceConfig.asset` consommé par Tower / WaveManager / Economy.
- `PathManager` retourne `List<List<Vector3>>` (N paths) au lieu d'un seul.
- `LevelRunner` gère `List<Castle>` + `castleLossMode` (any/all).
- 5-10 `LevelData.asset` early (W1-1 à W2-5) générés depuis JSON Phaser via Editor script.
- W1-1 toujours jouable (régression zéro).

**Tickets** : MIGRATE-CORE-01 (Towers SO), -02 (Enemies SO), -03 (BalanceConfig), -04 (multi-path/castle), -05 (Level Importer Editor tool), -06 (W1 levels port).

### Sprint 2.B — Mechanics (1-1.5 sem, ~8-12 tickets)

**Objectif** : implémenter tous les behaviors `TowerType.behavior` + champs spéciaux `EnemyType`. Chaque tour/enemy se comporte correctement isolément. Pas encore de synergies cross-tower.

**Critères de fin** :
- Tower behaviors implémentés : `attack` (default, fonctionnel POC), `cluster` (Mine spawn 3 mines en cercle, cooldown 12s), `slow` (Fan/Frost AoE slow tick), `buffAura` (Portal range), `coinPull` (Magnet aura + pull-slow).
- Tower modifiers : `parabolic` (Cannon arc projectile), `pierce` (Ballista/Crossbow N enemies), `aoe` (Mage/Cannon/Mine zone dmg), `flyerOnly` (Skyguard ne cible que `isFlyer`), `canHitFlyers` (Mage/Ballista/Crossbow), `armorBreakOnHit` (Acid debuff stack).
- Enemy behaviors : `isFlyer` (ignore path, vol direct vers castle, `flyHeight`), `isStealth` (Assassin cycle opacity), `shieldHP` (Shielded barrier avant `hp`), `isBoss` (aura visuelle + bossName), `chargeMs` (Brigand boost speed), `summonsMinions` (Warlord/Dragon spawn periodiquement), `aoeBlastMs` (Corsair AoE damage tours adjacentes).
- Object pool basique introduit pour enemies + projectiles (cf risk perf §4).

**Tickets** : MIGRATE-CORE-07 (Tower behaviors cluster/slow/buffAura), -08 (coinPull magnet), -09 (parabolic + AoE + pierce), -10 (flyer targeting), -11 (Enemy isFlyer + isStealth), -12 (Enemy shieldHP), -13 (Boss skeleton + summons), -14 (Enemy AoE blast Corsair/Dragon), -15 (object pool enemies+projectiles).

### Sprint 2.C — Specs D1 (0.5-1 sem, ~6-8 tickets)

**Objectif** : implémenter les 4 décisions design D1 (économie, pacing, L3 hybride, castle HP scaling).

**Critères de fin** :
- **D1-01 économie** : `_upgradeCost` ×5 total (×1.5 L2, ×2.5 L3) — `Tower.UpgradeTo(level, branch?)`. Interest bank +5%/wave avec reset si leak. Magnet rework (range 5 / coinMul 1.3 / cost 130 / cap 1 ou 2 si `allowMultiMagnet`). Treasure tiles `*` (CELL grammar + +100¢ si intact). Boss reward = 0× (`isBoss` check).
- **D1-02 pacing** : auto-start OFF strict (suppression timer auto WaveManager.cs:99-119), bouton "Lancer la vague (N)" UI Toolkit + raccourci `N` + debounce 300ms, skip bonus +30¢/5s, streak +5%/wave cap +25%.
- **D1-03 L3 hybride** : 4 tours signature (archer/mage/ballista/cannon) avec 2 branches DPS/utility. Radial menu UI Toolkit 2 boutons côte à côte au L2→L3. Synergies +50% → +25%. Refund sell 80% (Q8 statu quo, ≠ spec 70% : Mike a arbitré Q8=B).
- **D1-04 castle HP** : `BalanceConfig.CastleHPFor(world, level) = 100 + 50 × √world × difficultyMul`. `WORLD_PRESSURE_TABLE` appliqué dans Enemy.Init (mobHpMul / mobSpeedMul / mobCountMul). No-regen W6+.

**Tickets** : MIGRATE-CORE-16 (économie + boss 0×), -17 (interest bank + treasure tiles), -18 (magnet rework + anti-double), -19 (pacing manual wave + skip bonus + streak), -20 (L3 hybride + radial menu), -21 (castle HP scaling + mob pressure + no-regen W6+).

### Sprint 2.D — Levels (0.5-1 sem, ~4-6 tickets)

**Objectif** : porter les 80 levels Phaser → SO `.asset` Unity.

**Critères de fin** :
- 80 `.asset` LevelData dans `Assets/ScriptableObjects/Levels/W1-1..W10-8`.
- Editor menu `Tools > CrowdDefense > Import Levels From JSON` (one-shot ou re-runnable).
- Audit : chaque level a `world`/`level` extraits du naming, `castleHP` calculé ou override JSON optionnel (Q12).
- Level select menu UI Toolkit (grid 10×8, lock par progression).
- Save/load progression via PlayerPrefs (Q openings §3.5).
- Smoke test : compléter W1-1 à W2-5 en jeu sans erreur console.

**Tickets** : MIGRATE-CORE-22 (Level Importer JSON→SO bulk), -23 (Level select menu), -24 (save/load progression), -25 (smoke test 10 levels random).

### Sprint 2.E — Synergies + Polish (0.5 sem, ~3-5 tickets)

**Objectif** : porter `Synergies.cs` (système cross-tower) + tooltips UI + cutscenes briefings.

**Critères de fin** :
- `Synergies.cs` port complet (Resolve tick → buffMul, pierceBonus, multiShotBonus, _coinPullSources, etc.).
- 11 synergies hardcoded portées (cf `SYNERGY_DESCRIPTIONS` Phaser).
- Tooltip UI Toolkit "synergies actives" au hover tower.
- Briefings text-only (cutscenes 3D = Phase 3).

**Tickets** : MIGRATE-CORE-26 (Synergies port), -27 (synergies UI tooltip), -28 (briefings text screens).

---

## Section 2 — Premiers tickets MIGRATE-CORE-01..06 (briefables Sonnet)

### MIGRATE-CORE-01 — Compléter 12 TOWER_TYPES SO instances

**Goal** : porter les valeurs canoniques des 13 TOWER_TYPES Phaser (note : 13 actuels Phaser inclut "acid", on reste à 12 si on fusionne ou 13 si on le garde séparé — recommandation **13** car acid a son `_armorBreakOnHit` distinctif).

**Bloqué par** : aucun (peut paralléliser MIGRATE-CORE-02).

**Source Phaser à porter** : `/Users/mike/Work/milan project/src-v3/entities/Tower.js` lignes 9-119 (TOWER_TYPES dict) + ligne 121 (TOWER_ORDER) + ligne 123 (LEVEL_SCALE).

**Fichiers Unity à créer/modifier** :
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/TowerType.cs` — étendre avec champs manquants (cf §0 liste). Ajouter enum `TowerBehavior { Attack, Cluster, Slow, BuffAura, CoinPull }`.
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Towers/Tank.asset`, `Mage.asset`, `Ballista.asset`, `Mine.asset`, `Cannon.asset`, `Fan.asset`, `Frost.asset`, `Crossbow.asset`, `Portal.asset`, `Magnet.asset`, `Skyguard.asset`, `Acid.asset` (12 nouveaux).
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Towers/Archer.asset` — re-import pour s'aligner sur nouveau schéma (champs additionnels par défaut).

**Choix techniques (Unity-spécifiques)** :
- `TowerType` reste un `ScriptableObject` (data asset, sérialisable Inspector).
- Champs nullables (`armorBreakOnHit`, `synergies[]`) : utiliser `[Serializable]` nested struct + bool `hasArmorBreak` flag pour éviter `null` en Inspector.
- `LEVEL_SCALE` (×0.75 / ×1.0 / ×1.30) → static array dans `BalanceConfig` (MIGRATE-CORE-03), pas par tower.
- Synergies : champ `List<SynergyDef>` sérialisable (struct avec `type` enum, `from` string, `effectKey` string, `effectValue` float, `range` float). Pas de complexité full Phaser ici, juste data.
- Création des `.asset` : via Editor script bulk (`Assets/Editor/TowerSeedTool.cs`) **OU** via UnityMCP `manage_scriptable_object` (recommandé, plus rapide).

**Commits prévus** :
1. `feat(data): étendre TowerType SO pour behaviors + synergies + L3`
2. `feat(data): seed 12 TOWER_TYPES SO assets depuis Phaser`
3. `chore(data): re-import Archer.asset sur nouveau schéma`

**Verification** :
- UnityMCP : `list_assets` sur `Assets/ScriptableObjects/Towers/` → 13 `.asset`.
- UnityMCP : `read_console` clean (no errors / no missing refs).
- Play mode W1-1 : sélectionner Archer toolbar, placer tour, vérifier shoots Basic comme avant.
- Inspector check : ouvrir `Mage.asset`, vérifier `aoe = 2.0`, `canHitFlyers = true`, `damage = 2.76`, `cost = 70` (valeurs Phaser brutes ; le hike D1-01 cost se fera en MIGRATE-CORE-16).

**Estimation** : 3 commits, 3-4 h Sonnet.

---

### MIGRATE-CORE-02 — Compléter ~30 ENEMY_TYPES SO instances

**Goal** : porter les ~30 ENEMY_TYPES Phaser. Couvre tous les mobs basiques + theme variants (desert/forest/submarin/cyber) + boss multi-phase.

**Bloqué par** : aucun (parallélisable avec -01).

**Source Phaser à porter** : `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` lignes 53-213 (ENEMY_TYPES dict). 30 entries actuelles (count exact à confirmer par scan).

**Fichiers Unity à créer/modifier** :
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/EnemyType.cs` — étendre avec champs : `walkAnim` (string), `assetKey` (string mesh ref), `isFlyer`, `flyHeight`, `ignorePath`, `isStealth`, `stealthCycleMs`, `stealthOpacity`, `shieldHP`, `isBoss`, `isMidBoss`, `isApocalypseBoss`, `isFiery`, `isCorsair`, `isBrigand`, `bossAuraColor`, `bossName`, `immuneToFlyerBonus`, `chargeMs`, `chargeCooldownMs`, `chargeMul`, `summonsMinions`, `summonCooldownMs`, `summonType` (EnemyType ref → cycle attention), `aoeBlastMs`, `aoeBlastRadius`, `aoeBlastDamage`, `shaderOverlay` (string : "jellyfish" / "hologram" / null).
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Enemies/*.asset` × ~27 nouveaux (skeleton_minion, shielded, midboss, boss, brigand_boss, assassin, warlord_boss, flyer, corsair_boss, imp, dragon_boss, apocalypse_boss, cosmic_boss, kraken_boss, wizard_king, ai_hub, desert_runner, forest_brute, submarin_runner, forest_bee, plaine_pigeon, cyber_basic, cyber_runner, cyber_flyer, cyber_brute + tout autre trouvé au scan).

**Choix techniques** :
- `summonType` : ref **EnemyType** pas string, pour ref-safety Unity (Editor warn si missing).
- `shaderOverlay` reste un string id (Phase 3 introduira un `MaterialController` qui mappera id → URP shader).
- `assetKey` reste string (mesh GLTF, sera mappé Phase 3 via `MeshRegistry` SO).
- `bossAuraColor` : champ `Color` natif Unity.
- Boss : pas de mesh fancy pour Phase 2, juste primitive scale `Vector3.one * 1.5-2.0` selon `Scale` field.

**Commits prévus** :
1. `feat(data): étendre EnemyType SO pour flyer/stealth/boss/summons/blast`
2. `feat(data): seed ~27 ENEMY_TYPES SO assets depuis Phaser`
3. `chore(data): re-import Basic/Runner/Brute sur nouveau schéma`

**Verification** :
- UnityMCP : `list_assets` Enemies → ~30 `.asset`.
- Inspector check : `Shielded.asset` a `shieldHP = 4`, `Flyer.asset` a `isFlyer = true` + `flyHeight = 2.5`.
- Play mode régression W1-1 : Basic/Runner/Brute spawn + reach castle correctement.

**Estimation** : 3 commits, 4-5 h Sonnet.

---

### MIGRATE-CORE-03 — Créer `BalanceConfig` SO singleton

**Goal** : centraliser constantes globales actuellement éparses (TOWER_DAMAGE_MUL 1.6 hardcoded `Tower.cs:10`, LEVEL_SCALE, SWARM_MUL, floor reward endless, magnet rules) dans un SO unique consommé par Tower / WaveManager / Economy / LevelRunner.

**Bloqué par** : aucun (parallélisable -01/-02).

**Source Phaser** :
- `/Users/mike/Work/milan project/src-v3/entities/Tower.js:123` : `LEVEL_SCALE = { 1: 0.75, 2: 1.0, 3: 1.30 }`.
- `/Users/mike/Work/milan project/src-v3/entities/Tower.js:125` : `TOWER_DAMAGE_MUL = 1.6`.
- `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js:90` : `SWARM_MUL = 1.4 × ...`.
- Q1-Q18 décisions :
  - Q1 : floor reward endless = 0.70, difficultyMul = 1.1^(world-10) après W10.
  - Q2 : treasure value range 50-150¢ (recommandation D1-01 = flat 100¢, **valeur à mettre dans BalanceConfig** : champ `TreasureValueMin = 50`, `TreasureValueMax = 150`, helper `RollTreasureValue()` random uniform).
  - Q3 : magnet cap 1 (level.allowMultiMagnet → 2 sur W7+).
  - Q5 : skip bonus +30¢ flat fenêtre 5s.
  - Q6 : streak reset fenêtre 5s seulement.
  - Q7 : debounce 300ms clic + N.
  - Q8 : refund pelle 80%.
  - Q11 : no-regen W6+ skip total.
  - Q14 : floor castleHP W1-1 = 200 HP (note : D1-04 dit 150, Q14 dit 200 — **conflit, à arbitrer Mike §3**).

**Fichiers Unity** :
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/BalanceConfig.cs` — nouveau SO singleton.
- `/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Balance/BalanceConfig.asset` — nouveau.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Entities/Tower.cs:10` — remplacer `private const float TOWER_DAMAGE_MUL = 1.6f;` par `BalanceConfig.Instance.TowerDamageMul`.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/WaveManager.cs` — consommer `BalanceConfig.SwarmMul` au lieu de hardcoded.
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/Economy.cs` — pas modifié encore (économie D1-01 en MIGRATE-CORE-16).

**Choix techniques** :
- `BalanceConfig` = SO singleton via `[CreateAssetMenu]` + résolution Awake (`Resources.Load<BalanceConfig>("BalanceConfig")` si dans `Resources/`) **OU** assigné en serialize ref dans une scene root GameObject `_GameBootstrap`.
- Recommandation : **placement `Resources/`** car singleton de constantes (équivalent `enum const`), lifecycle = jeu entier.
- Tests : exposer `CastleHPFor(world, level)` static helper qui lit BalanceConfig.
- Pas de runtime mutation : champs readonly post-init.

**Champs `BalanceConfig` (initiaux, ajustables Phase 2.C)** :
```csharp
[Header("Tower scaling")]
public float TowerDamageMul = 1.6f;
public float[] LevelScale = { 0.75f, 1.0f, 1.30f }; // L1/L2/L3

[Header("Wave economy")]
public float SwarmMul = 1.4f;
public float FloorRewardCampaign = 0.5f;
public float FloorRewardEndless = 0.7f;
public float WorldRewardDecay = 0.05f; // 5% par world (D1-01 §3.3)

[Header("Castle HP (D1-04)")]
public float CastleHPBase = 100f;
public float CastleHPSqrtMul = 50f;
public int FloorCastleHPW1 = 200; // Q14, **À CONFIRMER vs D1-04 = 150**
public int NoRegenWorldThreshold = 6; // Q11

[Header("Magnet (D1-01 Q3)")]
public int MagnetCapDefault = 1;
public int MagnetCapAllowMulti = 2;
public float MagnetRange = 5f;
public float MagnetCoinMul = 1.3f;
public int MagnetCost = 130;

[Header("Pacing (D1-02)")]
public int SkipBonusGold = 30;
public float SkipWindowSeconds = 5f;
public float StreakBonusPerWave = 0.05f;
public int StreakCap = 5; // 5×5% = 25%
public int InputDebounceMs = 300;

[Header("Upgrade (D1-03)")]
public float UpgradeMulL2 = 1.5f;
public float UpgradeMulL3 = 2.5f; // total ×5 (Mike Q8 ratio)
public float SellRefundRatio = 0.8f; // Q8

[Header("Treasure (D1-01 §3.6)")]
public int TreasureValueMin = 50;
public int TreasureValueMax = 150;

[Header("Interest bank (D1-01 §3.5)")]
public float BankInterestRate = 0.05f; // +5%/wave
```

**Commits prévus** :
1. `feat(data): BalanceConfig SO singleton avec constantes globales`
2. `refactor(towers): consommer BalanceConfig.TowerDamageMul`
3. `refactor(systems): WaveManager.SwarmMul depuis BalanceConfig`

**Verification** :
- UnityMCP `manage_scriptable_object` : créer `BalanceConfig.asset` dans `Resources/`.
- Play mode W1-1 : régression zéro (Tower damage = 1.38 × 1.6 = 2.208 sur Basic 3 HP → die en 2 tirs comme avant).
- Editor menu Inspector : ouvrir BalanceConfig, vérifier tous les champs édités sont visibles.
- `read_console` : zéro warning `NullReferenceException` BalanceConfig.

**Estimation** : 3 commits, 2-3 h Sonnet.

---

### MIGRATE-CORE-04 — Multi-portal × multi-castle + castleLossMode + override castleHP

**Goal** : étendre `PathManager` pour produire **N paths** (cross product portals × castles BFS). Étendre `LevelData` pour supporter `castleLossMode` (any/all) + override `castleHP` optionnel par level (Q12). `LevelRunner` orchestre `List<Castle>` + agrège castle HP.

**Bloqué par** : aucun (utile à -05/-06 mais pas blocker).

**Source Phaser** :
- `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js:110-161` : `loadCastles()` + `loadCastlesFromGrid()` (multi-castle).
- `/Users/mike/Work/milan project/src-v3/systems/MapPathfinder.js` : `buildPathsFromGrid()` cross-product portals × castles.
- D1-04 §4.5 : override `castleHP` optionnel JSON.
- Q12 : override castleHP JSON optionnel.

**Fichiers Unity à créer/modifier** :
- `Assets/Scripts/Data/LevelData.cs` :
  - Ajouter `[SerializeField] private bool overrideCastleHP = false;`
  - Ajouter `[SerializeField] private int castleHPOverride = 200;`
  - Ajouter `[SerializeField] private CastleLossMode lossMode = CastleLossMode.Any;` (enum any = perdre 1 castle = game over, all = perdre tous).
  - Ajouter `[SerializeField] private bool allowMultiMagnet = false;` (Q3 W7+).
  - Ajouter `[SerializeField] private int world = 1;` + `level = 1;` (pour `CastleHPFor` lookup).
- `Assets/Scripts/Systems/PathManager.cs` : refactor pour produire `List<List<Vector3>> Paths { get; }`. BFS pour chaque (portal, castle) tuple. Cache + dirty flag si LevelData change.
- `Assets/Scripts/Systems/LevelRunner.cs` : `List<Castle> Castles`, agrégateur `int TotalCastleHP`, listener `Castle.OnDied` → check `lossMode`.
- `Assets/Scripts/Entities/Castle.cs` : déjà singleton dans POC, refactor pour devenir instance (suppression `Castle.Instance`). Index `castleIdx` pour multi-castle.
- `Assets/Scripts/Systems/WaveManager.cs` : `SpawnEnemy` doit pick un **portal aléatoire** (round-robin ou random selon WaveDef config).
- `Assets/Scripts/Data/WaveDef.cs` : ajouter `[SerializeField] private int portalIdx = -1;` (-1 = round-robin auto).
- `Assets/Scripts/Entities/Enemy.cs` : `Init(EnemyType, int pathIdx, int laneOffset)` au lieu de single waypoint list.

**Choix techniques** :
- Multi-castle agrégateur : `TotalCastleHP = Castles.Sum(c => c.Hp)`. Game over si `lossMode == Any && Castles.Any(c => c.IsDead)` ou si `lossMode == All && Castles.All(c => c.IsDead)`.
- Path selection enemy : `pathIdx` à init = `(spawnIdx % paths.Count)` si round-robin.
- `Castle.Instance` singleton retiré : remplacer par `LevelRunner.Instance.GetCastle(idx)`. Régression risque sur HUD (qui lisait `Castle.Instance.Hp`) — mitigation : `LevelRunner.Instance.TotalCastleHP`.
- Override castleHP : si `level.overrideCastleHP` → `castleHPOverride`, sinon `BalanceConfig.CastleHPFor(world, level)`.

**Commits prévus** :
1. `feat(data): LevelData multi-castle/portal config + override castleHP`
2. `feat(systems): PathManager génère N paths (portal × castle BFS)`
3. `refactor(systems): LevelRunner orchestre List<Castle> + lossMode`
4. `feat(entities): Enemy choisit pathIdx au spawn`
5. `refactor(ui): HudController lit TotalCastleHP au lieu de Castle.Instance.Hp`

**Verification** :
- UnityMCP : créer un test level `W2-3-debug.asset` avec 2 portals + 2 castles. PathManager doit produire 4 paths. Gizmo en Editor doit dessiner les 4.
- Play mode : ennemis spawn alterné sur 2 portals.
- W1-1 (1×1 portal/castle) : régression zéro.
- Game over test : sur W2-3-debug, faire mourir 1 castle → si `lossMode = Any` → game over immédiat. Si `All` → continue jusqu'à 2nd mort.

**Estimation** : 5 commits, 5-6 h Sonnet.

---

### MIGRATE-CORE-05 — Tower behaviors L1 (cluster, slow, buffAura, coinPull)

**Goal** : implémenter les 4 behaviors non-attack pour Mine / Fan&Frost / Portal / Magnet. Le ticket couvre la dispatch `Tower.Update()` selon `cfg.behavior` enum.

**Bloqué par** : MIGRATE-CORE-01 (TowerType.behavior enum + SO instances).

**Source Phaser** :
- Mine `cluster` : `Tower.js:42` (`clusterCount: 3, cooldownMs: 12000`) + logique spawn dans `Tower.js` méthode `_spawnCluster()`.
- Fan/Frost `slow` : `Tower.js:53-67` (`slowMul`, `slowDurationMs`).
- Portal `buffAura` : `Tower.js:84` (`buffMul: 1.5` aura range 5.5). Synergies.js l'applique.
- Magnet `coinPull` : `Tower.js:94` (`coinMul: 2.0`, range 6.5, pull-slow 0.7).

**Fichiers Unity à créer/modifier** :
- `Assets/Scripts/Entities/Tower.cs` : refactor `Update()` en switch sur `cfg.Behavior`.
- `Assets/Scripts/Entities/Mine.cs` (nouveau, hérite Tower ou behavior strategy) : spawn 3 mines en cercle au tick cooldown.
- `Assets/Scripts/Entities/MineExplosive.cs` (nouveau, MonoB) : entity statique qui détecte ennemis en range, explose au contact (AoE damage).
- `Assets/Scripts/Systems/SlowEffectManager.cs` (nouveau) : registre ennemis ralentis avec timer expiration. Appelé par Tower (behavior slow) en tick.
- `Assets/Scripts/Entities/Enemy.cs` : ajouter `currentSpeedMul` field + `ApplySlow(float mul, float durMs)`.
- `Assets/Scripts/Systems/BuffAuraManager.cs` (nouveau) ou logique dans `Tower.cs` : Portal en tick applique `_buffMul = 1.5` à toutes les tours dans range. **Note** : si on garde le pattern Synergies.js port pour Phase 2.E, alors buffAura sera consommée là — décision : faire MIGRATE-CORE-05 minimaliste (juste Portal applique buffMul aux towers in range, sans Synergies.cs port complet ici).
- `Assets/Scripts/Systems/CoinPullManager.cs` (nouveau) : registre des sources Magnet `(position, range, coinMul)`. Méthode `GetCoinMulAt(Vector3 pos) → float`. Consommé par `Enemy.OnDeath` pour boost reward.

**Choix techniques** :
- Pattern strategy via switch case `cfg.Behavior` plutôt qu'héritage `Mine : Tower` (évite explosion classes, garde une seule MonoB Tower).
- SlowEffectManager : `Dictionary<Enemy, (float untilTime, float mul)>`. Tick global expire les slows.
- Mine cooldown : `_spawnTimer` field dans Tower (réutilise existing cooldown), reset à `cfg.cooldownMs / 1000f` après spawn.
- AoE damage Mine : utilise OverlapSphere (Physics) ou loop manuel sur `WaveManager.ActiveEnemies` + distance check. **Recommandation** : loop manuel (pas de collider Setup en Phase 2, perf OK <50 mobs).

**Commits prévus** :
1. `feat(entities): Tower dispatch behaviors via switch case`
2. `feat(entities): Mine cluster spawn + MineExplosive AoE on contact`
3. `feat(systems): SlowEffectManager + Fan/Frost slow tick`
4. `feat(systems): BuffAura Portal applique buffMul aux towers in range`
5. `feat(systems): CoinPullManager + Magnet coinMul lookup au kill`

**Verification** :
- W1-1 modifié pour permettre poser Mine/Fan/Frost/Portal/Magnet (PlacementController toolbar à ajouter). **Note** : toolbar 12 towers = UI tâche séparée (peut-être MIGRATE-CORE-XX UI side).
- Play mode : poser Mine sur path, vérifier 3 mines apparaissent en cercle. Mob passe → mine explose → AoE dmg.
- Poser Frost adjacent à path, vérifier Basic ralenti (visuellement plus lent).
- Poser Magnet près Archer, vérifier reward kill = 2 × 1.3 = round(2.6) = 3 (avant : 2).

**Estimation** : 5 commits, 6-8 h Sonnet.

---

### MIGRATE-CORE-06 — Enemy behaviors (isFlyer, isStealth, shieldHP)

**Goal** : implémenter les 3 enemy behaviors les plus utilisés (couvre Flyer, Assassin, Shielded + dragon_boss multi-trait).

**Bloqué par** : MIGRATE-CORE-02 (EnemyType.* SO + champs étendus).

**Source Phaser** :
- `Enemy.js:109-113` flyer : `isFlyer: true, ignorePath: true, flyHeight: 2.5`. Mouvement direct vers castle, ignore BFS.
- `Enemy.js:96-100` assassin : `isStealth: true, stealthCycleMs: 2200, stealthOpacity: 0.25`. Cycle opacité régulier, peut "louper" hits si tower fire pendant phase basse opacité (mécanique précise dans `Enemy.js` à porter).
- `Enemy.js:74-78` shielded : `shieldHP: 4` (bouclier absorbe dégâts avant `hp`).
- `Enemy.js:126-133` dragon_boss : combo `isBoss + isFlyer + summonsMinions + aoeBlast + immuneToFlyerBonus`. (Boss summons en MIGRATE-CORE-13, blast en -14, immuneToFlyer ici).

**Fichiers Unity à créer/modifier** :
- `Assets/Scripts/Entities/Enemy.cs` :
  - Branche `Update()` : si `cfg.IsFlyer` → ignore waypoints, lerp direct vers `Castle.Instance.transform.position`, ajuste `transform.position.y = cfg.FlyHeight`.
  - Field `shieldHp` init `cfg.ShieldHP`. `TakeDamage(dmg)` : si `shieldHp > 0` → shieldHp -= dmg, sinon `hp -= dmg`.
  - Field `stealthOpacity` cycle : `Update()` lerp `MeshRenderer.material.color.a` selon sin(time / stealthCycleMs).
- `Assets/Scripts/Entities/Tower.cs` : `AcquireTarget` filtre `cfg.flyerOnly` → ne cible que `e.cfg.IsFlyer`. Bonus `cfg.flyerDmgMul` si `target.IsFlyer && !target.ImmuneToFlyerBonus`. Skip hit si target stealthOpacity active (Phaser logic à confirmer : soit miss probability, soit untargetable pendant phase).

**Choix techniques** :
- Stealth : Mike a-t-il choisi "miss probability" ou "untargetable in low-opacity phase" ? Le Phaser fait **untargetable** (cf `Tower.js:_canTarget()` à lire). Recommandation Phase 2 : **untargetable si `material.color.a < 0.4`**. Joueur voit visuellement le cycle, comprend pourquoi sa tour ne tire pas.
- Flyer movement : lerp direct + Y offset constant. Pas de A* nécessaire (vol = ligne droite).
- ImmuneToFlyerBonus : booléen lu dans `Tower.Fire` pour skip le ×1.5.
- ShieldHP : visual = halo doré scale 1.2 autour du mesh, désactivé quand shieldHp=0.

**Commits prévus** :
1. `feat(entities): Enemy isFlyer ignore path + fly height`
2. `feat(entities): Enemy shieldHP barrier avant hp`
3. `feat(entities): Enemy isStealth cycle opacity + untargetable`
4. `feat(entities): Tower flyerOnly filter + flyerDmgMul + immuneToFlyerBonus skip`

**Verification** :
- Test level avec 1 Flyer : vol direct vers castle, ignore path coudes.
- Test level avec 1 Shielded : Tank dmg 0.69 × 1.6 = 1.1 → 4 tirs pour casser shield (4 × 1.1 ≈ 4.4 → 4 tirs), puis 2 tirs pour HP 2 → total 6 tirs.
- Test level avec 1 Assassin : opacité oscille ; placer Archer adjacent, vérifier qu'il skip parfois (untargetable phase).
- Test Skyguard : place Skyguard, spawn Basic + Flyer ensemble → Skyguard ne tire que sur Flyer.

**Estimation** : 4 commits, 4-5 h Sonnet.

---

## Section 3 — Questions ouvertes à Mike (5)

### 3.1 Floor castleHP W1-1 : 150 ou 200 ?

Conflit interne décisions :
- **Q14** (interview 2026-05-11) : 200 HP au W1-1.
- **D1-04** §3.1 : formule `100 + 50 × √1 × 1.0 = 150` au W1-1.

Recommandation Opus : **garder Q14 = 200** (override floor min, Mike a explicitement répété "tutoriel permissif"). Implémentation : si `world == 1 && level == 1` → `Max(formule, 200)`. Sinon formule pure.

**Question Mike** : confirmer 200 vs 150 ?

### 3.2 Models 3D : import GLTF Phase 2 ou Phase 3 ?

Source Phaser : utilise GLTF Kenney/KayKit (`/Users/mike/Work/milan project/public/assets/`). Probablement ~50-100 MB d'assets.
- Option A : importer dès Phase 2 (réalisme visuel, le jeu "ressemble" tôt). Coût : 10-20h Sonnet (loader + variants + collisions). Risque : Phase 2.A se transforme en Phase asset-import.
- Option B : rester sur primitives Phase 2 (capsule/cube colorée par `bodyColor`), import GLTF en Phase 3 polish. Coût Phase 2 économisé.

Recommandation Opus : **Option B**. Phase 2 = ramener la logique gameplay, pas le visuel. Primitives suffisent pour valider mécaniques. Phase 3 dédiée à l'import + URP shaders + ToonMaterial port.

**Question Mike** : OK pour primitives Phase 2 + import GLTF Phase 3 ?

### 3.3 Audio : intégrer Phase 2 ou Phase 3 ?

Source Phaser : `src-v3/systems/Audio.js` + assets `.mp3/.ogg`. ~30 SFX + 5-10 musiques.
- Option A : intégrer minimal Phase 2 (3 SFX : tower fire, enemy die, castle hit). Permet QA "feel" plus tôt.
- Option B : zéro audio Phase 2, dédier Phase 3 audio sweep complet.

Recommandation Opus : **Option B**. Audio est un polish multi-iter (mix, ducking, fade). Phase 2 silencieuse OK pour validation gameplay.

**Question Mike** : zéro audio Phase 2 confirmé ?

### 3.4 Save system : PlayerPrefs, JSON file, ou ScriptableObject runtime ?

Pour persister progression level (W1-1 cleared, W1-2 unlocked, etc.) + settings (volume, lang) :
- **PlayerPrefs** : simple, atomic, registry Win/UserDefaults Mac/IndexedDB WebGL. Limite : strings/ints/floats only, pas JSON natif. Workaround : sérialiser un struct en JSON et stocker comme 1 string.
- **JSON file** (`Application.persistentDataPath/save.json`) : flexible, debuggable. Mais : WebGL `persistentDataPath` = IndexedDB virtuel, fonctionne mais async surprises.
- **ScriptableObject runtime mutation** : sale (CLAUDE.md warns : "modifications runtime de SO **persistent** dans l'Editor"). Pas recommandé pour save.

Recommandation Opus : **PlayerPrefs + JSON string** (1 clé `cd_progression_v1`). Cross-platform sûr, ~50 lignes code.

**Question Mike** : OK PlayerPrefs + JSON ? Ou tu veux un fichier explicite pour debug-friendly ?

### 3.5 Multi-language : Phase 2 ou Phase 3 ?

Source Phaser : texte FR hardcoded partout (UI Toolkit USS, bossName, labels towers). Unity a `com.unity.localization` (LocalizationPackage).
- Option A : hardcode FR Phase 2, refactor i18n Phase 4 (avant Steam release).
- Option B : intégrer LocalizationPackage dès Phase 2 (clean, mais coût setup 4-6h + tous les strings à wrapper).

Recommandation Opus : **Option A**. Le repo cible Steam initialement FR (Mike francophone, soft launch France probable). EN/ES viendront en Phase 4 packaging. Hardcoded FR sécurisé.

**Question Mike** : confirmer FR hardcoded Phase 2 ?

---

## Section 4 — Risks raised

### R1 — Perf 50+ ennemis simultanés sans pool (HIGH)

État actuel : `WaveManager.SpawnEnemy` → `Instantiate(enemyPrefab, ...)`. `Enemy.OnReachedCastle` / `TakeDamage` → `Destroy(gameObject)`. STATUS.md ligne 64 : "Pas de pool POC : Instantiate/Destroy direct (<50 mobs simultanés)".

Phase 2 atteint 100-200 mobs simultanés (W6+ avec `mobCountMul = 1.30 × SWARM_MUL 1.4 × wave 90 mobs` peut atteindre 160+ pic). Spike GC sur Destroy = stutter visible WebGL (30 fps mobile).

**Mitigation** : MIGRATE-CORE-15 = object pool `EnemyPool` + `ProjectilePool`. Préallouer 200 enemy GO en pool inactif. Recycler via `gameObject.SetActive(false/true)`. Estimé : 4-6h Sonnet. **Prio HIGH dans Sprint 2.B**.

### R2 — Unity-MCP edge cases sur ops bulk (MEDIUM)

`manage_scriptable_object` × 30 enemies + 12 towers = 42 calls. Phase 1 a montré rate limit issues sur certains batchs. Mitigation : `batch_execute` pour grouper (CLAUDE.md mentionne 10-100× speedup). Sonnet doit utiliser batch dès MIGRATE-CORE-01/-02.

### R3 — WebGL build time growing (MEDIUM)

Phase 1 = 123s sur Mac M1. Phase 2 ajoute ~30 SO + ~80 levels + logic. Estimé : 180-240s. Acceptable mais à surveiller. **Pas de mitigation immédiate**, monitorer.

### R4 — Bouton "Lancer wave" overlap toolbar towers mobile (MEDIUM)

D1-02 §wireframe mobile : bouton 80×80 bottom-right, joystick 96×96 bottom-left, toolbar tower scroll horizontal bottom 8px hauteur 60px. Conflit spatial possible viewport portrait 320×568 (iPhone SE). À tester en QA Phase 2.C.

### R5 — Spec D1-03 mismatch Q8 vs spec (LOW, déjà détecté)

D1-03 §"décisions Mike" : refund 70%. Q8 arbitrage : refund 80%. **Q8 prévaut** (interview > brouillon spec). À noter dans MIGRATE-CORE-20.

### R6 — Multi-castle agrégateur HUD HP (LOW)

POC HUD lit `Castle.Instance.Hp` direct. Refactor multi-castle → HUD lit `LevelRunner.TotalCastleHP`. Risque : break UI Toolkit binding existant. Mitigation : MIGRATE-CORE-04 commit 5 dédié à ce refactor + smoke test.

### R7 — Synergies cross-frame dependency (MEDIUM, Phase 2.E)

Synergies.js Phaser fait tick global qui mute `t._buffMul`, `e._slowMul` chaque frame. Si on porte naïvement en Unity sans coordination ordering (`Tower.Update` avant `Synergies.Tick`), bugs subtils (buff appliqué en retard 1 frame). Mitigation : `Synergies` en `LateUpdate` ou central tick orchestré par `LevelRunner`.

### R8 — Level Importer JSON→SO regressions (MEDIUM)

MIGRATE-CORE-22 doit lire 80 fichiers `/Users/mike/Work/milan project/src-v3/data/levels/world*-*.js` (format JS module export). Parser custom requis (regex ou nodejs eval). Recommandation : node script externe qui produit JSON intermédiaire, Editor C# script lit JSON, produit SO. Sépare parsing JS et création SO.

### R9 — Boss enemies multi-trait complexity (HIGH, Sprint 2.B)

Apocalypse_boss / cosmic_boss / kraken_boss combinent 4-5 traits (`isBoss + summonsMinions + aoeBlast + shaderOverlay + isApocalypseBoss`). État machine implicite (phases boss) pas modélisée Phaser, juste timers indépendants. Mitigation : implémenter chaque trait isolément (MIGRATE-CORE-13 summons, -14 blast). Pas de "BossStateMachine" Phase 2 (overkill).

---

## Annexe — Récap timeline + budget

| Sprint | Tickets | Estimé temps Sonnet | Cumul |
|---|---|---|---|
| 2.A Foundation | -01 à -06 | 23-31 h | 23-31 h |
| 2.B Mechanics | -07 à -15 | 35-45 h | 58-76 h |
| 2.C Specs D1 | -16 à -21 | 25-35 h | 83-111 h |
| 2.D Levels | -22 à -25 | 12-18 h | 95-129 h |
| 2.E Synergies+Polish | -26 à -28 | 8-12 h | 103-141 h |
| **Total Phase 2** | **28 tickets** | **~100-140 h** | — |

À 6-8 h Sonnet productifs/jour × 3-4 parallèles : **~3-4 semaines calendrier** conforme estimé STATUS.md.

**Décisions points à valider Mike avant démarrage** : §3.1 (200 HP), §3.2 (primitives Phase 2), §3.3 (audio Phase 3), §3.4 (PlayerPrefs+JSON), §3.5 (FR hardcoded).

**Démarrage recommandé** : lancer MIGRATE-CORE-01 + -02 + -03 en parallèle (3 Sonnet feature-dev en worktree, zéro overlap fichiers). Critical path linéaire ensuite : -04 → -05 ∥ -06.
