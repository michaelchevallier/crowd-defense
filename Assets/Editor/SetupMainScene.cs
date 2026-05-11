#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;

namespace CrowdDefense.Editor
{
    public static class SetupMainScene
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/CrowdDefense/Setup Main Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Purge any orphaned [Auto] GOs left over from previous play mode auto-create fallbacks
            foreach (var rootGO in scene.GetRootGameObjects())
            {
                if (rootGO.name.StartsWith("[Auto]"))
                    Object.DestroyImmediate(rootGO);
            }

            int regs = EnsureRegistries();
            BuildMainSceneTool.BuildMainScene();
            int sys  = EnsureNewSingletons();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[SetupMainScene] OK — {sys} singletons, 0 UI controllers, {regs} registries created/updated");
        }

        // ── 1. Registries SO ────────────────────────────────────────────────

        private static int EnsureRegistries()
        {
            int n = 0;
            n += EnsureRegistry<PerkRegistry>("Assets/Resources/PerkRegistry.asset");
            n += EnsureRegistry<MetaUpgradeRegistry>("Assets/Resources/MetaUpgradeRegistry.asset");
            n += EnsureRegistry<DoctrineRegistry>("Assets/Resources/DoctrineRegistry.asset");
            n += EnsureRegistry<SkinRegistry>("Assets/Resources/SkinRegistry.asset");
            n += EnsureRegistry<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
            n += EnsureRegistry<CutsceneRegistry>("Assets/Resources/CutsceneRegistry.asset");
            // BossDefRegistry is not a type — BossSystem.registry is List<BossDef> wired via Inspector
            AssetDatabase.SaveAssets();
            return n;
        }

        private static int EnsureRegistry<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return 0;
            System.IO.Directory.CreateDirectory("Assets/Resources");
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return 1;
        }

        // ── 2. New Singletons under Systems/ ────────────────────────────────

        private static int EnsureNewSingletons()
        {
            var systems = GameObject.Find("Systems") ?? new GameObject("Systems");
            int created = 0, existing = 0;
            EnsureChild<PerkSystem>(systems, "PerkSystem", ref created, ref existing);
            EnsureChild<BossSystem>(systems, "BossSystem", ref created, ref existing);
            EnsureChild<MetaUpgradeSystem>(systems, "MetaUpgradeSystem", ref created, ref existing);
            EnsureChild<DoctrineSystem>(systems, "DoctrineSystem", ref created, ref existing);
            EnsureChild<SkinSystem>(systems, "SkinSystem", ref created, ref existing);
            EnsureChild<RunContext>(systems, "RunContext", ref created, ref existing);
            return created;
        }

        private static T EnsureChild<T>(GameObject parent, string name, ref int created, ref int existing) where T : Component
        {
            var child = parent.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent.transform, false);
                child = go.transform;
                created++;
            }
            else
            {
                existing++;
            }
            var comp = child.GetComponent<T>();
            if (comp == null)
                comp = child.gameObject.AddComponent<T>();
            return comp;
        }
    }
}
#endif
