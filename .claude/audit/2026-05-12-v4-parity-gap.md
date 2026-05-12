# V4 Parity Gap — Diff consolidé (Audit A + B + C)

> Sources : `2026-05-12-v4-features-A.md` (V4 gameplay), `B.md` (V6 gameplay), `C.md` (look & feel).
> Pivot Mike 14h48 : priorité absolue parité V4. V6 features en + OK si plus joli/propre.

---

## 0. Synthèse globale

| Axe | V4 | V6 | Verdict |
|---|---|---|---|
| LOC gameplay | ~13 800 | ~47 000 | V6 **3.4× bloated** (post R6-02 partial) |
| Features gameplay distinctes | ~180 | ~260 | V6 ajouté ~80 features hors V4 |
| Textures Flux Schnell custom | 75 PNG (10 ground + 10 path + 10 sky + 22 VFX + 4 water + 4 lava + 5 pilot) | **3 PNG** | **GAP CRITIQUE** : 40 textures custom absentes côté Unity |
| Pipeline texture | `tools/gen_textures.py` Flux Schnell local + manifest JSON | aucun pipeline | À reconstituer en V6 |
| Shaders custom | 8 shaders Three.js (water/lava/portal/holo/kelp/star/jelly/smoke + toon) | 12 shaders Unity (V4 +outline +boss + snow +scanline) | V6 **supérieur** sauf textures sources |
| Post-processing | OutlinePass uniquement | URP bloom + vignette + color grading | V6 **supérieur** |
| Lighting | Hemisphere + 10 sun colors/thème | Flat ambient + sun colors | V4 partiel mieux (sky/ground separate) |
| Particles VFX sprites | 22 sprites Flux PNG | **0** (procédural Unity PS) | **GAP** — explosion/sparkle/glyph absent |

**Honest iso V4** : ~**55-65%** (gameplay) / ~**40-45%** (look & feel finition).

---

## 1. Features gameplay — Table par catégorie

Légende : ✅ PRESENT / ⚠️ PARTIAL / ❌ MISSING / 🆕 INVENTED-V6-ONLY

### Entités

| Feature V4 | V6 status | LOC port estimé | Complexité | Notes |
|---|---|---|---|---|
| Hero perks (Pierce/Ricochet/Fireball/Lightning/MultiShot/PierceExplode + Glaciation/Combustion/Pyromancie) | ✅ | — | — | V6 a tout porté + extensions |
| Hero ULT 30s cooldown fan 8 projs + AoE | ⚠️ | 30 | low | V6 lvl 10 + 60s CD (vs V4 30s sans lvl gate), V4 ULT 8 projs, V6 4-fan |
| Hero aura buff tours (fire rate + dmg proximité) | ✅ | — | — | OK |
| 13 tower types | ✅ | — | — | OK (acid/skyguard inclus) |
| Tower behaviors (cluster/slow/buffAura/coinPull) | ✅ | — | — | OK |
| Tower upgrades L1-L3 + L3 branches DPS/Utility | ✅ | — | — | OK |
| 11 synergies tour-tour (resolve frame) | ✅ | — | — | OK |
| 28 enemy types (incl 10 boss + variantes thématiques) | ⚠️ | 50 | medium | V6 a 28 base, vérifier variantes thématiques (assassin/warlord/corsair/imp/dragon/apocalypse/cosmic/kraken/wizard_king/ai_hub) toutes implé |
| Boss phases (enrage 50% / Apocalypse 4 phases / charge sprint / AoE pulse) | ⚠️ | 80 | medium | Boss base OK, Apocalypse 4 phases à vérifier complet, charge sprint, AoE pulse |
| Shield HP directionnel (block si angle <0.6) | ✅ | — | — | OK |
| BuildPoint halo + fill arc + dedup | ✅ | — | — | OK GhostPreviewController |

### Systèmes

| Feature V4 | V6 status | LOC port estimé | Complexité | Notes |
|---|---|---|---|---|
| LevelRunner wave system + SWARM_MUL 1.4 | ✅ | — | — | WaveManager OK |
| Grid-map ASCII pipeline (MapGrid 127 + Pathfinder 332 + Renderer 830) | ⚠️ | 200 | high | V6 MapRenderer 247 + GridData 105 + PathManager 242 — vérifier tile rendering fidèle (Water/Lava bridges, fog dense hors-bbox) |
| PathTiles 600 LOC (segments + courbes + T + + bridges) | ⚠️ | 400 | high | V6 a PathVariant 30 LOC seulement — **gap massif** pour visuel chemins |
| Weather (clouds/spores/sand/sky gradient billboard, transition thème) | ❌ | 150 | medium | V6 absent (sauf dust.png) |
| SceneDecor (placeNatureProp arbres/rochers/buissons selon thème, palette) | ⚠️ | 150 | medium | V6 absent en .cs (probablement via Quaternius assets directs) |
| AssetLoader/Variants (GLTF cache + manifest + applyTheme reteinte enemy basic) | ✅ | — | — | V6 AssetRegistry Addressables OK |
| Audio sfx (heroShoot/towerShoot/enemyHit/enemyDie/boom/waveStart/ult/noGold/cancel + mute) | ✅ | — | — | OK |
| Particles emit/emitColored/emitSprite (22 sprites Flux) | ⚠️ | 200 | medium | V6 SpawnX OK mais **22 sprites VFX absent**, procédural Unity PS only |
| JuiceFX flash/shake/event screen-shake | ✅ | — | — | OK (event-based) |
| Synergies resolve frame + getCoinMulAt + fallback | ✅ | — | — | OK |
| EventManager mid-run (sand_storm + lava_surge + carousel_spin) | ⚠️ | 80 | low | V6 EventSystem narratif 12 events — vérifier les 3 dynamiques (V4 stylé mid-wave) |
| SaveSystem (gems/levels/achievs/kills/skins/meta/runs/daily/migration) | ✅ | — | — | V6 SaveSystem 806 LOC OK |
| RunMap procédurale 7 actes (nodes combat/elite/mystery/shop/rest/boss) | ✅ | — | — | RunMap.cs 359 OK |
| BluePill téléportation hero (channel 2s + beam + aura + rings + cancel sur mouvement) | ✅ | — | — | OK |
| Tutorial steps séquentiels conditions + highlights UI + tutorialFlags | ✅ | — | — | TutorialState 230 OK |
| MusicManager tracks menu/combat/boss/victory + cross-fade | ✅ | — | — | OK |
| Daily challenge seed date → level procédural 5 vagues | ✅ | — | — | OK |
| Device.isMobile / hasTouchScreen | ✅ | — | — | OK |
| MapBalance expectedDifficulty / autoTune | ✅ | — | — | OK |
| MapValidator 1 spawn 1 castle + chemin valide + warns adjacence | ✅ | — | — | OK |

### Data definitions

| Feature V4 | V6 status | Count V4 | Count V6 | Notes |
|---|---|---|---|---|
| Perks standard + SET_BONUSES (17 perks + 6 sets) | ⚠️ | 17+6 | 29 (17 std + 6 school + 6 set) | V6 a plus, vérifier les 17 V4 originaux tous présents |
| Schools + SCHOOL_PERKS (3 écoles V4 : feu/givre/maconnerie + 6 perks exclusifs) | ⚠️ | 3 schools | 5 schools | V6 a 5 (Elementaire/Mecanique/Mystique/Bestiaire/Strategie), V4 3 — **noms différents**, vérifier mapping fidèle |
| Skins (3 catégories hero/castle/vfx, 17 skins) | ⚠️ | 17 skins | 5 skins | V6 a 5 hero only — **gap castle/vfx skins** |
| Modifiers (8 = 4 curses + 4 blessings) | ⚠️ | 8 | N | V6 ModifierRegistry, vérifier count + types fidèles |
| MetaUpgrades (10 upgrades 3 niveaux gems) | ✅ | 10 | 10 | OK MetaUpgradeDef |
| Events run (12 narratifs 2-choix) | ✅ | 12 | 12 | OK EventDef |
| Themes (11 thèmes : plaine/foret/desert/marais/glacier/volcan/foire/espace/submarin/medieval/cyberpunk) | ⚠️ | 11 | ~10 | LevelTheme enum, vérifier "foire" présent |
| Cutscenes (10 worlds intro) | ✅ | 10 | N | CutsceneDef OK |
| 80 levels campagne + endless + boss_rush + daily + debug | ✅ | 80+ | 90 | OK |

### UI

| Feature V4 | V6 status | LOC port estimé | Notes |
|---|---|---|---|
| BossUI (banner top + HP bar + boss name + cutscene 2s + music switch) | ⚠️ | 50 | V6 a boss banner mais simplifié post R6-02-051. Vérifier music switch + cutscene 2s |
| Minimap (canvas chemin + hero + ennemis + build points + castles) | ✅ | — | V6 MinimapController OK |
| Popups (spawnToast title+body, queue) | ✅ | — | V6 FloatingPopupController + Toast OK |
| RunMode UI (school select + run map 7 actes + perk choice + run events + tutorial panels) | ✅ | — | RunMapController OK |
| Shop (meta tiers + buy gems + reset + skin browser + equip) | ⚠️ | 50 | V6 a skin system, vérifier shop UI complet |
| TickMetrics (FPS + entity counts + wave info + debug overlay) | ✅ | — | V6 RuntimeProfilePanel OK |
| Toolbar (13 tours grid + forbidden + selected + synergy tooltip + pulse) | ✅ | — | V6 TowerToolbarController OK |
| WorldMap (W1-W10 + lock/unlock + stars + daily + run mode + debug section + level stats) | ✅ | — | V6 WorldMapController OK |

### V6 INVENTED-V6-ONLY (Mike accepte)

| Feature V6 invented | Verdict Mike |
|---|---|
| URP post-processing (bloom + vignette + color grading) | ✅ KEEP (V6 amélioration look & feel) |
| Toon shaders étendus (Snow + Boss Hologram + Boss Jellyfish + Scanline variant) | ✅ KEEP |
| Outline inverted hull (vs OutlinePass V4) | ✅ KEEP (technique Unity-native) |
| Combo system multiplier (kill chain) | ✅ KEEP si pas nuisible |
| TowerResearchTree (3 nodes/tower = meta-upgrade per tower) | ✅ KEEP (rebrand MetaUpgrades) |
| ComboSystem KillCount + multipliers | ✅ KEEP |
| Achievements / HiddenAchievementTracker | ✅ KEEP |
| Bestiary | ✅ KEEP |
| TalentSystem (3 talents inter-run) | ⚠️ Vérifier vs MetaUpgrades V4 |
| Doctrines (7 pré-run modifiers) | ✅ KEEP (extension V4 modifiers) |
| LifetimeStats / KillsPerWaveTracker | ✅ KEEP |
| HighScores leaderboard local | ✅ KEEP |
| Endless mode scaling W30/W50 | ✅ KEEP |
| WaveRewardSpawner chests | ✅ KEEP |
| Hero damage VFX (post-DELETE certains restent) | ✅ KEEP léger |

---

## 2. Look & feel — Table

| Catégorie | V4 | V6 | Status | Port effort | Priority |
|---|---|---|---|---|---|
| **Textures ground (1024px tileable 10 thèmes)** | 20 PNG Flux | 0 | ❌ MISSING | High (regen pipeline Flux Schnell + Unity import) | **P0** |
| **Textures path (512px tileable 10 thèmes)** | 10 PNG Flux | 0 | ❌ MISSING | High (idem) | **P0** |
| **Skybox equirectangular (2048x1024 10 thèmes)** | 10 PNG Flux | 0 (tint procédural) | ❌ MISSING | Medium (gen + skybox material 6-sides) | **P0** |
| **VFX sprites (sparkle_gold/explosion_big/blood_splat/22 total)** | 22 PNG Flux | 0 (procédural) | ❌ MISSING | Medium (gen + ParticleSystem texture sheet) | **P1** |
| **Water animated frames (4×512px)** | 4 PNG | 0 (Toon_Water_Animated shader) | ❌ MISSING | Low (shader OK, juste frames source) | **P2** |
| **Lava animated frames (4×512px)** | 4 PNG | 0 (Toon_Lava_Animated shader) | ❌ MISSING | Low (idem) | **P2** |
| Hemisphere light (sky=0xb0e0ff ground=0x4a6a3a) | ✅ | ⚠️ Flat ambient | PARTIAL | Low (`RenderSettings.ambientMode = Trilight`) | **P3** |
| Castle PointLight rouge hit | ✅ | ❌ Non porté | MISSING | Low | **P3** |
| PathTiles segments visual (droits/courbes/T/+ bridges wood/lava) | ✅ 600 LOC | ⚠️ PathVariant 30 LOC | PARTIAL | High (rendu chemin majeur visuel) | **P0** |
| Weather effects (clouds/spores/sand/sky gradient billboard, transition thème) | ✅ 143 LOC | ❌ Absent | MISSING | Medium | **P2** |
| SceneDecor nature props placement (arbre/rocher/buisson selon thème) | ✅ 333 LOC | ⚠️ via Quaternius directs probablement | PARTIAL | Medium | **P2** |
| Color grading per theme | ❌ Sun color only | ✅ ColorAdjustments URP | V6 SUPÉRIEUR | — | — |
| Bloom + Vignette HP-driven | ❌ | ✅ URP Volume | V6 SUPÉRIEUR | — | — |
| Outline entities | ✅ OutlinePass | ✅ InvertedHull | ÉQUIVALENT | — | — |
| Toon material 3-step gradient | ✅ | ✅ | ÉQUIVALENT | — | — |
| Shaders custom (water/lava/portal/holo/kelp/star/jelly/smoke) | ✅ 8 shaders | ✅ 12 shaders (+toon snow +boss holo +boss jelly +scanline) | V6 SUPÉRIEUR | — | — |
| Camera follow hero (smooth) | ✅ | ✅ HeroFollowCameraController | OK | — | — |

---

## 3. Synthèse + priorisation backlog R6-PARITY-V4

### Top 5 tickets P0 (critical gameplay impact OU critical visual impact)

1. **R6-PARITY-001 — Pipeline textures Flux Schnell port** : regen 40 textures Flux (ground×20 + path×10 + sky×10) via `tools/gen_textures.py` puis import Unity dans `Assets/Textures/{Ground,Path,Sky}/`. Effort ~4-6h.

2. **R6-PARITY-002 — PathTiles fidèle V4** : porter `src-v3/systems/PathTiles.js` 600 LOC vers Unity (segments droits + courbes + T + + bridges wood/lava avec materials thème). Effort ~6-8h.

3. **R6-PARITY-003 — Skybox per-theme images** : importer 10 skybox equirectangular Flux + créer Unity skybox material per thème + auto-switch on level load. Effort ~3-4h.

4. **R6-PARITY-004 — 22 VFX sprites Flux import** : import sparkle_gold, explosion_big, blood_splat, etc. dans `Assets/Textures/VFX/` + wire dans VfxPool ParticleSystem texture sheet. Effort ~3h.

5. **R6-PARITY-005 — Audit enemy types complets** : vérifier 28 enemies V4 (assassin, warlord_boss, corsair_boss, imp, dragon_boss, apocalypse_boss avec 4 phases, cosmic_boss, kraken_boss, wizard_king, ai_hub) tous présents avec leur behaviors (charge sprint, AoE pulse 4 phases, fire breath dragon, etc.). Effort ~4-6h pour combler les gaps détectés.

### Top 5 tickets P1 (high gameplay/visual impact)

6. **R6-PARITY-010 — Weather effects port** : clouds (plaine/desert), spores (foret), sable (desert/storm), sky gradient billboard, transition météo on thème change. Effort ~4-6h.

7. **R6-PARITY-011 — Castle skins + VFX skins** : V4 17 skins (5 hero + 8 castle + 4 vfx), V6 5 hero only. Importer définitions castle skins (mesh GLTF par thème) + VFX skin overrides. Effort ~5-8h.

8. **R6-PARITY-012 — V4 dynamic EventManager events** : sand_storm (range -25%, speed +15%) + lava_surge (1-3 tours inondées + castle dmg) + carousel_spin (ennemis changent path 30%). Effort ~3h.

9. **R6-PARITY-013 — SceneDecor port** : `placeNatureProp` (arbre/rocher/buisson selon thème), THEME_PALETTE, placement aléatoire seeded sur cells D/T. Effort ~4h.

10. **R6-PARITY-014 — Boss phases complete** : Apocalypse 4 phases avec invul P2 + speed×2 P3 + AoE pulse P4. Charge sprint brigand_boss. Fire breath dragon_boss. Effort ~5h.

### P2 / P3 (nice-to-have)

11. R6-PARITY-020 — Schools mapping V4 (feu/givre/maconnerie) ↔ V6 (5 schools) reconciliation
12. R6-PARITY-021 — BossUI cutscene 2s pause au spawn boss + music switch
13. R6-PARITY-022 — Hemisphere ambient (sky=0xb0e0ff ground=0x4a6a3a) via RenderSettings Trilight
14. R6-PARITY-023 — Water/Lava 4-frame textures source (shaders V6 ready, juste frames)
15. R6-PARITY-024 — Castle PointLight rouge sur hit

### Decisions Mike attendues avant backlog dispatch

1. **Pipeline textures Flux Schnell** : tu veux que je :
   - (a) Lance `tools/gen_textures.py` automatiquement avec ComfyUI:8188 local pour regen les 40 textures ? (cf memory reference_flux_local)
   - (b) Copy directement les PNG existants de `milan project/public/textures/` vers `crowd-defense/Assets/Textures/` ? (plus rapide si fichiers présents)
2. **PathTiles port** : V4 a 600 LOC PathTiles.js — peux-tu confirmer que c'est high prio visuel ? Sinon parking en P1.
3. **Castle/VFX skins** : tu veux les 12 manquants ou seulement quelques uns ?
4. **Schools naming** : V6 5 schools renommés vs V4 3 schools — on revert au mapping V4 strict ou keep V6 extension ?
5. **Mode dispatch R6-PARITY-V4** : supervisé batch / autonome time cap N h ?

---

## 4. Méthodologie

- Audit A : 180 features V4 catalogué (13.8k LOC)
- Audit B : 260+ features V6 catalogué (47k LOC post R6-02 partial)
- Audit C : 75 textures Flux V4 vs 3 textures V6 + diff shaders/lighting/materials/post-fx
- Diff consolidé : 50 rows total (30 gameplay + 17 look&feel + 3 synthèse)
- Time taken : ~25 min (3 agents parallèles + consolidation Opus)

R6-02 DELETE déjà fait : -6464 LOC (16 commits) — bloat réduit, mais textures pas adressées.
