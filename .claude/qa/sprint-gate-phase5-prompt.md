# Sprint-gate Phase 5 PARITY-V4 — auto-qa-runner brief

> Invoqué après Wave 1 + Wave 2 mergées (target ≥ 8/8 P0 + 12/15 P1).
> Source critères : `.claude/plans/phase5-parity-v4-master.md` §2.2 + §1.4.

## Hard assertions (toutes doivent PASS)

1. **Editor Play mode W1-1 end-to-end** : Unity Editor Play, charge `Main.unity` → menu → "NEW GAME" ou "Continuer" → W1-1 → spawn → kill enemies → wave complete → next wave → victory. 0 NRE console.

2. **8/8 P0 mergés sur main** : 
   ```bash
   git log --oneline origin/main | grep -E "P0-(LVL|UI)-[1-6]" | wc -l  # ≥ 8 commit groups (multiple per ticket OK)
   ```

3. **Console clean post-Wave 1** : 0 nouvelle erreur runtime vs baseline pre-Phase 5 (commit `460a0b04`).

4. **Build batchmode WebGL exit=0** : 
   ```bash
   UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.74f1/Unity.app/Contents/MacOS/Unity"
   "$UNITY_PATH" -batchmode -nographics -projectPath /Users/mike/Work/crowd-defense -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit -logFile build.log
   echo $?
   ```

5. **Worktrees nettoyés** : `git worktree list` montre uniquement `/Users/mike/Work/crowd-defense [main]` + `/private/tmp/crowd-defense-v6 [gh-pages]`. Cleanup `.claude/worktrees/agent-*` si présent :
   ```bash
   for wt in /Users/mike/Work/crowd-defense/.claude/worktrees/agent-*; do
     [ -d "$wt" ] && git worktree remove --force "$wt"
   done
   ```

6. **Screenshots V4/V6 triplets** : `.claude/audit/screenshots/` contient au moins 6 triplets P0 visuels (UI-1 à UI-6). Captures via Chrome MCP (V4) + Unity-MCP (V6).

7. **LevelRegistry.asset 90 GUIDs** : 
   ```bash
   grep -oE 'world[0-9]+-[0-9]+' Assets/Resources/LevelRegistry.asset | sort -u | wc -l  # = 90
   ls Assets/ScriptableObjects/Levels/*.asset | wc -l  # = 90
   ```

8. **Pas de file > 1000 LOC introduit** :
   ```bash
   find Assets/Scripts -name '*.cs' -newer .git/refs/tags/sprint-r6-end | xargs wc -l 2>/dev/null | awk '$1>1000{print}'
   # ne doit retourner rien
   ```

## Soft assertions (LLM-judge Opus, ≥ 3/4 PASS)

1. **UX flow joueur W1-1** : screen captures + lecture HUD intuitive, lisibilité instantanée.
2. **Visual parity HUD V4 vs V6** : screenshots side-by-side (.claude/audit/screenshots/), regard subjectif.
3. **Performance acceptable** : 60 FPS Editor wave 5 (lit Stats panel ou Profiler).
4. **Pas de dégradation UX vs pre-Phase 5** : compare general flow.

## Archive

`.claude/qa/reports/phase5-final-YYYY-MM-DD.md` avec :
- Compteur P0/P1/P2 mergés.
- Liste commits Phase 5 (`git log --oneline 460a0b04..origin/main | wc -l`).
- Score parity quantitative (axes §2.1 du plan).
- Hard/soft assertions status.
- Verdict : GREEN / YELLOW / RED.

## Si RED

Loop fix + re-gate max 3 tentatives :
1. Identifier hard assertions failed.
2. Spawn bug-fixer agents pour fixer.
3. Re-run sprint-gate.
4. Si toujours RED après 3 tentatives → T1 notif Mike + STOP avec instructions reprise.

## Notify

```bash
# DONE
./.claude/supervisor/tools/notify.sh T1 "Phase 5 PARITY-V4 ✅ DONE" "8/8 P0 + N/15 P1 mergés, sprint-gate green, ~M commits"

# Bloquage
./.claude/supervisor/tools/notify.sh T1 "Phase 5 PARITY-V4 ⚠ BLOCKED" "<raison> — voir .claude/qa/reports/phase5-blocker-YYYY-MM-DD.md"
```
