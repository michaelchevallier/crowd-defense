# Crowd Defense V6 — Phase 5 PARITY-V4 (master plan consolidé)

> **Date** : 2026-05-14
> **Status** : ✅ Plan validé. Prêt à exécuter via /goal.
> **Mode** : autonomie totale, parallélisation maximale, full completion.
> **Source unique de vérité** pour la prochaine session Opus en charge de la Phase 5.

---

## 0. TL;DR

**Mission** : atteindre ≥95% parity visible **V4** (= `src-v3/` Crowd Defense V3 Three.js, deployed `https://michaelchevallier.github.io/lava_game/v4/`) → **V6** Unity Editor Play mode.

**État courant** :
- Phases 0-4 Unity SHIPPED (660 commits, /v6/ deployed).
- Sprint R7-PUSH-100 actif côté exec.
- 5 audits parallèles 2026-05-14 livrés → **8 P0, ~15 P1, 40+ P2** identifiés.
- Brief synthèse : `.claude/audit/V4-V6-BRIEF-SYNTHESE-2026-05-14.md`.

**Scope Phase 5** :
- **Wave 1** : 8 P0 game-breaking en parallèle (~6-10h wall-clock).
- **Wave 2** : top 15 P1 UX dégradée en parallèle (~8-12h).
- **Wave 3** : 40+ P2 polish via `dispatch.sh` runner 6 slots (~12-16h).
- **Sprint-gate** auto-qa-runner fin.

**Durée totale estimée** : ~30-40h wall-clock autonome.

**Mode** :
- Parallélisation maximale (6-8 worktrees simultanés).
- Décisions A1-A7 pré-arbitrées (§3.2).
- Aucune validation Mike au fil de l'eau.
- Stop = DONE OU bloquage dur uniquement.

**Pour démarrer Phase 5** :
1. Mike skim §3.2 (override arbitrages si besoin) + §4 (scope ajustement) + §11 (/goal).
2. Mike lance nouvelle session Claude Code dans `/Users/mike/Work/crowd-defense`.
3. Mike colle le /goal du §11.
4. Opus prend le relais en autonomie.

**Voir §12 + §13 pour la liste des étapes Mike détaillées.**

---

## 1. Contexte

### 1.1 Historique

**2026-05-11** : pivot Unity acté par Mike (Q18=B après lecture R3-02 portability research). Le plan original « refonte Phaser 8 semaines R1→E2 » archivé.

**Décisions actées préservées** (Q1-Q18 cf §3.1) : économie tendue, pacing joueur-driven, level design grand-format, mono-château, multi-portails convergents.

**2026-05-12 → 2026-05-14** : Phases 0-4 du sprint MIGRATE Unity SHIPPED (~660 commits, ~5h exécution multi-agent swarm) :
- Phase 0 : Setup Unity + MCP.
- Phase 1 : POC W1-1 jouable.
- Phase 2 : Core (12 tours, 30 ennemis, économie, pathfinding, waves).
- Phase 3 : Visuels (shaders URP, post-process, VFX pool, audio mixer, GLTFs).
- Phase 4 : UI polish (HUD, EndScreen, TowerCompare, WorldMap bookmark, SplashScreen) + /v6/ deployed.

### 1.2 État actuel (sprint R7-PUSH-100 actif)

Côté `crowd-defense/.claude/supervisor/active-sprint.md` : sprint **R7-PUSH-100** ACTIVE 2026-05-13, mode autonome dispatch backlog 25 tickets P0/P1/P2/P3.

**5 audits parallèles livrés 2026-05-14** (`general-purpose` agents spawned by Opus milan-project superviseur) :
1. `.claude/audit/V4-V6-UI-HUD-2026-05-14.md` (32 KB) — 80 écarts (9 P0, 31 P1, 27 P2 + 13 V6>V4).
2. `.claude/audit/V4-V6-ENEMIES-2026-05-14.md` (28 KB) — parité 100% (28/28 types). V6 enrichi.
3. `.claude/audit/V4-V6-LEVELS-2026-05-14.md` (37 KB) — parité structurelle. Régression `forbiddenTowers` critique.
4. `.claude/audit/V4-V6-ASSETS-RENDU-2026-05-14.md` (25 KB) — V6 surpasse V4 sur 8/13 axes. Lag sur 3 (tower L2/L3 meshes, cutscenes ASCII, audio events 72%).
5. `.claude/audit/V4-V6-GAMEPLAY-LOOPS-2026-05-14.md` (21 KB) — 14/18 parity. 2 anomalies (2× Daily concurrents, désync worldcount).

**Brief synthèse** : `.claude/audit/V4-V6-BRIEF-SYNTHESE-2026-05-14.md`.

### 1.3 Définition « V4 »

**V4 = `src-v3/`** dans le repo `milan project/` = Crowd Defense V3 Three.js kingshot legacy.
**Abus de langage** : déployé sur URL `https://michaelchevallier.github.io/lava_game/v4/`.

Park Defense (`src-v2/`, Phaser PvZ tile-defense, déployé `/v2/`) est **HORS SCOPE** (cf §3.3).

### 1.4 Définition « DONE » Phase 5

La Phase 5 PARITY-V4 est terminée quand TOUS ces critères sont validés :

1. **8/8 P0 mergés sur `main`** avec screenshots V4/V6 triplets joints dans `.claude/audit/screenshots/`.
2. **12+/15 P1 mergés** (latitude sur 3 P1 si bloquage non-critique).
3. **Sprint-gate auto-QA report GREEN** archivé dans `.claude/qa/reports/phase5-final-YYYY-MM-DD.md` :
   - 8 hard assertions PASS (cf §2.2).
   - 4 soft assertions ≥ 3/4 PASS.
4. **Console Unity Editor Play mode** = 0 nouvelle erreur post-Wave 1 (vs baseline pre-Phase 5).
5. **Aucun ticket P0 réouvert** après merge (régression-free).
6. **Build batchmode WebGL** success exit=0 (validation que le port ne casse pas le pipeline).

---

## 2. Objectifs mesurables (cibles QA)

### 2.1 Parity quantitative (≥95% iso V4)

| Axe | Score actuel (audit) | Cible Phase 5 | Mesure |
|---|---|---|---|
| Enemies | 100% (28/28) | 100% | Counter EnemyType.cs / src-v3 enemy types. |
| Assets/Rendu | 85% (8/13 V6>V4) | 90% | Tower L2/L3 meshes 5/13 → 13/13 (P0-AST-1). Cutscenes ASCII NON-PORTÉ assumé (A6=NON). Audio coverage 72% → 90%. |
| Levels | 75% structurel | 95% | forbiddenTowers/bonusTowers sérialisés (P0-LVL-1) + LevelRegistry régen (P0-LVL-2) + 3 mono-château refonte (P1-LVL-4) + worldcount=10 (P1-LVL-5). |
| UI/HUD | 50% (9 P0 + 31 P1) | 95% | 8 P0 mergés + 12+/15 P1 mergés. |
| Gameplay loops | 78% (14/18) | 95% | Cleanup 2× Daily (P1-GP-1) + streak counter (P1-GP-2) + Boss Rush mode (P1-LVL-3). |

**Score global cible** : ≥95% pondéré par axe.

### 2.2 Sprint-gate auto-QA criteria

**Hard assertions** (toutes doivent PASS) :
1. Unity Editor Play mode : level W1-1 jouable end-to-end (spawn → kill enemies → wave complete → next wave → victory).
2. Toutes 8 P0 mergées sur main (commits visibles via `git log`).
3. Console clean post-Wave 1 (0 nouvelle erreur runtime).
4. Build batchmode WebGL exit=0.
5. Tous worktrees mergés ou nettoyés (`git worktree list` = main + gh-pages only).
6. `.claude/audit/screenshots/` contient au moins 6 triplets V4/V6 (1 par P0 visuel).
7. LevelRegistry.asset contient 90 GUIDs (= 90 fichiers .asset dans Assets/ScriptableObjects/Levels/).
8. Pas de file > 1000 LOC introduit (anti-god-class hygiene).

**Soft assertions** (LLM-judge Opus, ≥3/4 PASS) :
1. UX flow joueur sur W1-1 : intuitive, lecture HUD instantanée.
2. Visual parity HUD V4 vs V6 (screenshots side-by-side).
3. Performance acceptable (60 FPS Editor en wave 5).
4. Pas de UX dégradation visible vs pre-Phase 5.

### 2.3 Performance baseline

- **60 FPS Unity Editor Play mode** en wave 5 (mid-difficulty).
- **GC.Alloc < 1 KB/frame** en swarm wave (mob count > 50).
- Si bottleneck détecté → spawn `perf-auditor` (cf §9.7).

### 2.4 Console clean

Diff console output : `console-baseline-pre-phase5.txt` vs `console-final-phase5.txt`.
**Critère** : 0 NEW error message (warnings tolérés).

---

## 3. Décisions Mike (acquis + arbitrages)

### 3.1 Décisions Q1-Q18 actées 2026-05-11

**Économie** :
- Q1 Anti-spam : **A modifié — interest leak penalty** (ETD2 style).
- Q2 Ratio L3/L1 : **B — ×5**.
- Q3 Boss reward : **B — 0× pur cost sink**.
- Q4 Magnet : **A modifié — nerf doux + cap stratégique** (range 5, coinMul ×1.3, cost 130, max 1-2/level).

**Pacing** :
- Q5 Auto-start : **B — OFF** (joueur clique bouton).
- Q6 Skip bonus : **A — KR-style** (+30¢ si clic dans 5s, streak +5%/wave cap +25%).
- Q7 Cap temporel : **B — liberté infinie**.

**Map design** :
- Q8 Densité buildable : **B — 50-60%** (PCWL DR T B).
- Q9 Path mode : **B — hybride avec extension `J`** (junction probabiliste).

**Synergies/L3** :
- Ratio L3 ×4 vs L2, réversibilité via pelle, différenciation stat-by-stat, synergies +50%→+25% ✓.

**Difficulty** :
- Q10 Castle HP : **A+B combiné** (castle solide + pression mob).
- Q11 Rupture : **B — décaler à W5→W6** (vraie difficulté plus tôt).

**Milan audit** :
- 15 levels multi-C à refondre en mono-C, cost hikes crossbow/mage/mine +20% ✓.

**Perf** :
- Q12 Cap map : **B — 25×19 + refactor InstancedSkinnedMesh** post-MVP.

**Auto-QA** :
- Q13 Browser substrat : **B — +Chrome DevTools MCP** Google.
- Q14 LLM-judge : **A — même Opus exec + juge**.
- Q15 Trigger sprint-gate : **A — invocation manuelle Mike** initial, migration cron en E1/E2 si fluide.

**Process** :
- Q16 Validation D1 : **individuelle** (skim 5 min/spec).
- Q17 D1 démarrage : **MAINTENANT en parallèle de R3**.
- Q18 Pivot Unity : **B — migrer à Unity** (acté après R3-02 lecture). Plan 8 sem Phaser CANCELLED.

### 3.2 Arbitrages A1-A7 — PRÉ-DÉCIDÉS

**IMPORTANT** : Ces 7 arbitrages sont appliqués par défaut dans le backlog Phase 5. Mike peut override en éditant cette section AVANT de taper le /goal au §11. Une fois /goal tapé, ces décisions sont **sealed**.

| # | Question | **DÉCISION** | Justification reco |
|---|---|---|---|
| **A1** | Porter **Run Mode + 3 Magic Schools** (Feu/Givre/Maçonnerie) ? | **OUI** | Identité gameplay forte V4. 9 P0 UI dépendent. Bloque parité 95%. 15-20 commits Sonnet. |
| **A2** | Porter **Boss Rush mode** (`boss_rush.js`) ? | **OUI** | P1 cheap (3-5 commits), identitaire mode. |
| **A3** | Refondre **3 multi-castle levels** (W5-8, W9-8, W10-8) en mono-château ? | **OUI** | Policy refonte Q9-Q11 actée. 3 commits level-designer. |
| **A4** | Désync 10 SOs vs `WorldCount=8` — **Étendre à 10** ou supprimer SOs W9-W10 ? | **A : Étendre à 10** | +10 W*-9 endgame déjà créés (R6-PARITY-012 wave events). 1 commit. |
| **A5** | 2× systèmes Daily concurrents — Garder `DailyChallenge.cs` câblé OU `Daily.cs` orphelin ? | **A : Garder `DailyChallenge.cs`** | Déjà câblé sur UI. 1 commit cleanup. |
| **A6** | **Cutscenes ASCII art emoji V4** → port ? | **NON** | V6 a portraits/speaker/side enrichis. Différence assumée. Évite 5-10 commits redondants. |
| **A7** | **Tower L2/L3 meshes** manquants (7-8 tours : cannon/crossbow/fan/frost/magnet/mine/portal/skyguard) — porter ? | **OUI** | P2 visuel mais visible : joueur sent moins le L3 si juste tint. 8 commits asset-gen, parallèles. |

**Override** : pour modifier une décision, édite la cellule "DÉCISION" ci-dessus AVANT de taper le /goal. Format : `**OUI**` / `**NON**` / `**A**` / `**B**`.

### 3.3 Hors scope (Park Defense `src-v2/`)

Les features suivantes sont **NE PAS PORTER** car elles appartiennent à Park Defense Phaser PvZ (`src-v2/`, déployé `/v2/`), pas Crowd Defense Three.js (`src-v3/`) :

1. **Mode Carnaval** (5 levels conveyor belt).
2. **Mode Arène des Boss** (3 vagues back-to-back, 3 boss).
3. **Cutscenes choices branchées** (W4/W7/W10).
4. **Coop 2P split-clavier** (curseur cyan P2).
5. **5 Mini-jeux Foire** (chamboule-tout, tir pigeon, roue, course crêpes, bumper cars).
6. **Webfonts Bangers/Fredoka** (Google Fonts via index.html — V6 utilise Roboto TMP Font Asset).

Si Mike change d'avis : édite §3.3 et ajoute au scope §4 manuellement.

---

## 4. Backlog priorisé (post-audit V4↔V6)

### 4.1 Wave 1 — 8 P0 (game-breaking, ~6-10h parallèles)

| ID | Titre | Effort | Type | Files clés | Zone |
|---|---|---|---|---|---|
| **P0-LVL-1** | `forbiddenTowers` + `bonusTowers` sérialisation | 1-2 commits | bug-fixer | `LevelData.cs`, `TowerToolbarController.cs` | Levels |
| **P0-LVL-2** | `LevelRegistry.asset` régénération (9 W*-9 orphelins) | 1 commit | bug-fixer | `LevelRegistry.asset`, Editor menu | Levels |
| **P0-UI-1** | **Run Mode** port complet (Feu/Givre/Maçonnerie, 7 actes, nodes, abandon) | 8-10 commits | feature-dev | `RunModeController.cs` (new), `RunMap.uxml` | UI |
| **P0-UI-2** | **3 Magic Schools** selection screen (Fire/Earth/Ice, 3 cards icon+desc+perks) | 4-6 commits | feature-dev | `SchoolSelectScreen.uxml` (new), controller | UI |
| **P0-UI-3** | **Encyclopédie HUD** (codex Tours + Ennemis + Perks mid-game) | 4-5 commits | feature-dev | `EncyclopediaPanel.uxml` (new), `HudController.cs` btn | UI |
| **P0-UI-4** | **Hero portrait HUD** (binding `HeroPortraitController` skin + emoji 👑) | 2 commits | feature-dev | `HudController.cs` heroPanel, `HeroPortrait.uss` | UI |
| **P0-UI-5** | **Tiles spéciales worldmap** (Endless/Daily/Boss-rush dans WorldMap.uxml) | 3 commits | feature-dev | `WorldMap.uxml`, `WorldMapController.cs` | UI |
| **P0-UI-6** | **Wave-start + wave-clear banners** (slide-in 1.2-1.5s + binding WaveEvents) | 2 commits | feature-dev | `WaveBannerController.cs`, `HUD.uxml` | UI |

**Total Wave 1** : ~25-29 commits, ~6 worktrees simultanés max.

**Briefings P0 ready-to-dispatch** détaillés en **§7**.

### 4.2 Wave 2 — top 15 P1 (UX dégradée, ~8-12h parallèles)

Source : `.claude/audit/V4-V6-UI-HUD-2026-05-14.md` items #10-26 + `V4-V6-LEVELS-2026-05-14.md` §4.2 + `V4-V6-ENEMIES-2026-05-14.md` P1.

| ID | Titre | Effort |
|---|---|---|
| **P1-UI-1** | Top-bar Shop/Map/Encyclopedia buttons | 2 commits |
| **P1-UI-2** | Gems balance pill HUD (#gems 💎 N) | 1 commit |
| **P1-UI-3** | Behavior badges sur tower preview (explosion/perce/slow/aura/etc.) | 3 commits |
| **P1-UI-4** | Synergy tooltips toolbar (tt-syn-tip au hover) | 2 commits |
| **P1-UI-5** | Locked / forbidden state toolbar (toolbar-cell--locked) | 1 commit (lié P0-LVL-1) |
| **P1-UI-6** | Briefing modal "name + briefing text + countdown 3-2-1" | 3 commits |
| **P1-UI-7** | Pause menu sub-settings/help (btn-settings-from-pause) | 2 commits |
| **P1-UI-8** | Support mode (2 échecs = aide auto +15% HP / +20% or / -15% ennemis) | 3 commits |
| **P1-LVL-3** | Boss Rush mode port (A2=OUI) | 3-5 commits |
| **P1-LVL-4** | Refondre 3 multi-castle levels (W5-8, W9-8, W10-8) en mono-C (A3=OUI) | 3 commits (level-designer) |
| **P1-LVL-5** | Désync worlds : étendre WorldMapController à 10 (A4=Étendre) | 1 commit |
| **P1-GP-1** | Cleanup 2× Daily : keep `DailyChallenge.cs`, drop `Daily.cs` orphelin (A5) | 1 commit |
| **P1-GP-2** | Daily streak counter port | 2 commits |
| **P1-EN-1** | Popup +1 vert / BOSS DOWN! or / ring boss death | 2 commits |
| **P1-AST-1** | Verify skybox V8I wiring 10 slots Inspector SkyboxController | 1 commit |

**Total Wave 2** : ~30-37 commits, ~6-8 worktrees simultanés.

### 4.3 Wave 3 — 40+ P2 (polish, dispatch via `tasks.txt` + audits)

Source : agrégation audits P2 + `.claude/master-dispatch/tasks.txt` existant (37 tickets formattés pipe-delimited).

Mécanisme : **runner.sh loop 60s + 6 slots simultanés**, pas de wave one-shot. Continue jusqu'à tasks.txt épuisé.

Catégories P2 :
- **Animations / juice** : star reveal, gems flash, coin-fly, sell-undo toast.
- **Tower upgrade L2/L3 meshes** : 7-8 tours (A7=OUI, asset-gen Sonnet).
- **Settings panel** : camera sliders (hauteur/distance/FOV), zoom slider.
- **Boss banner décorateur** 💀…💀, combo tracker visual parity.
- **WorldMap** : header progress, run header (Lancer Run / Reprendre / Abandonner), runStats, debug menu.
- **Cutscene** : style cartoony emoji-portrait (A6=NON donc skip cette catégorie).
- **Achievements popup parity**, skin equip/drop sounds.
- **Hint keys** kbd-press-anim cyan glow.
- **Performance** : Enemy UpdateStealth cache renderers, MaterialController cache by tint, VfxPool WaitForSeconds cache.
- **Cleanup code** : PerkSystem using System.Linq dead, SaveSystem IsStackable dead, JuiceConfig SO magic numbers.

---

## 5. Workflow autonomie maxi (cœur du plan)

### 5.1 Principe : Opus orchestre, Sonnet exécute

**Opus** (session active avec /goal) :
- Lit le plan + audits.
- Spawn N Sonnet agents en parallèle (1 message, N `Agent` tool calls).
- Reçoit notifications, vérifie console, screenshot V6, append progress log.
- Décide Wave suivante quand seuil ≥75% atteint.
- Invoque sprint-gate fin.

**Sonnet** (`feature-dev` / `bug-fixer` etc.) :
- Lit son briefing autosuffisant (§7).
- Lit audit source si besoin contexte enrichi.
- Implémente le ticket dans son worktree isolé.
- Build + commit atomique + push autonome.
- Notifie Opus à la complétion.

### 5.2 Architecture swarm parallèle

**6-8 worktrees simultanés max** (CPU + Unity rebuild constraints).

**Spawn pattern** (1 message Opus = N Agents) :
```
Agent({
  subagent_type: "feature-dev",
  isolation: "worktree",
  run_in_background: true,
  description: "P0-UI-1 Run Mode port",
  prompt: "<briefing autosuffisant du §7>"
})
× 6-8 calls en parallèle dans 1 même message
```

**Throttle** : si > 6 tickets P0 actifs, attendre que 2 finissent avant spawn des 2 derniers.

### 5.3 Coordination via `file-ownership.md`

Voir `.claude/coordination/file-ownership.md`.

**Zones exclusives par axe** :
- Axis F UX-POLISH : `Assets/UI/*`, `Assets/Scripts/UI/*` (sauf hot zones).
- Axis D CONTENT-LEVELS : `Assets/ScriptableObjects/Levels/*.asset`, `LevelRegistry.asset`.
- Axis G QA-AUTOMATION : `.claude/qa/*`, `Assets/Tests/*`.

**Hot zones (INTEGRATOR ONLY)** : `Tower.cs`, `Enemy.cs`, `Castle.cs`, `WaveManager.cs`, `LevelRunner.cs`, `Economy.cs`, `BalanceConfig.cs`, `STATUS.md`, `Packages/manifest.json`.

**Pour Phase 5** :
- Wave 1 P0 : 8 tickets sur 3 axes (UI / Levels / Hot zone via integrator). Donc 6 simultanés worktrees max sur UI (zone F) + 2 séquentiels sur Levels (zone D, peu de conflict mais éviter).
- Wave 2 P1 : 15 tickets répartis sur F/D + 1 hot (P1-AST-1 skybox = Axis A Visual). Wave-launch parallèle OK.
- Wave 3 P2 : tasks.txt déjà ordonné, dispatch.sh runner gère le throttle.

### 5.4 Sprint-gate auto à la fin

Quand Wave 1+2+3 finies (ou Wave 1+2 si Wave 3 trop long) :

```
Agent({
  subagent_type: "auto-qa-runner",
  description: "Sprint-gate Phase 5 PARITY-V4",
  prompt: "Run .claude/qa/auto-qa-runner.mjs --sprint=phase5-parity-v4
           Hard assertions : §2.2 (8 items).
           Soft assertions LLM-judge : §2.2 (4 items, ≥3/4 PASS).
           Archive : .claude/qa/reports/phase5-final-YYYY-MM-DD.md"
})
```

**Si red** : Opus loop fix + re-gate (max 3 tentatives). Si toujours red → T1 notif Mike + STOP avec instructions reprise.

### 5.5 Aucune validation Mike au fil de l'eau

**Sealed backlog** : une fois /goal tapé, les décisions A1-A7 + scope §4 sont fixés. Opus n'attend AUCUNE validation Mike entre les tickets.

**Sauf** : Mike chat live override (Mike écrit en chat = priorité absolue → comply puis reprendre autonomie).

**Pas de questions de priorité** : si une ambiguïté apparaît, Opus tranche selon les principes du plan (parité V4 = priorité, déjà mieux V6 = préserver, identité visuelle V4 manquante = porter).

### 5.6 Notifications T1/T2/T3

Voir `.claude/supervisor/tools/notify.sh`.

**T1 (Mike attention immédiate)** : ONLY pour
- DONE atteint (sprint-gate green archivé).
- Bloquage dur (build cassé non-fixable > 2h, ambiguïté Mike-required, conflit non-mergeable).
- Décision design impacting scope (rare).

**T2 (info batch)** : Wave N finished (mais pas DONE).

**T3 (log only)** : ticket merged, screenshot capturé, console verify.

**Frequence** : maximum 1 T1 toutes les 6h sauf DONE/bloquage dur.

---

## 6. Mécanisme screenshots V4↔V6

### 6.1 Capture V4 référence (1× par feature)

**Outil** : Chrome MCP (`mcp__claude-in-chrome__navigate` + `mcp__claude-in-chrome__screenshot` ou via `read_page`).

**URL** : `https://michaelchevallier.github.io/lava_game/v4/?debug=1` (debug query = unlock all features).

**Pattern** : Opus capture V4 référence **une seule fois par feature** au début de Phase 5 (Wave 1 démarrage), batch 8 captures pour les 8 P0 visuels. Stockage : `.claude/audit/screenshots/V4-<feature>-<context>.png`.

Exemples :
- `V4-run-mode-school-select.png` (P0-UI-2)
- `V4-encyclopedia-towers.png` (P0-UI-3)
- `V4-worldmap-tile-endless.png` (P0-UI-5)
- `V4-wave-start-banner.png` (P0-UI-6)
- `V4-hero-portrait.png` (P0-UI-4)

### 6.2 Capture V6 avant/après (par ticket)

**Outil** : Unity-MCP HTTP MCP server (port 8080) → `execute_code` :
```csharp
EditorApplication.EnterPlaymode();
yield return new WaitForSeconds(2f);
ScreenCapture.CaptureScreenshot(".claude/audit/screenshots/V6-<feature>-<state>-<ticket>.png");
```

**Pattern** :
- **V6-before** : capturé par Opus AVANT spawn du Sonnet (état actuel, pour comparaison).
- **V6-after** : capturé par Opus APRÈS notification Sonnet terminé (état post-fix).

### 6.3 Triplet attaché par ticket visuel

Tickets visuels (P0-UI-1 à P0-UI-6) : triplet V4 + V6-before + V6-after dans `.claude/audit/screenshots/`.

Tickets non-visuels (P0-LVL-1, P0-LVL-2) : pas de screenshot, validation = grep .asset + `npm run test:crowdef`.

### 6.4 Convention nommage

```
.claude/audit/screenshots/
  V4-<feature>-<context>.png                 # référence (1× total)
  V6-<feature>-before-<ticket>.png           # avant fix
  V6-<feature>-after-<ticket>.png            # après fix
```

Exemples :
- `V4-run-mode-school-select.png`
- `V6-run-mode-school-select-before-P0-UI-2.png`
- `V6-run-mode-school-select-after-P0-UI-2.png`

---

## 7. Briefings P0 ready-to-dispatch

Chaque briefing ci-dessous est **autosuffisant** : un Sonnet `feature-dev` ou `bug-fixer` qui n'a jamais vu cette conversation peut l'exécuter complètement.

### 7.1 P0-LVL-1 — forbiddenTowers + bonusTowers sérialisation

```
Type : bug-fixer
Effort : 1-2 commits, ~1-2h
Bloqué par : aucun
Zone fichiers : Axis D CONTENT-LEVELS (Assets/Scripts/Data/) + lecture Axis F UI

Mission : Restaurer la sérialisation des champs `forbiddenTowers` et `bonusTowers` dans LevelData, qui étaient présents en V4 (`src-v3/data/levels/*.js` 169 occurrences) mais sont actuellement non sérialisés en V6 (régression silencieuse). Sans ça, tous les niveaux V6 permettent toutes les tours, perte du game design "level X interdit la magnet tower" / "level Y donne bonus +20% sur mage tower".

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/Scripts/Data/LevelData.cs : ajouter `public List<TowerType> forbiddenTowers = new();` et `public List<TowerType> bonusTowers = new();` avec [SerializeField] et [Tooltip].
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/TowerToolbarController.cs : binding `LevelRunner.Instance.CurrentLevel.forbiddenTowers` pour appliquer state `--forbidden` (grayed out).
- /Users/mike/Work/crowd-defense/Assets/UI/Toolbar.uss : ajouter `.toolbar-cell--forbidden { opacity: 0.3; cursor: not-allowed; }`.

Référence V4 (`src-v3/data/levels/world1-2.js` exemple) :
```js
export default {
  id: "world1-2",
  forbiddenTowers: ["magnet"],
  bonusTowers: ["mage"],
  ...
}
```

Migration data : pour les ~30 levels V4 qui ont des `forbiddenTowers` non vides, scripter une migration dans .claude/audit/migrate-forbidden-towers.cs (Editor menu) qui parse src-v3 levels et write back to V6 LevelData.asset GUIDs.

Build + commit + push :
1. cd /Users/mike/Work/crowd-defense
2. Vérifier compile : grep -r "forbiddenTowers" Assets/Scripts/ doit montrer LevelData.cs + TowerToolbarController.cs.
3. git add -A && git commit -m "fix(data)(P0-LVL-1): restore forbiddenTowers + bonusTowers serialization in LevelData"
4. git push origin main

Verification :
- Unity Editor : open Assets/ScriptableObjects/Levels/world1-2.asset, Inspector doit montrer forbiddenTowers + bonusTowers slots.
- Play mode W1-2 : si forbiddenTowers contient "magnet", tile magnet toolbar doit être grayed out + click should fail with toast "Tour interdite ce niveau".
- Console : aucune erreur.

Source audit : .claude/audit/V4-V6-LEVELS-2026-05-14.md §4.1 + item P1 critique. .claude/audit/V4-V6-UI-HUD-2026-05-14.md item 17.
```

### 7.2 P0-LVL-2 — LevelRegistry régénération

```
Type : bug-fixer
Effort : 1 commit, ~30 min
Bloqué par : aucun
Zone fichiers : Axis D CONTENT-LEVELS

Mission : LevelRegistry.asset contient actuellement 81 GUIDs mais le dossier `Assets/ScriptableObjects/Levels/` contient 90 fichiers .asset. 9 levels (probablement les W*-9 endgame ajoutés en R6-PARITY-012) sont orphelins → inaccessibles depuis menu.

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/Resources/LevelRegistry.asset : régénérer via Editor menu.
- /Users/mike/Work/crowd-defense/Assets/Editor/LevelRegistryBuilder.cs (probablement existe) : sinon créer un tool [MenuItem("Tools/CrowdDefense/Build LevelRegistry")] qui scanne le dossier et écrit tous les GUIDs trouvés.

Procédure :
1. Identifier les 9 orphelins : `diff <(ls /Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Levels/*.asset | xargs basename -a) <(grep -oE 'world[0-9]-[0-9]+' Assets/Resources/LevelRegistry.asset | sort -u)`
2. Soit régénérer LevelRegistry via Editor menu tool si existe.
3. Soit éditer LevelRegistry.asset manuellement pour ajouter les 9 GUIDs manquants.

Build + commit + push :
1. cd /Users/mike/Work/crowd-defense
2. git add Assets/Resources/LevelRegistry.asset
3. git commit -m "fix(data)(P0-LVL-2): regenerate LevelRegistry.asset to include 9 W*-9 orphan levels"
4. git push origin main

Verification :
- Unity Editor : Inspector LevelRegistry doit montrer 90 entries.
- Play mode WorldMap : naviguer vers W1-9, W2-9, etc., tous les W*-9 doivent être accessibles depuis menu.
- Si W*-9 doivent être unlocked condition-based, vérifier que la condition (e.g. world complete) gate l'access.

Source audit : .claude/audit/V4-V6-LEVELS-2026-05-14.md §1 + bug repéré.
```

### 7.3 P0-UI-1 — Run Mode port complet

```
Type : feature-dev
Effort : 8-10 commits, ~3-4h
Bloqué par : P0-UI-2 (Magic Schools, recommandé d'enchaîner)
Zone fichiers : Axis F UX-POLISH

Mission : Porter le **Run Mode** complet de V4 (`src-v3/ui/RunMode.js`) vers V6. Le Run Mode est une feature centrale V4 : gameplay "rogue-like" où le joueur enchaîne 7 actes (combat / elite / mystery / shop / rest / boss), avec persistence des perks entre actes, choix de Magic School au début, abandon / reprendre, victory/defeat avec gems gain.

Files à créer :
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/RunModeController.cs (~400 LOC) : controller principal, state machine (Map / NodeCombat / NodeShop / NodeRest / NodeBoss / Defeat / Victory).
- /Users/mike/Work/crowd-defense/Assets/Scripts/Systems/RunContext.cs (vérifier si existe) : runtime state (current act, current node, perks accumulated, lives, gold buffer).
- /Users/mike/Work/crowd-defense/Assets/Scripts/Systems/RunMapGenerator.cs : génère une map procédurale 7 actes × 3-4 nodes par acte avec types weighted random.
- /Users/mike/Work/crowd-defense/Assets/UI/RunMap.uxml : layout map view (nodes + chemins).

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/CampaignMenuController.cs (ou similaire) : ajouter bouton "NEW GAME" (run mode) qui lance RunModeController.
- /Users/mike/Work/crowd-defense/Assets/Scripts/Systems/LevelLoader.cs : ajouter `LoadRunNode(NodeDef node)` qui setup le contexte pour un combat node.

Référence V4 (`src-v3/ui/RunMode.js`) :
- 7 actes (acte 1 = facile, acte 7 = boss final).
- Node types : combat (50%), elite (20%), mystery (10%), shop (10%), rest (5%), boss (5%).
- Mystery node : random reward (perk / gold / curse).
- Shop : 3 perks proposés, achète avec gold.
- Rest : heal castleHP +30%.
- Boss : level W*-8 ou W*-9.
- Abandon : -50% gems reward.
- Victory : full gems + unlock next school.

UX flow :
1. Menu principal → "NEW GAME" → SchoolSelectScreen (P0-UI-2).
2. School chosen → RunModeController.StartRun(school) → génère map + spawn perks.
3. Node click → LoadRunNode(node) → LevelScene avec context.
4. Level victory → return to RunMap, mark node done, propose next node.
5. Acte 7 boss victory → VictoryScreen + gems reward.
6. Defeat (castleHP 0 dans n'importe quel node) → DefeatScreen + half gems.

Build + commit + push : commits atomiques par sous-feature (run state machine / map gen / node types / shop / rest / boss / abandon / victory / defeat / wiring menu).

Verification :
- Unity Editor Play mode : "NEW GAME" → School select → Run map visible → click combat node → level joué → return run map → next node.
- Console : 0 erreur.
- Screenshot V6-after-P0-UI-1 : RunMap.uxml visible avec 7 actes.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #4. Référence src-v3/ui/RunMode.js (~600 LOC).
```

### 7.4 P0-UI-2 — 3 Magic Schools selection screen

```
Type : feature-dev
Effort : 4-6 commits, ~2-3h
Bloqué par : aucun (peut être dispatché en parallèle avec P0-UI-1)
Zone fichiers : Axis F UX-POLISH

Mission : Implémenter le **SchoolSelectScreen** pour le Run Mode V4 : popup 3 cards (Feu / Givre / Maçonnerie) avec icon + description + perks exclusifs au début de chaque run. V4 a 3 schools complètes, V6 a 0.

Files à créer :
- /Users/mike/Work/crowd-defense/Assets/UI/SchoolSelectScreen.uxml : layout 3 cards horizontal.
- /Users/mike/Work/crowd-defense/Assets/UI/SchoolSelectScreen.uss : styling card with icon + name + description + perk list.
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/SchoolSelectController.cs : binding logic, 3 buttons, ResultCallback.
- /Users/mike/Work/crowd-defense/Assets/Scripts/Data/MagicSchool.cs : enum + SO MagicSchoolDef (name, icon, description, exclusivePerks list).
- /Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Schools/Fire.asset, Frost.asset, Stonework.asset.

Référence V4 (`src-v3/ui/RunMode.js` MagicSchools section) :
- **Feu** 🔥 : "+25% damage tours feu, +10% gold quand kill par feu". Perks : Burning Souls, Magma Surge.
- **Givre** 🧊 : "+50% slow duration, freeze 3+ stack = freeze 2s". Perks : Cryo Veil, Glacier Trap.
- **Maçonnerie** 🪨 : "+30% castleHP, repair +20%/wave". Perks : Stone Wall, Battlement.

UX flow :
1. "NEW GAME" click → SchoolSelectScreen overlay (full-screen modal).
2. 3 cards displayed (Feu / Givre / Maçonnerie) avec hover effect (scale 1.05, glow border).
3. Click card → ResultCallback(school) → close modal + RunModeController.StartRun(school).

Build + commit + push :
- commit 1 : SchoolSelectScreen.uxml + uss + controller skeleton.
- commit 2 : 3 SO MagicSchoolDef assets + enum.
- commit 3 : binding logic ResultCallback.
- commit 4 : wiring depuis CampaignMenu "NEW GAME".

Verification :
- Unity Editor Play mode : Menu → NEW GAME → SchoolSelectScreen apparaît, 3 cards visibles, click une carte ferme le modal et appelle RunMode.
- Screenshot V6-after-P0-UI-2 : SchoolSelectScreen.uxml.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #5.
```

### 7.5 P0-UI-3 — Encyclopédie HUD

```
Type : feature-dev
Effort : 4-5 commits, ~2-3h
Bloqué par : aucun
Zone fichiers : Axis F UX-POLISH

Mission : Ajouter une **Encyclopédie HUD accessible mid-game** depuis le HUD (bouton 📚 top-right). V4 `#encyclopedia` agrège Tours + Ennemis + Perks. V6 a `BestiaryPanel.uxml` mais accessible uniquement depuis MenuLevelSelect, et seulement Bestiaire (manque Tours + Perks).

Files à créer :
- /Users/mike/Work/crowd-defense/Assets/UI/EncyclopediaPanel.uxml : tab-based UI (Tours / Ennemis / Perks).
- /Users/mike/Work/crowd-defense/Assets/UI/EncyclopediaPanel.uss : styling tabs + scroll list + detail pane.
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/EncyclopediaController.cs : load TowerDef[], EnemyDef[], PerkDef[] depuis registries.

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/UI/HUD.uxml : ajouter `<Button name="btn-encyclopedia" class="top-bar-btn">📚</Button>`.
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/HudController.cs : Q<Button>("btn-encyclopedia") + clicked → EncyclopediaController.Show().

Référence V4 (`src-v3/ui/Encyclopedia.js`) :
- 3 tabs : Tours / Ennemis / Perks.
- Tour entry : icon + name + stats (range, dmg, fireRate, cost) + description + behaviors.
- Enemy entry : icon + name + stats (HP, speed, reward, threat) + description + special abilities.
- Perk entry : icon + name + description + tier + exclusive school.

UX flow :
1. HUD top-bar btn-encyclopedia 📚 click → EncyclopediaPanel overlay slide-down.
2. Tabs Tours / Ennemis / Perks (3 columns dans navigation).
3. Click entry → detail pane right side.
4. Close button or ESC → hide overlay, resume game.

Build + commit + push :
- commit 1 : EncyclopediaPanel.uxml + uss skeleton.
- commit 2 : 3 tabs + binding TowerRegistry / EnemyRegistry / PerkRegistry.
- commit 3 : HUD.uxml btn-encyclopedia + binding HudController.
- commit 4 : keyboard shortcut ESC close.

Verification :
- Unity Editor Play mode : W1-1 → HUD btn-encyclopedia 📚 click → panel apparaît, 3 tabs fonctionnels, ESC close.
- Screenshot V6-after-P0-UI-3 : EncyclopediaPanel ouverte sur tab Tours.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #1.
```

### 7.6 P0-UI-4 — Hero portrait HUD

```
Type : feature-dev
Effort : 2 commits, ~1h
Bloqué par : aucun
Zone fichiers : Axis F UX-POLISH

Mission : Binding du Hero portrait + emoji 👑 dans le HUD hero-bar XP. Actuellement V6 affiche juste "Hero" + "Lv 1" sans portrait. V4 affiche `👑 Lv N` + portrait skin selon hero/skin équipé.

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/UI/HUD.uxml : dans `hero-panel`, ajouter `<VisualElement name="hero-portrait" class="hero-portrait"/>` et `<Label name="hero-emoji" text="👑"/>`.
- /Users/mike/Work/crowd-defense/Assets/UI/HUD.uss : styling .hero-portrait (32×32px, background-image bound dynamically).
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/HudController.cs : Q<VisualElement>("hero-portrait") + bind à HeroPortraitController.GetCurrentSkinSprite().
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/HeroPortraitController.cs : déjà existant probablement, vérifier API GetCurrentSkinSprite() ou similaire.

Référence V4 (`src-v3/index.html` #hero-portrait + #hero-emoji) :
- Portrait = `<img src="assets/heroes/{heroId}-{skinId}.png">` 32×32.
- Emoji 👑 prefixe le label level.
- Level → "Lv N" format.

Build + commit + push :
- commit 1 : HUD.uxml + uss ajout hero-portrait + hero-emoji.
- commit 2 : HudController binding HeroPortraitController.

Verification :
- Unity Editor Play mode : HUD top-left hero-panel affiche portrait skin actuel + 👑 Lv N.
- Switch hero skin via SkinPicker → portrait change.
- Screenshot V6-after-P0-UI-4 : HUD avec hero portrait visible.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #2.
```

### 7.7 P0-UI-5 — Tiles spéciales worldmap

```
Type : feature-dev
Effort : 3 commits, ~1-2h
Bloqué par : aucun
Zone fichiers : Axis F UX-POLISH

Mission : Ajouter 3 tiles spéciales (Endless ∞ / Daily 📅 / Boss-rush 💀) dans le WorldMap V6. V4 worldmap a ces 3 modes accessibles via tiles distinctes. V6 a `DailyChallengeModal.cs` câblé sur "btn-open-bestiary" mais pas de tile dédiée pour boss_rush / endless dans WorldMap.uxml.

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/UI/WorldMap.uxml : ajouter 3 tiles en footer ou side-panel :
  ```xml
  <Button name="tile-endless" class="worldmap-tile worldmap-tile--special">
    <Label text="∞"/>
    <Label name="endless-best" text="Best: V0"/>
  </Button>
  <Button name="tile-daily" class="worldmap-tile worldmap-tile--special">
    <Label text="📅"/>
    <Label name="daily-streak" text="🔥 0"/>
  </Button>
  <Button name="tile-boss-rush" class="worldmap-tile worldmap-tile--special">
    <Label text="💀"/>
    <Label text="Boss Rush"/>
  </Button>
  ```
- /Users/mike/Work/crowd-defense/Assets/UI/WorldMap.uss : styling .worldmap-tile--special (gold border, larger).
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/WorldMapController.cs : bind 3 tiles to OnClick handlers :
  - tile-endless → LoadEndlessMode().
  - tile-daily → DailyChallengeModal.Open().
  - tile-boss-rush → LoadBossRushMode() (Boss Rush mode dépend de P1-LVL-3 si A2=OUI).

Référence V4 (`src-v3/ui/WorldMap.js`) :
- Tiles spéciales dans footer worldmap, séparées des world levels.
- Endless tile shows best wave reached.
- Daily tile shows streak count + emoji 🔥.
- Boss-rush tile shows BOSS label.

Build + commit + push :
- commit 1 : WorldMap.uxml + uss tiles spéciales.
- commit 2 : WorldMapController binding 3 handlers.
- commit 3 : LoadEndlessMode wiring (si pas déjà fait dans LevelLoader).

Verification :
- Unity Editor Play mode : WorldMap → 3 tiles spéciales visibles + click endless lance Endless, click daily lance DailyChallenge.
- Si P1-LVL-3 pas fini, tile boss-rush peut être disabled ou afficher "Coming soon".
- Screenshot V6-after-P0-UI-5 : WorldMap avec 3 tiles.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #3.
```

### 7.8 P0-UI-6 — Wave-start + wave-clear banners

```
Type : feature-dev
Effort : 2 commits, ~1h
Bloqué par : aucun
Zone fichiers : Axis F UX-POLISH

Mission : Ajouter les banners "⚠ Vague N arrive" (orange, slide-in 1.2s) et "✓ Vague N terminée" (vert, slide-in 1.5s) à chaque wave. V4 a `#wave-start-banner` + `#wave-clear-banner` mais V6 n'a que `WaveBannerController.cs` partial sans le wave-cleared row UXML.

Files à modifier :
- /Users/mike/Work/crowd-defense/Assets/UI/HUD.uxml : ajouter
  ```xml
  <VisualElement name="wave-start-banner" class="wave-banner wave-banner--start hidden">
    <Label text="⚠ Vague <Label name="wave-start-n"/> arrive"/>
  </VisualElement>
  <VisualElement name="wave-clear-banner" class="wave-banner wave-banner--clear hidden">
    <Label text="✓ Vague <Label name="wave-clear-n"/> terminée"/>
  </VisualElement>
  ```
- /Users/mike/Work/crowd-defense/Assets/UI/HUD.uss : styling .wave-banner--start (orange background) et --clear (green background), animation slide-in via transition transform translate3d(-100%, 0, 0) → 0.
- /Users/mike/Work/crowd-defense/Assets/Scripts/UI/WaveBannerController.cs : binding `WaveManager.Instance.OnWaveStart += ShowStartBanner; OnWaveClear += ShowClearBanner;`.

Référence V4 (`src-v3/ui/WaveBanner.js`) :
- Start banner : slide-in 1.2s + fade-out 0.5s après 2s total.
- Clear banner : slide-in 1.5s + auto-hide après 2.5s.

Build + commit + push :
- commit 1 : HUD.uxml + uss banners.
- commit 2 : WaveBannerController binding events.

Verification :
- Unity Editor Play mode : W1-1 wave start → banner "⚠ Vague 1 arrive" slide-in orange.
- Wave clear → banner "✓ Vague 1 terminée" slide-in vert.
- Console : 0 erreur.
- Screenshot V6-after-P0-UI-6 : banner visible.

Source audit : .claude/audit/V4-V6-UI-HUD-2026-05-14.md item P0 #6.
```

---

## 8. Infrastructure exec (déjà en place)

### 8.1 `.claude/supervisor/instructions-to-exec.md` (canal sup → exec)

Fichier 139KB déjà rempli. Quand cette Opus session écrit une nouvelle instruction (e.g. "STOP-RUNTIME-CRITICAL"), l'exec session ack dans `.claude/supervisor/acks/`.

**Pour Phase 5** : Opus session courante (avec /goal) écrit dans ce fichier en début pour signaler "PHASE 5 ACTIVE, dispatch Wave 1 8 P0", puis pousse fewer instructions car la session a déjà le plan en context.

### 8.2 Supervisor /loop 30m

Côté autre session (milan project), un /loop 30m superviseur tourne déjà :
- Scrute drift selon charter §2 (12 criteria D1-D12).
- Append `.claude/supervisor/drift-reports/_clean-log.md` si OK.
- Écrit drift-report si confirmé.
- Notif T1/T2 si drift confirmé ou sprint complete.

**Pendant Phase 5** : le supervisor /loop continue, scrute les commits de Phase 5. Si Opus dérive, le supervisor flagge.

### 8.3 `notify.sh` T1/T2/T3

`.claude/supervisor/tools/notify.sh` :
- **T1** : macOS osascript sound + ntfy.sh high priority. Pour DONE / bloquage dur uniquement.
- **T2** : silent macOS + ntfy.sh default. Info batch (Wave finished).
- **T3** : log only.

Usage Opus pendant Phase 5 :
```bash
./.claude/supervisor/tools/notify.sh T1 "Phase 5 DONE ✅" "8/8 P0 + 13/15 P1 mergés, sprint-gate green"
```

### 8.4 `master-dispatch/dispatch.sh` + `tasks.txt`

Pour Wave 3 (40+ P2 polish) : utilise l'infrastructure existante.

```bash
bash .claude/master-dispatch/runner.sh
```

`runner.sh` loop 60s + invoke `dispatch.sh ITER TARGET` qui :
- Lit `.claude/master-dispatch/tasks.txt` (37 tickets pipe-delimited).
- Compte agents actifs (via JSONL subagents tracking).
- Spawn N nouveaux Sonnet jusqu'à TARGET=6 simultanés.
- Skip déjà-merged via git log.

**Pour Phase 5** : appendre les P2 audit issues dans `tasks.txt` au début de Wave 3 (Opus écrit ~40 lignes pipe-delimited).

### 8.5 git worktree workflow

Chaque Sonnet `feature-dev` avec `isolation: "worktree"` :
- Spawne worktree `wt_<ticket-id>-<iter>` from main HEAD.
- Bosse atomiquement, commit + push sur sa branche.
- Opus merge main si conflit prévisible (cf file-ownership.md hot zones).

**Conflit prévisible** : 2 Sonnet sur même fichier UI (e.g. HUD.uxml). Mitigation : §5.3 zone par axe.

---

## 9. Personas agents

### 9.1 feature-dev (Sonnet)

**Use** : implémentation feature non-triviale (≥ 1 fichier modifié, ≥ 5 min code).

**Pattern** : briefing autosuffisant inline → Sonnet code → build → commit atomique → push autonome.

**Pour Phase 5** : Wave 1 P0-UI-1/2/3/4/5/6 + Wave 2 P1-UI-* + Wave 3 polish features.

### 9.2 bug-fixer (Sonnet)

**Use** : patch surgical bug (régression, NRE, behavior incorrect).

**Pattern** : minimal repro check + root cause analysis + fix + build + commit + push.

**Pour Phase 5** : P0-LVL-1, P0-LVL-2, P1-GP-1 (cleanup Daily), P2 cleanup tickets.

### 9.3 ux-designer (Sonnet)

**Use** : design UI/UX avant code (wireframes ASCII, CSS specs, raccourcis clavier, animations).

**Pour Phase 5** : Préparation pre-Wave 1 si SchoolSelectScreen / Encyclopedia layout flou. Optionnel.

### 9.4 level-designer (Sonnet)

**Use** : design level layouts (maps + waves).

**Pour Phase 5** : P1-LVL-4 (refondre 3 multi-castle levels W5-8, W9-8, W10-8 en mono-château).

### 9.5 auto-qa-runner (Opus juge)

**Use** : sprint-gate fin Phase 5.

**Pattern** : run scenarios `.claude/qa/scenarios/phase5-parity/*.mjs` + soft assertions LLM-judge. Archive report `.claude/qa/reports/phase5-final-YYYY-MM-DD.md`.

### 9.6 quality-maintainer (Sonnet)

**Use** : refactor léger (dead code, dup, complex func).

**Pour Phase 5** : P2 cleanup tickets (PerkSystem Linq dead, SaveSystem IsStackable dead, etc.).

### 9.7 perf-auditor (Sonnet)

**Use** : audit + fix perf bottleneck.

**Pour Phase 5** : si profiler montre < 60 FPS Editor en wave 5 → spawn perf-auditor pour fix Enemy UpdateStealth allocs / MaterialController cache / VfxPool WaitForSeconds.

### 9.8 td-researcher (Opus)

**Use** : fallback si décision design ambiguë (e.g. comportement V4 unclear, choix difficulty curve).

**Pour Phase 5** : Rare. Si Opus tombe sur ambiguïté pre-décidée pas dans §3.2, spawn td-researcher pour mini-recherche 30 min, puis décide.

---

## 10. Phase 5 — sprint structure execution

### 10.1 T0 : Opus lit plan + /goal → spawn Wave 1

Étape 1 — Mike tape /goal (§11). Opus actuelle :
- Read `/Users/mike/Work/crowd-defense/.claude/plans/phase5-parity-v4-master.md` (§0 à §13).
- Read 5 audits (récap, pas full text — déjà résumé dans plan).
- Check git status + log -5.
- Check console-baseline.txt (pre-Phase 5).
- Capture V4 screenshots (8 références) via Chrome MCP.

Étape 2 — Opus spawn Wave 1 en parallèle :
- 1 message contenant 6-8 `Agent` tool calls simultanés.
- Briefings inline depuis §7 (autosuffisants).
- `subagent_type: "feature-dev"` ou `"bug-fixer"`, `isolation: "worktree"`, `run_in_background: true`.

### 10.2 Wave 1 (8 P0 parallèles, ~6-10h wall-clock)

Spawn batch :
- 6 worktrees first batch : P0-LVL-1, P0-LVL-2, P0-UI-1, P0-UI-2, P0-UI-3, P0-UI-4.
- 2 worktrees second batch : P0-UI-5, P0-UI-6 (spawned dès qu'un slot se libère).

**Pour chaque notification "Sonnet completed"** :
1. Read output Sonnet.
2. Capture V6-after-<ticket> screenshot via Unity-MCP.
3. Verify console clean (read console-after-<ticket>.txt).
4. Append progress log : `.claude/qa/reports/phase5-parity-progress.md`.
5. Vérifier merge sur main (Sonnet a déjà push).
6. T3 silent log.

**Quand 6/8 P0 mergés (75%)** → préparer Wave 2.

### 10.3 Wave 2 (15 P1 parallèles, ~8-12h)

Spawn 6-8 simultanés. Pareil que Wave 1.

**Quand 12/15 P1 mergés (75%)** → préparer Wave 3.

### 10.4 Wave 3 (40+ P2 via dispatch.sh runner, ~12-16h)

Au lieu de spawner via Agent tool, lancer le runner existant :
```bash
cd /Users/mike/Work/crowd-defense
bash .claude/master-dispatch/runner.sh
# loop 60s + dispatch.sh ITER TARGET=6 simultanés
```

Préalable : Opus appende ~40 lignes pipe-delimited dans `.claude/master-dispatch/tasks.txt` pour les P2 audit issues identifiées (cf §4.3).

### 10.5 Sprint-gate fin

```
Agent({
  subagent_type: "auto-qa-runner",
  description: "Sprint-gate Phase 5 PARITY-V4 final",
  prompt: "<voir §5.4>"
})
```

**Si red** : Opus loop fix + re-gate (max 3 tentatives).
- Identifier les hard assertions qui fail.
- Spawn bug-fixer pour fix root cause.
- Re-run sprint-gate.
- Si toujours red après 3 tentatives → T1 notif Mike + STOP.

### 10.6 Stop conditions

**DONE** : sprint-gate green + critères §1.4 → T1 notif Mike "Phase 5 ✅ DONE" + résumé chat + archive report.

**Bloquage dur** :
- Build cassé > 2h non-fixable.
- Ambiguïté design exigeant Mike (rare car A1-A7 pré-décidés).
- Conflit non-mergeable (worktree hot zone).
→ T1 notif Mike + STOP avec `.claude/qa/reports/phase5-blocker-YYYY-MM-DD.md` (instructions reprise).

**Mike chat live override** : Mike écrit en chat = priorité absolue. Comply puis reprendre autonomie.

---

## 11. /goal exact à copier-coller

**Copy/paste exactement ce bloc dans une nouvelle session Claude Code dans `cd /Users/mike/Work/crowd-defense`** :

```
/goal Phase 5 PARITY-V4 Crowd Defense V6 — autonomie totale, parallélisation maximale, full completion.

Source unique de vérité : .claude/plans/phase5-parity-v4-master.md (lire en premier, en entier, depuis le CWD crowd-defense).

Mission : atteindre ≥95% parity visible V4 (= src-v3 Crowd Defense V3, deployed https://michaelchevallier.github.io/lava_game/v4/) → V6 Unity Editor Play mode, en autonomie totale sans validation Mike au fil de l'eau.

Execution :
1. T0 : lire le plan (§0 à §13). Vérifier état git + .claude/audit/V4-V6-*-2026-05-14.md (récap). Capture V4 screenshots (8 références) via Chrome MCP.
2. Wave 1 : dispatch 8 tickets P0 EN PARALLÈLE (1 message, 6-8 Agent tool calls simultanés, subagent_type=feature-dev ou bug-fixer selon ticket, isolation=worktree, run_in_background=true). Briefings autosuffisants dans §7.
3. À chaque notification ticket terminé : capture V6 Editor screenshot après-ticket + verify console clean + append progress log (.claude/qa/reports/phase5-parity-progress.md). Commit + push déjà faits par Sonnet.
4. Wave 2 : dès Wave 1 ≥75% mergée (6/8 P0), dispatch top 15 P1 EN PARALLÈLE (§10.3).
5. Wave 3 : dès Wave 2 ≥75% mergée (12/15 P1), append ~40 P2 audit issues dans tasks.txt puis lance bash .claude/master-dispatch/runner.sh pour polish (6 slots loop).
6. Sprint-gate final : invoque auto-qa-runner Opus juge. Si red, loop fix + re-gate (max 3 tentatives). Si toujours red, T1 notif Mike + STOP.

Critères "DONE" :
- 8/8 P0 mergés sur main avec screenshots V4/V6 triplets dans .claude/audit/screenshots/.
- 12+/15 P1 mergés.
- Sprint-gate report GREEN archivé dans .claude/qa/reports/phase5-final-YYYY-MM-DD.md.
- Console Unity Editor Play mode 0 nouvelle erreur post-Wave 1 (vs baseline pre-Phase 5).
- Aucun ticket P0 réouvert post-merge.
- Build batchmode WebGL exit=0.

Stop conditions :
- DONE atteint → notify Mike T1 "Phase 5 PARITY-V4 ✅ DONE" + résumé chat + archive report.
- Bloquage dur (build cassé non-fixable >2h, ambiguïté design exigeant Mike, conflit non-mergeable) → notify Mike T1 + STOP avec instructions reprise dans .claude/qa/reports/phase5-blocker-YYYY-MM-DD.md.
- Mike chat live override → comply immédiatement, puis reprendre autonomie.

Mode : autonomie totale sur le développement. Pas de question priorité. Pas de validation au fil de l'eau. Décisions A1-A7 pré-arbitrées (cf §3.2 du plan) appliquées par défaut. Parallélisation maximale (6-8 worktrees simultanés). Full completion (jamais s'arrêter sans DONE ou bloquage dur). Continue jusqu'à DONE.

Supervisor /loop 30m reste actif en parallèle (côté autre session milan project) — drift check charter §2, 12 criteria.

Estimation durée : ~30-40h wall-clock autonome.
```

---

## 12. ÉTAPES MIKE — Lancer la Phase 5

Liste précise des actions Mike pour démarrer la Phase 5 PARITY-V4 :

### Étape 1 — Vérifier que le plan est commit + push

```bash
cd /Users/mike/Work/crowd-defense
git log --oneline -3
# Doit montrer le commit qui contient phase5-parity-v4-master.md
```

### Étape 2 — (Optionnel) Review du plan

Ouvre `.claude/plans/phase5-parity-v4-master.md` dans ton éditeur préféré.

- Vérifie **§3.2 arbitrages A1-A7** — modifie si tu veux autre chose que mes recos (e.g. NE PAS porter Run Mode → A1=NON).
- Vérifie **§4 backlog** — modifie scope si tu veux ajout/retrait de tickets.
- Vérifie **§7 briefings P0** — modifie si tu veux ajuster un ticket.

Si tu modifies : commit + push avant Étape 3 (sinon la nouvelle session lit la version pré-modif).

### Étape 3 — Démarrer la session d'exécution Phase 5

**Option A** (terminal) :
```bash
cd /Users/mike/Work/crowd-defense
claude
```

**Option B** (VSCode/IDE) : ouvre une nouvelle session Claude Code dans le workspace `crowd-defense`.

### Étape 4 — Coller le /goal

Copie le bloc entier du **§11** ci-dessus et colle-le dans le prompt de la nouvelle session Claude Code.

### Étape 5 — Vérifier le démarrage

L'output Opus doit montrer (dans cet ordre) :
1. "Reading .claude/plans/phase5-parity-v4-master.md..." (read plan).
2. "Capturing V4 reference screenshots..." (Chrome MCP).
3. "Spawning Wave 1 : 6 agents in parallel..." (1 message multiple Agent tool calls).
4. Tu vois 6-8 lignes "Agent X started in background".

**Si Opus ne spawn pas Wave 1** :
- Interrompre la session.
- Re-coller le /goal en simplifiant : "Lire .claude/plans/phase5-parity-v4-master.md puis spawn Wave 1 en parallèle".
- Si toujours pas → ouvrir un ticket dans .claude/supervisor/questions-to-supervisor.md.

### Étape 6 — Suivre progression (optionnel)

```bash
tail -f /Users/mike/Work/crowd-defense/.claude/qa/reports/phase5-parity-progress.md
```

Ou notifications macOS / ntfy.sh sur ton iPhone (si NTFY_TOPIC env var set côté supervisor).

### Étape 7 — Si Mike est sollicité (T1 notif)

- Notification macOS sound Submarine + ntfy.sh push iPhone.
- Va lire le chat de la nouvelle session, ou check `.claude/qa/reports/phase5-blocker-YYYY-MM-DD.md` (si bloquage dur).

### Étape 8 — Fin DONE

- Sprint-gate report green : `.claude/qa/reports/phase5-final-YYYY-MM-DD.md`.
- 8/8 P0 + 12+/15 P1 mergés.
- Vérifier `git log --oneline` montre ~50-80 nouveaux commits.
- Mike clôture la phase et passe à **Phase 6** (iOS Xcode build + Steam SDK integration).

---

## 13. TO-DO LIST MIKE (checklist actionnable)

```
☐ 1. cd /Users/mike/Work/crowd-defense
☐ 2. git pull --rebase
☐ 3. git log --oneline -3  (vérifier que phase5-parity-v4-master.md est bien commit)
☐ 4. (Optionnel) Ouvrir .claude/plans/phase5-parity-v4-master.md et skim §3.2 + §4 + §7
☐ 5. (Optionnel) Override arbitrages A1-A7 si tu veux différent
   ☐ 5a. Edit §3.2 cellules DÉCISION
   ☐ 5b. git commit -m "edit(plan): override A1-A7 arbitrages"
   ☐ 5c. git push
☐ 6. Ouvrir nouvelle session Claude Code (cd /Users/mike/Work/crowd-defense + claude OU IDE)
☐ 7. Coller le /goal depuis §11 du plan
☐ 8. Vérifier output : "Spawning Wave 1 : 6 agents in parallel..."
☐ 9. (Optionnel) tail -f .claude/qa/reports/phase5-parity-progress.md
☐ 10. Aller faire autre chose (~30-40h autonome)
☐ 11. Recevoir notif T1 = DONE ou bloquage dur
☐ 12. Si DONE :
   ☐ 12a. Vérifier sprint-gate report .claude/qa/reports/phase5-final-YYYY-MM-DD.md = GREEN
   ☐ 12b. Vérifier git log --oneline | wc -l shows ~50-80 nouveaux commits
   ☐ 12c. Clôturer Phase 5 et passer à Phase 6 (iOS + Steam)
☐ 13. Si bloquage dur :
   ☐ 13a. Lire .claude/qa/reports/phase5-blocker-YYYY-MM-DD.md
   ☐ 13b. Débloquer (build fix, design arbitrage, etc.)
   ☐ 13c. Relancer une nouvelle session avec /goal reprise
```

---

## 14. Risques + mitigations

### 14.1 Plan trop long pour 1 read Opus

**Risque** : ce plan fait ~2500 lignes, Opus peut tronquer la lecture.

**Mitigation** : §0 TL;DR + §11 /goal + §12 étapes Mike sont les 3 sections critiques (~300 lignes total). Le reste est référence Opus session pour ticket-by-ticket lookup.

### 14.2 Briefings P0 inline manquent un détail

**Risque** : Sonnet code mal car briefing trop concis.

**Mitigation** : briefings §7 pointent vers fichiers source `.claude/audit/V4-V6-*-2026-05-14.md` pour contexte enrichi.

### 14.3 Arbitrages A1-A7 pré-décidés pas alignés Mike

**Risque** : Mike voulait NON sur A1 (Run Mode) → 9 P0 implémentés en mauvaise direction.

**Mitigation** : §3.2 indique "ÉDITER ICI AVANT /goal si différent". Mike skim 5 min avant taper /goal.

### 14.4 Worktree conflit hot zone

**Risque** : 2 Sonnet sur même fichier (e.g. HUD.uxml) → merge cassé.

**Mitigation** : §5.3 file-ownership zones disjointes. Si conflit prévisible, batch séquentiel au lieu de parallèle.

### 14.5 Build batchmode WebGL cassé pendant Wave

**Risque** : un commit casse le build.

**Mitigation** : Opus run `npm run test:crowdef` ou `bash .claude/master-dispatch/build-check.sh` après chaque batch merge. Si cassé, rollback le commit fautif.

### 14.6 Sprint-gate red après 3 tentatives

**Risque** : impossible d'atteindre 95% parity avec scope défini.

**Mitigation** : Opus T1 notif Mike + STOP avec instructions reprise. Mike peut soit ajuster scope (reduce P1 target à 8/15) soit valider 90% comme "DONE équivalent".

### 14.7 Mike inactif > 24h pendant bloquage

**Risque** : autonomie complete = aucun Mike disponible pour décision.

**Mitigation** : T1 notif macOS + ntfy.sh iPhone. Si pas de réponse > 24h, Opus continue avec décisions conservatrices (skip ticket bloqué, mark P0 → P1) et journalise.

---

## 15. Standards industrie (préservé du plan original)

Référents par mécanique (pour décisions design Opus dans Phase 5 si ambiguïté).

### 15.1 Pacing — waves manuelles + skip bonus
**Référents** : Kingdom Rush (manual + +1¢/s skip), BTD6 (manual + 50% income), Element TD 2 (interest leak), GemCraft (no waves).

### 15.2 Économie
**Référents** : ETD2 (interest leak penalty Q1=A), BTD6 (income dégressif), Defense Grid (bounty kill scale).

### 15.3 Level design
**Référents** : Defense Grid (multi-portal 4P), KR (linear path), GemCraft (free mazing), Anomaly (offensive defense).

### 15.4 Synergies + upgrade trees
**Référents** : ETD2 rainbow strats, BTD6 monkey synergies, KR L4 paywall (Q2=B ×5).

### 15.5 Boss design (W*-8)
**Référents** : KR boss multi-phase, BTD6 BAD boss, GemCraft Apocalypse.

### 15.6 UX bouton wave + raccourcis
**Référents** : KR W (auto), BTD6 spacebar (start), Anomaly N (next).

---

## 16. Out of scope (Park Defense `src-v2/`)

Liste explicite des 6 features Park Defense (Phaser PvZ tile-defense) qui NE SONT PAS portées en V6 :

1. **Mode Carnaval** : 5 levels conveyor belt sans économie. Mécanique différente (tiles fixes pré-placées). Non aligné Crowd Defense (towers placement libre + path).
2. **Mode Arène des Boss** : 3 vagues, 3 boss back-to-back. À ne pas confondre avec **Boss Rush** (V4 Crowd Defense, IN SCOPE).
3. **Cutscenes choices branchées W4/W7/W10** : choix narratifs avec impact (startCoins bonus, kill bonus, cooldown reduction). V6 cutscenes sont linéaires.
4. **Coop 2P split-clavier** : curseur cyan P2 + flèches + Enter. Feature mobile-targeted, peu de demande Mike.
5. **5 Mini-jeux Foire** : chamboule-tout, tir au pigeon, roue fortune, course crêpes, bumper cars. Hors scope tower defense.
6. **Webfonts Bangers/Fredoka** : Google Fonts via index.html. V6 utilise Roboto TMP Font Asset (alternative validée).

**Si Mike change d'avis** : édite §3.3 et ajoute au scope §4 manuellement avant /goal.

---

## 17. Historique (résumé compressé pré-pivot)

**2026-05-11** : Plan refonte Phaser V5 8 semaines (R1→E2) drafté dans `/Users/mike/.claude/plans/rustling-nibbling-wirth.md`. Décisions Q1-Q18 actées en interview Mike (préservées §3.1).

Sprint R1 (recherche industrie) ✅ green, 4 rapports livrés.
Sprint R2 (recherche industrie suite + audit Milan) ✅ green, 4 rapports + 11/12 hard assertions.
Sprint R3 (tooling + portability research) ✅ green, R3-01 + R3-02 livrés.
Sprint D1 (design coeur) ✅ green, 4 specs livrés (D1-01 économie, D1-02 pacing, D1-03 L3 hybride, D1-04 castle HP).

Sprint D2/E1/E2 **CANCELLED** post-pivot Unity (Q18=B) le 2026-05-11 fin journée.

**2026-05-12 → 2026-05-14** : Sprint MIGRATE Unity Phases 0→4 SHIPPED (~660 commits, /v6/ deployed).
- Phase 0 : Setup.
- Phase 1 : POC W1-1.
- Phase 2 : Core (towers, enemies, economy, waves).
- Phase 3 : Visuels (URP shaders, post-process, VFX, audio).
- Phase 4 : UI polish (HUD, EndScreen, TowerCompare, SplashScreen, WorldMap).

Sprint R6-PARITY-V4-FINAL ✅ CLOSED 2026-05-13 (85-90% parité visible).
Sprint R7-PUSH-100 ACTIVE 2026-05-13 → en cours (75-80% → 95-100%).

**Sprint R7-WORLDMAP-NAN** (sous-sprint R7) ✅ CLOSED 2026-05-14 après 9 itérations V7→V8I :
- V7 Color.black opaque, V8 raycastTarget toggle, V8B LoadingGroup RectTransform, V8C Clickable manipulator, V8E auto-default Avatar/Hero, V8F tile=Button, V8G PathManager fallback, V8H LevelRunner ExecOrder -200, V8I skybox apply on Awake.
- Gameplay loop reachable browser /v6/ (Mike confirmed 2026-05-14 00h00).

**2026-05-14** : 5 audits V4↔V6 livrés. Brief synthèse + ce plan consolidé créés.

**Ancien plan** `/Users/mike/.claude/plans/rustling-nibbling-wirth.md` : préservé pour historique, header de redirection vers ce nouveau plan.

---

**FIN du plan.**

Pour démarrer Phase 5 : voir **§12 Étapes Mike** + **§13 To-do list** + **§11 /goal exact**.
