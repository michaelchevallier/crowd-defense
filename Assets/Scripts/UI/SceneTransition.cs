#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrowdDefense.UI
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition? Instance { get; private set; }

        const float FadeDuration = 0.4f;

        Image _overlay = null!;
        bool _busy;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
            SetAlpha(0f);
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("FadePanel");
            panel.transform.SetParent(transform, false);
            _overlay = panel.AddComponent<Image>();
            _overlay.color = Color.black;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void SetAlpha(float a)
        {
            var c = _overlay.color;
            c.a = a;
            _overlay.color = c;
        }

        public void LoadSceneFade(string sceneName)
        {
            if (_busy) return;
            StartCoroutine(FadeAndLoad(sceneName));
        }

        IEnumerator FadeAndLoad(string sceneName)
        {
            _busy = true;
            yield return StartCoroutine(Fade(0f, 1f, FadeDuration));
            SceneManager.LoadScene(sceneName);
            yield return StartCoroutine(Fade(1f, 0f, FadeDuration));
            _busy = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            SetAlpha(to);
        }

        // Auto-instanciate from any code path before Instance is available.
        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("SceneTransition");
            go.AddComponent<SceneTransition>();
        }
    }
}
