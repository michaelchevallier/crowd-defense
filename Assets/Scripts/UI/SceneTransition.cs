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
            // V8 FIX: overlay must not block raycasts when invisible — otherwise
            // it eats all UI Toolkit clicks on the underlying scene panels.
            // raycastTarget is toggled ON only during active fades (see Fade/LoadSceneFade).
            _overlay.raycastTarget = false;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Loading UI group (progress bar + tip), positioned bottom-center.
            // V8 FIX: explicitly request RectTransform — CanvasGroup alone does NOT
            // auto-add it like Image/Slider/Text do, leaving GetComponent<RectTransform>
            // returning null on plain Transform → NRE on anchor assignments.
            var loadingGo = new GameObject("LoadingGroup", typeof(RectTransform));
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

        public void LoadSceneFade(string sceneName, Color fadeColor = default, float fadeDur = 0.5f)
        {
            if (_overlay == null)
            {
                Debug.LogError("[SceneTransition.LoadSceneFade] _overlay is null — Awake/BuildOverlay may not have completed. Skipping load.");
                return;
            }
            if (fadeColor == default) fadeColor = Color.black;
            if (_isLoading) return;
            _isLoading = true;
            StartCoroutine(FadeAndLoad(sceneName, fadeColor, fadeDur));
        }

        IEnumerator FadeAndLoad(string sceneName, Color fadeColor, float fadeDur)
        {
            _busy = true;
            Debug.Log($"[SceneTransition] FadeAndLoad start: scene='{sceneName}'");

            if (_overlay == null)
            {
                Debug.LogError("[SceneTransition] _overlay is null in FadeAndLoad — BuildOverlay not called");
                _busy = false;
                _isLoading = false;
                yield break;
            }

            _overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

            // Start async load immediately, hold activation
            Debug.Log($"[SceneTransition] Starting LoadSceneAsync('{sceneName}')");
            _loadingOp = SceneManager.LoadSceneAsync(sceneName);
            if (_loadingOp == null)
            {
                Debug.LogError($"[SceneTransition] Failed to load scene '{sceneName}' — check EditorBuildSettings.scenes");
                _busy = false;
                _isLoading = false;
                yield break;
            }
            _loadingOp.allowSceneActivation = false;
            Debug.Log($"[SceneTransition] LoadSceneAsync success, allowSceneActivation=false");

            // Wait up to 500 ms — skip loading UI entirely if load is already done
            float waited = 0f;
            const float ShowThreshold = 0.5f;
            while (waited < ShowThreshold && _loadingOp.progress < 0.9f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            bool showLoadingUi = _loadingOp.progress < 0.9f;
            Debug.Log($"[SceneTransition] Pre-fade checkpoint: showLoadingUi={showLoadingUi}, progress={_loadingOp.progress}");

            // V8 FIX: enable raycast blocking during active fade so clicks
            // don't slip through and trigger UI on the scene we're leaving.
            _overlay.raycastTarget = true;

            // Fade to color
            Debug.Log($"[SceneTransition] Starting Fade(0→1, dur={fadeDur})");
            yield return StartCoroutine(Fade(0f, 1f, fadeDur));
            Debug.Log($"[SceneTransition] Fade complete");

            if (showLoadingUi)
            {
                // Show loading UI with tip
                _tipLabel.text = PickTip();
                _progressBar.value = waited < ShowThreshold ? _loadingOp.progress / 0.9f : 0f;

                // Fade in loading group
                yield return StartCoroutine(FadeGroup(_loadingGroup, 0f, 1f, 0.2f));
            }

            if (showLoadingUi)
            {
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
            }

            Debug.Log($"[SceneTransition] Activating scene '{sceneName}'");
            _loadingOp.allowSceneActivation = true;
            _loadingOp = null;
            Debug.Log($"[SceneTransition] Scene activated, waiting for new scene to initialize");
            yield return new WaitForSeconds(0.1f); // Brief pause for scene Awake/Start

            // Fade out overlay, keep it transparent (alpha 0) so it doesn't cover the new scene.
            // Previous bug: setting _overlay.color = Color.black after fade-out re-set alpha=1
            // (Color.black = rgba(0,0,0,1)), making the overlay fully opaque black and hiding the
            // newly loaded scene. Fix: explicitly set color with alpha=0 (transparent black).
            Debug.Log($"[SceneTransition] Starting fade-out (1→0)");
            yield return StartCoroutine(Fade(1f, 0f, FadeDuration));
            if (_overlay != null)
            {
                _overlay.color = new Color(0f, 0f, 0f, 0f);
                // V8 FIX: disable raycast blocking now that we're invisible, so
                // the underlying scene's UI Toolkit / UGUI clicks work normally.
                _overlay.raycastTarget = false;
                Debug.Log($"[SceneTransition] Fade-out complete, transition finished (overlay alpha forced to 0, raycastTarget=false)");
            }
            else
            {
                Debug.LogError($"[SceneTransition] _overlay is null during fade-out");
            }

            _busy = false;
            _isLoading = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f) { SetAlpha(to); yield break; }
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
            Debug.Log("[SceneTransition.EnsureExists] Creating new SceneTransition GameObject");
            try
            {
                var go = new GameObject("SceneTransition");
                if (go == null)
                {
                    Debug.LogError("[SceneTransition.EnsureExists] Failed to create GameObject");
                    return;
                }
                var component = go.AddComponent<SceneTransition>();
                if (component == null)
                {
                    Debug.LogError("[SceneTransition.EnsureExists] Failed to add SceneTransition component");
                    Destroy(go);
                    return;
                }
                Debug.Log("[SceneTransition.EnsureExists] SceneTransition created successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneTransition.EnsureExists] Exception: {ex}");
            }
        }
    }
}
