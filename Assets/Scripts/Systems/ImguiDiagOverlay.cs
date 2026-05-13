#nullable enable
using UnityEngine;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// IMGUI diagnostic overlay attached to the auto-Camera GameObject. Renders via
    /// Unity's legacy GUI system (OnGUI), which is independent of URP shaders /
    /// UI Toolkit / PanelSettings. If THIS shows on WebGL but UI Toolkit doesn't,
    /// then the failure is specifically in the URP+UIToolkit composition path
    /// (Hidden/CoreSRP/CoreCopy unsupported on WebGL2 subpass inputs).
    ///
    /// Spawned automatically by EnsureMainCamera so we get visual feedback on the
    /// canvas without needing Menu.unity's UIDocument chain to work.
    /// </summary>
    public class ImguiDiagOverlay : MonoBehaviour
    {
        private GUIStyle? _bannerStyle;
        private GUIStyle? _boxStyle;
        private GUIStyle? _buttonStyle;
        private string _status = "Booting…";
        private int _frameCount;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _status = $"Active scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
        }

        private void Update()
        {
            _frameCount++;
        }

        private void OnGUI()
        {
            _bannerStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f, 1f) },
            };
            _boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
                padding = new RectOffset(12, 12, 12, 12),
            };
            _buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
            };

            int W = Screen.width;
            int H = Screen.height;
            GUI.Label(new Rect(0, 18, W, 40), "CROWD DEFENSE — IMGUI DIAG (iter#12)", _bannerStyle);

            int boxW = 520, boxH = 220;
            GUI.Box(new Rect((W - boxW) / 2f, 70, boxW, boxH),
                $"Engine OK\nFrame: {_frameCount}\nResolution: {W}×{H}\nGfx: {SystemInfo.graphicsDeviceName}\nShader level: {SystemInfo.graphicsShaderLevel}\nStatus: {_status}\n\nThis text proves IMGUI renders. If UIToolkit\nMainMenu does not, URP+WebGL composition is\nthe blocker (Hidden/CoreSRP/CoreCopy).",
                _boxStyle);

            int btnW = 220, btnH = 60, btnY = 320;
            if (GUI.Button(new Rect((W - btnW) / 2f, btnY, btnW, btnH), "PLAY  (test)", _buttonStyle))
            {
                _status = "Button clicked at frame " + _frameCount;
                Debug.Log("[ImguiDiagOverlay] Test button clicked");
            }
        }
    }
}
