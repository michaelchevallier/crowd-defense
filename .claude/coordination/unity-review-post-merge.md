# Unity Post-Merge Code Review — 12-Axis Swarm

Scope: last 30 commits on `main` (multi-axis swarm merge wave). Read-only audit.

Conventions:
- P0 = ships broken / runtime NullRef / dead pipeline. Block release.
- P1 = silent dysfunction, memory leak, hot-path waste at scale.
- P2 = cleanup / smell, not blocking.

---

## P0 — Scene Setup: ALL 17 new singletons/controllers missing from `Main.unity`

Verified via guid cross-reference between `*.cs.meta` and `Assets/Scenes/Main.unity`. Zero hits for all 17 components listed in brief. `MonoSingleton<T>` fallback in `Assets/Scripts/Common/MonoSingleton.cs:14-26` auto-creates a bare GameObject — this hides the bug at runtime but:
- Loses any `[SerializeField]` Inspector wiring (BossSystem.registry, TowerToolbarController.towerRegistry, PerkChoiceOverlay refs).
- For UI controllers marked `[RequireComponent(typeof(UIDocument))]`, the auto-created GameObject gets a UIDocument with NO PanelSettings nor UXML asset attached — every `.Q<>(...)` returns null and the controller is a no-op.

Affected (auto-fixable in scene, but currently unwired):
| Component | File | Note |
|---|---|---|
| `PerkSystem` | `Assets/Scripts/Systems/PerkSystem.cs:13` | OK with auto-create; relies on `Resources.Load<PerkRegistry>` — see Resources P0 |
| `BossSystem` | `Assets/Scripts/Systems/BossSystem.cs:10` | Has `[SerializeField] List<BossDef> registry` line 12 — auto-create leaves empty, all bosses inert |
| `MetaUpgradeSystem` | `Assets/Scripts/Systems/MetaUpgradeSystem.cs:25` | OK with auto-create |
| `DoctrineSystem` | `Assets/Scripts/Systems/DoctrineSystem.cs:9` | OK with auto-create |
| `SkinSystem` | `Assets/Scripts/Systems/SkinSystem.cs:12` | OK with auto-create |
| `RunContext` | `Assets/Scripts/Systems/RunContext.cs:10` | OK; uses DontDestroyOnLoad |
| `CutsceneController` | `Assets/Scripts/UI/CutsceneController.cs:15` | `[RequireComponent(UIDocument)]` — auto-create yields broken doc, line 41-47 `Q<>` returns null |
| `MinimapController` | `Assets/Scripts/UI/MinimapController.cs:13` | `[RequireComponent(UIDocument)]` — same |
| `DebugHudController` | `Assets/Scripts/UI/DebugHudController.cs:10` | `[RequireComponent(UIDocument)]` — same |
| `TowerToolbarController` | `Assets/Scripts/UI/TowerToolbarController.cs:11` | `[RequireComponent(UIDocument)]` + `[SerializeField] towerRegistry` (line 14) — DOUBLE break |
| `ShopController` | `Assets/Scripts/UI/ShopController.cs:11` | `[RequireComponent(UIDocument)]` |
| `WorldMapController` | `Assets/Scripts/UI/WorldMapController.cs:9` | OK — present in `Assets/Scenes/WorldMap.unity` (guid found line 162). Only missing in Main.unity (expected). |
| `PerkPickerController` | `Assets/Scripts/UI/PerkPickerController.cs:13` | `[RequireComponent(UIDocument)]` |
| `PerkChoiceOverlay` | `Assets/Scripts/UI/PerkChoiceOverlay.cs:13` | `[SerializeField] canvas/cardContainer/cardPrefab` lines 15-17 — auto-create useless |
| `HudPerkBadges` | `Assets/Scripts/UI/HudPerkBadges.cs:13` | `[SerializeField] badgeContainer/badgePrefab/setProgressText` 15-17 — same |
| `BossUI` | `Assets/Scripts/UI/BossUI.cs:13` | `[RequireComponent(UIDocument)]` — `Q<boss-banner>` returns null and prints LogError 36 |
| `SkinPickerController` | `Assets/Scripts/UI/SkinPickerController.cs:15` | `[RequireComponent(UIDocument)]` |

Fix: add each to Main.unity (or for HUD-shared ones, mount on the existing HUD GameObject which already holds a UIDocument).

---

## P0 — Missing `Resources/` SO assets

`Assets/Resources/` contains only `AssetRegistry.asset`, `BalanceConfig.asset`, `LevelRegistry.asset`, `LevelThemeMaterialConfig.asset`. The following `Resources.Load<T>` calls will return null at runtime:

| Loader | Code |
|---|---|
| `PerkRegistry` | `Assets/Scripts/Data/PerkRegistry.cs:53` |
| `SkinRegistry` | `Assets/Scripts/Data/SkinRegistry.cs:17` |
| `MetaUpgradeRegistry` | `Assets/Scripts/Data/MetaUpgradeRegistry.cs:14` |
| `DoctrineRegistry` | `Assets/Scripts/Data/DoctrineRegistry.cs:15` |
| `CutsceneRegistry` | `Assets/Scripts/Data/CutsceneRegistry.cs:17` |
| `AudioClipRegistry` | `Assets/Scripts/Systems/AudioController.cs:59` |
| `AchievementRegistry` | `Assets/Scripts/Systems/Achievements.cs:30` (exists in `ScriptableObjects/Achievements/` but NOT under Resources) |

Symptoms: PerkSystem skips all perks (Awake warns line 25), Shop empty, no doctrines, no cutscenes, no SFX from registry. Each new merge populated the *.cs but no agent created the actual `.asset` instances under `Assets/Resources/`.

Fix: create SO instances under `Assets/Resources/` for each, OR move existing assets into Resources/ and populate them via the editor (BossDef list, perk defs, etc.).

---

## P0 — `LevelRunner.heroType` / `heroPrefab` not serialized in `Main.unity`

`Assets/Scenes/Main.unity:919-925` LevelRunner serialized fields only carry `currentLevel` + `castlePrefab`. The new `[Header("Hero")]` fields at `Assets/Scripts/Systems/LevelRunner.cs:21-23` (`heroType`, `heroPrefab`, `spawnHero`) have no values. Because `spawnHero` defaults to `true` but `heroType == null`, `SpawnHero()` returns early at line 178. No Hero ever spawns → all PerkSystem / HudPerkBadges / PerkChoiceOverlay / Hero panel logic is dead.

Fix: open Main.unity in Editor, assign `Knight.asset` to LevelRunner.heroType, save.

---

## P0 — `LevelEvents.RaiseLevelStart/End` never called

`Assets/Scripts/Systems/LevelEvents.cs:20-24` exposes static raise methods. Grep for `LevelEvents.Raise` returns 0 hits across the codebase. Subscribers:
- `Assets/Scripts/UI/MinimapController.cs:28-29` (OnLevelStart/OnLevelEnd)
- `Assets/Scripts/Visual/LevelVisualBridge.cs:15-16` (HandleLevelStart/HandleLevelEnd)

Consequence: minimap never shows (line 51 `SetVisible(false)` initial, line 57 OnLevelStart never fires → stays hidden), visual bridge never applies level theme.

Fix: in `LevelRunner.Start()` after `SpawnCastle()`+`SpawnHero()`, call `LevelEvents.RaiseLevelStart(currentLevel, gridBounds)`. Call `RaiseLevelEnd()` on victory/gameover transitions.

---

## P0 — `HudController` hero panel never queried

`Assets/Scripts/UI/HudController.cs:42-48` declares `heroPanel`, `heroHpLabel`, `heroLevelLabel`, `heroXpBarFill`, `heroXpLabel`, `heroXpValue`, `heroUltLabel`. Inside `Start()` (lines 56-121) **none of them are assigned via `root.Q<>(...)`**. `UpdateHeroPanel()` (line 166) returns at line 169 because `heroPanel == null`. The hero XP / level / ult HUD is invisible despite the commit `5cd60ee feat(ui): HUD hero panel`.

Fix: add the missing `heroPanel = root.Q<VisualElement>("hero-panel"); heroHpLabel = root.Q<Label>("hero-hp-label"); ...` lines in `Start()`.

---

## P0 — `DoctrineController` event lambda subscribe/unsubscribe mismatch (leak)

`Assets/Scripts/UI/DoctrineController.cs:38` subscribes:
```csharp
DoctrineSystem.Instance.OnDoctrineChanged += _ => RefreshCards();
```
`Assets/Scripts/UI/DoctrineController.cs:45` unsubscribes with a NEW lambda:
```csharp
DoctrineSystem.Instance.OnDoctrineChanged -= _ => RefreshCards();
```
Each lambda is a distinct delegate instance — the unsubscribe never matches, so the subscription leaks. After scene reload, every prior DoctrineController instance is still wired to `OnDoctrineChanged`, executing on disposed VisualElements. NullRef cascade likely on second level entry.

Fix: extract `private void OnDoctrineChangedHandler(DoctrineDef? _) => RefreshCards();` and use a method-group `+= OnDoctrineChangedHandler` / `-= OnDoctrineChangedHandler`.

---

## P0 — `OnLevelComplete` has zero subscribers → perk picker never shows

`Assets/Scripts/Systems/LevelRunner.cs:36` declares `public event Action? OnLevelComplete`. Grep for `OnLevelComplete +=` returns 0 hits. The picker controller comment (`Assets/Scripts/UI/PerkPickerController.cs:12`) says "LevelRunner.OnLevelComplete hooks ShowAndWait(onDone)" but no caller wires it. After Victory, the picker is never invoked.

Fix: in `PerkPickerController.Start()` add `LevelRunner.Instance?.OnLevelComplete += () => ShowAndWait(() => { });` plus matching `-=` in `OnDestroy()`.

---

## P1 — `TowerTooltipController.PopulateTooltip` runs every frame even when unchanged

`Assets/Scripts/UI/TowerTooltipController.cs:48-73`: `Update()` calls `PopulateTooltip(hovered)` every frame as long as the cursor is over a tower. `PopulateTooltip` rebuilds the full StringBuilder text (lines 98-256) and reads ~12 synergy fields per call. With `_buffMul`/`_pierceBonus` reset+recomputed by Synergies.LateUpdate each frame, every read is fresh — fine, but the string allocation is not. The `sb.Clear()` reuse helps, but `L.Get(...)` calls likely allocate, and `tooltipHeader.text = sb.ToString()` re-uploads to UI Toolkit every frame.

Fix: cache the hovered tower's `(buffMul, pierceBonus, multiShotBonus, …)` last-seen tuple and only repopulate when any field changes. Or repaint at 10 Hz via `IVisualElementScheduledItem` like `MinimapController` does (line 54).

---

## P1 — `TowerToolbarController` requires `towerRegistry` SerializeField but field can be unset

`Assets/Scripts/UI/TowerToolbarController.cs:14` declares `[SerializeField] private TowerRegistry? towerRegistry`. If a developer adds the component via Unity-MCP rather than the inspector, line 42-47 logs a warning and bails. No `Resources.Load<TowerRegistry>` fallback. Combined with the missing scene setup (P0), placement is unusable.

Fix: add `towerRegistry ??= Resources.Load<TowerRegistry>("TowerRegistry");` at the top of `Start()`.

---

## P1 — `PerkPickerController.BuildOffers` leaks `ScriptableObject` instances on fallback path

`Assets/Scripts/UI/PerkPickerController.cs:86-91` creates transient `PerkDef` via `ScriptableObject.CreateInstance<PerkDef>()` inside the fallback (when PerkRegistry is null, which is currently always — see P0 Resources). Each call to `ShowAndWait` creates 3 new SOs and never destroys them. Combined with the missing PerkRegistry P0, every level-up leaks 3 SOs.

Fix: either remove the fallback (after PerkRegistry exists) or `DestroyImmediate(def)` after card consumption.

---

## P1 — `BossUI` and `BossSystem` use `EventManager` while singleton may not be in scene

`Assets/Scripts/Systems/BossSystem.cs:30-34` subscribes in `Start()` to `EventManager.Instance.Subscribe<...>`. `Assets/Scripts/UI/BossUI.cs:39-46` same pattern in `Start()`. If `BossSystem` auto-creates a frame *after* `BossUI.Start()` runs (Awake order is alphabetical but not guaranteed), the BossUI may fire `Encountered` events that the BossSystem doesn't yet receive in tests. Less critical than P0 but: BossSystem.OnDestroy unsubs `OnEnemySpawned` / `OnLevelEnded` only — does not unsubscribe its own publish channels (no need; it only publishes). OK.

Smell: `BossSystem` calls `RebuildDict()` from both `OnAwakeSingleton()` (line 23) AND `Start()` (line 29) "guards against domain reload emptying the dict (R4)". This is correct defensive coding, leave as-is.

---

## P1 — `BossSystem` runs `LateUpdate` every frame even when no boss

`Assets/Scripts/Systems/BossSystem.cs:89-121`: returns at line 91 if `_currentBoss == null`. Cheap guard, fine. No fix needed.

---

## P1 — `MinimapController.OnGenerateVisualContent` does GridCoords full traversal at 10 Hz

`Assets/Scripts/UI/MinimapController.cs:138-155`: nested loop `grid.Height × grid.Width` calling `GridCoords.Walkable.Contains(c)` (List<char>.Contains is O(n) but constant w=h=~30). At 10 Hz with `MarkDirtyRepaint()` (line 73), that's 9000 char.Contains/sec. Acceptable on desktop, watch on mobile. Also `grid.Castles` / `grid.Portals` re-iterated each repaint; usually <10 elements so fine.

No fix required, but consider caching the path/castle/portal Vector2s on `OnLevelStart`.

---

## P1 — `DebugHudController.Update` does `WaveManager.Instance?.ActiveEnemies.Count` every frame

`Assets/Scripts/UI/DebugHudController.cs:77-78`: each `Instance` access is a property getter, not a cached field; the chained `.ActiveEnemies.Count` allocates nothing but is a property tree walk. Per-frame UI text setter line 75/80/86/90 is unavoidable for FPS display. Acceptable since DebugHud is editor/debug-only (gated at line 27-32). No fix.

---

## P1 — `RadialMenuController` / `TowerTooltipController` use `FindFirstObjectByType<UIDocument>()` fallback

`Assets/Scripts/UI/RadialMenuController.cs:39` and `Assets/Scripts/UI/TowerTooltipController.cs:36` fall back to `FindFirstObjectByType<UIDocument>()` if `GetComponent` returns null. This is a one-shot Start() call — fine. But if the HUD UIDocument is hosted on a different GameObject than these controllers (which it currently is — only HudController is in Main.unity), they will succeed by accident. Brittle.

Fix: mount RadialMenuController + TowerTooltipController on the same HUD GameObject as HudController, so `GetComponent<UIDocument>()` works deterministically.

---

## P2 — `PerkSystem.Awake` uses `OnAwakeSingleton` instead of `Awake`

`Assets/Scripts/Systems/PerkSystem.cs:20`: uses the protected override hook from MonoSingleton<T>. Correct pattern. No issue.

## P2 — `DoctrineSystem.BuildRunConfig` clones BalanceConfig with `Instantiate(source)` (line 64)

`Assets/Scripts/Systems/DoctrineSystem.cs:62-69`: returns a new SO clone each call. Currently has zero callers (grep `BuildRunConfig` only finds the declaration). Dead code until LevelRunner wires it in.

When wired: ensure each level start releases the previous clone via `Destroy(prevClone)` to avoid SO accumulation across levels.

## P2 — `Update()` polling in `TutorialOverlayController`

`Assets/Scripts/UI/TutorialOverlayController.cs:81-98` polls `PlacementController.Instance.PlacedTowers` foreach every frame searching for `UpgradeLevel >= 2`. Acceptable for tutorial phase (rare scenario), but consider adding a `Tower.OnUpgraded` event to drop the poll.

## P2 — `SkinPickerController` allocates new `Button` per tab in `BuildTabs()` (line 70-83)

`Assets/Scripts/UI/SkinPickerController.cs:70-83` and `Assets/Scripts/UI/SkinPickerController.cs:192-198` `RefreshLocale` calls `BuildTabs()` again — rebuilds 3 buttons each locale change. Minor.

---

## Summary

13 P0 + 7 P1 + 4 P2.

The merge wave produced functional code but **left scene wiring + Resources/ asset population entirely undone**. The MonoSingleton auto-fallback masks the missing GameObjects from compile errors, so play-mode launches but every new feature is silently inert. Fix order:
1. Create all missing `Assets/Resources/*Registry.asset` SOs (PerkRegistry, SkinRegistry, MetaUpgradeRegistry, DoctrineRegistry, CutsceneRegistry, AudioClipRegistry, AchievementRegistry).
2. Add all 16 missing GameObjects to `Main.unity` (mount UI controllers on existing HUD UIDocument where appropriate).
3. Assign `LevelRunner.heroType = Knight.asset` in Main.unity.
4. Wire `LevelRunner.OnLevelComplete += PerkPickerController.ShowAndWait`.
5. Call `LevelEvents.RaiseLevelStart/End` from LevelRunner.
6. Fix `HudController.Start` to query hero panel `.Q<>` refs.
7. Fix `DoctrineController` lambda unsubscribe bug.
