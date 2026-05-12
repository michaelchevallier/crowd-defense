#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Entities;

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

        // Top-3 tower leaderboard
        private Text?      _lbHeader;
        private Text?      _lbRow1;
        private Text?      _lbRow2;
        private Text?      _lbRow3;

        // Bar chart "Kills par vague"
        private RectTransform? _chartArea;
        private readonly System.Collections.Generic.List<(Image bar, Text label)> _chartBars = new();

        // Trophy / medal icon (victory only)
        private Text?          _trophyText;
        private RectTransform? _trophyRect;

        // Share buttons (victory only)
        private GameObject? _shareRow;
        private Button?     _btnDiscord;
        private Button?     _btnTwitter;
        private Text?       _toastText;
        private Coroutine?  _toastCoroutine;
        private string      _shareLevelName = "";

        private bool _isVictory;

        // ── Colours ─────────────────────────────────────────────────────────────
        private static readonly Color VictoryPanelColor  = new(0.10f, 0.08f, 0.02f, 0.92f);
        private static readonly Color VictoryTitleColor  = new(1.00f, 0.84f, 0.00f, 1.00f);
        private static readonly Color DefeatPanelColor   = new(0.20f, 0.02f, 0.02f, 0.92f);
        private static readonly Color DefeatTitleColor   = new(1.00f, 0.30f, 0.20f, 1.00f);
        private static readonly Color ButtonNormalColor  = new(0.15f, 0.15f, 0.15f, 1.00f);
        private static readonly Color ButtonTextColor    = new(1.00f, 1.00f, 1.00f, 1.00f);
        private static readonly Color LbHeaderColor      = new(1.00f, 0.84f, 0.00f, 1.00f);
        private static readonly Color LbRowColor         = new(0.90f, 0.90f, 0.90f, 1.00f);
        private static readonly Color ShareDiscordColor  = new(0.27f, 0.31f, 0.70f, 1.00f);
        private static readonly Color ShareTwitterColor  = new(0.11f, 0.63f, 0.95f, 1.00f);
        private static readonly Color ToastColor         = new(0.10f, 0.70f, 0.30f, 1.00f);

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

            if (_trophyText != null)
            {
                if (isVictory && r != null)
                {
                    _trophyText.text = r.StarsEarned >= 3 ? "\U0001F947"
                                     : r.StarsEarned == 2 ? "\U0001F948"
                                     :                      "\U0001F949";
                    _trophyText.gameObject.SetActive(true);
                }
                else
                {
                    _trophyText.gameObject.SetActive(false);
                }
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

            // Cache level name for share messages
            _shareLevelName = LevelRunner.Instance?.CurrentLevel?.DisplayName
                           ?? LevelRunner.Instance?.CurrentLevel?.Id
                           ?? "ce niveau";

            if (_btnPrimaryLabel  != null) _btnPrimaryLabel.text  = "Rejouer";
            if (_btnSecondaryLabel != null)
                _btnSecondaryLabel.text = isVictory ? "Continuer" : "Menu";

            // Show share row only on victory
            if (_shareRow != null)
                _shareRow.SetActive(isVictory);
            if (_toastText != null)
                _toastText.gameObject.SetActive(false);
        }

        private void Activate()
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(FadeInPanel());
            if (_isVictory && _trophyRect != null)
                StartCoroutine(AnimateTrophy());
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
                    panelImg.color = new Color(c.r, c.g, c.b, 0.92f * eased);
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

                foreach (var sl in new[] { _lbHeader, _lbRow1, _lbRow2, _lbRow3 })
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

            foreach (var sl in new[] { _lbHeader, _lbRow1, _lbRow2, _lbRow3 })
            {
                if (sl == null) continue;
                var c = sl.color;
                sl.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        // Trophy: scale 0→1.3→1 (0.8s) + rotate Z 360° (1.5s) + gold glow pulse (loop)
        private IEnumerator AnimateTrophy()
        {
            if (_trophyRect == null || _trophyText == null) yield break;

            const float scaleDur  = 0.8f;
            const float rotateDur = 1.5f;
            const float totalDur  = rotateDur;

            _trophyRect.localScale    = Vector3.zero;
            _trophyRect.localRotation = Quaternion.identity;

            float elapsed = 0f;
            while (elapsed < totalDur)
            {
                float t = elapsed / totalDur;
                float unscaled = Time.unscaledDeltaTime;

                // Scale: 0 → 1.3 → 1 over scaleDur
                if (elapsed < scaleDur)
                {
                    float st = elapsed / scaleDur;
                    float scale = st < 0.75f
                        ? Mathf.Lerp(0f, 1.3f, st / 0.75f)
                        : Mathf.Lerp(1.3f, 1.0f, (st - 0.75f) / 0.25f);
                    _trophyRect.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    _trophyRect.localScale = Vector3.one;
                }

                // Rotate Z: 0 → 360° over rotateDur
                float angle = Mathf.Lerp(0f, 360f, t);
                _trophyRect.localRotation = Quaternion.Euler(0f, 0f, angle);

                elapsed += unscaled;
                yield return null;
            }

            _trophyRect.localScale    = Vector3.one;
            _trophyRect.localRotation = Quaternion.identity;

            // Gold glow pulse loop (alpha 1 → 0.55 → 1, 1.2s period)
            var baseColor = new Color(1.00f, 0.84f, 0.00f, 1.00f);
            var dimColor  = new Color(1.00f, 0.84f, 0.00f, 0.55f);
            while (_trophyText != null && _trophyText.gameObject.activeInHierarchy)
            {
                float phase = Mathf.PingPong(Time.unscaledTime, 0.6f) / 0.6f;
                _trophyText.color = Color.Lerp(baseColor, dimColor, phase);
                yield return null;
            }
        }

        // ── Share handlers ──────────────────────────────────────────────────────

        private void OnDiscordShareClicked()
        {
            string msg = $"J'ai conquis {_shareLevelName} avec 3 etoiles ! crowd-defense.io";
            GUIUtility.systemCopyBuffer = msg;
            ShowToast("Lien copie !");
        }

        private void OnTwitterShareClicked()
        {
            string text      = $"J'ai conquis {_shareLevelName} avec 3 etoiles sur Crowd Defense !";
            string url       = "https://crowd-defense.io";
            string encoded    = System.Uri.EscapeDataString(text);
            string urlEncoded = System.Uri.EscapeDataString(url);
            Application.OpenURL($"https://twitter.com/intent/tweet?text={encoded}&url={urlEncoded}");
        }

        private void ShowToast(string message)
        {
            if (_toastText == null) return;
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            if (_toastText == null) yield break;
            _toastText.text  = message;
            _toastText.color = new Color(ToastColor.r, ToastColor.g, ToastColor.b, 0f);
            _toastText.gameObject.SetActive(true);

            float t = 0f;
            while (t < 0.25f) { _toastText.color = new Color(ToastColor.r, ToastColor.g, ToastColor.b, t / 0.25f); t += Time.unscaledDeltaTime; yield return null; }
            _toastText.color = new Color(ToastColor.r, ToastColor.g, ToastColor.b, 1f);

            float held = 0f;
            while (held < 1.5f) { held += Time.unscaledDeltaTime; yield return null; }

            t = 0f;
            while (t < 0.4f) { _toastText.color = new Color(ToastColor.r, ToastColor.g, ToastColor.b, 1f - t / 0.4f); t += Time.unscaledDeltaTime; yield return null; }
            _toastText.gameObject.SetActive(false);
            _toastCoroutine = null;
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

            RefreshKillsChart();
            PopulateTowerLeaderboard();
        }

        private void PopulateTowerLeaderboard()
        {
            var placed = PlacementController.Instance?.PlacedTowers;
            if (placed == null || placed.Count == 0)
            {
                if (_lbHeader != null) _lbHeader.text = "";
                if (_lbRow1   != null) _lbRow1.text   = "";
                if (_lbRow2   != null) _lbRow2.text   = "";
                if (_lbRow3   != null) _lbRow3.text   = "";
                return;
            }

            var top3 = placed
                .Where(t => t != null && t.Config != null)
                .OrderByDescending(t => t.TotalKills)
                .Take(3)
                .ToList();

            if (_lbHeader != null) _lbHeader.text = "Top Tours";

            var rows = new[] { _lbRow1, _lbRow2, _lbRow3 };
            string[] medals = { "1.", "2.", "3." };
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                if (i < top3.Count)
                {
                    var t = top3[i];
                    string name = t.Config!.DisplayName;
                    int lvl = t.UpgradeLevel;
                    int kills = t.TotalKills;
                    rows[i]!.text = $"{medals[i]} {name} L{lvl}  -  {kills} kills";
                }
                else
                {
                    rows[i]!.text = "";
                }
            }
        }

        // Panel is 600x560. Chart anchor spans x:[0.04,0.96] y:[0.42,0.53].
        private const float ChartW = 600f * (0.96f - 0.04f);   // 552
        private const float ChartH = 560f * (0.53f - 0.42f);   // 61.6

        private void RefreshKillsChart()
        {
            if (_chartArea == null) return;

            var tracker = KillsPerWaveTracker.Instance;
            var data    = tracker?.KillsByWave;
            int maxK    = tracker?.MaxKillsInAnyWave ?? 0;

            int waveCount = WaveManager.Instance?.TotalWaves ?? 0;
            if (data != null)
                foreach (var kv in data)
                    if (kv.Key + 1 > waveCount) waveCount = kv.Key + 1;

            if (waveCount <= 0 || maxK <= 0)
            {
                foreach (var (bar, lbl) in _chartBars) { bar.gameObject.SetActive(false); lbl.gameObject.SetActive(false); }
                return;
            }

            while (_chartBars.Count < waveCount)
            {
                var barGo  = new GameObject($"Bar{_chartBars.Count}");
                barGo.transform.SetParent(_chartArea, false);
                var barImg = barGo.AddComponent<Image>();
                barImg.color = new Color(0.20f, 0.75f, 0.35f, 0.85f);

                var lblGo  = new GameObject($"Lbl{_chartBars.Count}");
                lblGo.transform.SetParent(_chartArea, false);
                var lblTxt = lblGo.AddComponent<Text>();
                lblTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblTxt.fontSize  = 10;
                lblTxt.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
                lblTxt.alignment = TextAnchor.UpperCenter;

                _chartBars.Add((barImg, lblTxt));
            }

            const float labelH = 18f;
            const float barPad = 2f;
            float barW = ChartW / waveCount;

            for (int i = 0; i < _chartBars.Count; i++)
            {
                var (bar, lbl) = _chartBars[i];
                if (i >= waveCount) { bar.gameObject.SetActive(false); lbl.gameObject.SetActive(false); continue; }

                bar.gameObject.SetActive(true);
                lbl.gameObject.SetActive(true);

                int kills  = (data != null && data.TryGetValue(i, out int k)) ? k : 0;
                float barH = Mathf.Max(2f, (ChartH - labelH) * ((float)kills / maxK));

                var barRect = bar.GetComponent<RectTransform>();
                barRect.anchorMin        = Vector2.zero;
                barRect.anchorMax        = Vector2.zero;
                barRect.pivot            = new Vector2(0.5f, 0f);
                barRect.sizeDelta        = new Vector2(barW - barPad * 2f, barH);
                barRect.anchoredPosition = new Vector2(i * barW + barW * 0.5f, 0f);

                var lblRect = lbl.GetComponent<RectTransform>();
                lblRect.anchorMin        = Vector2.zero;
                lblRect.anchorMax        = Vector2.zero;
                lblRect.pivot            = new Vector2(0.5f, 0f);
                lblRect.sizeDelta        = new Vector2(barW, labelH);
                lblRect.anchoredPosition = new Vector2(i * barW + barW * 0.5f, barH);

                lbl.text = kills > 0 ? $"V{i + 1}\n{kills}" : $"V{i + 1}";
            }
        }

        private void ClearStats()
        {
            if (_statKills  != null) _statKills.text  = "";
            if (_statGold   != null) _statGold.text   = "";
            if (_statTowers != null) _statTowers.text = "";
            if (_statTime   != null) _statTime.text   = "";
            if (_statWaves  != null) _statWaves.text  = "";
            if (_lbHeader   != null) _lbHeader.text   = "";
            if (_lbRow1     != null) _lbRow1.text     = "";
            if (_lbRow2     != null) _lbRow2.text     = "";
            if (_lbRow3     != null) _lbRow3.text     = "";
            foreach (var (bar, lbl) in _chartBars) { bar.gameObject.SetActive(false); lbl.gameObject.SetActive(false); }
        }

        // ── UGUI construction ───────────────────────────────────────────────────
        // Panel: 600x560
        // Layout (y anchors, bottom=0 top=1):
        //   Buttons      0.02 – 0.12
        //   LB row 3     0.12 – 0.20
        //   LB row 2     0.20 – 0.28
        //   LB row 1     0.28 – 0.36
        //   LB header    0.36 – 0.43
        //   Bar chart    0.31 – 0.42  (overlaps LB header base — shifted)
        //   Wait — chart placed at 0.43 – 0.54
        //   StatTime     0.54 – 0.63
        //   StatTowers/Waves 0.63 – 0.73
        //   StatKills/Gold   0.73 – 0.83
        //   Subtitle     0.83 – 0.91
        //   Title        0.91 – 1.00

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

            // Centred panel — expanded to 560px height to fit leaderboard
            var panelGo = new GameObject("EndPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot     = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600f, 560f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = VictoryPanelColor;
            _panel = panelGo;

            var statColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Trophy / medal icon — left of title, victory only
            _trophyText = CreateLabel(panelGo.transform, "TrophyIcon",
                anchorMin: new Vector2(0.01f, 0.88f),
                anchorMax: new Vector2(0.22f, 1.00f),
                fontSize: 52, color: new Color(1.00f, 0.84f, 0.00f, 1.00f));
            _trophyRect = _trophyText.GetComponent<RectTransform>();
            _trophyText.gameObject.SetActive(false);

            // Title
            _titleText = CreateLabel(panelGo.transform, "TitleLabel",
                anchorMin: new Vector2(0f, 0.91f),
                anchorMax: new Vector2(1f, 1.00f),
                fontSize: 48, color: VictoryTitleColor);

            // Subtitle (stars or castle HP)
            _subtitleText = CreateLabel(panelGo.transform, "SubtitleLabel",
                anchorMin: new Vector2(0.05f, 0.82f),
                anchorMax: new Vector2(0.95f, 0.91f),
                fontSize: 20, color: statColor);

            // Stats grid — 2 columns x 3 rows
            _statKills  = CreateLabel(panelGo.transform, "StatKills",
                anchorMin: new Vector2(0.04f, 0.72f), anchorMax: new Vector2(0.50f, 0.82f),
                fontSize: 17, color: statColor);
            _statGold   = CreateLabel(panelGo.transform, "StatGold",
                anchorMin: new Vector2(0.52f, 0.72f), anchorMax: new Vector2(0.97f, 0.82f),
                fontSize: 17, color: statColor);
            _statTowers = CreateLabel(panelGo.transform, "StatTowers",
                anchorMin: new Vector2(0.04f, 0.62f), anchorMax: new Vector2(0.50f, 0.72f),
                fontSize: 17, color: statColor);
            _statWaves  = CreateLabel(panelGo.transform, "StatWaves",
                anchorMin: new Vector2(0.52f, 0.62f), anchorMax: new Vector2(0.97f, 0.72f),
                fontSize: 17, color: statColor);
            _statTime   = CreateLabel(panelGo.transform, "StatTime",
                anchorMin: new Vector2(0.04f, 0.53f), anchorMax: new Vector2(0.97f, 0.62f),
                fontSize: 17, color: statColor);

            // Left-align individual stat labels
            foreach (var lbl in new[] { _statKills, _statGold, _statTowers, _statWaves, _statTime })
                if (lbl != null) lbl.alignment = TextAnchor.MiddleLeft;

            // Bar chart "Kills par vague"
            var chartGo = new GameObject("KillsChart");
            chartGo.transform.SetParent(panelGo.transform, false);
            _chartArea = chartGo.AddComponent<RectTransform>();
            _chartArea.anchorMin = new Vector2(0.04f, 0.42f);
            _chartArea.anchorMax = new Vector2(0.96f, 0.53f);
            _chartArea.offsetMin = Vector2.zero;
            _chartArea.offsetMax = Vector2.zero;

            // Top-3 tower leaderboard — below chart
            _lbHeader = CreateLabel(panelGo.transform, "LbHeader",
                anchorMin: new Vector2(0.04f, 0.35f), anchorMax: new Vector2(0.96f, 0.42f),
                fontSize: 15, color: LbHeaderColor);
            _lbHeader.alignment = TextAnchor.MiddleLeft;

            _lbRow1 = CreateLabel(panelGo.transform, "LbRow1",
                anchorMin: new Vector2(0.04f, 0.27f), anchorMax: new Vector2(0.96f, 0.35f),
                fontSize: 15, color: LbRowColor);
            _lbRow1.alignment = TextAnchor.MiddleLeft;

            _lbRow2 = CreateLabel(panelGo.transform, "LbRow2",
                anchorMin: new Vector2(0.04f, 0.19f), anchorMax: new Vector2(0.96f, 0.27f),
                fontSize: 15, color: LbRowColor);
            _lbRow2.alignment = TextAnchor.MiddleLeft;

            _lbRow3 = CreateLabel(panelGo.transform, "LbRow3",
                anchorMin: new Vector2(0.04f, 0.12f), anchorMax: new Vector2(0.96f, 0.19f),
                fontSize: 15, color: LbRowColor);
            _lbRow3.alignment = TextAnchor.MiddleLeft;

            // Share row: Discord + Twitter — visible on victory only, above action buttons
            _shareRow = new GameObject("ShareRow");
            _shareRow.transform.SetParent(panelGo.transform, false);
            var shareRowRect = _shareRow.AddComponent<RectTransform>();
            shareRowRect.anchorMin = new Vector2(0.05f, 0.12f);
            shareRowRect.anchorMax = new Vector2(0.95f, 0.20f);
            shareRowRect.offsetMin = Vector2.zero;
            shareRowRect.offsetMax = Vector2.zero;

            (_btnDiscord, _) = CreateColoredButton(_shareRow.transform, "BtnDiscord",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0.47f, 1f),
                bgColor: ShareDiscordColor,
                labelText: "Discord");
            _btnDiscord.onClick.AddListener(OnDiscordShareClicked);

            (_btnTwitter, _) = CreateColoredButton(_shareRow.transform, "BtnTwitter",
                anchorMin: new Vector2(0.53f, 0f),
                anchorMax: new Vector2(1f, 1f),
                bgColor: ShareTwitterColor,
                labelText: "Twitter");
            _btnTwitter.onClick.AddListener(OnTwitterShareClicked);

            _shareRow.SetActive(false);

            // Toast — centred just above share row, hidden by default
            _toastText = CreateLabel(panelGo.transform, "ToastLabel",
                anchorMin: new Vector2(0.10f, 0.20f),
                anchorMax: new Vector2(0.90f, 0.27f),
                fontSize: 16, color: ToastColor);
            _toastText.gameObject.SetActive(false);

            // Primary button (Rejouer) — left
            (_btnPrimary, _btnPrimaryLabel) = CreateButton(panelGo.transform, "BtnPrimary",
                anchorMin: new Vector2(0.05f, 0.02f),
                anchorMax: new Vector2(0.47f, 0.11f));
            _btnPrimary.onClick.AddListener(OnPrimaryClicked);

            // Secondary button (Continuer / Menu) — right
            (_btnSecondary, _btnSecondaryLabel) = CreateButton(panelGo.transform, "BtnSecondary",
                anchorMin: new Vector2(0.53f, 0.02f),
                anchorMax: new Vector2(0.95f, 0.11f));
            _btnSecondary.onClick.AddListener(OnSecondaryClicked);

            _panel.SetActive(false);
        }

        private static (Button btn, Text label) CreateColoredButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor, string labelText)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = new Color(Mathf.Min(bgColor.r + 0.13f, 1f), Mathf.Min(bgColor.g + 0.13f, 1f), Mathf.Min(bgColor.b + 0.13f, 1f), 1f);
            colors.pressedColor     = new Color(Mathf.Max(bgColor.r - 0.07f, 0f), Mathf.Max(bgColor.g - 0.07f, 0f), Mathf.Max(bgColor.b - 0.07f, 0f), 1f);
            btn.colors = colors;

            var labelGo   = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            Stretch(labelRect);
            var txt = labelGo.AddComponent<Text>();
            txt.text      = labelText;
            txt.fontSize  = 18;
            txt.color     = ButtonTextColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return (btn, txt);
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
