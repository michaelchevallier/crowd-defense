#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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

            EnsureDiagnosticOverlay();
        }

        // Diagnostic UI overlay : proves UIToolkit pipeline + PanelSettings work
        // independent of any UXML/USS asset reference. If this shows on screen but
        // MainMenu UI doesn't, the issue is in the MainMenu UXML/USS asset chain.
        private static void EnsureDiagnosticOverlay()
        {
            // Skip if an existing UIDocument with visible content is already present
            var existing = Object.FindFirstObjectByType<UIDocument>();
            if (existing != null && existing.rootVisualElement != null)
            {
                Debug.Log($"[EnsureMainCamera] UIDocument present, root childCount={existing.rootVisualElement.childCount}");
            }

            var diagGo = new GameObject("DiagOverlay");
            var doc = diagGo.AddComponent<UIDocument>();
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.sortingOrder = 9999;
            doc.panelSettings = settings;

            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[EnsureMainCamera] DiagOverlay: rootVisualElement still null after panelSettings assign");
                return;
            }

            var banner = new Label("CROWD DEFENSE — DIAGNOSTIC OVERLAY (iter#11)");
            banner.style.position = Position.Absolute;
            banner.style.top = 20;
            banner.style.left = 0;
            banner.style.right = 0;
            banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            banner.style.fontSize = 28;
            banner.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f, 1f));
            banner.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(banner);

            var box = new VisualElement();
            box.style.position = Position.Absolute;
            box.style.top = 80;
            box.style.left = new Length(50f, LengthUnit.Percent);
            box.style.translate = new StyleTranslate(new Translate(new Length(-200, LengthUnit.Pixel), 0));
            box.style.width = 400;
            box.style.height = 240;
            box.style.backgroundColor = new StyleColor(new Color(0.15f, 0.25f, 0.4f, 0.95f));
            box.style.borderTopWidth = 2; box.style.borderBottomWidth = 2; box.style.borderLeftWidth = 2; box.style.borderRightWidth = 2;
            box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new StyleColor(new Color(0.5f, 0.7f, 1f, 1f));
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            root.Add(box);

            var status = new Label("If you see this, UI Toolkit + URP work.\nMainMenu UXML/USS is the broken layer.");
            status.style.color = new StyleColor(Color.white);
            status.style.fontSize = 18;
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(status);

            Object.DontDestroyOnLoad(diagGo);
            Debug.Log("[EnsureMainCamera] DiagOverlay injected");
        }
    }
}
