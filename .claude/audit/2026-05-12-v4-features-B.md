# Audit V6 Features (gameplay) — B

> Inventaire exhaustif V6 actuel post-R6-02 partial DELETE (2026-05-12).

---

## Entities (8 fichiers — 8 083 LOC total)

| Fichier | LOC | Features distinctes |
|---|---|---|
| Hero.cs | 1 582 | 1. Movement WASD + run (smoothed accel), 2. Auto-attack range R + manual fire, 3. XP/Level system (max level configurable via HeroType.XpCurve), 4. Ultimate (level 10, 60s cooldown, AOE + fan-shot), 5. Perks runtime list (6 slots visibles), 6. HP + respawn 15s + invul flash, 7. Crit (chance + mul + stagger), 8. MultiShot/Pierce/PierceExplode, 9. Lifesteal, 10. WaveRegen, 11. Fireball perk, 12. Ricochet perk (3 bounces), 13. Lightning perk (chain cibles), 14. Glaciation/Combustion/Pyromancie perks, 15. Tower aura buff (FireRateMul + dmg), 16. BluePill channeling, 17. Cast sweep skill (slot 0/1), 18. Move-attack pierce bonus, 19. FirstTowerFree perk, 20. Skin tier 0/1/2 avec tint prefs, 21. Cape cloth simulation, 22. Idle dance animation, 23. AoeBlast public API, 24. Death cinematic + camera zoom, 25. Perk icons billboard flottants, 26. Aura decals sol |
| Tower.cs | 2 574 | 1. Upgrade L1→L2→L3 (coût ×UpgradeMulL2/L3), 2. L3 DPS branch (SlowOnHit, BurnDot, ArmorBreak, Knockback, ChainLightning, FinalExplosion), 3. L3 Utility branch (TankBlockAura, BerserkerActive, BulwarkAura, FreezeOnHit), 4. GuardMode All/AirOnly/GroundOnly, 5. Cluster synergy discount 20%, 6. ResearchTree mul (damage/range/firerate), 7. TowerHP (30 HP, repair, destroy anim), 8. LevelDmgScale par world, 9. CumCost tracker + GetLiveDps (5s window), 10. Synergy halo tint, 11. Recoil anim + muzzle flash light, 12. Chain lightning AOE, 13. SpawnMineRing (mine-cluster), 14. Slow AOE tick (frost/ice), 15. CoinPull magnet behavior, 16. Selection ring + range ring, 17. Cluster highlight, 18. Star row visuels L1/L2/L3, 19. Upgrade arrow (si pas les moyens), 20. Affordable highlight, 21. Aim-line vers cible, 22. Damage icon type, 23. HeroBuff dmg mul aura, 24. Kill streak float text, 25. Sell 80% refund, 26. ElementalTint + L3Tint, 27. SpawnUpgradeRing + Confetti, 28. Skin tier swap mesh, 29. Idle breathing anim, 30. Head aim rotation |
| Enemy.cs | 2 362 | 1. Pathing waypoint (ground + pool), 2. Flyer path (ignore ground, fly height), 3. Stealth (cycle visible/invisible), 4. Shield HP (second health bar), 5. Elite variant (scale×1.15, gold trail), 6. Boss 3-phase (scale, tint LerpBossTint), 7. Apocalypse boss 4-phase invul cycles, 8. Mid-boss, 9. Minion summons (SummonType/burst), 10. AOE blast (radius + telegraph), 11. Charge attack (charge mul, cooldown), 12. Fire breath (dragon boss), 13. Enrage phase (speed mul, summon CD mul, VFX ring), 14. ArmorBreak debuff, 15. Freeze (tint + slow), 16. Burn DoT (visual), 17. Knockback (waypoint rewind), 18. Brigand flag, Corsair flag, Fiery flag, 19. Variant Fast/Tough/Regen/Armored, 20. HP bar (boss=double scale), 21. Shield halo visual, 22. Boss aura glow, 23. Stealth ring VFX, 24. Debuff icons (slow/burn/freeze/armor), 25. Ground decals (slow puddle, fire), 26. Target reticle overhead, 27. Spawn pop-in anim, 28. Boss spawn cinematic (5 rays, rotation), 29. ChaseHero mode, 30. OnDeathStatic event, 31. PressureSpeedMul |
| Castle.cs | 761 | 1. HP formule (100 + 50×√world × difficultyMul), 2. InitWithFormula + Init override, 3. DamageStage (Intact/Cracked/Ruined/Critical) mesh swap, 4. ArmorBreak, 5. HP bar UGS, 6. Regen(amount), 7. GrantBonusHP, 8. Victory banner spawn, 9. Gate open/close anim, 10. Candle particle VFX, 11. Grayscale sur mort, 12. Hit VFX (flash + juice), 13. Overrun check, 14. WasHitThisWave flag |
| Projectile.cs | 361 | 1. Linear + Parabolic trajectoires, 2. Pool release, 3. Pierce multi-enemy, 4. FinalExplosion AOE, 5. ChainLightning on-hit, 6. FreezeOnHit, 7. SlowOnHit, 8. PropagateAoE, 9. ArmorBreak on-hit, 10. Trail renderer + element tint |
| HeroProjectile.cs | 237 | 1. Pierce multi-hit, 2. Fireball mode (AOE sur expiration), 3. Ricochet (bounce vers prochain ennemi), 4. Lightning chain, 5. PierceExplode, 6. Pool reset/reuse |
| MineExplosive.cs | 72 | 1. Init(dmg, radius), 2. Trigger sur proximité ennemi, 3. Explode AOE + VFX |
| TreasureTile.cs | 134 | 1. Init cellule + Init break-tile (value), 2. HeroCollect (pickup), 3. EnemyProximity (enemy steals), 4. Sparkle particles |

---

## Systems (56 fichiers — 11 512 LOC total)

| Fichier | LOC | Purpose | Features |
|---|---|---|---|
| LevelRunner.cs | 846 | Orchestrateur principal | GameState machine (Lobby/Playing/Paused/Won/Lost), spawn Castle/Hero/Treasure/Path, mid-level snapshot/restore, cutscene hooks, gem reward, RestartLevel, SetGameSpeed (0.5×/1×/2×/3×) |
| SaveSystem.cs | 806 | Persistance PlayerPrefs JSON | ProgressData v3, RunState, MidLevelState, EndlessLeaderboard, Skins, MetaUpgrades, Stars, ExportToJson/Import |
| MusicManager.cs | 645 | Audio adaptative | Crossfade, world EQ par world, wave intensity layers, combat layer, sting system, duck, mute |
| WaveManager.cs | 553 | Spawn + progression vagues | StartNextWave, skip window (5s/+30¢), streak bonus (+5%/cap 5), pressure mob W1-W10, EnemyVariant roll, PendingSpawnCount |
| GhostPreviewController.cs | 462 | Preview placement | Ghost semi-transparent pour chaque tower type |
| PlacementController.cs | 454 | Input placement | Click/touch, select tower, place, upgrade, sell, hover cell, compare |
| Synergies.cs | 427 | Calcul synergies | Aura buff (tower→tower), CrossEffect, ApplyToEnemy, Passive, HeroAuraBuff, PullActive synergy, badges HUD |
| PerkSystem.cs | 364 | Gestion perks hero | ApplyPerk, RollChoices (school filter), SetBonus activation, legendaire unlock, stackable |
| AudioController.cs | 364 | SFX | SFX per event (kill, place tower, upgrade, sell, hit, perk) |
| RunMap.cs | 359 | Roguelite run map | Generate(worldId, seed), nodes (Combat/Shop/Event/Rest/Boss), MoveTo, RepairReachability |
| WaveRewardSpawner.cs | 333 | Coffres entre vagues | Chest click → perk/coin/skin/gem choice |
| MapBalance.cs | 327 | Calcul difficulté | EnemyStat/TowerStat tables, LevelMetrics, AutoTune, ExpectedDifficulty |
| MapRenderer.cs | 247 | Rendu grille | BuildForLevel, ApplyWorldTheme (materials par world) |
| PathManager.cs | 242 | Pathfinding | Multi-path support (PathMeta portals), waypoints build |
| TutorialState.cs | 230 | Tutoriel | Phases step-by-step, IsCompleted, arrowGuide |
| TreasureSpawner.cs | 199 | Spawn trésors | IntactCount, CheckEnemyProximity, breakTile spawn |
| EndlessMode.cs | 195 | Mode infini | Scaling ×1.10 HP W30, ×1.15 W50, AppendNextWave, top records |
| SkinSystem.cs | 189 | Cosmétiques | EquipSkin, UnlockAndEquip, ApplyToHero, ApplyToTowers |
| Economy.cs | 187 | Gold + Bank | AddGoldFromKill (+ popup), TransferToBank, ProcessInterestBank (5%/cap 500), FlagCastleDamaged |
| BluePill.cs | 179 | Skill héros | Channeling (hold button) → effet zone, cancel si mouvement |
| LifetimeStats.cs | 177 | Stats vie entière | TodayRuns/Kills/Time, total kills/waves/playtime |
| LootSpawner.cs | 173 | Pickups sol | Spawn health/coin/perk drops sur mort ennemi |
| EventSystem.cs | 172 | Événements roguelite | Roll après vague/fin niveau, choix EventDef (gold, perk, HP, etc.) |
| Achievements.cs | 163 | Succès | TrackEvent, Unlock, IsUnlocked, CompletionRatio (N succès) |
| CoinPullManager.cs | 162 | Magnet pièces | RegisterSource, GetCoinMulAt, SpawnCoinFlyTo, RangeMul, SpeedMul |
| Daily.cs | 160 | Défi quotidien | BuildDailyLevel (seed déterministe par date), DailyLevelSpec |
| EventBridge.cs | 159 | Pont events | Relay EventManager → UI sans couplage |
| EnemyPathingSystem.cs | 156 | Batching paths | Tick() bulk update waypoints |
| BossSystem.cs | 146 | Gestion boss | Detect spawn, publish BossEncountered/HpChanged/Defeated events |
| EnemyPool.cs | 142 | Object pool ennemis | SpawnFromType, ReleaseTyped, PrewarmType |
| TowerHoverController.cs | 122 | Hover tower | Show info card au survol |
| DailyChallenge.cs | 119 | Specs défis | ChallengeModifier (NoPerks/HalfGold/FastEnemies), PerksDisabled, GoldMul |
| HighScores.cs | 111 | Leaderboard local | HighScore per level, maxWaveReached, totalKills |
| TalentSystem.cs | 109 | Talents (méta inter-run) | TowerDamage/HeroPower/GoldIncome levels (max 5), EarnPoint, TryUpgrade |
| RunContext.cs | 109 | Contexte run roguelite | CoinMul, TowerRangeMul, TowerFireRateMul, CurrentLevelIndex |
| HiddenAchievementTracker.cs | 109 | Suivi succès cachés | Détection conditions spéciales |
| GridData.cs | 105 | Données grille | Map rows parsing, walkable/buildable cells |
| DoctrineSystem.cs | 104 | Doctrines (pré-run bonus) | Activate(doctrineId), BuildRunConfig (modifie BalanceConfig) |
| MetaUpgradeSystem.cs | 102 | Upgrades méta | ComputeBonuses (RunBonuses), 10 types upgrades |
| TowerResearchTree.cs | ~80 | Arbre recherche tours | 3 nodes par tower, DamageMul/RangeMul/FireRateIntervalMul |
| ComboSystem.cs | ~60 | Combo kills | KillCount, ComboLevel 1-5, ActiveMultiplier, reset timer |
| StatsTracker.cs | ~50 | Stats run | Per-wave kill tracking |
| Bestiary.cs | ~50 | Bestiaire | RecordKill, IsUnlocked, KillCount par enemy |
| SlowEffectManager.cs | ~40 | Pool slow effets | ApplySlow(enemy, mul, durMs) |
| PathVariant.cs | ~30 | Variantes path | Grid variant selection |
| GridCoords.cs | ~25 | Coords utilitaire | World↔cell conversion |
| KillsPerWaveTracker / WaveHistoryLog / LevelEvents / LevelLoader / PlayerProfile / SchoolRegistry / EventManager / HeroFollowCameraController / HeroProjectilePool / ProjectilePool / AudioMixerController / EnemyAmbientChatter / EnemyHoverController / KeyBindings / RunMap | < 120 each | Utils/helpers | Pools, binding, tracking |

---

## Data SO defs (35 fichiers — 2 038 LOC total)

| Fichier | Assets | Description |
|---|---|---|
| TowerType.cs | 13 towers | Id, Cost, Damage, Range, FireRateMs, Aoe, Pierce, Behavior (Attack/Cluster/Slow/BuffAura/CoinPull), Synergies[], DamageType, 4 enums (TowerBehavior/DamageType/SynergyType/TowerId) |
| EnemyType.cs | 28 enemies | Hp/Speed/Damage/Reward, IsBoss/IsMidBoss/IsApocalypseBoss, IsFlyer, IsStealth, ShieldHP, Summons, Charge, AoeBlast, AoEAttack, Fiery, Brigand, Corsair, EnemyVariant enum |
| HeroType.cs | 5 heroes | Damage/Range/FireRateMs/MoveSpeed, UltCooldownMs, UltAoeRadius, UltFanShotCount, XpCurve[], AvatarColor/Label/StatLines/Tooltip/ApplyArchetype |
| PerkDef.cs | 17 standard + 6 school + 6 set bonus = ~29 | category, tag, rarity, stackable, school, 25+ stat modifiers, 6 special abilities (fireball/ricochet/lightning/pierceExplode/combustion/glaciation), magnetRange/coinFlySpeed, downgrade stats |
| LevelData.cs | 90 levels | World, Level, MapRows[], CellSize, StartCoins, CastleHP, Waves[], Theme, CutsceneIdAtStart, IsEndless, AllowMultiMagnet |
| WaveDef.cs | inline in LevelData | EnemySpawnEntry[], spawnRateMs, breakMs, portalIdx, scaleMul, SpawnPattern |
| BalanceConfig.cs | 1 singleton | TowerDamageMul, LevelScale[], CritChance, SwarmMul, CastleHP formula, Magnet caps, SkipBonus/Window, StreakBonus/Cap, UpgradeMul L2/L3, SellRefundRatio, Treasure values, BankInterest, Combo config, WorldPressure table |
| MetaUpgradeDef.cs | 10 upgrades | castle_hp, coin_multi, coins_start, gem_multi, hero_dmg, hero_fire_rate, hero_range, perk_reroll, tower_discount, xp_boost |
| DoctrineDef.cs | 7 doctrines | alchemist, berserker, merchant, paladin, saboteur, sentinel, trickster + DoctrineModifier[] |
| SkinDef.cs | 5 skins | knight_default/mage/paladin/ranger/warrior |
| BossDef.cs | 10 boss assets | 1 per world (W1 Brigand → W10 AiHub) |
| EventDef.cs | 12 events | ancient_forge, ash_merchant, buried_treasure, frozen_pond, haunted_shrine, lava_geyser, merchant_caravan, raven_omen, rival_hero, starving_traveler, wandering_knight, wisdom_oracle |
| SchoolDef.cs | 5 schools | Elementaire, Mecanique, Mystique, Bestiaire, Strategie |
| AssetRegistry.cs | – | Charge GLTF/textures via Addressables, Towers[], Enemies[], Heroes[] |
| JuiceConfig.cs | 1 | Shake intensities, flash colors, punch scale params |
| AchievementDef / AchievementRegistry | N achievs | Succès avec conditions |
| ModifierDef / ModifierRegistry | N modifiers | Buffs/debuffs templates |
| CutsceneDef / CutsceneRegistry | N cutscenes | Séquences dialogues entre worlds |
| TutorialStepDef / TutorialRegistry | N steps | Guide tutoriel |
| LevelTheme / LevelThemeMaterialConfig | enum 10+ | Plaine, Desert, Forest, Cyber, Volcano, Arctic, Ocean, etc. |

---

## Levels (90 LevelData assets)

- Format : ScriptableObject .asset YAML, 1 fichier par niveau
- Structure : W{world}-{level}.asset, ex. W1-1.asset à W10-9.asset
- Worlds : W1 à W10, 9 levels each = 90 total
- Chaque niveau : MapRows (ASCII grid), 9 waves min (EnemySpawnEntry[]), spawnRateMs, breakMs, theme string, startCoins, CastleHP calculé via formule
- Sample W1-1 : 3+ waves, Basic + Runner + Brute, spawnRateMs 900→650→600

---

## UI (85 fichiers — 20 047 LOC total)

| Fichier | LOC | Features |
|---|---|---|
| L.cs | 1 812 | Localisation statique FR/EN — toutes les strings UI |
| HudController.cs | 1 235 | Gold bar, Castle HP bar, Wave progress dots, Kill count, Wave timer, Skip button (5s window), Break pill (BluePill), Hero HP + ult ring, Boss HP bar inline, Boss intro banner, BankTooltip, Perk badges HUD, WavePreview roster, Restart confirm |
| SettingsPanelController.cs | 792 | Volume sliders (music/sfx), langue, colorblind, graphics, controls rebind |
| RadialMenuController.cs | 583 | Menu radial tower (upgrade L2/L3, sell, guard mode toggle, repair) |
| WorldMapController.cs | 486 | Carte monde W1-W10, unlock progress, level select par world |
| MinimapController.cs | 439 | Minimap (enemies, towers, castle, hero dots) |
| StatsLifetimePanel.cs | 410 | Stats lifetime + stats run (kills, waves, playtime, gold, etc.) |
| TutorialOverlayController.cs | 409 | Overlay tuto step-by-step avec flèches |
| SettingsRegistry.cs | 382 | ShowDamageIcons, ShowFps, ColorblindMode settings |
| EndScreenController.cs | 370 | Victory/Defeat screen, LevelResult display, gem rewards, stars |
| LevelSelectController.cs | 366 | Sélection niveau par world, stars, locked indicator |
| TutorialIntroPanel.cs | 350 | Intro tuto (slides) |
| FloatingPopupController.cs | 329 | Gold/XP/kill float texts |
| TowerSelectMenuController.cs | 325 | Menu sélection tour (tooltip, coût, dps preview) |
| AchievementsPanel.cs | 303 | Panneau achievements grid |
| BossIntroBannerController.cs | 293 | Banner d'intro boss (warning + show cinematic name) |
| HeroPickScreen.cs | 292 | Choix héros avant run (5 heroes, stats, archetype) |
| SceneTransition.cs | 290 | Transitions écrans (fade, wipe) |
| AvatarPickPanel.cs | 287 | Pick d'avatar Warrior/Mage/Ranger |
| PerkPickerController.cs | 285 | Choix perk post-vague (reroll, school filter) |
| TowerToolbarController.cs | 263 | Barre towers bas écran (hotkeys 1-9, afford tint) |
| PerkChoiceOverlay.cs | 258 | Overlay 3 choix perks (on level-up) |
| StatisticsController.cs | 253 | Statistiques de run (DPS/kills par tour, perks, waves) |
| TowerComparePanel.cs | 252 | Comparaison 2 tours (stats side-by-side) |
| KeyBindingsPanel.cs | 246 | Remapping clavier |
| ShopController.cs | 239 | Shop entre runs (méta upgrades, skins, doctrines) |
| HeroSkillBarController.cs | 236 | Barre skills héros (ult + 2 skills + BluePill) |
| AchievementToastController.cs | 234 | Toast pop succès débloqué |
| HeroPortraitController.cs | 230 | Portrait héros HUD + HP bar |
| ComboHudController.cs | 227 | Combo kill HUD (multiplicateur + milestone banner) |
| SkinPickerController.cs | 223 | Choix skin équipé |
| WaveClearedController.cs | 222 | Card "Vague terminée" (gold gain, castle HP) |
| HudPerkBadges.cs | 220 | Badges perks actifs sur HUD |
| TowerInfoPanel.cs | 214 | Panneau info tour sélectionnée |
| TutorialArrowGuide.cs | 210 | Flèches guide tuto |
| ToastController.cs | 210 | Toast système (perk, achievement, info) |
| CurrentRunPerksPanel.cs | 207 | Liste perks actifs run |
| CutsceneController.cs | 206 | Lecteur cutscenes (dialogues, portraits) |
| TowerTooltipController.cs | 204 | Tooltip tower au hover |
| +45 autres fichiers UI | < 200 each | RunMapController, WorldMap, Boss UI, Synergy HUD, Doctrine UI, Level Summary, Run Summary, Save Slots, Virtual Joystick, Wave Banner, Wave Tips, Pause Menu, Credits, Leaderboard, Debug HUD, Bestiary Panel, Help Overlay, Event Choice Overlay, History Log Panel, Talent Panel, etc. |

---

## Visual (27 fichiers — 5 674 LOC total)

| Fichier | LOC | Features |
|---|---|---|
| VfxPool.cs | 1 106 | SpawnImpact, SpawnDeath, SpawnDeathPuff(tier), SpawnExplosion, SpawnCoinBurst, SpawnConfetti(tint), SpawnHitFlash, SpawnLevelUp, SpawnPerkPickup, SpawnMuzzleFlash, SpawnAttackStream, SpawnSpark, SpawnUpgradeBurst, SpawnUpgradeConfetti, SpawnFrost, SpawnPortal, SpawnFireBreath |
| SceneDecor.cs | 430 | Décors statiques par theme (arbres, rochers, props) |
| CameraController.cs | 430 | Follow hero (toggle), castle fallback, map bounds clamp, external pan |
| PathTiles.cs | 429 | Tuiles chemin thémées (BuildForLevel, theme apply) |
| WeatherController.cs | 376 | Météo par theme (rain/snow/sand/fog), ambient audio, ApplyAmbient(worldId) |
| JuiceFX.cs | 257 | Shake(intensity), Flash(color), ShakeOnCritHit, PunchScale, SlowMo |
| AmbientParticles.cs | 202 | Particules ambiantes par theme (fireflies, ash, snow) |
| ParallaxBackground.cs | 179 | Background parallaxe layers (Init/ClearAll par theme) |
| CoinToken.cs | 171 | Animation pièce volant vers HUD (arc + bounce) |
| PathRevealAnimator.cs | 163 | Révélation animée du chemin au départ |
| EnemyHpBar.cs | 161 | HP bar ennemi (boss scale×2), debuff icons |
| MaterialController.cs | 151 | ApplyToon, UpdateTint, ApplyShaderOverlay, ApplyOverrideMaterial |
| ColorblindPalette.cs | 150 | Mode daltonien (ApplyToGameObject) |
| ThemeAmbientController.cs | 149 | Lumières/fog/skybox par LevelTheme |
| PathfinderVisualization.cs | 146 | Debug paths visuels |
| PostProcessController.cs | 142 | URP bloom/vignette par theme, FlashRedVignette |
| FogOfWar.cs | 130 | Fog hors bbox jouable |
| MapDecorations.cs | 126 | Décorations procédurales sur grille |
| StarfieldBackground.cs | 123 | Fond étoilé (world cyber/cosmic) |
| PlacementHighlight.cs | 121 | Highlight cellule buildable au hover |
| AssetVariants.cs | 121 | Variantes visuelles assets (tier skins) |
| AnimationController.cs | 88 | Pilote Animator clips (walk/idle/attack) |
| PathPreviewRenderer.cs | 86 | Preview ligne chemin ennemi |
| Outline.cs | 77 | Inverted hull outline (port static) |
| MusicPulseVisualizer.cs | 76 | Pulse visuel sur beat musique |
| LevelVisualBridge.cs | 61 | Sync visuel theme entre LevelData et controllers visuels |
| WindSway.cs | 23 | Oscillation plantes/décors |

---

## Total

- **Total LOC V6 : ~47 000** (8 083 entities + 11 512 systems + 2 038 data + 20 047 UI + 5 674 visual + ~400 divers)
- **Total features distinctes : ~260** (estimé en comptant rows ci-dessus)

### Features V6 clairement INVENTÉES (absentes de V4 prototype Phaser)

1. RunMap roguelite (nœuds Combat/Shop/Event/Rest/Boss, seed génération)
2. Doctrine system (7 doctrines pré-run modifiant BalanceConfig)
3. Hero playable (WASD, auto-attack, perks, XP/level, ultimate, respawn)
4. TalentSystem inter-run (TowerDamage/HeroPower/GoldIncome nodes)
5. MetaUpgrade system (10 upgrades persist entre runs)
6. DailyChallenge (level déterministe seed par date, modifiers)
7. EndlessMode avec scaling W30/W50
8. Skin system (5 skins hero, équipement par targetType/targetId)
9. AchievementSystem (Unlock, TrackEvent, CompletionRatio)
10. Bestiary (RecordKill, IsUnlocked par enemy)
11. CutsceneSystem (entre worlds, dialogues)
12. BluePill channeling skill
13. TreasureTile (ennemi vole le trésor si touche)
14. Combo kill system (multiplicateur 1→5)
15. Tower research tree (3 nodes par tour, persistant)
16. L3 branch DPS vs Utility (10+ effets par branche)
17. BulwarkAura / TankBlockAura (L3 defensive)
18. Interest Bank (5%, cap 500, TransferToBank)
19. PressureMob (mob supplémentaire W1-W10 selon BalanceConfig.WorldPressureTable)
20. Apocalypse boss 4-phase avec invul cycles
21. GLTF asset loading via AssetRegistry (Unity Addressables)
22. ColorblindPalette system
23. VirtualJoystick (mobile)
24. L.cs localisation statique FR/EN (1 812 LOC)
25. FogOfWar hors-bbox

### Features V6 potentiellement MANQUANTES vs V4 (à vérifier)

1. Score par vague (V4 avait scoring live par wave) — présent via ScoreCalc mais UI réduite post-DELETE
2. Menu principal complet (MenuController réduit −694 LOC) — certaines pages peut-être supprimées
3. EndScreenController réduit (−846 LOC) — détail résultat possiblement tronqué
4. HudController réduit (−1 600 LOC) — features HUD incertaines
5. Wave skip bonus UI (SkipWindowSeconds/SkipBonusGold définis mais UI vérifiée partiellement)
6. Perk répertoire complet : 17 standard actuels vs possiblement plus en V4 (SetBonus school perks 6 + 6 = 12 supplémentaires, total 29)
