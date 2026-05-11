#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Ensures PanelSettings.themeStyleSheet is loaded at runtime.
    /// WebGL builds may strip the theme asset if it's not in Resources/.
    /// This fallback loads it from Resources/UI/ if null.
    /// </summary>
    internal class RuntimeThemeFixup : MonoBehaviour
    {
        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.panelSettings == null) return;

            if (doc.panelSettings.themeStyleSheet == null)
            {
                var theme = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme");
                if (theme != null)
                {
                    doc.panelSettings.themeStyleSheet = theme;
                }
                else
                {
                    Debug.LogWarning("[RuntimeThemeFixup] Theme not found in Resources/UI/UnityDefaultRuntimeTheme");
                }
            }
        }
    }
}
