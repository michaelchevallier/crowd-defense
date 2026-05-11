#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Fallback end-screen overlay (UGUI, no UIDocument required).
    // Auto-subscribes to LevelRunner.OnSummaryReady. Shows victory/defeat panel,
    // pause time during display, and wires Retry / Continue buttons.
    public sealed class EndScreenController : MonoSingleton<EndScreenController>
    {
        // ── UGUI refs (built at runtime) ────────────────────────────────────────
        private Canvas?    _canvas;
        private GameObject? _panel;
        private Text?      _titleText;
        private Text?      _subtitleText;
        private Button?    _btnPrimary;
        private Button?    _btnSecondary;
        private Text?      _btnPrimaryLabel;
        private Text?      _btnSecondaryLabel;

        // Stats grid (2 columns × 5 rows)
        private Text?      _statKills;
        private Text?      _statGold;
        private Text?      _statTowers;
        private Text?      _statTime;
        private Text?      _statWaves;

        private bool _isVictory;

        // ── Colours ─────────────────────────────────────────────────────────────
        private static readonly Color VictoryPanelColor  = new(0.10f, 0.08f, 0.02f, 0.92f);
        private static readonly Color VictoryTitleColor  = new(1.00f, 0.84f, 0.00f, 1.00f);
        private static readonly Color DefeatPanelColor   = new(0.20f, 0.02f, 0.02f, 0.92f);
        private static readonly Color DefeatTitleColor   = new(1.00f, 0.30f, 0.20f, 1.00f);
        private static readonly Color ButtonNormalColor  = new(0.15f, 0.15f, 0.15f, 1.00f);
        private static readonly Color ButtonTextColor    = new(1.00f, 1.00f, 1.00f, 1.00f);

        protected override void OnAwakeSingleton()
        {
            DontDestroyOnLoad(gameObject);
            BuildUI();

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady += OnSummaryReady;
        }

        protected override void OnDestroySingleton()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady -= OnSummaryReady;
        }

        private void OnSummaryReady(LevelResult result)
        {
            // If LevelSummaryController is present and handled it, do nothing.
            var existing = Object.FindFirstObjectByType<LevelSummaryController>();
            if (existing != null) return;

            if (result.IsVictory) ShowVictory(result);
            else ShowDefeat(result);
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void ShowVictory(LevelResult? result = null)
        {
            _isVictory = true;
            ApplyTheme(isVictory: true, result);
            Activate();
        }

        public void ShowDefeat(LevelResult? result = null)
        {
            _isVictory = false;
            ApplyTheme(isVictory: false, result);
            Activate();
        }

        // ── Internals ───────────────────────────────────────────────────────────

        private void ApplyTheme(bool isVictory, LevelResult? r)
        {
            var panelImg = _panel?.GetComponent<Image>();
            if (panelImg != null)
                panelImg.color = isVictory ? VictoryPanelColor : DefeatPanelColor;

            if (_titleText != null)
            {
                _titleText.text  = isVictory ? "VICTOIRE !" : "DEFAITE";
                _titleText.color = isVictory ? VictoryTitleColor : DefeatTitleColor;
            }

            if (_subtitleText != null && r != null)
            {
                if (isVictory)
                {
                    var starsStr = new string('*', r.StarsEarned);
                    var dotsStr  = new string('.', 3 - r.StarsEarned);
                    _subtitleText.text = starsStr + dotsStr;
                }
                else
                {
                    _subtitleText.text = $"Chateau : {r.CastleHPRemaining}/{r.CastleHPMax} PV";
                }
            }
            else if (_subtitleText != null)
            {
                _subtitleText.text = isVictory ? "Toutes les vagues vaincues !" : "Le chateau est tombe.";
            }

            if (r != null)
                PopulateStats(r);
            else
                ClearStats();

            if (_btnPrimaryLabel  != null) _btnPrimaryLabel.text  = "Rejouer";
            if (_btnSecondaryLabel != null)
                _btnSecondaryLabel.text = isVictory ? "Continuer" : "Menu";
        }

        private void Activate()
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(FadeInPanel());
        }

        private IEnumerator FadeInPanel()
        {
            const float duration = 0.5f;
            float elapsed = 0f;

            var panelImg    = _panel?.GetComponent<Image>();
            var btnRect1    = _btnPrimary?.GetComponent<RectTransform>();
            var btnRect2    = _btnSecondary?.GetComponent<RectTransform>();

            // Buttons start 60px below final position
            Vector2 btnOffset = new(0f, -60f);
            Vector2 btnFinal1 = btnRect1?.anchoredPosition ?? Vector2.zero;
            Vector2 btnFinal2 = btnRect2?.anchoredPosition ?? Vector2.zero;
            if (btnRect1 != null) btnRect1.anchoredPosition = btnFinal1 + btnOffset;
            if (btnRect2 != null) btnRect2.anchoredPosition = btnFinal2 + btnOffset;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t * t * (3f - 2f * t); // smoothstep

                if (panelImg != null)
                {
                    var c = panelImg.color;
                    panelImg.color = new Color(c.r, c.g, c.b, (_isVictory ? 0.92f : 0.92f) * eased);
                }
                if (btnRect1 != null) btnRect1.anchoredPosition = Vector2.Lerp(btnFinal1 + btnOffset, btnFinal1, eased);
                if (btnRect2 != null) btnRect2.anchoredPosition = Vector2.Lerp(btnFinal2 + btnOffset, btnFinal2, eased);

                if (_titleText != null)
                {
                    var c = _titleText.color;
                    _titleText.color = new Color(c.r, c.g, c.b, eased);
                }
                if (_subtitleText != null)
                {
                    var c = _subtitleText.color;
                    _subtitleText.color = new Color(c.r, c.g, c.b, eased);
                }

                foreach (var sl in new[] { _statKills, _statGold, _statTowers, _statTime, _statWaves })
                {
                    if (sl == null) continue;
                    var c = sl.color;
                    sl.color = new Color(c.r, c.g, c.b, eased);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Ensure final state
            if (panelImg != null)
            {
                var c = panelImg.color;
                panelImg.color = new Color(c.r, c.g, c.b, 0.92f);
            }
            if (btnRect1 != null) btnRect1.anchoredPosition = btnFinal1;
            if (btnRect2 != null) btnRect2.anchoredPosition = btnFinal2;

            foreach (var sl in new[] { _statKills, _statGold, _statTowers, _statTime, _statWaves })
            {
                if (sl == null) continue;
                var c = sl.color;
                sl.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        private void OnPrimaryClicked()
        {
            Time.timeScale = 1f;
            _panel?.SetActive(false);

            var id = LevelRunner.Instance?.CurrentLevel?.Id;
            if (!string.IsNullOrEmpty(id))
                LevelLoader.LoadLevel(id!);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnSecondaryClicked()
        {
            Time.timeScale = 1f;
            _panel?.SetActive(false);

            if (_isVictory)
            {
                var nextId = RunContext.Instance?.NextLevelId;
                if (!string.IsNullOrEmpty(nextId))
                {
                    RunContext.Instance!.AdvanceLevel(nextId!);
                    LevelLoader.LoadLevel(nextId!);
                }
                else
                {
                    LevelLoader.GoToWorldMap();
                }
            }
            else
            {
                LevelLoader.GoToWorldMap();
            }
        }

        // ── Stats helpers ───────────────────────────────────────────────────────

        private void PopulateStats(LevelResult r)
        {
            int minutes = (int)(r.PlaytimeSeconds / 60f);
            int seconds = (int)(r.PlaytimeSeconds % 60f);
            int totalWaves = WaveManager.Instance?.TotalWaves > 0 ? WaveManager.Instance.TotalWaves : r.WaveReached;

            if (_statKills  != null) _statKills.text  = $"Tues : {r.Kills}";
            if (_statGold   != null) _statGold.text   = $"Or gagne : {r.GoldEarned}c";
            if (_statTowers != null) _statTowers.text = $"Tours : {r.TowersPlaced}";
            if (_statTime   != null) _statTime.text   = $"Temps : {minutes}m {seconds:D2}s";
            if (_statWaves  != null) _statWaves.text  = $"Vagues : {r.WaveReached}/{totalWaves}";
        }

        private void ClearStats()
        {
            if (_statKills  != null) _statKills.text  = "";
            if (_statGold   != null) _statGold.text   = "";
            if (_statTowers != null) _statTowers.text = "";
            if (_statTime   != null) _statTime.text   = "";
            if (_statWaves  != null) _statWaves.text  = "";
        }

        // ── UGUI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            // Screen-space overlay canvas — no camera reference needed, always on top.
            var canvasGo = new GameObject("EndScreen_Canvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen dark backdrop
            var backdropGo = new GameObject("Backdrop");
            backdropGo.transform.SetParent(canvasGo.transform, false);
            var backdropRect = backdropGo.AddComponent<RectTransform>();
            Stretch(backdropRect);
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.55f);

            // Centred panel
            var panelGo = new GameObject("EndPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot     = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600f, 420f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = VictoryPanelColor;
            _panel = panelGo;

            var statColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Title
            _titleText = CreateLabel(panelGo.transform, "TitleLabel",
                anchorMin: new Vector2(0f, 0.76f),
                anchorMax: new Vector2(1f, 0.97f),
                fontSize: 52, color: VictoryTitleColor);

            // Subtitle (stars or castle HP)
            _subtitleText = CreateLabel(panelGo.transform, "SubtitleLabel",
                anchorMin: new Vector2(0.05f, 0.64f),
                anchorMax: new Vector2(0.95f, 0.77f),
                fontSize: 22, color: statColor);

            // Stats grid — 2 columns × 3 rows (col left: kills/towers/time, col right: gold/waves)
            // Row anchors: 0.48–0.62 / 0.34–0.48 / 0.20–0.34
            _statKills  = CreateLabel(panelGo.transform, "StatKills",
                anchorMin: new Vector2(0.04f, 0.48f), anchorMax: new Vector2(0.50f, 0.63f),
                fontSize: 18, color: statColor);
            _statGold   = CreateLabel(panelGo.transform, "StatGold",
                anchorMin: new Vector2(0.52f, 0.48f), anchorMax: new Vector2(0.97f, 0.63f),
                fontSize: 18, color: statColor);
            _statTowers = CreateLabel(panelGo.transform, "StatTowers",
                anchorMin: new Vector2(0.04f, 0.34f), anchorMax: new Vector2(0.50f, 0.49f),
                fontSize: 18, color: statColor);
            _statWaves  = CreateLabel(panelGo.transform, "StatWaves",
                anchorMin: new Vector2(0.52f, 0.34f), anchorMax: new Vector2(0.97f, 0.49f),
                fontSize: 18, color: statColor);
            _statTime   = CreateLabel(panelGo.transform, "StatTime",
                anchorMin: new Vector2(0.04f, 0.20f), anchorMax: new Vector2(0.97f, 0.35f),
                fontSize: 18, color: statColor);

            // Left-align individual stat labels
            foreach (var lbl in new[] { _statKills, _statGold, _statTowers, _statWaves, _statTime })
                if (lbl != null) lbl.alignment = TextAnchor.MiddleLeft;

            // Primary button (Rejouer) — left
            (_btnPrimary, _btnPrimaryLabel) = CreateButton(panelGo.transform, "BtnPrimary",
                anchorMin: new Vector2(0.05f, 0.03f),
                anchorMax: new Vector2(0.47f, 0.18f));
            _btnPrimary.onClick.AddListener(OnPrimaryClicked);

            // Secondary button (Continuer / Menu) — right
            (_btnSecondary, _btnSecondaryLabel) = CreateButton(panelGo.transform, "BtnSecondary",
                anchorMin: new Vector2(0.53f, 0.03f),
                anchorMax: new Vector2(0.95f, 0.18f));
            _btnSecondary.onClick.AddListener(OnSecondaryClicked);

            _panel.SetActive(false);
        }

        private static Text CreateLabel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text      = "";
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        private static (Button btn, Text label) CreateButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = ButtonNormalColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = ButtonNormalColor;
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f, 1f);
            colors.pressedColor     = new Color(0.08f, 0.08f, 0.08f, 1f);
            btn.colors = colors;

            var labelGo   = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            Stretch(labelRect);
            var txt = labelGo.AddComponent<Text>();
            txt.text      = "";
            txt.fontSize  = 20;
            txt.color     = ButtonTextColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return (btn, txt);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
