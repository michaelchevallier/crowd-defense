#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrowdDefense.EditorTools
{
    public static class V3ScreenshotBatch
    {
        [MenuItem("Tools/CrowdDefense/QA/V3Batch/Screenshot")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory("Library/V3Screenshots");

            var scenes = new[] { "Loader", "Menu", "WorldMap", "Main" };
            var sb = new StringBuilder();
            sb.AppendLine("=== V3 Screenshot Batch ===");
            sb.AppendLine($"Date: {DateTime.UtcNow:O}");
            sb.AppendLine();

            int passed = 0, failed = 0;

            foreach (var scene in scenes)
            {
                try
                {
                    CaptureScene(scene);
                    sb.AppendLine($"PASS: {scene} captured");
                    passed++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"FAIL: {scene} — {ex.Message}");
                    failed++;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"=== Summary: {passed} PASSED, {failed} FAILED ===");

            File.WriteAllText("Library/V3Screenshots/report.txt", sb.ToString());
            Debug.Log(sb.ToString());

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        private static void CaptureScene(string sceneName)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);

            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
                throw new Exception($"No Camera found in scene {sceneName}");

            int w = 1920, h = 1080;
            var rt = new RenderTexture(w, h, 32, RenderTextureFormat.ARGB32);
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            var png = tex.EncodeToPNG();
            File.WriteAllBytes($"Library/V3Screenshots/{sceneName}.png", png);
            Debug.Log($"[V3Screenshot] {sceneName}: {png.Length} bytes");

            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
