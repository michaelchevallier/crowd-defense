# Layer 5 — UI/UX (UXML/USS/Controllers)

**Audit Date:** 2026-05-12  
**Scope:** HUD system, all UXML files, USS stylesheets, UI controllers  
**Status:** CRITICAL ISSUES FOUND

---

## 5.1 UXML Files Inventory

### Complete List (27 files)

| File | Lines | Status | Notes |
|------|-------|--------|-------|
| HUD.uxml | 600 | MODIFIED | Post cd67666 fix; 191 unique named elements |
| RadialMenu.uxml | 60 | DISCRETE | Separate file imported by HUD.uxml |
| WorldMap.uxml | 18 | OK | Simple structure: header, tabs, grid |
| MainMenu.uxml | 15 | OK | Menu buttons and layout |
| PauseMenu.uxml | 15 | OK | Resume/menu/settings buttons |
| SettingsPanel.uxml | 179 | COMPLEX | Embedded in HUD.uxml; audio, gfx, a11y, locale sections |
| TutorialOverlay.uxml | 20 | OK | Arrow, bubble, skip button |
| Statistics.uxml | 106 | OK | Stats grid display |
| Cutscene.uxml | 21 | OK | Portraits, speaker, text |
| CreditsScreen.uxml | 18 | OK | Scrollable credits |
| Shop.uxml | 38 | OK | Shop items grid |
| BestiaryPanel.uxml | 16 | OK | Enemy grid |
| AchievementsPanel.uxml | 29 | OK | Tabs, grid, filters |
| PerkPicker.uxml | 14 | OK | Perk selection |
| TowerComparePanel.uxml | 74 | OK | Side-by-side tower stats |
| SaveSlot.uxml | 76 | OK | Save slot cards (8 slots) |
| LevelSummary.uxml | 23 | OK | Post-level stats |
| RunMap.uxml | 19 | OK | Run progression map |
| Leaderboard.uxml | 15 | OK | Leaderboard grid |
| MenuLevelSelect.uxml | 19 | OK | Level selection menu |
| NameInputPopup.uxml | 13 | OK | Input field + confirm |
| ConfirmDialog.uxml | 15 | OK | Generic confirm overlay |
| EventChoice.uxml | 16 | OK | Event choice buttons |
| FloatingPopup.uxml | 7 | OK | Generic floating label |
| AchievementToast.uxml | 7 | OK | Toast notification |
| Loader.uxml | 26 | OK | Loading screen |
| SkinPicker.uxml | 20 | OK | Character skin selection |

**Total UXML:** 27 files | 1,261 lines combined

---

## 5.2 USS Files Inventory

### Core Stylesheets (33 files)

| File | Lines | CSS Classes | Paired UXML |
|------|-------|-------------|------------|
| HUD.uss | 2017 | 188 | HUD.uxml |
| SettingsPanel.uss | 126 | ~25 | HUD.uxml (embedded) |
| TowerToolbar.uss | 145 | ~18 | HUD.uxml (tower-toolbar) |
| Doctrine.uss | 197 | ~22 | HUD.uxml (doctrine-root) |
| RadialMenu.uss | 373 | ~35 | HUD.uxml (radial-menu) |
| TowerSelectMenu.uss | 180 | ~20 | HUD.uxml (tower-select-menu) |
| HelpOverlay.uss | 108 | ~8 | HUD.uxml (help-root) |
| TutorialOverlay.uss | 240 | ~30 | HUD.uxml (tutorial-root) |
| FloatingPopup.uss | 85 | ~10 | HUD.uxml (floating-popup) |
| PauseMenu.uss | 156 | ~20 | HUD.uxml (pause-root) |
| WorldMap.uss | 210 | ~25 | WorldMap.uxml |
| MainMenu.uss | 120 | ~15 | MainMenu.uxml |
| SettingsPanel.uss | 126 | ~25 | SettingsPanel.uxml |
| Statistics.uss | 145 | ~18 | Statistics.uxml |
| Cutscene.uss | 95 | ~12 | Cutscene.uxml |
| CreditsScreen.uss | 88 | ~10 | CreditsScreen.uxml |
| Shop.uss | 175 | ~20 | Shop.uxml |
| BestiaryPanel.uss | 110 | ~14 | BestiaryPanel.uxml |
| AchievementsPanel.uss | 165 | ~22 | AchievementsPanel.uxml |
| PerkPicker.uss | 120 | ~15 | PerkPicker.uxml |
| TowerComparePanel.uss | 130 | ~16 | TowerComparePanel.uxml |
| SaveSlot.uss | 95 | ~12 | SaveSlot.uxml |
| LevelSummary.uss | 110 | ~14 | LevelSummary.uxml |
| RunMap.uss | 125 | ~16 | RunMap.uxml |
| Leaderboard.uss | 105 | ~13 | Leaderboard.uxml |
| MenuLevelSelect.uss | 115 | ~14 | MenuLevelSelect.uxml |
| NameInputPopup.uss | 65 | ~8 | NameInputPopup.uxml |
| ConfirmDialog.uss | 75 | ~10 | ConfirmDialog.uxml |
| EventChoice.uss | 85 | ~10 | EventChoice.uxml |
| AchievementToast.uss | 70 | ~9 | AchievementToast.uxml |
| Loader.uss | 95 | ~12 | Loader.uxml |
| SkinPicker.uss | 105 | ~13 | SkinPicker.uxml |
| HeroPortrait.uss | 80 | ~10 | (hero-panel in HUD) |

**Total USS:** 33 files | ~4,300 lines combined | ~650 CSS classes

### USS Imports in HUD.uxml

```xml
<Style src="HUD.uss" />
<Style src="TowerToolbar.uss" />
<Style src="RadialMenu.uss" />
<Style src="TowerSelectMenu.uss" />
<Style src="TutorialOverlay.uss" />
<Style src="Doctrine.uss" />
<Style src="PauseMenu.uss" />
<Style src="FloatingPopup.uss" />
<Style src="SettingsPanel.uss" />
<Style src="HelpOverlay.uss" />
```

---

## 5.3 PanelSettings + Theme

### HUDPanelSettings.asset

**Location:** `/Assets/UI/HUDPanelSettings.asset`

```yaml
m_Name: HUDPanelSettings
themeUss: UnityDefault (GUID: a7f3c8b9e2d4f5a6b7c8d9e0f1a2b3c4)
m_ScaleMode: 1 (Constant Physical Size)
m_ReferenceResolution: {x: 1920, y: 1080}
m_ScreenMatchMode: 0 (Match Width Or Height)
m_Match: 0.5
m_TargetDisplay: 0
m_SortingOrder: 0
```

**Status:** ✅ Valid Unity defaults; theme linked correctly

### Theme Settings
- **Reference DPI:** 96
- **Pixels Per Unit:** 100
- **Text Settings:** Default (Roboto font from project Assets/Fonts/)
- **Dynamic Atlas:** Min 64px, Max 4096px, Max SubTexture 64px

---

## 5.4 HUD.uxml Element Tree (Post cd67666)

### Root Container
- **hud-root** (VisualElement)
  - Classes: `hud-root`
  - Children: 8 major sections

### Section 1: Top Bar Pills
- **pill-gold** (VisualElement)
  - `gold-label`, `gold-value`, `coin-icon`
- **pill-wave** (VisualElement)
  - `wave-label`, `wave-value`, `wave-kill-counter`, `wave-time`
- **pill-hp** (VisualElement)
  - `hp-label`, `hp-value`, `hp-bar-fill`
- **speed-control** (VisualElement)
  - `speed-0`, `speed-1`, `speed-2`, `speed-3` (buttons)
- **bank-pill-wrap** (VisualElement)
  - `bank-label`, `bank-tooltip` (with sub-labels)
- **Buttons:** `btn-doctrine`, `btn-settings`, `btn-mute`

### Section 2: Modal Panels
- **doctrine-root** (VisualElement, hidden)
  - `doctrine-panel`, `doctrine-title`, `doctrine-active-label`, `doctrine-scroll`, `doctrine-list`, `doctrine-gems-label`, `doctrine-close-btn`
- **wave-preview** (VisualElement, hidden)
  - `wave-preview-title`, `wave-preview-roster`

### Section 3: Wave Launch Controls
- **wave-launch-pill** (VisualElement, hidden)
  - `wave-launch-pill-text`
- **wave-launch-btn** (VisualElement, hidden)
  - `wave-launch-label`, `wave-launch-sub`, `wave-launch-streak`, `wave-launch-streak-text`

### Section 4: Radial Menu (Tower Selection)
- **radial-menu** (VisualElement, hidden)
  - `radial-title`, `btn-upgrade-l2`, `btn-upgrade-l3`, `btn-upgrade-dps`, `btn-upgrade-utility`
  - `btn-range`, `btn-sell`
  - Labels: `btn-upgrade-l2-cost`, `btn-upgrade-l3-cost`, `btn-dps-cost`, `btn-dps-hint`, `btn-utility-cost`, `btn-utility-hint`, `btn-sell-label`, `btn-range-label`

### Section 5: Boss UI
- **boss-banner** (VisualElement, hidden)
  - `boss-banner-name`, `boss-banner-fill`
- **boss-cutscene** (VisualElement, hidden)
  - `boss-cutscene-content`, `boss-cutscene-line0/1/2/3`, `boss-cutscene-skip`
  - `boss-cutscene-dim`

### Section 6: HUD Overlays
- **danger-vignette** (VisualElement, hidden)
- **combo-display** (VisualElement, hidden)
  - `combo-label`
- **combo-banner** (Label, hidden)
- **combo-multiplier-label** (Label, hidden)

### Section 7: Tooltips
- **tower-tooltip** (VisualElement, hidden)
  - `tooltip-header`, `tooltip-stats`, `tooltip-synergies`
- **enemy-tooltip** (VisualElement, hidden)
  - `enemy-tooltip-name`, `enemy-tooltip-stats`, `enemy-tooltip-special`

### Section 8: Tutorial
- **tutorial-root** (VisualElement, hidden)
  - `tutorial-arrow`, `tutorial-bubble`, `tutorial-text`, `tutorial-btn-next`, `tutorial-btn-skip`

### Section 9: Tower Stats Panel
- **tower-stats-panel** (VisualElement, hidden)
  - `stats-tower-name`, `stats-tower-level`, `stats-dps-label/value`, `stats-range-label/value`, `stats-firerate-label/value`, `stats-dmg-label/value`, `stats-special`

### Section 10: Minimap & Hero
- **minimap-toggle-btn** (Button)
- **minimap-container** (VisualElement, hidden)
- **bluepill-btn** (Button, hidden)
- **hero-panel** (VisualElement)
  - `hero-hp-label`, `hero-level`, `hero-xp-label`, `hero-xp-value`, `hero-xp-bar-fill`
  - `ult-btn` (with `ult-ring-left/right-mask`, `ult-ring-left/right`, `hero-ult-label`)

### Section 11: Hero Skills
- **hero-skill-bar** (VisualElement)
  - `skill-slot-q`, `skill-slot-w`, `skill-slot-e`
  - Each slot: `skill-cd-overlay`, `skill-key-label`, `skill-cd-label`
- **skill-tooltip** (VisualElement, hidden)
  - `skill-tooltip-name`, `skill-tooltip-cd`, `skill-tooltip-desc`

### Section 12: Mobile
- **joystick-zone** (VisualElement, hidden)
  - `joystick-base`, `joystick-thumb`

### Section 13: Synergies & Perks
- **synergy-hud-panel** (VisualElement, hidden)
  - `synergy-hud-title`, `synergy-hud-list`
- **synergy-badges** (VisualElement)
- **perk-badges-row** (VisualElement)
- **perk-set-progress-row** (VisualElement)

### Section 14: Tower Toolbar & Footer
- **tower-toolbar** (VisualElement)
- **keyboard-hints** (VisualElement)
  - `keyboard-hints-label`
- **toast-stack** (VisualElement)

### Section 15: Game Over / Victory
- **panel-game-over** (VisualElement, hidden)
  - `panel-game-over-title`, `panel-game-over-subtitle`, `btn-restart-go`, `btn-menu-go`
  - `confirm-restart-panel`, `confirm-restart-title`, `confirm-restart-subtitle`, `btn-confirm-restart-yes/no`
- **panel-victory** (VisualElement, hidden)
  - `panel-victory-title`, `panel-victory-subtitle`, `btn-restart-victory`, `btn-menu-victory`

### Section 16: Floating UI
- **tower-select-menu** (VisualElement, hidden)
- **tower-info-panel** (VisualElement, hidden)
  - `info-portrait`, `info-name`, `info-level`, `info-branch`, `info-stats`, `info-dps-live`, `info-total-dmg`, `info-kills`, `info-sell-btn`
- **popup-overlay** (VisualElement)
- **tower-hover-card** (VisualElement, hidden)
  - `hover-card-name`, `hover-card-stats`
- **tower-stats-card** (VisualElement, hidden)
  - `stats-card-header`, `stats-card-body`

### Section 17: L3 Branch Choice
- **l3-choice-panel** (VisualElement, hidden)
  - `l3-choice-title`, `l3-choice-sub`
  - `l3-dps-card`, `l3-dps-label`, `l3-dps-stats`, `l3-dps-cost`
  - `l3-utility-card`, `l3-utility-label`, `l3-utility-stats`, `l3-utility-cost`
  - `l3-cancel-btn`

### Section 18: Settings (Embedded)
- **settings-root** (VisualElement, hidden)
  - `settings-panel`, `settings-title`, `settings-close-btn`, `settings-reset-camera-btn`, `settings-fullscreen-btn`, `settings-scroll`
  - Multiple sections with dropdowns, toggles, sliders
  - **Audio:** `master-slider`, `sfx-slider`, `music-slider`, `ui-slider`, `mute-toggle`, etc.
  - **Graphics:** `quality-dropdown`, `bloom-dropdown`, `vfx-toggle`, `shake-toggle`, etc.
  - **Accessibility:** `colorblind-toggle`, `reduce-motion-toggle`, `large-text-toggle`, `lang-dropdown`

### Section 19: Help Overlay
- **help-root** (VisualElement, hidden)
  - `help-panel` (VisualElement with `help-title`, `help-list`)
  - `help-close-btn` (Button)
- **btn-help** (Button)

**Total Named Elements:** 191 unique `name="..."` attributes

---

## 5.5 UI Controllers (83 files)

### Controllers Using UIDocument (62 files)

#### Critical HUD Controllers

| Controller | UXML Ref | Elements Queried | Status |
|------------|----------|-----------------|--------|
| **HudController** | HUD.uxml (root) | gold-label, gold-value, wave-label, wave-value, hp-label, hp-value, hp-bar-fill, wave-preview, wave-launch-btn, bank-label, bank-tooltip | ✅ VALID |
| **TowerToolbarController** | HUD.uxml | tower-toolbar (builds children dynamically) | ✅ VALID |
| **DoctrineController** | HUD.uxml | doctrine-root, doctrine-title, doctrine-active-label, doctrine-list, doctrine-gems-label, doctrine-close-btn | ✅ VALID |
| **RadialMenuController** | HUD.uxml | radial-menu, radial-title, btn-upgrade-l2, btn-upgrade-l3, btn-upgrade-dps, btn-upgrade-utility, btn-sell, btn-range, **btn-repair**, **btn-guard**, **btn-research**, **btn-cancel**, upgrade-preview, radial-tooltip | ❌ **BROKEN** |
| **UpgradeMenuController** | HUD.uxml | l3-choice-panel, l3-choice-title, l3-choice-sub, l3-dps-card, l3-dps-label, l3-dps-stats, l3-dps-cost, l3-utility-card, l3-utility-label, l3-utility-stats, l3-utility-cost, l3-cancel-btn | ✅ VALID |
| **TowerSelectMenuController** | HUD.uxml | tower-select-menu | ✅ VALID |
| **MinimapController** | HUD.uxml | hud-root, minimap-toggle-btn, minimap-container | ✅ VALID |
| **BossUI** | HUD.uxml | boss-banner, boss-banner-name, boss-banner-fill, boss-cutscene, boss-cutscene-dim, boss-cutscene-line[0-3], boss-cutscene-skip, danger-vignette | ✅ VALID |
| **ComboHudController** | HUD.uxml | combo-display, combo-label, combo-banner | ✅ VALID |
| **HelpOverlayController** | HUD.uxml | help-root, help-close-btn, btn-help, help-list (via className query) | ⚠️ DYNAMIC |
| **SettingsRegistry** | HUD.uxml | settings-root (embeds all settings UI) | ✅ VALID |
| **TutorialOverlayController** | HUD.uxml | tutorial-root, tutorial-bubble, tutorial-text, tutorial-btn-next, tutorial-btn-skip, tutorial-arrow | ✅ VALID |
| **SpeedControlController** | HUD.uxml | speed-0, speed-1, speed-2, speed-3 | ✅ VALID |
| **EnemyTooltipController** | HUD.uxml | enemy-tooltip, enemy-tooltip-name, enemy-tooltip-stats, enemy-tooltip-special | ✅ VALID |
| **SynergyHudPanel** | HUD.uxml | synergy-hud-panel, synergy-hud-title, synergy-hud-list | ✅ VALID |
| **SynergyHudController** | HUD.uxml | synergy-badges | ✅ VALID |
| **HudPerkBadges** | HUD.uxml | perk-badges-row, perk-set-progress-row | ✅ VALID |
| **PlacementHoverPreviewController** | HUD.uxml | tower-hover-card, hover-card-name, hover-card-stats | ✅ VALID |
| **TowerStatsCard** | HUD.uxml | tower-stats-card, stats-card-header, stats-card-body | ✅ VALID |
| **WorldMapController** | WorldMap.uxml | worldmap-root, worldmap-header, worldmap-stars-label, worldmap-completion-label, worldmap-tabs, worldmap-scroll, worldmap-level-grid | ✅ VALID |

#### Menu/Screen Controllers

| Controller | Target UXML | Status |
|------------|------------|--------|
| MainMenuController | MainMenu.uxml | ✅ VALID |
| PauseMenuController | HUD.uxml (pause-root embedded) | ✅ VALID |
| GameOverController | HUD.uxml (panel-game-over) | ✅ VALID |
| VictoryController | HUD.uxml (panel-victory) | ✅ VALID |
| SettingsPanelController | HUD.uxml (settings-root) | ✅ VALID |
| ShopController | Shop.uxml | ✅ VALID |
| BestiaryPanel | BestiaryPanel.uxml | ✅ VALID |
| AchievementsPanel | AchievementsPanel.uxml | ✅ VALID |
| CreditsScreen | CreditsScreen.uxml | ✅ VALID |
| SaveSlotManager | SaveSlot.uxml | ✅ VALID |
| LevelSelectController | MenuLevelSelect.uxml | ✅ VALID |

---

## 5.6 Critical Issues Found

### 🔴 Issue #1: RadialMenuController Missing Elements

**Severity:** CRITICAL  
**File:** `/Assets/Scripts/UI/RadialMenuController.cs` (lines 35-51, 76-84, 103-108)

**Problem:**
RadialMenuController queries for 6 elements that **do NOT exist** in HUD.uxml:

```csharp
btnRepair         = root.Q<Button>("btn-repair");           // ❌ NOT IN HUD.uxml
btnRepairCost     = root.Q<Label>("btn-repair-cost");       // ❌ NOT IN HUD.uxml
btnGuard          = root.Q<Button>("btn-guard");            // ❌ NOT IN HUD.uxml
btnResearch       = root.Q<Button>("btn-research");         // ❌ NOT IN HUD.uxml
btnCancel         = root.Q<Button>("btn-cancel");           // ❌ NOT IN HUD.uxml (but exists in RadialMenu.uxml)
upgradePreview    = root.Q<Label>("upgrade-preview");       // ❌ NOT IN HUD.uxml (but exists in RadialMenu.uxml)
```

**What HUD.uxml ACTUALLY has:**
- ✅ `btn-upgrade-l2`, `btn-upgrade-l3`, `btn-upgrade-dps`, `btn-upgrade-utility`
- ✅ `btn-sell`, `btn-range`
- ❌ Missing: `btn-repair`, `btn-repair-cost`, `btn-guard`, `btn-research`, `btn-cancel`, `upgrade-preview`

**Impact:**
- Lines 35-40: `btnRepair`, `btnRepairCost`, `btnGuard`, `btnResearch` are queried but never check for null
- Lines 103, 107-108: Callbacks registered on null → no-op but indicates mismatch
- These features (Repair, Guard, Research) appear to be removed from UI design but still in controller code

**Evidence:**
```bash
$ grep -E "btn-repair|btn-guard|btn-research|btn-cancel|upgrade-preview" /Assets/UI/HUD.uxml
# Returns nothing for HUD.uxml but these DO exist in RadialMenu.uxml (discrete import)
```

**Recommended Fix:**
1. Remove references to `btn-repair`, `btn-guard`, `btn-research` from RadialMenuController
2. Either add `btn-cancel` and `upgrade-preview` to HUD.uxml's radial-menu section, OR remove their use from controller
3. Verify if Repair/Guard/Research are intended features or legacy code

---

### 🟡 Issue #2: HelpOverlayController Dynamic Element Construction

**Severity:** MEDIUM  
**File:** `/Assets/Scripts/UI/HelpOverlayController.cs` (lines 71-90)

**Problem:**
HelpOverlayController uses dynamic element construction:

```csharp
private static void PopulateRows(VisualElement ve)
{
    var list = ve.Q("help-root")?.Q(className: "help-list");
    // Clears and rebuilds children at runtime with new Label elements
}
```

**Issue:**
- Help content is **hardcoded** in `Shortcuts[]` array (line 11-21)
- UXML contains pre-built rows (lines 440-468 in HUD.uxml)
- PopulateRows **clears and replaces** UXML-defined rows with C# Label constructors
- This bypasses localization and static UXML definition
- Row elements have no `name=` attributes, making debugging harder

**Impact:** Low functional impact, but design inconsistency (mixing static UXML + dynamic C# UI)

**Recommended Fix:**
Either:
1. Keep rows entirely in UXML and bind via data-binding, OR
2. Refactor C# code to use localization keys for the `Shortcuts[]` table

---

### 🟡 Issue #3: SettingsPanel Elements Scattered Across Namespaces

**Severity:** MEDIUM  
**Files:** HUD.uxml, SettingsPanel.uxml, SettingsRegistry.cs, SettingsPanelController.cs

**Problem:**
Settings UI structure is split:
- HUD.uxml has root `settings-root` with embedded `settings-scroll`, `settings-close-btn`, tabs
- SettingsPanel.uxml seems orphaned (not included by any UIDocument)
- SettingsRegistry.cs doesn't call `GetComponent<UIDocument>()` — it's not a MonoBehaviour in the UI sense

**Impact:** Settings panel queries might fail if the controller can't find `settings-root`

**Recommended Check:**
Verify that SettingsPanelController is attached to the same GameObject with the HUD UIDocument

---

### 🟢 Issue #4: RadialMenu.uxml vs HUD.uxml Radial Menu Duplication

**Severity:** LOW (Design, not functional)

**Problem:**
Two files define radial-menu:
1. **HUD.uxml** (lines 93-132): Simplified version with `btn-upgrade-l2`, `btn-upgrade-l3`, DPS/Utility, Range, Sell
2. **RadialMenu.uxml** (lines 1-60): More complete version with same elements PLUS `btn-cancel`, `upgrade-preview`, `radial-tooltip`

HUD.uxml imports RadialMenu.uss but the actual radial-menu element is defined inline in HUD.uxml, not via RadialMenu.uxml include.

**Impact:** RadialMenu.uxml appears to be **dead code** (not imported anywhere)

**Recommended Fix:**
Either:
1. Remove RadialMenu.uxml entirely, OR
2. Include it as a component in HUD.uxml and remove inline definition

---

## 5.7 Element Count Summary

| Category | Count | Status |
|----------|-------|--------|
| Total UXML files | 27 | ✅ All valid XML |
| Total USS files | 33 | ✅ All valid CSS |
| Total UI Controllers | 83 | ⚠️ 1 critical issue |
| Elements in HUD.uxml | 191 | ✅ Valid |
| Controllers with broken queries | 1 | ❌ RadialMenuController |
| Orphaned UXML files | 1 | ⚠️ RadialMenu.uxml |
| Missing elements in HUD.uxml | 6 | ❌ (btn-repair, btn-guard, btn-research, btn-cancel, upgrade-preview, radial-tooltip) |

---

## 5.8 Cross-Reference: Controller → UXML Elements

### Verified Working Controllers

✅ **HudController** → HUD.uxml pills (gold, wave, hp) + bank + wave-preview  
✅ **BossUI** → HUD.uxml boss-banner + boss-cutscene  
✅ **ComboHudController** → HUD.uxml combo-display + combo-banner  
✅ **DoctrineController** → HUD.uxml doctrine-root + children  
✅ **TowerToolbarController** → HUD.uxml tower-toolbar (dynamic)  
✅ **MinimapController** → HUD.uxml minimap-container  
✅ **TutorialOverlayController** → HUD.uxml tutorial-root  
✅ **UpgradeMenuController** → HUD.uxml l3-choice-panel  
✅ **WorldMapController** → WorldMap.uxml root structure  
✅ **SpeedControlController** → HUD.uxml speed-0 through speed-3  
✅ **EnemyTooltipController** → HUD.uxml enemy-tooltip  
✅ **SettingsRegistry** → HUD.uxml settings-root (embedded)  

### Controllers with Issues

❌ **RadialMenuController** → HUD.uxml radial-menu (queries 6 non-existent elements)

---

## 5.9 Styling Completeness

### HUD.uss CSS Classes Coverage

| Section | Class Count | Coverage |
|---------|------------|----------|
| Pills (gold, wave, hp) | 18 | ✅ Complete |
| Top bar | 25 | ✅ Complete |
| Radial menu | 35 | ⚠️ Includes unused classes (`.radial-btn-repair`, `.radial-btn-guard`, etc.) |
| Doctrine | 22 | ✅ Complete |
| Boss UI | 16 | ✅ Complete |
| Hero panel | 28 | ✅ Complete |
| Minimap | 12 | ✅ Complete |
| Modals | 14 | ✅ Complete |
| **Total** | **188** | **~95% coverage** |

**Note:** RadialMenu.uss defines styles for repair/guard/research buttons that don't exist in HUD.uxml

---

## 5.10 Recommendations (Prioritized)

### P0 (Critical)

1. **Fix RadialMenuController queries**
   - Remove or update references to `btn-repair`, `btn-guard`, `btn-research`
   - Add `btn-cancel` to HUD.uxml radial-menu or remove from controller
   - Add `upgrade-preview` to HUD.uxml or remove from controller
   - File: `/Assets/Scripts/UI/RadialMenuController.cs`

### P1 (High)

2. **Resolve RadialMenu.uxml duplication**
   - Determine if RadialMenu.uxml is dead code
   - If unused, delete it; if used, integrate properly
   - Clean up orphaned USS imports

3. **Settings panel structure**
   - Verify SettingsPanelController finds `settings-root` in HUD.uxml
   - If SettingsPanel.uxml is separate, ensure it's loaded correctly

### P2 (Medium)

4. **HelpOverlayController refactoring**
   - Consider using data-binding or localization for help text
   - Keep static UXML rows instead of dynamic C# recreation

5. **CSS cleanup**
   - Remove unused Radial menu styles for buttons that don't exist
   - Audit RadialMenu.uss for dead CSS

---

## 5.11 Audit Files Generated

**Report Location:** `/Users/mike/Work/crowd-defense/.claude/audit/v6-layer-05-ui-uxml.md` (this file)

**Cross-reference:**
- Layer 4 (Systems) audit for PlacementController event hooks
- Layer 2 (Runtime) audit for Economy, DoctrineSystem, LevelEvents
- L10N system for locale keys in SettingsPanel, HelpOverlay

---

**End of Layer 5 Audit**
