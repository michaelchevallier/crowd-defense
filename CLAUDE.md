# Crowd Defense — Instructions Claude

## Projet

Jeu **tower defense** Unity 6 LTS (`6000.0.74f1`), C#, cible multi-platform (Steam Mac/Win/Linux, iOS, Android, WebGL). Migration depuis prototype web Phaser/Three.js (`milan project` repo, tag `v5.0-pre-pivot-unity`).

**Origines** : projet "Milan Park Defense — Foire en Lave" pivoté Phaser → Three.js → Unity 6 (2026-05-11). Le thème "Milan/lave/foire" est retiré pour ce repo : projet renommé **Crowd Defense** pour rester ouvert sur le thème final.

Repo : https://github.com/michaelchevallier/crowd-defense (privé)

## Stack

- **Unity 6 LTS** : `6000.0.74f1` (Apple silicon, build supports Mac/iOS/Android/WebGL)
- **C# / .NET Standard 2.1** (Unity-géré)
- **Unity-MCP** : https://github.com/CoplayDev/unity-mcp (Claude Code orchestre Unity Editor via 34+ MCP tools)
- **Claude Code Opus 4.7** : Mike pilote stratégie, Sonnet exécute tickets feature-dev en worktree

## État

Sprint MIGRATE en cours (post-pivot 2026-05-11). Voir `STATUS.md` pour status multi-session.

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
# CLI Unity batch mode (no UI)
Unity -batchmode -nographics -projectPath . -executeMethod BuildScript.BuildWebGL -quit
Unity -batchmode -nographics -projectPath . -executeMethod BuildScript.BuildOSX -quit
Unity -batchmode -nographics -projectPath . -executeMethod TestRunner.RunAll -quit
```

Via Unity-MCP depuis Claude Code :
- `mcp__unity-mcp__run_play_mode` — lance play mode
- `mcp__unity-mcp__read_console` — lit logs Editor
- `mcp__unity-mcp__batch_execute` — bulk ops
- `mcp__unity-mcp__manage_scriptable_object` — CRUD SO

## Cible architecture (à confirmer par le `/plan` formel migration)

```
Assets/
  Scripts/
    Entities/            # MonoBehaviour : Tower, Enemy, Hero
    Data/                # ScriptableObject : TowerType, EnemyType, LevelData
    Systems/             # Singleton : LevelRunner, Economy, Synergies, Pacing
    UI/                  # MonoBehaviour : HudController, WaveButton, RadialMenu
    Build/               # Editor scripts : BuildScript, TestRunner
  Prefabs/
    Towers/              # 12 tower prefabs
    Enemies/             # 30 enemy prefabs
    UI/                  # button, panel, toast
  ScriptableObjects/
    Towers/              # 12 SO assets
    Enemies/             # 30 SO assets
    Levels/              # 80 SO assets
    Balance/             # 1 SO BalanceConfig (Q1-Q14)
  Scenes/
    Main.unity           # Menu + LevelRunner + HUD
    Loader.unity         # First scene, splash
  Settings/              # URP, Input, Build profiles
docs/                    # Cf README — specs, research, decisions
.claude/                 # Personas + status
```

## Backlog actuel

🚧 **Sprint MIGRATE en cours**. Voir `STATUS.md` pour détail des phases.

Specs D1 (target design moteur-agnostique) ramenées dans `docs/specs/design/` :
- D1-01 économie (`_upgradeCost` ×5, interest bank, magnet rework, treasure tiles, boss 0×)
- D1-02 pacing (bouton "Lancer la vague (N)" + skip bonus 5s/30¢ + streak +5%/+25%)
- D1-03 L3 hybride (4 tours signature × 2 branches DPS/utility, sell 80%)
- D1-04 castle HP (formule `100 + 50 × √world × difficultyMul` + pressure mob W1-W10 + no-regen W6+)

Decisions Q1-Q18 arbitrées dans `docs/decisions/Q1-Q18-arbitrages.md`.

## Références

- Unity 6 docs : https://docs.unity3d.com/6000.0/Documentation/Manual/
- Unity-MCP : https://github.com/CoplayDev/unity-mcp
- Archive Phaser : https://github.com/michaelchevallier/lava_game (tag `v5.0-pre-pivot-unity`)
