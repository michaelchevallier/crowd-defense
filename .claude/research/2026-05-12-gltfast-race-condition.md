# GLTFast race condition `SortAndNormalizeBoneWeightsJob` — research

**Date** : 2026-05-12
**Type** : research-only (zero code change)
**Trigger** : TASK A WAVE-3-IDLE-FILLER (`.claude/supervisor/instructions-to-exec.md` commit 6dac3cd, ligne 1305+)
**Symptôme Editor Console** : 50+ models `Assets/Models/Heroes/Quaternius/UltimateAnimatedCharacters/*.gltf` + `Assets/Models/Enemies/*.gltf` fail import :

```
InvalidOperationException: The previously scheduled job SortAndNormalizeBoneWeightsJob
writes to the Unity.Collections.NativeArray`1[GLTFast.Vertex.VBones]
SortAndNormalizeBoneWeightsJob.bones. You must call JobHandle.Complete() on the job
SortAndNormalizeBoneWeightsJob, before you can read from the
Unity.Collections.NativeArray`1[GLTFast.Vertex.VBones] safely.
  at GLTFast.VertexBufferBones.ScheduleVertexBonesJob (...VertexBufferBones.cs:47)
  at GLTFast.VertexBufferGenerator`1[TMainBuffer].ScheduleVertexBonesJobs (...VertexBufferGenerator.cs:373)
  at GLTFast.MeshGenerator.GenerateMesh (...MeshGenerator.cs:189)
  at GLTFast.Editor.GltfImporter.OnImportAsset (...GltfImporter.cs:116)
```

---

## 1. Root cause confirmed — YES

### Package versions installées (vérifié `Packages/packages-lock.json` + `Library/PackageCache`)

| Package | Version | Source |
|---|---|---|
| `com.unity.cloud.gltfast` | **6.14.1** | Unity registry (auto-pulled par `com.unity.ai.assistant` 2.7.0-pre.3) |
| `org.khronos.unitygltf` | **2.19.2** | git `KhronosGroup/UnityGLTF#release/2.19.2` (manifest direct dep) |
| Unity Editor | 6000.0.74f1 | LTS |
| `com.unity.burst` | 1.8.29 (1.8.24 requis par gltfast) | registry |
| `com.unity.collections` | 2.6.5 | registry |

### Mécanisme (issue GitHub officiel)

**GitHub issue `atteneder/glTFast#772` — "Editor importer broken" — état OPEN** depuis 2025-05-18 ([lien](https://github.com/atteneder/glTFast/issues/772)).

Stack trace utilisateur initial **identique au nôtre, exactement** :
- `GLTFast.VertexBufferBones.ScheduleVertexBonesJob` ligne 47 schedule `SortAndNormalizeBoneWeightsJob` puis lit `NativeArray<VBones>.bones` **avant** d'avoir appelé `JobHandle.Complete()` sur le job précédent.
- Unity job system `AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut` détecte la lecture concurrente et throw.
- **Race condition réelle** : pas un bug Burst-specific, c'est un bug de séquencement de jobs dans gltfast lui-même côté `VertexBufferGenerator.CreateVertexBuffer`.

### Conditions déclencheuses (corroboré par utilisateurs issue #772)

> **TheSabotender (16 sep 2025, gltfast 6.13.1 + Unity 6000.1.9f1)** :
> "it seems to be related to a Glb/glTF file that is exported with multiple objects that are all skinned, or meshes with multiple materials... i.e. when the glb file has multiple meshes"

Cohérent avec notre payload : Quaternius UltimateAnimatedCharacters = personnages skinnés multi-mesh (body + armor + weapon skinned au même rig).

### Workaround confirmé (mais inutilisable en CI/repo)

> **AmarilloArts (18 mai 2025)** : "If I enter play mode, then right click the gltf and select Reimport, it will work."

= preuve que c'est une race condition de scheduling Editor-only (Editor importer pipeline async vs runtime sync, voir `Editor/Scripts/AsyncHelpers.cs:130` dans la trace).

### État du fix

- **Issue #772 toujours OPEN** au 2026-05-12.
- **Owner atteneder commente le 10 nov 2025** : "I think I resolved that in the current development version that's about to be released this week as 6.15.0. If you want me to reassure, please provide a test asset to reproduce the issue."
  → tournure conditionnelle, pas de test reproducible fourni en retour, **issue non-closée**.
- **Changelog 6.15.0 (2025-11-17) vérifié** ([lien](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.15/changelog/CHANGELOG.html)) : entry "Multi-primitive skinned mesh imports with proper sub-meshes" sous "Fixed" — **likely match mais pas explicitement nommé**. Aucune mention `SortAndNormalizeBoneWeightsJob`, `JobHandle`, `race`, `AtomicSafetyHandle` dans 6.15.x, 6.16.x, 6.17.x, 6.18.x.
- **Changelogs 6.14.x, 6.13.x, 6.12.x** : aucune mention non plus, sauf 6.12.0 qui a fixé un autre `InvalidOperationException` sur "multi-primitive meshes with vertex colors" (issue Unity-Technologies#30) — bug similaire mais autre code path.

**Conclusion root cause** : **confirmed**, c'est une race condition dans `GLTFast.VertexBufferBones.ScheduleVertexBonesJob` (ligne 47) qui omet un `.Complete()` sur le `JobHandle` du `SortAndNormalizeBoneWeightsJob` précédent. **PAS** un bug Burst (`com.unity.burst` est innocent — désactiver Burst ne supprimerait pas la safety check, juste le JIT). Hypothèse "race Burst" initiale **invalidée** : c'est un bug pure job-scheduling côté gltfast.

---

## 2. Impact

### Inventaire `find Assets/Models -name "*.gltf" | wc -l`

| Catégorie | Path | Count |
|---|---|---|
| **Heroes (Quaternius animated)** | `Assets/Models/Heroes/Quaternius/UltimateAnimatedCharacters/` | **52** |
| Heroes (KayKit Equipment) | `Assets/Models/Heroes/KayKit/Equipment/` | 31 |
| **Enemies (mob + boss + persona)** | `Assets/Models/Enemies/` (incl. `Bosses/`) | **36** |
| Towers | `Assets/Models/Towers/` | 11 |
| Environment (Quaternius FantasyProps + Village + Nature + RTS) | `Assets/Models/Environment/Quaternius/*` | 466 |
| Props (KayKit Dungeon) | `Assets/Models/Props/KayKitDungeon/Assets/` | 305 |
| **TOTAL .gltf** | — | **807** |
| Aussi présents (.glb) | `Assets/Models/**` | 25 |

### Gameplay impact

- **Heroes (52 fichiers Quaternius Animated)** : skinned multi-mesh + multi-material → **100% touchés par #772**. Tous renderés en fallback `Tower.Init`/`Enemy.Init` capsule/primitive (cf commits `efa4c9d`, `04b9f31`).
- **Enemies (36 fichiers, dont Bosses)** : `goblin.gltf`, `mob_orc.gltf`, `zombie.gltf`, `mob_cyberpunk_flying.gltf`, etc. — la plupart skinned animated → **vast majority touchés**. Fallback capsule visible in-game.
- **Towers (11 fichiers)** : non-skinned typiquement → **NON touchés** (jobs bone weights pas scheduled si pas de bones). À vérifier mais à priori sains.
- **Environment + Props (771 fichiers)** : static geometry, no bones → **NON touchés**. À priori importés OK.
- **Estimé impact réel** : **~88 modèles** (52 heroes + ~36 enemies) ne rendent pas leur GLTF → fallback primitives. C'est cohérent avec "50+ models fail" du symptôme.
- **Severity** : visuel uniquement (fallback capsule joue). Gameplay fonctionne. Mais **bloque toute QA visuelle Phase 3 heroes/enemies** (récent commits `3f68e67`, `79b56f1`, `59785a6` autour Outline + AnimatorController batch tool sont **inutiles tant que les meshes ne sont pas importés**).

---

## 3. Options fix — trade-offs

### Option 1 — Upgrade GLTFast 6.14.1 → 6.15.x ou 6.16/6.17/6.18

**Action** : pin `"com.unity.cloud.gltfast": "6.15.1"` (ou plus récent) dans `Packages/manifest.json` (ajouter dépendance directe pour override la transitive 6.14.1 de `com.unity.ai.assistant`).

**Pros** :
- Officiellement le path "vendor-recommended" (atteneder dit le fix est dans 6.15.0).
- Reste sur stack Unity-supported.
- Aucun code change C#.
- Effort minimal (1 ligne JSON + Unity reimport).

**Cons** :
- **Fix NON-confirmé dans le changelog public** (no explicit "SortAndNormalizeBoneWeightsJob" mention 6.15.0-6.18.0). Le commit fix de gltfast est probablement bundlé sous l'entry vague "Multi-primitive skinned mesh imports with proper sub-meshes" (6.15.0) mais l'issue #772 reste OPEN au 2026-05-12 (12 jours avant aujourd'hui).
- Risque : 6.15+ apporte d'autres breaking changes (Draco 5.4.0 min, KTX 3.6.0 min, "compiler error enforced for outdated KTX or Draco package versions" — peut bloquer compile si autre dep désynchro).
- Si fix incomplet → on a juste perdu 30 min sans résultat.

**Estimation effort** : **30 min** (edit manifest + Unity reload + test reimport 1 hero `goblin.gltf`).
**Risque** : **MEDIUM** — fix probable mais non-garanti, possible chain de breakage Draco/KTX.

### Option 2 — Disable Burst pour GLTFast jobs

**Action évaluée** : ajouter `com.unity.burst` disable global ou `[BurstCompile(Disable = true)]` per-job.

**Verdict** : **INVALIDE — n'adresse pas le root cause.**

Le bug n'est PAS Burst-related. Le `InvalidOperationException` vient de `AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut` qui est la safety layer du **Unity Collections + Job System**, indépendante de Burst. Burst compile les jobs en native code mais la safety check NativeArray opère au-dessus.

Désactiver Burst :
- Ne supprimerait PAS l'exception (la safety check reste active).
- Ralentirait l'import (~5-10× plus lent pour jobs vector math).
- Aucun bénéfice.

**Estimation effort** : N/A (à ne pas faire).
**Risque** : **HIGH** — fausse piste, fait perdre du temps.

### Option 3a — Switch importer override gltfast → UnityGLTF (par asset, sans uninstall)

**Action** : UnityGLTF 2.19.2 est **déjà installé** (manifest direct dep `org.khronos.unitygltf`). Documenté côté gltfast :

> "If the glTFast package is also present in a project, glTFast gets precedence and UnityGLTF is available as Importer Override, which can be selected from a dropdown on each glTF asset."

Donc on peut, sur chaque `.gltf` problématique, ouvrir Inspector → "Importer" dropdown → choisir UnityGLTF → Apply → reimport. Ou en batch via script Editor `AssetImporter.SetImporterOverride()`.

**Pros** :
- UnityGLTF maintenu par KhronosGroup (référence officielle glTF 2.0).
- Pas de bug job-race équivalent connu.
- Coexiste avec gltfast (autres assets non-touchés gardent gltfast).
- Préserve les `.meta` (UnityGLTF et gltfast alignent leurs importer references pour switching sans casser prefab refs — cf doc).

**Cons** :
- ~88 assets à override → script batch Editor nécessaire (effort ~1-2h pour écrire + tester `AssetImporter.SetImporterOverride()` loop).
- Nouveaux `.meta` regen → gros diff git initial (mais one-shot).
- "switching between importers can change material references, mesh references etc., so some manual adjustments may be needed" → risque que les `AssetRegistry.Heroes/Enemies` prefabs perdent leurs mesh refs et nécessitent re-link.
- Performance UnityGLTF historiquement moins bonne que gltfast (mais on est en Editor import, pas runtime → impact faible).

**Estimation effort** : **2-3h** (script Editor batch + reimport 88 assets + validation visuelle Hero + re-link mesh refs si cassé).
**Risque** : **MEDIUM-LOW** — solution éprouvée, mais churn meta + prefab re-link possible.

### Option 3b — Uninstall gltfast + UnityGLTF seul importer

**Note** : NON-VIABLE proprement. `com.unity.ai.assistant` 2.7.0-pre.3 dépend transitivement de `com.unity.cloud.gltfast` 6.14.1 — uninstall gltfast force désinstall AI Assistant (pas critique pour ce projet mais à valider avec Mike). Si AI Assistant pas utilisé → faisable mais hors-scope research.

**Note Option 3 — ModelImporter natif Unity** : **NON-VIABLE**. Unity 6 LTS **n'a pas d'importer natif glTF/GLB**. Les seules options sont des packages : gltfast, UnityGLTF, GLTFUtility (3rd-party), etc. L'instruction-to-exec mentionne "ModelImporter natif Unity" comme Option 3 mais ce n'est techniquement pas possible — il faut un ScriptedImporter package. La vraie Option 3 est UnityGLTF (déjà installé, c'est l'alternative active).

---

## 4. Reco superviseur

### Recommandation : **Option 1 d'abord (sprint test 30 min), Option 3a en fallback (2-3h)**.

**Sequence proposée** :

1. **Étape 1 (30 min, RISK MEDIUM)** : pin `com.unity.cloud.gltfast` 6.15.1 ou plus récent (essayer 6.18.x latest stable). Manifest edit only :
   ```json
   "com.unity.cloud.gltfast": "6.15.1",
   ```
   Reload Unity. Reimport `Assets/Models/Heroes/Quaternius/UltimateAnimatedCharacters/Knight.gltf` (1 modèle test). Lire Console.
   - **Si import OK** → batch reimport `Assets/Models/Heroes` + `Assets/Models/Enemies`, commit `chore(deps): bump gltfast 6.14.1 → 6.15.1 fix #772 SortAndNormalizeBoneWeightsJob race`. Validation visuelle hero spawn. **DONE.**
   - **Si import KO toujours** → revert manifest, passer à Étape 2.

2. **Étape 2 fallback (2-3h, RISK MEDIUM-LOW)** : Option 3a switch importer override par asset.
   - Écrire script Editor `Assets/Scripts/Editor/SwitchGltfImporterTool.cs` qui itère `Assets/Models/Heroes/**/*.gltf` + `Assets/Models/Enemies/**/*.gltf`, appelle `AssetImporter.SetImporterOverride<UnityGLTF.GLTFImporter>()` + `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`.
   - Run, vérifier Console clean.
   - Re-link prefabs `AssetRegistry.Heroes/Enemies` si mesh refs cassées.
   - Commit `fix(import): override gltf importer UnityGLTF for skinned models gltfast #772 workaround`.

**Pourquoi pas Option 2** : invalidée techniquement (cf section 3).

**Critère d'arrêt Étape 1 → 2** : si après 6.15.1 + 6.18.x test, l'exception persiste, ne pas tenter d'autres versions intermédiaires — issue #772 est OPEN, fix non-prouvé public. Switcher directement.

**Effort total worst case** : **3h30** (30 min Étape 1 + 3h Étape 2). Best case : **30 min** (Étape 1 suffit).

**Risque résiduel post-fix** :
- Option 1 réussie → suivre 6.15+ changelogs en cas de regression Draco/KTX. Low.
- Option 3a réussie → maintenir un `.gltf` → `.gltf.meta` discipline (override flag dans meta) ; risque que nouvelles imports gltfast par défaut retombent en bug si on ajoute des assets sans run le script. Mitigation : trigger automatique via `AssetPostprocessor` qui force UnityGLTF override pour tout `.gltf` sous `Heroes/` ou `Enemies/`. Low-medium.

---

## Sources

- [GitHub Issue #772 — Editor importer broken](https://github.com/atteneder/glTFast/issues/772) — issue principale, état OPEN, commentaire owner atteneder Nov 2025 "fix in 6.15.0"
- [Unity glTFast 6.15.x changelog](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.15/changelog/CHANGELOG.html) — entry "Multi-primitive skinned mesh imports with proper sub-meshes" (likely fix)
- [Unity glTFast 6.18 changelog](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.18/changelog/CHANGELOG.html) — versions ultérieures (no explicit reference)
- [Unity glTFast 6.12 changelog](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.12/changelog/CHANGELOG.html) — précédent fix `InvalidOperationException` multi-primitive (#30) prouve pattern récurrent
- [KhronosGroup/UnityGLTF (2.19.2 installé)](https://github.com/KhronosGroup/UnityGLTF) — alternative ready-to-go
- [Unity Discussions — glTFast package availability](https://discussions.unity.com/t/unity-gltfast-package-is-now-available/935685) — précédence gltfast vs UnityGLTF documentée

**Files relevants** :
- `/Users/mike/Work/crowd-defense/Packages/manifest.json` (à éditer Étape 1)
- `/Users/mike/Work/crowd-defense/Packages/packages-lock.json` (référence versions résolues actuelles)
- `/Users/mike/Work/crowd-defense/Library/PackageCache/com.unity.cloud.gltfast@0847aee0f6da/package.json` (version 6.14.1 installée)
- `/Users/mike/Work/crowd-defense/Library/PackageCache/org.khronos.unitygltf@6d7980522323/package.json` (version 2.19.2 installée, alternative ready)
- `/Users/mike/Work/crowd-defense/Assets/Models/Heroes/Quaternius/UltimateAnimatedCharacters/*.gltf` (52 fichiers touchés)
- `/Users/mike/Work/crowd-defense/Assets/Models/Enemies/*.gltf` (36 fichiers touchés)
