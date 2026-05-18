#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.EditorTools
{
    /// <summary>
    /// Idempotent: scans Assets/ScriptableObjects/Bosses/ for BossDef assets,
    /// loads them, and wires the BossSystem GameObject's registry field in
    /// Main.unity. Run via Tools/CrowdDefense/QA/Wire BossSystem Registry.
    /// Designed to be safe to re-run after BossDef additions.
    /// </summary>
    public static class BossSystemRegistryAutoWire
    {
        [MenuItem("Tools/CrowdDefense/QA/Wire BossSystem Registry")]
        public static void Wire()
        {
            string scenePath = "Assets/Scenes/Main.unity";
            var openScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var bs = Object.FindFirstObjectByType<CrowdDefense.Systems.BossSystem>();
            if (bs == null)
            {
                Debug.LogError("[WireBossReg] No BossSystem in Main.unity");
                return;
            }

            // Load all BossDef assets
            var guids = AssetDatabase.FindAssets("t:BossDef", new[] { "Assets/ScriptableObjects/Bosses" });
            var defs = new List<CrowdDefense.Data.BossDef>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var d = AssetDatabase.LoadAssetAtPath<CrowdDefense.Data.BossDef>(path);
                if (d != null) defs.Add(d);
            }
            Debug.Log($"[WireBossReg] Loaded {defs.Count} BossDef assets");

            // Wire via SerializedObject
            var so = new SerializedObject(bs);
            var regProp = so.FindProperty("registry");
            if (regProp == null)
            {
                Debug.LogError("[WireBossReg] BossSystem.registry SerializedProperty not found");
                return;
            }
            regProp.ClearArray();
            for (int i = 0; i < defs.Count; i++)
            {
                regProp.InsertArrayElementAtIndex(i);
                regProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(openScene);
            EditorSceneManager.SaveScene(openScene);
            Debug.Log($"[WireBossReg] Wired {defs.Count} BossDef into BossSystem.registry and saved Main.unity");
        }

        // Also auto-run from V3LoopAutoRunner one-shot pref so headless runs are self-sufficient.
        [InitializeOnLoadMethod]
        private static void AutoWireOnLoad()
        {
            if (Application.isBatchMode) return;
            if (!EditorPrefs.GetBool("cd_v3loop_autowire_boss", false)) return;
            EditorPrefs.SetBool("cd_v3loop_autowire_boss", false); // one-shot
            EditorApplication.delayCall += () =>
            {
                try { Wire(); }
                catch (System.Exception ex) { Debug.LogException(ex); }
            };
        }
    }
}
