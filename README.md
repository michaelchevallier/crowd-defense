# Crowd Defense

A tower defense game built with **Unity 6 LTS** targeting Steam (Mac/Win/Linux), iOS, Android, and WebGL.

> Status: pre-release — active development. See `LICENSE` for usage terms.

## Stack

- **Engine** — Unity 6 LTS
- **Language** — C# (.NET Standard 2.1)
- **Render pipeline** — URP
- **Target platforms** — Steam standalone (Mac/Win/Linux), iOS, Android, WebGL

## Build

### Build WebGL (Unity Editor)

Menu `CrowdDefense > Build WebGL`, or via CLI batch mode from the project root:

```bash
"$UNITY_PATH" \
  -batchmode -nographics -projectPath . \
  -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit
```

Output: `Build/WebGL/`.

### Other targets

Same pattern with `BuildOSX`, `BuildWindows`, `BuildLinux`, `BuildIOS`, `BuildAndroid`, or `BuildAll` (Mac/Win/Linux).

### Local test

```bash
cd Build/WebGL && python3 -m http.server 8000
# http://localhost:8000
```

## Live preview

A current build is deployed for previewing:

🎮 [michaelchevallier.github.io/crowd-defense/v6/](https://michaelchevallier.github.io/crowd-defense/v6/)

## Project layout

```
Assets/
  Scripts/
    Entities/      Tower, Enemy, Castle (MonoBehaviour gameplay)
    Data/          TowerType, EnemyType, LevelData, BalanceConfig (ScriptableObject)
    Systems/       LevelRunner, WaveManager, Economy, AudioController, etc.
    UI/            HUD, menus, settings (UI Toolkit)
    Visual/        JuiceFX, VfxPool, MaterialController, etc.
  ScriptableObjects/  Tower/Enemy/Level SO instances
  Models/             GLTF models (Towers/, Enemies/, etc.)
  Audio/              SFX + music
  Shaders/            URP shaders (toon, outline, boss effects)
  Editor/             Build scripts + Editor tooling
  Prefabs/            Reusable GameObjects
  Scenes/             Main.unity
docs/                 Design specs, decisions, research
.github/              CI workflows, Dependabot
```

## Security

See [SECURITY.md](SECURITY.md) for vulnerability disclosure policy.

## License

Proprietary — All Rights Reserved. See [LICENSE](LICENSE).
