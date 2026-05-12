#nullable enable
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CrowdDefense.Editor
{
    // Génère un AnimatorController par asset GLTF importé sous Assets/Models/.
    // Clip patterns supportés :
    //   Idle   : "idle", "flying_idle"
    //   Walk   : "walk", "run", "fast_flying"
    //   Attack : "attack", "shoot", "melee", "punch", "bite", "headbutt"
    //   Death  : "death", "die", "dead"
    // Output : Assets/Resources/Animations/Controllers/{fileName}.controller
    // Lancer via menu Tools > CrowdDefense > Build Animator Controllers
    //              puis > Wire Animators To Prefabs
    public static class BuildAnimatorControllers
    {
        private const string k_ModelsRoot = "Assets/Models";
        private const string k_OutputDir  = "Assets/Resources/Animations/Controllers";

        // Alias for -executeMethod CLI invocation.
        public static void GenerateAll() => Build();

        [MenuItem("Tools/CrowdDefense/Build Animator Controllers")]
        public static void Build()
        {
            Directory.CreateDirectory(k_OutputDir);

            // t:Model only catches classic FBX/OBJ. UnityGLTF .gltf/.glb use ScriptedImporter → t:GameObject.
            // Union with t:Model ensures both kinds are scanned.
            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { k_ModelsRoot });
            var goGuids    = AssetDatabase.FindAssets("t:GameObject", new[] { k_ModelsRoot });
            string[] guids = modelGuids.Union(goGuids).ToArray();
            int created = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext  = Path.GetExtension(path).ToLowerInvariant();
                // Only ingest model files (FBX/OBJ/GLTF/GLB) — skip .prefab / .asset under Models/.
                if (ext != ".fbx" && ext != ".obj" && ext != ".gltf" && ext != ".glb") continue;
                var importer = AssetImporter.GetAtPath(path);
                if (importer is not ModelImporter && importer is not UnityEditor.AssetImporters.ScriptedImporter) continue;

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                var clips = allAssets.OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToArray();

                if (clips.Length == 0)
                {
                    skipped++;
                    continue;
                }

                string fileName       = Path.GetFileNameWithoutExtension(path);
                string controllerPath = $"{k_OutputDir}/{fileName}.controller";

                if (File.Exists(controllerPath))
                {
                    skipped++;
                    continue;
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var rootSM = controller.layers[0].stateMachine;

                AnimatorState? idleState   = null;
                AnimatorState? walkState   = null;
                AnimatorState? attackState = null;
                AnimatorState? deathState  = null;

                foreach (var clip in clips)
                {
                    var state = rootSM.AddState(clip.name);
                    state.motion = clip;

                    string lower = clip.name.ToLowerInvariant();

                    if (idleState == null && (lower == "idle" || lower == "flying_idle"))
                        idleState = state;

                    if (walkState == null && (lower.Contains("walk") || lower.Contains("run") || lower == "fast_flying"))
                        walkState = state;

                    if (attackState == null && (
                            lower.Contains("attack") || lower.Contains("shoot") || lower.Contains("melee") ||
                            lower.Contains("punch")  || lower.Contains("bite")  || lower.Contains("headbutt")))
                        attackState = state;

                    if (deathState == null && (lower.Contains("death") || lower.Contains("die") || lower == "dead"))
                        deathState = state;
                }

                if (idleState != null)
                    rootSM.defaultState = idleState;

                if (idleState != null && walkState != null)
                {
                    controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);

                    var toWalk = idleState.AddTransition(walkState);
                    toWalk.hasExitTime = false;
                    toWalk.duration    = 0.2f;
                    toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");

                    var toIdle = walkState.AddTransition(idleState);
                    toIdle.hasExitTime = false;
                    toIdle.duration    = 0.2f;
                    toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
                }

                if (attackState != null && (idleState != null || walkState != null))
                {
                    controller.AddParameter("attackTrigger", AnimatorControllerParameterType.Trigger);

                    if (idleState != null)
                    {
                        var t = idleState.AddTransition(attackState);
                        t.hasExitTime = false;
                        t.duration    = 0.1f;
                        t.AddCondition(AnimatorConditionMode.If, 0, "attackTrigger");
                    }
                    if (walkState != null)
                    {
                        var t = walkState.AddTransition(attackState);
                        t.hasExitTime = false;
                        t.duration    = 0.1f;
                        t.AddCondition(AnimatorConditionMode.If, 0, "attackTrigger");
                    }

                    if (idleState != null)
                    {
                        var back = attackState.AddTransition(idleState);
                        back.hasExitTime = true;
                        back.exitTime    = 0.9f;
                        back.duration    = 0.2f;
                    }
                }

                if (deathState != null)
                {
                    controller.AddParameter("dieTrigger", AnimatorControllerParameterType.Trigger);
                    var src = idleState ?? walkState;
                    if (src != null)
                    {
                        var t = src.AddTransition(deathState);
                        t.hasExitTime = false;
                        t.duration    = 0.1f;
                        t.AddCondition(AnimatorConditionMode.If, 0, "dieTrigger");
                    }
                }

                EditorUtility.SetDirty(controller);
                created++;

                Debug.Log(
                    $"[BuildAnimatorControllers] '{fileName}' — {clips.Length} clips" +
                    $" | idle={idleState?.name ?? "-"}" +
                    $" walk={walkState?.name ?? "-"}" +
                    $" attack={attackState?.name ?? "-"}" +
                    $" death={deathState?.name ?? "-"}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildAnimatorControllers] Terminé — {created} créés, {skipped} ignorés.");
        }

        // Builds one AnimatorController per KayKit hero (knight, mage, ranger, barbarian,
        // rogue, rogue_hooded) using shared Rig_Medium animation GLBs.
        // Output : Assets/Resources/Animations/Controllers/{heroName}.controller
        // Idempotent — skips existing controllers.
        [MenuItem("Tools/CrowdDefense/Build KayKit Hero Controllers")]
        public static void BuildKayKitHeroControllers()
        {
            const string generalGlbPath  = "Assets/Models/Heroes/KayKit/Animations/gltf/Rig_Medium/Rig_Medium_General.glb";
            const string movementGlbPath = "Assets/Models/Heroes/KayKit/Animations/gltf/Rig_Medium/Rig_Medium_MovementBasic.glb";

            Directory.CreateDirectory(k_OutputDir);

            var generalClips  = AssetDatabase.LoadAllAssetsAtPath(generalGlbPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();
            var movementClips = AssetDatabase.LoadAllAssetsAtPath(movementGlbPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();

            if (generalClips.Length == 0 && movementClips.Length == 0)
            {
                Debug.LogWarning("[BuildAnimatorControllers] KayKit Rig_Medium GLBs not found or have no clips. Check paths:\n" +
                    $"  {generalGlbPath}\n  {movementGlbPath}");
                return;
            }

            AnimationClip? FindClip(AnimationClip[] pool, params string[] names)
            {
                foreach (var n in names)
                    foreach (var c in pool)
                        if (string.Equals(c.name, n, System.StringComparison.OrdinalIgnoreCase)) return c;
                return null;
            }

            var idleClip   = FindClip(generalClips,  "Idle_A", "Idle_B");
            var walkClip   = FindClip(movementClips, "Walking_A", "Walking_B", "Running_A");
            var attackClip = FindClip(generalClips,  "Hit_A", "Hit_B", "Throw", "Use_Item", "Interact");
            var deathClip  = FindClip(generalClips,  "Death_A", "Death_B");

            string[] heroNames = { "knight", "mage", "ranger", "barbarian", "rogue", "rogue_hooded" };
            int created = 0, skipped = 0;

            foreach (var heroName in heroNames)
            {
                string controllerPath = $"{k_OutputDir}/{heroName}.controller";
                if (File.Exists(controllerPath)) { skipped++; continue; }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var rootSM = controller.layers[0].stateMachine;

                AnimatorState? idleState   = null;
                AnimatorState? walkState   = null;
                AnimatorState? attackState = null;
                AnimatorState? deathState  = null;

                if (idleClip != null)
                {
                    idleState = rootSM.AddState("Idle");
                    idleState.motion = idleClip;
                    rootSM.defaultState = idleState;
                }
                if (walkClip != null)
                {
                    walkState = rootSM.AddState("Walk");
                    walkState.motion = walkClip;
                }
                if (attackClip != null)
                {
                    attackState = rootSM.AddState("Attack");
                    attackState.motion = attackClip;
                }
                if (deathClip != null)
                {
                    deathState = rootSM.AddState("Death");
                    deathState.motion = deathClip;
                }

                if (idleState != null && walkState != null)
                {
                    controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
                    var toWalk = idleState.AddTransition(walkState);
                    toWalk.hasExitTime = false; toWalk.duration = 0.2f;
                    toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
                    var toIdle = walkState.AddTransition(idleState);
                    toIdle.hasExitTime = false; toIdle.duration = 0.2f;
                    toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
                }

                if (attackState != null)
                {
                    controller.AddParameter("attackTrigger", AnimatorControllerParameterType.Trigger);
                    foreach (var src in new[] { idleState, walkState })
                    {
                        if (src == null) continue;
                        var t = src.AddTransition(attackState);
                        t.hasExitTime = false; t.duration = 0.1f;
                        t.AddCondition(AnimatorConditionMode.If, 0, "attackTrigger");
                    }
                    if (idleState != null)
                    {
                        var back = attackState.AddTransition(idleState);
                        back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.2f;
                    }
                }

                if (deathState != null)
                {
                    controller.AddParameter("dieTrigger", AnimatorControllerParameterType.Trigger);
                    var src = idleState ?? walkState;
                    if (src != null)
                    {
                        var t = src.AddTransition(deathState);
                        t.hasExitTime = false; t.duration = 0.1f;
                        t.AddCondition(AnimatorConditionMode.If, 0, "dieTrigger");
                    }
                }

                EditorUtility.SetDirty(controller);
                created++;
                Debug.Log($"[BuildAnimatorControllers] KayKit hero '{heroName}' — " +
                    $"idle={idleClip?.name ?? "-"} walk={walkClip?.name ?? "-"} " +
                    $"attack={attackClip?.name ?? "-"} death={deathClip?.name ?? "-"}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildAnimatorControllers] KayKit heroes — {created} créés, {skipped} ignorés.");
        }

        // Assigne chaque .controller généré à l'Animator du GLTF importé correspondant.
        // Couvre Heroes + Enemies. Idempotent (safe à relancer).
        // Note : Unity interdit l'édition directe du prefab GLTF importé — on passe par
        // SerializedObject sur le ModelImporter, puis ReimportAsset pour appliquer.
        [MenuItem("Tools/CrowdDefense/Wire Animators To Prefabs")]
        public static void WireAnimatorsToPrefabs()
        {
            if (!Directory.Exists(k_OutputDir))
            {
                Debug.LogWarning("[WireAnimators] Pas de controllers — lance d'abord 'Build Animator Controllers'.");
                return;
            }

            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[]
            {
                $"{k_ModelsRoot}/Heroes",
                $"{k_ModelsRoot}/Enemies"
            });

            int wired   = 0;
            int missing = 0;

            foreach (string guid in modelGuids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(modelPath) is not ModelImporter) continue;

                string fileName       = Path.GetFileNameWithoutExtension(modelPath);
                string controllerPath = $"{k_OutputDir}/{fileName}.controller";

                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller == null)
                {
                    missing++;
                    continue;
                }

                // ModelImporter expose defaultClipAnimations mais pas le controller directement.
                // La seule façon stable d'attacher un controller à un GLTF prefab importé est via
                // PrefabUtility sur un Prefab Variant, ou via le SerializedObject du root GameObject.
                // On charge le root GameObject importé et on modifie l'Animator via SerializedObject.
                var rootGO = AssetDatabase.LoadMainAssetAtPath(modelPath) as GameObject;
                if (rootGO == null)
                {
                    missing++;
                    continue;
                }

                var animator = rootGO.GetComponentInChildren<Animator>(includeInactive: true);
                if (animator == null)
                {
                    // GLTF sans Animator — on force l'ajout via importer extraUserData flag (non standard).
                    // On se contente de logger : le prefab n'a pas d'Animator.
                    Debug.LogWarning($"[WireAnimators] {fileName}: pas d'Animator sur le prefab GLTF importé.");
                    missing++;
                    continue;
                }

                if (animator.runtimeAnimatorController == controller)
                    continue;

                var so = new SerializedObject(animator);
                var controllerProp = so.FindProperty("m_Controller");
                if (controllerProp == null)
                {
                    missing++;
                    continue;
                }

                controllerProp.objectReferenceValue = controller;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rootGO);
                wired++;

                Debug.Log($"[WireAnimators] {fileName} -> controller assigné.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WireAnimators] Terminé — {wired} wirés, {missing} sans controller ou animator.");
        }
    }
}
#endif
