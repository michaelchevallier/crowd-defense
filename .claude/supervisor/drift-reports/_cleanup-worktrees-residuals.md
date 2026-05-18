# Worktrees Residuals — Night Swarm #5 cleanup (2026-05-18 08:23 CEST)

## Result

- **37 / 37 worktrees `md-*` mergés et removed** sur `origin/main` HEAD `44d670d8`.
- **0 stale** worktree (toutes les branches `md-*` étaient ancestors de `origin/main`).
- **Branches `md-*` deleted** : 37 / 37.
- Final worktree count : **2** (main + 1 locked agent-a73a01820a2fddc28).

## Method

```bash
# Pour chaque branch md-* :
git merge-base --is-ancestor <head> origin/main  # check mergé
git worktree remove <path> --force                # supprime worktree
git branch -D <branch>                            # supprime branche
git worktree prune                                # cleanup .git/worktrees/
```

## Residual locked worktree

- `/Users/mike/Work/crowd-defense/.claude/worktrees/agent-a73a01820a2fddc28` @ `5afb95a8` — locked, NOT touched. C'est probablement un worktree d'un autre Claude agent en cours. À vérifier manuellement plus tard si nécessaire.

## Branches deleted (alpha)

md-achievements-track-event-2, md-bosssystem-defeat-once-4, md-build-main-scene-singletons-3, md-castle-no-regen-w6-2, md-doctrinecontroller-lambda-leak-4, md-enemy-stealth-allocs-1, md-eventsystem-pipe-parser-1, md-eventsystem-recursion-guard-1, md-floating-popups-wire-3, md-gameoverpanel-score-breakdown-2, md-hero-panel-query-1, md-hero-projectile-pool-3, md-hud-font-roboto-3, md-hud-multi-uidocument-1, md-hudcontroller-start-refactor-4, md-juiceconfig-magic-numbers-4, md-juicefx-camera-snap-1, md-levelevents-raise-1, md-maprenderer-streams-3, md-materialcontroller-cache-1, md-monosingleton-refactor-1, md-onlevelcomplete-wire-1, md-outline-shared-mat-race-1, md-pathtiles-reveal-anim-3, md-perkpicker-leak-fix-2, md-perksystem-aura-magic-4, md-perksystem-forteresse-magic-4, md-perksystem-using-linq-3, md-run-summary-modal-3, md-savesystem-catch-warning-4, md-savesystem-isstackable-dead-4, md-settingsregistry-debounce-4, md-smoketest-e2e-unity-5, md-tower-l3-cascade-pierce-3, md-tutorial-phase6-proximity-3, md-vfxpool-waitforseconds-1, md-weather-ambient-audio-2

Total : 37 branches.
