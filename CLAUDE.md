# Crowd Defense — Instructions Claude

## Projet

Jeu **tower defense** Unity 6 LTS (`6000.0.74f1`), C#, cible multi-platform (Steam Mac/Win/Linux, iOS, Android, WebGL). Migration depuis prototype web Phaser/Three.js (`milan project` repo, tag `v5.0-pre-pivot-unity`).

## 📂 Source code à migrer (référence canonique)

**Paths legacy sources** : voir `.claude/internal/legacy-sources.md` (gitignored, dev notes privées avec mapping Phaser → Unity et locations disque locales).

**Source map détaillé** : voir `STATUS.md` section "📂 SOURCE CODE À MIGRER" (tableau Phaser → Unity per fichier).

**Note importante** : les agent personas dans `.claude/agents/` ont été adaptés Unity context. Quelques refs résiduelles aux paths legacy sources sont **volontaires** (source code à migrer, à lire mais pas à modifier).

**Origines** : projet pivoté Phaser → Three.js → Unity 6 (2026-05-11). Projet renommé **Crowd Defense** pour rester ouvert sur le thème final.

## Stack

- **Unity 6 LTS** : `6000.0.74f1` (Apple silicon, build supports Mac/iOS/Android/WebGL)
- **C# / .NET Standard 2.1** (Unity-géré)
- **Unity-MCP** : https://github.com/CoplayDev/unity-mcp (Claude Code orchestre Unity Editor via 34+ MCP tools)
- **Claude Code Opus 4.7** : Mike pilote stratégie, Sonnet exécute tickets feature-dev en worktree

## État

Sprint **R6-PARITY-V4-FINAL** livré (2026-05-12) — **95-99% parité V4** sur les axes visibles et runtime stable. Highlights post-pivot :
- 35 UI controllers migrés sur `UIControllerBase` (factorisation init / refs / lifecycle).
- Hero / Enemy / Tower / Castle splittés en **partial classes** par responsabilité (Combat, Movement, Anim, Stats, Init, Lifecycle, …).
- 8 dynamic events V4 wired sur 61 / 90 levels (D1-04 castle pressure + events runtime).
- 30+ textures Flux Schnell wired ground / path / sky / castle (cf `tools/gen_textures.py`).
- AudioClipRegistry 76 entries sémantiquement clean (clip keys canon).
- Audit reports détaillés dans `.claude/audit/*` (parity gap, perf, dedup, scene wires, schools mapping).

Voir `STATUS.md` pour le tracker session-par-session et le backlog priorisé.

## Workflow Opus orchestre, Sonnet exécute

**Règle de base** : pour CHAQUE tâche non-triviale (≥ 1 fichier modifié, > 5 min de code), Opus ne code pas directement. Opus scope, écrit un ticket auto-suffisant, et délègue à un Sonnet `feature-dev` en worktree. Ça libère le contexte d'Opus pour la stratégie et garde des commits atomiques propres.

Cf le workflow détaillé dans le `CLAUDE.md` historique du repo `milan project` (toujours valide, adapter "fichiers JS" → "fichiers C# / scènes Unity / ScriptableObjects").

### Brief pour ticket Sonnet (adapté Unity)

Doit contenir :
- **Type** (feature-dev / bug-fixer / quality-maintainer)
- **Estimé** (en commits + heures)
- **Bloqué par** (autres tickets) si applicable
- **Brief** : contexte 1-2 phrases + état actuel à refactor
- **Commits à livrer** : liste séquentielle, chacun avec titre `<type>(<scope>): <quoi en français court>` + détails par fichier (chemin absolu, classe + méthode + comportement attendu)
- **Verification** : commandes build (Unity batch mode CLI) + live test scenarios via Unity-MCP play mode
- **Files critiques** : liste paths absolus modifiés/créés

### Worktrees

`isolation: "worktree"` crée copie isolée pour chaque agent. Évite conflits sur fichiers communs (`Assets/Scripts/Tower.cs`, `Assets/Scripts/LevelRunner.cs`).

## Conventions

- **Commits atomiques** : `feat(...)`, `fix(...)`, `refactor:`, `chore:`. Footer Co-Authored-By Claude Opus 4.7.
- **Pas de commentaires** sauf si WHY non-obvious (invariant subtil, workaround spécifique, contrainte cachée).
- **Pas de `Debug.Log`** en prod build (utiliser `#if UNITY_EDITOR` ou un wrapper conditionnel).
- **C# moderne** : `nullable` enabled, expression-bodied members, pattern matching, file-scoped namespaces.
- **Pas de doc XML longue** : noms de classes/méthodes auto-documentés.
- **Performance** : 60 FPS desktop / 30 FPS mobile, GPU instancing pour tours et ennemis, jobs system pour pathing.

## Pièges Unity 6 LTS (à se rappeler)

### MonoBehaviour vs ScriptableObject
- **MonoBehaviour** : runtime behavior, attaché GameObject. Lourd, ne jamais en créer des centaines (utiliser pool ou ECS).
- **ScriptableObject** : data asset, partagé, sérialisable, modifiable via Inspector. Cible pour `TowerType`, `EnemyType`, `LevelData`, `BalanceConfig`.

### Sérialisation
- Champs `[SerializeField] private` = sérialisés mais privés (best practice vs `public` champs).
- `[NonSerialized]` pour exclure du save.
- Modifications runtime de SO **persistent** dans l'Editor (peuvent salir le repo via meta files). Toujours wrap les writes runtime dans `#if UNITY_EDITOR` ou utiliser une copie.

### Update / FixedUpdate / LateUpdate
- `Update` : chaque frame (input, gameplay logic).
- `FixedUpdate` : interval fixe (physics, déterministe).
- `LateUpdate` : après tous les Updates (camera follow, post-processing).

### Coroutines vs async
- Coroutines = Unity-native, manipulables (StopCoroutine), gérées par le scene.
- async/await = pratique mais attention au context (Unity main thread), préférer UniTask si performance critique.

### Performance pitfalls
- `Find`, `GetComponent` en `Update` = catastrophe. Cache dans `Awake`/`Start`.
- `Instantiate`/`Destroy` répétés = catastrophe. Object pool.
- `Mathf.PI` lookup chaque frame = catastrophe. Cache en local static.
- LINQ `.Where().Select().ToList()` en hot path = catastrophe. Préférer for loop manuel.

### Unity-MCP gotchas
- `manage_scriptable_object` peut créer/modifier des SO depuis Claude, mais le rebuild Editor est nécessaire pour re-import (`AssetDatabase.Refresh()` auto-géré par MCP).
- `batch_execute` = critique pour rester sous le rate limit (10-100× speedup sur ops bulk).
- `run_play_mode` lance Unity en play mode pour QA gameplay réel.

### Builds
- WebGL builds : 5-15 MB compressé (gros vs Phaser 395 KB, mais cross-platform natif).
- iOS builds : require Xcode local + Apple Developer account + cert.
- Steam builds : Mac/Win/Linux standalone via Build Settings → Steamworks SDK plugin.

## Workflow session

1. **Lire `STATUS.md` en premier** — source of truth multi-session de la migration Unity.
2. Lire ce fichier (`CLAUDE.md` root).
3. `git log --oneline | head -10` pour voir l'état.
4. `git branch --show-current` doit retourner `main`.
5. Vérifier Unity-MCP connexion : depuis Claude Code, tools `mcp__unity-mcp__*` doivent répondre.
6. Test live : Unity Editor ouvert + play mode si gameplay.

## Tester en local

```bash
# CLI Unity batch mode (no UI) — depuis la racine du projet
"$UNITY_PATH" -batchmode -nographics -projectPath . -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit
"$UNITY_PATH" -batchmode -nographics -projectPath . -executeMethod CrowdDefense.Build.BuildScript.BuildOSX -quit
"$UNITY_PATH" -batchmode -nographics -projectPath . -executeMethod TestRunner.RunAll -quit
```

Via Unity-MCP depuis Claude Code :
- `mcp__unity-mcp__run_play_mode` — lance play mode
- `mcp__unity-mcp__read_console` — lit logs Editor
- `mcp__unity-mcp__batch_execute` — bulk ops
- `mcp__unity-mcp__manage_scriptable_object` — CRUD SO

## Architecture actuelle (état post-R6-PARITY-V4-FINAL)

```
Assets/
  Scripts/
    Entities/            # MonoBehaviour split en partial classes :
                         #   Hero.cs + Hero.{Anim,Combat,Movement,Abilities,Perks}.cs
                         #   Enemy.cs + Enemy.{Anim,Behaviors,Combat,Init,Lifecycle,Movement,Stats,Update}.cs
                         #   Tower.cs + Tower.{Anim,Combat,Effects,Placement,Upgrade}.cs
                         #   Castle.cs + Castle.{HP,VFX}.cs
    Data/                # ScriptableObject : TowerType, EnemyType, LevelData, BalanceConfig, AudioClipRegistry
    Systems/             # Singleton : LevelRunner, Economy, Synergies, Pacing, WaveManager, AudioController
    UI/                  # 35 controllers basés sur UIControllerBase (HUD, RadialMenu, WorldMap, etc.)
    Build/               # Editor scripts : BuildScript, TestRunner, BuildAnimatorControllers
  Prefabs/Towers,Enemies,UI/
  ScriptableObjects/Towers,Enemies,Levels,Balance/
  Scenes/Main.unity, Loader.unity
  Settings/              # URP, Input, Build profiles
docs/                    # specs, research, decisions
.claude/                 # personas, plans, specs, audit reports, status tracker
```

## Backlog actuel

✅ **Sprint R6-PARITY-V4-FINAL livré** (2026-05-12) — 95-99% parité V4 visible + runtime stable.

Phases livrées : POC (Phase 1) → Migrate (Phases 2-4) → Sprint R6-PARITY-V4 (waves 1-4 + tickets N1-N33) → R6-PARITY-V4-FINAL. WebGL live `/v6/`. Voir tracker session-par-session : `STATUS.md` (root) + `.claude/status/STATUS.md` (status interne).

**Spécifications D1** (target design moteur-agnostique) `docs/specs/design/` — toutes wired runtime :
- D1-01 économie (`_upgradeCost` ×5, interest bank, magnet rework, treasure tiles, boss 0×)
- D1-02 pacing (bouton "Lancer la vague (N)" + skip bonus 5s/30¢ + streak +5%/+25%)
- D1-03 L3 hybride (4 tours signature × 2 branches DPS/utility, sell 80%)
- D1-04 castle HP (formule `100 + 50 × √world × difficultyMul` + pressure mob W1-W10 + no-regen W6+)

Decisions Q1-Q18 arbitrées dans `docs/decisions/Q1-Q18-arbitrages.md`.

**Reste 1-5% gap V4** : anim state machines runtime polish, real SFX clip pass, Castle .fbx art final, perf-3fix non shippé. Détail prioritisé dans audit `.claude/audit/SPRINT-R6-PARITY-V4-FINAL-completion.md`.

## Références

- Unity 6 docs : https://docs.unity3d.com/6000.0/Documentation/Manual/
- Unity-MCP : https://github.com/CoplayDev/unity-mcp
- Archive Phaser : https://github.com/michaelchevallier/lava_game (tag `v5.0-pre-pivot-unity`)
