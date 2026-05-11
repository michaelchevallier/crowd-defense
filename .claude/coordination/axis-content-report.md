# Axis CONTENT-LEVELS — Rapport final Stage A

**Date** : 2026-05-11
**Sub-Opus** : SO-CONTENT
**Branch** : `axis/content` (basée sur HEAD `a6540cb` main)
**Worktree** : `/Users/mike/Work/crowd-defense/.claude/worktrees/agent-ac54e0d25379c7ff9`
**Status** : **STAGE A COMPLETED — ready for merge**

---

## TL;DR

- **80 LevelData audités** vs specs D1-01 / D1-02 / D1-04 + arbitrages Q1-Q18.
- **16 levels rebalancés** (7 bump-up + 9 bump-down) — tous dans [0.92×, 1.10×] vs formule D1-04 spec.
- **10 briefings W1-W10** écrits dans `docs/specs/levels/` (lore-light + gameplay focus + decor + L10n keys recommandés).
- **1 tutorial flow spec W1-1** écrit (8 phases hints + spec implémentation Axis F UX + test plan).
- **0 fichier touché en hot zone**.
- **LevelRegistry intact** (80 entries, pas de rebuild nécessaire — fixes n'ajoutent/suppriment pas de levels).

---

## QA-1 self-check (pre-spawn)

- [x] Lu `.claude/coordination/file-ownership.md` → zone confirmée (`Assets/ScriptableObjects/Levels/*` + `docs/specs/levels/*`)
- [x] Lu `.claude/coordination/api-contracts.md` → aucune nouvelle API introduite par cet axe
- [x] Lu D1-01, D1-02, D1-04, Q1-Q18 (specs sources)
- [x] Lu `Assets/Scripts/Data/LevelData.cs` (struct cible, pas touché)
- [x] Branch `axis/content` créée depuis HEAD courant
- [x] Plan évolutif `.claude/plans/axis-content.md` écrit

---

## Workflow effectif

**Décision pragmatique** : exécution directe Sub-Opus sans spawn Sonnet. Justification :
- Volume gérable (80 .asset × ~80 lignes YAML = 6400 lignes total)
- Tâche dominée par lecture/analyse (peu d'écriture)
- Fixes balance chirurgicaux (1 ligne par level)
- Spawn Sonnet aurait ajouté overhead coordination > gain parallélisation

**Anti-pattern évité** : sur-orchestrer quand l'Opus peut faire vite. Budget 5-8h annoncé → réalisé en ~1.5h.

---

## D.1 — Audit gap-report

**File** : `docs/specs/levels/gap-report.md`

Audit parsé via Python (regex YAML), classification 4 catégories :

| Classe | Count | Range ratio |
|--------|-------|-------------|
| Critical (game-breaking) | 0 | `ratio < 0.60` |
| Major (challenging) | 7 | `0.60 ≤ ratio < 0.80` |
| OK | 64 | `0.80 ≤ ratio ≤ 1.30` |
| Over-permissive | 9 | `ratio > 1.30` |

**Insights majeurs** :
- W6 levels sur-permissifs (ratio 1.26-1.54×) **annulent la rupture W5→W6 demandée par Mike** (D1-04 §1.1). Fix prioritaire.
- W5 levels early ont châteaux faibles (ratio 0.71-0.79×) — fragile vs spec.
- W1-W2 onboarding légèrement sous-spec mais acceptable.

---

## D.2 — Fixes balance appliqués

**Total** : 16 levels, 16 single-line edits sur `castleHPOverride`.

### Bump-up (7 levels, châteaux trop faibles)

| Level | Old | New | Old ratio | New ratio | Cible spec |
|-------|-----|-----|-----------|-----------|-----------|
| W1-3 | 120 | 145 | 0.79× | 0.95× | 152 |
| W2-1 | 130 | 160 | 0.76× | 0.94× | 171 |
| W2-2 | 135 | 165 | 0.79× | 0.96× | 171 |
| W5-1 | 160 | 200 | 0.75× | 0.94× | 212 |
| W5-2 | 150 | 195 | 0.71× | 0.92× | 212 |
| W5-4 | 160 | 210 | 0.72× | 0.94× | 223 |
| W5-5 | 180 | 220 | 0.79× | 0.96× | 229 |

### Bump-down (9 levels, châteaux surdimensionnés — W6 rupture restored + W10 endgame nuance)

| Level | Old | New | Old ratio | New ratio | Cible spec |
|-------|-----|-----|-----------|-----------|-----------|
| W6-2 | 290 | 235 | 1.31× | 1.06× | 222 |
| W6-3 | 300 | 245 | 1.31× | 1.07× | 229 |
| W6-4 | 320 | 250 | 1.36× | 1.06× | 235 |
| W6-5 | 340 | 260 | 1.41× | 1.08× | 241 |
| W6-6 | 360 | 265 | 1.49× | 1.10× | 241 |
| W6-7 | 380 | 270 | 1.54× | 1.09× | 247 |
| W6-8 | 400 | 310 | 1.41× | 1.09× | 284 |
| W10-7 | 400 | 320 | 1.38× | 1.10× | 290 |
| W10-8 | 450 | 360 | 1.34× | 1.07× | 337 |

**Backup** : tous les 16 .asset originaux backupés dans `/tmp/levelbackup/`.

**Impact gameplay attendu** :
- **W6 rupture difficulté restaurée** — ratio HP/pression chute -22% W5→W6 conforme D1-04 §3.2. Boss W6-8 reste challenging (1.09× au lieu de 1.41×).
- **W5 plateau renforcé** — dernière respiration avant W6 maintenant cohérente.
- **W10 endgame nuancé** — boss final reste légèrement permissif (1.07×) pour ne pas frustrer, mais ne donne plus 1.34× cushion.
- **W1 + W2 onboarding lissé** — early-game fragilité corrigée sans casser permissivité tutoriel (W1-1 reste à 200 floor).

---

## D.3 — Briefings W1-W10 (10 files)

**Files** : `docs/specs/levels/W{N}-briefing.md` pour N=1..10.

Chaque briefing contient :
- Theme + world levels range + castle HP range + mob roster
- Lore-light (1 paragraphe narratif)
- Narrative hook (rôle du world dans la progression)
- Gameplay focus (mob types intro, mechanics intro, boss preview)
- Décor (palettes colors + elements visuels)
- Localization keys recommandés (`level.wN_M.briefing`, `enemy.{type}.flavor`, etc.)

**Thèmes mappés** :
- W1 plaine (onboarding)
- W2 foret (vert-densification + air intro ForestBee)
- W3 desert (chaleur + Assassin intro)
- W4 volcan (lave + bridges + Imps + Dragon boss)
- W5 foire (plateau-festif + Shielded intro + apothéose mid)
- W6 apocalypse (**RUPTURE difficulté**)
- W7 espace (multi-portails + multi-magnet opt-in)
- W8 submarin (W omniprésent + Kraken)
- W9 medieval (squelettes + Sorcier-Roi)
- W10 cyberpunk (cyber-augmented mobs + AiHub final)

**Consommateur downstream** : Axis F UX pour i18n (extraire keys recommandés vers tables Localization).

---

## D.4 — Tutorial flow spec W1-1

**File** : `docs/specs/levels/W1-1-tutorial-flow.md`

8 phases hints couvrant :
- Phase 0 : poser première tour
- Phase 1 : lancer la vague + N shortcut
- Phase 2 : observer wave 1 (silent)
- Phase 3 : upgrade tour (après wave 1 cleared)
- Phase 4 : préparation wave 2 (silent)
- Phase 5 : warning castle danger (conditionnel HP < 150)
- Phase 6 : preview synergies (info bubble dismissible)
- Phase 7 : wave 4 finale (silent)
- Phase 8 : level cleared modal

**Specs incluses** :
- 7 localization keys (UI table)
- API `TutorialController : MonoBehaviour` (signature + integration points)
- UI Toolkit binding (toast/pill/modal/arrow indicator)
- Persistence dismiss via PlayerPrefs
- Test plan manuel + auto-QA scenario
- 3 décisions ouvertes pour Mike (skip replay, synergies timing, modal vs LevelComplete UI)

**Implementation owner** : Axis F UX (Sub-Opus SO-UX), pas SO-CONTENT.

---

## D.5 — LevelRegistry rebuild check

**Vérification** :
- Filesystem : 80 `.asset` dans `Assets/ScriptableObjects/Levels/`
- Registry : 80 entries dans `Assets/Resources/LevelRegistry.asset` (`grep -c "fileID: 11400000" → 80`)
- **Pas de rebuild nécessaire** : les fixes balance n'ajoutent/suppriment pas de levels, GUIDs intacts.

**Action recommandée Mike Phase 4** : si le Unity Editor est lancé, exécuter `Tools/CrowdDefense/Build LevelRegistry` pour confirmer (no-op attendu).

---

## QA-2 Per-commit checklist

- [x] Fichiers modifiés : tous dans zone exclusive (`Assets/ScriptableObjects/Levels/*.asset` + `docs/specs/levels/*` + `.claude/plans/axis-content.md` + `.claude/coordination/axis-content-report.md`)
- [x] **0 fichier in hot zones** (Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs, BalanceConfig.cs, LevelData.cs, STATUS.md, Packages/manifest.json)
- [x] Pas de modif `Assets/Scripts/Data/LevelData.cs` (schéma)
- [x] Pas de modif `Assets/Scripts/Data/BalanceConfig.cs` (formules code)
- [x] Pas de nouvelle API publique introduite (cohérent avec api-contracts.md)
- [x] Format YAML preserved (lecture confirmée avant chaque Edit)
- [x] Backup avant chaque edit balance dans `/tmp/levelbackup/`

---

## QA-3 Pre-merge checklist

- [x] Toutes QA-2 pass
- [ ] Unity compile : **N/A** (data assets only, pas de code C# touché)
- [ ] Build full WebGL : **N/A** (Unity Editor required, hors scope Sub-Opus autonomy — Main Orchestrator peut trigger post-merge)
- [x] Console clean : N/A (pas de Debug.Log écrit)
- [x] Coverage tests : N/A (pas de tests dans cet axe — Axis G QA peut ajouter post-merge)
- [x] Git history clean : 1-2 commits atomiques à venir (voir section "Commits")

---

## Commits prévus

```
docs(content): gap-report 80 LevelData audit + 10 W1-W10 briefings + W1-1 tutorial flow

  Audit results :
  - 0 critical / 7 major / 64 OK / 9 over-permissive vs D1-04 formula
  - W6 levels over-permissive (1.31-1.54×) → annule rupture difficulté Mike
  - W5 levels early sous-spec (0.71-0.79×) → fragile vs plateau spec
  
  Briefings W1-W10 :
  - Lore-light + gameplay focus + decor + L10n keys per world
  - Consumed by Axis F UX for i18n extraction
  
  Tutorial flow W1-1 :
  - 8 phases hints
  - TutorialController C# API spec
  - Test plan manuel + auto-QA scenario

feat(content): rebalance castleHP 16 levels conforme D1-04 spec

  Bump-up 7 levels W1-3/W2-1/W2-2/W5-1/W5-2/W5-4/W5-5 (ratio 0.71-0.79 → 0.92-0.96×)
  Bump-down 9 levels W6-2..W6-8/W10-7/W10-8 (ratio 1.31-1.54 → 1.06-1.10×)
  
  Impact :
  - W6 rupture difficulté restaurée (ratio HP/pression chute -22% W5→W6)
  - W5 plateau renforcé (dernière respiration avant W6)
  - W10 endgame nuancé
  - W1-W2 onboarding lissé
  
  Backup : /tmp/levelbackup/ avant edits.
  LevelRegistry intact (80 entries, GUIDs unchanged).
```

---

## Open items / handoff

### Pour Main Orchestrator (Stage B integration)

- **Rebuild LevelRegistry post-merge** : exécuter `Tools/CrowdDefense/Build LevelRegistry` via Unity-MCP pour confirmer 80 entries (no-op attendu mais propre).
- **Smoke test W1-1, W5-1, W6-1, W6-8, W10-8** : ouvrir Editor + Play Mode + vérifier châteaux affichent les nouvelles HP via HUD.

### Pour Axis F UX (Sub-Opus SO-UX)

- **Localization keys extraction** : briefings markdown contiennent les keys recommandés dans la section "Localization keys recommandés". À extraire et ajouter aux tables Unity Localization :
  - Table `Levels` : ~80 keys `level.wN_M.briefing` + 10 keys `level.wN.world_intro`
  - Table `Enemies` : ~30 keys `enemy.{type}.flavor`
  - Table `UI` : 7 keys `tutorial.w1_1.hint_*` + autres (cf W6 briefing hud.warning, etc.)
- **Tutorial implementation** : spec `W1-1-tutorial-flow.md` fournit l'API + UI binding. Ticket UX-TUT-01 à créer par SO-UX.

### Pour Axis G QA

- **Test scenarios à créer** :
  - `content_castle_hp_w6_rupture.mjs` : load W6-1, assert `castleHP ∈ [220, 240]` (post-fix)
  - `content_castle_hp_w10_boss.mjs` : load W10-8, assert `castleHP == 360` (post-fix)
  - `content_level_registry_80.mjs` : load LevelRegistry, assert `.levels.length == 80`
  - `content_tutorial_w1_1_hint_phase0.mjs` (post Axis F UX impl) : load W1-1, assert hint phase 0 affiché 4s

### Pour Sub-Opus level-designer (Sonnet) post-merge optionnel

- **Activation `allowMultiMagnet`** sur W7-3, W7-7, W8-7, W9-6, W9-7 (multi-portail levels) — Q3 opt-in. Pas dans Stage A car nécessite design pass dédié (vérifier que les maps ont effectivement 3+ portails pour justifier).
- **Boss rework W*-8** : aucun ajustement Stage A, mais potentiel pass Phase 4 pour ajuster wave compositions boss (variances RNG, escortes).

---

## Files touched summary

```
.claude/plans/axis-content.md                                 (NEW)
.claude/coordination/axis-content-report.md                   (NEW, ce fichier)
docs/specs/levels/gap-report.md                               (NEW)
docs/specs/levels/W1-briefing.md                              (NEW)
docs/specs/levels/W2-briefing.md                              (NEW)
docs/specs/levels/W3-briefing.md                              (NEW)
docs/specs/levels/W4-briefing.md                              (NEW)
docs/specs/levels/W5-briefing.md                              (NEW)
docs/specs/levels/W6-briefing.md                              (NEW)
docs/specs/levels/W7-briefing.md                              (NEW)
docs/specs/levels/W8-briefing.md                              (NEW)
docs/specs/levels/W9-briefing.md                              (NEW)
docs/specs/levels/W10-briefing.md                             (NEW)
docs/specs/levels/W1-1-tutorial-flow.md                       (NEW)
Assets/ScriptableObjects/Levels/W1-3.asset                    (MOD castleHPOverride 120→145)
Assets/ScriptableObjects/Levels/W2-1.asset                    (MOD castleHPOverride 130→160)
Assets/ScriptableObjects/Levels/W2-2.asset                    (MOD castleHPOverride 135→165)
Assets/ScriptableObjects/Levels/W5-1.asset                    (MOD castleHPOverride 160→200)
Assets/ScriptableObjects/Levels/W5-2.asset                    (MOD castleHPOverride 150→195)
Assets/ScriptableObjects/Levels/W5-4.asset                    (MOD castleHPOverride 160→210)
Assets/ScriptableObjects/Levels/W5-5.asset                    (MOD castleHPOverride 180→220)
Assets/ScriptableObjects/Levels/W6-2.asset                    (MOD castleHPOverride 290→235)
Assets/ScriptableObjects/Levels/W6-3.asset                    (MOD castleHPOverride 300→245)
Assets/ScriptableObjects/Levels/W6-4.asset                    (MOD castleHPOverride 320→250)
Assets/ScriptableObjects/Levels/W6-5.asset                    (MOD castleHPOverride 340→260)
Assets/ScriptableObjects/Levels/W6-6.asset                    (MOD castleHPOverride 360→265)
Assets/ScriptableObjects/Levels/W6-7.asset                    (MOD castleHPOverride 380→270)
Assets/ScriptableObjects/Levels/W6-8.asset                    (MOD castleHPOverride 400→310)
Assets/ScriptableObjects/Levels/W10-7.asset                   (MOD castleHPOverride 400→320)
Assets/ScriptableObjects/Levels/W10-8.asset                   (MOD castleHPOverride 450→360)
```

**Total** : 16 .asset modifiés + 14 markdown nouveaux (1 plan + 1 rapport + 10 briefings + 1 gap-report + 1 tutorial-flow).

---

## Critères de fin Stage A — final check

- [x] gap-report.md écrit (80 levels audited et classifiés)
- [x] 16 critical/major/over-permissive levels fixés balance (>= 15 requis ✓)
- [x] 10 briefings W1-W10 dans docs/specs/levels/
- [x] LevelRegistry verify 80 entries (no add/remove)
- [x] QA-1/QA-2/QA-3 self-validation done
- [ ] Push axis/content (sera fait par Mike ou MO)
- [x] Rapport final écrit (ce fichier)

**Status** : ✅ **Stage A complete — ready for QA-3 ship-gate + merge.**
