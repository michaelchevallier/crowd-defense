# MIGRATE-POC-05 — Tower + Projectile 3D + Raycast placement

> Ticket 5/8 du Phase 1 POC. Place archer sur cell GRASS, tower acquiert + tire, projectile homing tue enemies.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 3 commits, ~95 min (le plus gros POC)
- **Bloqué par** : POC-01..04 ✅
- **Branch** : `main` direct
- **Working dir** : `/Users/mike/Work/crowd-defense/`

## Objectif

3 composants + 1 controller + 2 prefabs :
1. `Tower` MonoBehaviour 3D (Cube + Cylinder primitives, "first" targeting via `Enemy.CurrentWaypoint`, fire cooldown, instanciation Projectile)
2. `Projectile` MonoBehaviour 3D (Sphere primitive, homing target, damage on hit)
3. `PlacementController` MonoBehaviour (raycast écran→plane Y=0, validate cell GRASS + gold, spawn Tower)
4. `Tower.prefab` + `Projectile.prefab`

**Note** : `Economy.TrySpend` n'existe pas (POC-06). En attendant, `PlacementController` log `[Place] cost=X gold (stub)` et place gratuitement. POC-06 remplacera.

## Source canonique

Lire AVANT :
1. `/Users/mike/Work/milan project/src-v3/entities/Tower.js` lignes 125-208 (construction class) + acquireTarget + fire logic (vers ligne 400-500, ToolSearch via Bash si besoin).
2. `/Users/mike/Work/milan project/src-v3/entities/BuildPoint.js` (validation placement, courte).

## Décisions techniques

- **Targeting "first"** : enemy avec `CurrentWaypoint` max dans la range (= le plus avancé vers castle). Pattern Kingdom Rush / BTD6.
- **Range check** : `(enemy.position - tower.position).sqrMagnitude < cfg.Range * cfg.Range` (skip sqrt).
- **Camera ref cached** : `private Camera cam;` dans `Awake()` de `PlacementController` (pas `Camera.main` en Update).
- **Raycast plane Y=0** : `Plane ground = new Plane(Vector3.up, Vector3.zero); ground.Raycast(ray, out float dist); Vector3 hitPos = ray.GetPoint(dist);`
- **Projectile homing** : `transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime)`. Si target null/dead → Destroy.
- **TOWER_DAMAGE_MUL = 1.6f** hardcoded const dans Tower.cs. Applied au moment du Fire : `target.TakeDamage(cfg.Damage * TOWER_DAMAGE_MUL);`.
- **PlacementController** : POC = un seul towerType (archer) hardcoded via `[SerializeField] TowerType selectedTowerType;`. Pas de menu pick.

---

## Commit 1 — `feat(entities): add Tower + Projectile with first-target acquisition + homing`

### Fichier : `Assets/Scripts/Entities/Tower.cs`

```csharp
#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Tower : MonoBehaviour
    {
        private const float TOWER_DAMAGE_MUL = 1.6f;

        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;

        public TowerType? Config => cfg;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;

            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = type.BodyColor;
            }

            transform.localScale = Vector3.one * type.SizeMultiplier;
        }

        private void Update()
        {
            if (cfg == null) return;
            cooldown -= Time.deltaTime;

            if (target == null || target.IsDead || OutOfRange(target))
                target = AcquireTarget();

            if (target != null && cooldown <= 0f)
            {
                Fire(target);
                cooldown = cfg.FireRateMs / 1000f;
            }
        }

        private bool OutOfRange(Enemy e)
        {
            if (cfg == null || e == null) return true;
            return (e.transform.position - transform.position).sqrMagnitude > cfg.Range * cfg.Range;
        }

        private Enemy? AcquireTarget()
        {
            if (cfg == null || WaveManager.Instance == null) return null;
            float rangeSq = cfg.Range * cfg.Range;
            Enemy? best = null;
            int bestWp = -1;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                if (e.CurrentWaypoint > bestWp)
                {
                    bestWp = e.CurrentWaypoint;
                    best = e;
                }
            }
            return best;
        }

        private void Fire(Enemy t)
        {
            if (projectilePrefab == null || cfg == null) return;
            var go = Instantiate(projectilePrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Init(t, cfg.Damage * TOWER_DAMAGE_MUL, cfg.ProjectileSpeed, cfg.ProjectileColor);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 0.9f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, cfg.Range);
        }
#endif
    }
}
```

### Fichier : `Assets/Scripts/Entities/Projectile.cs`

```csharp
#nullable enable
using UnityEngine;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Projectile : MonoBehaviour
    {
        private Enemy? target;
        private float damage;
        private float speed;
        private float lifetimeSec = 3f;

        public void Init(Enemy target, float damage, float speed, Color color)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            var rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = color;
                rend.material.SetFloat("_Smoothness", 0.9f);
            }
        }

        private void Update()
        {
            lifetimeSec -= Time.deltaTime;
            if (lifetimeSec <= 0f || target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPos = target.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude < 0.04f)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
```

### Prefabs

**`Assets/Prefabs/Towers/Tower.prefab`** :
- GameObject racine "Tower" (empty)
- Enfant "Base" : Cube primitive, scale (1, 0.5, 1), Y=0.25 (base au sol).
- Enfant "Top" : Cylinder primitive, scale (0.6, 0.6, 0.6), Y=0.85.
- Component Tower.cs sur le root.

**`Assets/Prefabs/Projectile.prefab`** :
- GameObject racine "Projectile"
- MeshFilter (Sphere primitive) + MeshRenderer
- Scale (0.2, 0.2, 0.2)
- Component Projectile.cs.

Création via `manage_gameobject` + `manage_prefabs` (cf POC-04 pattern Enemy prefab). Pour le Tower avec 2 enfants : créer parent empty, créer 2 GameObjects primitive children, attach script sur parent, save as prefab.

### Process commit 1

1. Write Tower.cs + Projectile.cs.
2. `refresh_unity` + verify compile.
3. Créer 2 prefabs via MCP.
4. Assign `projectilePrefab` ref dans Tower.prefab (drag-drop logique → execute_code SerializedObject fallback).
5. `git add Assets/Scripts/Entities/Tower.cs Assets/Scripts/Entities/Projectile.cs Assets/Prefabs/Towers/ Assets/Prefabs/Projectile.prefab*` + commit.

---

## Commit 2 — `feat(systems): add PlacementController with raycast click-to-place tower`

### Fichier : `Assets/Scripts/Systems/PlacementController.cs`

```csharp
#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private TowerType? selectedTowerType;
        [SerializeField] private GameObject? towerPrefab;
        [SerializeField] private GameObject? projectilePrefab;

        private Camera? cam;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null || !Input.GetMouseButtonDown(0)) return;
            if (selectedTowerType == null || towerPrefab == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Grid == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPos = ray.GetPoint(dist);

            var grid = PathManager.Instance.Grid;
            Vector2Int cell = GridCoords.WorldToCell(hitPos, grid.Width, grid.Height, grid.CellSize);
            if (!grid.IsBuildable(cell.x, cell.y))
            {
#if UNITY_EDITOR
                Debug.Log($"[Place] reject cell ({cell.x},{cell.y}) char='{grid.At(cell.x, cell.y)}' (not buildable)");
#endif
                return;
            }

            int cost = selectedTowerType.Cost;
            // TODO POC-06 : if (!Economy.Instance.TrySpend(cost)) { Debug.Log("[Place] not enough gold"); return; }
#if UNITY_EDITOR
            Debug.Log($"[Place] cost={cost} gold (stub, free) at cell ({cell.x},{cell.y})");
#endif

            Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
            var go = Instantiate(towerPrefab, cellWorld, Quaternion.identity);
            var tower = go.GetComponent<Tower>();
            if (tower != null)
                tower.Init(selectedTowerType, projectilePrefab);
        }
    }
}
```

### Scene setup

- Créer GameObject "PlacementController" sous Systems.
- Ajouter component PlacementController.
- Assign `selectedTowerType` = Archer.asset, `towerPrefab` = Tower.prefab, `projectilePrefab` = Projectile.prefab via SerializedObject.

### Process commit 2

1. Write PlacementController.cs.
2. `refresh_unity` + verify compile.
3. Setup PlacementController GO + refs.
4. Save scene.
5. Test play mode 60s : Click sur grass cell → log `[Place] cost=30 gold (stub, free)` → Tower visible → enemies in range → projectiles tirés → kills.
6. `git add Assets/Scripts/Systems/PlacementController.cs Assets/Scenes/` + commit.

---

## Commit 3 — `chore: link Tower.prefab projectile ref + verify scene refs`

Si commit 1 a laissé certaines refs non assignées (notamment projectilePrefab dans Tower.prefab → la valeur passée à Tower.Init via PlacementController est la source de vérité, donc Tower.prefab.projectilePrefab peut rester null), valide juste qu'on est cohérent :
- Tower.prefab : pas besoin de projectilePrefab assigné (vient via Init).
- PlacementController : ref vers Projectile.prefab assignée.

Si tout est OK en commit 2, **skip commit 3**. Sinon, regroupe les fixes ici.

### Process commit 3 (optionnel)

Si pas de fix nécessaire, juste push (les commits 1+2 suffisent). Sinon write fix + commit.

---

## Verification finale

```bash
find Assets/Scripts/Entities -name "*.cs" | wc -l   # 2 (Enemy, Tower, Projectile = 3 actually after POC-05)
find Assets/Prefabs/Towers -name "*.prefab" | wc -l # 1
find Assets/Prefabs -name "Projectile.prefab" | wc -l # 1
```

Via MCP play mode (1 minute test) :
- Click sur cell grass (par exemple cell (3, 3) en haut au centre).
- Logs `[Place] cost=30 ...` + Tower visible (cube bleu base + cylinder top).
- Wave 1 spawn enemies → entrent dans range tower → sphère jaune (projectile) volent vers eux → kill → log `[Enemy] killed type=basic reward=2`.
- Range Gizmo wireframe sphère bleue visible quand Tower sélectionné en Editor.

**Critères succès** :
- 3 .cs files (Tower, Projectile, PlacementController) compilent
- 2 prefabs (Tower, Projectile) créés
- Click-to-place fonctionne sur grass, reject sur autres cells
- Tower acquiert + tire + Projectile tue enemies
- 2-3 commits pushed sur main

## Pièges anticipés

1. **`Camera.main` returns null** si la Main Camera n'a pas le tag `MainCamera`. Vérifier dans Inspector. Sinon assigner via `[SerializeField] Camera cam;` Inspector drag.
2. **Raycast plane Y=0** : si caméra trop tiltée, ray parallèle au plane → no intersection. Avec rot X=60° pos (0,12,-6), ray intersecte bien.
3. **Click sur UI** : pas d'UI POC encore, mais en POC-07 il faudra `EventSystem.current.IsPointerOverGameObject()` filter pour ignorer clicks UI.
4. **Tower placement sur cell occupée** : POC ne check pas si une tower est déjà sur la cell → 2e click même cell = 2 towers superposées. Phase 2 corrigera. Si tu veux un quick fix POC : maintenir `HashSet<Vector2Int> occupiedCells` dans PlacementController.
5. **Projectile lifetime 3s** : skip Destroy si target survit longtemps mais sort de range. Sans le timeout, projectiles immortels.
6. **`SetFloat("_Smoothness", 0.9f)`** sur URP : URP utilise `_Smoothness` (Lit shader). Si shader Standard, c'est `_Glossiness`. Le code Init est compatible URP par défaut.

## Quand fini

2-3 commits push, termine.
