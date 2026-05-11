# MIGRATE-POC-02 — ScriptableObject definitions + W1-1 instances

> Ticket 2/8 du Phase 1 POC. Définit les data SO + crée les 5 instances W1-1.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 2 commits, ~55 min
- **Bloqué par** : MIGRATE-POC-01 (scaffold) ✅ done
- **Branch** : `main` direct (pas worktree — cf POC-01 brief même raison Unity-MCP)
- **Working directory** : `/Users/mike/Work/crowd-defense/`

## Objectif

Créer 4 classes ScriptableObject + 1 struct sérialisable, puis créer 5 instances `.asset` pour le W1-1 :

- `TowerType.cs` (SO)
- `EnemyType.cs` (SO)
- `WaveDef.cs` (struct sérialisable + nested `EnemySpawnEntry`)
- `LevelData.cs` (SO)
- 1× `Archer.asset`, 3× enemies `Basic.asset` / `Runner.asset` / `Brute.asset`, 1× `W1-1.asset`

## Décisions techniques imposées

- **Convention C#** : namespace `CrowdDefense`, `nullable enable` au top de chaque fichier, `[SerializeField] private` pour fields Inspector.
- **`[CreateAssetMenu]`** sur chaque SO class (requis pour création via Unity menu Asset > Create).
- **Pas de méthode runtime** sur les SO data (data pure, immutable runtime). Wrap toute mutation runtime `#if UNITY_EDITOR` mais aucune attendue POC.
- **Color** : `Color` Unity (RGBA float 0-1), pas `Color32` ni hex string. Conversion hex → Color : `new Color(r/255f, g/255f, b/255f)`.
- **Cell types LevelData** : conservés en `string[] mapRows`. Le parsing en `char[,]` viendra dans POC-03 (`GridData.Parse`).

## Source canonique à porter

Lis intégralement AVANT de créer les fichiers :

1. `/Users/mike/Work/milan project/src-v3/entities/Tower.js` lignes 9-130 (TOWER_TYPES.archer + TOWER_DAMAGE_MUL ligne 125)
2. `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` lignes 53-75 (ENEMY_TYPES.basic, runner, brute)
3. `/Users/mike/Work/milan project/src-v3/data/levels/world1-1.js` (intégral, 30 lignes)
4. `/Users/mike/Work/milan project/src-v3/systems/MapGrid.js` lignes 7-22 (CELL enum, WALKABLE/BUILDABLE sets — référence pour le parsing POC-03)

---

## Commit 1 — `feat(data): add TowerType/EnemyType/WaveDef/LevelData ScriptableObject definitions`

### Fichier 1 : `Assets/Scripts/Data/TowerType.cs`

```csharp
#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "TowerType", menuName = "CrowdDefense/TowerType")]
    public class TowerType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private int unlockWorld = 1;
        [SerializeField] private int cost = 30;

        [Header("Combat")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private int fireRateMs = 700;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float aoe = 0f;
        [SerializeField] private int pierce = 0;

        [Header("Visual")]
        [SerializeField] private Color bodyColor = Color.blue;
        [SerializeField] private Color projectileColor = Color.yellow;
        [SerializeField] private float sizeMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public int UnlockWorld => unlockWorld;
        public int Cost => cost;
        public float Damage => damage;
        public float Range => range;
        public int FireRateMs => fireRateMs;
        public float ProjectileSpeed => projectileSpeed;
        public float Aoe => aoe;
        public int Pierce => pierce;
        public Color BodyColor => bodyColor;
        public Color ProjectileColor => projectileColor;
        public float SizeMultiplier => sizeMultiplier;
    }
}
```

### Fichier 2 : `Assets/Scripts/Data/EnemyType.cs`

```csharp
#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "EnemyType", menuName = "CrowdDefense/EnemyType")]
    public class EnemyType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";

        [Header("Stats")]
        [SerializeField] private float hp = 3f;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private int damage = 5;
        [SerializeField] private int reward = 2;

        [Header("Visual")]
        [SerializeField] private float scale = 0.55f;
        [SerializeField] private Color bodyColor = Color.red;

        public string Id => id;
        public string DisplayName => displayName;
        public float Hp => hp;
        public float Speed => speed;
        public int Damage => damage;
        public int Reward => reward;
        public float Scale => scale;
        public Color BodyColor => bodyColor;
    }
}
```

### Fichier 3 : `Assets/Scripts/Data/WaveDef.cs`

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyType type;
        public int count;
    }

    [Serializable]
    public struct WaveDef
    {
        public List<EnemySpawnEntry> entries;
        public int spawnRateMs;
        public int breakMs;
    }
}
```

### Fichier 4 : `Assets/Scripts/Data/LevelData.cs`

```csharp
#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "CrowdDefense/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private string theme = "plaine";

        [Header("Map")]
        [SerializeField] private string[] mapRows = new string[0];
        [SerializeField] private float cellSize = 1f;

        [Header("Economy")]
        [SerializeField] private int startCoins = 120;
        [SerializeField] private int castleHP = 120;

        [Header("Waves")]
        [SerializeField] private List<WaveDef> waves = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Theme => theme;
        public IReadOnlyList<string> MapRows => mapRows;
        public float CellSize => cellSize;
        public int StartCoins => startCoins;
        public int CastleHP => castleHP;
        public IReadOnlyList<WaveDef> Waves => waves;
    }
}
```

### Process commit 1

1. Crée les 4 fichiers via `Write` tool.
2. `mcp__UnityMCP__refresh_unity` pour force l'AssetDatabase à indexer.
3. `mcp__UnityMCP__read_console` → verify no compile errors. Si erreur → fix avant commit.
4. `mcp__UnityMCP__manage_editor get_state` → verify `isCompiling=false`.
5. `git add Assets/Scripts/Data/` + commit avec message `feat(data): add TowerType/EnemyType/WaveDef/LevelData ScriptableObject definitions`.

---

## Commit 2 — `feat(data): create W1-1 ScriptableObject instances (archer + 3 enemies + W1-1 level)`

Crée les 5 instances `.asset` via `mcp__UnityMCP__manage_scriptable_object` (action `create` ou équivalent — voir doc tool, fallback `create_asset` via Editor menu si besoin).

### Instance 1 : `Assets/ScriptableObjects/Towers/Archer.asset`

Type : `TowerType`. Fields (source `Tower.js` ligne 10-16) :

| Field | Value | Source Phaser |
|---|---|---|
| `id` | `"archer"` | clé TOWER_TYPES |
| `displayName` | `"Archer"` | `label` |
| `unlockWorld` | `1` | `unlockWorld` |
| `cost` | `30` | `cost` |
| `damage` | `1.38` | `damage` (multiplier 1.6 appliqué runtime côté Tower.cs POC-05, pas ici) |
| `range` | `8` | `range` |
| `fireRateMs` | `700` | `fireRateMs` |
| `projectileSpeed` | `22` | `projSpeed` |
| `aoe` | `0` | `aoe` |
| `pierce` | `0` | `pierce` |
| `bodyColor` | RGB(58/255, 106/255, 191/255, 1) = `new Color(0.2275f, 0.4157f, 0.7490f, 1f)` | `fallbackColor 0x3a6abf` |
| `projectileColor` | RGB(255/255, 210/255, 63/255, 1) = `new Color(1f, 0.8235f, 0.2471f, 1f)` | `projColor 0xffd23f` |
| `sizeMultiplier` | `1.0` | `sizeMultiplier` |

### Instance 2 : `Assets/ScriptableObjects/Enemies/Basic.asset`

Type : `EnemyType`. Fields (source `Enemy.js` ligne 54-58) :

| Field | Value |
|---|---|
| `id` | `"basic"` |
| `displayName` | `"Basic"` |
| `hp` | `3` |
| `speed` | `1.2` |
| `damage` | `5` |
| `reward` | `2` |
| `scale` | `0.55` |
| `bodyColor` | RGB(198/255, 58/255, 16/255) = `new Color(0.7765f, 0.2275f, 0.0627f, 1f)` (rouge brun) |

### Instance 3 : `Assets/ScriptableObjects/Enemies/Runner.asset`

Source `Enemy.js` ligne 64-68 :

| Field | Value |
|---|---|
| `id` | `"runner"` |
| `displayName` | `"Runner"` |
| `hp` | `1` |
| `speed` | `2.4` |
| `damage` | `4` |
| `reward` | `2` |
| `scale` | `0.45` |
| `bodyColor` | RGB(0, 232/255, 255/255) = `new Color(0f, 0.9098f, 1f, 1f)` (cyan) |

### Instance 4 : `Assets/ScriptableObjects/Enemies/Brute.asset`

Source `Enemy.js` ligne 69-73 :

| Field | Value |
|---|---|
| `id` | `"brute"` |
| `displayName` | `"Brute"` |
| `hp` | `12` |
| `speed` | `0.8` |
| `damage` | `12` |
| `reward` | `8` |
| `scale` | `0.7` |
| `bodyColor` | RGB(138/255, 74/255, 34/255) = `new Color(0.5412f, 0.2902f, 0.1333f, 1f)` (marron) |

### Instance 5 : `Assets/ScriptableObjects/Levels/W1-1.asset`

Type : `LevelData`. Fields (source `world1-1.js` intégral) :

| Field | Value |
|---|---|
| `id` | `"world1-1"` |
| `displayName` | `"Plaine — 1"` |
| `theme` | `"plaine"` |
| `cellSize` | `4.0` (fidèle source, pas 1.0) |
| `startCoins` | `120` |
| `castleHP` | `120` (fidèle source — pas 200 ; on respecte le canonique) |
| `mapRows` | array de 7 strings, **fidèle au source** : |

```
"00000W0000000DL"
"P1111~111110D0L"
"00000W000010DDL"
"00000W0000100DL"
"00000W000010D0L"
"00000W000010DDL"
"0DD0DWD0D0C0DDL"
```

Note cell types présents (cf MapGrid.js) : `0` GRASS, `1` PATH, `W` WATER, `~` BRIDGE_WATER, `D` DECOR, `L` LAVA, `P` PORTAL, `C` CASTLE.

| `waves` | List<WaveDef> de **4 entries** (pas 3) : |

**Wave 0** : `entries = [{ type: Basic, count: 35 }]`, `spawnRateMs = 900`, `breakMs = 4500`.

**Wave 1** : `entries = [{ type: Basic, count: 62 }, { type: Runner, count: 14 }]`, `spawnRateMs = 650`, `breakMs = 4500`.

**Wave 2** : `entries = [{ type: Basic, count: 62 }, { type: Runner, count: 21 }, { type: Brute, count: 4 }]`, `spawnRateMs = 600`, `breakMs = 4500`.

**Wave 3** : `entries = [{ type: Basic, count: 55 }, { type: Runner, count: 28 }, { type: Brute, count: 7 }]`, `spawnRateMs = 550`, `breakMs = 0`.

Total enemies = 35 + 76 + 87 + 90 = **288**.

### Process commit 2

1. Pour chaque SO instance, utilise `mcp__UnityMCP__manage_scriptable_object` (charge tool via ToolSearch d'abord).
   - Crée d'abord les 3 enemy types (Basic, Runner, Brute) avant le LevelData (le LevelData référence les EnemyType dans ses WaveDef.entries[].type).
   - Puis Archer.
   - Puis W1-1 (qui référence les 3 enemy types).
2. Si `manage_scriptable_object` ne supporte pas la création directe (varie selon version Unity-MCP), fallback via :
   - `mcp__UnityMCP__execute_menu_item menu_path="Assets/Create/CrowdDefense/EnemyType"` puis renommer le file et populater fields via `manage_scriptable_object set`.
   - OU via `mcp__UnityMCP__execute_code` qui exécute du C# Editor-side avec `AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnemyType>(), "Assets/ScriptableObjects/Enemies/Basic.asset")` + SerializedObject.FindProperty + ApplyModifiedProperties.
3. **Pour les refs croisées dans W1-1.waves[].entries[].type** : utiliser le path d'asset string ou ObjectField via SerializedObject (Editor API). Si bloqué → fallback : créer W1-1 minimal (sans waves) + ouvre la scène en Editor pour que Mike (humain) drag-drop les enemy refs. Préviens-moi si tu es bloqué.
4. `mcp__UnityMCP__read_console` → no errors.
5. `mcp__UnityMCP__refresh_unity` pour finaliser AssetDatabase.
6. `git add Assets/ScriptableObjects/` + commit avec message `feat(data): create W1-1 ScriptableObject instances (archer + 3 enemies + W1-1 level)`.
7. `git push origin main`.

---

## Verification finale

```bash
find Assets/Scripts/Data -name "*.cs" | wc -l           # 4 (TowerType, EnemyType, WaveDef, LevelData)
find Assets/ScriptableObjects -name "*.asset" | wc -l    # 5 (Archer, Basic, Runner, Brute, W1-1)
git log --oneline -3                                    # 2 nouveaux commits
```

Via MCP :
```
mcp__UnityMCP__manage_editor get_state           # isCompiling=false
mcp__UnityMCP__read_console                      # no errors
mcp__UnityMCP__manage_scriptable_object get path="Assets/ScriptableObjects/Towers/Archer.asset"
  # → vérif fields populés (damage=1.38, range=8, cost=30, etc.)
mcp__UnityMCP__manage_scriptable_object get path="Assets/ScriptableObjects/Levels/W1-1.asset"
  # → vérif waves.Count=4, mapRows.Length=7, castleHP=120
```

**Critères succès** :
- 4 .cs files dans `Assets/Scripts/Data/` compilent sans erreur.
- 5 .asset files dans `Assets/ScriptableObjects/` créés et fields populés.
- W1-1 référence correctement les 3 EnemyType assets dans ses waves.
- 2 commits propres push vers `origin/main`.

## Pièges anticipés

1. **Refs croisées SO** : assigner `EnemyType` ref dans `WaveDef.entries[].type` via MCP peut être tricky. Plan B = `execute_code` Editor avec `AssetDatabase.LoadAssetAtPath<EnemyType>(path)` + `serializedObject.FindProperty("waves").GetArrayElementAtIndex(i).FindPropertyRelative("entries").GetArrayElementAtIndex(j).FindPropertyRelative("type").objectReferenceValue = enemyTypeAsset;`
2. **Color encoding** : Unity sérialise `Color` en RGBA float 0-1. Pas RGB int. Si tu vois `(1, 0, 0, 1)` à la place du rouge brun, vérifie la conversion `r/255f`.
3. **CreateAssetMenu menuName** : doit être exact, sinon le menu Asset > Create > CrowdDefense ne s'affiche pas. Format : `"CrowdDefense/TowerType"`.
4. **TOWER_DAMAGE_MUL 1.6** : NE PAS le multiplier dans le SO. Le damage SO = valeur canonique (1.38). Le multiplier sera appliqué dans `Tower.cs` au moment du Fire en POC-05. Hardcode pour POC, BalanceConfig SO en Phase 2.
5. **Cell types non-walkable** : `W` (water) et `L` (lava) sont des barrières naturelles. Mais `~` (bridge over water) et `^` (bridge over lava) sont walkable. C'est pour POC-03 (BFS), pas ton ticket — mais lis MapGrid.js pour bien encoder mapRows.
6. **GITHUB_TOKEN env var** : si `git push` échoue 401 → `unset GITHUB_TOKEN && git push origin main`.

## Quand tu as fini

Push les 2 commits sur `origin/main`, termine ton tour, je prends le relais pour POC-03. Si blocker (notamment refs croisées SO) → arrête, explique précisément quoi a foiré.
