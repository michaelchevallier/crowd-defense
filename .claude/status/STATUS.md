# Crowd Defense — STATUS.md tracker (multi-session)

> Source of truth multi-session pour le sprint en cours. À lire en début de chaque session opus, à updater en fin.
> Pattern emprunté à `milan project` repo. Fichier root `STATUS.md` reste le récit long-form du sprint MIGRATE ; ce fichier-ci est le **tracker court** pour orientation rapide.

---

## Current sprint

**R6-PARITY-V4-FINAL** — ✅ **COMPLETE** (95%+ parity atteint, closure `16fe830`).

- Sprint dates : 2026-05-12 16h05 → 2026-05-13 02h35 (~10h30 wall)
- Total commits : **98** (`739efc7..HEAD`)
- Retrospective : `.claude/audit/SPRINT-R6-RETROSPECTIVE.md`
- Tag : `v6.0-parity-v4-sprint-r6`
- Live build : `https://michaelchevallier.github.io/crowd-defense/v6/`

## Next sprint

**Candidates** (à arbitrer Mike) :
- **R7 polish** : Foire/Medieval castle textures + 80 levels art + L3 upgrade branching
- **R8 perf** : 3 perf-3fix profilage live (60 FPS desktop / 30 FPS mobile)
- **R9 playable MVP** : 5-wave loop polish + onboarding tutorial + FTUE
- **STOP** : break + replayability self-test

---

## V4 baseline vs V6 effectif (par axe)

| Axe | Code-level parity | User-facing visible | Gap principal |
|---|---|---|---|
| Gameplay (loop, towers, enemies, wave) | 96-97% | **85-95%** | Player loop BuildPoint flow partiel |
| Visuel (textures, materials, FX) | 95% | **40-60%** | Materials gris (78 PNG Flux non-wired) |
| UI / HUD | 95% | **50-70%** | Toolbar/minimap wiring fragile, positioning broken |
| Audio | 100% | **0-30%** | MusicManager wired (`cdfe829`), SFX en cours |
| Animations | 90% | **10-20%** | Animators T-pose, controllers default broken |
| Menu / Nav | 90% | **30-50%** | MenuScene + SceneNav bug-fixer en cours |
| Camera / Input | 100% | **40-60%** | Bindings fixés (`de6eec1`), Cinemachine setup unclear |

**Note** : "Le code existe à 96%, mais le wiring Unity Editor est ~40% complet" (audit `adb68ee`). Pas un problème de code, un problème workflow.

---

## Recent commits (last 20 origin/main)

```
cc3bcf2 supervisor: WAVE 4 BACKLOG (T1-T7) — textures/toolbar/minimap/ghost/wave-UI/animator/integration
5af7e53 fix(parity): wire 22 VFX prefabs in VfxPool (eliminate fallback null-guard)
cdfe829 fix(wiring): add MusicManager to Main.unity scene
a76b81c supervisor: WAVE 3 BACKLOG (V1-V8) — orthogonal aux 4 bug-fixers Opus wave-3 player-loop
2b53896 fix(wiring): wire MusicManager audio clips (menu/calm/intense/boss)
de6eec1 fix(parity): camera + input bindings match V4 reference
adb68ee chore(audit): TRUE v4↔v6 visible-parity (honest user-facing assessment)
1e07f7c fix(parity): Hero ULT V4 mode (30s/8 proj/dmg=15 all heroes) — audit P1 #2
a296016 fix(parity): Enable warlord charge + dragon fire breath (P1 audit)
cc37b8b fix(parity): Add SchoolDef.starterTowerType field (P2 audit)
99c3c3b fix(parity): SkinRegistry wire 10 castle + 4 vfx skins (P0 audit)
12c7694 supervisor: MASSIVE backlog tracks A-E (Mike no-idle directive) — 9 tracks pour 99%→100%+ parité
ad3804d chore(audit): V4↔V6 parity gap final 5% audit
4dcfed2 fix(scene): wire ProjectilePool projectilePrefab in Main.unity
ed76df8 fix(scene): wire ProjectilePool projectilePrefab in Main.unity
3cb134b fix(runtime-pool): AudioMixerController add Assets/Resources/Audio.meta
792f5e9 fix(runtime): MonoSingleton skip auto-create during scene unload (OnDestroy cascade)
1917b0a fix(wiring): ProjectilePool.projectilePrefab wire to Prefabs/Projectile.prefab in Main.unity
b9f6685 fix(runtime-pool): ProjectilePool warn-once null prefab (40x spam → 1x)
a8f887e supervisor: URGENT PARITY-FINISH backlog (5 tickets) — Mike no-stop directive
```

---

## Pending tickets (top 10)

WAVE 3 (orthogonal aux 4 bug-fixers wave-3 player-loop) :

1. **W3-V1** ✅ DONE (`5af7e53`) — VfxPool wire 22 prefabs
2. **W3-V2** — Cutscenes 10 worlds wired runtime confirmation
3. **W3-V3** — Achievements 56 wired + tracked
4. **W3-V4** — Meta-upgrades 10 wired + Persist
5. **W3-V5** — Modifiers (8 curses+blessings) runtime test
6. **W3-V6** — Code dedup + dead code audit
7. **W3-V7** ✅ DONE (this file) — STATUS.md crowd-defense create
8. **W3-V8** — Build Settings audit + ProjectSettings polish

WAVE 4 (Top 5 user-facing gaps audit `adb68ee` non couverts) :

9. **W4-T1 (P0)** — Textures wire 78 Flux PNG → ground+path materials
10. **W4-T2..T7 (P1-P2)** — Toolbar / Minimap / Ghost preview / Wave UI / Animator defaults / 5-wave integration test

---

## Instructions next session opus

À lire dans cet ordre :

1. **`.claude/supervisor/instructions-to-exec.md`** — dernières directives Mike + supervisor (WAVE 3 + WAVE 4 backlog actifs)
2. **`.claude/audit/2026-05-12-*`** — audits récents (parity gap, visible-parity-honest, scene-wires, schools-mapping, triage-table)
3. **`STATUS.md`** (root) — récit long-form sprint MIGRATE (455 lignes, phases 0-4 done, phase 5 next)
4. **`CLAUDE.md`** (root) — Unity context + workflow Opus orchestre / Sonnet exécute
5. **`.claude/supervisor/questions-to-supervisor.md`** — Q en cours, resolved récents
6. **`.claude/supervisor/charter.md`** — règles cap LOC, cron policy, ScheduleWakeup interdits

Workflow type :
- Read instructions-to-exec.md → pioche un ticket WAVE 3/4 non-DONE
- Spawn Sonnet feature-dev en worktree OU bug-fixer direct selon complexité
- Push autonome chaque commit, **NEVER IDLE** (Mike directive)
- Coordination avec 4 bug-fixers Opus wave-3 actifs (cf zone safe lignes 1701-1718 instructions)

---

## Architecture state

**Partial classes split done** (17 fichiers, cap 500 LOC strict charter §1 règle #3) :

- **Castle** (3 fichiers) : `Castle.cs` + `Castle.HP.cs` + `Castle.VFX.cs`
- **Tower** (6 fichiers) : `Tower.cs` + `Tower.Anim.cs` + `Tower.Combat.cs` + `Tower.Effects.cs` + `Tower.Placement.cs` + `Tower.Upgrade.cs`
- **Enemy** (8 fichiers) : `Enemy.cs` (491 LOC under cap) + `Enemy.Anim.cs` + `Enemy.Behaviors.cs` + `Enemy.Combat.cs` + `Enemy.Init.cs` + `Enemy.Lifecycle.cs` + `Enemy.Movement.cs` + `Enemy.Stats.cs` + `Enemy.Update.cs`
- Plus `EnemyBossBehaviors.cs` 446 LOC (re-mesuré sous cap après wave-3 `6dac3cd`)

**Runtime infra acquis** :

- `VfxPool` : 22 prefabs wired (`5af7e53`) — élimine null-guard fallback
- `ProjectilePool` : projectilePrefab wired (`1917b0a`, fixé `4dcfed2` via Main.unity)
- `MonoSingleton` : `_destroying` flag skip auto-create durant OnDestroy cascade scene unload (`792f5e9`)
- `MusicManager` : ajouté Main.unity scene (`cdfe829`) + audio clips wired menu/calm/intense/boss (`2b53896`)
- `AudioMixerController` : `Assets/Resources/Audio.meta` ajouté (`3cb134b`)
- `Camera + Input` bindings match V4 reference (`de6eec1`)

**Outline.cs** static inverted hull scale 1.02 back-face material wire post-GLTF spawn (`79b56f1`..`59785a6`).

---

## Known issues (questions en cours)

Cf `.claude/supervisor/questions-to-supervisor.md` :

- Toutes les Q ouvertes courant 16h-18h sont `[resolved]` (vfx-bindings-cap, stash-WIP, enemy-refacto-cap, cap-enemy auto-resolved par BACKLOG-WAVE-3).
- Pas de Q `[pending]` active à ce stade.

Risks transverse (cf root STATUS.md "Risks raised") :
- Unity-MCP edge cases (jeune, peut buguer)
- WebGL bundle ~5-15 MB compressé vs 395 KB Phaser
- Steamworks/Apple Developer/Google Play setup nécessite Mike (credentials)
- Mike skill Unity/C# = zéro (vulgarisation systématique)

---

*Dernière update : 2026-05-13 02h35 — sprint R6-PARITY-V4-FINAL marked COMPLETE (closure `16fe830`).*
