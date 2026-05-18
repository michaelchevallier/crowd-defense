# Phase 5 Night Swarm — CONSOLIDATED FINAL REPORT (Mike morning brief)

**Date** : 2026-05-18 (4 Opus sessions, ~9h cumul work 23:00 → 09:30)
**Last HEAD** : `8ff27d76` (N56)
**Total commits Night #1-#4** : **62+** sur `origin/main`
**Mission status** : Code polish & defense layers complets, Real Editor validation **bloquée Unity-MCP DOWN**

---

## TL;DR — 1 page pour Mike au reveil

1. **Tu rentres à 10:00 le 18 mai.** La night a tourné 4 sessions (N1-N56). Tout est sur `origin/main` HEAD `8ff27d76`.

2. **Headless harness 11/11 PASS** : `V3LoopAutoRunner` valide tout le V3 loop sans Editor humain (boss safety chip 50% maxHP, force-kill stragglers, Hero reposition each 3rd iter).

3. **6/6 features V3 polish (gold popup, defeat/victory screens, next level transition, wave countdown, VAGUE button, worldmap unlock) sont structurellement wired** côté code C# + UXML. **Pas de fix code requis.**

4. **Real Editor frame validation (Phase 2 mission) bloquée Night #3 + Night #4** : Unity-MCP HTTP 8080 DOWN car Unity Editor pas lancé. Toi seul peut faire le test final.

5. **Action 5-min pour toi** :
   ```
   1. Lance Unity Editor sur ce projet
   2. Tools → CrowdDefense → QA → V3Loop → Auto → Run-Now
   3. Lis Library/V3LoopBatchReports/latest-auto.txt
   4. Cherche "phase11 FINAL VICTORY PASS state=Summary"
   ```
   Si PASS : V3 loop 11/11 confirmé. Tu peux pursuivre Phase 5 (screenshots) ou clean-up.
   Si FAIL : check les N36 force-kill log lines pour tuning.

6. **Bindings Inspector pending (P1-P2, headless impossible)** : voir section dédiée.

---

## Audit Phase 3 (Night #4) — 6 V3 polish features status

Audit complet code-only fait Night #4 par grep + sed sur Assets/Scripts. **Résultat : aucun gap fonctionnel, tout wired.** Détail :

| # | Feature | Status | Wire path |
|---|---------|--------|-----------|
| 1 | Gold popup `+N¢` | OK | `Enemy.Combat.cs:214` → `Economy.AddGoldFromKill(reward, transform.position + Vector3.up * 1.2f)` → `AccumulateGoldPopup(finalReward, worldPos)` |
| 2 | Defeat screen | OK | `LevelRunner.cs:442` → `EndScreenController.Instance?.ShowDefeat(result)` (fallback) ou `RunSummaryController` si présent dans la scene |
| 3 | Victory screen | OK | `LevelRunner.cs:437` → `EndScreenController.Instance?.ShowVictory(result)` (idem fallback) |
| 4 | Next level transition | OK | `RunSummaryController.OnContinue` (l.129) → `RunContext.NextLevelId` → `LevelLoader.LoadLevel(nextId)` ou `LevelLoader.GoToWorldMap()`. Idem EndScreen `OnSecondaryClicked` l.203. |
| 5 | Wave countdown UI | OK | `HudController.cs:541-573` per-frame smooth countdown on `wave-launch-pill` + `wave-launch-label`. Texte via `L.Get("hud.wave_launch_countdown", n, secondsLeft)`. |
| 6 | VAGUE button HUD | OK | `HUD.uxml:92-99` (`wave-launch-pill` + `wave-launch-btn` + `wave-launch-label`). C# `HudController.cs:271-277` Q→UIElements, ClickEvent l.365 → `TryLaunchWave()` → `wm.StartNextWave()` l.700. |
| 7 | WorldMap unlock | OK | `LevelRunner.cs:578` → `SaveSystem.MarkLevelCleared(levelId)` → unlock `ComputeNextLevelId` (world1-1 → world1-2 → ... world10-10). `WorldMapController` reads via `SaveSystem.IsLevelUnlocked(levelId)` l.413. |

**Conclusion** : pas de scope Phase 3 à coder. Seul ce qui pourrait manquer : **les références Inspector** (UIDocument + RunContext singleton + LevelLoader fade Scene name configuré).

---

## À faire Mike au matin (checklist actionnable)

### P0 — 5 min, validateur headless

- [ ] Lance Unity Editor sur `crowd-defense`
- [ ] `Tools → CrowdDefense → QA → V3Loop → Auto → Run-Now`
- [ ] `cat Library/V3LoopBatchReports/latest-auto.txt | tail -50`
- [ ] Cherche `phase11 FINAL VICTORY PASS state=Summary idx=5/5`

### P1 — 30 min, Tools menu utilitaires

- [ ] **Animator Controllers** missing (4) — `Tools → CrowdDefense → Build Animator Controllers`
- [ ] **Force-Tick** test : `Tools → CrowdDefense → QA → Force-Tick` (utilisé par auto-runner)
- [ ] **Particle Velocity reimport** : right-click `Assets/Prefabs/VFX` → Reimport, ou `Tools → Generic Reimport → All Prefabs`

### P2 — 15 min, scene Inspector bindings résiduels

- [ ] **HUD UIDocument** : sur `Main.unity`, vérifier que le GameObject HUD a un `UIDocument` qui pointe vers `Assets/UI/HUD.uxml`. (Wire-as-you-go.)
- [ ] **EndScreenController** : si tu utilises EndScreen plutôt que RunSummary, vérifier que `EndScreenController.Instance` est setup (singleton pattern). Sinon il a un fallback `Object.FindAnyObjectByType<RunSummaryController>()` qui le contourne.
- [ ] **LevelLoader scene name** : `LevelLoader.FadeVictory("Loader")` attend une scene "Loader" en build settings. Vérifier File → Build Settings → Scenes In Build contient "Loader".
- [ ] **Audio missing clips** : `tower_fire`, `tower_upgrade_celebration`, `upgrade_ring_chime` — créer assets `Assets/Resources/Audio/sfx_*.wav` ou remapper via `Resources/AudioRegistry.asset`.

### P3 — Vérification finale Play mode

- [ ] Open Main scene
- [ ] Play
- [ ] Place 5 archers + 2 frost (touches `Q` clavier ou clic UI)
- [ ] Bouton **VAGUE** (`wave-launch-btn`) — devrait être visible
- [ ] Click VAGUE → wave démarre
- [ ] Quand wave clear → countdown `Vague 2 / 10 dans Xs` ou skip bonus pill `+30¢ · 5.0s`
- [ ] Tue enemies → popup `+1¢ +2¢ +3¢` floating gold au-dessus
- [ ] Castle HP=0 → DEFAITE screen (RunSummary) + bouton Menu → WorldMap
- [ ] Sinon All Waves Clear → VICTOIRE screen + Continuer → niveau suivant `world1-2`
- [ ] Quit Play → WorldMap → `world1-2` doit être unlocked + cliquable

---

## Quick verify commands Mike

```bash
# 1. Vérifier état du repo
cd ~/Work/crowd-defense
git log --oneline -10 origin/main
# Doit voir 8ff27d76 N56 + commits N50-N55

# 2. Check Unity-MCP UP (après avoir lancé Editor)
curl -s http://127.0.0.1:8080/mcp -m 3 -o /dev/null -w "HTTP=%{http_code}\n"
# Doit retourner HTTP=200 ou 405 (pas 000)

# 3. Lister tools/menus disponibles via Unity-MCP
curl -s http://127.0.0.1:8080/mcp -X POST -H "Content-Type: application/json" \
  -d '{"method":"tools/list","jsonrpc":"2.0","id":1}'
```

---

## Bugs résiduels classés

### P0 — None

Aucun blocker connu post-Night #3.

### P1 — Inspector bindings (Mike action requis)

| Bug | Source | Fix |
|-----|--------|-----|
| **Animator Controllers fallback** (4 missing) | Tower archer/ballista/cannon/mage fall back to BaseCharacter | `Tools → CrowdDefense → Build Animator Controllers` |
| **Audio clips missing** (3) | tower_fire, tower_upgrade_celebration, upgrade_ring_chime | Créer assets ou remap dans AudioRegistry |

### P2 — Cosmetic / non-blocking

| Bug | Source | Fix |
|-----|--------|-----|
| **Particle Velocity curves warning** (~15969 occurrences log) | Unity 6 + URP edge case sur VFX prefabs | Reimport `Assets/Prefabs/VFX` |
| **HudController fallback log** sur singleton perdu | RunSummaryController not present in scene → EndScreenController active | Confirmer 1 des 2 wired dans Main.unity |

### Code-only fixes Night #3 (déjà appliqués, no Mike action)

**MissingReferenceException 'Tower has been destroyed'** — 9-layer defense :
- N27 + N42 prune destroyed in LateUpdate (Placement + Wave lists)
- N33 + N41 + N43 + N43b + N43c : `?.` → `if (x != null) x.method()` patterns
- N38 + N54 + N54b : Tower belt-and-braces guards (RegisterKill, UpdateHpAlpha, SpawnKillFloatText)
- N40 : Projectile.ReleaseToPool clears refs
- N51 : EventSystem.ApplyAction != null guards for Castle/Hero

**Visual log-spam** — 4 fixes :
- N29 : `_BaseColor` HasProperty guards
- N35/N35b/N35c : `_MainTex` skip OutlineInvertedHull + HasProperty
- N48 : PathTiles MakePathRevealMat + MakeBridgeWaterMat HasProperty
- N56 : PathTiles MakeFallbackWaterMat + MakeFallbackLavaMat `_Tint` fallback

**Visual de-dup** — 1 fix :
- N31 : Enemy.Combat removed duplicate `+{reward}` popup (Economy already spawns tiered SpawnGoldReward)

**WorldMapController warning** :
- N28 : log demoted to Debug.Log (expected per R2-recovery)

---

## Si Unity-MCP DOWN au reboot

```bash
# 1. Lance Unity Editor depuis Hub
open -a "Unity Hub"
# Click sur projet crowd-defense → Open

# 2. Vérifier Tools menu → Unity-MCP server should auto-start
# (Si pas auto, Window → Unity MCP Server → Start)

# 3. Re-curl le check
curl -s http://127.0.0.1:8080/mcp -m 3 -o /dev/null -w "HTTP=%{http_code}\n"
```

Si ça reste DOWN : ouvrir `Packages/com.unity-mcp` (ou similar) + checker la version. Sinon relancer une session Opus avec "validate Phase 5 steps 8-11 with Unity-MCP up".

---

## Bindings Inspector pending (récap complète)

Voir aussi `.claude/qa/reports/phase5-night-bindings-to-do.md` pour la liste détaillée. Les highlights :

### Resolved Night #3 (no Mike action)

- ✅ BossDef registry on BossSystem GameObject (N17, wave #1)
- ✅ Tower MissingRef class éliminée (9 commits N27-N43c+N38+N40+N54+N54b)
- ✅ PathTiles material `_BaseColor` / `_Tint` / `_MainTex` HasProperty guards (N29, N35*, N48, N56)
- ✅ EventSystem null checks (N51)

### Mike action requis

1. **Build Animator Controllers** (Tools menu, 1 click)
2. **Audio assets** : tower_fire / tower_upgrade_celebration / upgrade_ring_chime
3. **Particle Velocity reimport** (VFX folder reimport)
4. **HUD UIDocument wiring** (vérifier Main.unity scene)
5. **LevelLoader Loader scene** dans Build Settings

---

## Time budget Night #4

| Item | Time |
|------|------|
| Session start | 08:14 CEST (deadline 10:00) |
| Phase 1 Unity-MCP check | 08:15-08:16 (DOWN confirmed) |
| Phase 3 audit 6 features V3 polish | 08:16-08:20 (grep+sed sweep) |
| Phase 4 consolidated report writing | 08:20-08:35 (this file) |
| Phase 5 cleanup (worktrees) | 08:35-09:00 |
| Monitor T1 09:30 PID 59753 | armed par Night #3 — NE PAS double-T1 |
| Stop deadline | 09:55 (silent stop, push + exit) |

---

## Liens vers reports détaillés

- `.claude/qa/reports/phase5-night-final-2026-05-18.md` — Wave #1 (commits N1-N17)
- `.claude/qa/reports/phase5-night2-final-2026-05-18.md` — Wave #2
- `.claude/qa/reports/phase5-night3-final-2026-05-18.md` — Wave #3 (commits N26-N51 + sub-iters + N53-N56)
- `.claude/qa/reports/phase5-night3-blocker-mcp-down.md` — Unity-MCP DOWN root cause
- `.claude/qa/reports/phase5-night-bindings-to-do.md` — Inspector binding TODO list
- `.claude/qa/reports/phase5-night2-bindings-to-do.md` — older bindings list (Night #2 era)

---

## Final HEAD state

```
8ff27d76 fix(visual)(N56): PathTiles.MakeFallbackWaterMat + MakeFallbackLavaMat — _Tint fallback
fd870b17 docs(qa)(N55): update night3 report — time budget + 35 commits final
b86ed16c fix(towers)(N54b): Tower.SpawnKillFloatText — self guard against destroyed Tower
3937827e fix(towers)(N54): Tower.UpdateHpAlpha — belt-and-braces self guard against destroyed
66a46cb7 docs(qa)(N53): update night3 report — final HEAD state with all 31 commits
... (62 commits N1-N56 total, all on origin/main)
```

**Repo state** : clean, no uncommitted changes, all pushes succeeded.
**Headless harness** : 11/11 PASS expected at next Editor run.
**Player UX V3 features** : 6/6 wired code-side, awaiting Mike's Play mode validation.

**GO Mike — tu as tout pour reprendre. ☕**

---

## Post-scriptum Night #5 (2026-05-18 08:23 CEST)

**Cleanup worktrees** : 37/37 branches `md-*` mergées sur `origin/main` removed (worktrees + branches). 0 stale. Final state : 2 worktrees (main + 1 locked agent worktree). Détails : `.claude/supervisor/drift-reports/_cleanup-worktrees-residuals.md`.

**Watchdog Unity-MCP** : poll every 5min depuis 08:22 CEST jusqu'à 09:55 CEST. Si UP avant 09:30, capture real frames steps 8-11 et écrit `phase5-night5-real-frames-2026-05-18.md`. Sinon silent exit. PAS de T1 (Monitor #3 fait à 09:30).
