# Input Handler Audit — 2026-05-12

## Verdict

**Code Quality Status** : PARTIALLY CENTRALIZED

## Current State

### ✅ Centralized via KeyBindings.cs

KeyBindings singleton manages 11 actions with customizable keybinds (stored PlayerPrefs):
- pause (Escape)
- speed (Space)
- mute (M)
- debug (F3)
- birdseye (V)
- follow (F)
- help (H)
- pathPreview (P)
- save (F5)
- load (F9)
- reset (R)

### ⚠️ Hardcoded Handlers (Not in KeyBindings)

15+ files with direct `Input.GetKeyDown(KeyCode.*)` instead of `KeyBindings.GetKey()`:

**Tower Selection Hotkeys (Alpha1-4)** — PlacementController.cs:66-69
- Alpha1 = "archer"
- Alpha2 = "mage"
- Alpha3 = "cannon"
- Alpha4 = "frost"

**Hero Skill Hotkeys (Q/W/E)** — HeroSkillBarController.cs:75-77
- Q = skill slot 0
- W = skill slot 1
- E = skill slot 2

**Speed Adjustment (Equals/Minus)** — SpeedControlController.cs:41-44
- Equals / Keypad+ = speed up
- Minus / Keypad- = speed down

**Debug Hotkeys (S/U)** — PlacementController.cs:73-84 (dev-only)
- S = sell selected tower
- U = upgrade selected tower

**UI Shortcuts**
- L = history log (HistoryLogPanel.cs:69)
- N = new wave (HudController.cs:144)
- Space = start wave (HudController.cs:149)
- Escape = close menu (TowerSelectMenuController.cs, TowerComparePanel.cs, ConfirmDialog.cs)
- / = toggle help (HelpOverlayController.cs:43, alternate to H)

## Recommendation

### Phase 1 (Quick Win)
Add 8 missing action keys to KeyBindings.Defaults:
```csharp
{ "tower_select_1", KeyCode.Alpha1 },
{ "tower_select_2", KeyCode.Alpha2 },
{ "tower_select_3", KeyCode.Alpha3 },
{ "tower_select_4", KeyCode.Alpha4 },
{ "skill_q", KeyCode.Q },
{ "skill_w", KeyCode.W },
{ "skill_e", KeyCode.E },
{ "speed_adjust_up", KeyCode.Equals },
{ "speed_adjust_down", KeyCode.Minus },
```

### Phase 2 (Migrate Callers)
Replace hardcodes in:
1. PlacementController.cs (Alpha1-4) → `KeyBindings.GetKey("tower_select_N")`
2. HeroSkillBarController.cs (Q/W/E) → loop over bindings
3. SpeedControlController.cs (Equals/Minus) → `KeyBindings.GetKey("speed_adjust_*")`

### Phase 3 (Optional)
- Add UI panel to rebind tower/skill hotkeys (currently only CORE_03 uses KeyBindingsPanel for predefined actions)
- Consolidate ESC handlers into PlacementController deselect

## Files to Modify

- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/KeyBindings.cs` (+9 entries)
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/PlacementController.cs` (lines 66-69)
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/HeroSkillBarController.cs` (lines 75-77)
- `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/SpeedControlController.cs` (lines 41-44)

## Complexity

Phase 1: 10 min (add to KeyBindings)
Phase 2: 15 min (migrate callers)
Phase 3: deferred (low priority, nice-to-have)

**Current workload**: ~3 hours full refactor (with testing). **Skipped this session** (P2, not blocking).
