# Sprint R6-02 — Coordination (DELETE pass)

> Sprint sealed après Mike validation 14h15 (chat).
> Backlog : `.claude/specs/R6-EXEC/_backlog.md` (24 tickets).

## Mode

- **Mode** : Supervisé Mike batch (par défaut, Mike valide chaque batch 3-4 tickets done)
- **Time cap** : pas de cap explicite, ~4-6 heures estimé pour 24 tickets DELETE
- Si Mike écrit "GO autonomous sprint R6-02 time cap N heures" → switch en mode autonome avec ScheduleWakeup 1800s

## Worktrees ownership (zones disjointes)

| Owner | Worktree | Files | Tickets |
|---|---|---|---|
| **owner-A** | `wt-hero` | `Hero.cs` (sequential 001→003) | R6-02-001, R6-02-002, R6-02-003 |
| **owner-A2** | `wt-hero-portrait` | `HeroPortraitController.cs` (independent) | R6-02-004 |
| **owner-B** | `wt-tower` | `Tower.cs` (sequential 010→016, god class), `Projectile.cs`, `RadialMenuController.cs` | R6-02-010..016 |
| **owner-C** | `wt-enemy` | `Enemy.cs` (sequential 020→025), `WaveManager.cs` (telegraph fix) | R6-02-020..025 |
| **owner-D** | `wt-castle` | `Castle.cs` (sequential 030→031), `CameraController.cs` (shake) | R6-02-030, R6-02-031 |
| **owner-E** | `wt-systems` | `Difficulty.cs`, `DifficultySelector.cs`, `BalanceConfig.cs`, `RandomMapGenerator.cs`, `ReplayRecorder.cs` | R6-02-040, R6-02-041, R6-02-042 |
| **owner-F** | `wt-hud` | `HudController.cs` (sequential 050→051, god class), UXML/USS | R6-02-050, R6-02-051 |
| **owner-G** | `wt-menu-endscreen` | `MenuController.cs`, `EndScreenController.cs` | R6-02-060, R6-02-061 |

## API contracts à préserver pendant DELETE

- `Hero.Init()`, `Hero.TakeDamage()`, `Hero.OnHeroDamaged` event, `Hero.GainXp()`, `Hero.Current` static accessor
- `Tower.Init()`, `Tower.Fire()`, `Tower.RegisterKill()`, `Tower.Upgrade()`, `Tower.SetSelected()`
- `Enemy.Init()`, `Enemy.TakeDamage()`, `Enemy.OnDeath()`, `Enemy.SpawnSpark` basique via VfxPool
- `Castle.Instance`, `Castle.TakeDamage()`, `Castle.HP/HPMax`
- `HudController.Show*Toast`, `HudController.OnGoldChanged`, gold counter top-right minimum
- `EndScreenController.Show(result)` core summary path
- `LevelData.castleHPOverride` (Q12), `BalanceConfig.CastleHPFor()` (Q14 = 200 W1-1)

## Hot zones interdites pendant R6-02

- `Assets/Scripts/Data/BalanceConfig.cs` — modifier UNIQUEMENT via ticket Q14 (R6-04, pas R6-02)
- `Assets/ScriptableObjects/Levels/*.asset` — schema clean, ne pas re-toucher (`f4fd720` + `071812e` validés)
- `Assets/Scripts/Data/*Registry.cs` — perks/schools/skins KEEP intact

## Merge order

1. Worktree E (systems, file deletions atomiques) → mergeable indépendamment
2. Worktree D (Castle, petit, 2 tickets) → mergeable indépendamment
3. Worktree A (Hero) + A2 (HeroPortrait) parallèles → merge sequence
4. Worktree C (Enemy) → mergeable indépendamment
5. Worktree B (Tower god class, 7 tickets sequential) → en chemin critique
6. Worktree F (HUD god class, 2 tickets sequential) → en chemin critique
7. Worktree G (Menu + EndScreen, dépend de wt-systems ReplayRecorder DELETE) → après wt-systems

## Smoke test attendu post-sprint

- `mcp__UnityMCP__build_webgl` exit 0
- `mcp__UnityMCP__read_console` errors: 0 (warnings préexistants OK)
- Play mode 1 level (W1-1) jouable sans NullRef
- Gold counter visible, wave label affiché, towers placables et fire
- Pas de regression Q-N validés : Q3 magnet cap, Q5 skip bonus, Q12 castle override

## Self-report agent template

Chaque agent feature-dev rend 100 mots max :

```
Ticket : R6-02-XXX [feature name]
LOC : +X / -Y (net Z)
Compile OK : y/n
Console errors résiduels : [liste si y]
Backlog items ajoutés (R6-found-during-exec.md) : [liste si y]
Commit hash : [SHA]
```

## Backlog R6-found-during-exec

Fichier : `.claude/backlog/R6-found-during-exec.md` (créé vide). Agents y ajoutent toute amélioration vue mais NON exécutée pendant ticket.

## Push notif

Configure via `PushNotification` à fin de sprint (tous tickets done ou time cap reached ou STOP).
