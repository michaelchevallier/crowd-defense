# V4 (Phaser 2D) ↔ V6 (Unity 6 URP) — Audit Assets / Rendu / VFX

**Date** : 2026-05-14
**Source V4** : `/Users/mike/Work/milan project/src-v2/` (Phaser 4, scene `LevelScene`, dist `/v4/`)
**Source V6** : `/Users/mike/Work/crowd-defense/Assets/` (Unity 6 LTS URP 17.3.0, scène `Main.unity`, dist `/v6/`)
**Scope** : Code & assets review only (pas de live test, WebGL bloqué par mémo `feedback_no_webgl_test_for_now.md`)
**Last V6 commit** : `735abdc4` skybox fix V8I (apply on Awake)

---

## TL;DR

| Catégorie | V4 visible | V6 code | V6 wired | Score combiné |
|---|---|---|---|---|
| 1. Themes mondes | 10 | 10 | ~9/10 ⚠️ Foire skybox via OnAwake fix | **90%** |
| 2. Particles / Kill VFX | OK (cap 200) | 22 builders | 14 + 8 extras | **95%** (V6 surpasse V4) |
| 3. Weather | 6 presets | 14 types / 10 themes | proc fallback ok ⚠️ prefabs absents | **75%** |
| 4. Hitstop + Shake | OK basic | OK + slowmo + crit throttle | ⚠️ camera ref runtime | **85%** |
| 5. Flash damage / hud | OK | Flash + RedVignette | ⚠️ UIDocument required | **80%** |
| 6. Shaders custom | 0 (Phaser) | 12 (Toon, Water, Lava, Hologram, Jellyfish, Portal, Kelp, Starfield, Outline, BossHologram, BossJellyfish, SmokeTrail) | matériaux wired | **GAIN V6** |
| 7. Post-process | 0 (Phaser) | URP Bloom + Vignette + ColorGrading per theme | code only ⚠️ no Inspector profile | **GAIN V6** |
| 8. Tower visuals L1/L2/L3 | data only | 18 GLTFs (archer/ballista/mage/tank L1+L2+L3 = 12 + 6 single-tier) | ⚠️ MaterialController upgrade tint only L3 | **70%** |
| 9. Enemy visuals (17 V4 types) | colored containers | 28 EnemyData + 31 GLTFs mob + 13 GLTFs bosses | ScriptableObjects done | **GAIN V6** |
| 10. Boss visuals (3 V4) | scaled+horn/crown procédural | 10 BossDef + 13 boss GLTFs + BossAura pulse + Hologram shader overlay | ScriptableObjects done | **GAIN V6** |
| 11. Hero visuals (BluePill) | 1 procédural | 5 hero archetypes + 6 KayKit chars + 5 knight skin variants | ScriptableObjects done | **GAIN V6** |
| 12. Audio (procéduraux V4 + 3 tracks) | OK 10 SFX + 3 musics | 23 .ogg SFX + 10 .ogg ambient + MixerGroups | wiring 72% per STATUS R7 | **70%** |
| 13. Cutscenes ASCII (V4) | 6 (1/monde) + ASCII art | 10 CutsceneDef (1/monde) | ⚠️ **lines only — pas d'ASCII art port** | **50%** |

**Verdict global** : V6 a un système rendu/VFX/audio nettement plus avancé que V4 sur 6 catégories (shaders, post-process, enemies/towers/bosses, hero), mais **lag sur 3 zones** : Foire weather/audio wiring, ASCII art cutscenes, et runtime Animator state machines (déjà connu STATUS R7 T-VISUAL-002).

---

## 1. Themes mondes (sky + ground + weather + decor)

### V4 (`src-v2/systems/Theme.js`)
- **10 thèmes** : Plein Été, Crépuscule, Tempête, Volcan, Apocalypse, Foire Magique, Espace, Sous-Marin, Médiéval, Cyberpunk
- 4 propriétés gradient par thème : `skyTop`, `skyBottom`, `groundTop`, `groundBottom`, `laneA/B/Stroke`, `accent`, `weather` (clear/fireflies/rain/ash/lightning/magic/stars/bubbles/leaves/neon)
- **Pas de shader** : pure gradient Phaser fillStyle + tile rectangles colorés

### V6
- **`SkyboxController.cs`** (10 panoramic `skybox_*.mat` au format `equirectangular`) — actuellement V8I commit `735abdc4` fix : apply skybox on Awake plutôt qu'OnLevelStart
- **`ThemeAmbientController.cs`** : sun rotation + color + intensity + ambient Trilight + fog density per theme (10 mappings)
- **`PostProcessController.cs`** : per-theme post-exposure + contrast + colorFilter + saturation
- **`Resources/Materials/`** : 10 `ground_<theme>.mat` + 10 `path_<theme>.mat` + `LevelThemeMaterialConfig.asset`
- **`SceneDecor.cs`** : THEME_PALETTE par theme (BigKeys/MediumKeys/SmallKeys/RockKeys, counts, scale ranges) — 10 thèmes spécifiés
- **`Textures/Sky/`** : 10 PNG sky_<theme> (assets pour blit ou cubemap)

### Gap
- ✅ **V6 surpasse V4** : a un vrai pipeline 3D lighting (sun + ambient + fog) que V4 simulait par gradient flat
- ⚠️ **P1** : `SkyboxController` V8I fix dépend de l'assignation Inspector des 10 `skybox_*.mat` — vérifier que tous les 10 slots Inspector sont remplis sur Main.unity (sinon Plaine appliqué par défaut)
- ⚠️ **P2** : `ThemeAmbientConfig` resources existants pour 10 themes (`Resources/Lighting/ThemeAmbient_*`), mais nécessite vérif runtime

---

## 2. Particles / Kill VFX (explosion, smoke, blood, boss ring, treasure shimmer)

### V4 (`src-v2/systems/Particles.js` + `entities/Visitor.js:543-590`)
- Pool simple : `count` global cap **200 actifs**, `spawnCircle`, `spawnRect`, `burst(N, factory)`
- Kill VFX :
  - **basic kill** : 10 circles colorés (shirtColor + skin + accent + white), tween radial 50+random
  - **boss kill** : 18 circles + ring or stroke `0xffd23f` scale 10x + popup "BOSS DOWN!"
  - **damage flash** : alpha 0.4 yoyo 60ms
  - **popup damage number** color-mapped (lava=orange, frost=cyan)

### V6 (`Visual/VfxPool.cs` + `VfxPoolExtra.cs` + `VfxPoolBuilders.cs/2.cs`)
- **22 VFX builders** : Impact, Death, Explosion, CoinBurst, HitFlash, LevelUp, PerkPickup, Frost, Portal, FireBreath, MuzzleFlash, UpgradeBurst, Spark, UpgradeConfetti, ElectricCloud, ExplosionSmall, GlyphDark, HealAura, LightningBolt, PoisonCloud, ShieldAura, SlowAura
- **ObjectPool<ParticleSystem>** Unity-native, DefaultCapacity 24, MaxPoolSize 100 par pool
- **LOD adaptatif** : si FPS<30 → maxParticles ×0.5
- **PreWarm** auto au démarrage
- **Sub-emitters** : Explosion → ExplosionSmoke + SmokeGray automatiquement
- **SpawnDeathPuff(tier)** : tier 0 white / tier 1 magenta x2 / tier 2 orange x5
- **`Castle.VFX.cs`** : smoke + fire + dangerLight + sparks loops HP-based
- **`Enemy.Anim.cs` TriggerHitFlash** : white tint 90ms

### Gap
- ✅ **V6 surpasse V4 par ordre de grandeur** (22 vs ~3 kill VFX types)
- ✅ **Boss ring or équivalent** : `JuiceFX.Flash(Color.white, 400)` + `SpawnDeathPuff(tier=2)` orange burst
- ⚠️ **Damage numbers** : V6 a `FloatingPopupController.SpawnDamage(dmg, worldPos)` ligne 77 — port OK
- ⚠️ **P2** : TreasureChest V4 (`+75¢`) → V6 a `TreasureTile.cs` + `TreasureSpawner.cs` mais pas vérifié VFX shimmer (à confirmer)

---

## 3. Weather (rain, snow, ash, fog, fireflies, neon, magic, bubbles, leaves)

### V4
- Pas de système Weather séparé (signature `theme.weather` string seulement, rendu via particles dans LevelScene)
- 10 weather strings : clear, fireflies, rain, ash, lightning, magic, stars, bubbles, leaves, neon

### V6 (`Visual/WeatherController.cs`)
- **14 WeatherType enum** : Clouds, Rain, Snow, Wind, Embers, Pollen, Dust, Confetti, Bubbles, Stars, Ash, Mist, Fireflies, NeonRain
- **`ThemeWeather` map** 10→[1-3 types] : Plaine=Clouds, Foret=Pollen+Mist, Desert=Dust+Wind, Apocalypse=Dust+Ash+Wind (DesertStorm dense), Volcan=Embers+Ash, Espace=Stars+Snow, Submarin=Bubbles, Medieval=Dust+Wind, Cyberpunk=NeonRain+Wind, Foire=Confetti+Pollen
- **`AmbientClips`** mapping vers `Resources/Audio/Ambient/ambient_*.ogg` (10 fichiers présents : `Ambient_W1_plaine.ogg` à `Ambient_W10_cyberpunk.ogg`)
- **Procedural ParticleSystem fallback** quand prefab non-assigné — `BuildProcedural(wt)` config 14 types avec gravity/speed/lifetime/color/noise
- **`AmbientParticles.cs`** : pollen + 3 papillons quads animés pour Worlds 1-3 (Plaine/Foret/garden)

### Gap
- ✅ **V6 dépasse V4 largement** (V4 n'avait pas vraiment d'implémentation visible weather)
- ⚠️ **P1** : aucun prefab assigné par défaut dans `Prefabs/Weather/` (le `PrefabPaths` map pointe vers `Prefabs/Weather/{Type}` Resources path) — **les 14 weather types tomberont sur procedural fallback** sauf si Mike ajoute les prefabs
- ⚠️ **P2** : `Weather.Foire = Confetti+Pollen` mais audio Foire = `ambient_calm` (pas de musique fête spécifique)
- ❌ **P2 manquant** : V4 weather `lightning` (Apocalypse W5) → V6 absent du WeatherType enum (substitué par Dust+Ash+Wind)
- ❌ **P2 manquant** : V4 weather `magic` (Foire Magique W6) → V6 mappé Confetti+Pollen, manque l'effet "particule étoilée" original
- ❌ **P3 manquant** : V4 weather `leaves` (Médiéval W9) → V6 substitué Dust+Wind

---

## 4. Hitstop + Screen shake

### V4 (`src-v2/systems/JuiceFX.js`)
- `hitstop(ms)` : `scene.tweens.timeScale=0` + `physics.world.isPaused=true` pendant N ms
- `shake(mag, dur)` : `cameras.main.shake(dur, mag/1000)`
- Presets : `kill` 65ms + 4mag 100ms, `explode` 12mag 250ms, `fire` 2mag 80ms, `boss` 16mag 500ms

### V6 (`Visual/JuiceFX.cs`)
- Subscribe automatique aux events `EnemyKilledEvent`, `BossEncounteredEvent`, `BossDefeatedEvent`, `CastleHitEvent`, `CastleDestroyedEvent`, `LevelEndedEvent`
- **Shake** : amplitude + duration, decay linéaire jusqu'à 0 sur durée, `LateUpdate` accumulation-safe (un seul slot)
- **SlowMo** (équivalent hitstop) : `Time.timeScale=N` pendant durée, coroutine
- **ShakeOnCritHit** : throttled 0.15s entre triggers (anti-spam crit)
- **PunchScale(target, peakScale, duration)** : tween scale up-down sur transform (V4 n'a pas d'équiv exposé)
- **Settings gate** : `SettingsRegistry.Instance.ShakeEnabled` toggle

### Gap
- ✅ **V6 surpasse V4** : event-driven auto-subscribe + slow-mo + crit throttle + punch-scale + settings toggle
- ⚠️ **P2** : `Camera.main` cached dans `OnAwakeSingleton` — si camera switch dynamique (cinematic, freecam debug), `SetBaseCamPos` doit être appelé sinon shake offset baseline incorrect

---

## 5. Flash effects (damage red, levelup green, gold yellow, hud)

### V4 (`src-v2/systems/Flash.js`)
- `Flash.entity(target, color, ms)` : applique `setFillStyle` ou `setTint` récursif sur container + restore après ms
- `Flash.hud(text, color, ms)` : change text color + scale tween 1.3→1
- Color presets via call sites : `0xff4444` damage, `0x00ff00` levelup, `0xffd23f` gold

### V6
- **`JuiceFX.Flash(color, ms)`** : fullscreen UIDocument overlay opacity 0→0.45→0
- **`JuiceFX.PunchScale(transform, peak, dur)`** : tween scale (équivalent V4 hud scale)
- **`PostProcessController.FlashRedVignette(intensity, fadeOut)`** : red vignette burst HP-driven
- **`Enemy.Anim.cs:TriggerHitFlash`** : white tint sur SkinnedMeshRenderer via MPB, 90ms
- **Castle damage feedback** : `JuiceFX.Flash(Color(0.9, 0.1, 0, 0.3), 800)` quand HP<15%

### Gap
- ✅ V6 : multiple layers (entity tint, screen overlay, vignette burst)
- ⚠️ **P0** : `JuiceFX.Flash` requires `UIDocument` in scene — if absent, log warning + no-op (voir code `EnsureFlashOverlay` ligne 211-243)
- ⚠️ **P1** : V4 `Flash.hud` (HUD text color flash on coin/levelup) → V6 a `FloatingPopupController.SpawnDamage` mais pas équivalent flash existing UI element text color (à vérifier coverage)

---

## 6. Shaders custom (V4=0, V6=12)

### V4
- Phaser canvas — pas de shader custom (V4 alternative `src-v3` Three.js avec `Shaders.js` mais pas dans le scope V4)

### V6 (`Assets/Shaders/`)
- **Toon family** (5 shaders) : `Toon_Lit`, `Toon_Lava`, `Toon_Water`, `Toon_Snow`, `OutlineInvertedHull` + 2 animated (`Toon_Lava_Animated`, `Toon_Water_Animated`)
- **Boss family** (4 shaders) : `BossHologram`, `BossJellyfish`, `Hologram`, `Hologram_Scanline`, `Jellyfish`
- **Decor** (5 shaders) : `Kelp`, `Kelp_Sway`, `Portal`, `Portal_Vortex`, `Starfield`, `SmokeTrail`
- **`Resources/Materials/`** : 50+ matériaux dérivés
- **`MaterialController.cs`** : `ApplyToon(root, tint, transparent)` traverse récursif, applique cache (Color, transparent, Texture?) → dédoublonné

### Gap
- ✅ **V6 victoire massive** : V4 n'a aucun shader custom
- ✅ Toon outline équivalent inverted hull dans `OutlineInvertedHull.shader`
- ✅ Boss visuals : Jellyfish + Hologram overlays applicables sur `_ShaderOverlay` child via `MaterialController.ApplyShaderOverlay(root, key, tint)`
- ⚠️ **P2** : `WaterLavaAnimController.cs` 4 fps tile animation OK (`Resources/Tiles/{Water,Lava}/water_frame_01..08.png`) — vérifier en runtime que MapRenderer enregistre bien les water/lava renderers via `RegisterWater`/`RegisterLava`

---

## 7. Lighting & Post-Process

### V4
- Aucun (Phaser canvas 2D)

### V6
- **URP 17.3.0** lit pipeline
- **`PostProcessController.cs`** crée à runtime un GlobalVolume avec :
  - **Bloom** : threshold 0.9 / intensity 0.5 / scatter 0.7
  - **Vignette** : HP-driven (0 si HP>30%, 0.45 si HP=0%)
  - **ColorAdjustments** : per-theme exposure + contrast + colorFilter + saturation (10 thèmes mappés)
- **`Resources/DefaultVolumeProfile.asset`** existe en backup
- **`ThemeAmbientController.cs`** : sun rotation/color/intensity + AmbientMode.Trilight + fog density

### Gap
- ✅ **V6 victoire absolue**
- ⚠️ **P1** : `PostProcessController` crée le volume à runtime via `ScriptableObject.CreateInstance<VolumeProfile>()` — risk fade-in unsync au 1er frame (volume pas encore appliqué). Mike pourrait préférer un volume préassigné dans Main.unity hierarchy
- ⚠️ **P3** : `LightingSettings` baked pas vérifié (probablement realtime only)

---

## 8. Tower visuals (per level upgrades)

### V4
- **15 tile types** : coin, water, cottoncandy, lava, fan, catapult, frost, magnet, portal, tamer, mine, neon, laser, bulle, shovel
- **Pas de niveau visuel L1/L2/L3** dans V4 — c'est un tower defense PvZ-like où chaque tile a 1 visuel seul
- **12 skins shop** achetables tickets (V4 shop)

### V6
- **13 TowerData ScriptableObjects** : Acid, Archer, Ballista, Cannon, Crossbow, Fan, Frost, Mage, Magnet, Mine, Portal, Skyguard, Tank
- **18 Tower GLTFs** dans `Models/Towers/` :
  - **3 with L1+L2+L3** : archer (3), ballista (3), tank (3)
  - **2 with L1+L2** : mage (2)
  - **8 single-tier** : cannon, crossbow, fan, frost, magnet, mine, portal
- **MaterialController.UpdateTint** : peut changer le tint à l'upgrade L3 sans changer le mesh

### Gap
- ✅ V6 a un système **3D upgrade tier visuel partiel** que V4 n'a pas
- ⚠️ **P0 gap** : **6 towers n'ont pas de meshes L2/L3** (cannon, crossbow, fan, frost, magnet, mine, portal, skyguard) → l'upgrade L2/L3 ne changera que le tint
- ⚠️ **P1** : besoin de produire L2+L3 meshes pour ces 7 towers, OU décider que ces tours restent same-mesh + tint change OK design
- ⚠️ **P2** : 19 SkinDef assets pour heroes (`knight_default`, `knight_mage`, `knight_paladin`, `knight_ranger`, `knight_warrior`) + 9 castle skins per-theme + 4 VFX skins (feu/glace/nature/ombre) — système fonctionnel mais coverage 19 vs 12 V4 (gain)

---

## 9. Enemy visuals (V4=17 types, V6=28 SO + 31 GLTFs)

### V4 (`entities/Visitor.js:TYPE_DEFS`)
- **22 types** définis : basic, tank, vip, skeleton, flying, boss, clown, funambule, magicien, boudeur, trompette, enfant, lavewalker, stiltman, magicboss, lavaqueen, carnivalboss, ovniboss, kraken, dragon, netboss + 1 base
- Visuels = **Phaser Container** composé manuellement (torso rect + arms + head circle + hat shapes) + optional sprite si textures.exists(spriteKey)
- 8 sprite keys reconnus : visitor_zombie, visitor_female, visitor_adventurer

### V6
- **28 EnemyData ScriptableObjects** dans `ScriptableObjects/Enemies/` (Basic, Runner, Flyer, Brute, Boss, Imp, Midboss, Shielded, Assassin, SkeletonMinion, AiHub, ApocalypseBoss, BrigandBoss, CorsairBoss, CosmicBoss, CyberBasic/Brute/Flyer/Runner, DesertRunner, DragonBoss, ForestBee, ForestBrute, KrakenBoss, PlainePigeon, SubmarinRunner, WarlordBoss, WizardKing)
- **31 mob GLTFs** dans `Models/Enemies/` : goblin, knight, knightgolden, mob_alpaking, mob_armabee, mob_birb, mob_blob_green/pink/spiky, mob_bunny, mob_cactoro, mob_cyberpunk_2legs/character/flying/large, mob_dino, mob_espace_astronaut, mob_frog, mob_ninja, mob_orc, mob_pigeon, mob_skeleton, mob_squidle, mob_tribal, pirate, soldier, wizard, zombie
- **13 boss GLTFs** dans `Models/Enemies/Bosses/` : apocalypse_orc_skull, cyberpunk_hub_ia, espace_ghost(_skull), foret_mushroom_king, medieval_sorcier_roi, submarin_kraken, volcan_blue_demon/demon/dragon/dragon_evolved/dragon_v2, volcan_yeti
- **Boss aura pulse** : `Enemy.Behaviors.cs:TickBossAura` — scale sine + alpha + colorMPB
- **Animator Controllers** : 206 fichiers dans `Resources/Animations/Controllers/` (! beaucoup)

### Gap
- ✅ **V6 surpasse V4 sur l'inventaire et le rendu 3D**
- ⚠️ **P0 (déjà connu STATUS R7 T-VISUAL-002)** : Animator state machines pas tous validés runtime — risque que enemies idle/spawn pose
- ⚠️ **P2** : V4 `funambule` (walk on water) → V6 pas de mapping clair vers EnemyData (probablement `Assassin` ou `Runner`)
- ⚠️ **P2** : V4 `lavewalker` (immune lava+frost) → V6 pas trouvé d'équivalent direct
- ⚠️ **P3** : V4 `enfant`/`trompette` (special types) → V6 simplifié

---

## 10. Boss visuals (V4=3 vrais, V6=10 Boss SO)

### V4
- **3 vrais boss multi-phases** : magicboss (summon), lavaqueen (lava-trail puddle), carnivalboss (3-phases : phase2 spawn 8 mobs, phase3 immune lava)
- Visuel = container `Visitor` avec `scale` 1.7-2.0 + hat (horn/crown/wig) + couleurs spécifiques
- Pas d'aura ni de shader spécial

### V6
- **10 BossDef ScriptableObjects** (1/monde) : Boss_W1_Brigand → Boss_W10_AiHub
- **13 boss GLTFs** (cf section 9)
- **Boss aura pulse continue** (`Enemy.Behaviors.cs:TickBossAura`) : scale sine + alpha + color
- **Apocalypse boss 4-phase state machine** (`EnemyBossBehaviors.cs:TickApocalypseBoss`) : P1 normal → P2 invul+imps 2s pour 6s → P3 speed×2 → P4 AoE pulse 1.5s
- **Boss shader overlays** disponibles : `bosshologram`, `bossjellyfish`, `phantom` via `MaterialController.ApplyShaderOverlay`
- **JuiceFX OnBossEncountered** : shake + slow-mo `BossSpawnSlowMoScale` ms
- **JuiceFX OnBossDefeated** : shake 0.30 amp 600ms + slow-mo 0.05x 100ms

### Gap
- ✅ **V6 surpasse V4** (10 vs 3, + multi-phase apocalypse + aura pulse + shader overlay)
- ⚠️ **P2** : V4 `lavaqueen` lava-trail puddle (`Visitor.js:_doLavaQueenTrail`) — V6 équivalent ?  Peut-être via PoisonField VFX, mais à vérifier sur DragonBoss/Volcan boss
- ⚠️ **P2** : V4 `carnivalboss` phase3 spawns minions — V6 a `EnemyBossBehaviors.cs` summon mechanics mais coverage à vérifier vs V4 spec

---

## 11. Hero visuals (V4=BluePill seul, V6=5 archetypes + 5 skins)

### V4 (`src-v3/systems/BluePill.js` — note : V4 = src-v2, BluePill est V3 alt)
- V4 Park Defense n'a **pas de Hero personnage jouable** — c'est pure tower defense PvZ, joueur clique tiles
- Pas de hero visual à comparer

### V6
- **5 HeroDef ScriptableObjects** : Barbarian, Knight, Mage, Ranger, Rogue
- **5 knight skins** : default/mage/paladin/ranger/warrior (variants visuels)
- **6 KayKit hero GLTFs** : Barbarian.glb, Knight.glb, Mage.glb, Ranger.glb, Rogue.glb, Rogue_Hooded.glb
- **52 Quaternius alt characters** disponibles si Mike veut switcher hero/skin
- **HeroPortraitController.cs** dans UI

### Gap
- ✅ **V6 introduit un Hero playable que V4 n'avait pas** (différence game design : V6 = action/crowd defense, V4 = PvZ)
- N'est pas un gap V4→V6, c'est un upgrade game design

---

## 12. Audio (V4=10 SFX procéduraux + 3 tracks oscillator, V6=23 SFX OGG + 3 tracks)

### V4 (`src-v2/systems/Audio.js` + `MusicManager.js`)
- **10 SFX procéduraux Web Audio API** : place, coin, gold, hit, fire, kill, explode, win, lose, click, ui (tones + noise filters)
- **3 musiques procédurales oscillator** : menu (Ré mineur arpège 16-step), calm (Do majeur 12-step), intense (8-step + drums + bass + snare)
- Pas de fichier audio (purement procédural)
- Crossfade 1.5s entre tracks

### V6
- **23 SFX .ogg** dans `Audio/SFX/` : achievement, blue_pill, boom, boss_charge, castle_hit, coin_pickup, enemy_die_basic/medium/boss, enemy_hit, gem_gain, hero_shoot, level_up, perk_pick, skin_equip, tower_built/shoot/upgrade, wave_clear/start
- **10 ambient .ogg** dans `Audio/Music/` (1 per world : Ambient_W1_plaine.ogg → Ambient_W10_cyberpunk.ogg) + `Ambient_W1.mp3` legacy
- **MainAudioMixer** dans `Resources/Audio/` avec groupes Master/SFX/Music/UI
- **AudioController.cs** : pool 8 AudioSources, MinReplayInterval 28ms anti-spam, FadeIn/FadeOut music, Play3D spatial, Play3DPitched, PlayLoop("ambient", ...), procedural beep fallback si clip manquant (hash freq déterministe)
- **MusicManager.cs** : 4 tracks (menu, calm, intense, boss) + adaptive layer intensity 0=calm, 1=intense, 2=boss + per-world EQ pitch shift

### Gap
- ✅ **V6 dépasse V4** sur la couverture audio (33 fichiers vs 0 + 13 logical tracks vs 3)
- ⚠️ **P1 (STATUS R7 audit score 72%)** : 23 SFX mapping vers events à vérifier complet — `R7-AUDIO-REMAP` commit `ab8c212` a fixé 3 semantic mappings
- ⚠️ **P1** : Music ambient W1-W10 (10 tracks) ≠ V4 3 logical tracks (menu/calm/intense) — V6 a per-world ambient mais V4 system simpler. Vérifier que `MusicManager.PlayWorld(worldId)` route correctement
- ⚠️ **P2** : V4 `Audio.gold()` cascading 3 tones → V6 `gem_gain.ogg` ne reproduira pas cet effet (mais probablement upgrade)
- ❌ **P2 manquant** : V4 procedural beep `kill()` sawtooth 220Hz → V6 a `enemy_die_basic.ogg` mais le **feeling procédural sera différent** (probablement perçu mieux par les joueurs en OGG)

---

## 13. Cutscenes (V4=ASCII art, V6=text lines only)

### V4 (`src-v2/data/cutscenes.js`)
- 6 cutscenes (1 par monde W1→W6, pas W7-W10 dans V4)
- Structure : `{ title, color, bgTop, bgBottom, panels[], art[] }`
- **`art`** : array de lignes ASCII + emojis utilisé pour mood/ambiance visuelle
- Exemple W1 :
  ```
  "       ☀️",
  "    🌳    🌳",
  "  🎪 → → → → 🚪",
  "    💀💀💀",
  ```
- Choix narratifs dans cutscenes 4/7/10 (V4) sauvegardés en `narrativeChoices`

### V6 (`Data/CutsceneDef.cs` + `ScriptableObjects/Cutscenes/`)
- **10 CutsceneDef** (1/monde, W1→W10) — coverage 10 vs 6 V4 (++)
- Structure : `{ id, titleKey, lines: [{ speaker, textKey, portrait, side }] }`
- Lecture W1 exemple : 4 lignes "Narrateur" texte plat sans portrait

### Gap
- ✅ **V6 a 10 cutscenes vs 6 V4** (+ couverture W7-W10)
- ❌ **P1 manquant** : **V4 ASCII art** (`cutscene.art[]`) **n'est PAS porté en V6**. `CutsceneDef.lines[].portrait` est un `Sprite?` qui pourrait remplacer l'ASCII, mais aucun sprite n'est assigné dans les 10 CutsceneDef actuels
- ❌ **P2 manquant** : V4 `cutscene.color/bgTop/bgBottom` (gradient background) n'a pas d'équivalent dans `CutsceneDef` — fond cutscene V6 est probablement neutre/uniforme
- ⚠️ **P2** : V4 narrative choices (cutscene 4/7/10 → `narrativeChoices` save) — V6 a `save` system mais coverage narrative choice à vérifier dans `CutsceneController.cs`

---

## Liste P0 / P1 / P2 récapitulative

### P0 (bloquant visible parity)
1. **Tower upgrade visual L2/L3 manque pour 7-8 towers** (cannon, crossbow, fan, frost, magnet, mine, portal, skyguard) — upgrade ne change que le tint, pas le mesh
2. **JuiceFX.Flash dépend UIDocument** — si pas dans scène Main.unity, no-op silencieux
3. **Animator state machines runtime non validés** (déjà T-VISUAL-002 STATUS R7)

### P1 (parity gap modéré)
1. **Skybox V8I fix Inspector** : vérifier que les 10 `skybox_*.mat` slots sont assignés sur Main.unity SkyboxController component
2. **Weather prefabs absents** (`Prefabs/Weather/{Type}` 14 paths) → tous les types tombent sur procedural fallback (acceptable mais moins riche que prefabs custom)
3. **Cutscenes ASCII art V4 non porté** → V6 lines plain text seulement. Soit porter en `Sprite?` portrait, soit accepter design change
4. **Audio Music W1-W10 routing** : 10 ambient tracks V6 vs 3 logical V4 — vérifier `MusicManager.PlayWorld(worldId)` route correct
5. **Audio events mapping** : STATUS R7 dit 72% audio coverage, donc 28% events sans SFX — finir la liste

### P2 (polish / nice-to-have)
1. **Weather lightning/magic/leaves V4** absent du V6 enum (substitués par Dust/Ash/Wind/Confetti)
2. **Cutscene background gradient V4** (bgTop/bgBottom) non porté en CutsceneDef
3. **Lavaqueen lava-trail V4** → vérifier équivalent V6 sur DragonBoss/Volcan boss
4. **Carnivalboss phase3 minions spawn V4** → vérifier coverage `EnemyBossBehaviors.cs`
5. **Funambule (walk on water) + lavewalker** V4 types → pas de mapping V6 clair
6. **PostProcess Volume préassigné** vs runtime creation — design choice
7. **Castle damage stages 4 assets** (`castle_intact/cracked/critical/ruined.asset`) wired into Castle prefab swap?
8. **Treasure shimmer VFX** V4 → V6 `TreasureTile.cs` à vérifier visuels

### P3 (cosmétique)
1. **V4 Audio.gold() cascading 3 tones** vs V6 single `gem_gain.ogg`
2. **V4 enfant/trompette/stiltman special types** simplifiés en V6

---

## Conclusion

**V6 est globalement plus avancé que V4** sur 8/13 catégories (shaders, post-process, enemies, towers, bosses, hero, weather, particles/VFX) grâce au pipeline Unity URP 3D + ScriptableObjects + 28 EnemyData + 13 boss meshes.

**V6 est en retard sur 3 catégories** :
1. **Cutscenes ASCII art** non porté (V4 avait emoji-based mood)
2. **Audio events mapping** 72% (R7 audit)
3. **Tower L2/L3 visual upgrade** seulement 5/13 tours ont des meshes haut-tier

**Gaps mineurs** : Foire weather magic effect, lightning particle effect Apocalypse, narrative choices coverage.

Le commit V8I (`735abdc4`) skybox-on-Awake résout le grey background browser. Le suivi STATUS R7 (sprint actif "PUSH-100", 80% raw / 85% weighted parity) est cohérent avec cet audit.

**Recommandation** : Prioriser P0-1 (Tower L2/L3 meshes) et P1-3 (Cutscene ASCII art port) pour combler les gaps visuels les plus criants vs V4.
