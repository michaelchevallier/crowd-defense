# Layer 6 — Gameplay Mechanics (Audit Report)

**Date**: 2026-05-12  
**Scope**: V6 gameplay systems (hero, towers, waves, combat, perks, schools, doctrines, achievements, meta-upgrades, modifiers, events)  
**Baseline**: V4 Hero.js (837 LOC), BuildPoint walk-in tower placement, 3 schools  
**Status**: REGRESSION DETECTED on tower placement; 5 schools confirmed; 6 set bonuses; 8 dynamic events

---

## 6.1 Hero Mechanics

**File**: `Assets/Scripts/Entities/Hero.cs` (~800 LOC when complete)

### Movement
- **Input**: WASD + directional acceleration (MoveAccel=8)
- **Bounds**: Runtime-set max X/Z from grid; no boundary penalty
- **Auto-attack**: Toggle via PlayerPrefs `hero_auto_attack_v1`
- **Animation**: GLTF mesh + Animator (`Idle` ↔ `Walk`)

### Combat
- **Range-based auto-attack**: Target nearest enemy ≤ range
- **Cooldown**: `FireRateMul` applied per shot (perk multiplier chain)
- **Projectile**: Spawned by HeroProjectilePool, tint per hero type
- **Fire-trail**: Combustion perk spawns trail particles (tracked in `_fireTrails[]`)

### XP / Level-up
- **Progression**: Level 1 → MaxLevel (type.MaxLevel); XpToNext consumed per level
- **Pick flow**: OnLevelUp event triggers PerkPickerController
- **Perk application**: PerkSystem.ApplyPerk() applies multipliers immediately

### Ultimate (Slot R — Level 10+)
- **Cooldown**: 60s (const)
- **AoE Radius**: 4 world units
- **Damage**: 5× tower/hero dmg
- **Status**: E slot (Slot 2) exists in code; R slot mechanics not yet wired UI-side

### Special Perks Applied at Init
- `ForteressePerk`: Castle HP max ×1.25 (BalanceConfig)
- `MursPierre`: Flag only (no current effect)
- `CristalGlace`: Flag only (no current effect)
- `Glaciation`, `Combustion`, `Pyromancie`: Flags for VFX/mechanics

### Stats Multipliers (PerkSystem Application)
- FireRate, Range, Damage, MoveSpeed, CoinGain, XpMul
- CritChance, CritMul, CritStaggerMs
- MultiShot, PierceCount, Lifesteal, WaveRegen
- Projectile effects: Fireball, Ricochet, Lightning, PierceExplode
- Tower buffs: FirstTowerFree, TowerCostMul, TowerFireRateAura

### Death / Respawn
- **Condition**: HP ≤ 0 → TriggerMidLevelDeath()
- **Respawn delay**: 15s (hardcoded)
- **Respawn HP**: 50% max
- **Invul duration**: 2s post-respawn (flashing animation)
- **Condition to cancel**: Level state = Lost/Complete/Summary

---

## 6.2 Tower Placement

**File**: `Assets/Scripts/Systems/PlacementController.cs` (~455 LOC)

### Placement Workflow
1. **Tower type select**: Hotkeys 1/2/3/4 or menu (TowerSelectMenuController)
2. **Placement mode**: `selectedTowerType` != null → ghost preview active
3. **Click-to-place**: Raycast to ground plane → cell check → cost deduction → Instantiate

### Buildable Cell Detection
- **Grid check**: `grid.IsBuildable(x, y)` (PathManager.Grid)
- **Anti-overlap**: No collision detection; only pathfinding cells rejected
- **BuildPoint**: ❌ **REGRESSION — NOT IMPLEMENTED** (V4 had walk-in circular trigger zones)

### Cost Calculation
- **Base cost**: `selectedTowerType.Cost`
- **Hero perk discount**: `TowerCostMul` applied (may be ≤1 for discounts)
- **FirstTowerFree**: Once per run if hero has perk
- **Failure fallback**: Toast "Tour selectionnee", audio `place_invalid`, screen shake

### Tower Selection (Radial Menu — CORE-20)
- **Click on tower** (no type selected) → snap radius 1.5 world units → radial menu
- **Shift+click**: Tower compare panel (TowerComparePanel slot A/B)
- **ESC**: Deselect tower
- **Debug S**: Sell tower; **U**: Upgrade tower (editor only)

### Magnet Cap (Anti-spam, D1-01 Q3)
- **Default**: 1 magnet tower per level
- **Allow-multi**: 2 if `LevelData.AllowMultiMagnet == true`
- **Check**: Before placement, count `TowerBehavior.CoinPull` towers

### Touch Input
- **Single tap**: Tower select or place (platform switch UNITY_ANDROID || UNITY_IOS)
- **Double-tap**: Upgrade selected tower (0.3s window)

### Events Fired
- `OnTowerPlaced(tower)`: Tutorial, achievement hooks
- `OnTowerSelected(tower)`: Radial menu UI update
- `OnTowerUpgraded(tower, level)`: UI sync
- `OnTowerSold(tower, refund)`: Economy feedback
- `OnHoverPlacementCell(cell)`: PathfinderVisualization (every frame if type selected)

### Gap: BuildPoint Missing
**V4 had**: Hero walks onto circular trigger → UI picker → choose tower → confirm → place  
**V6 has**: Only click-based direct placement (PlacementController.Update hotspot)  
**Impact**: UI/UX regression; no walk-in tower building or pre-placement preview.

---

## 6.3 Wave System

**File**: `Assets/Scripts/Systems/WaveManager.cs` (~553 LOC)

### State Machine
1. **Lobby**: Wait for first click (OnBreakStateChanged fires)
2. **WaveActive**: Enemies spawning + auto-pathing
3. **WaveBreak**: All enemies dead; 5s skip-bonus window open
4. **LevelComplete**: All waves cleared → OnLevelComplete → perk pick → Summary
5. **Lost**: Castle reaches 0 HP → delayed → Summary

### Spawn Mechanics
- **Batching**: Pre-computed enemy list per wave (shuffled Fisher-Yates)
- **Rate**: `spawnRateMs` × `_specialSpawnRateMul` × `_varSpawnRateMul` × pattern multiplier
- **Pattern types**: Sparse (2×), Cluster (0.1×/4× toggle), VFormation (0.05×/4×)
- **Portal resolution**: `-1` → round-robin; `>= 0` → match meta.PortalIdx or fallback

### Endless Mode Scaling
- **W0-29**: HP mul = 1.15^wave (exponential)
- **W30-49**: HP mul = base×1.09^(wave-30) (compounded)
- **W50+**: HP mul = base×1.135^(wave-50) (harder)
- **Special waves** (every 5 waves after W10):
  - Elite swarm (W10-15): spawn rate ×0.6, count ×1.5
  - Boss rush (W15-20): scale ×1.5, rate ×1.5, count ×0.5
  - Chaos (W20-25): rate ±30%, count ×1.25 (cycles)

### Skip Bonus (D1-02 Pacing)
- **Window**: 5 seconds post-wave-clear (configurable)
- **Gold reward**: `SkipBonusGold` (BalanceConfig)
- **Streak tracking**: Consecutive skip claims (capped)
- **Streak reward mul**: 1 + streak × StreakBonusPerWave (default ×0.05, cap 1.25)
- **Reset**: Window expires OR castle damaged during break

### Wave Variance (D1-04)
- **Seed**: `levelId.hashcode ^ waveIdx` (deterministic replay)
- **Count variance**: ±cfg.WaveCountVariance (default 0.1 = ±10%)
- **Spawn variance**: ±cfg.WaveSpawnVariance (default 0.1 = ±10%)

### Pressure Mob (D1-04 Difficulty Scaling)
- **Rate**: 0% W1 → 60% W10 (linear, world-based)
- **Delay**: 3s after wave start
- **Speed**: 1.5× baseline
- **Type**: Random from wave entry pool

### Wave Regen (D1-04 World Progression)
- **Applies**: World < NoRegenWorldThreshold (default W6)
- **Amount**: +5 HP per wave cleared

### Events
- `OnWaveStart(idx)`: HUD, audio, boss warning
- `OnWaveCleared(idx)`: Reward popup, skip window open
- `OnKillCountChanged(killed, spawned)`: HUD counter
- `OnBreakStateChanged`: Skip button state + countdown

### Stats Snapshotted per Wave
- `LastWaveGoldEarned`: Calculated post-clear
- `LastWaveKillCount`: _waveKillCount
- `LastWaveElapsedSeconds`: unscaledTime delta

---

## 6.4 Combat System

**File**: `Assets/Scripts/Entities/Tower.Combat.cs` (~250+ LOC excerpt)

### Tower Attack Loop
1. **Target acquisition**: Within range + guard mode + line-of-sight check (StealthAlpha ≥ 0.4)
2. **Firearm cooldown**: Decrements each frame; triggers ExecuteFire when ≤ 0
3. **Projectile fire**: Spawned from pool, Init(target, dmg, speed, color, pierce, aoe, parabolic, ...)

### Damage Calculation Stack
```
base = cfg.Damage × TowerDamageMul (balance) × TalentSystem × ResearchDamage × _buffMul × _heroBuffDmg × _levelDmgScale × L3DmgMul

streak_bonus = base × (1 + streak × 0.05) if _lastKillTime within 2s window

berserker_bonus = base × L3BerserkerDmgMul if castle.hp < threshold

crit_roll = random < CritChance ? dmg × CritMul : dmg
crit_bonus_L3 = random < L3CritChance ? dmg × L3CritMul : dmg

flyer_bonus = dmg × max(cfg.FlyerDmgMul, _flyerDmgBonus) if target.IsFlyer && !ImmuneToFlyerBonus
```

### Projectile Effects
- **Pierce**: Passes through `N` enemies (L3Pierce or cfg.Pierce + _pierceBonus)
- **AoE**: Damage radius on hit (L3Aoe or cfg.Aoe)
- **Multi-shot**: Extra projectiles fired in spread pattern (cfg.Id=="archer" DPS: 15° spread, else 12°)
- **Parabolic**: Arcing trajectory with flight duration & apex height

### Special Tower Mechanics
- **Slow tower (L3 SlowOnHit)**: ApplySlow(target, mul=0.5, dur=500ms)
- **Freeze tower (L3 FreezeOnHit)**: ApplySlow(target, mul=0.0, dur=varies)
- **Chain lightning (L3 Mage DPS)**: FireChainLightning(origin, dmg×0.6, jumps=3, range=5)
- **Tank Berserker (L3 Tank DPS)**: ×2 dmg when castle < 50% HP
- **Tank Bulwark Aura (L3 Tank Utility)**: 20% damage reduction to nearby allies (range=4)
- **Archer MultiShot (L3 Archer DPS)**: +2 projectiles
- **Archer Crit (L3 Archer Utility)**: +25% crit chance, 3× crit mul
- **Crossbow FinalExplosion (L3 Crossbow DPS)**: 2.5m AoE on final pierce
- **Crossbow Pierce (L3 Crossbow Utility)**: +3 pierce count
- **Mage ChainLightning (L3 Mage DPS)**: Jump to 3 targets, 5m range
- **Mage Freeze (L3 Mage Utility)**: Freeze on hit (500ms)

### Enemy Target Priority
- **Flyers**: Prioritize by distance to castle (closer = higher priority)
- **Ground**: Prioritize by distance to tower (closer = higher priority)
- **Guard modes**: AirOnly / GroundOnly / All filters

### Guard Mode (Tower Property)
- **All**: Default, accept all targets
- **AirOnly**: Only attack flyers
- **GroundOnly**: Only attack ground units

---

## 6.5 Perk System

**File**: `Assets/Scripts/Systems/PerkSystem.cs` (~365 LOC)

### Perk Counts
- **Standard perks**: Stored in PerkRegistry.Standard (loaded asset count TBD)
- **School perks**: 5 schools × ~1-2 perks per school (dynamic per SchoolDef)
- **Set bonuses**: 6 (one per PerkTag: Foudre, Sang, Pierre, Feu, Vide, Or)
- **Legendary**: Runtime-generated when all achievements unlocked

### Schools → Tags → Set Bonuses
```
Elementaire → Feu (Fire)
Mecanique → Pierre (Stone)
Mystique → Vide (Void)
Bestiaire → Sang (Blood)
Strategie → Or (Gold)
```

### Perk Application (On Level-up)
1. **RollChoices()**: Filter by school, apply rarity weights (Common 60, Uncommon 30, Rare/Epic 10, Legendary 5)
2. **ApplyPerk()**: Multiplier chain (damage, range, fireRate, etc.)
3. **Set bonus check**: Tag count vs threshold → auto-apply if met

### Multiplier Chains (Hero Properties)
- `FireRateMul *= 1 - def.fireRate` (neg = faster)
- `RangeMul *= 1 + def.range`
- `DamageMul *= 1 + def.damage`
- `MoveSpeedMul *= 1 + def.moveSpeed`
- `CoinGainMul *= 1 + def.coinGain`
- `CritChance += def.critChance`

### Projectile Modifiers
- **Fireball**: radius + dmg mul
- **Ricochet**: bounces + decay per bounce
- **Lightning**: target count + dmg mul
- **PierceExplode**: radius + dmg mul on pierce threshold

### Tower Synergy Perks
- `TowerCostMul`: Affects PlacementController cost calculation
- `FirstTowerFree`: One tower costs 0 (once per run)
- `TowerFireRateAura`: Nearby towers fire faster (range TBD per perk or default 8m)

### Downside Perks (Risk/Reward)
- `downRange`, `downDamage`, `downFireRate`, `downCoinReward`: Negative multipliers

### Stackability Rules
- `stackable`: If true, multiple instances allowed
- `maxStacks`: Limit per perk (0 = no limit)
- `transform`: Mutually exclusive (only one active at a time)
- **LastChance guarantee**: If no transform acquired by last level-up, roll guaranteed transform

### Events
- `OnPerkApplied(hero, def)`: UI toast + audio
- `OnSetBonusActivated(hero, bonus)`: Set bonus audio

---

## 6.6 Schools System

**File**: `Assets/Scripts/Data/SchoolDef.cs` + `Assets/Scripts/Systems/SchoolRegistry.cs`

### 5 Schools Defined
1. **Elementaire** (Fire) → Starter tower: TBD (asset ref in starterTowerType)
2. **Mecanique** (Stone) → Starter tower: TBD
3. **Mystique** (Void) → Starter tower: TBD
4. **Bestiaire** (Blood) → Starter tower: TBD
5. **Strategie** (Gold) → Starter tower: TBD

### School Structure
- `id`: Unique identifier
- `displayName`: Localized UI label
- `description`: Flavor text
- `theme`: Color tint (used by HUD)
- `icon`: Sprite for UI
- `perks`: List<PerkDef> belonging to this school
- `unlockCost`: Gem cost to unlock (0 = always available)
- `starterTowerType`: Tower ID (e.g., "mage") given free at school selection

### School Picker Flow
1. **HeroPickScreen**: Player selects hero + school
2. **PerkSystem.SetPickedSchools()**: Filter perks to selected school(s) + generic pool
3. **PerkSystem.ApplyFreeSetBonus()**: School tag → auto-apply set bonus (SchoolToTag parse)
4. **RunContext**: School stored in run state for future reference

### Free Set Bonus Logic (Entry D1-01 Q2 reference)
- **Trigger**: RunContext.selectedSchool != null
- **Lookup**: PerkSystem.SchoolToTag(schoolId) → PerkTag
- **Apply**: PerkRegistry.GetBonus(tag) → ApplySetBonus()

---

## 6.7 Doctrines System

**File**: `Assets/Scripts/UI/DoctrineController.cs` + `Assets/Scripts/Systems/DoctrineSystem.cs`

### Doctrine Mechanics
- **Active doctrine**: One per run (stored in PlayerPrefs `cd_active_doctrine_v1`)
- **Gem cost**: Per doctrine (gemCost field in DoctrineDef)
- **Selection UI**: DoctrineController (UI component)

### Effect Keys (9 supported)
1. **TowerDamageMul**: Multiplier on all tower base damage
2. **SwarmMul**: Enemy count multiplier
3. **MagnetRange**: Coin magnet range ×
4. **MagnetCoinMul**: Coin pull amount ×
5. **BankInterestRate**: Gold bank interest rate ×
6. **SkipBonusGold**: Skip bonus gold amount ×
7. **StreakBonusPerWave**: Streak reward multiplier per consecutive skip
8. **SellRefundRatio**: Tower sell refund %
9. **CastleHPBase**: Base castle HP ×

### Activation Flow
1. **DoctrineSystem.TryActivate()**: Check gems ≥ cost
2. **BuildRunConfig()**: Clone BalanceConfig, apply modifiers per effect key
3. **LevelRunner**: Uses cloned config for run instance
4. **Deactivate()**: Clear doctrine, revert to default BalanceConfig

### Persistence
- Active doctrine ID + gem count stored in PlayerPrefs (DoctrineSystem cache)

---

## 6.8 Achievements System

**File**: `Assets/Scripts/Systems/Achievements.cs` + `Assets/Scripts/Data/AchievementDef.cs`

### Expected Count: ~56 achievements (audit-tracked)

### Predicate Types
1. **Event**: One-off unlock (e.g., `Unlock("wave_5_reached")`)
2. **Counter**: Deferred threshold check (e.g., `TrackEvent("tower_placed", 1)`)

### Properties per Achievement
- `id`: Unique key
- `titleKey`: Localized title (string key for i18n)
- `descKey`: Localized description
- `hidden`: If true, don't show in UI until unlocked
- `points`: Leaderboard/profile score
- `category`: Combat, Economy, Progression, Misc
- `rewardGold`: Gold granted on unlock (default 50)
- `iconEmoji`: Emoji display

### Counter Event Hooks (TrackEvent Pattern)
- **tower_placed**: Tower placement count
- **wave_cleared**: Wave completions
- **enemy_killed**: Total kills
- *others TBD*

### Persistent Storage
- **Unlocked set**: PlayerPrefs `cd.achievements.unlocked` (CSV)
- **Unlock order**: PlayerPrefs `cd.achievements.order` (chronological, for "recent" UI)
- **Counter values**: PlayerPrefs `cd.ach.counter.<eventKey>`

### Legendary Perk Trigger
- **Condition**: All achievements unlocked (IsAllUnlocked() check)
- **Effect**: PerkSystem.UnlockLegendaryPerk()
- **Perk**: Runtime-created with CritChance +20%, CoinGain ×2

### Events
- `OnUnlocked(id)`: Toast + audio (AchievementToastController subscribes)

---

## 6.9 Meta-Upgrade System

**File**: `Assets/Scripts/Systems/MetaUpgradeSystem.cs` + `Assets/Scripts/Data/MetaUpgradeDef.cs`

### Expected Count: 10 meta-upgrades (audit-tracked)

### Effect Keys (10 supported)
1. **CastleHPMul**: Castle max HP multiplier
2. **HeroDamageMul**: Hero weapon damage multiplier
3. **StartCoinsBonus**: Initial gold per run
4. **XpMul**: Experience gain multiplier
5. **HeroRangeMul**: Hero attack range multiplier
6. **CoinGainMul**: Gold reward per kill multiplier
7. **PerkChoiceCountBonus**: Extra perk options per level-up
8. **HeroFireRateMul**: Hero fire rate multiplier
9. **GemGainMul**: Gem reward multiplier
10. **TowerUpgradeDiscount**: Tower upgrade cost reduction (max % not added elsewhere)

### Structure per Upgrade
- `id`: Unique key
- `tier`: 1/2/3 (display grouping)
- `maxLevel`: Max upgrade level (typically 3)
- `costsPerLevel`: Array of gem costs per level [5, 15, 40] default
- `effects`: Array of (key, valuePerLevel) pairs (stacked multiplicatively)

### Persistence
- **Saved**: SaveSystem.GetMetaUpgradeLevel(upgradeId) → read from PlayerPrefs
- **Applied at run start**: MetaUpgradeSystem.ComputeBonuses() → RunBonuses struct

### Application
- **RunBonuses**: Struct applied to Hero + Castle at LevelStart
- **Economy**: StartCoinsBonus added to initial economy

### Events
- Castle HP bonus applied via HandleLevelStart() after LevelRunner.SpawnCastle()

---

## 6.10 Modifiers System (Curses & Blessings)

**File**: `Assets/Scripts/Data/ModifierDef.cs` + `Assets/Scripts/Data/ModifierRegistry.cs`

### Expected Count: 8 modifiers (4 curses + 4 blessings, audit-tracked)

### Modifier Types
- **Curse**: Negative effect (player chooses for challenge/reward trade)
- **Blessing**: Positive effect (reward or rare find)

### Structure
- `id`: Unique key
- `displayName`: UI label
- `modifierType`: Curse or Blessing
- `desc`: Effect description
- `applyAction`: Action string (parsed by EventSystem; same syntax as ChoiceDef.applyAction)

### Action Parsing (Event Bridge)
- Actions like `"coins+20"`, `"castleHP-50"`, `"pendingPerk=legendary"`, `"skipNextPerk"`
- Applied immediately on activation or deferred depending on context

### Usage
- **Daily challenges**: May apply modifiers to run (TBD per DailyChallenge)
- **Endless mode**: May apply modifiers (TBD per EndlessMode)

---

## 6.11 Dynamic Events System

**File**: `Assets/Scripts/Systems/DynamicEventManager.cs` (~400 LOC)

### 8 Event Types Implemented (V4 Parity R6-PARITY-012)

#### 1. **SandStorm** (18s default)
- **Effect**: Tower range ×0.75, enemy speed ×1.15
- **VFX**: Dust weather preset spawned
- **Restore**: Previous values on stop

#### 2. **LavaSurge** (18s default)
- **Effect**: 1-3 random towers disabled, castle 5 dmg/s
- **VFX**: Orange impact at tower positions
- **Restore**: Re-enable towers on stop

#### 3. **CarouselSpin** (one-shot)
- **Effect**: 30% of alive enemies reassigned to random alternate path
- **Condition**: Requires ≥2 paths
- **VFX**: None (instant)

#### 4. **VoidPulse** (18s default)
- **Effect**: Castle 1 dmg/s, purple expanding pulse VFX
- **VFX**: Dark purple impact ring

#### 5. **ZeroG** (8s typical)
- **Effect**: Enemy speed ×0.5, tower range ×1.2, fire-rate ×0.85 (via range proxy)
- **Note**: Fire-rate applied as range multiplier (V4 fidelity)
- **Restore**: Previous values on stop

#### 6. **Undertow** (18s default)
- **Effect**: Enemies on water tiles slow ×0.7, path rewind 1 tile
- **Condition**: Requires water path tiles (map-dependent)
- **Restore**: Previous speeds on stop

#### 7. **BattleCry** (18s default)
- **Effect**: All affected enemies speed ×1.4, inflict temporary buff
- **Duration**: Parameter-based (default 18s)
- **VFX**: Gold/yellow audio pulse (SFX)

#### 8. **Hack** (18s default)
- **Effect**: Random tower disabled + malfunction (FriendlyFireMode = true)
- **Scope**: Single tower
- **VFX**: Glitch/error SFX + visual artifacts (TBD detail)

### Trigger Mechanism
- **Hook**: WaveManager.OnWaveStart(waveIdx)
- **Data**: LevelData.WaveEvents[] array (data-driven)
- **Lookup**: Match waveIndex + eventType → parse enum → StartEvent()

### Configuration (LevelData)
```csharp
struct WaveEventEntry
{
    int waveIndex;
    string eventType;    // "sand_storm", "lava_surge", etc.
    float duration;      // 0 = use default (18s)
    float param;         // Event-specific param (unused most)
}
```

### Constraints
- **One active**: Only one event per wave; StartEvent() force-stops prior
- **Duration**: Clamped 3–60s (defaults 18s)
- **Cleanup**: ForceStop() restores all state on timer or early end

---

## 6.12 Gaps vs V4

### User-Facing Regressions

1. **Tower placement (CRITICAL)**
   - ❌ BuildPoint walk-in: V4 had hero circle triggers → picker UI
   - ❌ V6 only supports hotkey+click direct placement
   - **Impact**: No spatial tower-building affordances; UI/feel reduced
   - **Workaround**: None (requires PlacementController refactor + BuildPoint entity)

2. **Hero Ultimate (R slot)**
   - ✓ Mechanics coded (60s cooldown, 4m AoE, 5× dmg)
   - ❌ No UI keybind display (E slot visible, R slot not wired)
   - ✓ HeroSkillBarController may have UI stub

3. **School Starter Tower**
   - ✓ Data: `starterTowerType` field in SchoolDef
   - ❌ Flow: No verified pickup/spawn on school selection
   - **Status**: Likely partial (audit just shipped this field)

4. **Magnet Aura Visual**
   - ✓ Code: MagnetAuraCircle() built in Tower.Init()
   - ✓ Range: BalanceConfig.MagnetSlowRadius
   - ✓ But: Interaction with coin pickup (CoinPullManager) unclear

### Completeness Issues

5. **Perk Standard Count**
   - **V4**: 17 base + 6 school + 6 set bonuses = 29 total
   - **V6**: Count TBD (PerkRegistry.asset not introspected from binary)
   - **Risk**: May be under-provisioned if school count grew but perk pool stayed flat

6. **School Perks Distribution**
   - **V4**: 3 schools × 2 perks = 6
   - **V6**: 5 schools × ? perks = ? (audit just added starterTowerType field)
   - **Risk**: School perks may not be balanced or fully defined

7. **Achievement Counter Hooks**
   - ✓ Infrastructure: TrackEvent(eventKey, delta) pattern exists
   - ❌ Coverage: Only sample hooks found (wave_5_reached, tower_placed, wave_cleared)
   - **Risk**: Many predicates may not be wired in game systems

8. **Dynamic Event Rarity**
   - **V4**: Rare mid-wave occurrences (balance TBD)
   - **V6**: Data-driven per level (WaveEvents[])
   - **Risk**: Low event spawn rate may make them feel missing

### Technical Gaps

9. **Guard Mode Enum**
   - ✓ Defined: GuardMode.All / AirOnly / GroundOnly
   - ❌ UI: No tower selector menu for guard mode (internal only)
   - **Impact**: Can't change guard mode post-placement

10. **Tower L3 Branching**
    - ✓ Mechanic: 4 signature towers (Archer, Crossbow, Tank, Mage) with DPS/Utility branches
    - ❌ UI: No branch picker shown (auto-applied on L3?)
    - ❌ Code path: ApplyL3Branch() exists but picker flow unclear

---

## 6.13 Recommended Priority Tickets

### P0 (Blocking User Experience)
1. **CORE-25: Tower Placement UI** — Restore BuildPoint walk-in + picker
   - Add BuildPoint.cs entity (circle trigger)
   - Wire TowerSelectMenuController to appear on hero overlap
   - Update PlacementController to accept BuildPoint placement mode

2. **CORE-26: Hero Ultimate (R Key)** — Wire UI + audio
   - Add R keybind to HeroSkillBarController
   - Bind ULT fire to Input.GetKeyDown(KeyBindings.GetKey("ult_fire"))
   - Audio + VFX on cast

### P1 (Data Completeness)
3. **CORE-27: School Starter Tower** — Verify + implement pickup
   - Confirm starterTowerType per school (audit field added)
   - Implement free tower spawn at run start (PlacementController.RestoreTowers path or new)
   - Tutorial integration

4. **CORE-28: Perk Pool Audit** — Count + balance
   - Run editor tool on PerkRegistry.asset → count perks per rarity/school
   - Verify V4 parity (17 base + 6 school expected)
   - Flag gaps vs balance design doc

5. **CORE-29: Achievement Hook Coverage** — Wire all event keys
   - Grep codebase for Achievements.TrackEvent() calls
   - Verify counter hooks in: tower_placed, enemy_killed, perk_acquired, waves_cleared, etc.
   - Add missing hooks in game systems

### P2 (Polish)
6. **CORE-30: Guard Mode UI** — Add tower selector menu
   - TowerSelectMenuController or RadialMenuController → guard mode picker
   - Store per tower (Tower.CurrentGuardMode property)
   - Persist in mid-level save (PlacedTowerEntry)

7. **CORE-31: L3 Branch Picker** — UI for signature tower paths
   - On L3 upgrade, show DPS/Utility choice dialog
   - Store in Tower.UpgradeBranch
   - Apply corresponding L3Stats from table

8. **CORE-32: Dynamic Event Tuning** — Balance spawn rates + feedback
   - Review WaveEvents[] across all levels → ensure rarity feels right
   - Add on-screen indicator when event active (banner + SFX)
   - Tweak event durations per feedback

---

## Summary: Top 5 User-Facing Gaps

| Issue | Severity | Impact | Status |
|-------|----------|--------|--------|
| **Missing BuildPoint tower placement UI** | CRITICAL | Cannot build via walk-in; hotkey+click feels unnatural | Regression |
| **Hero Ultimate (R) not wired** | HIGH | Mechanic coded but unreachable; keybind missing | Partial |
| **School starter tower unclear** | MEDIUM | Data field added, flow untested | Likely incomplete |
| **Perk pool potentially under-spec** | MEDIUM | 5 schools may exceed standard perk budget | Risk |
| **Achievement counter hooks sparse** | LOW | Many events may not track achievements | Coverage gap |

---

**End of Audit Report**

