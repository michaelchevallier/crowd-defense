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
    // Convention de nommage clips Quaternius : "Idle", "Walking_A", "Running_A",
    //   "1H_Melee_Attack_Chop", "Death_A", etc.
    // Output : Assets/Resources/Animations/Controllers/{fileName}.controller
    // Mike : lancer via menu Tools > CrowdDefense > Build Animator Controllers
    //        après chaque import de nouveaux modèles GLTF.
    public static class BuildAnimatorControllers
    {
        private const string k_ModelsRoot = "Assets/Models";
        private const string k_OutputDir  = "Assets/Resources/Animations/Controllers";

        [MenuItem("Tools/CrowdDefense/Build Animator Controllers")]
        public static void Build()
        {
            Directory.CreateDirectory(k_OutputDir);

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { k_ModelsRoot });
            int created = 0;
            int skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter) continue;

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                var clips = allAssets.OfType<AnimationClip>()
                    // Ignore preview clips internes Unity (préfixés __preview__)
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToArray();

                if (clips.Length == 0)
                {
                    skipped++;
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                string controllerPath = $"{k_OutputDir}/{fileName}.controller";

                // Skip si controller up-to-date (ne pas écraser les tweaks manuels)
                if (File.Exists(controllerPath))
                {
                    skipped++;
                    continue;
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var rootSM = controller.layers[0].stateMachine;

                AnimatorState? idleState    = null;
                AnimatorState? walkState    = null;
                AnimatorState? attackState  = null;
                AnimatorState? deathState   = null;

                // Ajoute un state par clip + détecte les states clés par substring
                foreach (var clip in clips)
                {
                    var state = rootSM.AddState(clip.name);
                    state.motion = clip;

                    string lower = clip.name.ToLowerInvariant();

                    if (idleState == null && lower.Contains("idle"))
                        idleState = state;

                    if (walkState == null && (lower.Contains("walk") || lower.Contains("run")))
                        walkState = state;

                    if (attackState == null && (lower.Contains("attack") || lower.Contains("shoot") || lower.Contains("melee")))
                        attackState = state;

                    if (deathState == null && (lower.Contains("death") || lower.Contains("die")))
                        deathState = state;
                }

                // Idle = state par défaut
                if (idleState != null)
                    rootSM.defaultState = idleState;

                // Ajoute paramètre isWalking + transitions Idle ↔ Walk
                if (idleState != null && walkState != null)
                {
                    controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);

                    var toWalk = idleState.AddTransition(walkState);
                    toWalk.hasExitTime     = false;
                    toWalk.duration        = 0.2f;
                    toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");

                    var toIdle = walkState.AddTransition(idleState);
                    toIdle.hasExitTime     = false;
                    toIdle.duration        = 0.2f;
                    toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
                }

                // Ajoute attackTrigger + transition depuis Idle (et Walk si présent)
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

                    // Retour à Idle après l'attaque
                    if (idleState != null)
                    {
                        var back = attackState.AddTransition(idleState);
                        back.hasExitTime  = true;
                        back.exitTime     = 0.9f;
                        back.duration     = 0.2f;
                    }
                }

                // Ajoute dieTrigger (pas de retour — Death est terminal)
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
    }
}
#endif
