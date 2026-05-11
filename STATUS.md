# Crowd Defense — Status Tracker (Unity Migration)

> Source of truth multi-session du sprint MIGRATE. À lire en début de chaque session opus, à updater en fin.

## ⚠️ AVANT TOUT — fix env shell parent Mike

Si Mike vient de switcher cwd ici via `cd /Users/mike/Work/crowd-defense && claude`, le `GITHUB_TOKEN` invalide hérité de son shell parent va **réapparaître** dans cette session (le fix précédent était dans le snapshot zsh de l'ancienne session). Symptôme : `gh repo create`, `gh repo view`, etc. retournent `HTTP 401: Bad credentials`.

**Fix immédiat session courante** :
```bash
# dans n'importe quelle commande Bash que tu lances :
unset GITHUB_TOKEN && gh <whatever>
```

**Fix permanent** : Mike doit tracer la source dans son shell parent. `~/.zshrc` ligne 21 est commenté donc innocent. Le token vient probablement d'une commande tapée manuellement dans son shell ou d'un fichier sourced (.zsh_history replay, alias claude wrapper, etc.). Diagnostic à faire **AVANT** le prochain `claude` :
```bash
set | grep GITHUB_TOKEN
# si présent : trouve la source et retire-la
unset GITHUB_TOKEN
```

## Where we are

- **Current sprint** : MIGRATE Phase 1 POC ✅ **DONE** (2026-05-11, ~2h Sonnet pipeline).
- **Current focus** : activation GitHub Pages (action manuelle Mike) + Phase 2 ramp-up.
- **Next milestone** : Phase 2 migration core (12 TOWER_TYPES + 30 ENEMY_TYPES + 80 levels + D1 specs).

### Phase 1 POC livré

**18 commits** (17 sur `main` + 1 sur `gh-pages`) en ~2h d'exécution multi-agent parallèle.

| Ticket | Status | Commits main |
|---|---|---|
| POC-01 scaffold + scene 3D | ✅ | `d4b41ab`, `41ec424` |
| POC-02 SO defs + W1-1 instances | ✅ | `f6be1fe`, `f57a629` |
| POC-03 grid + BFS pathfinding | ✅ | `3180cb2`, `931875d` |
| POC-04 Enemy + WaveManager | ✅ | `0133070` |
| POC-05 Tower + Projectile + Placement | ✅ | `23661fd`, `818b6f9` |
| POC-06 Economy + Castle + LevelRunner | ✅ | `d5f140d`, `0d04a0a`, `b5982a9` |
| POC-07 HUD UI Toolkit | ✅ | `e2890c2` (partial), `742e2b1` (final) |
| POC-08 WebGL build + deploy /v6/ | ✅ | `9c5b52f` (partial), `1f269eb`, `05726b3` ; `gh-pages: 051686a` |

**Build WebGL** : 6 MB compressé Brotli, IL2CPP, 123s sur Mac M1. `Builds/WebGL/` local OK.

### ⚠️ ACTION MANUELLE Mike requise

**Activer GitHub Pages** : `https://github.com/michaelchevallier/crowd-defense/settings/pages`
- Source : `gh-pages` branch
- Folder : `/` (root)
- Save → propagation ~30s

Puis l'URL `https://michaelchevallier.github.io/crowd-defense/v6/` retournera HTTP 200 et le jeu sera publié.

Pour Mike : tu peux aussi tester local sans activation GitHub Pages :
```bash
cd /Users/mike/Work/crowd-defense/Builds/WebGL && python3 -m http.server 8000
# → http://localhost:8000
```

### Décisions Phase 1 POC actées (Mike 2026-05-11)

- **Scope full fidélité** sur 4 axes : UI Toolkit (pas uGUI), 2.5D 3D primitives (pas 2D sprites), 7×15 fidèle Phaser avec stream/bridge/lava, deploy /v6/ inclus POC.
- **Wave sizing** : 35/76/87/90 fidèle source (288 enemies total) + dev cheat `Time.timeScale=10f` touche Tab dans LevelRunner.
- **Pas de pool POC** : Instantiate/Destroy direct (<50 mobs simultanés). Pool en Phase 2 si besoin.
- **W1-1 castleHP=120** (fidèle source, pas 200 spec Q14).
- **TOWER_DAMAGE_MUL=1.6** hardcoded const dans Tower.cs (à remonter en BalanceConfig SO Phase 2).

### Plan + briefs Sonnet

- Plan formel : `.claude/plans/phase1-poc-plan.md`
- Briefs auto-suffisants : `.claude/specs/MIGRATE-POC-01.md` .. `MIGRATE-POC-08.md` (8 fichiers, ~2300 lignes total)
- Editor menu fallbacks créés par Sonnet en cours de route : `Assets/Editor/POC05Setup.cs`, `POC06Setup.cs`, `POC07Setup.cs` (utiles si refs scene se cassent)

### Workflow autonome multi-agent — validé

Mike a explicitement autorisé "vraiment plusieurs agents en parallèle" (2026-05-11). 9 sub-agents Sonnet feature-dev orchestrés en pipeline + parallèle (POC-05 ∥ POC-08partial, POC-06 ∥ POC-07partial). Pattern à généraliser Phase 2 quand tickets orthogonaux.

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

## 📂 SOURCE CODE À MIGRER — où chercher quoi

**Repo source Phaser/Three.js (frozen)** : `/Users/mike/Work/milan project/` (sur disque local, NON dans ce repo crowd-defense). Tag `v5.0-pre-pivot-unity` = état figé pré-pivot — référence canonique pour tout port Unity.

**Comment lire le source** : Read tool avec paths absolus depuis `/Users/mike/Work/milan project/...`. Pas besoin de clone, le repo est déjà checkout localement.

**Structure clé du source à porter** :

| Source path | Quoi | Target Unity |
|---|---|---|
| `milan project/src-v3/entities/Tower.js` | 12 TOWER_TYPES + Tower class | `Assets/Scripts/Data/TowerType.cs` (SO) + `Assets/Scripts/Entities/Tower.cs` (MonoB) |
| `milan project/src-v3/entities/Enemy.js` | ~30 ENEMY_TYPES + Enemy class + behaviors | `Assets/Scripts/Data/EnemyType.cs` (SO) + `Assets/Scripts/Entities/Enemy.cs` (MonoB) |
| `milan project/src-v3/entities/Projectile.js`, `Mine.js`, `MagnetBomb.js`, etc. | Projectiles + spec entities | `Assets/Scripts/Entities/Projectile.cs` etc. |
| `milan project/src-v3/systems/LevelRunner.js` | Core game loop (waves, kills, gold, castleHP) | `Assets/Scripts/Systems/LevelRunner.cs` |
| `milan project/src-v3/systems/MapGrid.js` + `MapPathfinder.js` | Grid + pathfinding | `Assets/Scripts/Systems/PathManager.cs` |
| `milan project/src-v3/systems/Synergies.js` | Synergies passives | `Assets/Scripts/Systems/Synergies.cs` |
| `milan project/src-v3/data/levels/world*.js` | 80 levels JSON | `Assets/ScriptableObjects/Levels/W*-*.asset` |
| `milan project/src-v3/main.js:1097-1118` | `_upgradeCost` + radial menu | Cf spec D1-03 |
| `milan project/src-v3/CLAUDE.md` | Grammar v5 + vocab design | Référence, déjà copié dans `docs/design/grammar-v5.md` |
| `milan project/index.html` | HUD layout actuel | Unity UI Toolkit (UI Builder + USS) |

**Specs D1** dans `docs/specs/design/D1-*.md` référencent ces paths source. À chaque ticket MIGRATE-POC-XX, démarrer par `Read` du fichier source Phaser correspondant pour comprendre l'implementation actuelle, puis porter en C# Unity.

**Reset si milan project modifié** : `git -C "/Users/mike/Work/milan project" checkout v5.0-pre-pivot-unity` (mais shouldn't be modified — c'est une archive frozen).

---

## 📋 Contexte pivot (référence)

Repo né d'un pivot acté le 2026-05-11. Projet précédent (`milan project` Phaser/Three.js) archivé tag `v5.0-pre-pivot-unity`. Décision : passer à Unity 6 LTS + C# + Unity-MCP.

**Raisons** : shipping multi-platform natif <12 mois + tooling Unity intégré + Unity-MCP autonomie Opus ~70-80 %.

**Skills Mike Unity/C# au départ** : zéro (option C). Apprentissage on-the-fly via vulgarisations + Unity-MCP-piloté.

## 🚀 Sprint MIGRATE phases

| Phase | Goal | Estimé | Status |
|-------|------|--------|--------|
| **0** | Setup Unity Hub + Editor + Unity-MCP + projet Unity init | 1-3 j | ✅ **DONE** (2026-05-11) |
| **1** | POC 1 niveau W1-1 fonctionnel (Tower + Enemy + LevelRunner + HUD + WebGL build) | 1-2 sem | ✅ **DONE** (2026-05-11, ~2h Sonnet pipeline) |
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

## 🛠 Backlog tooling autonomie (à setup quand besoin)

Outils non installés pour maximiser autonomie Opus sur génération d'assets (3D / textures / audio / anims). Pas bloquant Phase 2. À setup au trigger correspondant.

### 1. Blender + Blender MCP — **T1**

- **Goal** : modeler towers/enemies low-poly via Python script Blender, export `.glb` auto-import Unity. Remplace les `GameObject.CreatePrimitive()` du POC par real models.
- **Source** : Blender free + MCP https://github.com/ahujasid/blender-mcp
- **Install** :
  ```bash
  brew install --cask blender
  uvx blender-mcp  # installs server
  # + addon Python dans Blender (Preferences > Add-ons > Install from File)
  # + config ~/.claude.json mcpServers : "blender": { "command": "uvx", "args": ["blender-mcp"] }
  ```
- **À faire quand** : démarrage Phase 3 visuels OU première fois qu'on veut un tower visuel non-primitive.

### 2. ComfyUI MCP — **T1**

- **Goal** : wrapper MCP sur le ComfyUI:8188 + Flux Schnell existant pour gen textures via tool call au lieu de bash python.
- **Source** : https://github.com/joshuajaco/comfy-mcp-server
- **Install** :
  ```bash
  npx -y comfy-mcp-server
  # + config ~/.claude.json mcpServers : "comfy": { "command": "npx", "args": ["-y", "comfy-mcp-server"], "env": { "COMFYUI_URL": "http://127.0.0.1:8188" } }
  ```
- **Pipeline existant** : `python3 /Users/mike/Work/milan project/tools/gen_textures.py` (cf memory `reference_flux_local`).
- **À faire quand** : besoin de gen textures fréquent (Phase 2 props + terrain, ou Phase 3 polish).

### 3. Hunyuan3D-2 local — **T2**

- **Goal** : image 2D → mesh 3D `.glb`. Pour boss multi-phase visuels + castle decorative.
- **Source** : https://github.com/Tencent/Hunyuan3D-2 (MPS M1 compatible, ~10 GB model)
- **Install** :
  ```bash
  git clone https://github.com/Tencent/Hunyuan3D-2 ~/tools/Hunyuan3D-2
  cd ~/tools/Hunyuan3D-2 && uv pip install -r requirements.txt
  ```
- **À faire quand** : Phase 3 boss / castle visuels (assets uniques difficiles à modeler manuellement).

### 4. Stable Audio Open — **T2**

- **Goal** : SFX local (tower shoot, enemy death, hit, ambient) sans dépendance cloud.
- **Source** : https://github.com/Stability-AI/stable-audio-open (MPS, ~3 GB)
- **Install** :
  ```bash
  git clone https://github.com/Stability-AI/stable-audio-open ~/tools/stable-audio-open
  cd ~/tools/stable-audio-open && uv pip install -r requirements.txt
  ```
- **À faire quand** : Phase 3 polish audio sweep. **Alternative zero-install** : packs CC0 Kenney + freesound.org.

### 5. Mixamo auto-downloader — **T3**

- **Goal** : automate fetch d'anims humanoid (walk/run/attack/death) pour enemies Phase 3.
- **Source** : https://github.com/loveletter/mixamo-auto-downloader (ou fork actif)
- **Nécessite** : compte Adobe gratuit, login 1× par Mike.
- **À faire quand** : Phase 3 enemies animés.

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
- [x] **`/plan` formel Phase 1 POC** + 4 questions structurantes validées Mike (2026-05-11)
- [x] Phase 1 POC tickets MIGRATE-POC-01..08 (17 commits main + 1 gh-pages)
- [x] Phase 1 POC livré : W1-1 jouable WebGL build OK + push `gh-pages /v6/`
- [ ] **Mike : activer GitHub Pages** (Settings → Pages → Source = `gh-pages`, `/`)
- [ ] Phase 2 planning + tickets MIGRATE-CORE-XX

## ⚠️ Caveats & pièges pour la nouvelle session

| # | Piège | Conséquence | Mitigation |
|---|---|---|---|
| 1 | **`GITHUB_TOKEN` invalide hérité du shell parent Mike** | `gh` commands → HTTP 401 Bad credentials | `unset GITHUB_TOKEN` AVANT `claude` (cf section AVANT TOUT en haut). Workaround per-command : `unset GITHUB_TOKEN && gh ...` |
| 2 | **Specs D1 référencent paths `src-v3/...`** (44 occurrences à travers D1-01..04) | New Claude peut chercher dans `crowd-defense/src-v3/` → not found | Toujours préfixer mentalement avec `/Users/mike/Work/milan project/` (cf tableau "📂 SOURCE CODE À MIGRER") |
| 3 | **Repo `milan project/` doit rester sur disque** pendant toute la migration | Si supprimé → source Phaser à porter perdu (recoverable via clone GitHub mais friction) | Ne pas `rm -rf` `/Users/mike/Work/milan project/`. Archive frozen tag `v5.0-pre-pivot-unity` sur remote en backup. |
| 4 | **`.claude/qa/` non porté** dans crowd-defense (auto-qa-runner.mjs + scenarios + reports restent dans milan project) | Si new Claude veut sprint-gate via Chrome MCP scenarios → not found | À réécrire pour Unity context : `mcp__UnityMCP__run_play_mode` + `manage_scene` + `run_tests` au lieu de Chrome MCP. Pas urgent Phase 1. |
| 5 | **`.claude/settings.local.json` non porté** (permissions Bash spécifiques milan project) | New Claude aura prompts permission répétés pour `git add`, etc. | Re-run `/fewer-permission-prompts` skill une fois quelques sessions Bash accumulées dans crowd-defense |
| 6 | **`tools/gen_textures.py` (pipeline Flux Schnell)** reste dans `milan project/tools/` | Si new Claude veut generate textures via Flux → not found dans crowd-defense | Path absolu si besoin : `python3 /Users/mike/Work/milan project/tools/gen_textures.py`. Pipeline ComfyUI:8188. Cf memory `reference_flux_local`. |
| 7 | **subagent_type `general-purpose`** (built-in) ne lit pas les frontmatter `.claude/agents/` | Spawned agent défaut à Opus → tokens ×3-7 vs Sonnet | Toujours passer `model: "sonnet"` (exec) ou `"haiku"` (light search) en param Agent tool call quand subagent = general-purpose |
| 8 | **Unity Editor + MCP server crash possible** entre sessions | Tools `mcp__UnityMCP__*` fail | Relancer via `open -na "/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app" --args -projectPath /Users/mike/Work/crowd-defense` + 10s wait |

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
