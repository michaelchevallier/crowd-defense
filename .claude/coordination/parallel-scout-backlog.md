# Parallel Scout Backlog — Crowd Defense Unity Migration

> Maintained by Permanent Parallel Scout. Items are ready to dispatch to Sonnet feature-dev / bug-fixer / quality-maintainer.
> Format: P0 = ship-blocker, P1 = strong V4 parity, P2 = polish / juicy. Sorted within each priority by readiness.
> Generated: 2026-05-11

---

## In-flight (DO NOT DUPLICATE)

- Wave preview UI (HUD.uxml + WavePreviewController)
- Synergy HUD badges (HUD.uxml + Synergies.cs event add)
- CoinPull animation polish (CoinPullManager.cs)
- QA smoke test live /v6/
- Perf audit live /v6/
- Texture gen ComfyUI Flux Schnell

---

## Ready to dispatch

### [P0] Wire LevelEvents.Raise + OnLevelComplete subscribers (unblocks Minimap + LevelVisualBridge + PerkPicker)
- **Files**: `Assets/Scripts/Systems/LevelRunner.cs`, `Assets/Scripts/UI/PerkPickerController.cs`
- **V5 ref**: `milan project/src-v3/systems/LevelRunner.js` (level start/end emission)
- **Conflicts with**: none — none of the in-flight items touch LevelRunner.cs or PerkPickerController.cs
- **Estimate**: 1 commit / 30 min
- **Spec sketch**:
  - In `LevelRunner.Start()` after `SpawnCastle()` + `SpawnHero()`, call `LevelEvents.RaiseLevelStart(currentLevel, gridBounds)`.
  - In `SetState(GameState.GameOver|Victory)` call `LevelEvents.RaiseLevelEnd(currentLevel)`.
  - In `PerkPickerController.Start()` add `LevelRunner.Instance.OnLevelComplete += HandleLevelComplete` (method group, NOT lambda) + matching `-=` in `OnDestroy()`.
  - HandleLevelComplete → `ShowAndWait(() => { /* resume / next */ })`.
  - **Verify**: play mode W1-1 victory → minimap should display + perk picker overlay should show.
- **Refs**: `unity-review-post-merge.md` P0 sections "LevelEvents.RaiseLevelStart/End never called" + "OnLevelComplete has zero subscribers".

### [P0] HudController hero panel Q<> wiring (HUD hero XP/lvl/ult dead)
- **Files**: `Assets/Scripts/UI/HudController.cs`
- **V5 ref**: `milan project/src-v3/main.js` hero panel block (~line 1000+) + `milan project/src-v3/ui/TickMetrics.js:92-112` (ult cooldown)
- **Conflicts with**: in-flight Synergy HUD badges also touches HUD.uxml/HudController, but hero panel queries live in separate `Start()` block — coordinate with that ticket OR run after it merges. **Recommend dispatching AFTER synergy badges merge** to avoid HudController.cs merge conflict.
- **Estimate**: 1 commit / 20 min
- **Spec sketch**:
  - In `HudController.Start()`, add the `.Q<>()` lookups for fields declared at lines 42-48: `heroPanel`, `heroHpLabel`, `heroLevelLabel`, `heroXpBarFill`, `heroXpLabel`, `heroXpValue`, `heroUltLabel`.
  - Hook `UpdateHeroPanel()` into the HUD refresh schedule.
  - Verify the matching UXML elements exist in `Assets/UI/HUD.uxml`; if missing, add them in the hero-panel container.
- **Refs**: `unity-review-post-merge.md` P0 "HudController hero panel never queried".

### [P0] DoctrineController lambda subscribe/unsubscribe leak fix
- **Files**: `Assets/Scripts/UI/DoctrineController.cs`
- **V5 ref**: none (Unity-only bug)
- **Conflicts with**: none
- **Estimate**: 1 commit / 10 min
- **Spec sketch**:
  - Extract `private void HandleDoctrineChanged(DoctrineDef? _) => RefreshCards();`.
  - Replace `OnDoctrineChanged += _ => RefreshCards();` with `+= HandleDoctrineChanged;`.
  - Replace `OnDoctrineChanged -= _ => RefreshCards();` with `-= HandleDoctrineChanged;`.
  - **Verify**: scene reload twice, no leaked subscriptions, no NullRef on reload.
- **Refs**: `unity-review-post-merge.md` P0 "DoctrineController event lambda subscribe/unsubscribe mismatch".

---

### [P1] Achievements.TrackEvent wiring (counters + predicate evaluation)
- **Files**: `Assets/Scripts/Systems/Achievements.cs`, `Assets/Scripts/Entities/Enemy.cs`, `Assets/Scripts/Entities/Tower.cs`, `Assets/Scripts/Systems/WaveManager.cs`, `Assets/Scripts/Systems/Economy.cs`, `Assets/Scripts/Systems/LevelRunner.cs`, `Assets/Scripts/Systems/Synergies.cs`
- **V5 ref**: `milan project/src-v3/systems/SaveSystem.js` achievement counters block
- **Conflicts with**: none of the in-flight items modify hot callsites (Enemy.Die, Tower.OnPlaced, Economy.AddGold). Synergies.cs is touched by in-flight badges work — **dispatch this ticket only AFTER synergy-badges merges** to avoid contention on Synergies.cs event additions.
- **Estimate**: 2 commits / 1.5h
- **Spec sketch**:
  - Add `TrackEvent` calls at 6 hot sites listed in `Achievements.cs:80-85` TODO block.
  - Add `CheckCounterAchievements(string eventKey, int current, string? context)` private method that iterates the registry and evaluates per-achievement predicates (port from `AchievementDef.cs` if predicate fields exist, else use simple counter thresholds matching v5 IDs: `first_blood`, `wave_clear_10`, `tower_master`, `million_gold`, `world1_complete`).
  - Verify: play W1-1, kill 1 enemy → `first_blood` unlocks → toast displays.
- **Refs**: `csharp-review-post-merge.md` "TODOs hors scope" + `Achievements.cs:93` TODO comment.

### [P1] Floating popups system (damage / coin / gem text)
- **Files**: NEW `Assets/Scripts/UI/FloatingPopupController.cs`, NEW `Assets/UI/FloatingPopup.uxml`, `Assets/UI/FloatingPopup.uss`. Wire calls in `Assets/Scripts/Entities/Enemy.cs`, `Assets/Scripts/Systems/Economy.cs`, `Assets/Scripts/Entities/TreasureTile.cs`.
- **V5 ref**: `milan project/src-v3/ui/Popups.js` (full, 69 LOC — spawnToast / spawnCoinPopup / spawnGemsPopup / spawnDamagePopup / spawnFlyingCoin)
- **Conflicts with**: CoinPullManager.cs in-flight polish — coordinate (FloatingPopup is parallel system; CoinPullManager keeps managing the existing fly-to-HUD coin pull; this ticket adds the **textual** popups for damage / gold values). Talk-to-author of CoinPull ticket before dispatch.
- **Estimate**: 2 commits / 2h
- **Spec sketch**:
  - Singleton `FloatingPopupController : MonoSingleton<FloatingPopupController>` with `SpawnDamage(int dmg, Vector3 worldPos)`, `SpawnCoin(int amount, Vector3 worldPos)`, `SpawnGems(int amount, Vector3 worldPos)`.
  - Each call projects worldPos → screen, creates a Label child under a dedicated overlay UXML, animates `translateY` -40px + opacity 1→0 over 900ms via `IVisualElementScheduledItem`.
  - Wire from Enemy.TakeDamage (display dmg), Economy.AddGold (+N over hit point), TreasureTile.Collect (gems).
  - Pool the Label instances (max 50 active).
- **Verify**: shoot a few enemies — damage numbers float up; treasure collect → +50¢ popup.

### [P1] Game Over + Victory panel score breakdown
- **Files**: `Assets/UI/HUD.uxml`, `Assets/UI/HUD.uss`, `Assets/Scripts/UI/HudController.cs`, `Assets/Scripts/UI/L.cs`
- **V5 ref**: `milan project/src-v3/ui/RunMode.js:272-330` (showRunVictory + showRunDefeat formatRunStats), `milan project/src-v3/main.js:1021` runVictoryOverlay HTML
- **Conflicts with**: in-flight Synergy HUD badges + hero panel wiring both touch HudController.cs / HUD.uxml. **Dispatch AFTER both merge** to avoid 3-way conflict.
- **Estimate**: 2 commits / 1.5h
- **Spec sketch**:
  - Extend `panel-victory` and `panel-game-over` UXML blocks with a `stats-container` showing: waves cleared, enemies killed, gold earned, towers placed, perks acquired, time elapsed.
  - In HudController.OnGameStateChanged Victory/GameOver, populate stats from RunContext + Achievements counter prefs.
  - Star rating for victory: 3 stars if no castle damage, 2 stars if HP > 50 %, 1 star otherwise. Animate stars 1-by-1 with 300ms stagger (USS class `.star-pop`).
  - L.cs i18n keys EN/FR for stats labels.
- **Verify**: play W1-1 to victory → stats pane displays + 3 stars animate in.

### [P1] PathTiles tile-by-tile reveal anim at level start
- **Files**: `Assets/Scripts/Systems/MapRenderer.cs` (likely), `Assets/Scripts/Visual/LevelVisualBridge.cs`. Maybe new `Assets/Scripts/Visual/PathRevealAnimator.cs`.
- **V5 ref**: `milan project/src-v3/systems/PathTiles.js` (600 LOC, tile-by-tile reveal sequenced by distance from spawn)
- **Conflicts with**: none — none of the in-flight items touch MapRenderer or LevelVisualBridge.
- **Estimate**: 2 commits / 2h
- **Spec sketch**:
  - On `LevelEvents.OnLevelStart`, identify all path tiles, sort by distance from portal cell.
  - Iterate with 60ms stagger: spawn each tile with localScale 0 → 1 via `Vector3.Lerp` over 200ms (or set scale 1 immediately and animate via USS-equivalent transform on GameObject).
  - Optional: chain a brief flash white material → base color at end.
  - Skipable via `if (Time.timeScale > 1f) instant-reveal-all`.
- **Verify**: load W1-1 → path appears tile by tile from spawn point.

### [P1] DailyChallenge UI panel (DailyChallengeController.cs)
- **Files**: NEW `Assets/Scripts/UI/DailyChallengeController.cs`, NEW `Assets/UI/DailyChallenge.uxml`, NEW `Assets/UI/DailyChallenge.uss`. Probable button hook in WorldMap.unity or MenuScene.
- **V5 ref**: `milan project/src-v3/systems/Daily.js` (already ported as `Assets/Scripts/Systems/Daily.cs`)
- **Conflicts with**: none
- **Estimate**: 2 commits / 2h
- **Spec sketch**:
  - UI Toolkit panel with: Daily Seed (read from `Daily.Instance.GetDailySeed()`), Challenge name + modifier, Best score, "Play" button.
  - `Play` button starts Daily.cs run flow (calls existing API in Daily.cs).
  - Open via WorldMap toolbar button or Menu screen button.
  - L.cs FR/EN keys.
- **Verify**: open panel → seed displayed deterministically same all day; click Play → level loads with daily modifier applied.

### [P1] BluePill mode UI panel + entry button
- **Files**: NEW `Assets/Scripts/UI/BluePillController.cs`, NEW `Assets/UI/BluePill.uxml`, NEW `Assets/UI/BluePill.uss`. Button in MenuScene or WorldMap.
- **V5 ref**: `milan project/src-v3/systems/BluePill.js` (already ported as `Assets/Scripts/Systems/BluePill.cs`)
- **Conflicts with**: none
- **Estimate**: 2 commits / 1.5h
- **Spec sketch**:
  - Panel description of BluePill mode (V5: god-mode autoplay for testing/debug, gates by setting).
  - Toggle "Enable BluePill mode" → calls `BluePill.Instance.Enable(true/false)`.
  - Read-only stats display: BluePill kills, BluePill levels passed.
  - Show only if `Application.isEditor || debug build`.
- **Verify**: enable BluePill → start W1-1 → enemies die instantly / autoplay runs.

### [P1] Tower upgrade RadialMenu wire-up to PlacementController
- **Files**: `Assets/Scripts/UI/RadialMenuController.cs`, `Assets/Scripts/Systems/PlacementController.cs`, `Assets/Scripts/Entities/Tower.cs`
- **V5 ref**: `milan project/src-v3/main.js:1097-1118` `_upgradeCost` + radial menu, `docs/specs/design/D1-03-upgrade-l3-hybride-spec.md`
- **Conflicts with**: none — none of the in-flight items touch RadialMenu/Placement/Tower upgrades.
- **Estimate**: 3 commits / 2.5h
- **Spec sketch**:
  - On left-click placed Tower, PlacementController shows RadialMenu at tower world-pos.
  - 3-4 segments: Upgrade L2 / Upgrade L3-DPS / Upgrade L3-Utility / Sell (80% refund per Q8).
  - Each shows cost label, disabled if `Economy.Gold < cost`.
  - On click → `tower.UpgradeTo(level, branch)` + economy debit.
  - Close menu on outside-click or ESC.
- **Verify**: place crossbow → click → menu opens → click upgrade-L2 → tower visual changes + gold deducted.

### [P1] FPS meter + debug HUD overlay (toggle key F)
- **Files**: `Assets/Scripts/UI/DebugHudController.cs` (existing), `Assets/UI/HUD.uxml`, possibly `Assets/UI/HUD.uss`
- **V5 ref**: `milan project/src-v3/ui/TickMetrics.js:1-36` (FPS color thresholds), `:38-73` (combo tracker), `:75-90` (wave countdown)
- **Conflicts with**: in-flight Synergy HUD badges touches HUD.uxml — **dispatch this ticket AFTER synergy badges merge** to avoid conflict.
- **Estimate**: 1 commit / 1h
- **Spec sketch**:
  - DebugHudController already exists with FPS basic display — enhance with V5 parity: keyboard `F` toggles visibility, color thresholds (`.good >50fps`, `.warn 30-50`, `.bad <30`).
  - Add `WaveCountdown` element (matches in-flight wave preview, coordinate).
- **Verify**: press F → FPS hides; play → color shifts on stress.

### [P1] Settings panel — Volume slider polish + Reset camera button
- **Files**: `Assets/Scripts/UI/SettingsPanelController.cs`, `Assets/UI/SettingsPanel.uxml`
- **V5 ref**: `milan project/src-v3/main.js` settings overlay block (search "settings-overlay")
- **Conflicts with**: none
- **Estimate**: 1 commit / 45 min
- **Spec sketch**:
  - Volume sliders: Master, SFX, Music, UI (4 sliders). Each bound to `AudioController.SetXVol(float)`. Persist in `SettingsRegistry` SO.
  - "Reset Camera" button → `Camera.main.GetComponent<CameraRig>()?.ResetToDefault()` (camera class likely exists, else create stub).
  - Live preview (slider change → audio level changes instantly).
- **Verify**: drag sliders → audio levels change in real-time; reload scene → persisted.

---

### [P2] AssetVariants — procedural skin/theme color swapping
- **Files**: NEW `Assets/Scripts/Visual/AssetVariants.cs`. Wire from `Assets/Scripts/Entities/Tower.cs` + `Enemy.cs` Init.
- **V5 ref**: `milan project/src-v3/systems/AssetVariants.js` (106 LOC)
- **Conflicts with**: none
- **Estimate**: 1 commit / 1h
- **Spec sketch**:
  - Static method `AssetVariants.ApplySkin(GameObject root, SkinDef skin)`: walks MeshRenderer.materials, applies palette color from skin.
  - Wire after `Init()` in Tower / Enemy spawn.
  - Verify with 2 swatches in SkinRegistry.

### [P2] Random Events (modifiers + RUN_EVENTS) integration
- **Files**: NEW `Assets/Scripts/Data/EventDef.cs` (SO), NEW `Assets/Scripts/Data/EventRegistry.cs`, NEW `Assets/Scripts/Systems/EventSystem.cs`. UI in NEW `Assets/Scripts/UI/EventChoiceOverlay.cs`.
- **V5 ref**: `milan project/src-v3/data/events.js` (242 LOC, 6 events × 2 choices), `milan project/src-v3/data/modifiers.js` (68 LOC, 8 curse/blessing modifiers)
- **Conflicts with**: none
- **Estimate**: 4 commits / 3h
- **Spec sketch**:
  - SO `EventDef` (title, body, 2 ChoiceDef `{label, applyAction}`).
  - SO `ModifierDef` (id, name, icon, type, desc, applyAction).
  - System rolls 1 event after each level finish with 30% chance.
  - Overlay UI with 2-button choice; clicking applies action to `RunContext`.
  - Modifiers applied at level start, displayed in HUD as small badges.

### [P2] Save slot management (3 slots)
- **Files**: `Assets/Scripts/Systems/SaveSystem.cs`, NEW `Assets/Scripts/UI/SaveSlotController.cs`, NEW `Assets/UI/SaveSlot.uxml`
- **V5 ref**: `milan project/src-v3/systems/SaveSystem.js` (slot abstraction in V4 not in V5)
- **Conflicts with**: none
- **Estimate**: 3 commits / 2.5h
- **Spec sketch**:
  - Extend SaveSystem with `currentSlot` int (0-2). PlayerPrefs keys gain slot suffix.
  - SaveSlot UI panel before main menu: 3 cards each showing slot summary (last played, world, gems).
  - "New Game" / "Continue" / "Delete" per slot.
- **Verify**: create 3 separate runs → switching slots loads correct state.

### [P2] LeaderboardPanel (local endless leaderboard)
- **Files**: NEW `Assets/Scripts/UI/LeaderboardController.cs`, NEW `Assets/UI/Leaderboard.uxml`. `Assets/Scripts/Systems/SaveSystem.cs` already has `endlessLeaderboard` references.
- **V5 ref**: `milan project/src-v3/ui/WorldMap.js:224` getLeaderboard, `milan project/src-v3/systems/SaveSystem.js:33` endlessLeaderboard, `main.js:1688` addLeaderboardEntry
- **Conflicts with**: none
- **Estimate**: 2 commits / 1.5h
- **Spec sketch**:
  - List top 10 endless mode runs (wave reached + timestamp).
  - Highlight current player run.
  - Click row → optional load run snapshot (V4 doesn't, skip).
  - Button in WorldMap or Menu.

### [P2] Achievement registry seeding (extend from 10 to 50+ achievements)
- **Files**: NEW `Assets/ScriptableObjects/Achievements/*.asset` (40+ new SOs), `Assets/Scripts/Data/AchievementRegistry.cs`, `Assets/Editor/BatchRebuild.cs` (extend tool)
- **V5 ref**: V5 implicit via 50+ unlock IDs scattered across `SaveSystem.js` + `main.js` calls to `unlockAchievement(id)`
- **Conflicts with**: none — additive content. Coordinate with P1 "Achievements.TrackEvent wiring" — best to dispatch BEFORE that ticket so wiring has full registry to predicate against.
- **Estimate**: 2 commits / 1.5h
- **Spec sketch**:
  - Grep all `unlockAchievement("...")` calls in V5 source → list of IDs.
  - Add Editor menu `Build/BuildAchievementRegistry` that generates 40+ `.asset` SO instances under `Assets/ScriptableObjects/Achievements/`.
  - Each SO: id, title (FR/EN), description (FR/EN), icon path, predicate type (counter / event), threshold int.
  - Update `AchievementRegistry.asset` All array.
- **Verify**: open AchievementRegistry inspector → 50+ items.

### [P2] PathfinderVisualization (dotted path preview)
- **Files**: NEW `Assets/Scripts/Visual/PathfinderVisualization.cs`. Wire from PlacementController on hover.
- **V5 ref**: V5 doesn't have explicit module — would be Three.js LineSegments with dashed material between Path waypoints; consult `milan project/src-v3/systems/Path.js`
- **Conflicts with**: none
- **Estimate**: 1 commit / 1h
- **Spec sketch**:
  - On tower placement hover, draw a dashed `LineRenderer` from spawn → path → castle showing enemy traversal.
  - Hide when not in placement mode.
- **Verify**: enter placement mode → dashed path visible on grid.

### [P2] Toast notifications generic system (for non-achievement events)
- **Files**: extend `Assets/Scripts/UI/AchievementToastController.cs` OR NEW `Assets/Scripts/UI/ToastController.cs` (generic).
- **V5 ref**: `milan project/src-v3/ui/Popups.js:11-17` spawnToast
- **Conflicts with**: none
- **Estimate**: 1 commit / 45 min
- **Spec sketch**:
  - Generic toast API `Toast.Show(title, body, durationMs, icon?)`.
  - Queue identical to AchievementToast (slide-in, auto-dismiss).
  - Wire from key game events (perk acquired, doctrine selected, level milestone).
- **Verify**: pick a perk → toast pops "+1 Range".

### [P2] CSharp P1 cleanup pass (csharp-review-post-merge.md items 1-6)
- **Files**: `Assets/Scripts/Systems/PerkSystem.cs`, `Assets/Scripts/Systems/SaveSystem.cs`, `Assets/Scripts/Data/PerkDef.cs`, `Assets/Scripts/Systems/BossSystem.cs`
- **V5 ref**: none (Unity-only cleanup)
- **Conflicts with**: none
- **Estimate**: 1 commit / 1h (Sonnet quality-maintainer)
- **Spec sketch**:
  - Remove unused `using System.Linq;` in PerkSystem.cs:4 if unused.
  - Remove dead `IsStackable` method in SaveSystem.cs:264.
  - Add Editor warning to SaveSystem.cs:74 catch.
  - Add `forteresseCastleHpMul = 1.5f` to PerkDef + read from def in PerkSystem.cs:190.
  - Extract `DefaultTowerAuraRange` const in PerkSystem.cs:185 (note: csharp-review already mentions an `8f` extracted in commit `8f541fe` — verify if still needed).
  - Consolidate `_defeatedPublished` in BossSystem.cs in a `PublishDefeatOnce()` method.
- **Refs**: `csharp-review-post-merge.md` P1 sections 1-6.

### [P2] Magic-strings → enum for MetaUpgradeEffect / DoctrineEffect keys
- **Files**: `Assets/Scripts/Data/MetaUpgradeDef.cs`, `Assets/Scripts/Data/DoctrineDef.cs`, `Assets/Scripts/Systems/MetaUpgradeSystem.cs`, `Assets/Scripts/Systems/DoctrineSystem.cs`
- **V5 ref**: `milan project/src-v3/data/metaUpgrades.js` (10 keys), `milan project/src-v3/data/schools.js` doctrines
- **Conflicts with**: none
- **Estimate**: 1 commit / 1h
- **Spec sketch**:
  - Create `enum MetaUpgradeEffectKey { CastleHPMul, HeroDamageMul, ... }` (10 values).
  - Create `enum DoctrineEffectKey { ... }`.
  - Update SO fields + system switches.
  - Verify all SO assets in `Assets/ScriptableObjects/Registry/` MetaUpgrades + Doctrines re-imported clean.
- **Refs**: `csharp-review-post-merge.md` P2#9.

---

## To investigate later (not yet specced)

- **Statistics screen detailed** — V4 has per-run breakdown; V5 only has `formatRunStats` in RunMode.js:272. Bigger ticket — defer until P0/P1 cleared.
- **WeatherController.PlayAmbientAudio / StopAmbientAudio stub** — TODO Phase 4 (csharp-review-post-merge.md). Defer.
- **Tutorial.js multi-phase port** — gap-analysis lists this. Tutorial dialogues already merged (`2f422b7`), need to check if FSM covers all 7 V5 phases or just 6.
- **Shop.js cosmetics UI** — Shop SO + ShopController exist, but actual cosmetics catalog (skins / themes / tower models) not populated. Investigate scope before specing.
- **VfxPool coroutine allocation pattern fix** — csharp-review-post-merge.md P2#11. Polish, defer.
- **RunMap.js — roguelike map generation** — major content port, parents of Run mode. Defer to Phase 4 dedicated sprint.
- **Cutscenes Visual content** — CutsceneRegistry seeded (1 cutscene per world?); needs actual text content per `data/cutscenes.js`.
