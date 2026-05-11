# Crowd Defense — Status Tracker (Unity Migration)

> Source of truth multi-session du sprint MIGRATE. À lire en début de chaque session opus, à updater en fin.

## Where we are

- **Current sprint** : MIGRATE Phase 0 (setup infra) — démarré 2026-05-11.
- **Current focus** : Install Unity Hub OK ; en attente login Mike + license activation ; install Unity 6 LTS Editor next.
- **Next milestone** : `/plan` formel migration via ExitPlanMode, puis Phase 1 POC (1 niveau fonctionnel).

## 📋 Contexte pivot

Ce repo est né d'un **pivot majeur** acté le 2026-05-11. Le projet précédent (prototype web Phaser/Three.js dans `milan project` repo) a été archivé sur tag `v5.0-pre-pivot-unity` après lecture du rapport R3-02 portability research.

**Décision** : passer de Three.js JS vanilla → Unity 6 LTS C# avec Unity-MCP pour automation Claude Code.

**Raisons** :
- Shipping multi-platform natif (Steam, iOS, Android, WebGL) requis < 12 mois.
- Tooling Unity intégré (ScriptableObjects, Inspector, Scene editor) annule besoin custom tooling (Sprint TE Phaser annulé).
- Unity-MCP mature (5800★, 34+ tools, batch_execute 10-100× speedup) → autonomie Opus ~70-80 % sur la migration tech.

**Skills Mike Unity/C# au départ** : zéro (option C). Apprentissage on-the-fly via vulgarisations + Unity-MCP-piloté.

## 🚀 Sprint MIGRATE phases

| Phase | Goal | Estimé | Status |
|-------|------|--------|--------|
| **0** | Setup Unity Hub + Editor + Unity-MCP + projet Unity init | 1-3 j | in_progress |
| **1** | POC 1 niveau fonctionnel (Tower + Enemy + LevelRunner + HUD) | 1-2 sem | pending |
| **2** | Migration core (12 TOWER_TYPES + 30 ENEMY_TYPES + LevelRunner + Q1-Q14 specs) | 3-4 sem | pending |
| **3** | 80 levels + pacing bouton + castle HP + boss + cutscenes | 2-3 sem | pending |
| **4** | Builds Mac/Win/iOS/Android/WebGL + Steam/App Store/Play Store | 1-2 sem | pending |

**Total estimé** : 7-12 sem (vs 12-18 industry avg).

## Phase 0 — Setup checklist détaillée

### Install Unity Hub (autonome Opus)
- [x] `brew install --cask unity-hub` → installé `/Applications/Unity Hub.app` (2026-05-11)
- [x] Vérif CLI `Unity Hub.app/Contents/MacOS/Unity Hub -- --headless help` → OK

### Login Unity Hub + license (manuel Mike, ~5 min)
- [ ] Lancer Unity Hub
- [ ] Click "Sign in" → ouvre browser
- [ ] Login compte Unity (créer si pas existant — Personal gratuit pour solo dev <100K $/an)
- [ ] Back to Hub → activate Personal license
- [ ] Verify : Hub affiche "Personal" en haut

### Install Unity 6 LTS Editor + modules (autonome via CLI post-login)
- [ ] `"Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install --version 6000.0.74f1`
- [ ] Modules : Mac Build Support (toujours), iOS Build Support, Android Build Support, WebGL Build Support
- [ ] Wait download ~10 GB (~30-60 min selon connexion)
- [ ] Verify : `editors -i` liste 6000.0.74f1 (Apple silicon)

### Setup Unity-MCP server (autonome Opus)
- [ ] Clone CoplayDev/unity-mcp dans `~/Tools/unity-mcp` ou via package manager
- [ ] Config Claude Code : ajouter MCP server entry dans `~/.claude/settings.json` ou `~/.claude.json`
- [ ] Restart Claude Code → tools `mcp__unity-mcp__*` disponibles

### Init projet Unity dans crowd-defense (autonome Opus)
- [ ] `/Applications/Unity/Hub/Editor/6000.0.74f1/Unity.app/Contents/MacOS/Unity -batchmode -createProject /Users/mike/Work/crowd-defense -quit`
- [ ] First open via Hub → Unity génère `Assets/`, `Packages/`, `ProjectSettings/`
- [ ] `.gitignore` Unity déjà en place ✓
- [ ] `.gitattributes` Unity LFS déjà en place ✓

### Install Unity-MCP plugin dans projet (autonome Opus)
- [ ] Add package via Package Manager URL ou manifest.json edit
- [ ] Verify : Unity-MCP toolbar visible dans Unity Editor

### `/plan` formel migration (Opus rédige + Mike valide)
- [ ] Lancer `/plan` skill dans Claude Code
- [ ] Tickets MIGRATE-01..MIGRATE-N définis avec dépendances
- [ ] ExitPlanMode validation Mike

## Decisions arbitrées Mike (Q1-Q18)

Référence complète : `docs/decisions/Q1-Q18-arbitrages.md`.

**Synthèse one-liner par décision** :
- Q1 : floor reward endless 0.70 + fin mécanique W>10 (`difficultyMul = 1.1^(world-10)`)
- Q2 : treasure value range 50-150¢
- Q3 : cap global 1 magnet/level (opt-in 2 sur W7+)
- Q4 : auto-start OFF strict (zéro fail-safe)
- Q5 : skip bonus +30¢ flat fenêtre 5s
- Q6 : streak reset uniquement si fenêtre expire (clic hors fenêtre conserve)
- Q7 : debounce 300 ms clic + N
- Q8 : refund pelle 80 % statu quo
- Q9 : pierce non capé (borné range projectile)
- Q10 : 1-click direct au choix L3
- Q11 : no-regen W6+ skip total
- Q12 : override castleHP JSON optionnel
- Q13 : UI HUD HP couleur → D1-09 (passe UI dédiée)
- Q14 : floor castleHP W1-1 = 200 HP
- Q15 : tooling overlay Tweakpane phase 1 → **DÉPRÉCIÉ Unity** (Inspector natif remplace)
- Q16 : data JSON séparés → **devient ScriptableObjects Unity**
- Q17 : save-back Vite plugin → **DÉPRÉCIÉ Unity** (Editor save direct)
- Q18 : **MIGRER UNITY 6 LTS direct** (plan 8 sem Phaser annulé)

## Specs D1 ramenées

| Spec | Path | Statut Unity |
|---|---|---|
| D1-01 économie | `docs/specs/design/D1-01-economy-spec.md` | À ré-implémenter en C# (`Balance.cs` + `Economy.cs` + `TowerType` SO) |
| D1-02 pacing | `docs/specs/design/D1-02-pacing-spec.md` | UI réimplémenté en Unity UI Toolkit (UI Builder + USS) |
| D1-03 L3 hybride | `docs/specs/design/D1-03-upgrade-l3-hybride-spec.md` | API `Tower.UpgradeTo(level, branch)` en MonoBehaviour |
| D1-04 castle HP | `docs/specs/design/D1-04-castle-hp-spec.md` | Formules portables en `Balance.cs` static helpers |

## Research ramenée (référence stratégique)

- `docs/research/R1-01..R1-04` : benchmarks industrie (économie, pacing, mapdesign, autoqa)
- `docs/research/R2-04..R2-07` : synergies, difficulty curve, milan audit, perf baseline
- `docs/research/R3-01..R3-02` : tooling research, portability research (a abouti au pivot Unity)

## Open TODOs (transverse)

- [x] Pivot Q18=B acté (2026-05-11)
- [x] Repo `crowd-defense` créé (privé GitHub + local `/Users/mike/Work/crowd-defense/`)
- [x] Doc ramenée (specs D1, research R1-R3, decisions Q1-Q18, agents personas)
- [x] `.gitignore` + `.gitattributes` Unity standards en place
- [x] CLAUDE.md + STATUS.md initiaux écrits
- [x] Unity Hub installé via brew
- [ ] **Mike : login Unity Hub + license Personal** (~5 min manuel)
- [ ] Install Unity 6 LTS Editor + modules (CLI autonome post-login)
- [ ] Install Unity-MCP server + plugin
- [ ] Init projet Unity dans crowd-defense
- [ ] `/plan` formel migration + ExitPlanMode
- [ ] Phase 1 POC démarre

## Risks raised

- **Unity-MCP édge cases** : encore jeune (5800★ mais 2025-2026), peut buguer sur cas complexes. À monitorer pendant Phase 1.
- **WebGL bundle size** : ~5-15 MB compressé vs 395 KB Phaser. Acceptable mais perd identité "web léger". À benchmarker post-POC.
- **Steamworks SDK + Apple Developer + Google Play setup** : nécessite Mike (credentials privés). Pas bloquant avant Phase 4.

## Recent commits

À paster `git log --oneline -5` à chaque session.

```
(initial bootstrap pending)
```

---

## 🌅 Notes pour Mike au prochain réveil session

### Ce que tu trouves
1. **Repo crowd-defense créé** local + remote GitHub (privé).
2. **Doc ramenée** dans `docs/` : specs D1, research R1-R3, decisions Q1-Q18.
3. **Unity Hub installé** via brew. Reste à login.
4. **CLAUDE.md + STATUS.md** initialisés.

### Pour reprendre
1. Login Unity Hub (manuel, ~5 min).
2. Dis-moi "OK Hub login done" → je continue install Editor + Unity-MCP en autonome.
3. Une fois Unity-MCP up, on lance `/plan` formel migration.
4. Phase 1 POC démarre.
