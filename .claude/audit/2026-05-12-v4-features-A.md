# Audit V4 Features (gameplay) — A

> Inventaire exhaustif V4 (milan project, legacy frozen, src-v3).

## Entities (5 fichiers — 3 445 LOC total)

| Fichier | LOC | Features distinctes |
|---|---|---|
| Hero.js | 837 | 1. Mouvement WASD + run (sprint ×1.8), 2. Auto-attack closest enemy in range, 3. Projectors avec trail particles, 4. MultiShot (spread N projectiles), 5. Pierce (projectile traverse N ennemis), 6. Ricochet (rebond sur N cibles avec dmg decay), 7. Fireball (AoE on hit), 8. Lightning (frappe N cibles supplémentaires par tir), 9. PierceExplode (explosion AoE à consommation), 10. Crit chance/mul + stagger freeze, 11. Glaciation (30% slow 2s sur hit), 12. Combustion (kill laisse trail de feu 2s), 13. Pyromancie (10% chance projectile bonus on kill), 14. ULT 30s cooldown (fan 8 projs + AoE 6.5u), 15. Level XP system (6 niveaux, XP_CURVE), 16. Hero aura buff sur towers proches (fire rate + dmg), 17. FirstTowerFree perk, 18. TowerCostMul perk, 19. Hero skin asset swap, 20. Fly mode debug (heroFlyMode) |
| Tower.js | 1159 | 1. 13 types de tours : archer/tank/mage/ballista/mine/cannon/fan/frost/crossbow/portal/magnet/skyguard/acid, 2. 3 niveaux d'upgrade (damage ×1.5/×2.5, range ×1.2/×1.4), 3. Behaviors : standard/cluster/slow/buffAura/coinPull, 4. FlyerOnly targeting (skyguard), 5. CanHitFlyers flag, 6. AoE on hit, 7. Pierce projectiles, 8. Parabolic arc (cannon), 9. Barrages multiples (cannon L3), 10. ChainExplosion (mine L3), 11. ArmorBreak debuff (ballista/acid), 12. SlowOnHit (cannon+frost synergie), 13. FreezeOnHit (skyguard+frost), 14. PropagateAoE (crossbow+mage), 15. AppliesSlow (crossbow+frost), 16. KnockbackOnHit (crossbow+fan), 17. FinalExplosion (crossbow L3), 18. TankBlockAura (tank L3 — DoT zone), 19. Propeller animation (fan), 20. Synergy halo visuel, 21. Range ring interactif, 22. Icon badge sprite, 23. Tier pip texture, 24. Gold tint L3, 25. Hero buff dmg aura, 26. Event multipliers (range/fire rate), 27. Recoil animation on fire |
| Enemy.js | 920 | 1. 28 types d'ennemis (basic/runner/brute/shielded/midboss/boss/brigand_boss/assassin/warlord_boss/flyer/corsair_boss/imp/dragon_boss/apocalypse_boss/cosmic_boss/kraken_boss/wizard_king/ai_hub + variantes thématiques), 2. Shield HP directionnel (bloqué si angle < 0.6), 3. Stealth cycling (opacity pulsante), 4. Flyer mode (ignorePath + flyHeight + bob), 5. Boss enrage à 50% HP (speed ×1.4, summon CD ×0.6), 6. Minion summon timer (boss), 7. AoE blast timer (corsair/dragon), 8. Apocalypse 4 phases (invulnérable P2, speed×2 P3, AoE pulse P4), 9. Charge sprint (brigand boss), 10. Slow multiplicateur avec timer, 11. Freeze (stoppe le mouvement), 12. Armor break debuff (dmgTakenMul), 13. Hit flash emissive, 14. Floating damage popup sprites, 15. HP bar dynamique (vert→jaune→rouge), 16. Boss aura ring pulsant, 17. Shader overlay (jellyfish/hologram), 18. Lane offset pour multi-path, 19. Death VFX (blood_splat / explosion_big sprites) |
| Castle.js | 310 | 1. HP bar + HP text overlay canvas, 2. Tint rouge < 30% HP, 3. Grayscale si mort, 4. Smoke particles < 50% HP, 5. Danger PointLight rouge pulsant < 20% HP, 6. Theme GLTF par thème (11 thèmes), 7. Castle addons GLTF (espace : antenna/radar, cyberpunk : antenna) |
| BuildPoint.js | 219 | 1. Halo ring coloré (neutral/valid/invalid/occupied/poor), 2. Build fill arc progressif (drain coins au fil du temps), 3. Dedup build points (distance sq.), 4. generateBuildPointsFromGrid (grille adjacente aux path cells), 5. generateBuildPointGrid (legacy perpendiculaire aux paths), 6. detachTower avec remboursement 80%, 7. Halo intent interpolation |

---

## Systems (~28 fichiers — 7 759 LOC total)

| Fichier | LOC | Purpose | Features distinctes |
|---|---|---|---|
| LevelRunner.js | 1333 | Orchestrateur principal | 1. Wave spawning avec SWARM_MUL 1.4×, 2. Wave break timer auto-avance, 3. Grid-map pipeline + legacy path pipeline, 4. Tower building (drain coins en temps réel), 5. Hero buff aura tick (fire rate/dmg proximité), 6. Synergies.resolve() chaque frame, 7. Coin reward par kill (magnet mul + hero coinGainMul), 8. XP gain par kill, 9. Lifesteal castle regen, 10. Gem drops (boss/midboss), 11. Skin drops (boss unique kill), 12. Achievement kills_100, 13. Support mode (swarm ×0.85, coins ×1.2, hp +15%), 14. Modifier aléatoire ou fixe au lancement, 15. Event manager intégré, 16. Showcase tooltip zones, 17. Boss summon handler (event), 18. Boss AoE handler vs hero, 19. FireTrails tick (combustion perk), 20. CristalGlace aura tick |
| EventManager.js | 240 | Événements mid-run | 1. sand_storm (range -25%, speed +15%), 2. lava_surge (1-3 tours inondées, castle dmg 1/2s), 3. carousel_spin (ennemis changent de path 30%), 4. déclenchement on wave start |
| SaveSystem.js | 487 | Persistance localStorage | 1. Gems compteur, 2. Level completion tracking, 3. Achievements, 4. Total kills, 5. Skins owned/equipped (3 catégories), 6. Meta upgrades levels, 7. Run state (roguelike), 8. Daily streak, 9. Support mode flag par level, 10. Version migration v6, 11. Debug save isolée |
| MapGrid.js | 127 | Parse grille ASCII | 1. Cellules : 0=grass, 1=path, P=spawn, C=castle, D=decor, W=water, L=lava, ~=water bridge, ^=lava bridge, R=rock, 2. bbox calcul, 3. cellToWorld |
| MapPathfinder.js | 332 | BFS grille → spline | 1. Multi-path (P→C par BFS), 2. CatmullRom spline, 3. pathMeta castleIdx mapping |
| MapRenderer.js | 830 | Rendu 3D grille | 1. Ground tiles par cellule, 2. Path tile overlays (PathTiles), 3. Water/lava animated shader, 4. Bridge meshes (wood/lava), 5. SceneDecor nature props, 6. Fog dense hors-bbox |
| MapBalance.js | 233 | Outils balance | Analyse densité build points, recommandations waves |
| MapValidator.js | 283 | Validation grille | 1. Au moins 1 spawn P, 2. Au moins 1 castle C, 3. Chemin P→C valide, 4. Warns non-adjacence build points |
| Audio.js | 205 | Effets sonores | 1. sfxHeroShoot, 2. sfxTowerShoot, 3. sfxEnemyHit, 4. sfxEnemyDie (boss/brute/basic), 5. sfxBoom, 6. sfxWaveStart, 7. sfxUlt, 8. sfxNoGold, 9. sfxCancel, 10. Mute global |
| Particles.js | 185 | Système particules | 1. emit() burst de particules, 2. emitColored() par type ennemi, 3. emitSprite() (sprites Flux : blood_splat, explosion_big, sparkle_gold), 4. Pool de meshes réutilisables |
| JuiceFX.js | 51 | Effets écran | 1. flash(color, durationMs) — overlay couleur écran, 2. shake(intensity, durationMs) — screen shake, 3. Évènement crowdef:screen-shake |
| Weather.js | 143 | Météo par thème | 1. Nuages flottants (plaine/desert), 2. Spores (foret), 3. Sable (desert/storm), 4. Sky gradient billboard, 5. Transition météo sur changement thème |
| SceneDecor.js | 333 | Décors de scène | 1. placeNatureProp (arbre/rocher/buisson selon thème), 2. THEME_PALETTE (couleurs par thème), 3. Placement aléatoire seeded sur cells D/T |
| BluePill.js | 211 | Téléportation héros | 1. Canal 2s stationnaire, 2. Beam + aura + rings VFX, 3. Annulation si mouvement, 4. Téléportation vers spawn initial |
| Tutorial.js | 229 | Tutoriel | 1. Steps séquentiels avec conditions, 2. Highlights UI elements, 3. Progression auto, 4. tutorialFlags dans save |
| RunMap.js | 168 | Mode roguelike map | 1. Génération procédurale 7 actes (seed), 2. Node types : combat/elite/mystery/shop/rest/boss, 3. Edges avec reachability vérifiée, 4. Boss pool par acte, 5. Elite nodes (swarm ×1.3) |
| MusicManager.js | 228 | Musique adaptative | 1. Tracks : menu/combat/boss/victory, 2. Cross-fade, 3. Volume control, 4. Mute |
| AssetLoader.js | 293 | Chargement assets | 1. GLTF loader avec cache, 2. Manifest JSON, 3. Progress callback, 4. Fallback gracieux |
| AssetVariants.js | 106 | Variantes thématiques | applyTheme() — reteinte/clone enemy basic selon thème courant |
| AnimationController.js | 55 | Animation GLTF | 1. play(name, fade), 2. has(name), 3. tick(dt), 4. dispose() |
| Path.js | 17 | Spline legacy | buildPath() — CatmullRom depuis array points |
| PathTiles.js | 600 | Tiles de chemin | 1. Meshes segments droits/courbes/T/+ selon voisins, 2. bridge wood/lava materials |
| PathVariant.js | 124 | Variantes path | isPathLike(), getPathVariant() selon contexte voisins |
| Shaders.js | 445 | Shaders custom | 1. createWaterMaterial (animated UV scroll), 2. createLavaMaterial (animated), 3. createJellyfishMaterial (pulsant), 4. createHologramMaterial (scanlines), 5. disposeShader() |
| ToonMaterial.js | 79 | Style toon | 1. applyToonToScene() — MeshToonMaterial, 2. darkenHex(), 3. Gradient texture shared |
| Outline.js | 50 | Contour noir | addOutlineToScene() — hull inversé scale 1.02-1.08 back-face |
| Synergies.js | 276 | Synergies tours | 1. 11 synergies décrites, 2. resolve() chaque frame (aura/applyToEnemy/passive/crossEffect), 3. getCoinMulAt() magnet positional, 4. Fallback behavior pour tours sans synergies |
| Daily.js | 67 | Défi quotidien | 1. Seed date → level procédural, 2. 5 vagues générées depuis pools, 3. dateKey YYYY-MM-DD |
| Device.js | 29 | Détection device | isMobile(), hasTouchScreen() |

---

## Data definitions

| Fichier | Count | Description |
|---|---|---|
| perks.js | 248 LOC, 17 perks PERKS | + 6 SET_BONUSES (foudre/sang/pierre/feu/vide/or, seuil 3 perks par tag) + rollPerkChoices() avec transform garanti last chance |
| schools.js | 135 LOC, 3 écoles | feu/givre/maconnerie + 6 SCHOOL_PERKS exclusifs (2 par école) + set bonus auto au départ |
| skins.js | 211 LOC, 17 skins | 3 catégories (hero/castle/vfx) — 5 hero achetables, 4+ boss drops, skins castle et vfx avec bonus stats |
| modifiers.js | 68 LOC, 8 modifiers | 4 cursess + 4 blessings (appliqués au lancement run ou random) |
| metaUpgrades.js | 167 LOC, 10 meta upgrades | 3 tiers, 3 niveaux par upgrade, coût en gems, reset gems |
| events.js | 242 LOC, 12 run events | Événements narratifs 2-choix pour mode roguelike (marchands, forges, présages) |
| themes.js | 206 LOC, 11 thèmes | plaine/foret/desert/marais/glacier/volcan/foire/espace/submarin/medieval/cyberpunk — couleurs, fog, météo |
| cutscenes.js | 115 LOC, ~10 cutscenes | Textes introductifs par monde (world1..world10) |

---

## Levels (83 fichiers dans data/levels/)

- **Format** : export default object — `id`, `name`, `theme`, `cellSize`, `map` (grille ASCII), `waves.list[]`, `castleHP`, `startCoins`, `heroSpawn`, `briefing`, `forbiddenTowers?`, `bonusTowers?`, `modifier?`
- **Cellules map** : `0`=grass, `1`=path, `P`=spawn, `C`=castle, `D`=decor, `W`=water, `L`=lava, `~`=water bridge, `^`=lava bridge, `R`=rock
- **Worlds** : W1→W10 avec 8 niveaux chacun (world1-1..world10-8) = 80 niveaux campagne
- **Autres** : endless.js, boss_rush.js, daily (procédural), 10 debug levels thématiques, world1-mazetest/showcase/streamtest
- **Difficulté** : castleHP 120 (W1-1) → 450 (W10-8), waves 4→9 vagues, spawnRateMs 900ms→230ms, types d'ennemis progressifs
- **Bonus levels** : non (pas de niveau bonus numéroté hors-monde, mais boss_rush et endless comme modes spéciaux)

---

## UI (8 fichiers — 1 618 LOC total)

| Fichier | LOC | Features distinctes |
|---|---|---|
| BossUI.js | 101 | 1. Banner top avec HP bar fill dynamique, 2. Boss name display, 3. Boss HP bar séparée, 4. Pause cutscene overlay 2s au spawn, 5. Music switch boss, 6. Charging flash state |
| Minimap.js | 171 | 1. Canvas 2D avec chemin(s), 2. Point héros mobile, 3. Ennemis dots colorés, 4. Build points dots, 5. Châteaux icônes, 6. Resize responsive |
| Popups.js | 69 | 1. spawnToast(title, body) — notification disparaît auto, 2. File queue, 3. CSS animations |
| RunMode.js | 567 | 1. School selection UI (3 écoles), 2. Run map procédurale 7 actes affichée, 3. Node click → charge level, 4. Perk choice screen (3 options après level), 5. Run events (2-choix narratifs), 6. Run stats display, 7. Run tutorial panels, 8. Resume run state, 9. formatRunStats() |
| Shop.js | 193 | 1. Meta upgrades par tiers (1/2/3), 2. Pip progress level, 3. Buy avec gems, 4. Reset upgrades, 5. Skin browser par catégorie, 6. Equip skin, 7. Unlock via gems ou drop |
| TickMetrics.js | 112 | 1. FPS counter, 2. Entity counts (enemies/towers/projectiles), 3. Wave info, 4. Game time, 5. Debug overlay toggle |
| Toolbar.js | 141 | 1. Grid 13 tours avec icônes emoji, 2. Forbidden towers grisés, 3. Selected state highlight, 4. Synergy tooltip on hover (tower label + coût + synergies), 5. Pulse animation on click |
| WorldMap.js | 264 | 1. Grid worlds W1-W10 avec lock/unlock, 2. Étoile de complétion, 3. Daily challenge bouton, 4. Run mode (roguelike) bouton, 5. Debug section (10 thème debug levels), 6. Level stats (best wave, etc.) |

---

## Total

- **Total LOC V4** : ~13 800 (entities 3 445 + systems 7 759 + ui 1 618 + data 1 392 + levels ~1 600 estimé)
- **Total features distinctes** : ~180 features identifiées
- **Top 10 features gameplay les plus importantes** (par impact core loop) :
  1. **Grid-map ASCII pipeline** (MapGrid + MapPathfinder + MapRenderer) — fondation de tous les 83 niveaux
  2. **LevelRunner wave system** — SWARM_MUL, spawn, wave break, coin/gem/XP economy
  3. **Hero avec 20+ perks** — mobilité, projectiles spéciaux (pierce/ricochet/fireball/lightning/multishot)
  4. **13 tower types** avec 3 upgrades chacun et behaviors distincts (cluster/slow/coinPull/buffAura)
  5. **11 synergies tour-tour** résolues chaque frame (crossEffect/aura/passive)
  6. **28 enemy types** dont 10 boss avec phases/enrage/summon/AoE spéciales
  7. **RunMap procédurale** (roguelike 3 actes, 7 rows nodes, perk choices)
  8. **SaveSystem** complet (gems, upgrades, skins, runs, achievements, streak daily)
  9. **3 Schools + 17 perks + 6 set bonuses** — run progression hero
  10. **Modifiers (8) + EventManager mid-run** — variété par partie
