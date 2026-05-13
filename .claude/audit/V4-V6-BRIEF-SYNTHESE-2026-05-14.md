# Brief synthèse V4 → V6 — 2026-05-14 (post-5-audits)

> **V4** = `src-v3/` (Crowd Defense V3 Three.js, déployé sur URL `/v4/` par abus de langage).
> **V6** = `crowd-defense/` Unity 6 LTS (deployed `/v6/`, sprint R7-PUSH-100 actif).
> Périmètre parity = Crowd Defense V3 (PAS Park Defense `src-v2/` qui est hors scope).
> 5 audits parallèles livrés dans `.claude/audit/V4-V6-*-2026-05-14.md`.

---

## 1. État global parity

| Axe | Status | Score | Headline |
|---|---|---|---|
| **Enemies** | ✅ **PARITY 100%** | 28/28 types | V6 enrichi (variants Fast/Tough/Regen, élite W5+). Manque popup +1 / BOSS DOWN. |
| **Assets/Rendu** | 🟢 **V6 > V4** sur 8/13 axes | ~85% | Shaders URP, post-process, particles, GLTFs, hero, audio. Manque : tower L2/L3 meshes (5/13), cutscenes ASCII, audio coverage 72%. |
| **Levels** | 🟡 **PARITY structurelle, gaps fonctionnels** | 10/10 mondes | 90 campagne (+10 W*-9 vs V4 80). **Régression critique : `forbiddenTowers`/`bonusTowers` non sérialisés (169 occurrences perdues)**. |
| **UI/HUD** | 🔴 **80 écarts** | ~50% | 9 P0 (Encyclopédie, Hero portrait, **Run Mode + 3 Magic Schools**, wave banners, tiles spéciales worldmap). V6 a aussi 13 features en plus que V4. |
| **Gameplay loops** | 🟢 **Parity 14/18** | ~78% | Anomalies à acter : 2 systèmes Daily concurrents (`Daily.cs` orphelin), désync 10 SOs vs `WorldCount=8`. |

**Verdict global** : V6 est **~75-80% iso V4** (cohérent avec STATUS R7-PUSH-100 cible 85→95%). Quelques régressions silencieuses critiques à fixer (forbiddenTowers, LevelRegistry 9 orphelins, 2× Daily). Côté visuel V6 surpasse souvent V4. Le grand chantier reste **UI/HUD** (9 P0).

---

## 2. Arbitrages binaires à trancher Mike

| # | Question | Reco | Effort si OUI | Effort si NON |
|---|---|---|---|---|
| **A1** | Porter le **Run Mode + 3 Magic Schools** complets (Fire/Earth/Ice — V4 src-v3 feature majeure) ? | **OUI** (P0 UI/HUD #1 + différenciation gameplay forte) | 15-20 commits Sonnet | -9 P0 brouhaha, V6 reste sans Run Mode |
| **A2** | Porter **Boss Rush mode** (V4 `boss_rush.js` non porté) ? | **OUI** (P1, ~3-5 commits, peu coûteux et identitaire) | 3-5 commits | OK skip, juste retirer la tile du worldmap si visible |
| **A3** | Refondre les **3 levels multi-castle** (W5-8, W9-8, W10-8) en mono-château (policy refonte) ? | **OUI** (D2-02 spec déjà actée Q-N) | 3 commits level-designer | rester en violation policy |
| **A4** | Régler la **désync 10 SOs vs WorldCount=8** : étendre WorldMapController à 10 OU supprimer SOs W9-W10 ? | **A : Étendre à 10** (cohérent avec +10 W*-9 endgame déjà créés) | 1 commit | Perte de 18 levels W9-W10 déjà créés |
| **A5** | Désambiguïser **2 systèmes Daily** : garder `DailyChallenge.cs` câblé OU `Daily.cs` orphelin ? | **A : Garder `DailyChallenge.cs`** (déjà câblé sur UI) | 1 commit cleanup | Maintenance dette technique |
| **A6** | **Cutscenes ASCII art emoji-based** V4 → port ? | **NON** (V6 enrichi avec speaker/portrait/side déjà mieux) | 5-10 commits port + design | OK V6 reste différent assumé |
| **A7** | **Tower L2/L3 meshes** manquants pour 7-8 tours (cannon/crossbow/fan/frost/magnet/mine/portal/skyguard) — porter ? | **OUI** mais P2 (visuel polish) | 8 commits asset-gen | upgrade visible que par tint, joueur sent moins le L3 |

**Note** : Les 6 features "absent" listées par l'audit Gameplay Loops (Carnaval, Mini-jeux Foire, Coop 2P, Cutscenes choices branchées, Webfonts Bangers/Fredoka, Mode Arène Boss) sont **HORS SCOPE** car Park Defense `src-v2/`, pas Crowd Defense `src-v3/`. À confirmer mais sauf indication contraire = on ignore.

---

## 3. Backlog priorisé (mapping vers R7-PUSH-100)

### P0 — Game-breaking ou régression silencieuse (8 tickets)

| ID | Ticket | Source audit | Effort |
|---|---|---|---|
| P0-LVL-1 | **Port `forbiddenTowers` + `bonusTowers` sérialisation** (169 occurrences perdues) | Levels | 1-2 commits |
| P0-LVL-2 | **Régénérer `LevelRegistry.asset`** pour inclure 9 W*-9 orphelins | Levels | 1 commit |
| P0-UI-1 | **Run Mode** port complet (gating run quotidien, daily streak, etc.) | UI/HUD | 8-10 commits |
| P0-UI-2 | **3 Magic Schools** (Fire / Earth / Ice — système synergies tower) | UI/HUD | 8-12 commits |
| P0-UI-3 | **Encyclopédie HUD** (codex tours + ennemis accessible mid-game) | UI/HUD | 4-5 commits |
| P0-UI-4 | **Hero portrait HUD** | UI/HUD | 2 commits |
| P0-UI-5 | **Tiles spéciales worldmap** (Endless / Daily / Boss-rush) | UI/HUD | 3 commits |
| P0-UI-6 | **Wave-start + wave-clear banners** | UI/HUD | 2 commits |

### P1 — UX dégradée (parmi 31 UI + 5 Levels + 4 Gameplay + 4 Assets)

Top 15 P1 :
- P1-UI-1 Top-bar Shop/Map/Encyclopedia buttons
- P1-UI-2 Gems balance pill HUD
- P1-UI-3 Behavior badges sur tower preview
- P1-UI-4 Synergy tooltips toolbar
- P1-UI-5 Locked / forbidden state toolbar (lié P0-LVL-1)
- P1-UI-6 Briefing modal "name + countdown 3-2-1"
- P1-UI-7 Pause menu sub-settings/help
- P1-UI-8 Support mode (2 échecs = aide auto)
- P1-LVL-3 Boss Rush mode port (3-5 commits, A2 = OUI)
- P1-LVL-4 Refondre 3 multi-castle levels W5-8/W9-8/W10-8 (A3 = OUI)
- P1-LVL-5 Désync 10/8 worlds (A4 = étendre à 10)
- P1-GP-1 Désambiguïser 2× Daily (A5 = keep DailyChallenge)
- P1-GP-2 Daily streak counter port
- P1-EN-1 Popup +1 vert / BOSS DOWN! or / ring boss death
- P1-AST-1 Verify skybox V8I wiring 10 slots Inspector

### P2 — Polish (27 + 13 mineurs)

40+ items polish (peut être délégué à un Sonnet `juice-polisher` en wave dédiée). Voir audits source pour détail.

---

## 4. Mécanisme screenshots validation

### Workflow proposé

Pour chaque ticket P0/P1 **visuel** :

1. **Capture V4 référence** (1 fois) : Chrome MCP sur `https://michaelchevallier.github.io/lava_game/v4/`, screenshot dans `.claude/audit/screenshots/V4-<feature>-<context>.png`
2. **Capture V6 avant** (1 fois par ticket) : Unity Editor Play mode via Unity-MCP, screenshot dans `.claude/audit/screenshots/V6-<feature>-before-<ticket>.png`
3. **Capture V6 après** (post-commit) : idem, `.claude/audit/screenshots/V6-<feature>-after-<ticket>.png`
4. **Ticket close** : screenshot triplet joint, validation visuelle.

### Tickets non-visuels (régressions code)

P0-LVL-1, P0-LVL-2, P1-GP-1 (cleanup Daily), P1-AST-1 (skybox wiring) → pas de screenshots, validation = `npm run test:crowdef` + grep .asset + console clean.

### Outils

- **Chrome MCP** : `mcp__claude-in-chrome__navigate` + `mcp__claude-in-chrome__gif_creator` (GIF si animation)
- **Unity-MCP** : execute_code → `EditorApplication.EnterPlaymode()` + `ScreenCapture.CaptureScreenshot()`

---

## 5. Formulations /goal candidates

### Goal A — Sprint R7 finishing (~2-3 semaines)
> Atteindre **95%+ parité visible V4 (src-v3 Crowd Defense V3) → V6 Unity** validée en Unity Editor Play mode + screenshots side-by-side. Livrer les 8 P0 + top 15 P1 du backlog audit `V4-V6-BRIEF-SYNTHESE-2026-05-14.md`. Arbitrages A1-A7 résolus (Mike décide AVANT dispatch). Stop = sprint-gate auto-QA report green archivé dans `.claude/qa/reports/`, 8/8 P0 mergés, 12+/15 P1 mergés, screenshots V4/V6 triplets pour tickets visuels.

### Goal B — Parity tactique (~1 semaine, scope serré)
> Fermer le top **8 P0** du backlog audit V4↔V6 (forbiddenTowers, LevelRegistry, Run Mode, Magic Schools, Encyclopédie, Hero portrait, tiles spéciales worldmap, wave banners). Dispatch parallèle Sonnet feature-dev (worktrees). Stop = 8/8 P0 mergés sur main + Unity Editor Play mode sans nouvelle erreur console + screenshots validation joints.

### Goal C — Strategic full-stack (~4-6 semaines)
> Livrer Phase 5 du plan MIGRATE Crowd Defense : parity 95% + iOS Xcode build + Steam SDK integration. Inclut 8 P0 + 25+ P1 audit V4↔V6 + tickets Phase 5 build natif. Stop = build iOS .ipa + Steam upload OK + parity validation report green.

**Recommandation** : **Goal A** — scope clair (audit-driven), critère mesurable (95% + sprint-gate green), durée raisonnable (2-3 sem), pas de friction avec ton plan MIGRATE existant.

---

## 6. Next action (proposé)

1. **Toi** : valides ou modifies arbitrages A1-A7 (~5 min skim ce brief).
2. **Toi** : choisis le /goal (A/B/C) et le poses.
3. **Moi** : génère les **8 briefings P0 ready-to-dispatch** dans `.claude/supervisor/instructions-to-exec.md` pour la session exec crowd-defense (qui dispatchera en autonomie en worktrees parallèles).
4. **Moi superviseur** : continue scrutes /loop 30m, surveille progression, sprint-gate auto-QA en fin.
5. **Exec session crowd-defense** : ack instructions, dispatch P0 wave 1 (4 tickets parallèles workspace : LVL-1, UI-3, UI-4, UI-5 — pas de zone fichier conflictuelle).

---

## Sources

- `.claude/audit/V4-V6-UI-HUD-2026-05-14.md` (32 KB)
- `.claude/audit/V4-V6-ENEMIES-2026-05-14.md` (28 KB)
- `.claude/audit/V4-V6-LEVELS-2026-05-14.md` (37 KB)
- `.claude/audit/V4-V6-ASSETS-RENDU-2026-05-14.md` (25 KB)
- `.claude/audit/V4-V6-GAMEPLAY-LOOPS-2026-05-14.md` (21 KB)
- Master plan `crowd-defense/STATUS.md` (Phase 4 SHIPPED, sprint R7-PUSH-100 actif)
- Charter superviseur `crowd-defense/.claude/supervisor/charter.md`
