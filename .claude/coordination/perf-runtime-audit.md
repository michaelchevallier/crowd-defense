# Perf Runtime Audit — 2026-05-12 (code-based, Chrome MCP unavailable)

## Méthode
Audit statique du code C# déployé (build WebGL Unity 6 sur GitHub Pages v6).
Pas de mesure live (Chrome MCP non disponible dans cet environnement).

## Bottlenecks identifiés par ordre de sévérité

### B1 — Camera.main en hot path (CRITIQUE)
- `Enemy.UpdateHpBar()` : `Camera.main` chaque frame × N ennemis actifs
- `Enemy.UpdateDebuffIcons()` : `Camera.main` chaque frame × 4 icônes × N ennemis
- `EnemyHpBar.FaceCamera()` : `Camera.main` chaque `LateUpdate` × N ennemis
- `CoinToken.FlyRoutine()` : `Camera.main` chaque frame × M coins en vol
- `Camera.main` déclenche un `Object.FindObjectOfType` interne si pas caché — O(scène) par appel

### B2 — GetComponent + .material en Update (MODÉRÉ)
- `Enemy.UpdateGroundDecals()` : `_decalSlow.GetComponent<MeshRenderer>()` + `rend.material.color` chaque 2 frames sur TOUS les ennemis ralentis
- `.material.color =` (ligne 1362-1364 Enemy.cs) crée une instance Material unique par accès → allocation GC continue

### B3 — HashSet allocation dans FireChainLightning (MODÉRÉ)
- `Tower.FireChainLightning()` : `new HashSet<Enemy> { origin }` à chaque tir L3 Archmage
- Sur AoE en vague dense = alloc GC répétée mid-frame

### B4 — VfxPool : `GetLiveDps()` RemoveAt(0) sur List (FAIBLE)
- `Tower.GetLiveDps()` fait `RemoveAt(0)` qui shifte tout le tableau — O(N) par appel
- Appelé périodiquement via UI mais peut être déclenché souvent

## Estimé FPS
Sans mesure live : sur base des patterns → probable 45-55 FPS bureau avec ~20-30 ennemis actifs.
Goulot principal : Camera.main × N × 60fps = centaines de scene-scans/sec.

## Fix prioritaire recommandé
B1 : Cacher `Camera.main` dans un champ statique mis à jour 1x/frame (ou en Awake).
B2 : Cacher le `MeshRenderer` du `_decalSlow` et utiliser `MaterialPropertyBlock` au lieu de `.material.color`.
