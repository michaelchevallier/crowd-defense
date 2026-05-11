#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CrowdDefense.Entities;
using CrowdDefense.Systems;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class POC05Setup
    {
        [MenuItem("CrowdDefense/POC-05: Create Tower + Projectile Prefabs")]
        public static void CreatePrefabs()
        {
            CreateTowerPrefab();
            CreateProjectilePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[POC05] Tower.prefab + Projectile.prefab created.");
        }

        [MenuItem("CrowdDefense/POC-05: Setup PlacementController in Scene")]
        public static void SetupScene()
        {
            // Find or create PlacementController GO under Systems
            var systemsGO = GameObject.Find("Systems");
            if (systemsGO == null)
            {
                Debug.LogError("[POC05] 'Systems' GO not found in scene.");
                return;
            }

            var pcTransform = systemsGO.transform.Find("PlacementController");
            GameObject pcGO;
            if (pcTransform != null)
            {
                pcGO = pcTransform.gameObject;
                Debug.Log("[POC05] PlacementController already exists, updating refs.");
            }
            else
            {
                pcGO = new GameObject("PlacementController");
                pcGO.transform.SetParent(systemsGO.transform, false);
            }

            var pc = pcGO.GetComponent<PlacementController>() ?? pcGO.AddComponent<PlacementController>();

            var so = new SerializedObject(pc);

            // Assign selectedTowerType = Archer.asset
            var archerAsset = AssetDatabase.LoadAssetAtPath<TowerType>("Assets/ScriptableObjects/Towers/Archer.asset");
            if (archerAsset != null)
                so.FindProperty("selectedTowerType").objectReferenceValue = archerAsset;
            else
                Debug.LogError("[POC05] Archer.asset not found.");

            // Assign towerPrefab
            var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower.prefab");
            if (towerPrefab != null)
                so.FindProperty("towerPrefab").objectReferenceValue = towerPrefab;
            else
                Debug.LogWarning("[POC05] Tower.prefab not found — run CreatePrefabs first.");

            // Assign projectilePrefab
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            if (projPrefab != null)
                so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
            else
                Debug.LogWarning("[POC05] Projectile.prefab not found — run CreatePrefabs first.");

            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[POC05] PlacementController setup complete.");
        }

        private static void CreateTowerPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Towers/Tower.prefab";

            // Root GO (empty)
            var root = new GameObject("Tower");
            root.AddComponent<Tower>();

            // Child Base: Cube, scale (1, 0.5, 1), Y=0.25
            var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGO.name = "Base";
            baseGO.transform.SetParent(root.transform, false);
            baseGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            baseGO.transform.localScale = new Vector3(1f, 0.5f, 1f);
            // Remove collider (Tower does not need primitive colliders)
            Object.DestroyImmediate(baseGO.GetComponent<BoxCollider>());

            // Child Top: Cylinder, scale (0.6, 0.6, 0.6), Y=0.85
            var topGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topGO.name = "Top";
            topGO.transform.SetParent(root.transform, false);
            topGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            topGO.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Object.DestroyImmediate(topGO.GetComponent<CapsuleCollider>());

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[POC05] Created {prefabPath}");
        }

        private static void CreateProjectilePrefab()
        {
            const string prefabPath = "Assets/Prefabs/Projectile.prefab";

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
            go.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[POC05] Created {prefabPath}");
        }
    }
}
#endif
