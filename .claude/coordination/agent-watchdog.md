# Agent Watchdog — Cron Single-Shot

## 2026-05-12 02:51 — iter 31 (single-shot)

### Stats
- Now epoch : 1778547065
- Actifs (mtime < 8 min) : 5
- Stalled (mtime 8-30 min) : 12
- Total mtime < 30 min : 17
- Worktrees : 16
- Unity batchmode pré-action : aucun process, **UnityLockfile présent (0 bytes, 02:49) stale orphelin**
- Unity batchmode post-action : aucun process, lockfile purgé

### Actions
- `pkill -9 -f "Unity.app.*batchmode"` → aucun process matché (rc=1, killed=0).
- `rm -f Temp/UnityLockfile` → LOCK_REMOVED (stale, 02:49, sans owner).
- Watchdog.sh NON déclenché (lockfile stale ≠ batch silent > 15 min).
- 12 stalled outputs read-only. Échantillons tail :
  - `brp5pvcv4` (~8 min) → `EXIT: 0` (terminé proprement)
  - `blmr2l0x2` (~13 min) → dump listing historique (artefact watchdog précédent)
  - False positives : ce sont des outputs Sonnet déjà terminés, pas des agents stuck.

### Actifs (< 8 min)
```
byk64ujba, b1kmzl7aj, bocjwk1gj, bbpmhvd7r, bkph17w17
```

### Stalled (8-30 min)
```
blmr2l0x2, bpcw8de65, b7rboqnmt, bvu55yih2, boskcp31l,
beaopg5ep, bydpw3c1j, bp9hcmqxg, b7kobg125, btqzuanxk,
b7plltnau, brp5pvcv4
```

### Évolution iter 30 → 31
- Actifs (< 8 min) 8 → 5 (-3) : décroissance fresh.
- Stalled (8-30 min) 11 → 12 (+1).
- Unity batch : N → N (stable).
- Lockfile : absent (iter 30 final) → **présent stale** réapparu (02:49) → purgé.
  Note : observé aussi par iter 30 en flash. Possible cycle de touch/clear par un autre runner.
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:5 Stalled:12 Killed:0 Worktrees:16 UnityBatch:N`

## 2026-05-12 02:50 — iter 30 (single-shot)

### Stats
- Actifs (mtime < 30 min, find direct) : 19 (initial wc=17, re-scan=19, transient touch mid-run)
- Fresh (mtime < 8 min) : 8
- Stalled (mtime 8-30 min) : 11
- Worktrees : 16
- Unity batchmode : **NON** (aucun process `-batchmode`, aucun Editor)
- UnityLockfile pré-action : présent (0 bytes, 02:49) au 1er check → absent au 2e check (auto-cleanup ?)

### Actions
- Lockfile : nothing to do (déjà absent au moment de l'action). Aucun process Unity batch / Editor à killer.
- `pkill` non déclenché (aucun match `Unity.app.*batchmode`).
- 11 stalled signalés (read-only, outputs résiduels Sonnet terminés).
- 5+ `dotnet VBCSCompiler.dll` détectés (Roslyn workers orphelins, non bloquants).
- Aucun agent feature-dev actif côté processus.

### Évolution iter 29 → 30
- Actifs (30 min) 20 → 19 (-1) : stable.
- Fresh (< 8 min) — → 8 (info ajoutée).
- Stalled 11 → 11 (stable).
- Unity batch : N → N (stable).
- Lockfile : absent → flash present-stale → absent (auto-cleared entre checks).
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:8 Stalled:11 Killed:0 Worktrees:16 UnityBatch:N`

## 2026-05-12 — iter 29 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 20
- Stalled (mtime 8-30 min) : 11
- Worktrees : 16
- Unity batchmode : **NON** (aucun process)
- UnityLockfile : **absent**

### Actions
- Pas de lockfile à purger.
- Pas de batch Unity à killer.
- 11 stalled signalés (read-only, outputs résiduels Sonnet terminés).

### Stalled outputs (8-30 min)
```
blmr2l0x2, beaopg5ep, bydpw3c1j, b39to239a, bvu55yih2,
b7rboqnmt, bnvqijugc, byt9ck4xd, bl4re5hvq, bpcw8de65,
boskcp31l
```

### Évolution iter 28 → 29
- Actifs 10 → 20 (+10) : forte hausse (fenêtre 30 min vs 8 min iter 28).
- Stalled 12 → 11 (-1).
- Unity batch : N → N (stable).
- Lockfile : purgé → absent.
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:20 Stalled:11 Killed:0 Worktrees:16 UnityBatch:N`

## 2026-05-12 02:46 — iter 28 (single-shot)

### Stats
- Actifs (mtime < 8 min) : 10
- Stalled (mtime 8-30 min) : 12
- Worktrees : 16
- Unity batchmode : **NON** (aucun process `Unity.app.batchmode`)
- UnityLockfile pré-action : **présent (0 bytes, stale, 02:46) — orphelin, pas de process Unity**

### Actions
- `pkill -9 -f "Unity.app.*batchmode"` → aucun process matché (rc≠0 normal, killed=0).
- `rm -f Temp/UnityLockfile` → LOCK_REMOVED (stale orphelin, aucun batch owner).
- 12 stalled signalés (read-only).
- Watchdog.sh NON déclenché (pas de batch tournant).
- 4× `dotnet VBCSCompiler.dll` résiduels Roslyn (non bloquants).

### Stalled outputs (mtime 8-30 min)
```
blmr2l0x2(02:21), beaopg5ep(02:31), bydpw3c1j(02:32),
b39to239a(02:18), b7rboqnmt(02:23), bvu55yih2(02:25),
bnvqijugc(02:18), byt9ck4xd(02:17), bdvvlc03j(02:17),
bl4re5hvq(02:19), bpcw8de65(02:22), boskcp31l(02:27)
```

### Évolution iter 27 → 28
- Actifs (< 8 min fresh) : 8 → 10 (+2).
- Stalled : 14 → 12 (-2).
- Unity batch : N → N (stable).
- Lockfile : absent → **présent stale** → purgé.
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:10 Stalled:12 Killed:0 Worktrees:16 UnityBatch:N`

## 2026-05-12 — iter 27 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 22
- Fresh (mtime < 8 min) : 8
- Stalled (mtime 8-30 min) : 14
- Worktrees : 16
- Unity batchmode : **NON** (aucun process `Unity.app.batchmode`)
- UnityLockfile : **absent**

### Actions
- Pas de lockfile à purger (absent).
- Pas de batch Unity à killer (aucun process).
- 14 stalled signalés (read-only, artefacts outputs Sonnet terminés).
- Aucun agent feature-dev actif côté processus.

### Stalled outputs (8-30 min)
```
bw0q8pd8v, blmr2l0x2, bydpw3c1j, ba5f2qfcc, beaopg5ep,
b39to239a, bvu55yih2, b7rboqnmt, bnvqijugc, byt9ck4xd,
bl4re5hvq, bdvvlc03j, bpcw8de65, boskcp31l
```

### Évolution iter 26 → 27
- Actifs 31 → 22 (-9) : décroissance significative.
- Fresh 8 → 8 (stable).
- Stalled 22 → 14 (-8) : outputs vieillissent hors fenêtre 30 min.
- Unity batch : Y → N (build WebGL terminé / quit entre iter 26 et iter 27).
- Lockfile : présent legitimate → absent (Unity batch quitté proprement).
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:22 Stalled:14 Killed:0 Worktrees:16 UnityBatch:N`

## 2026-05-12 02:40 — iter 26 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 31 (wc -l direct = 31, listing détaillé n=22 (filtre 8-30) + fresh=8 = 30 → 1 output borderline pivot)
- Fresh (mtime < 8 min) : 8
- Stalled (mtime 8-30 min) : 22
- Worktrees : 16
- Unity batchmode : **OUI** (PID 63049 + zsh wrapper 63047 — BuildWebGL démarré 02:39, elapsed 1m36s)
- UnityLockfile : **présent (0 bytes, 02:39) — LEGITIMATE (Unity batch alive, /tmp/unity_build.log mtime = now, 541 KB en cours d'écriture)**

### Actions
- **Lockfile NOT purgé** — Unity batch alive et actif (log écrit en temps réel, build WebGL post-relance iter 25 redémarré). Killer = corrompre.
- 22 stalled signalés (read-only, outputs résiduels Sonnet terminés).
- Unity batch < 15 min (1m36 silent ? non — log mtime = now) → watchdog.sh NON déclenché.
- Aucun nouveau Sonnet feature-dev planifiable tant que build WebGL singleton occupe le projet.

### Évolution iter 25 → 26
- Actifs 29 → 31 (+2) : légère hausse (nouveaux outputs depuis iter 25).
- Stalled 23 → 22 (-1).
- Unity batch : N → **Y** (BuildWebGL relancé entre iter 25 et iter 26, PID 63049 elapsed 1m36).
- Lockfile : purgé → présent legitimate.
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:31 Stalled:22 Killed:0 Worktrees:16 UnityBatch:Y`

## 2026-05-12 — iter 25 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 29
- Stalled (mtime 8-30 min) : 23
- Worktrees : 16
- Unity batchmode pré-action : **OUI** (PID 60231 + zsh wrapper 60227 — BuildWebGL démarré 02:34)
- UnityLockfile pré-action : **présent**

### Actions
- `kill -9 60231 60227` → confirmed KILLED_OK (Unity batchmode WebGL build interrompu sur instruction explicite étape 3).
- `rm Temp/UnityLockfile` → LOCK_REMOVED.
- 23 stalled signalés (outputs read-only, pas de kill par convention).

### Note
Instruction étape 3 = "LOCKFILE → kill + rm Temp/UnityLockfile" : appliquée. Build WebGL interrompu (était en cours depuis iter 24). Lockfile purgé. Si le build doit reprendre, relancer via `BuildScript.BuildWebGL` après vérif état Library.

### Évolution iter 24 → 25
- Actifs 36 → 29 (-7).
- Stalled 29 → 23 (-6).
- Unity batch : Y → N (kill exécuté).
- Lockfile : présent → purgé.
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:29 Stalled:23 Killed:2 Worktrees:16 UnityBatch:N`

## 2026-05-12 02:36 — iter 24 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 36
- Fresh (mtime < 8 min) : 6
- Stalled (mtime 8-30 min) : 29
- Worktrees : 16
- Unity batchmode : **OUI** (2 process `Unity.app.batchmode` — WebGL build actif)
- UnityLockfile : **présent, 0 bytes, mtime 02:34 — LEGITIMATE (Unity batch WebGL build en cours)**

### Actions
- **Lockfile NOT purgé** — Unity batchmode WebGL build actif (clang++ emscripten il2cpp en compilation, processes spawned 02:35, CPU 85-95%, owners PID 60747/60765/60770/60781/60779). Killer le lockfile = corrompre le build.
- 29 stalled signalés (read-only, pas de kill — outputs vieillis hors fenêtre fresh).
- Aucun nouveau Sonnet feature-dev à schedule tant que build WebGL singleton n'est pas terminé.

### Évolution iter 23 → 24
- Actifs (< 30 min) 41 → 36 (-5) : décroissance continue.
- Fresh < 8 min 7 → 6 (-1).
- Stalled 34 → 29 (-5).
- Unity batch : N → **Y** (build WebGL démarré entre iter 23 et iter 24).
- Lockfile : purgé (stale) → présent (legitimate).
- Worktrees : 16 → 16 (stable).

### Note critique
Build WebGL en cours = Unity Editor / batchmode singleton occupe le projet. Toute tentative d'agent Sonnet feature-dev sur Assets/Scripts va échouer (collision lock) jusqu'à fin du build.

### Output stdout
`Actifs:36 Stalled:29 Killed:0 Worktrees:16 UnityBatch:Y`

## 2026-05-12 02:32 — iter 23 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 41
- Fresh (mtime < 8 min) : 7
- Stalled (mtime 8-30 min) : 34
- Worktrees : 16
- Unity batchmode : **NON** (aucun process `Unity.app.batchmode`)
- UnityLockfile : **présent, 0 bytes, 02:32:04 — stale (pas de process Unity Editor)**

### Actions
- Lockfile `Temp/UnityLockfile` purgé (stale, aucun owner).
- 4 process `dotnet exec VBCSCompiler` détectés (Roslyn workers résiduels) — pas de Unity Editor ni batchmode parent.
- 34 stalled signalés (read-only, pas de kill par instruction — outputs sont artefacts de runs Sonnet terminés).
- Aucun agent feature-dev actif côté processus.

### Évolution iter 22 → 23
- Actifs (< 30 min) 46 → 41 (-5) : décroissance.
- Fresh < 8 min 7 → 7 (stable).
- Stalled 39 → 34 (-5) : décroissance attendue (outputs vieillissent hors fenêtre 30 min).
- Unity batch : N → N (stable).
- Lockfile : absent → present-stale → purgé.

## 2026-05-12 — iter 22 (single-shot)

### Stats
- Actifs (mtime < 30 min) : 46
- Fresh (mtime < 8 min) : 7
- Stalled (mtime 8-30 min) : 39
- Worktrees : 16
- Unity batchmode : **NON** (aucun process)
- UnityLockfile : **absent**

### Actions
- Pas de lockfile à purger.
- Pas de batch Unity à killer.
- 39 stalled signalés (read-only, pas de kill par instruction).
- Aucun process Sonnet feature-dev actif dans `ps` — les outputs stalled sont des artefacts de runs précédents, pas des agents en cours.

### Échantillon tails stalled
- `byt9ck4xd` (02:17) : "Aborting batchmode due to failure: Scripts have compiler errors" — terminé en échec
- `bnvqijugc` (02:18) : "Multiple Unity instances cannot open the same project" — terminé en collision
- `bdvvlc03j` (02:17) : "No Unity + have save — stop watching" — terminé OK
- `blmr2l0x2` (02:21) : listing de chemins, terminé
- `bl4re5hvq` / `b39to239a` / `bw0q8pd8v` : vides (terminé sans output trailing)

### Évolution iter 21 → 22
- Actifs (< 30 min) 48 → 46 (-2) : décroissance lente.
- Fresh < 8 min 7 → 7 (stable).
- Stalled 40 → 39 (-1) : stable.
- Unity batch : N → N (stable).
- UnityLockfile : absent → absent (stable).
- Worktrees : 16 → 16 (stable).

### Output stdout
`Actifs:46 Stalled:39 Killed:0 Worktrees:16 UnityBatch:N`
