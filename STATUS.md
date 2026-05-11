# Crowd Defense — Status Tracker (Unity Migration)

> Source of truth multi-session du sprint MIGRATE. À lire en début de chaque session opus, à updater en fin.

## Where we are

- **Current sprint** : MIGRATE Phase 0 ✅ **DONE** (setup infra complète, 2026-05-11).
- **Current focus** : Phase 1 POC à démarrer (1 niveau fonctionnel W1-1, ~1-2 sem).
- **Next milestone** : `/plan` formel Phase 1 → Tickets MIGRATE-POC-01..08 → Sonnet feature-dev en série.

## 🚀 Instructions PROCHAINE SESSION OPUS

**À lire dans cet ordre** :
1. Ce fichier (STATUS.md) — tu y es.
2. `CLAUDE.md` (Unity context + Workflow Opus orchestre / Sonnet exécute).
3. `docs/decisions/Q1-Q18-arbitrages.md` (toutes les décisions Mike).
4. `docs/specs/design/D1-01..D1-04` (target design moteur-agnostique).
5. (Si besoin référence) `docs/research/R3-02-portability-research.md` pour le pourquoi du pivot.

### État infra (vérifier au boot)

- ✅ Unity Hub installé (`/Applications/Unity Hub.app`)
- ✅ Unity 6.3 LTS Editor (`6000.3.15f1` Apple silicon) installé
- ✅ WebGL Build Support module installé
- ✅ Python 3.11 + uv installés
- ✅ Projet Unity init (`Assets/`, `Packages/`, `ProjectSettings/`)
- ✅ Unity-MCP package installé (CoplayDev v9.6.x dans `Library/PackageCache/`)
- ✅ EditorPrefs `MCPForUnity.AutoStartOnLoad=true` + `UseHttpTransport=true` → HTTP server auto-démarre au load Unity Editor
- ✅ Claude Code MCP config `~/.claude.json` user-scope : `UnityMCP: http://127.0.0.1:8080/mcp`
- ✅ Tools `mcp__UnityMCP__*` chargeable via ToolSearch (44+ tools)

**Vérif rapide au boot** :
```bash
ps aux | grep "Unity.app/Contents/MacOS/Unity" | grep -v grep   # Editor running ?
lsof -iTCP:8080 -sTCP:LISTEN                                   # MCP HTTP server up ?
claude mcp list | grep UnityMCP                                # ✓ Connected ?
```

Si Editor pas running : `open -na "/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app" --args -projectPath /Users/mike/Work/crowd-defense` + attendre ~10s pour HttpAutoStartHandler.

### Action immédiate

Une fois infra verified, **démarre Phase 1 POC** :

**Option A** : Lance directement le `/plan` skill + ExitPlanMode pour formaliser les 8 tickets MIGRATE-POC-* avant exécution. Recommandé si Mike veut valider l'architecture.

**Option B** : Démarre direct MIGRATE-POC-01 (scaffold) en autonome via tools UnityMCP. Recommandé si Mike a déjà validé l'architecture cible (cf Phase 1 plan ci-dessous).

Mike décide après lecture de Phase 1 plan.

### Pour delegation Sonnet feature-dev (workflow standard)

Briefings tickets à écrire dans `.claude/specs/MIGRATE-POC-XX.md`. Personas Sonnet adaptés Unity context (feature-dev déjà sonnet, bug-fixer haiku, qa-tester haiku). Models override appliqués sur 3 agents (auto-qa-runner, level-designer, ux-designer) Opus→Sonnet.

## 📋 Contexte pivot (référence)

Repo né d'un pivot acté le 2026-05-11. Projet précédent (`milan project` Phaser/Three.js) archivé tag `v5.0-pre-pivot-unity`. Décision : passer à Unity 6 LTS + C# + Unity-MCP.

**Raisons** : shipping multi-platform natif <12 mois + tooling Unity intégré + Unity-MCP autonomie Opus ~70-80 %.

**Skills Mike Unity/C# au départ** : zéro (option C). Apprentissage on-the-fly via vulgarisations + Unity-MCP-piloté.

## 🚀 Sprint MIGRATE phases

| Phase | Goal | Estimé | Status |
|-------|------|--------|--------|
| **0** | Setup Unity Hub + Editor + Unity-MCP + projet Unity init | 1-3 j | ✅ **DONE** (2026-05-11) |
| **1** | POC 1 niveau W1-1 fonctionnel (Tower + Enemy + LevelRunner + HUD + WebGL build) | 1-2 sem | pending |
| **2** | Migration core (12 TOWER_TYPES + 30 ENEMY_TYPES + LevelRunner + Q1-Q14 specs) | 3-4 sem | pending |
| **3** | 80 levels + pacing bouton + castle HP + boss + cutscenes | 2-3 sem | pending |
| **4** | Builds Mac/Win/iOS/Android/WebGL + Steam/App Store/Play Store | 1-2 sem | pending |

**Total estimé** : 7-12 sem.

## Phase 1 POC — proposition architecture

**Cible** : W1-1 fonctionnel en Unity, valider que Unity-MCP me laisse coder en autonomie sur cycle complet.

**Scope minimal** :
- Grille 7×13 + 1 path
- 1 tower (crossbow) + 3 enemies (crawler, runner, brute)
- 3 waves
- Économie : gold rewards + tower placement cost
- Castle HP + game over
- HUD minimal (gold, wave, HP)
- WebGL build deploy `/v6/`

**Architecture cible** :
```
Assets/Scripts/
  Data/        # SO : TowerType, EnemyType, LevelData
  Entities/    # MonoB : Tower, Enemy, Castle
  Systems/     # LevelRunner, WaveManager, Economy, PathManager
  UI/          # HudController
Assets/Prefabs/, ScriptableObjects/, Scenes/
```

**Tickets proposés (~12-17 commits, 1-2 Sonnet feature-dev en série)** :
1. MIGRATE-POC-01 : Scaffold project structure + scenes + folders
2. MIGRATE-POC-02 : SO definitions (TowerType, EnemyType, LevelData) + 1 instance each
3. MIGRATE-POC-03 : Grid + path system (`PathManager`)
4. MIGRATE-POC-04 : Enemy spawn + movement (`WaveManager`, `Enemy`)
5. MIGRATE-POC-05 : Tower targeting + shooting (`Tower`, projectile)
6. MIGRATE-POC-06 : Economy + castle HP + game over (`Economy`, `Castle`)
7. MIGRATE-POC-07 : UI HUD minimal (`HudController`)
8. MIGRATE-POC-08 : WebGL build + deploy `/v6/` (Steamcmd / GH Pages)

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
- [x] Mike : login Unity Hub + license Personal activée
- [x] Unity 6.3 LTS Editor (`6000.3.15f1`) installé via Hub CLI
- [x] WebGL Build Support module installé
- [x] Python 3.11 + uv (Unity-MCP requirement) installés
- [x] Projet Unity init dans crowd-defense (Assets/Packages/ProjectSettings)
- [x] Unity-MCP package CoplayDev installé via manifest.json git URL
- [x] EditorPrefs AutoStartOnLoad + UseHttpTransport set → HTTP server auto-démarre
- [x] Claude Code config user-scope `UnityMCP ✓ Connected`
- [x] Smoke test UnityMCP (`manage_scene get_active` + `read_console` réussis)
- [x] Allowlist `crowd-defense/.claude/settings.json` (10 MCP patterns read-only)
- [x] Model overrides agents (auto-qa-runner/level-designer/ux-designer Opus→Sonnet)
- [x] Memory consolidation (V3-obsoletes deleted, transverses copied, project_crowd_defense_unity + feedback_unity_skill added)
- [ ] **`/plan` formel Phase 1 POC** + ExitPlanMode validation Mike
- [ ] Phase 1 POC tickets MIGRATE-POC-01..08 (~12-17 commits)
- [ ] Phase 1 POC livré : W1-1 jouable WebGL `/v6/`

## Risks raised

- **Unity-MCP edge cases** : encore jeune (5800★ mais 2025-2026), peut buguer sur cas complexes. À monitorer pendant Phase 1.
- **WebGL bundle size** : ~5-15 MB compressé vs 395 KB Phaser. Acceptable mais perd identité "web léger". À benchmarker post-POC.
- **Steamworks SDK + Apple Developer + Google Play setup** : nécessite Mike (credentials privés). Pas bloquant avant Phase 4.
- **`GITHUB_TOKEN` env var invalide hérité du shell parent Mike** : workaround actuel = `unset GITHUB_TOKEN` ajouté au snapshot zsh Claude Code de la session 2026-05-11. **Pour les futures sessions** : Mike doit tracer la source dans son shell parent (`set | grep GITHUB_TOKEN` AVANT lancement `claude`) et l'unset là. Sinon chaque `gh` command bind sur le token expiré → 401.
- **Mike skill Unity/C# = zéro** : nécessite vulgarisation à chaque concept Unity nouveau introduit. Anticipé dans `feedback_unity_skill_and_autonomy` memory.

## Recent commits

```
fbc76e0 chore: bootstrap crowd-defense Unity port from milan project pivot
```

(`git log --oneline -5` à updater à chaque session)

---

## 🌅 Notes pour Mike au prochain réveil session

### Ce que tu trouves
1. **Phase 0 Setup ✅ DONE** : Unity Hub + Editor 6.3 LTS + WebGL + Unity-MCP + projet init + smoke test.
2. **Tout est auto** : prochain launch Unity Editor → HttpAutoStartHandler démarre server auto → tools `mcp__UnityMCP__*` disponibles après restart Claude Code.
3. **Doc complète** dans `docs/` : specs D1, research R1-R3, decisions Q1-Q18, plan archive Phaser.
4. **Phase 1 POC plan** déjà esquissé dans cette STATUS.md (architecture + 8 tickets).

### Pour reprendre
1. Vérifier Unity Editor running + MCP server up (cf "Vérif rapide au boot" ci-dessus).
2. Si tout vert : démarre `/plan` formel Phase 1 POC OU lance direct MIGRATE-POC-01 scaffold.
3. Si Editor pas running : relance via `open -na ...` (cf commande dans Instructions).

Bon courage. La migration commence vraiment maintenant.
