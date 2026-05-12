# Sprint R6-02 DELETE backlog — Sealed scope

> Sealed après validation Mike triage table (commit `d87125c`).
> Chaque ticket = 1 row triage table validée DELETE.
> Format : ID — feature — zone — LOC estimé — deps — status.

## Tickets

### Hero zone (1 file)

- [ ] **R6-02-001** Hero cinematics pack DELETE (charge-up windup + respawn 1.5s + footstep dust + swing arc trail + idle weapon glow + damage screen edge vignette + ultimate AoE shockwave invention)
  - Triage row : 4
  - Zone : `Assets/Scripts/Entities/Hero.cs` (lines à identifier par agent), `Assets/Scripts/UI/HudController.cs` (damage vignette)
  - LOC estimé : -400 Hero.cs + -50 HudController.cs
  - Deps : none
  - Worktree : owner-A

- [ ] **R6-02-002** Hero crown milestone levels 10/20/30 DELETE
  - Triage row : 5
  - Zone : `Hero.cs`
  - LOC estimé : -80
  - Deps : 001 (same file, sequential)
  - Worktree : owner-A

- [ ] **R6-02-003** Hero kill counter + finisher cinematic boss DELETE
  - Triage row : 6
  - Zone : `Hero.cs`
  - LOC estimé : -120
  - Deps : 002
  - Worktree : owner-A

- [ ] **R6-02-004** HeroPortraitController extensions DELETE (HP ring + flame + perk badges row + detailed tooltip + ultimate ready ring pulse)
  - Triage row : 9
  - Zone : `Assets/Scripts/UI/HeroPortraitController.cs`
  - LOC estimé : -400 (garder ~200 LOC core XP bar + portrait simple)
  - Deps : none
  - Worktree : owner-A2

### Tower zone (1 file, sequential)

- [ ] **R6-02-010** Tower XP system DELETE (+5% dmg / 10 kills)
  - Triage row : 12
  - Zone : `Tower.cs`
  - LOC estimé : -120
  - Deps : none
  - Worktree : owner-B

- [ ] **R6-02-011** Tower targeting priority cycle UI DELETE (First/Last/Strongest/Weakest/Closest enum + radial menu button + floating priority label)
  - Triage row : 13
  - Zone : `Tower.cs`, `Assets/Scripts/UI/RadialMenuController.cs`
  - LOC estimé : -150 Tower + -30 RadialMenu
  - Deps : 010
  - Worktree : owner-B

- [ ] **R6-02-012** Tower L4 elite tier DELETE (gold tint + sparkles all research)
  - Triage row : 14
  - Zone : `Tower.cs`
  - LOC estimé : -80
  - Deps : 011
  - Worktree : owner-B

- [ ] **R6-02-013** Tower lightning beam zig-zag + chain to 2 DELETE
  - Triage row : 16
  - Zone : `Tower.cs`
  - LOC estimé : -160
  - Deps : 012
  - Worktree : owner-B

- [ ] **R6-02-014** Tower windup squash 0.1s + Perlin aim wobble DELETE
  - Triage row : 17
  - Zone : `Tower.cs`
  - LOC estimé : -90
  - Deps : 013
  - Worktree : owner-B

- [ ] **R6-02-015** Tower idle visual cluster DELETE (idle breathing scale + shimmer pulse + elemental tint + selection ring cyan + hover highlight target enemy + upgrade arrow indicator + star row visual L1/L2/L3 + target reticle)
  - Triage row : 18
  - Zone : `Tower.cs`, related coroutines
  - LOC estimé : -400 (8 features groupées)
  - Deps : 014
  - Worktree : owner-B

- [ ] **R6-02-016** Tower combat polish cluster DELETE (muzzle flash extension + hit confirmation white flash + kill counter floating +1 text + projectile trail variants per element + selection click sound + upgrade ring expanding + upgrade confetti burst)
  - Triage row : 19
  - Zone : `Tower.cs`, `Projectile.cs`
  - LOC estimé : -350 (7 features groupées, garder muzzle flash basique éventuellement)
  - Deps : 015
  - Worktree : owner-B

### Enemy zone (1 file)

- [ ] **R6-02-020** Enemy variant trails DELETE (Fast=blue, Tough=brown, Regen=green, Armored=grey TrailRenderer)
  - Triage row : 23
  - Zone : `Enemy.cs`
  - LOC estimé : -100
  - Deps : none
  - Worktree : owner-C

- [ ] **R6-02-021** Enemy spawn telegraph 1s ground glow + boss horn DELETE
  - Triage row : 24
  - Zone : `Enemy.cs`, `WaveManager.cs` (SpawnWithTelegraph)
  - LOC estimé : -130
  - Deps : 020
  - Worktree : owner-C

- [ ] **R6-02-022** Enemy ground crack VFX boss/elite DELETE
  - Triage row : 25
  - Zone : `Enemy.cs`
  - LOC estimé : -80
  - Deps : 021
  - Worktree : owner-C

- [ ] **R6-02-023** Enemy hit blood splash + crit slow-mo + screen-shake DELETE
  - Triage row : 26
  - Zone : `Enemy.cs`
  - LOC estimé : -140
  - Deps : 022
  - Worktree : owner-C

- [ ] **R6-02-024** Enemy death ragdoll fall + dust puff DELETE (garder SpawnDeath basique V4)
  - Triage row : 27
  - Zone : `Enemy.cs`
  - LOC estimé : -80
  - Deps : 023
  - Worktree : owner-C

- [ ] **R6-02-025** Enemy elite gold star marker + glow aura DELETE
  - Triage row : 28
  - Zone : `Enemy.cs`
  - LOC estimé : -85
  - Deps : 024
  - Worktree : owner-C

### Castle zone (1 file)

- [ ] **R6-02-030** Castle decorations cluster DELETE (siege debris < 30% HP + gate close bounce + ambient candles per world W1-W10 + blood splatter decals + repair animation between waves + metallic gate material + world decorations W5-W8 — garder candles via SceneDecor port)
  - Triage row : 32
  - Zone : `Castle.cs`
  - LOC estimé : -600 (7 features groupées)
  - Deps : none
  - Worktree : owner-D

- [ ] **R6-02-031** Castle screen shake intensified by hit magnitude + heavy audio DELETE
  - Triage row : 33
  - Zone : `Castle.cs`, `Visual/CameraController.cs`
  - LOC estimé : -80
  - Deps : 030
  - Worktree : owner-D

### Systems zone

- [ ] **R6-02-040** Difficulty slider Easy/Normal/Hard/Brutal DELETE (revenir à V4 difficultyMul per-level 1.0/1.05/1.1/1.15/1.2/1.5)
  - Triage row : 42
  - Zone : `Data/Difficulty.cs` (delete), `Data/BalanceConfig.cs` (revert), `UI/DifficultySelector.cs` (delete), `UI/LevelSelectController.cs` (cleanup hooks), `UI/HudController.cs` (badge cleanup)
  - LOC estimé : -140 + file deletions
  - Deps : none
  - Worktree : owner-E

- [ ] **R6-02-041** RandomMapGenerator.cs DELETE (invention pure, V4 = maps prédéfinies)
  - Triage row : 43 split
  - Zone : `Systems/RandomMapGenerator.cs` (delete file)
  - LOC estimé : -79
  - Deps : none
  - Worktree : owner-E

- [ ] **R6-02-042** ReplayRecorder.cs DELETE (invention pure replay highlights)
  - Triage row : 51 split
  - Zone : `Systems/ReplayRecorder.cs` (delete file)
  - LOC estimé : -128
  - Deps : none
  - Worktree : owner-E

### HUD zone (1 file god class)

- [ ] **R6-02-050** HUD popups inventions DELETE (wave countdown big 3-2-1-GO + wave intro banner slide-in left + wave clear summary popup + perfect streak banner + level start banner cinematic + tutorial popup BIENVENUE + difficulty selector badge)
  - Triage row : 48
  - Zone : `HudController.cs`
  - LOC estimé : -700 (7 popups groupées)
  - Deps : none (HudController god class, sequential)
  - Worktree : owner-F

- [ ] **R6-02-051** HUD permanent badges DELETE (perk-badges-row + perk-set-progress-row + synergy-hud-panel + combo-multiplier + coin rotation 360 + gold rolling counter animation + castle HP icon pulse <25% + wave timer red pulse <5s + wave streak particles trail + enemy intel hover popup + wave preview mini-cards tier border + tower placement preview range circle + boss epic healthbar phase markers + kill float text)
  - Triage row : 49
  - Zone : `HudController.cs`, related UXML/USS
  - LOC estimé : -900 (14 features groupées, conserver gold counter top-right + wave label simple seulement)
  - Deps : 050 (same file, sequential)
  - Worktree : owner-F

### MainMenu + EndScreen zone

- [ ] **R6-02-060** MainMenu invention extensions DELETE (seasonal accents particles + animated gradient bg + Play button hover particles + logo splash 1.4s + credits screen + achievement showcase row top 5 + daily challenge button + hardcore button conditional)
  - Triage row : 50
  - Zone : `MenuController.cs`, related UXML/USS
  - LOC estimé : -500 (garder splash logo basique + main menu basique seulement)
  - Deps : none
  - Worktree : owner-G

- [ ] **R6-02-061** EndScreen 4 extensions DELETE (stars celebration scale-in confetti + share button + quick retry + confirm modal exit + replay highlights wiring) + KEEP-REFACTO core ~250 LOC
  - Triage row : 51
  - Zone : `EndScreenController.cs`
  - LOC estimé : -966 (de 1216 LOC vers ~250 LOC core)
  - Deps : 042 (ReplayRecorder doit être DELETE avant)
  - Worktree : owner-G

## Summary

- **24 tickets** sealed
- **LOC delta total estimé** : **-6750 LOC** (entities ~-3800 + UI ~-2100 + systems ~-850)
- **Cibles fichiers** :
  - Hero.cs 2320 → ~1670 (-650)
  - Tower.cs 2970 → ~1740 (-1230)
  - Enemy.cs 2806 → ~2191 (-615)
  - Castle.cs 1313 → ~633 (-680)
  - HudController.cs (god class) → estimer (-1600)
  - EndScreenController.cs 1216 → ~250 (-966)
  - MenuController.cs → estimer (-500)
- **3 files supprimés** : RandomMapGenerator.cs, ReplayRecorder.cs, Difficulty.cs (orphans après DELETE)

## Hard rules respect

- Aucun ticket touche le code KEEP (Tower research = MetaUpgrades rebrand, Boss enrage aura, Hero XP curve, etc.)
- Compile + console gate après chaque ticket (charter §1 règle #5)
- Self-report agent obligatoire 100 mots max (charter §1 règle #8)
- Max 4 worktrees simultanés (charter §1 règle #9)
- Aucun feature ajouté pendant DELETE (no-feature-creep)
