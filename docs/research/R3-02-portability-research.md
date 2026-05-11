# R3-02 — Portability Research : Three.js vs Unity vs Godot vs PlayCanvas vs Bevy

> Sprint R3, livrable 2/2. Recherche industrie pure. Décision Mike post-lecture.
> Question Mike : "Le jeu devient de plus en plus complexe est-ce qu'un moteur comme Unity surtout avec le unity-mcp serait pas plus approprié ? Notamment si on veut commencer a déployer un peu ?"

## TL;DR (lecture 30 sec)

- **Unity-MCP existe**, mature, 5800★, 34+ tools (`manage_scriptable_object` inclus), MIT, Unity 2021.3 LTS+.
- **Godot-MCP existe** en plusieurs implémentations (Coding-Solo/Dokujaa/Claude-GoDot-MCP avec 170 tools).
- **PlayCanvas-MCP officiel existe** (`playcanvas/editor-mcp-server`).
- **Bevy-MCP existe** (`bevy_brp_mcp`, debugger-oriented).
- État actuel Milan : Three.js + Vite + JS vanilla, ~20 372 LOC dans `src-v3/`, 98 levels, 5 entities (BuildPoint, Castle, Enemy, Hero, Tower), bundle ~395 KB gz.
- **Gagnant matrix bruts** : PlayCanvas (31/40) > Three.js (30/40) > Godot (29/40) > Unity (27/40) > Bevy (20/40). Matrix non pondérée — la pondération bouleverse l'ordre.
- **Coût migration domine** : Three.js statu quo = 0 h (R3-01 tooling = 80-120 h), PlayCanvas = 240-400 h, Godot = 320-480 h, Unity = 480-720 h, Bevy = 800-1200 h.

---

## Contexte projet (état des lieux)

- Stack actuel : Three.js + Vite + ESM + JS vanilla (zéro TypeScript).
- Codebase : ~20 372 LOC JS dans `src-v3/`.
- Entities principales : `Tower.js`, `Enemy.js`, `Castle.js`, `Hero.js`, `BuildPoint.js`.
- Levels : 98 fichiers JSON-like dans `src-v3/data/levels/`.
- Bundle output : ~395 KB gzipped, web-only, hébergé GitHub Pages.
- AI tooling actuel : Claude Code + Edit/Read tools (filesystem-direct, pas de MCP custom Milan-side).
- Validators existants : `scripts/validate-maps.mjs`, `scripts/balance-check.mjs` (Node CLI).
- Audience cible : usage personnel + Milan + démos amis. Pas de monétisation à court terme.

Plan stratégique en cours : 8 semaines pour refonte "vrai jeu stratégique" (R1→R2→D1+R3→D2→E1→E2). Décision R3-02 portabilité peut **réorienter le plan entier** si Mike choisit migration.

---

## Option 1 — Continuer Three.js + tooling custom (statu quo + R3-01)

### 1.1 Description

Three.js est une **librairie de rendu 3D bas niveau** (pas un game engine complet). Elle expose scene graph, cameras, lights, materials, renderer WebGL/WebGPU, puis sort du chemin ([Three.js docs](https://threejs.org/)). Tout le reste (scenes, input, physics, audio, UI, level editor) est custom code, custom build.

**Langage** : JS vanilla + ESM. **Public cible** : devs web full-stack qui veulent contrôle total. **Paradigme** : code-first, no editor, no opinions.

Three.js domine l'écosystème WebGL avec **5 millions de downloads hebdomadaires** ([utsubo.com benchmark 2026](https://www.utsubo.com/blog/threejs-vs-babylonjs-vs-playcanvas-comparison)) — 300× plus que Babylon.js ou PlayCanvas.

### 1.2 Effort de migration

**Zéro** — c'est l'état actuel. Coût implicite : continuer à coder tools custom (cf R3-01 = 80-120 h pour 3-5 outils).

### 1.3 Outils inclus

**Rien** dans Three.js stock. Pas de scene editor (alt: Three.js Editor détaché), pas d'Inspector (alt: lil-gui/dat.GUI/Tweakpane runtime), pas de Prefab natif, pas de Tilemap (alt: LDtk export JSON), pas d'Animation graph (alt: AnimationMixer code-only), pas d'asset pipeline (alt: GLTF + custom Vite plugin). R3-01 vise à construire 3-5 outils maison.

### 1.4 Déploiement

Web-only natively : HTML5 + WebGL/WebGPU, bundle ~395 KB gz = <2 s sur 4G ([utsubo](https://www.utsubo.com/blog/threejs-vs-unity-web-comparison)). Mobile native : wrap **Capacitor (Ionic)** ou Cordova legacy ([techtarget 2026](https://www.techtarget.com/searchmobilecomputing/news/252496257/Ionic-Capacitor-emerging-as-successor-to-Cordova)) — Three.js + GLTF **fonctionne dans Capacitor** ([discussion #5562](https://github.com/ionic-team/capacitor/discussions/5562)). Desktop : Electron/Tauri wrap (+30-80 MB). Steam : possible via Electron, $100 Direct fee récupérable après $1k revenus ([Steamworks](https://partner.steamgames.com/doc/gettingstarted/appfee)). iOS Apple Dev $99/an ([Apple Dev](https://developer.apple.com/programs/whats-included/)). Android Google Play $25 one-time.

### 1.5 AI tooling (MCP)

**Pas de MCP dédié Three.js officiel** — mais Claude Code via Edit/Read tools sur filesystem fonctionne très bien sur JS files (workflow actuel Milan = preuve par fait).

**Avantage** : zéro friction, contexte 1M tokens lit tout `src-v3/` en une passe.
**Limite** : pas de "run play mode + inspect runtime state". Pour debug runtime, Mike doit ouvrir DevTools.

R3-01 pourrait inclure un MCP custom Milan (hot-reload tweak, inspect runtime, snapshot scene) — ~40-80 h supplémentaires.

### 1.6 Performance

- **Bundle Three.js core tree-shaké** : 500 KB - 1 MB ([utsubo](https://www.utsubo.com/blog/threejs-vs-unity-web-comparison)). Milan actuel = 395 KB gz.
- **Load time** : 2-6 s contre 8-30 s Unity WebGL.
- **60 FPS** : tenu sur Milan actuel avec <100 draw calls ([utsubo 100 tips](https://www.utsubo.com/blog/threejs-best-practices-100-tips)).
- **Mobile** : 395 KB gz = négligeable sur 4G/5G. Aucune limite Safari memory ceiling.

### 1.7 Communauté

- **5M downloads/semaine npm** — écosystème massif.
- StackOverflow : ~20k+ tagged questions.
- Discord/forum officiels actifs.
- Tutos : 1000s sur YouTube, Three.js Journey, Bruno Simon, etc.
- Courbe solo dev : déjà maîtrisée par Mike (preuve par fait).

### 1.8 Limites + risques

- **Tout custom = dette tech long terme** : chaque feature engine-level (physics, animation graph, scene editor) à construire ou importer librairie.
- **Pas d'éditeur visuel** : level design 100% ASCII grid + JSON. R3-01 doit livrer un level editor sinon scaling levels = bloquant.
- **Mobile certifié** : Capacitor ajoute couche WebView (perf -10-20 %).
- **Steam acceptation** : possible mais "HTML5 wrapped in Electron" perçu comme amateur par certains éditeurs.
- **Avantage caché** : Mike est productif sur le stack actuel, pas de switch cost.

---

## Option 2 — Migrate vers Unity (avec Unity-MCP)

### 2.1 Description

Unity est **the industry-standard 2D/3D game engine** depuis 2005. Multi-platform native (iOS, Android, Win, Mac, Linux, WebGL, consoles, VR). Langage principal : **C#**. Asset Store massive. Inspector + Prefab + ScriptableObject + Animator + Tilemap natifs.

**Paradigme** : composant-based (MonoBehaviour) + ScriptableObject (data assets). **Public cible** : indie à AAA. Référence pour TD 2D ([Udemy course "Learn 2D Tower Defense in Unity"](https://www.udemy.com/course/learn-how-to-create-a-2d-tower-defense-game-in-unity-2020/)).

### 2.2 Effort de migration

**Estimation 480-720 h** (12-18 semaines à 40 h/sem solo dev) :

| Sous-tâche | Heures | Détail |
|---|---|---|
| Rewriting render pipeline | 80-120 | Three.js scene → Unity GameObjects + sprites/meshes |
| Rewriting entities | 100-160 | Tower/Enemy/Castle/Hero/BuildPoint → MonoBehaviour + ScriptableObject |
| Rewriting data (TOWER_TYPES, ENEMY_TYPES) | 60-100 | JS objects → ScriptableObject assets (un asset par tower/enemy) |
| Rewriting 98 levels | 80-120 | JSON-like JS → Unity Scenes ou Tilemap+JSON |
| Rewriting UI/HUD | 60-100 | index.html DOM → Unity UI (Canvas + UI Toolkit) |
| Asset pipeline | 40-80 | Imports textures, audio, GLTF→FBX |
| Build chain + CI | 30-60 | Unity Cloud Build ou GitHub Actions |
| Tests + polish | 30-60 | QA scenarios Unity Test Framework |

**Impact sur plan 8 semaines en cours** : plan **annulé**. Migration = 12-18 nouvelles semaines + perte gains R1+R2 (recherche reste valable mais implémentations refaites).

### 2.3 Outils inclus

Cherry-pick : Scene Editor (drag-drop, transform handles 3D), Inspector (Custom editors via `[CustomEditor]`, drag-drop refs, [Unity docs](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)), **ScriptableObjects** parfaits pour TOWER_TYPES/ENEMY_TYPES ([Unity blog](https://unity.com/how-to/architect-game-code-scriptable-objects)), Prefabs + Variants (L1/L2/L3), Animator state machines, Tilemap + Tile Palette 2D natifs, Profiler built-in multi-platform, VFX Graph + Shader Graph, Asset Store 20k+, Unity Test Framework.

### 2.4 Déploiement

**Multi-platform natif** : iOS, Android, Win, macOS, Linux, WebGL, PS5, Xbox, Switch, VR (Quest, Vision Pro). Build chain = un bouton. Bundle WebGL : **5-15 MB initial, 25-50 MB final** ([utsubo](https://www.utsubo.com/blog/threejs-vs-unity-web-comparison)), **5-10× plus gros que Three.js**. Load time WebGL : 8-30 s vs 2-6 s Three.js.

**Licence 2026** ([Unity pricing](https://unity.com/products/pricing-updates)) : Personal free si revenus+funding <$200k/an (Mike éligible) ; Pro $2 310/seat/an (+5% 12 jan 2026) ; Enterprise $4-5k/seat/an >$25M. **Runtime Fee annulé** pour gaming (Unity 2024). Steam $100 Direct, iOS Apple Dev $99/an, Android Google Play $25.

### 2.5 AI tooling (MCP) — POINT CLÉ MIKE

**Unity-MCP existe en plusieurs implémentations matures** :

| Project | Stars | Approach | Notable |
|---|---|---|---|
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | 5 800★ | Python server + Unity Package | **34+ tools** incl. `manage_scriptable_object`, `batch_execute` |
| [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) | actif | Node.js MCP server | Cursor, Windsurf, Codex CLI, Antigravity, Copilot |
| [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | actif | AI Skills + CLI | "Any C# method → tool in 1 line" |
| [NoSpoonLab/unity-mcp](https://github.com/NoSpoonLab/unity-mcp) | actif | C# server | Server fully implemented in C# |
| [HuntNight/unity-mcp-advanced](https://github.com/HuntNight/unity-mcp-advanced) | actif | Cursor-focused | Hot Reload + scene analysis + C# execution |

**Capabilities CoplayDev/unity-mcp** ([WebFetch confirmed](https://github.com/CoplayDev/unity-mcp)) :
- `manage_gameobject`, `manage_scene`, `manage_asset`, `manage_script`, `manage_prefabs`, `manage_components`, `manage_material`, `manage_shader`, `manage_texture`
- `manage_animation`, `manage_camera`, `manage_physics` (21 actions), `manage_graphics`, `manage_probuilder`, `manage_vfx`, `manage_build`, `manage_packages`, `manage_editor`, `manage_ui`, `manage_scriptable_object`
- `manage_profiler` (14 actions : frame timing, memory snapshots, Frame Debugger)
- `unity_reflect`, `unity_docs`, `run_tests`, `read_console`
- `batch_execute` (10-100× plus rapide que loops)

**Architecture** : Python server local (3.10+) + Unity Editor package + MCP client (Claude Code/Desktop/Cursor/VS Code).

**Versions supportées** : Unity 2021.3 LTS → Unity 6. **MIT license**. ~700 forks.

**Limitations identifiées** :
- Editor-only : pas de runtime injection (par design — Unity ferme Editor en build).
- Localhost only (sécurité).
- "Use batch_execute pour 10-100× speedup" sur opérations multiples (overhead per-call non négligeable).
- Roslyn validation = NuGet package séparé.

**Vs Claude Code + Three.js custom workflow actuel** :
- Avantage MCP : "Modify scene + run play mode + inspect entity X" en un round-trip natif.
- Avantage Three.js : "Edit src/Tower.js ligne 145" est trivial via Edit tool, contexte 1M tokens lit tout.
- Verdict : MCP-Unity est **strictement supérieur pour itération visuelle** (placer mob, ajuster valeur, voir résultat en Editor). Three.js Edit reste meilleur pour **refacto de logique pure**.

### 2.6 Performance

- **WebGL bundle** : 5-15 MB initial, 25-50 MB final. **Critique pour web** : 3-10× load time vs Three.js.
- **iOS/Android natif** : Unity excellent, 60 FPS standard, optimisations IL2CPP.
- **Desktop natif** : Unity excellent.

→ **Si target principale = web, Unity perd**. **Si target = mobile + Steam, Unity gagne largement**.

### 2.7 Communauté + tutos

- StackOverflow : 200k+ tagged questions.
- Unity Learn : tutos officiels gratuits.
- YouTube : Brackeys (archived but viral), Code Monkey, GameDev.tv, Sebastian Lague.
- Asset Store : 20k+ assets (Unity Editor TD templates inclus).
- Courbe solo dev : **2-4 semaines pour productivité** si pas de C# préalable. Mike vient de JS = transition pas triviale mais faisable (C# = JS strict-typed à 70%).

### 2.8 Limites + risques

- **Bundle WebGL bloat** = killer pour web-only deploy.
- **Vendor lock-in** : ScriptableObjects, Prefabs = format Unity propriétaire. Migration future hors-Unity = douloureuse.
- **Pricing 2026** : changes annuelles fréquentes ([80.lv 2026 announcement](https://80.lv/articles/unity-announces-its-upcoming-2026-price-changes)). Risque hike futur (Runtime Fee 2023 = précédent inquiétant même si annulé).
- **C# learning curve** : Mike doit absorber C# + Unity paradigm en parallèle.
- **Migration coût** = 480-720 h = annule plan 8 semaines.
- **Mais** : si target = Steam + iOS + Android, Unity = chemin court terme. Si target = web-only = pas pertinent.

---

## Option 3 — Migrate vers Godot 4

### 3.1 Description

Godot 4 est un **engine open source complet** (MIT license), gratuit sans royalties. Multi-platform natif. Langages : **GDScript** (Python-like, intégré Editor) ou **C#** (.NET 8). Inspector + Scene + Node + Tilemap + Animator + Shader natifs.

**Paradigme** : Scene Tree + Nodes (héritage composition). **Public cible** : indie 2D/3D. En 2026 considéré "engine production-ready, plus underdog" ([dev.to 2026](https://dev.to/linou518/godot-vs-unity-in-2026-which-engine-should-indie-developers-choose-50g4)).

### 3.2 Effort de migration

**Estimation 320-480 h** (8-12 semaines solo dev) :

| Sous-tâche | Heures | Détail |
|---|---|---|
| Apprentissage Godot + GDScript | 40-80 | Mike pas familier. GDScript = "hours not days" learning ([Coding Quests](https://codingquests.io/blog/gdscript-vs-csharp-godot-4)) |
| Rewriting entities | 80-120 | Tower/Enemy → Node + Scene scenes |
| Rewriting data | 40-80 | TOWER_TYPES → Godot Resources (équivalent ScriptableObject) |
| Rewriting 98 levels | 80-120 | JS → Godot Scenes (.tscn) ou TileMap |
| UI/HUD | 40-60 | Control nodes Godot UI |
| Asset pipeline | 20-40 | Imports automatiques Godot |
| Build chain | 20-40 | Export presets Godot |

**Plus rapide qu'Unity** : GDScript learning curve plus douce, Scene Tree plus simple que Prefab Variants, **Resources** = ScriptableObject equivalent natif.

### 3.3 Outils inclus

Editor unifié ~80 MB exe (Scene + Script + Tilemap + Animator + Shader graph), Scene Tree (hierarchy + transform handles), Inspector drag-drop, **Resources** = ScriptableObject equivalent natif, **TileMap excellent pour TD grid-based** Milan, AnimationPlayer + AnimationTree state machines, Profiler + Debugger built-in, C# support moins poli que GDScript mais [Microsoft maintient `godot-csharp-essentials`](https://chickensoft.games/blog/gdscript-vs-csharp).

### 3.4 Déploiement

**Multi-platform natif** : iOS, Android, Win, macOS, Linux, Web, consoles via porteurs tiers (W4 Games). Web export : [WebGL 2.0 Compatibility mode](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html), bundle compressé **6-9 MB optimisé**, jusqu'à 19 MB unoptimisé ([popcar](https://popcar.bearblog.dev/how-to-minify-godots-build-size/), [amann.dev](https://amann.dev/blog/2025/godot_web_size/)). Mobile : iOS export natif + Android AAB/APK ([Android Devs Godot guide](https://developer.android.com/games/engines/godot/godot-export)). Steam $100, iOS $99, Android $25. **Bundle web 6-9 MB = pire que Three.js (395 KB), meilleur qu'Unity (25-50 MB)**.

### 3.5 AI tooling (MCP)

**Godot-MCP existe en plusieurs implémentations** :

| Project | Approach | Notable |
|---|---|---|
| [Coding-Solo/godot-mcp](https://github.com/Coding-Solo/godot-mcp) | Launch editor + run + debug | Bundled GDScript approach |
| [Dokujaa/Godot-MCP](https://github.com/Dokujaa/Godot-MCP) | Claude-focused | Asset Library listed |
| [Godot MCP Pro](https://godotengine.org/asset-library/asset/4961) | Cursor/Cline/Windsurf/Claude | Asset Library officielle |
| [DaRealDaHoodie/Claude-GoDot-MCP](https://github.com/DaRealDaHoodie/Claude-GoDot-MCP) | **170 tools** | "Real-time control over the Godot editor" |
| [LeeSinLiang/godot-mcp](https://github.com/LeeSinLiang/godot-mcp) | actif | Standard MCP server |
| [ee0pdt/Godot-MCP](https://github.com/ee0pdt/Godot-MCP) | Create + edit games | Claude integration |

**Capabilities Coding-Solo/godot-mcp** ([WebFetch confirmed](https://github.com/Coding-Solo/godot-mcp)) :
- Launch Godot Editor, run projects debug mode, start/stop execution.
- Console output retrieval, error messages capture.
- Scene editing : create scenes, add nodes, load sprites/textures, mesh library export.
- UID management Godot 4.4+.
- **Limitation** : "Script editing isn't directly supported" via tools — bundled GDScript script approach.

**Verdict vs Three.js** : ressources plus jeunes que Unity-MCP (moins de stars, moins de tools), mais **Claude-GoDot-MCP (170 tools)** annoncé comme comprehensive. Maturité intermédiaire entre Unity-MCP et PlayCanvas-MCP.

### 3.6 Performance

- **Bundle Web** : 6-9 MB optimisé, **6× plus gros que Three.js**, **3-5× plus petit qu'Unity WebGL**.
- **Startup time** : historiquement 10+ s WASM compile, amélioré Godot 4.x mais reste lent.
- **60 FPS** : tenu sur 2D TD standards. Pas de benchmark spécifique Milan-like trouvé.
- **Mobile** : exports légers vs Unity, 10-20 MB APK typique.

### 3.7 Communauté + tutos

- **22e engine populaire 2026** (vs Unity #1) mais croissance rapide post-Unity-runtime-fee 2023.
- Discord officiel actif (200k+ membres).
- StackOverflow : ~5k tagged questions (10× moins qu'Unity).
- YouTube : moins de tutos qu'Unity mais HeartBeast, GDQuest, Brackeys (post-pivot Godot) productifs.
- Courbe solo dev : "**zero to functional in hours, not days**" pour GDScript ([Coding Quests](https://codingquests.io/blog/gdscript-vs-csharp-godot-4)).

### 3.8 Limites + risques

- **Bundle web 6-9 MB** = encore lourd vs Three.js.
- **Écosystème plus petit** : moins d'assets, moins de docs, moins de StackOverflow.
- **C# integration moins polie** : si Mike préfère C# à GDScript.
- **GDScript = vendor lock-in** : pas portable hors Godot.
- **Mais** : MIT, gratuit, pas de royalties, pas de licence fee. Indépendance long terme.
- Cas TD réels : [quiver-dev/tower-defense-godot4](https://github.com/quiver-dev/tower-defense-godot4) template officiel + [Dampf "Cozy Tower Defense" 3D](https://forum.godotengine.org/t/thank-you-godot-i-created-a-stylistic-3d-tower-defense-in-godot-dampf-the-cozy-tower-defense/52745) shipped game.

---

## Option 4 — Migrate vers PlayCanvas

### 4.1 Description

PlayCanvas est un **engine WebGL/WebGPU open source avec Editor cloud collaboratif**. "Like Figma for 3D" ([utsubo](https://www.utsubo.com/blog/threejs-vs-babylonjs-vs-playcanvas-comparison)). Langage : **JavaScript** (script components). Engine open source, Editor SaaS (free tier + paid).

**Paradigme** : Entity-Component + scripts JS. **Public cible** : équipes web 3D, jeux mobile/messaging (Snapchat AR, WhatsApp games). **Famille** : web-first comme Three.js mais avec editor.

### 4.2 Effort de migration

**Estimation 240-400 h** (6-10 semaines solo dev) :

| Sous-tâche | Heures | Détail |
|---|---|---|
| Apprentissage PlayCanvas Editor + workflows | 20-40 | Cloud editor, conventions |
| Rewriting entities | 60-100 | Three.js classes → PlayCanvas script components |
| Rewriting data | 30-60 | TOWER_TYPES → PlayCanvas assets (JSON) ou enum scripts |
| Rewriting 98 levels | 80-120 | JS → PlayCanvas Scenes (cloud editor) |
| UI/HUD | 40-60 | PlayCanvas UI (HTML overlay ou ScreenComponent) |
| Asset pipeline | 10-20 | Drag-drop dans editor cloud |

**Le plus court chemin pour rester web-first** : pas de switch langage (JS reste).

### 4.3 Outils inclus

Editor cloud ([playcanvas.com/products/editor](https://playcanvas.com/products/editor)) : Scene + Inspector + Asset manager, real-time collab Figma-style, drag-drop refs, **built-in physics, audio, asset management, animation** ([utsubo](https://www.utsubo.com/blog/threejs-vs-babylonjs-vs-playcanvas-comparison)), hot reload mobile (test on device, live update code+assets).

### 4.4 Déploiement

Web natif optimisé (bundle compétitif Three.js). Mobile via Cordova/Capacitor wrap **+ mobile social/messaging clients (Twitter, WhatsApp, Snap)** — différenciant. Desktop via Electron/Tauri. Steam $100 Direct, iOS Apple Dev $99, Android $25.

**Pricing PlayCanvas 2026** ([playcanvas.com/plans](https://playcanvas.com/plans)) : Free $0 (1GB storage, unlimited **public** projects, REST API) ; Personal $15/mois (10GB, unlimited **private** projects) ; Organization $50/seat/mois (50GB/seat, team mgmt). Pour Mike solo : $15/mois si projet privé, **gratuit** si projet public.

### 4.5 AI tooling (MCP)

**MCP officiel maintenu par PlayCanvas eux-mêmes** :
- [`playcanvas/editor-mcp-server`](https://github.com/playcanvas/editor-mcp-server) — **endorsement officiel** (équipe core).
- Architecture : MCP server + **Chrome Extension** (WebSocket) qui parle au PlayCanvas Editor browser-side.
- "Create a sphere" → API calls → Editor.
- Driven par Claude Desktop (free tier insuffisant ; Pro recommandé).

**Verdict** : MCP officiel = signal fort de maturité. Différence vs Unity-MCP/Godot-MCP : intégration Editor cloud via browser extension (Editor étant dans le navigateur).

### 4.6 Performance

- **Bundle** : comparable Three.js (proche du métal WebGL).
- **Mobile** : "**PlayCanvas frequently outperforms** the others on mobile frame rate" ([utsubo](https://www.utsubo.com/blog/threejs-vs-babylonjs-vs-playcanvas-comparison)).
- **Load time** : 2-4 s typique.

### 4.7 Communauté + tutos

- Écosystème plus petit que Three.js (5M dl/sem) ou Unity, mais **niche web 3D** consolidée.
- Docs officielles solides.
- Forum officiel actif.
- Snapchat/Meta/Mozilla l'utilisent en prod (cas industriels).

### 4.8 Limites + risques

- **Editor cloud-only** = dépendance serveur PlayCanvas (offline impossible sans projets téléchargés).
- **Niche plus petite** = moins de tutos qu'Unity/Godot.
- **JS only** = pas de C#/Rust si préférence performance.
- **Vendor lock-in soft** : engine open source mais Editor proprio. Export du runtime possible.
- **Pricing $15/mois** pour projets privés = micro mais non-zero.
- **Avantage caché** : Mike reste 100% JS, courbe d'apprentissage la plus courte des migrations.

---

## Option 5 — Migrate vers Bevy (Rust)

### 5.1 Description

Bevy est un **engine open source ECS pur en Rust**, MIT/Apache 2.0. "A refreshingly simple data-driven game engine" ([bevyengine.org](https://www.bevyengine.org/)).

**Langage** : **Rust**. **Paradigme** : ECS (Entity Component System) pur, code-only, no editor (en cours dans roadmap).

**Public cible** : devs Rust qui veulent performance + safety. État 2026 : v0.18 ([Bevy 0.18 release](https://bevy.org/news/bevy-0-18/)) + v0.18.1 patch ([Prism News mars 2026](https://www.prismnews.com/news/bevy-releases-v0181-to-stabilize-018-fix-regressions)).

### 5.2 Effort de migration

**Estimation 800-1200 h** (20-30 semaines solo dev) :

| Sous-tâche | Heures | Détail |
|---|---|---|
| **Apprentissage Rust** | 200-400 | Si Mike pas familier. Ownership/borrow checker = courbe lourde. |
| Apprentissage Bevy ECS | 80-160 | Paradigm shift OOP→ECS |
| Rewriting entities en ECS | 200-300 | Tower/Enemy/Castle → Components + Systems |
| Rewriting data | 80-120 | TOWER_TYPES → Bevy Resources (data assets) |
| Rewriting 98 levels | 100-150 | Pas de scene editor → JSON Bevy + code |
| UI/HUD | 80-120 | bevy_ui (jeune) ou egui crate |
| Asset pipeline | 40-80 | bevy_asset ecosystem |

**Le plus long chemin**. Coût d'apprentissage Rust dominant.

### 5.3 Outils inclus

No native editor (bevy_editor 2026 WIP), ECS pur code-first, hot reload stable depuis 0.18 ([Medium 2026](https://medium.com/solo-devs/bevy-in-2025-rusts-game-engine-taking-over-indie-dev-caec2ae50c09)), pas de Prefab/Animator/Tilemap natifs (alts: bevy scenes, bevy_animation, bevy_ecs_tilemap crates). Bevy = **le plus proche philosophiquement de Three.js (code-only)** mais bien plus lourd en boilerplate Rust.

### 5.4 Déploiement

Multi-platform : Web (WASM ~5-20 MB), Win/Mac/Linux natifs (Rust binaires, perf top), iOS/Android supportés en 2026 ([Aarambh Dev 2026](https://aarambhdevhub.medium.com/rust-game-engines-in-2026-bevy-vs-macroquad-vs-ggez-vs-fyrox-which-one-should-you-actually-use-9bf93669e83f)), Steam natif via cargo. Indie shipped : **Toroban** (puzzle wrapping) sur Steam (Bevy 0.18 notes).

### 5.5 AI tooling (MCP)

**Bevy-MCP existe** :
- [`bevy_brp_mcp`](https://lib.rs/crates/bevy_brp_mcp) (natepiano) : MCP via **Bevy Remote Protocol (BRP)** — inspect, launch, mutate apps.
- [`bevy_debugger_mcp`](https://docs.rs/bevy_debugger_mcp/) (Ladvien) : debugger AI-assisted, monitor entities, component changes real-time.
- [`Nub/bevy_mcp`](https://github.com/Nub/bevy_mcp) : MCP server for BRP.

**Status** : unifié dans `bevy_brp` workspace en mars 2026.

**Capabilities** : launch app, inspect entities, monitor components, mutate runtime state. **Debugger-oriented** (vs Unity-MCP "build/edit-oriented") car Bevy n'a pas d'Editor.

### 5.6 Performance

- **ECS** : "scales to millions of entities with zero overhead" ([bevyengine.org](https://www.bevyengine.org/)).
- **Native** : Rust = top-tier performance.
- **WASM web** : load time + bundle compétitif Unity WebGL mais pas Three.js.

### 5.7 Communauté + tutos

- **Most popular Rust engine** mais écosystème global beaucoup plus petit qu'Unity/Godot.
- StackOverflow : ~1k tagged questions.
- Discord actif (~30k membres).
- "This Week in Bevy" newsletter hebdo.
- Courbe solo dev : **lourde** si Rust pas connu. Bevy 0.x = breaking changes fréquents.

### 5.8 Limites + risques

- **Pre-1.0** : "not ready for any form of stabilization yet" — breaking changes à chaque version mineure ([GitHub discussion #9789](https://github.com/bevyengine/bevy/discussions/9789)).
- **Rust learning curve** : 3-6 mois pour devenir productif sans expérience préalable.
- **Pas d'editor mature** : level design en code = douloureux pour 98 levels.
- **Indie shipped games rare** mais croissants (Toroban prouve la voie).
- **Avantage caché** : performance native top-tier + zéro royalties + open source pur. Long terme idéal si target perf.

---

## Decision Matrix (8 critères, 5 options, score 1-5)

Score : 1 = très défavorable, 5 = très favorable. **Plus haut = meilleur pour Milan CD V3**.

| Critère | Three.js | Unity | Godot 4 | PlayCanvas | Bevy |
|---|---|---|---|---|---|
| **1. Effort migration** (5=zéro) | 5 | 1 | 2 | 3 | 1 |
| **2. Outils inclus** (5=tout natif) | 1 | 5 | 4 | 4 | 1 |
| **3. Déploiement multi-platform** | 2 | 5 | 5 | 3 | 4 |
| **4. AI tooling (MCP) maturité** | 2 | 5 | 4 | 4 | 3 |
| **5. Performance web bundle** (5=plus léger) | 5 | 1 | 3 | 5 | 3 |
| **6. Communauté/tutos** | 5 | 5 | 4 | 3 | 2 |
| **7. Langage familiarité Mike** (JS) | 5 | 2 | 2 | 5 | 1 |
| **8. Licence/coût long terme** | 5 | 3 | 5 | 4 | 5 |
| **TOTAL /40** | **30** | **27** | **29** | **31** | **20** |

### Lecture matrix

- **PlayCanvas (31/40)** : score brut le plus élevé. Web-first + Editor + MCP officiel + JS-only + bundle léger. Loose sur tutos communauté.
- **Three.js (30/40)** : très proche, gagne sur effort migration (0) et perf bundle.
- **Godot 4 (29/40)** : gagne sur licence + déploiement, perd sur langage (GDScript) + effort.
- **Unity (27/40)** : gagne sur outils + MCP + déploiement + tutos, **perd massivement sur effort migration + bundle web + licence**.
- **Bevy (20/40)** : perd sur quasi tout sauf perf native + licence. Bon pour devs Rust convaincus, pas pour Milan.

### Pondération possible

Si Mike pondère "déploiement mobile/Steam" ×3 : Unity passe à 35 (vs 27 base), Godot 32. Three.js et PlayCanvas restent compétitifs.
Si Mike pondère "effort migration" ×3 : Three.js domine largement (40 vs 27 Unity).

---

## Coût de migration depuis Milan actuel (synthèse)

| Option | Heures estimées | Semaines (40h/sem) | Impact plan 8 sem |
|---|---|---|---|
| Three.js statu quo + R3-01 tooling | 80-120 h (tooling only) | 2-3 sem | **Plan préservé**, sprint TE inséré |
| Unity | 480-720 h | 12-18 sem | **Plan annulé**, redéfini en plan migration |
| Godot 4 | 320-480 h | 8-12 sem | **Plan annulé**, redéfini |
| PlayCanvas | 240-400 h | 6-10 sem | **Plan partiellement annulé**, certaines pieces R1+R2 réutilisables |
| Bevy | 800-1200 h | 20-30 sem | **Plan annulé**, refonte longue durée |

**Référence** : 20 372 LOC src-v3 actuel + 98 levels. Estimations basées sur 70-150 LOC/heure migration (vs 40-80 LOC/heure réécriture from scratch).

---

## Recommandations décisionnelles pour Mike (descriptif)

### Three.js statu quo + R3-01

Garder Three.js + Vite + JS vanilla signifie capitaliser sur 20 372 LOC déjà productifs et le workflow Claude Code mature. Le plan 8 semaines est préservé : sprint TE insère 3-5 outils custom (level editor, tower/enemy editor, balance dashboard, validator GUI) pour 80-120 h supplémentaires. Bundle reste 395 KB gz, perf web-first imbattable. **Risque principal** : tout reste custom à long terme, mobile/Steam = Capacitor/Electron wraps avec perf -10-20 %. Pas de MCP natif au moteur — Claude Code Edit/Read sur filesystem suffit pour refacto logique mais pas pour itération visuelle "run play mode + inspect". Si Milan reste un projet web personnel + démos amis sans ambition Steam/iOS, c'est l'option la plus rentable.

### Unity

Unity offre l'écosystème le plus mature (5800★ unity-mcp, 34+ MCP tools incl. `manage_scriptable_object`, `batch_execute`, ScriptableObjects natifs parfaits pour TOWER_TYPES/ENEMY_TYPES, Tilemap + Animator + Profiler intégrés, multi-platform natif Steam/iOS/Android/console). Migration coûte 480-720 h (12-18 sem) — **annule le plan 8 semaines en cours**. Bundle WebGL 5-15 MB initial, 25-50 MB final = 3-10× pire que Three.js sur web. Licence Personal gratuite tant que revenus <$200k/an, Pro $2 310/seat/an au-delà. Apprentissage C# requis (~2-4 sem). **Profil idéal** : Mike veut shipping Steam + iOS + Android, prêt à perdre 3-4 mois pour gagner outils premium et déploiement natif. Si target reste web-only, Unity sur-engineering.

### Godot 4

Godot 4 combine open source MIT (zéro royalties, zéro licence), Editor mature, Resources natives (équivalent ScriptableObjects), TileMap excellent pour TD grid-based, GDScript courbe douce ("hours not days"), exports natifs iOS/Android/Steam/Win/Mac/Linux. Migration 320-480 h (8-12 sem) — **annule plan 8 sem**. Plusieurs MCP servers existent (Coding-Solo, Dokujaa, Claude-GoDot-MCP 170 tools). Bundle web 6-9 MB optimisé = pire que Three.js, meilleur qu'Unity. Apprentissage GDScript rapide pour Mike (JS-like). **Profil idéal** : Mike veut shipping multi-platform sans licence fee + maintien indépendance long terme + maîtriser un engine production-ready 2D. Moins de tutos qu'Unity mais croissance forte post-Unity-runtime-fee.

### PlayCanvas

PlayCanvas est le **compromis optimal** d'après matrix brute (31/40) : web-first comme Three.js, Editor cloud collaboratif "Figma-style", JS-only (zéro switch langage), MCP officiel maintenu par PlayCanvas eux-mêmes, mobile via Capacitor + bonus mobile-social (Snap, WhatsApp), build léger compétitif Three.js. Migration 240-400 h (6-10 sem) — partiellement annule plan 8 sem mais réutilise R1+R2 sur design. Free tier OK pour projets publics, $15/mois pour privés. **Risque principal** : niche plus petite qu'Unity/Godot (moins de tutos, plus de docs officielles needed), Editor cloud dependency (offline limité). **Profil idéal** : Mike veut Editor + JS preserved + web-first + mobile via wrapper. Souvent oublié face à Unity, mais signal fort = MCP officiel maintenu par l'équipe core.

### Bevy

Bevy est l'**option long terme philosophique** : ECS pur, Rust, MIT/Apache, performance native top-tier, MCP debugger-oriented (`bevy_brp_mcp`). Migration 800-1200 h (20-30 sem) — **annule plan 8 sem largement**. Pré-1.0 = breaking changes à chaque release. Pas d'editor mature (bevy_editor work-in-progress 2026), level design 98 levels en code = très douloureux. Rust learning curve = 3-6 mois sans préalable. Quelques indies shippent (Toroban Steam). **Profil idéal** : Mike veut investir 6+ mois dans Rust pour bénéficier de la performance native pure + indépendance technologique radicale + shipping multi-platform natif. Pour un TD 2D web-first, Bevy = **sur-investissement** sauf si Mike veut spécifiquement apprendre Rust + ECS.

---

## Synthèse pour décision (résumé exécutif)

| Decision | Quand choisir |
|---|---|
| **Three.js statu quo + R3-01 tooling** | Si Milan = projet web personnel + Milan + démos amis. Pas d'ambition Steam/iOS court terme. **Plan 8 sem préservé.** |
| **Unity** | Si Mike veut shipping Steam + iOS + Android dans <12 mois. Prêt à perdre 12-18 sem pour gagner toolchain premium. **Plan 8 sem annulé.** |
| **Godot 4** | Si Mike veut shipping multi-platform sans licence fee, maintien indépendance, GDScript = OK. **Plan 8 sem annulé.** |
| **PlayCanvas** | Si Mike veut Editor + JS preserved + bundle léger. Compromis web-first avec gain Editor. **Plan partiellement réutilisable.** |
| **Bevy** | Si Mike veut apprendre Rust + investir 6 mois minimum. **Hors-scope pour TD 2D web-first usual.** |

**État Unity-MCP** : très mature (5800★, 34+ tools, MIT, `batch_execute` 10-100× speedup). **État Godot-MCP** : multiples implémentations, jusqu'à 170 tools (Claude-GoDot-MCP). **État PlayCanvas-MCP** : officiel maintenu par PlayCanvas. **État Bevy-MCP** : debugger-oriented uniquement.

---

## Sources

- Three.js : https://threejs.org/ ; https://www.utsubo.com/blog/threejs-vs-unity-web-comparison ; https://www.utsubo.com/blog/threejs-vs-babylonjs-vs-playcanvas-comparison
- Unity-MCP : https://github.com/CoplayDev/unity-mcp ; https://github.com/CoderGamester/mcp-unity ; https://github.com/IvanMurzak/Unity-MCP ; https://github.com/NoSpoonLab/unity-mcp ; https://github.com/HuntNight/unity-mcp-advanced
- Unity pricing 2026 : https://unity.com/products/pricing-updates ; https://80.lv/articles/unity-announces-its-upcoming-2026-price-changes ; https://www.vendr.com/marketplace/unity
- Unity ScriptableObjects : https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html ; https://unity.com/how-to/architect-game-code-scriptable-objects
- Godot-MCP : https://github.com/Coding-Solo/godot-mcp ; https://github.com/Dokujaa/Godot-MCP ; https://github.com/DaRealDaHoodie/Claude-GoDot-MCP ; https://github.com/LeeSinLiang/godot-mcp ; https://github.com/ee0pdt/Godot-MCP ; https://godotengine.org/asset-library/asset/4961
- Godot Docs : https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html ; https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html ; https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_ios.html ; https://developer.android.com/games/engines/godot/godot-export
- Godot vs Unity 2026 : https://dev.to/linou518/godot-vs-unity-in-2026-which-engine-should-indie-developers-choose-50g4 ; https://codingquests.io/blog/gdscript-vs-csharp-godot-4 ; https://chickensoft.games/blog/gdscript-vs-csharp
- Godot bundle size : https://popcar.bearblog.dev/how-to-minify-godots-build-size/ ; https://amann.dev/blog/2025/godot_web_size/
- Godot TD : https://github.com/quiver-dev/tower-defense-godot4 ; https://forum.godotengine.org/t/thank-you-godot-i-created-a-stylistic-3d-tower-defense-in-godot-dampf-the-cozy-tower-defense/52745
- PlayCanvas : https://playcanvas.com/ ; https://playcanvas.com/plans ; https://playcanvas.com/products/editor ; https://github.com/playcanvas/editor-mcp-server ; https://www.ojambo.com/the-mobile-web-secret-performance-killers-are-dead-playcanvas-vs-threejs-2026
- Bevy : https://www.bevyengine.org/ ; https://github.com/bevyengine/bevy ; https://bevy.org/news/bevy-0-18/ ; https://www.prismnews.com/news/bevy-releases-v0181-to-stabilize-018-fix-regressions ; https://github.com/bevyengine/bevy/discussions/9789
- Bevy MCP : https://docs.rs/bevy_debugger_mcp/ ; https://lib.rs/crates/bevy_brp_mcp ; https://github.com/Nub/bevy_mcp ; https://github.com/ladvien/bevy_debugger_mcp
- Rust engines 2026 : https://aarambhdevhub.medium.com/rust-game-engines-in-2026-bevy-vs-macroquad-vs-ggez-vs-fyrox-which-one-should-you-actually-use-9bf93669e83f ; https://medium.com/solo-devs/bevy-in-2025-rusts-game-engine-taking-over-indie-dev-caec2ae50c09
- Mobile wrapper : https://capacitorjs.com/ ; https://www.techtarget.com/searchmobilecomputing/news/252496257/Ionic-Capacitor-emerging-as-successor-to-Cordova ; https://github.com/ionic-team/capacitor/discussions/5562
- Steam Direct : https://partner.steamgames.com/doc/gettingstarted/appfee
- Apple Developer : https://developer.apple.com/programs/whats-included/
