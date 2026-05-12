#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Guarantees at least one Camera exists in the active scene.
    /// Menu.unity ships without a Camera (UI-only scene), but URP Forward renderer
    /// needs a Camera to drive its render loop or nothing draws — not even
    /// Screen Space Overlay UI Toolkit panels on WebGL2.
    ///
    /// Auto-creates a minimal MainCamera at the origin if Camera.main is null at
    /// scene start. The camera does not render any 3D content (CullingMask = 0)
    /// but it primes the URP pipeline so UI Toolkit panels composite correctly.
    /// </summary>
    public static class EnsureMainCamera
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCamera()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckActive();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CheckActive();

        private static void CheckActive()
        {
            if (Camera.main != null) return;
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null) return;

            var go = new GameObject("MainCamera-Auto");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            cam.cullingMask = ~0;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = 0;
            go.transform.position = new Vector3(0f, 1f, -10f);

            var listenerExisting = Object.FindFirstObjectByType<AudioListener>();
            if (listenerExisting == null)
                go.AddComponent<AudioListener>();
        }
    }
}
