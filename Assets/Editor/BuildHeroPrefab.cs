#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Editor
{
    public static class BuildHeroPrefab
    {
        private const string PrefabPath   = "Assets/Prefabs/Hero.prefab";
        private const string HeroTypePath = "Assets/ScriptableObjects/Heroes/Knight.asset";

        [MenuItem("Tools/CrowdDefense/Build Hero Prefab")]
        public static void BuildFromMenu() => Build(silent: false);

        /// <summary>
        /// Creates (or refreshes) Assets/Prefabs/Hero.prefab.
        /// Root has Hero MonoBehaviour + capsule body mesh (fallback, replaced at runtime
        /// if GLTF is present in AssetRegistry).
        /// Called by SetupMainScene.Run() before WireInspectorRefs.
        /// </summary>
        public static void Build(bool silent = false)
        {
            var heroType = AssetDatabase.LoadAssetAtPath<HeroType>(HeroTypePath);
            Color bodyColor = heroType != null
                ? heroType.BodyColor
                : new Color(0.227f, 0.416f, 0.749f);

            // ── Build in-memory hierarchy ────────────────────────────────────
            var root = new GameObject("Hero");
            root.AddComponent<Hero>();

            // Body mesh — capsule (taller, narrower than default)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale    = new Vector3(0.5f, 0.6f, 0.5f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var rend = body.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                // Prefer toon lit, fallback to URP/Standard
                var shader = Shader.Find("CrowdDefense/Toon/Lit")
                          ?? Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Standard");
                var mat = new Material(shader) { color = bodyColor };
                mat.name = "Hero_Body_Mat";
                rend.sharedMaterial = mat;
            }

            // ── Save as prefab ───────────────────────────────────────────────
            System.IO.Directory.CreateDirectory("Assets/Prefabs");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogError("[BuildHeroPrefab] SaveAsPrefabAsset failed.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                Debug.Log($"[BuildHeroPrefab] Hero.prefab saved at {PrefabPath} (bodyColor={bodyColor})");
        }
    }
}
#endif
