# R6-PARITY-012 — V4 dynamic EventManager mid-wave events

**Sprint** : R6-PARITY-V4 (Batch P1-B, WT2')
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #6
**Source V4** : `/Users/mike/Work/milan project/src-v3/systems/EventManager.js`

## Contexte

V4 a 3 dynamic mid-wave events qui modifient le gameplay temporairement :
- **sand_storm** : range -25% / speed +15% (towers nerf, enemies buff) pour 30s
- **lava_surge** : 1-3 cells path inondées (towers ON those cells disabled) + castle dmg 5/s pendant 20s
- **carousel_spin** : 30% des ennemis changent de path actuel (re-route) une fois

V6 a `EventSystem` 12 events narratifs (lore-only). Ajouter ces 3 dynamiques mid-wave.

## Task

1. Read source V4 `/Users/mike/Work/milan project/src-v3/systems/EventManager.js`.
2. Locate V6 `Assets/Scripts/Systems/EventSystem.cs` (ou similar). Identifier extension hook.
3. Créer (ou ajouter dans EventSystem) :
   - `DynamicEventManager.cs` (cap ≤500 LOC) : trigger random 1 des 3 events à mid-wave (wave % 5 == 0 par exemple)
   - **SandStormEvent** : 30s, query all `Tower.Range *= 0.75`, all `Enemy.Speed *= 1.15`. Visual : WeatherController spawn sand_storm preset (overlap R6-PARITY-010).
   - **LavaSurgeEvent** : 20s, select 1-3 cells path random, mark inondées (visual : Toon_Lava material flash on cells), towers ON those cells `IsDisabled = true`, castle `HP -= 5 * deltaTime`.
   - **CarouselSpinEvent** : one-shot, query 30% enemies alive, `Enemy.ForceRecalcPath()` (random alternate path si disponible).
4. **Coord avec R6-PARITY-010** : sand_storm visual peut réutiliser WeatherController.spawn(SandStormPreset) si ticket merged avant. Sinon placeholder simple particle.

## Exploit Unity

- Animator triggers pour cell lava_surge flash
- ParticleSystem global pour sand storm visual full screen
- AnimationCurve pour speed ramp transitions

## Hard rules

- Cap 500 LOC DynamicEventManager.cs
- No feature creep (juste ces 3 events, pas extension narrative)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-012): dynamic mid-wave events — sand_storm + lava_surge + carousel_spin`
- Self-report : 3 events impl (3/3 ?), LOC ajoutées, integration avec R6-PARITY-010 y/n (sand_storm visual), compile OK, commit hash

## Time estimate

~3h
