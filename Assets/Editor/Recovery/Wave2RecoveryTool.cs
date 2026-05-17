#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.EditorTools.Recovery
{
    // Wave R2 Recovery — autonomous repair pass for Mike's "no visible gameplay" Play-mode bug
    // bundle. Reads the current scene state, copies skybox materials into Resources so runtime
    // can load them, wires up SkyboxController Inspector slots, ensures RenderSettings.skybox
    // is assigned, sets Camera.main clear flags to Skybox, scrubs missing-script components,
    // and (optionally) renders a Camera screenshot to .claude/qa-screenshots/.
    //
    // Run from Unity menu : Tools > CrowdDefense > Recovery > Wave2 > RunAll
    //
    // Side effects : modifies Main.unity (saves it), copies .mat assets, writes screenshots.
    public static class Wave2RecoveryTool
    {
        private const string SkyboxSrcDir   = "Assets/Materials/Skybox";
        private const string SkyboxDstDir   = "Assets/Resources/Skybox";
        private const string ScreenshotDir  = "Assets/../.claude/qa-screenshots";

        [MenuItem("Tools/CrowdDefense/Recovery/Wave2/RunAll")]
        public static void RunAll()
        {
            Debug.Log("=== Wave R2 Recovery RunAll START ===");

            CopySkyboxesToResources();
            WireSkyboxControllerSlots();
            EnsureSkyboxAssigned();
            EnsureCameraClearFlagsSkybox();
            ScrubMissingScripts();
            EnsureTowerToolbarWiring();
            EnsurePlacementControllerWiring();
            SaveScene();
            CaptureScreenshot();

            Debug.Log("=== Wave R2 Recovery RunAll DONE — enter Play mode to verify ===");
        }

        [MenuItem("Tools/CrowdDefense/Recovery/Wave2/Audit")]
        public static void Audit()
        {
            var mr = UnityEngine.Object.FindAnyObjectByType<MapRenderer>();
            var sky = UnityEngine.Object.FindAnyObjectByType<SkyboxController>();
            var cam = Camera.main;
            var hud = UnityEngine.Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            var lr = UnityEngine.Object.FindAnyObjectByType<LevelRunner>();
            int missing = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                foreach (var c in go.GetComponents<Component>()) if (c == null) missing++;
            }

            Debug.Log(
                $"[Audit] MapRenderer={(mr!=null?mr.transform.childCount.ToString():"NULL")} " +
                $"SkyboxController={(sky!=null)} RenderSettings.skybox={(RenderSettings.skybox!=null?RenderSettings.skybox.name:"NULL")} " +
                $"Camera.clearFlags={(cam!=null?cam.clearFlags.ToString():"NO-CAM")} " +
                $"HUD={(hud!=null?hud.name:"NULL")} LevelRunner={(lr!=null)} MissingScripts={missing}");
        }

        // ── Skybox: copy .mat into Resources so runtime Resources.Load can find them ─────

        public static void CopySkyboxesToResources()
        {
            if (!Directory.Exists(SkyboxSrcDir))
            {
                Debug.LogWarning($"[Wave2Recovery] No skybox source dir at {SkyboxSrcDir}");
                return;
            }
            if (!AssetDatabase.IsValidFolder(SkyboxDstDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Skybox");
            }

            int copied = 0;
            foreach (var src in Directory.GetFiles(SkyboxSrcDir, "skybox_*.mat"))
            {
                string file = Path.GetFileName(src);
                string dst  = SkyboxDstDir + "/" + file;
                if (File.Exists(dst)) continue;
                bool ok = AssetDatabase.CopyAsset(src, dst);
                if (ok) copied++;
                else Debug.LogWarning($"[Wave2Recovery] Failed to copy {src} -> {dst}");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Wave2Recovery] CopySkyboxesToResources copied={copied}");
        }

        // ── SkyboxController Inspector slot wiring via SerializedObject ──────────────────

        private static readonly (string field, string asset)[] SkyboxFieldMap =
        {
            ("skyboxPlaine",     "skybox_plaine.mat"),
            ("skyboxForet",      "skybox_foret.mat"),
            ("skyboxDesert",     "skybox_desert.mat"),
            ("skyboxVolcan",     "skybox_volcan.mat"),
            ("skyboxApocalypse", "skybox_apocalypse.mat"),
            ("skyboxEspace",     "skybox_espace.mat"),
            ("skyboxSubmarin",   "skybox_submarin.mat"),
            ("skyboxMedieval",   "skybox_medieval.mat"),
            ("skyboxCyberpunk",  "skybox_cyberpunk.mat"),
            ("skyboxFoire",      "skybox_foire.mat"),
        };

        public static void WireSkyboxControllerSlots()
        {
            var sky = UnityEngine.Object.FindAnyObjectByType<SkyboxController>();
            if (sky == null)
            {
                Debug.LogWarning("[Wave2Recovery] No SkyboxController in scene — skipping wiring");
                return;
            }

            var so = new SerializedObject(sky);
            int assigned = 0;
            foreach (var (field, file) in SkyboxFieldMap)
            {
                var prop = so.FindProperty(field);
                if (prop == null) continue;
                if (prop.objectReferenceValue != null) continue;

                string path = SkyboxDstDir + "/" + file;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    string fallback = SkyboxSrcDir + "/" + file;
                    mat = AssetDatabase.LoadAssetAtPath<Material>(fallback);
                }
                if (mat != null)
                {
                    prop.objectReferenceValue = mat;
                    assigned++;
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sky);
            Debug.Log($"[Wave2Recovery] WireSkyboxControllerSlots assigned={assigned}/10");
        }

        public static void EnsureSkyboxAssigned()
        {
            if (RenderSettings.skybox != null) return;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxDstDir + "/skybox_plaine.mat");
            if (mat == null)
                mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxSrcDir + "/skybox_plaine.mat");
            if (mat == null)
                mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (mat != null)
            {
                RenderSettings.skybox = mat;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                DynamicGI.UpdateEnvironment();
                Debug.Log($"[Wave2Recovery] EnsureSkyboxAssigned set RenderSettings.skybox={mat.name}");
            }
        }

        // ── Camera clear flags ───────────────────────────────────────────────────────────

        public static void EnsureCameraClearFlagsSkybox()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Wave2Recovery] No Camera.main — skipping clear flags");
                return;
            }
            cam.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(cam.gameObject);
            Debug.Log($"[Wave2Recovery] Camera.main.clearFlags=Skybox");
        }

        // ── Missing scripts cleanup ──────────────────────────────────────────────────────

        public static void ScrubMissingScripts()
        {
            int total = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0)
                {
                    total += removed;
                    EditorUtility.SetDirty(go);
                }
            }
            Debug.Log($"[Wave2Recovery] ScrubMissingScripts removed={total}");
        }

        // ── TowerToolbar wiring ──────────────────────────────────────────────────────────

        public static void EnsureTowerToolbarWiring()
        {
            var tt = UnityEngine.Object.FindAnyObjectByType<TowerToolbarController>();
            if (tt == null)
            {
                Debug.LogWarning("[Wave2Recovery] No TowerToolbarController in scene");
                return;
            }
            var so = new SerializedObject(tt);
            var prop = so.FindProperty("towerRegistry");
            if (prop != null && prop.objectReferenceValue == null)
            {
                var reg = AssetDatabase.LoadAssetAtPath<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
                if (reg != null)
                {
                    prop.objectReferenceValue = reg;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(tt);
                    Debug.Log("[Wave2Recovery] TowerToolbarController.towerRegistry wired");
                }
            }
        }

        // ── PlacementController wiring (TowerRegistry + tower/projectile prefabs) ────────

        public static void EnsurePlacementControllerWiring()
        {
            var pc = UnityEngine.Object.FindAnyObjectByType<PlacementController>();
            if (pc == null)
            {
                Debug.LogWarning("[Wave2Recovery] No PlacementController in scene");
                return;
            }
            var so = new SerializedObject(pc);
            int wired = 0;

            var regProp = so.FindProperty("towerRegistry");
            if (regProp != null && regProp.objectReferenceValue == null)
            {
                var reg = AssetDatabase.LoadAssetAtPath<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
                if (reg != null) { regProp.objectReferenceValue = reg; wired++; }
            }

            var prefProp = so.FindProperty("towerPrefab");
            if (prefProp != null && prefProp.objectReferenceValue == null)
            {
                var tower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towers/Tower.prefab");
                if (tower != null) { prefProp.objectReferenceValue = tower; wired++; }
            }

            var projProp = so.FindProperty("projectilePrefab");
            if (projProp != null && projProp.objectReferenceValue == null)
            {
                var proj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
                if (proj != null) { projProp.objectReferenceValue = proj; wired++; }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pc);
            Debug.Log($"[Wave2Recovery] PlacementController wired={wired}");
        }

        // ── Save & screenshot ────────────────────────────────────────────────────────────

        public static void SaveScene()
        {
            if (Application.isPlaying)
            {
                Debug.Log("[Wave2Recovery] Play mode — skipping SaveScene (Unity disallows)");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Wave2Recovery] Saved scene {scene.name}");
            }
        }

        public static void CaptureScreenshot()
        {
            var cam = Camera.main;
            if (cam == null) return;
            int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            cam.Render();
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            string fullDir = Path.GetFullPath(ScreenshotDir);
            Directory.CreateDirectory(fullDir);
            string path = Path.Combine(fullDir, $"V6-recovery-wave2-after-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[Wave2Recovery] Screenshot -> {path}");
        }
    }
}
