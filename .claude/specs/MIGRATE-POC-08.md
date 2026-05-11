# MIGRATE-POC-08 — WebGL build + deploy /v6/ GitHub Pages

> Ticket 8/8 final POC. Build WebGL local + deploy public sur `/v6/` GitHub Pages.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 2-3 commits, ~70 min (40 min code + 15-25 min build)
- **Bloqué par** : POC-01..07 ✅
- **Branch** : `main` direct (push principal) + branche `gh-pages` (deploy)
- **Working dir** : `/Users/mike/Work/crowd-defense/`

## Objectif

3 livrables :
1. **`Assets/Editor/BuildScript.cs`** : MenuItem `CrowdDefense/Build WebGL` qui build le projet.
2. **WebGL Player Settings** configurés (Brotli + Decompression Fallback + Color Space Linear + Memory 256MB).
3. **Deploy `/v6/`** : push `Builds/WebGL/` content vers branche `gh-pages` sous folder `/v6/`. URL accessible : `https://michaelchevallier.github.io/crowd-defense/v6/`.

## Source canonique

- Aucun (Unity-spécifique).
- Référence build Phaser : `/Users/mike/Work/milan project/scripts/deploy_v5.sh` si existe (sinon, on improvise).

## Décisions techniques

- **Build via Editor script** : MenuItem `[MenuItem("CrowdDefense/Build WebGL")]`. Pas de CI/CD POC.
- **Output path** : `Builds/WebGL/` (gitignored).
- **Deploy gh-pages** : 
  - Branch `gh-pages` créée si pas déjà (vérifier `git ls-remote origin gh-pages`).
  - Copier `Builds/WebGL/*` vers `/tmp/gh-pages-checkout/v6/`.
  - Push vers `gh-pages`.
  - **NE PAS** push depuis le checkout principal (corromprait l'arbo).
  - Use worktree pour cleanliness : `git worktree add /tmp/gh-pages-checkout gh-pages`.
- **GITHUB_TOKEN** : invalide dans le shell parent Mike (STATUS.md piège #1). Wrapper `unset GITHUB_TOKEN && gh ...` ou `unset GITHUB_TOKEN && git push`.
- **Relative paths** : Unity génère index.html avec paths relatifs par défaut. OK pour deploy sous `/v6/`.

---

## Commit 1 — `feat(build): add Editor BuildScript with BuildWebGL MenuItem`

### Fichier : `Assets/Editor/BuildScript.cs`

```csharp
#nullable enable
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Build
{
    public static class BuildScript
    {
        private const string OutputFolder = "Builds/WebGL";
        private const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("CrowdDefense/Build WebGL")]
        public static void BuildWebGL()
        {
            ApplyWebGLPlayerSettings();

            string absOutput = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            if (Directory.Exists(absOutput))
                Directory.Delete(absOutput, true);
            Directory.CreateDirectory(absOutput);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building WebGL → {absOutput}");
            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"[BuildScript] Build {summary.result} : size={summary.totalSize / 1024 / 1024} MB, time={summary.totalTime.TotalSeconds:F1}s");

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.Combine(absOutput, "index.html"));
        }

        private static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.template = "PROJECT:Default";
        }
    }
}
#endif
```

### Process commit 1

1. Créer dossier `Assets/Editor/` (folder convention Unity pour Editor-only code).
2. Write BuildScript.cs.
3. `refresh_unity` + verify compile (`UnityEditor` namespace doit être accessible — folder `Editor` permet).
4. `mcp__UnityMCP__execute_menu_item menu_path="CrowdDefense/Build WebGL"` pour lancer le build (lent : 5-15 min). 
   - **Important** : utilise `run_in_background: true` sur le tool call si dispo, ou `Monitor` pour follow le log Unity. Sinon timeout possible. Alternative : lancer en CLI batch mode `Unity -batchmode -projectPath . -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit` (mais Mac M1 nécessite GUI parfois).
5. Wait completion. Vérifier `Builds/WebGL/index.html` existe + `Build/` subfolder non vide.
6. `mcp__UnityMCP__read_console` → log `[BuildScript] Build Succeeded : size=XX MB`.
7. Vérifier `.gitignore` contient `Builds/` (sinon ajouter).
8. `git add Assets/Editor/BuildScript.cs` (+ .gitignore si modifié) + commit `feat(build): add Editor BuildScript with BuildWebGL MenuItem`.

---

## Commit 2 — `docs(build): add build + deploy /v6/ instructions to README`

Petit commit doc pour Mike puisse rebuilder/redeploy en autonomie post-POC.

### Patch `README.md` (ajouter section)

```markdown
## Build & Deploy

### Build WebGL local
```
Unity Editor : menu `CrowdDefense > Build WebGL` (ou CLI `Unity -batchmode -projectPath . -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit`).
Output dans `Builds/WebGL/index.html`. Test local :
```bash
cd Builds/WebGL && python3 -m http.server 8000
# Browse http://localhost:8000
```

### Deploy /v6/ GitHub Pages
```bash
# 1. Build (cf ci-dessus).
# 2. Si branche gh-pages pas encore créée :
unset GITHUB_TOKEN
git worktree add /tmp/gh-pages-checkout gh-pages 2>/dev/null || git checkout --orphan gh-pages && git rm -rf . && git commit --allow-empty -m "init gh-pages" && git push origin gh-pages && git checkout main && git worktree add /tmp/gh-pages-checkout gh-pages

# 3. Copy build → /v6/
rm -rf /tmp/gh-pages-checkout/v6
mkdir -p /tmp/gh-pages-checkout/v6
cp -r Builds/WebGL/* /tmp/gh-pages-checkout/v6/

# 4. Commit & push gh-pages
cd /tmp/gh-pages-checkout
git add v6
git commit -m "deploy v6 POC build $(date +%Y-%m-%d)"
unset GITHUB_TOKEN && git push origin gh-pages

# 5. URL accessible (après ~30s propagation) :
# https://michaelchevallier.github.io/crowd-defense/v6/
```

### Note GITHUB_TOKEN

Le shell parent Mike a un `GITHUB_TOKEN` invalide (expiré). Toujours `unset GITHUB_TOKEN` avant `gh` ou `git push` (sinon HTTP 401).
```

### Process commit 2

1. Edit README.md (append section "Build & Deploy").
2. `git add README.md` + commit `docs(build): add build + deploy /v6/ instructions to README`.

---

## Commit 3 — `chore(deploy): publish v6 POC build to gh-pages branch`

**Optionnel** : si Sonnet a accès au push gh-pages et le veut atomique, ce commit est sur `gh-pages` branch directement (pas `main`).

### Process commit 3

1. Vérifier branche gh-pages existe sur remote :
   ```bash
   unset GITHUB_TOKEN && git ls-remote --heads origin gh-pages
   ```
2. Si absent, créer :
   ```bash
   git checkout --orphan gh-pages
   git rm -rf .
   git commit --allow-empty -m "init gh-pages branch"
   unset GITHUB_TOKEN && git push origin gh-pages
   git checkout main
   ```
3. Worktree :
   ```bash
   git worktree add /tmp/gh-pages-checkout gh-pages
   ```
4. Copy build :
   ```bash
   rm -rf /tmp/gh-pages-checkout/v6
   mkdir -p /tmp/gh-pages-checkout/v6
   cp -r Builds/WebGL/* /tmp/gh-pages-checkout/v6/
   ```
5. Commit + push gh-pages :
   ```bash
   cd /tmp/gh-pages-checkout
   git add v6
   git commit -m "deploy v6 POC build $(date +%Y-%m-%d)"
   unset GITHUB_TOKEN && git push origin gh-pages
   ```
6. Wait 30-60s propagation. Test URL `https://michaelchevallier.github.io/crowd-defense/v6/` via `curl -I` (200 attendu).
7. Cleanup : `git worktree remove /tmp/gh-pages-checkout`.

---

## Verification finale

```bash
ls Builds/WebGL/                    # index.html + Build/ + StreamingAssets/ + TemplateData/
du -sh Builds/WebGL/                # ~5-15 MB compressé
git log --oneline -5
```

Test browser :
- Local : `python3 -m http.server 8000` dans `Builds/WebGL/` → `http://localhost:8000` → Unity logo loading → game start → W1-1 jouable.
- Remote (après deploy) : `https://michaelchevallier.github.io/crowd-defense/v6/` → idem.

**Critères succès** :
- BuildScript.cs compile + MenuItem visible Unity menu.
- Build WebGL produit `Builds/WebGL/index.html` sans erreur.
- Game charge dans browser + W1-1 jouable (HUD visible, click-to-place archer fonctionne, enemies spawn, can win/lose).
- Deploy `/v6/` accessible publiquement.
- 2-3 commits pushed (main + gh-pages).

## Pièges anticipés

1. **Build lent (5-15 min)** : IL2CPP + Brotli compression. Utiliser `run_in_background` ou batch mode. Si Unity-MCP timeout sur `execute_menu_item`, alternative : run CLI `Unity -batchmode ...` via Bash run_in_background + Monitor.
2. **WebGL build échoue silently** : check `mcp__UnityMCP__read_console` post-build. Erreurs courantes :
   - Shader compilation : URP shaders nécessitent platform target = WebGL dans Player Settings.
   - Out of memory : augmenter heap Unity Editor (`Unity > Preferences > GI Cache > Maximum cache size`).
   - IL2CPP missing : install via Unity Hub Add Modules.
3. **`.gitignore` `Builds/`** : déjà devrait être là (CLAUDE.md mentionne). Verify pas committer 15 MB de build.
4. **GitHub Pages 404 après push gh-pages** : settings GitHub repo doivent activer Pages depuis branche `gh-pages` root (settings → pages → source = gh-pages, /). À la première fois, Mike doit toggler ce setting manuellement (UI GitHub web).
5. **CORS / HTTPS** : GitHub Pages serve en HTTPS. Unity WebGL fonctionne en HTTPS sans config (différent du file://).
6. **`PROJECT:Default` template** : template HTML par défaut Unity. Si Mike veut un loading screen custom, créer template Phase 2.
7. **Color Space Linear sur WebGL** : peut nécessiter Graphics API = WebGL 2.0 (PlayerSettings > Graphics APIs for WebGL). Defaults généralement OK.
8. **Worktree cleanup** : `git worktree remove /tmp/gh-pages-checkout` à la fin pour pas laisser des states bizarres.

## Quand fini

Build OK, /v6/ accessible publiquement. Phase 1 POC complète. Termine et reviens à Opus pour debrief.
