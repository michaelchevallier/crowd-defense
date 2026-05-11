#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Entities;

namespace CrowdDefense.Editor
{
    public static class POC06Setup
    {
        [MenuItem("CrowdDefense/POC-06: Setup Economy + LevelRunner in Scene")]
        public static void SetupEconomyAndLevelRunner()
        {
            // Find or create Systems parent
            var systems = GameObject.Find("Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
                Undo.RegisterCreatedObjectUndo(systems, "Create Systems GO");
            }

            // LevelRunner
            var lrGO = GameObject.Find("LevelRunner");
            if (lrGO == null)
            {
                lrGO = new GameObject("LevelRunner");
                Undo.RegisterCreatedObjectUndo(lrGO, "Create LevelRunner GO");
                lrGO.transform.SetParent(systems.transform);
            }
            if (lrGO.GetComponent<LevelRunner>() == null)
                Undo.AddComponent<LevelRunner>(lrGO);

            // Assign W1-1 LevelData
            var levelData = AssetDatabase.LoadAssetAtPath<CrowdDefense.Data.LevelData>("Assets/ScriptableObjects/Levels/W1-1.asset");
            if (levelData != null)
            {
                var lr = lrGO.GetComponent<LevelRunner>();
                var so = new SerializedObject(lr);
                so.FindProperty("currentLevel").objectReferenceValue = levelData;
                so.ApplyModifiedProperties();
            }

            // Economy
            var ecoGO = GameObject.Find("Economy");
            if (ecoGO == null)
            {
                ecoGO = new GameObject("Economy");
                Undo.RegisterCreatedObjectUndo(ecoGO, "Create Economy GO");
                ecoGO.transform.SetParent(systems.transform);
            }
            if (ecoGO.GetComponent<Economy>() == null)
                Undo.AddComponent<Economy>(ecoGO);

            // Script Execution Order: LevelRunner = -100
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Systems/LevelRunner.cs");
            if (script != null)
            {
                MonoImporter.SetExecutionOrder(script, -100);
                Debug.Log("[POC06Setup] LevelRunner execution order set to -100");
            }

            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[POC06Setup] Economy + LevelRunner created under Systems");
        }

        [MenuItem("CrowdDefense/POC-06: Setup Castle in Scene")]
        public static void SetupCastle()
        {
            // Create Castle GO at root
            var castleGO = GameObject.Find("Castle");
            if (castleGO == null)
            {
                castleGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                castleGO.name = "Castle";
                Undo.RegisterCreatedObjectUndo(castleGO, "Create Castle GO");
            }

            castleGO.transform.localScale = new Vector3(2f, 2f, 2f);
            castleGO.transform.position = Vector3.zero;

            // Maroon material
            var rend = castleGO.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(CrowdDefense.Common.ShaderUtil.GetLitShader());
                mat.color = new Color(0.4f, 0.25f, 0.1f, 1f);
                rend.material = mat;
            }

            // Attach Castle.cs
            if (castleGO.GetComponent<Castle>() == null)
                Undo.AddComponent<Castle>(castleGO);

            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[POC06Setup] Castle GO created at root");
        }
    }
}
#endif
