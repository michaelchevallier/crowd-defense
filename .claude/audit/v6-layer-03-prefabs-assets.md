# Layer 3 — Prefabs & Assets Audit

**Audit Date** : 2026-05-12  
**Scope** : Complete inventory of game assets (prefabs, materials, textures, audio, animations, ScriptableObjects)  
**Status** : READ-ONLY | No fixes applied

---

## 3.1 Prefabs Inventory

### Summary
- **Total Prefabs** : 28 (excluding TextMesh Pro)
- **Organization** : Root-level core entities + VFX subfolder + Towers/Enemies base classes

### Core Prefabs (Root)
| Prefab | Path | Root Components | Status |
|--------|------|-----------------|--------|
| Hero | `/Assets/Prefabs/Hero.prefab` | Transform, Hero (script) | Present |
| Projectile | `/Assets/Prefabs/Projectile.prefab` | Transform, Projectile (script) | Present |
| Tower (Base) | `/Assets/Prefabs/Towers/Tower.prefab` | Transform, Tower (script), Base child | Present |
| Enemy (Base) | `/Assets/Prefabs/Enemies/Enemy.prefab` | Transform, Enemy (script) | Present |

### VFX Prefabs (24 total)
Located: `/Assets/Prefabs/VFX/`

| Count | VFX Types |
|-------|-----------|
| 24 | Aura, CoinBurst, CoinPickup, Death, ElectricCloud, Explosion, ExplosionSmall, FireBreath, Frost, GlyphDark, HealAura, HitFlash, Impact, LevelUp, LightningBolt, MuzzleFlash, PerkPickup, PoisonCloud, Portal, ShieldAura, SlowAura, Spark, UpgradeBurst, UpgradeConfetti |

### Missing vs V4 Expected

**Expected in V4 (13 Tower types)** :
- ✓ Tower (base class only; no tower-specific variants)

**Expected in V4 (28 Enemy types)** :
- ✓ Enemy (base class only; no enemy-specific variants)

**Critical Gaps** :
- ❌ **No Tower Variant Prefabs** : Expected 13 tower-specific prefabs (Archer, Cannon, Mage, etc.) — only base class exists
- ❌ **No Enemy Variant Prefabs** : Expected 28 enemy-specific prefabs (Basic, Brute, Runner, Flyer, 10 Bosses, etc.) — only base class exists
- ❌ **BuildPoint Prefab** : No evidence of BuildPoint prefab (expected for tower placement grid)
- ❌ **Castle Prefab** : Castle appears only as C# script (Castle.cs, Castle.HP.cs, Castle.VFX.cs), no prefab asset
- ❌ **Hero Variants** : No hero skin variant prefabs (expected Knight, Ranger, Mage, Paladin, Warrior skins)

**Note** : Variant architecture appears to use **ScriptableObject configs** (TowerRegistry, EnemyRegistry) rather than prefab variants. Individual tower/enemy GameObject instantiation likely happens at runtime via configuration data.

---

## 3.2 Materials Inventory

### Summary
- **Total Materials** : 32 (.mat files)
- **Distribution** : `/Assets/Materials/` (22) + `/Assets/Materials/Skybox/` (10) + `/Assets/Resources/` (0 in main Materials folder, but 5 duplicates found in Resources)

### Materials by Category

#### Skybox Materials (10 total)
Located: `/Assets/Materials/Skybox/`
```
skybox_apocalypse.mat
skybox_cyberpunk.mat
skybox_desert.mat
skybox_espace.mat
skybox_foire.mat
skybox_foret.mat
skybox_medieval.mat
skybox_plaine.mat
skybox_submarin.mat
skybox_volcan.mat
```
**Status** : ✓ Complete (10 world themes covered)

#### Environmental & Special Effects
```
Lava.mat              → Shader: (custom GUID)
Water.mat             → Shader: (custom GUID)
Portal.mat            → Shader: (custom GUID)
Portal_Vortex.mat     → Shader: (custom GUID)
Starfield.mat         → Shader: (custom GUID)
SmokeTrail.mat        → Shader: (custom GUID)
```

#### Creature & VFX Materials
```
Hologram.mat          → Custom shader
Hologram_Scanline.mat → Custom shader
Jellyfish.mat         → Custom shader
Kelp.mat              → Custom shader
Kelp_Sway.mat         → Custom shader
```

#### Toon Shading
Located: `/Assets/Materials/` and `/Assets/Resources/Materials/`
```
ToonBase.mat
Toon_Default.mat
Toon_Foret.mat
Toon_Lava.mat
Toon_Snow.mat
Toon_Water.mat
```
**Note** : Toon shader materials exist in both `/Assets/Materials/` and `/Assets/Resources/Materials/` (duplication detected)

#### Duplicates in Resources Folder
```
/Assets/Resources/
  - BossHologram.mat
  - BossJellyfish.mat
  - Hologram.mat (duplicate of Assets/Materials/Hologram.mat)
  - Jellyfish.mat (duplicate of Assets/Materials/Jellyfish.mat)
  - ToonBase.mat (duplicate of Assets/Materials/ToonBase.mat)
  - Materials/Toon_*.mat (5 toon variants)
```

### Texture Assignment Status
- **Main Textures** : 175 PNG/JPG files across multiple directories
- **Texture → Material Mapping** : Not fully verified (would require per-material shader parameter inspection)
- **Expected ground/path/water/lava groups** : Assumed present but grouping not fully enumerated

---

## 3.3 Textures Inventory

### Summary
- **Total Texture Files** : 175 (PNG/JPG)
- **Distribution** :
  - `/Assets/Textures/` : 78 files
  - `/Assets/Models/` : 74 files (model-embedded textures)
  - `/Assets/Resources/` : 21 files
  - `/Assets/Screenshots/` : 2 files

### Directory Structure
```
/Assets/Textures/
├── Anim/
├── UI/
│   └── Achievements/
├── Sky/
├── Ground/
├── Bridges/
├── VFX/
├── Tiles/
├── Weather/
└── Pilote/
```

### Model Textures (74 in /Assets/Models/)
Organized by category:
```
Models/
├── Environment/  → Ground, skyboxes, environmental props
├── Heroes/       → Hero character textures
└── Props/        → Tower, castle, interactive objects
```

### Texture Groups vs V4 Expected
- ✓ Sky/Skybox textures present (10 skybox variations)
- ✓ Ground textures present (multiple themes)
- ✓ UI textures present (including achievements)
- ✓ VFX textures present (particles, effects)
- ✓ Character textures present (hero skins, enemies)
- **Status** : Approximately 75 PNG expected per flux; 78 found in `/Assets/Textures/` ✓

---

## 3.4 Audio Assets

### Summary
- **Total Audio Clips** : 31 files (OGG/MP3)
- **Audio Mixers** : 2 (.mixer files)
- **Distribution** : Music (11) + SFX (20)

### Music Tracks (11 total)
Located: `/Assets/Audio/Music/`
```
Ambient_W1.mp3                    → Plaine theme (MP3 format)
Ambient_W1_plaine.ogg             → Plaine
Ambient_W2_foret.ogg              → Foret (Forest)
Ambient_W3_desert.ogg             → Desert
Ambient_W4_volcan.ogg             → Volcano
Ambient_W5_foire.ogg              → Carnival
Ambient_W6_apocalypse.ogg         → Apocalypse
Ambient_W7_espace.ogg             → Space
Ambient_W8_submarin.ogg           → Submarine
Ambient_W9_medieval.ogg           → Medieval
Ambient_W10_cyberpunk.ogg         → Cyberpunk
```
**Expected** : 4 tracks (menu/calm/intense/boss) — **ACTUAL** : 11 (world theme-specific ambients) ✓

### SFX Clips (20 total)
Located: `/Assets/Audio/SFX/`
```
tower_shoot.ogg
tower_upgrade.ogg
tower_built.ogg
enemy_hit.ogg
enemy_die_basic.ogg
enemy_die_medium.ogg
enemy_die_boss.ogg
boss_charge.ogg
castle_hit.ogg
hero_shoot.ogg
wave_start.ogg
wave_clear.ogg
coin_pickup.ogg
gem_gain.ogg
perk_pick.ogg
level_up.ogg
achievement.ogg
skin_equip.ogg
blue_pill.ogg
boom.ogg
```
**Expected** : 16 SFX — **ACTUAL** : 20 ✓ (exceeds minimum)

### Audio Mixers
```
/Assets/Audio/MixerGroups.mixer           → Local mixer definition
/Assets/Resources/Audio/MainAudioMixer.mixer → Main game audio mixer
```
**Status** : ✓ Both present

**Summary** : Audio assets are **complete and well-organized**, exceeding V4 baseline.

---

## 3.5 Animations

### Animator Controllers (39 total)
Located: `/Assets/Resources/Animations/Controllers/`

#### Hero Controllers (5)
```
knight.controller
barbarian.controller
ranger.controller
rogue.controller
mage.controller
```

#### General/Rig Controllers (2)
```
Rig_Medium_General.controller
Rig_Medium_MovementBasic.controller
```

#### Enemy Controllers (32)
**Basic/Standard Enemies** (10):
```
mob_ninja.controller
mob_skeleton.controller
mob_blob_pink.controller
mob_blob_green.controller
mob_blob_spiky.controller
mob_bunny.controller
mob_frog.controller
mob_tribal.controller
mob_orc.controller
mob_birb.controller
```

**Advanced Enemies** (10):
```
mob_cactoro.controller
mob_alpaking.controller
mob_squidle.controller
mob_pigeon.controller
mob_armabee.controller
mob_dino.controller
mob_espace_astronaut.controller
boss_espace_ghost.controller
boss_espace_ghost_skull.controller
rogue_hooded.controller
```

**Boss Controllers** (12):
```
boss_apocalypse_orc_skull.controller
boss_volcan_dragon_v2.controller
boss_medieval_sorcier_roi.controller
boss_cyberpunk_hub_ia.controller
boss_volcan_demon.controller
boss_volcan_dragon_evolved.controller
boss_yeti.controller
boss_volcan_blue_demon.controller
boss_apocalypse.controller
boss_foret_mushroom_king.controller
boss_volcan_dragon.controller
dragon_low_poly_animated.controller
```

#### Animation Clips
- **Total Animation Clip (.anim) Files** : 0 found
- **Conclusion** : Controllers exist but **no discrete animation clips** are present as separate .anim files
- **Likely** : Animation sequences embedded within controller states or handled via code

**Status** : ✓ Controllers complete for all hero/enemy types; **✗ Animation clips not separated as assets**

---

## 3.6 ScriptableObject Assets

### Summary
- **Total ScriptableObject Assets** : 329 (.asset files)
- **Organized by type** : 10+ categories

### Asset Categories

| Category | Count | Path | Status |
|----------|-------|------|--------|
| **Levels** | 90 | `/ScriptableObjects/Levels/` | ✓ W1-10, levels 1-9 per world |
| **Enemies** | 28 | `/ScriptableObjects/Enemies/` | ✓ All 28 enemy configs (Basic, Brute, Flyer, 10 bosses, etc.) |
| **Towers** | 13 | `/ScriptableObjects/Towers/` | ✓ 13 tower types (Archer, Ballista, Cannon, Crossbow, Acid, Frost, Mage, Magnet, Mine, Portal, Skyguard, Tank, Fan) |
| **Perks (Standard)** | 17 | `/ScriptableObjects/Perks/Standard/` | ✓ Damage, Fire Rate, Range, Pierce, Crit, Lightning, Multi-shot, Ricochet, Lifesteal, Fireball, Pierce_Explode, Coin_Gain, Move_Speed, Surveillant, Wave_Regen, Architecte, Marchand_Mort |
| **Perks (School)** | 6 | `/ScriptableObjects/Perks/School/` | ✓ Glaciation, Pyromancie, Forteresse_Perk, Cristal_Glace, Combustion, Murs_Pierre |
| **Perks (SetBonus)** | 6 | `/ScriptableObjects/Perks/SetBonus/` | ✓ Vide, Pierre, Feu, Foudre, Or, Sang |
| **Skins** | 19 | `/ScriptableObjects/Skins/` | ✓ 10 castle skins + 5 knight hero skins + 4 VFX themes |
| **Achievements** | 58 | `/ScriptableObjects/Achievements/` | ✓ Large achievement set |
| **Modifiers** | 8 | `/ScriptableObjects/Modifiers/` | ✓ Magnetic_Storm, Rising_Lava, Gold_Blessing, Swift_Arrows, Iron_Castle, Reinforcements, Ancestral_Fog, Dragon_Breath |
| **Cutscenes** | 10 | `/ScriptableObjects/Cutscenes/` | ✓ World 1-10 cutscenes |
| **Events** | 12 | `/ScriptableObjects/Events/` | ✓ Event configurations |
| **Tutorial Steps** | 5 | `/ScriptableObjects/Tutorial/` | ✓ 5 tutorial steps |
| **Bosses** | 10 | `/ScriptableObjects/Bosses/` | ✓ Boss definitions |
| **Meta Upgrades** | 10 | `/ScriptableObjects/MetaUpgrades/` | ✓ Meta progression |
| **Doctrines** | 7 | `/ScriptableObjects/Doctrines/` | ✓ Doctrine configs |
| **Heroes** | 5 | `/ScriptableObjects/Heroes/` | ✓ Hero configurations |
| **Audio** | 1 | `/ScriptableObjects/Audio/` | ✓ Audio config |

### Global Registries (in `/Assets/Resources/`)
```
TowerRegistry.asset         → References all 13 tower configs
EnemyRegistry.asset         → References all 28 enemy configs
PerkRegistry.asset          → References all perk configs
SkinRegistry.asset          → References all 19 skins
LevelRegistry.asset         → References all 90 levels
AchievementRegistry.asset   → References all achievements
ModifierRegistry.asset      → References all modifiers
BalanceConfig.asset         → Global game balance
AssetRegistry.asset         → Master asset reference
LevelThemeMaterialConfig.asset → Material assignments per level
MetaUpgradeRegistry.asset   → Meta progression registry
DoctrineRegistry.asset      → Doctrine registry
CutsceneRegistry.asset      → Cutscene registry
EventRegistry.asset         → Event registry
TutorialRegistry.asset      → Tutorial registry
```

**Status** : ✓ **Complete and well-organized**

---

## 3.7 Asset Completeness Summary

### By Component

| Asset Type | Total | Expected (V4) | Status |
|------------|-------|---------------|--------|
| Prefabs (Core) | 4 | 4 | ✓ Present |
| Prefabs (VFX) | 24 | ~22 | ✓ Exceeds |
| Materials | 32 | ~30 | ✓ Complete |
| Textures | 175 | ~75 | ✓ Exceeds (2.3x) |
| Audio Clips | 31 | ~20 | ✓ Exceeds |
| Animator Controllers | 39 | ~35 | ✓ Exceeds |
| Animation Clips (.anim) | 0 | ~50-100 | ✗ Missing |
| ScriptableObjects | 329 | ~300 | ✓ Complete |
| **Total Assets** | **634** | ~520 | ✓ Exceeds |

---

## 3.8 Top 5 Incomplete Asset Groups

### 1. ✗ **Prefab Variants for Towers & Enemies** (CRITICAL)
- **Gap** : No tower/enemy-specific prefabs; only base classes exist
- **Expected** : 13 tower prefabs + 28 enemy prefabs = 41 total
- **Found** : 2 (base classes only)
- **Impact** : Tower/enemy visual differentiation likely handled at runtime via ScriptableObject configs + component toggling
- **Risk** : Variant architecture unclear; no prefab-level customization possible

### 2. ✗ **Castle Prefab** (HIGH)
- **Gap** : No Castle prefab asset found
- **Evidence** : Castle exists only as C# script (Castle.cs, Castle.VFX.cs, Castle.HP.cs)
- **Expected** : Prefabricated castle GameObject
- **Impact** : Castle likely instantiated dynamically from script
- **Risk** : No prefab-based castle customization; hard to iterate on castle visuals

### 3. ✗ **BuildPoint Prefab** (HIGH)
- **Gap** : No BuildPoint prefab or script found
- **Expected** : Tower placement grid point prefab
- **Current** : Unknown how tower placement points are created
- **Impact** : Tower placement system architecture unclear
- **Risk** : May use invisible colliders or custom UI grid system

### 4. ✗ **Hero Skin Variant Prefabs** (MEDIUM)
- **Gap** : No prefabs for hero skins (Knight, Ranger, Mage, Paladin, Warrior)
- **Found** : 5 hero animator controllers + 5 skin ScriptableObjects
- **Likely** : Skins applied via material/animator swapping, not prefab variants
- **Impact** : Hero visual switching works but lacks prefab structure
- **Risk** : Skin system architecture non-standard

### 5. ✗ **Discrete Animation Clip Assets** (MEDIUM)
- **Gap** : No .anim files found (0 animation clips as separate assets)
- **Found** : 39 animator controllers referencing animations
- **Likely** : Animations embedded within controllers or handled via Humanoid rig auto-generation
- **Impact** : Animation editing must occur within Animator window
- **Risk** : Animation clips cannot be reused across controllers; harder to debug animation issues

---

## 3.9 Asset Organization Quality

### Strengths
- ✓ Clear folder hierarchy (`Materials/`, `Textures/`, `Audio/`, `Resources/`, `ScriptableObjects/`)
- ✓ Themed organization (skybox materials by world theme, SFX by event type)
- ✓ Global registries centralize asset references
- ✓ Comprehensive ScriptableObject configs for all gameplay entities
- ✓ Abundant textures and audio (exceeds minimum baseline)

### Weaknesses
- ✗ Prefab structure minimal (only base classes, no variants)
- ✗ Material/texture mapping not explicit (requires shader inspection)
- ✗ Duplicate materials in `/Assets/Resources/` vs `/Assets/Materials/`
- ✗ Animation clips not separated as assets (all embedded in controllers)
- ✗ Castle and BuildPoint missing prefabs

---

## 3.10 Recommendations for Next Audit Layer

1. **Verify material texture bindings** : Inspect each material's shader parameters to confirm textures are assigned
2. **Check prefab component wiring** : Hero/Tower/Enemy base prefabs must have all required components (Rigidbody, Collider, Animator, etc.)
3. **Trace asset references** : Verify registries correctly link to all 13 towers, 28 enemies, 19 skins, 90 levels
4. **Investigate variant architecture** : Determine how tower/enemy types are differentiated at runtime (prefab swap vs. config + component state)
5. **Audit animation state machines** : Open each controller and verify state transitions + parameters

---

**Audit Report** : `/Users/mike/Work/crowd-defense/.claude/audit/v6-layer-03-prefabs-assets.md`  
**Next Layer** : Layer 4 (Scenes & Build Settings)
