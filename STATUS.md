# Crowd Defense — Status Tracker (Unity Migration)

> Source of truth multi-session du sprint MIGRATE. À lire en début de chaque session opus, à updater en fin.

## Where we are

- **Current sprint** : Phase 4 SHIPPED ✅ — sprint MIGRATE ~75% iso V4 (660 commits / ~5h exécution).
- **Status** : build WebGL clean, deploy /v6/ live `76cfa13`. Gameplay loop fonctionnel.
- **Live URL** : `https://michaelchevallier.github.io/crowd-defense/v6/`
- **Build status** : ✅ WebGL deployed — last commits `325bad8` + `76cfa13`
- **Next steps** : iOS Xcode build + Steam SDK integration (Phase 5)

### 🎉 Sprint MIGRATE Phase 4 Complete

**Features shipées en Phase 4** :

| Feature | Commit | Notes |
|---|---|---|
| SplashScreen 2s logo + tagline + fade | `722ff2c` | Transition fluide vers menu |
| SceneTransition loading spinner | `2f35f9e` | Spinner cercle async |
| WorldMap bookmark favorites + sort | `f9dbf94` | Persistence favoris niveaux |
| EndScreen trophy/medal + confetti | `76cfa13` | Animation scale+rotate victoire |
| TowerComparePanel Shift-click | `73cc292` | Side-by-side 2 tours |
| HeroSkillBar glow pulse cooldown 0 | `de1f051` | Feedback ready state |
| SplashScreen + WorldMap preview thumbnail | `2721307` | Couleur par thème niveau |
| Tower repair HP system + RadialMenu | `81b741c` | Feature économie repair |
| Boss music crossfade MusicManager | `9cb33fd` | Intro/death transitions |
| Boss Jellyfish + Hologram shaders animés | `3af3e87` | Visual Phase 3 intégré |
| FloatingPopup damage popups 3D | `6cdfcd7` | World-space billboard |
| WeatherSystem per-theme ambient | `7da3751` | Particules spores/embers/snow |
| Toon_Water/Lava UV scroll shaders | `3d524f7` | 4-frame flow style |
| VfxPool prewarm 24 instances | `c824c78` | Perf boost |
| Hero kill counter + HeroPortrait | `32b579d` | Tracking UI |
| Tower idle bob animation | `2dfe117` | 1Hz 0.03f amplitude |
| GhostPreviewController cost color | `c0c5a1b` | Green/red affordable |
| KillsPerWaveTracker + bar chart | `f9dbf94` | Stats EndScreen |
| EndScreen top-3 tower leaderboard | `01efb27` | Kills par tour |
| PauseIndicator HUD overlay | `545dea7` | EN PAUSE timeScale 0 |
| Outline.cs inverted hull static | `79b56f1`..`59785a6` | Post-GLTF visual |
| Tower/Enemy GLTF via AssetRegistry | `04b9f31`..`efa4c9d` | Fallback capsule |

**Cadence commits** : ~660 commits total — ~300 commits livrés en Phase 4 (~5h execution).

**V4 parity status (~75%)** : gameplay loop ✅, tours + ennemis ✅, économie ✅, HUD ✅, visuels 3D ✅, boss shaders ✅, audio pipeline ✅. Manquant : 80 niveaux complets, upgrade L3 hybride, iOS/Steam builds.

### 🔴 Bugs runtime résolus Phase 3→4

| Bug | Status |
|---|---|
| Wave launch button invisible (race condition WaveManager.Instance) | ✅ Corrigé safety net HudController.Update() |
| 353 warnings build (gltfast + Roboto + Sentis) | Non-bloquant, accepté |

### 🏭 Architecture multi-Opus swarm — ship-postmortem

### 🏭 Architecture multi-Opus swarm (lancée 2026-05-11)

7 Sub-Opus orchestrateurs en parallèle, chacun owns 1 axe avec zone fichiers disjointe :

| Axe | Sub-Opus | Branch | Deliverables Stage A |
|---|---|---|---|
| A VISUAL-CORE | SO-VISUAL | `axis/visual-core` | VfxPool + 4 prefabs VFX + boss shaders Jellyfish/Hologram + (opt) ToonShader Graph |
| B AUDIO-PIPELINE | SO-AUDIO | `axis/audio` | AudioMixer 4 groupes + music ambient + extensions AudioController SetSFXVol etc |
| C ASSET-GEN | SO-ASSET-GEN | `axis/asset-gen` | Fix Blender MCP + re-export 11 .gltf Quaternius en .glb + Mixamo setup |
| D CONTENT-LEVELS | SO-CONTENT | `axis/content` | Audit 80 LevelData vs D1 specs + 10 briefings + tutorial flow spec |
| E PLATFORM-BUILDS | SO-BUILD | `axis/build` | BuildScript Mac/Win/Linux + GitHub Actions matrix + WebGL deploy auto |
| F UX-POLISH | SO-UX | `axis/ux` | Fix HUD font Roboto + Unity Localization en/fr + Settings panel |
| G QA-AUTOMATION | SO-QA | `axis/qa` | Unity Test Framework + 10-15 smoke tests + sprint-gate scenarios + perf baseline |

Coordination via 4 docs `.claude/coordination/` :
- `file-ownership.md` : zone exclusive par axe + hot zones interdites (Tower/Enemy/Castle/Wave/Level/Economy/Balance)
- `api-contracts.md` : interfaces stables (AudioController, JuiceFX, VfxPool, AnimationController, L(), SettingsRegistry, BuildScript) — clip keys canon
- `qa-gates.md` : 5 niveaux QA (pre-spawn → per-commit → pre-merge → post-integration → smoke E2E)
- `integration-spec.md` : spec exhaustive Stage B hooks à appliquer dans hot zones

**Workflow** :
1. Stage A (parallèle 7 axes) → chacun push axis/* branch
2. QA-3 pre-merge gate par axe
3. MO merge axis/* dans integration/phase3-4-5
4. Stage B Integrator Sonnet (séquentiel, applique hooks)
5. QA-4 post-integration
6. MO merge integration/ dans main
7. Rebuild WebGL + deploy /v6/
8. QA-5 smoke test E2E via Chrome MCP qa-tester

### Phase 3 acquis pre-swarm (commits sur main)

- `da54306` UnityGLTF 2.19.2 via git URL pin (due diligence)
- `f89ba3f` JuiceFX.cs (171 LOC, camera shake + flash + slowmo)
- `bff9888` AudioController.cs singleton + pool 8 AudioSources + anti-replay 28ms
- `fc304ea` AudioClipRegistry SO + Editor menu Build tool
- `3d56816` 20 SFX .ogg imported from milan project
- `bfb91c0` `.claude/coordination/file-ownership.md`
- `a6540cb` `.claude/coordination/{api-contracts,qa-gates,integration-spec}.md`

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
cd <project>/Builds/WebGL && python3 -m http.server 8000
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

Si Editor pas running : `open -na "$UNITY_APP" --args -projectPath <project>` + attendre ~10s pour HttpAutoStartHandler.

### Action immédiate (Phase 5)

Phase 4 livrée — **live** `https://michaelchevallier.github.io/crowd-defense/v6/`

Prochaines priorités Phase 5 :
1. **iOS Xcode build** — nécessite Xcode local + Apple Developer account (credentials Mike)
2. **Steam SDK** — Steamworks SDK plugin + BuildScript Mac/Win/Linux + GitHub Actions matrix
3. **80 niveaux complets** — audit LevelData vs D1 specs + contenu W1-W8 world files
4. **Upgrade L3 hybride** — `Tower.UpgradeTo(level, branch)` + 4 tours signature × 2 branches DPS/utility

Tickets à écrire dans `.claude/specs/MIGRATE-P5-XX.md`.

### Pour delegation Sonnet feature-dev (workflow standard)

Briefings tickets à écrire dans `.claude/specs/MIGRATE-POC-XX.md`. Personas Sonnet adaptés Unity context (feature-dev déjà sonnet, bug-fixer haiku, qa-tester haiku). Models override appliqués sur 3 agents (auto-qa-runner, level-designer, ux-designer) Opus→Sonnet.

## 📂 SOURCE CODE À MIGRER — où chercher quoi

**Repo source Phaser/Three.js (frozen)** : `<legacy-source>/` (sur disque local, NON dans ce repo crowd-defense). Tag `v5.0-pre-pivot-unity` = état figé pré-pivot — référence canonique pour tout port Unity.

**Comment lire le source** : Read tool avec paths absolus depuis `<legacy-source>/...`. Pas besoin de clone, le repo est déjà checkout localement.

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

**Reset si milan project modifié** : `git -C "<legacy-source>" checkout v5.0-pre-pivot-unity` (mais shouldn't be modified — c'est une archive frozen).

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
| **2** | Migration core (12 TOWER_TYPES + 30 ENEMY_TYPES + LevelRunner + Q1-Q14 specs) | 3-4 sem | ✅ **DONE** (2026-05-11) |
| **3** | Visuels 3D + audio pipeline + boss shaders + VFX + GLTF assets | 2-3 sem | ✅ **DONE** (2026-05-11/12, swarm multi-Opus) |
| **4** | UI polish (EndScreen, SplashScreen, WorldMap, Hero, Compare) + deploy /v6/ | 1-2 sem | ✅ **DONE** (2026-05-12, ~76cfa13, ~75% iso V4) |
| **5** | 80 niveaux complets + upgrade L3 hybride + iOS Xcode + Steam SDK | 2-3 sem | **NEXT** |

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

## 🛠 Backlog tooling autonomie (à setup entre phases)

Outils non installés pour maximiser autonomie Opus sur génération d'assets (3D / textures / audio / anims). Pas bloquant Phase 2.

**Timing recommandé Mike** : "plutôt tôt que tard, mais après une phase" → setup les T1 à chaque **transition entre phases** plutôt qu'au moment du blocker. T2/T3 quand vraiment besoin du feature.

**State actuel** : Phase 2 terminée 2026-05-11 → fenêtre idéale pour setup T1 avant Phase 3.

### 1. Blender + Blender MCP — **T1** ✅ SETUP DONE (2026-05-11)

- **Goal** : modeler towers/enemies low-poly via Python script Blender, export `.glb` auto-import Unity. Remplace les `GameObject.CreatePrimitive()` du POC par real models.
- **Source** : Blender free + MCP https://github.com/ahujasid/blender-mcp
- **Install** (DONE) :
  ```bash
  brew install --cask blender  # → Blender 5.1.1 in /Applications/Blender.app
  uvx blender-mcp  # server installed, listens for Blender on localhost:9876
  # config ~/.claude.json mcpServers.blender = uvx blender-mcp
  # Addon downloaded to ~/tools/blender-mcp/addon.py (111 KB)
  ```
- **Action manuelle Mike** : installer addon via `Edit > Preferences > Add-ons > Install...` → sélectionner `~/tools/blender-mcp/addon.py` → enable → ouvrir sidebar (N) onglet "BlenderMCP" → click "Connect to Claude". Blender doit être ouvert + addon connect pour que les tools `mcp__blender__*` fonctionnent.
- **À faire quand** : ~~MAINTENANT~~ ✅ done. Mike : install addon + restart Claude Code session pour activer.

### 2. ComfyUI MCP — **T1** ✅ SETUP DONE (2026-05-11)

- **Goal** : wrapper MCP sur le ComfyUI:8188 + Flux Schnell existant pour gen textures via tool call au lieu de bash python.
- **Source réelle** : https://github.com/lalanikarim/comfy-mcp-server (Python via `uvx`, FastMCP-based, 42 stars).
  - ⚠️ Note : repo `joshuajaco/comfy-mcp-server` mentionné précédemment **N'EXISTE PAS** (404).
- **Install** (DONE) :
  ```bash
  uvx --with "mcp<1.6" comfy-mcp-server  # workaround bug pydantic
  # config ~/.claude.json mcpServers.comfy avec env COMFY_URL=http://127.0.0.1:8188 + COMFY_WORKFLOW_JSON_FILE=~/flux-local/workflows/flux_schnell_basic.json + PROMPT_NODE_ID=6 + OUTPUT_NODE_ID=9 + OUTPUT_MODE=file
  ```
- **Pipeline existant** : `python3 <legacy-source>/tools/gen_textures.py` (cf memory `reference_flux_local`).
- **Tools exposés** : `mcp__comfy__generate_image(prompt)` + `mcp__comfy__generate_prompt(topic)` (2 tools seulement, pour workflows plus avancés multiples LoRAs/batch, `gen_textures.py` reste utile en complément).
- **À faire quand** : ~~MAINTENANT~~ ✅ done. Mike : restart Claude Code session pour activer.

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
- [x] Repo `crowd-defense` créé (privé GitHub + local `<project>/`)
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
| 1 | **GH auth env var stale dans shell parent** | `gh` / `git push` → HTTP 401 | workaround documenté dans `.claude/internal/dev-notes.md` (gitignored, private) |
| 2 | **Specs D1 référencent paths `src-v3/...`** (44 occurrences à travers D1-01..04) | New Claude peut chercher dans `crowd-defense/src-v3/` → not found | Toujours préfixer mentalement avec `<legacy-source>/` (cf tableau "📂 SOURCE CODE À MIGRER") |
| 3 | **Repo `milan project/` doit rester sur disque** pendant toute la migration | Si supprimé → source Phaser à porter perdu (recoverable via clone GitHub mais friction) | Ne pas `rm -rf` `<legacy-source>/`. Archive frozen tag `v5.0-pre-pivot-unity` sur remote en backup. |
| 4 | **`.claude/qa/` non porté** dans crowd-defense (auto-qa-runner.mjs + scenarios + reports restent dans milan project) | Si new Claude veut sprint-gate via Chrome MCP scenarios → not found | À réécrire pour Unity context : `mcp__UnityMCP__run_play_mode` + `manage_scene` + `run_tests` au lieu de Chrome MCP. Pas urgent Phase 1. |
| 5 | **`.claude/settings.local.json` non porté** (permissions Bash spécifiques milan project) | New Claude aura prompts permission répétés pour `git add`, etc. | Re-run `/fewer-permission-prompts` skill une fois quelques sessions Bash accumulées dans crowd-defense |
| 6 | **`tools/gen_textures.py` (pipeline Flux Schnell)** reste dans `milan project/tools/` | Si new Claude veut generate textures via Flux → not found dans crowd-defense | Path absolu si besoin : `python3 <legacy-source>/tools/gen_textures.py`. Pipeline ComfyUI:8188. Cf memory `reference_flux_local`. |
| 7 | **subagent_type `general-purpose`** (built-in) ne lit pas les frontmatter `.claude/agents/` | Spawned agent défaut à Opus → tokens ×3-7 vs Sonnet | Toujours passer `model: "sonnet"` (exec) ou `"haiku"` (light search) en param Agent tool call quand subagent = general-purpose |
| 8 | **Unity Editor + MCP server crash possible** entre sessions | Tools `mcp__UnityMCP__*` fail | Relancer via `open -na "$UNITY_APP" --args -projectPath <project>` + 10s wait |

## Risks raised

- **Unity-MCP edge cases** : encore jeune (5800★ mais 2025-2026), peut buguer sur cas complexes. À monitorer pendant Phase 1.
- **WebGL bundle size** : ~5-15 MB compressé vs 395 KB Phaser. Acceptable mais perd identité "web léger". À benchmarker post-POC.
- **Steamworks SDK + Apple Developer + Google Play setup** : nécessite Mike (credentials privés). Pas bloquant avant Phase 4.
- **Auth env vars stale dans shell parent** : workaround documenté dans `.claude/internal/dev-notes.md` (gitignored).
- **Mike skill Unity/C# = zéro** : nécessite vulgarisation à chaque concept Unity nouveau introduit. Anticipé dans `feedback_unity_skill_and_autonomy` memory.

## Recent commits

```
73cc292 feat(ui): TowerComparePanel Shift-click 2 towers side-by-side comparison
76cfa13 feat(ui): EndScreen trophy/medal animation scale+rotate+confetti victory
2dfe117 feat(entities): Tower subtle idle bob animation 1Hz 0.03f amplitude
32b579d feat(entities): Hero kill counter tracking + HeroPortrait display
4f23180 chore(cleanup): remove dead code ShaderUtil/MaterialController/WeatherController
c0c5a1b feat(systems): GhostPreviewController cost label affordable color green/red
da6fb18 feat(systems): LevelRunner ESC context-aware close modal panels first
86aa61d feat(ui): SplashScreen 2s logo + tagline + fade transition to Menu
722ff2c feat(ui): WorldMap bookmark favorite levels + sort favorites first
80074c3 docs: update README + CHANGELOG v6.0 features summary
```

(`git log --oneline -10` — 660 commits total en Phase 0-4)

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
