# Recursion / Infinite-Loop Suspects
_Audit date: 2026-05-12 — quality-maintainer pass_

## Top 5 classés par probabilité

| # | File:Line | Pattern | Suspicion % | Notes |
|---|-----------|---------|-------------|-------|
| 1 | `Assets/Scripts/Systems/EventSystem.cs:103` | `ApplyAction(mod.ApplyAction)` — direct recursive call; if a `ModifierDef.applyAction` asset is ever serialised to `"modifier=<id>"` that points back to itself (or forms a chain A→B→A), this stack-overflows with zero guard | **85 %** | No depth counter, no visited-set, no cycle detection. Currently no `.asset` uses `modifier=` as its own `applyAction` value but there is no compile-time or runtime guard preventing it. Also: compound `applyAction` strings like `"castleHP-30\|pendingPerk=sang"` (pipe-separated, 4 assets) are NOT parsed — silently ignored, dead effect. |
| 2 | `Assets/Scripts/Common/MonoSingleton.cs:17-24` (Instance getter) + any `OnAwakeSingleton` that accesses another `Singleton.Instance` | Auto-create path: `new GameObject → AddComponent<T>()` triggers `Awake()` → sets `_instance` → calls `OnAwakeSingleton()`. If that override accesses a *different* singleton whose `Instance` getter also auto-creates, the chain can re-enter. `ComboSystem.OnAwakeSingleton` (line 23) calls `EventManager.Instance?.Subscribe` — if `EventManager` is missing from scene, its auto-create fires `EventManager.Awake` which is benign here, but the pattern is fragile for any future override that reads back a sibling singleton | **55 %** | Not a crash today; becomes one if any future `OnAwakeSingleton` references a sibling singleton that itself auto-creates and touches a third. |
| 3 | `Assets/Scripts/Systems/PathManager.cs:33-34` — `Waypoints => Paths[0]` / `WaypointCount => Waypoints.Count` | Two-level expression-bodied chain; `WaypointCount` → `Waypoints` → `Paths[0]` → `_empty`. Not a loop, but if `Paths` property were ever made expression-bodied and referenced `Waypoints` the cycle would be invisible. Low-risk as-is but structurally fragile | **20 %** | No current loop; flagged as maintainability smell only. |
| 4 | `Assets/Scripts/Systems/WaveManager.cs:133` — `OnWaveCleared?.Invoke(currentWaveIdx)` inside `Update()` → `EventSystem.OnWaveCleared` → `StartCoroutine(RollAfterDelay)` → `ApplyAction` | Not a sync recursion — coroutine defers one frame. However if the coroutine itself re-triggers a wave-clear (e.g., a `castleHP-N` kills the last enemy... which is impossible today but structurally possible if the event graph changes) | **15 %** | Low probability, worth a comment |
| 5 | `Assets/Scripts/Systems/EventSystem.cs:74` — `ApplyAction` pipe-separator not handled | `Event_haunted_shrine`, `Event_merchant_caravan`, `Event_raven_omen`, `Event_lava_geyser` assets use `"A\|B"` compound actions. `ApplyAction` has no `\|` splitter — all compound effects silently fire only action A (or are ignored entirely). Not a recursion crash but a silent logic bug that will manifest during gameplay QA | **N/A (logic bug, not recursion)** | 4 assets affected. |

## Recommandations

1. **Priorité 1** — Ajouter un guard de profondeur dans `EventSystem.ApplyAction` pour le cas `modifier=` :
   ```csharp
   internal static void ApplyAction(string action, int depth = 0)
   {
       if (depth > 8) { Debug.LogError("[EventSystem] ApplyAction max depth reached — cycle in ModifierDef?"); return; }
       // ...
       ApplyAction(mod.ApplyAction, depth + 1);
   }
   ```
2. **Priorité 2** — Implémenter le parsing pipe (`|`) dans `ApplyAction` : splitter sur `|`, appeler récursivement chaque token. Sans quoi 4 events live sont partiellement silencieux.
3. **Priorité 3** — Ajouter `random50:A:B` parser (utilisé dans `Event_raven_omen`) : même problème, action ignorée silencieusement.
