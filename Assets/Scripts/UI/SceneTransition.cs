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
        const int TipCount = 15;

        Image _overlay = null!;
        Slider _progressBar = null!;
        Text _tipLabel = null!;
        CanvasGroup _loadingGroup = null!;
        Image _spinner = null!;
        bool _busy;
        bool _isLoading;
        int _lastTipIdx = -1;
        AsyncOperation? _loadingOp;

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
            _loadingGroup.alpha = 0f;
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Full-screen black fade panel
            var panel = new GameObject("FadePanel");
            panel.transform.SetParent(transform, false);
            _overlay = panel.AddComponent<Image>();
            _overlay.color = Color.black;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Loading UI group (progress bar + tip), positioned bottom-center
            var loadingGo = new GameObject("LoadingGroup");
            loadingGo.transform.SetParent(transform, false);
            _loadingGroup = loadingGo.AddComponent<CanvasGroup>();
            _loadingGroup.interactable = false;
            _loadingGroup.blocksRaycasts = false;
            var lgRect = loadingGo.GetComponent<RectTransform>();
            lgRect.anchorMin = new Vector2(0.5f, 0f);
            lgRect.anchorMax = new Vector2(0.5f, 0f);
            lgRect.pivot = new Vector2(0.5f, 0f);
            lgRect.sizeDelta = new Vector2(600f, 80f);
            lgRect.anchoredPosition = new Vector2(0f, 40f);

            // Tip label
            var tipGo = new GameObject("TipLabel");
            tipGo.transform.SetParent(loadingGo.transform, false);
            _tipLabel = tipGo.AddComponent<Text>();
            _tipLabel.fontSize = 16;
            _tipLabel.fontStyle = FontStyle.Italic;
            _tipLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            _tipLabel.alignment = TextAnchor.MiddleCenter;
            _tipLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tipRect = tipGo.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0f, 0.4f);
            tipRect.anchorMax = Vector2.one;
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;

            // Progress bar background
            var barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(loadingGo.transform, false);
            var barBg = barBgGo.AddComponent<Image>();
            barBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0.35f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;

            // Slider (progress bar)
            var sliderGo = new GameObject("ProgressBar");
            sliderGo.transform.SetParent(loadingGo.transform, false);
            _progressBar = sliderGo.AddComponent<Slider>();
            _progressBar.minValue = 0f;
            _progressBar.maxValue = 1f;
            _progressBar.value = 0f;
            _progressBar.interactable = false;
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0.35f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Slider fill area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _progressBar.fillRect = fillRect;

            // Spinner: 32x32 arc circle, centered above progress bar
            var spinnerGo = new GameObject("Spinner");
            spinnerGo.transform.SetParent(loadingGo.transform, false);
            _spinner = spinnerGo.AddComponent<Image>();
            _spinner.color = new Color(1f, 1f, 1f, 0.9f);
            // Build a thin ring via filled radial image (270° arc)
            _spinner.type = Image.Type.Filled;
            _spinner.fillMethod = Image.FillMethod.Radial360;
            _spinner.fillAmount = 0.75f;
            var spinRect = spinnerGo.GetComponent<RectTransform>();
            spinRect.anchorMin = new Vector2(0.5f, 1f);
            spinRect.anchorMax = new Vector2(0.5f, 1f);
            spinRect.pivot = new Vector2(0.5f, 0f);
            spinRect.sizeDelta = new Vector2(32f, 32f);
            spinRect.anchoredPosition = new Vector2(0f, 8f);
        }

        void Update()
        {
            if (_busy)
                _spinner.rectTransform.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
        }

        void SetAlpha(float a)
        {
            var c = _overlay.color;
            c.a = a;
            _overlay.color = c;
        }

        string PickTip()
        {
            int idx;
            do { idx = Random.Range(0, TipCount); }
            while (idx == _lastTipIdx && TipCount > 1);
            _lastTipIdx = idx;
            return L.Get($"tip.{idx}");
        }

        public void LoadSceneFade(string sceneName)
        {
            if (_isLoading) return;
            _isLoading = true;
            StartCoroutine(FadeAndLoad(sceneName));
        }

        IEnumerator FadeAndLoad(string sceneName)
        {
            _busy = true;

            // Fade to black
            yield return StartCoroutine(Fade(0f, 1f, FadeDuration));

            // Show loading UI with tip
            _tipLabel.text = PickTip();
            _progressBar.value = 0f;

            // Start async load, hold activation
            _loadingOp = SceneManager.LoadSceneAsync(sceneName);
            _loadingOp.allowSceneActivation = false;

            // Fade in loading group
            yield return StartCoroutine(FadeGroup(_loadingGroup, 0f, 1f, 0.2f));

            // Update progress until Unity's 0.9 cap
            while (_loadingOp.progress < 0.9f)
            {
                _progressBar.value = _loadingOp.progress / 0.9f;
                yield return null;
            }

            // Smooth fill to 100%
            yield return StartCoroutine(SmoothProgress(_progressBar.value, 1f, 0.3f));

            // Brief pause at 100% so player sees it
            yield return new WaitForSecondsRealtime(0.15f);

            // Fade out loading group, then activate scene
            yield return StartCoroutine(FadeGroup(_loadingGroup, 1f, 0f, 0.2f));
            _loadingOp.allowSceneActivation = true;
            _loadingOp = null;

            // Fade out black overlay
            yield return StartCoroutine(Fade(1f, 0f, FadeDuration));
            _busy = false;
            _isLoading = false;
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

        IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        IEnumerator SmoothProgress(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _progressBar.value = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _progressBar.value = to;
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
