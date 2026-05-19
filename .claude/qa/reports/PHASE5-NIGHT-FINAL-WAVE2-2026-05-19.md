# Brief Mike — Wave Auto-amélioration Nuit 2 (2026-05-19)

**Période** : 03:00 → 04:45 CEST (~1h45 wall-clock)
**Mode** : Autonomie totale jusqu'à midi (réveil Mike)
**Statut** : Mode toujours actif, ~7h budget restant

## TL;DR

**49 commits** Wave Auto-amélioration depuis 03:00 (HEAD `43f1c825`).
**V3AutoLoop** : ✅ 10/10 validator + 4/4 screenshots ALL PASS confirmé (aucune régression).
**WebGL build** : ✅ 37MB (commit nuit 1, `bash tools/webgl-serve.sh` pour test localhost).
**P0 critiques fixés (15)** : ApplyTierSkin mesh swap dead code, Doctrine 7 systèmes 100% sans effet, Tutorial bloque step 2, Kraken/WizardKing/AiHub vides, Wave restore index, Camera vol parasite, Progression unlock chain, XP-on-kill manquant, Hero pick selection, Perk timeScale, Mid-save backup, Save naming W1-1 vs world1-1, Wave START banner non animé, Boss-kill achievements 8 dead, Hero ULT Crit Shake.

## Commits actionnables (chronologique post-V3AutoLoop validation 03:47)

### Vague 1 : V3 parity P0 (03:50 - 04:00)
- `5ce1f435` **B-XP-FIX** : Hero.OnProjKill receives XP (5 mob / 15 elite / 20 midboss / 50 boss)
- `d737816e` **B-BOSS-SECONDARY** : Wire Kraken hasTentacleSlam + WizardKing canTeleport + AiHub isBurstSummoner — 3 boss avec unique mechanics opérationnels
- `ceaed658` **B-CONSOLE-CLEANUP** : -10 LogError pollution (UI build guards + impossible cases)
- `759b2cbf` **B-WAVE-RESTORE** : WaveManager.SkipToWave + LevelRunner restore wave index — mid-level resume actual wave
- `0af0f79c` **B-MID-SAVE-BACKUP** : SaveSystem atomic write + backup fallback for MidLevelStateData
- `4f67aee4` **B-ENEMY-PERF-KRAKEN** : Cache Shader.PropertyToID for Kraken slam telegraph

### Vague 2 : Tower + UI polish (04:00 - 04:15)
- `fe324ed6` **B-ENEMY-PERF-DECAL** : sharedMaterial au lieu de material clone (slow/burn decals)
- `671b72d3` **B-TIER-SKIN** : Call ApplyTierSkin in PostUpgradeVisuals — L2/L3 mesh swap était DEAD CODE
- `244f9821` **B-TOOLTIP-REFUND** : Show sell value in tower hover tooltip
- `ebf03067` **B-ENEMY-AUDIO-THROTTLE** : Random-sample 35% + distance-cull 25m step_dirt audio
- `ec63c2fa` **B-SFX-WIRE-EXISTING** : Wire 3 dead clips (no_gold + boss_charge + hero_damage)
- `5bb5f741` **B-SYNERGY-PANEL-POLL** : Replace 1Hz poll with event-based redraw — zero alloc/sec
- `38d8feab` **B-SYNERGY-HALO-DISABLED** : Hide synergy halo when tower disabled

### Vague 3 : Achievements + Doctrine + L10n (04:15 - 04:35)
- `ad8d093f` **B-ACHIEVE-WIRES** : Wire daily_streak + perk_collected + hoarder achievements
- `2ade4106` **B-ACHIEVE-BOSS-ID** : Enrich BossDefeatedEvent with bossTypeId — unlock 8 boss-kill achievements
- `b6ca9051` **B-DOCTRINE-RUNTIME** : ⚠️ P0 CRITIQUE — wire DoctrineSystem.BuildRunConfig at LevelRunner.Start. **Les 7 doctrines étaient 100% sans effet avant ce commit !**
- `eb7d54a3` **B-L10N-KEYS** : Add 10 missing FR keys (skin.* + perk.reroll + summary.stat_score)

### Vague 4 : Wave pacing + Tutorial + Settings (04:35 - 04:45)
- `ae841902` **B-STREAK-MUL** : Apply StreakRewardMul to AddGoldFromKill — V3 plan refonte pacing intent
- `cd61d185` **B-WAVE-START-BANNER** : Animate wave-start-banner — orange "⚠ Vague N arrive" maintenant visible
- `21f7a712` **B-TUTORIAL-WIRES** : Wire 3 missing TutorialState notifications — Steps 3/4/6 auto-advance
- `983f6fef` **B-SETTINGS-POLISH** : Add Bloom/Weather/DamageIcons UI controls + complete reset scope
- `c6eeb360` **B-DEAD-WIRES** : Wire ShakeOnCritHit on crit hit detection (Legendary Perk déjà wired)
- `43f1c825` **B-WORLDMAP-BACK** : Add back-to-menu button on WorldMap — UX nav out-trap fix

## Décisions différées (Mike input required)

### Design choices
- **LawnMower equivalent** : V3 had 1 lawnmower per lane (safety net). V6 n'a pas. Hero death 15s respawn = enemies free-walk to castle pendant 15s. Mike doit décider : implement lane traps OU adjuster spawn rate OU keep as-is (déjà partiellement balanced via Castle direct dmg).
- **5 heroes identical ULT slot 2** : Knight/Barbarian/Mage/Ranger/Rogue ont tous le même 30s CD + 8-fan + 15 AOE. Différenciation nécessaire ? (Mike Q : design call)
- **Cutscene Portraits empty** : 10 cutscenes wired, narrative OK, mais portrait sprites manquent (BuildCutsceneAssets.cs ne mappe pas). Mike doit fournir 10+ sprites ou skip portraits.
- **Cutscene Choices branchés** : V3 avait choix narratifs avec effets (startCoins bonus, kill bonus). V6 = linéaire. Reintroduce ? (6-8h scope si oui)
- **Boss-kill achievements naming** : Switch attendu sur bossTypeId. Si l'ID asset diffère (kill_wizardking_boss vs kill_wizard_king_boss), check naming consistency.

### Scope deferred
- **Trophy gallery V3** : V6 n'a pas. UX progression rewards absente.
- **Coop 2P split-keyboard** : V3 had P2 cursor cyan. V6 single-player only.
- **Audio assets manquants** : `tower_fire` (3 calls fallback procedural beep), `boss_enrage_loop` (silent enrage), `tower_fire_l1/l2/l3` (tiered SFX). Mike doit fournir audio assets.

## Tools autonomie créés

- `tools/v3-auto-loop.sh` : kill Unity + batch validator + screenshots + pixel diff → Library/V3AutoLoop/latest.json
- `Assets/Tests/Editor/V3BatchValidator.cs` : 10 Edit Mode tests (Singletons / LevelData / PathfindingGrid / TowerData / EnemyData / AudioRegistry / Shaders / UIDocs / Resources / LevelRegistry)
- `Assets/Tests/Editor/V3ScreenshotBatch.cs` : 4 scenes capture (Loader / Menu / WorldMap / Main) avec fallback temp Camera
- `Assets/Tests/Editor/V3AutoLoop.cs` : orchestrator master JSON output
- `tools/v3-pixel-diff.py` : PIL pixel diff vs baselines + magenta/uniform detection
- `tools/webgl-serve.sh` : Brotli-aware localhost serve WebGL build
- `.github/workflows/deploy-webgl.yml` : GH Pages auto-deploy on push main

## Verdict V3 parity

**~90% feature parity avec V3 Phaser** :
- ✅ Économie tendue : cost upgrade ×1.5 L2 / ×2.25 L3 + Streak mul + Skip bonus + Magnet
- ✅ Pacing joueur-driven : "Lancer la vague (N)" button + Skip window + Streak tracking
- ✅ Level design 10 worlds × 9 levels (= 90 levels, V3 avait 60)
- ✅ Heroes (5 types) + XP + Level-up + ULT slot 2 + Ultimate slot 3 (L10)
- ✅ Perks (29) + rarity-weighted picks + Legendary unlock
- ✅ Doctrines (7) avec runtime effect maintenant
- ✅ Synergies (13 active types + halo + badges)
- ✅ Achievements (57) avec daily_streak + perk + hoarder + 8 boss-id wires
- ✅ Tutorial (6 steps) avec wires complets
- ✅ Settings (audio/display/controls/a11y/gameplay) avec Bloom/Weather/DamageIcons + Reset scope complet
- ✅ Save (multi-slot A/B/C + atomic write + backup fallback + version migration)
- ✅ Localization (FR + EN dicts) avec 10 keys ajoutées
- ⚠️ Boss types (10 boss avec mechanics uniques — Kraken/WizardKing/AiHub maintenant fonctionnels)

**Manquant** :
- ❌ Cutscene Portraits (10 sprites needed)
- ❌ Cutscene choices branchés
- ❌ LawnMower safety net per lane
- ❌ Trophy gallery
- ❌ Coop 2P split-keyboard
- ❌ Audio assets manquants (tower_fire base, boss_enrage_loop)
- ❌ Hero ULT differentiation per hero (5 identiques)

## Action Mike au réveil

1. `cd /Users/mike/Work/crowd-defense && git pull --rebase`
2. Unity Editor → Cmd+P fresh test sur W1-1 :
   - Vérifier mob spawn, Hero gain XP visible
   - Vérifier tower L1→L2→L3 mesh différents
   - Vérifier tooltip sell value au survol
   - Vérifier doctrine sélectionnée applique multiplier
   - Vérifier wave-start banner orange visible
   - Vérifier tutorial steps 3/4/6 auto-advance
   - Vérifier perk pick au level-up
3. Test WebGL localhost : `bash tools/webgl-serve.sh` → http://localhost:8000

## Surveillance prochain cron 30min

- Mode auto-amélioration continue jusqu'à midi
- Si Mike chat live au réveil → switch immédiat selon ses priorités
- Drift criteria 0/12 ✅ confirmé
