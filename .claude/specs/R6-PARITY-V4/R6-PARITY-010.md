# R6-PARITY-010 — Weather effects port

**Sprint** : R6-PARITY-V4 (Batch P1-B, WT1')
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #5
**Source V4** : `/Users/mike/Work/milan project/src-v3/systems/Weather.js` (143 LOC)

## Contexte

V4 a Weather system per-thème (clouds plaine/desert, spores foret, sable desert/storm, sky gradient billboard, transition thème). V6 absent.

## Task

1. Read source V4 `/Users/mike/Work/milan project/src-v3/systems/Weather.js`.
2. Créer `Assets/Scripts/Visual/WeatherController.cs` (cap ≤500 LOC) :
   - Subscribe `LevelEvents.OnLevelStart` (lookup `LevelTheme` enum → choisir weather preset)
   - Per-theme weather presets :
     - **plaine** : drifting clouds high (ParticleSystem world-space, scrolling)
     - **foret** : spores particles low + light fog
     - **desert** : sand particles ground level + heat shimmer post-process
     - **desert_storm** : sand storm dense, vision range -25%
     - **glacier** : snow particles
     - **volcan** : ember sparks rising
     - **marais** : mist low + fireflies sparse
     - **espace** : star particles distant
     - **submarin** : bubble particles rising
     - **medieval** : light dust motes
     - **cyberpunk** : neon rain + glitch flicker
   - Lifecycle : OnLevelStart spawn → OnLevelEnd despawn
3. **Exploit Unity** : ParticleSystem world-space + sub-emitters + Noise modules. VFXGraph si compute-heavy (sand_storm full coverage). URP volume post-processing pour heat shimmer.
4. **Placeholder-first** : si textures absentes, particles sans texture sheet (simple billboards colored).

## Hard rules

- Cap 500 LOC WeatherController.cs (charter §1)
- No feature creep (juste weather visuals, pas gameplay impact = autre ticket R6-PARITY-012)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-010): Weather effects port — 11 themes presets + per-theme ParticleSystem`
- Self-report : LOC ajoutées, themes covered (X/11), Unity exploits used, compile OK, commit hash

## Time estimate

~4-6h
