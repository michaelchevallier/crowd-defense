# MIGRATE-POC-06 — Economy + Castle + LevelRunner + Time.timeScale cheat

> Ticket 6/8 du Phase 1 POC. Wiring économique + game over/victory + dev cheat accéléré.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 3 commits, ~50 min
- **Bloqué par** : POC-01..05 ✅
- **Branch** : `main` direct
- **Working dir** : `/Users/mike/Work/crowd-defense/`

## Objectif

3 composants singletons + branchement events :
1. `Economy` (Gold, TrySpend, AddGold, event OnGoldChanged)
2. `Castle` (HP/HPMax, TakeDamage, events OnHPChanged + OnDestroyed) — MonoB attaché à un GameObject visuel cube près du castle cell
3. `LevelRunner` (GameState Play/GameOver/Victory, Time.timeScale pause, dev cheat Tab toggle 1x↔10x)
4. **Patch** : modifier `Enemy.cs` pour appeler `Economy.AddGold(reward)` + `Castle.TakeDamage(damage)` au lieu des log stubs (TODO de POC-04).
5. **Patch** : modifier `PlacementController.cs` pour appeler `Economy.TrySpend(cost)` au lieu du stub.

## Source canonique

Lire AVANT :
1. `/Users/mike/Work/milan project/src-v3/entities/Castle.js` lignes 215-233 (`takeDamage` + emit destroyed event). Skip VFX/audio/UI HP bar (Phase 2).
2. `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` lignes 27-80 (constructor + castleHP getters + state) + ligne 459 (`setSpeed` pattern).

## Décisions techniques

- **Singleton pattern** : `static Instance` + Awake check (cf PathManager).
- **Script Execution Order** : LevelRunner = -100 (s'awake en premier, avant Economy/Castle/WaveManager/PathManager qui le référencent). À set via `mcp__UnityMCP__manage_editor` ou Edit > Project Settings > Script Execution Order > add CrowdDefense.Systems.LevelRunner = -100.
- **Time.timeScale pause** : `Time.timeScale = (state == Play) ? targetSpeed : 0f;` où `targetSpeed = 1f` normal ou `10f` si cheat.
- **Dev cheat Tab key** : dans `LevelRunner.Update`, `if (Input.GetKeyDown(KeyCode.Tab)) targetSpeed = (targetSpeed == 1f) ? 10f : 1f;`. Log change.
- **Castle GameObject** : Cube primitive à la position du castle cell (cf PathManager.Grid.Castles[0]). Scale 2. Color marron foncé. Pas de mesh complexe POC.

---

## Commit 1 — `feat(systems): add Economy + LevelRunner orchestrator with state machine + dev speed cheat`

### Fichier : `Assets/Scripts/Systems/Economy.cs`

```csharp
#nullable enable
using System;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class Economy : MonoBehaviour
    {
        public static Economy? Instance { get; private set; }

        public int Gold { get; private set; }
        public event Action<int>? OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (LevelRunner.Instance?.CurrentLevel != null)
                SetGold(LevelRunner.Instance.CurrentLevel.StartCoins);
            else
                SetGold(120);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) return false;
            if (Gold < amount) return false;
            SetGold(Gold - amount);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            SetGold(Gold + amount);
        }

        private void SetGold(int v)
        {
            Gold = v;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
```

### Fichier : `Assets/Scripts/Systems/LevelRunner.cs`

```csharp
#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public enum GameState { Play, GameOver, Victory }

    public class LevelRunner : MonoBehaviour
    {
        public static LevelRunner? Instance { get; private set; }

        [SerializeField] private LevelData? currentLevel;

        public GameState State { get; private set; } = GameState.Play;
        public LevelData? CurrentLevel => currentLevel;
        public event Action<GameState>? OnStateChanged;

        private float targetSpeed = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyTimeScale();
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted += OnVictory;
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= OnVictory;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                targetSpeed = Mathf.Approximately(targetSpeed, 1f) ? 10f : 1f;
                ApplyTimeScale();
#if UNITY_EDITOR
                Debug.Log($"[LevelRunner] speed cheat → {targetSpeed}x");
#endif
            }
        }

        public void SetState(GameState s)
        {
            if (State == s) return;
            State = s;
            ApplyTimeScale();
            OnStateChanged?.Invoke(State);
#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] state → {s}");
#endif
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = State == GameState.Play ? targetSpeed : 0f;
        }

        private void OnVictory() => SetState(GameState.Victory);
    }
}
```

### Scene setup (commit 1)

- Créer GameObjects "LevelRunner" + "Economy" sous Systems.
- Assigner LevelRunner.currentLevel = W1-1.asset.
- Set Script Execution Order : `Edit > Project Settings > Script Execution Order` ajouter `CrowdDefense.Systems.LevelRunner = -100`. Via MCP : `mcp__UnityMCP__execute_code` :
  ```csharp
  var order = MonoImporter.GetExecutionOrder(MonoImporter ... actually use:
  var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Systems/LevelRunner.cs");
  MonoImporter.SetExecutionOrder(script, -100);
  ```

### Process commit 1

1. Write Economy.cs + LevelRunner.cs.
2. Setup scene GameObjects + Script Execution Order.
3. Save scene.
4. Compile check, no errors.
5. `git add Assets/Scripts/Systems/Economy.cs Assets/Scripts/Systems/LevelRunner.cs Assets/Scenes/ ProjectSettings/` + commit.

---

## Commit 2 — `feat(entities): add Castle singleton with HP + visual cube + Enemy/PlacementController wiring`

### Fichier : `Assets/Scripts/Entities/Castle.cs`

```csharp
#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Castle : MonoBehaviour
    {
        public static Castle? Instance { get; private set; }

        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;

        public event Action<int, int>? OnHPChanged;
        public event Action? OnDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            int max = LevelRunner.Instance?.CurrentLevel?.CastleHP ?? 120;
            HP = HPMax = max;
            OnHPChanged?.Invoke(HP, HPMax);

            // Position castle GameObject at PathManager castle cell
            if (PathManager.Instance?.Grid != null && PathManager.Instance.Grid.Castles.Count > 0)
            {
                var grid = PathManager.Instance.Grid;
                var castleCell = grid.Castles[0];
                transform.position = GridCoords.CellToWorld(castleCell.x, castleCell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
            }
        }

        public void TakeDamage(int dmg)
        {
            if (IsDead || dmg <= 0) return;
            HP = Mathf.Max(0, HP - dmg);
            OnHPChanged?.Invoke(HP, HPMax);
            if (HP == 0)
            {
                OnDestroyed?.Invoke();
                LevelRunner.Instance?.SetState(GameState.GameOver);
#if UNITY_EDITOR
                Debug.Log("[Castle] destroyed → GameOver");
#endif
            }
        }
    }
}
```

### Patch Enemy.cs (remplacer stubs)

Dans `Assets/Scripts/Entities/Enemy.cs` :

**TakeDamage** : remplacer la ligne TODO par :
```csharp
Economy.Instance?.AddGold(reward);
```

**OnReachedCastle** : remplacer la ligne TODO par :
```csharp
Castle.Instance?.TakeDamage(dmg);
```

Add `using CrowdDefense.Systems;` au top du fichier si pas déjà présent.

### Patch PlacementController.cs (remplacer stub)

Dans `Assets/Scripts/Systems/PlacementController.cs`, dans `Update` au moment du cost check :

Remplacer :
```csharp
// TODO POC-06 : if (!Economy.Instance.TrySpend(cost)) { Debug.Log("[Place] not enough gold"); return; }
#if UNITY_EDITOR
Debug.Log($"[Place] cost={cost} gold (stub, free) at cell ({cell.x},{cell.y})");
#endif
```

Par :
```csharp
if (Economy.Instance == null || !Economy.Instance.TrySpend(cost))
{
#if UNITY_EDITOR
    Debug.Log($"[Place] reject : not enough gold ({Economy.Instance?.Gold ?? 0} < {cost})");
#endif
    return;
}
#if UNITY_EDITOR
Debug.Log($"[Place] cost={cost} gold, remaining={Economy.Instance.Gold}");
#endif
```

### Scene setup (commit 2)

- Créer GameObject "Castle" hors Systems (visuel) :
  - Cube primitive scale (2, 2, 2)
  - Material color marron foncé (e.g. `new Color(0.4f, 0.25f, 0.1f, 1f)`)
  - Component Castle.cs attached
- Position sera réajustée par Castle.Start (lit PathManager.Grid.Castles[0]).
- Note : laisser la Position Inspector à (0, 0, 0) — Start override.

### Process commit 2

1. Write Castle.cs.
2. Edit Enemy.cs + PlacementController.cs (remplacer stubs).
3. `refresh_unity` + compile check.
4. Scene setup Castle GO + Cube visual + component.
5. Save scene.
6. Test play mode complet (1 wave minimum) avec Time.timeScale = 10x (Tab key) :
   - Gold change visible via `Economy.Instance.Gold` debug.
   - Castle HP descend si pas de tower.
   - Si HP atteint 0 → state = GameOver + Time.timeScale=0.
7. `git add Assets/Scripts/Entities/Castle.cs Assets/Scripts/Entities/Enemy.cs Assets/Scripts/Systems/PlacementController.cs Assets/Scenes/` + commit.

---

## Commit 3 — `chore: verify all scene refs + Script Execution Order`

Validation sanity :
- LevelRunner.currentLevel = W1-1.asset ✓
- PathManager.levelData = W1-1.asset ✓
- WaveManager.levelData = W1-1.asset, enemyPrefab = Enemy.prefab ✓
- PlacementController.selectedTowerType = Archer.asset, towerPrefab = Tower.prefab, projectilePrefab = Projectile.prefab ✓
- Tous les singletons (LevelRunner, Economy, PathManager, WaveManager, PlacementController, Castle) ont leur GameObject parent dans Systems (sauf Castle = visual root).
- Script Execution Order : LevelRunner = -100.

Si tout est OK depuis commit 1+2, skip ce commit (juste push). Sinon, fix + commit.

### Process commit 3 (optionnel)

`git push origin main` (`unset GITHUB_TOKEN && git push` si 401).

---

## Verification finale

```bash
find Assets/Scripts -name "*.cs" | wc -l   # ~10 (4 Data, 4 Systems, 3 Entities)
git log --oneline -5
```

Via MCP play mode (test scenarios) :
1. **Scenario victory** : Placer 1 archer à cell (3, 1) au début. Press Tab → 10x speed. Attendre ~3 min réel (≈ 30 min de jeu accéléré). Toutes les 4 waves clear → state = Victory → log `[LevelRunner] state → Victory` + Time.timeScale = 0.
2. **Scenario GameOver** : Ne placer aucune tower. Press Tab → 10x. Attendre wave 1 → enemies arrivent au castle → HP descend de 120 → 0 → state = GameOver.
3. **Scenario Economy** : Vérifier Gold = 120 au start. Placer archer (cost 30) → Gold = 90. Kill un basic enemy → Gold +2 → 92.

**Critères succès** :
- 3 .cs files (Economy, Castle, LevelRunner) compilent
- Enemy + PlacementController patches branchés
- Tab key fonctionne, log speed change
- Game over / Victory triggers corrects
- Time.timeScale=0 sur état non-Play
- 2-3 commits pushed

## Pièges anticipés

1. **Time.timeScale=0 bloque Update** : les Update tournent quand même mais Time.deltaTime=0. OK pour gameplay (tout stoppe). Les events UI (cliquer Restart bouton POC-07) doivent utiliser `Time.unscaledDeltaTime` si timing nécessaire.
2. **Script Execution Order ne se propage pas via git** : settings sont dans `ProjectSettings/EditorBuildSettings.asset` ou un autre asset. Commit ProjectSettings/ inclut. Verify post-push qu'un fresh clone reproduit l'order.
3. **Order Awake** : LevelRunner Awake doit tourner avant Castle/Economy/WaveManager.Start. Avec -100 sur LevelRunner, c'est garanti.
4. **Castle.Start ré-position transform** : si Mike a déjà drag-drop un Castle GO à une position visuelle, le Start va l'écraser. Acceptable POC. Phase 2 : flag `[SerializeField] bool autoPositionFromGrid = true;`.
5. **Tab key conflict** : Tab a usage navigatif Unity Editor. En Play mode focus Game view, OK. Sinon try KeyCode.F.
6. **`MonoImporter.SetExecutionOrder`** : nécessite asset import refresh après. `AssetDatabase.Refresh()` post-call.

## Quand fini

2-3 commits push, termine.
