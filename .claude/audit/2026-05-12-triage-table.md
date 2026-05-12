# Triage Table — 2026-05-12 (R6-01 Track A)

> Source : git log V6 + `wc -l` + grep V4 (`/Users/mike/Work/milan project/src-v3/`) + `Q1-Q18-arbitrages.md`.
> Reco = **KEEP / KEEP-REFACTO / DELETE / FREEZE**. Mike valide chaque row avant R6-02 exec.

| # | Feature | LOC V6 | Source V4 (path + LOC) | Bloat × | Hors V4 ? | Q-N réf | Reco | Notes |
|---|---|---|---|---|---|---|---|---|
| **A. GOD CLASSES — Hero.cs (2320 V6 / 837 V4 = 2.77×)** ||||||||
| 1 | Hero core (movement, attack, anim, idle) | ~700 | `entities/Hero.js` (full) | 0.8× | NON | — | KEEP-REFACTO | Split en HeroBehavior + HeroAnim + HeroCombat |
| 2 | Hero XP system + curve [50,110,190,250,325] | ~150 | `Hero.js:20-21,83,197` | 1× | NON | — | KEEP | Match V4 exact |
| 3 | Hero Ultimate ability R + AoE | ~200 | `Hero.js:699 crowdef:ult-fired` | 2× | NON | — | KEEP-REFACTO | V4 a Ult mais V6 ~2× bloat (charge-up + shockwave) |
| 4 | Hero cinematics pack (charge-up 0.5s + respawn 1.5s + footstep dust + swing arc + idle weapon glow + damage screen edge) | ~400 | absent V4 | ∞ | OUI | — | DELETE | 60% du bloat Hero.cs. 6 features VFX inventées. |
| 5 | Hero crown milestone levels 10/20/30 | ~80 | absent V4 | ∞ | OUI | — | DELETE | Visual invention |
| 6 | Hero kill counter + finisher cinematic boss | ~120 | absent V4 | ∞ | OUI | — | DELETE | |
| 7 | Hero perks + sets integration (PERKS, SET_BONUSES, SCHOOL_PERKS) | ~250 | `Hero.js:10-11 imports` | 1.5× | NON | — | KEEP-REFACTO | V4 legit, wire à valider |
| 8 | Hero skin system | ~100 | `Hero.js:26 skinAsset` | 1× | NON | — | KEEP | |
| 9 | Hero portrait UI (HP ring + flame + perk badges + tooltip + XP bar) | ~600 (HeroPortraitController.cs) | absent V4 (UI top-bar XP only) | ∞ | OUI | — | DELETE-REFACTO | Garder XP bar simple top-left, supprimer ring/flame/badges/tooltip |
| **B. GOD CLASSES — Tower.cs (2970 V6 / 1159 V4 = 2.56×)** ||||||||
| 10 | Tower core combat (Fire, target, projectile, range) | ~600 | `entities/Tower.js` core | 0.8× | NON | — | KEEP-REFACTO | Split TowerCombat + TowerVisual + TowerUpgrade |
| 11 | Tower upgrade L1→L3 + L3 hybrid branches | ~250 | V4 partial | 1.5× | NON | Q8/Q10 | KEEP-REFACTO | Q10 1-click L3, Q8 refund 80% à vérifier |
| 12 | Tower XP system +5% dmg / 10 kills | ~120 | absent V4 (grep neg) | ∞ | OUI | — | DELETE | Hors spec |
| 13 | Tower targeting priority cycle UI (First/Last/Strongest/Weakest/Closest) | ~150 | absent V4 | ∞ | OUI | — | DELETE | Inventé, V4 = auto-closest |
| 14 | Tower L4 elite tier (gold tint + sparkles) | ~80 | absent V4 | ∞ | OUI | — | DELETE | |
| 15 | Tower research panel + OnResearchUnlocked event | ~200 | absent V4 | ∞ | OUI | — | DELETE-or-FREEZE | Mike: tu veux research tree ? |
| 16 | Tower lightning beam zig-zag + chain to 2 | ~160 | absent V4 (V4 projectile only) | ∞ | OUI | — | DELETE | |
| 17 | Tower windup squash 0.1s + aim wobble Perlin | ~90 | absent V4 | ∞ | OUI | — | DELETE | |
| 18 | Tower idle breathing + shimmer pulse + elemental tint + selection ring + hover highlight + upgrade arrow + star row + target reticle | ~400 | absent V4 | ∞ | OUI | — | DELETE | 8 features VFX inventées groupées |
| 19 | Tower muzzle flash + hit confirmation + kill counter +1 + projectile trail variants + select sound + upgrade ring + upgrade confetti | ~350 | absent V4 (muzzle minimal V4) | ∞ | OUI | — | DELETE | Garder muzzle flash basique éventuellement |
| **C. GOD CLASSES — Enemy.cs (2806 V6 / 920 V4 = 3.05×)** ||||||||
| 20 | Enemy core (move, HP, damage, death) | ~500 | `entities/Enemy.js` core | 1× | NON | — | KEEP-REFACTO | |
| 21 | Boss enrage aura (<30% HP) | ~80 | `Enemy.js:312-322` | 1× | NON | Q11 | KEEP | V4 legit |
| 22 | Boss AoE attack pattern + minion spawner 60% HP | ~150 | V4 likely | 1.5× | NON | — | KEEP-REFACTO | Vérifier match V4 |
| 23 | Enemy variant trails (Fast=blue, Tough=brown, etc.) | ~100 | absent V4 (V4 = base color only) | ∞ | OUI | — | DELETE | |
| 24 | Enemy spawn telegraph 1s ground glow + boss horn | ~130 | absent V4 | ∞ | OUI | — | DELETE | |
| 25 | Enemy ground crack VFX boss/elite | ~80 | absent V4 | ∞ | OUI | — | DELETE | |
| 26 | Enemy hit blood splash + crit slow-mo + screen-shake | ~140 | absent V4 | ∞ | OUI | — | DELETE | |
| 27 | Enemy death ragdoll fall + dust puff | ~80 | V4 has SpawnDeath simple | 2× | OUI (extension) | — | DELETE | Keep basic SpawnDeath only |
| 28 | Enemy elite gold star marker + glow aura | ~85 | absent V4 | ∞ | OUI | — | DELETE | |
| 29 | Enemy boss epic healthbar phase markers + name + smooth fill | ~230 | V4 had BossUI.js | 1.5× | NON | — | KEEP-REFACTO | V4 BossUI existe |
| **D. GOD CLASSES — Castle.cs (1313 V6 / 310 V4 = 4.23×)** ||||||||
| 30 | Castle core (HP, gate, damage) | ~250 | `entities/Castle.js` | 0.8× | NON | Q14 | KEEP-REFACTO | Q14 floor HP W1-1 conflit |
| 31 | Castle HP formula 100+50√W×diffMul + override JSON | ~80 | spec D1-04 | 1× | NON | Q12 Q14 | KEEP | Q12 OK, Q14 à trancher (120 vs 200) |
| 32 | Castle siege debris < 30% HP + repair anim + blood splatter decals + ambient candles per world + gate close bounce + metallic gate material + world decor W5-W8 | ~600 | partial V4 (`SceneDecor.js decor_medieval_candles`) | ∞ | partiel | — | DELETE | Garder candles via SceneDecor port, jeter le reste |
| 33 | Castle screen shake intensified by hit magnitude + heavy audio | ~80 | absent V4 | ∞ | OUI | — | DELETE | |
| **E. SYSTEMS HORS V4 OU TRÈS BLOATED** ||||||||
| 34 | Doctrines (DoctrineDef + DoctrineRegistry) | ~150 | aka `data/schools.js` (135L) ? | 1.1× | NON (= Schools) | — | KEEP-REFACTO | Confirmer si Doctrines = Schools rename |
| 35 | Schools (SchoolDef) | ~80 | `data/schools.js:135L` | 0.6× | NON | — | KEEP | V4 legit |
| 36 | Perks (PerkDef + PerkSetBonusDef + PerkRegistry) | ~200 | `data/perks.js:248L` | 0.8× | NON | — | KEEP | V4 legit |
| 37 | Skins (SkinDef + SkinRegistry) | ~100 | `data/skins.js:211L` | 0.5× | NON | — | KEEP | V4 legit |
| 38 | Achievements (AchievementDef + Registry + unlock toast slide-in) | ~250 | partial V4 (SaveSystem flags ?) | 1.5× | partiel | — | KEEP-REFACTO | V4 a SaveSystem mais toast = invention |
| 39 | Modifiers (8 .asset : magnetic_storm, rising_lava, etc.) | ~120 | `data/modifiers.js:68L` + run logic | 1.8× | NON | — | KEEP-REFACTO | V4 legit |
| 40 | Events (EventDef + EventRegistry) | ~150 | `data/events.js:242L` + `EventManager.js:240L` | 0.3× | NON | — | KEEP | V6 sous-implémenté (vs V4) |
| 41 | MetaUpgrades (MetaUpgradeDef + Registry) | ~120 | `data/metaUpgrades.js:167L` | 0.7× | NON | — | KEEP | V4 legit |
| 42 | Difficulty slider Easy/Normal/Hard/Brutal (0.7/1.0/1.3/1.6) | ~140 | absent V4 (V4 = difficultyMul per-level 1.0..1.5) | ∞ | OUI | — | DELETE | V4 = par-level mul, pas slider global |
| 43 | Roguelike mode stub + RandomMapGenerator | ~300 | `ui/RunMode.js` (size?) | partial | NON | — | FREEZE | Investissement fait, pas en chemin critique R6 |
| 44 | WorldMap scene (bookmark + sort + thumbnails) | ~250 | `ui/WorldMap.js` | partial | NON | — | KEEP-REFACTO | V4 legit |
| 45 | Tutorial popup BIENVENUE first launch | ~120 | `systems/Tutorial.js:229L` | 0.5× | NON | — | KEEP-REFACTO | V4 a Tutorial.js mais V6 popup intrusive style ≠ V4 |
| 46 | Synergies (SynergyHudPanel + activation flash) | ~200 | `systems/Synergies.js:276L` | 0.7× | NON | — | KEEP | V4 legit |
| 47 | BluePill / DoctrineButton hero ability | ~150 | `systems/BluePill.js:211L` | 0.7× | NON | — | KEEP | V4 legit |
| **F. POLISH / HUD INVENTIONS** ||||||||
| 48 | HUD popups inventés (wave countdown big 3-2-1-GO + wave intro banner slide-in + wave clear summary + perfect streak banner + level start banner cinematic + tutorial BIENVENUE popup + difficulty selector) | ~700 (HudController bloat) | absent V4 (timer auto V4) | ∞ | OUI | — | DELETE | Mike feedback : HUD ≠ V4 → cleanup massif |
| 49 | HUD permanent badges (perk-badges-row + perk-set-progress-row + synergy-hud-panel + combo-multiplier + coin rotation + gold rolling counter + castle HP icon pulse + wave timer red pulse + wave streak particles + enemy intel hover popup + wave preview mini-cards + tower placement preview range + boss epic healthbar + kill float text) | ~900 | partial V4 (V4 mini badges only) | ∞ | OUI | — | DELETE | Conserver gold counter top-right + wave label only |
| 50 | MainMenu seasonal accents particles + animated gradient bg + Play button hover particles + logo splash + credits screen + achievement showcase row + daily challenge button + hardcore button conditional | ~500 (MenuController) | partial V4 (V4 minimal menu) | ∞ | OUI | — | DELETE | Garder splash logo + main menu basique seulement |
| 51 | EndScreen stars celebration + share + quick retry + replay highlights + confirm modal exit | ~300 (EndScreenController) | V4 = simple summary | ∞ | partial | — | DELETE-or-FREEZE | Garder stars + summary basique |
| 52 | KeyBindingsPanel rebinding UI + close X fix | ~250 | V4 had keymap fix | ∞ | OUI | — | KEEP (fix récent) | Bug fix Mike `38d1588` essentiel |
| 53 | Achievement unlock toast slide-in right + queue | ~140 | absent V4 | ∞ | OUI | — | DELETE | |
| 54 | MusicManager extensions (DuckMusic + combat layer crossfade + world EQ tuning + wave intensity ramp + boss intro swell) | ~250 | `systems/MusicManager.js:228L` | 1.1× | NON | — | KEEP-REFACTO | V4 a MusicManager mais V6 ajouts à valider |
| 55 | VfxPool 8+ spawn methods (SpawnConfetti + SpawnDeath + SpawnImpact + SpawnSpark + SpawnAttackStream + SpawnUpgradeBurst + SpawnUpgradeConfetti + SpawnUpgradeRing + SpawnGroundShockwave + SpawnFootstep + SpawnFloatText) | ~600 | `systems/Particles.js:185L` | 3.2× | partiel | — | KEEP-REFACTO | Garder SpawnDeath + SpawnImpact + SpawnSpark basiques, jeter 5+ inventions |
| 56 | Settings player name TextField + PlayerProfile bind + locale FR/EN | ~200 | partial V4 | 1.5× | partiel | — | KEEP | V4 had locale, refacto OK |

---

## Synthèse

- **DELETE** : ~20 rows = ~5000-6000 LOC à supprimer (60+ features inventées hors V4)
- **KEEP-REFACTO** : ~14 rows = god class split obligatoire (4 entities + Hero perks + Tower combat + Enemy core + Castle core + Synergies wire + Musical Manager + WorldMap + Tutorial + VfxPool + Achievements + Doctrines + EnemyBossUI + Settings)
- **KEEP** : ~10 rows = features V4 fidèles à garder telles quelles (XP curve Hero + Skins + Schools + Perks + MetaUpgrades + Events + Castle HP formula + Q3 Magnet + KeyBindings fix + BluePill)
- **FREEZE** : 2 rows = Roguelike RandomMap + Tower research panel (incertain, investment fait)

LOC delta estimé après R6-02 DELETE = **-5500 à -7000 LOC**. Hero/Tower/Enemy/Castle reviennent vers 800-1000 chacun (vs 2300-2970). Total entities ≈ 4500 LOC (vs 10213 actuel, vs 3445 V4 — proche cible).

## 3 décisions Mike — TRANCHÉES ✅

1. ✅ **Castle HP W1-1 = 200** (Q14 spec D1-04 ground truth, override STATUS.md "120").
2. ✅ **Tower research panel (row 15)** : KEEP-REFACTO. Rebrand "TowerResearch" → "MetaUpgrades" pour aligner V4 vocabulary (`data/metaUpgrades.js`). Intégrer dans R6-04 Q-N spec. 292 LOC propres (TowerResearchTree 93 + TowerResearchPanel 199).
3. ✅ **Row 43 Roguelike SPLIT** : KEEP-REFACTO RunMap.cs/RunMapController.cs (port V4 `ui/RunMode.js` 567 + `systems/RunMap.js` 168), DELETE RandomMapGenerator.cs 79 LOC (invention pure, V4 = maps prédéfinies).
4. ✅ **Row 51 EndScreen SPLIT** : KEEP-REFACTO core EndScreen ~250 LOC (display summary), DELETE 4 extensions (~966 LOC : stars celebration confetti, share button, quick retry, confirm modal exit) + DELETE ReplayRecorder.cs 128 LOC (invention).

## LOC delta révisé après R6-02 DELETE

- DELETE pass total : **~6500-8000 LOC** (au lieu de 5500-7000 initial)
- Inclut : 60+ features inventées hors V4 + EndScreen extensions + RandomMapGenerator + ReplayRecorder
- Entities target post-DELETE : Hero 2320→~800, Tower 2970→~1000, Enemy 2806→~900, Castle 1313→~500
- Total entities ≈ 3200 LOC (≤ 3445 V4 — alignement enfin)
