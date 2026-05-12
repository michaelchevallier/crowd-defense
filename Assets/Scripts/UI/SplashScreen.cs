#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrowdDefense.UI
{
    public class SplashScreen : MonoBehaviour
    {
        const string SkipKey = "skip_splash_v1";
        const float FadeIn = 0.4f;
        const float Hold = 1.2f;
        const float FadeOut = 0.4f;
        const string TargetScene = "Menu";

        CanvasGroup _group = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (PlayerPrefs.GetInt(SkipKey, 0) == 1) return;

            // Only run when no Menu/Main scene is already active (i.e. first boot)
            string active = SceneManager.GetActiveScene().name;
            if (active == "Menu" || active == "Main") return;

            var go = new GameObject("SplashScreen");
            DontDestroyOnLoad(go);
            go.AddComponent<SplashScreen>();
        }

        void Awake()
        {
            BuildCanvas();
            StartCoroutine(RunSequence());
        }

        void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.1f, 1f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Root CanvasGroup for fade
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // Logo text
            var logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(transform, false);
            var logoText = logoGo.AddComponent<Text>();
            logoText.text = "CROWD DEFENSE";
            logoText.fontSize = 72;
            logoText.fontStyle = FontStyle.Bold;
            logoText.color = Color.white;
            logoText.alignment = TextAnchor.MiddleCenter;
            var logoRect = logoGo.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0f, 0.45f);
            logoRect.anchorMax = new Vector2(1f, 0.65f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            // Tagline text
            var tagGo = new GameObject("Tagline");
            tagGo.transform.SetParent(transform, false);
            var tagText = tagGo.AddComponent<Text>();
            tagText.text = "Tower Defense Game";
            tagText.fontSize = 24;
            tagText.fontStyle = FontStyle.Italic;
            tagText.color = new Color(0.75f, 0.75f, 0.85f, 1f);
            tagText.alignment = TextAnchor.MiddleCenter;
            var tagRect = tagGo.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0f, 0.35f);
            tagRect.anchorMax = new Vector2(1f, 0.45f);
            tagRect.offsetMin = Vector2.zero;
            tagRect.offsetMax = Vector2.zero;
        }

        IEnumerator RunSequence()
        {
            yield return StartCoroutine(FadeGroup(0f, 1f, FadeIn));
            yield return new WaitForSecondsRealtime(Hold);
            yield return StartCoroutine(FadeGroup(1f, 0f, FadeOut));

            PlayerPrefs.SetInt(SkipKey, 1);
            PlayerPrefs.Save();

            Destroy(gameObject);
            SceneManager.LoadScene(TargetScene);
        }

        IEnumerator FadeGroup(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
