#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Top-center dedicated HP bar for boss encounters.
    // 800×60 px panel, black bg 60% alpha, large red→green fill, HP numbers.
    // Listens to BossEncounteredEvent, BossHpChangedEvent, BossDefeatedEvent.
    // No prefab required — canvas built in code.
    public class BossHpBarController : MonoSingleton<BossHpBarController>
    {
        private Canvas?        _canvas;
        private RectTransform? _fillRect;
        private TextMeshProUGUI? _nameLabel;
        private TextMeshProUGUI? _hpLabel;
        private Image?         _fillImage;

        private float _maxHp;
        private float _currentRatio;
        private float _blinkTimer;

        private static readonly Color ColorFull    = new Color(0.18f, 0.82f, 0.18f); // green
        private static readonly Color ColorLow     = new Color(0.90f, 0.15f, 0.15f); // red
        private static readonly Color ColorEnraged = new Color(1.00f, 0.50f, 0.00f); // orange mid

        private static readonly Color LabelGreen  = new Color(0.18f, 0.82f, 0.18f);
        private static readonly Color LabelOrange = new Color(1.00f, 0.55f, 0.00f);
        private static readonly Color LabelRed    = new Color(0.90f, 0.15f, 0.15f);

        protected override void OnAwakeSingleton()
        {
            BuildCanvas();
            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<BossEncounteredEvent>(OnEncountered);
            em.Subscribe<BossHpChangedEvent>(OnHpChanged);
            em.Subscribe<BossDefeatedEvent>(OnDefeated);
        }

        protected override void OnDestroySingleton()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<BossEncounteredEvent>(OnEncountered);
            em.Unsubscribe<BossHpChangedEvent>(OnHpChanged);
            em.Unsubscribe<BossDefeatedEvent>(OnDefeated);
        }

        private void OnEncountered(BossEncounteredEvent e)
        {
            _maxHp = e.MaxHp;
            if (_nameLabel != null) _nameLabel.text = $"\U0001F451 {e.DisplayName.ToUpper()}";
            RefreshBar(1f);
            if (_canvas != null) _canvas.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_hpLabel == null || _currentRatio >= 0.33f) return;
            _blinkTimer += Time.deltaTime;
            _hpLabel.color = (_blinkTimer % 0.6f < 0.3f) ? LabelRed : Color.white;
        }

        private void OnHpChanged(BossHpChangedEvent e) => RefreshBar(e.Ratio);

        private void OnDefeated(BossDefeatedEvent _)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private void RefreshBar(float ratio)
        {
            _currentRatio = ratio;
            _blinkTimer   = 0f;

            if (_fillRect != null)
                _fillRect.anchorMax = new Vector2(ratio, 1f);

            if (_fillImage != null)
                _fillImage.color = ratio switch
                {
                    > 0.5f => Color.Lerp(ColorEnraged, ColorFull, (ratio - 0.5f) / 0.5f),
                    _      => Color.Lerp(ColorLow, ColorEnraged, ratio / 0.5f),
                };

            if (_hpLabel != null)
            {
                float current    = ratio * _maxHp;
                float pct        = ratio * 100f;
                Color pctColor   = pct > 66f ? LabelGreen : pct >= 33f ? LabelOrange : LabelRed;
                string pctStyled = $"<color=#{ColorUtility.ToHtmlStringRGB(pctColor)}>{pct:F0}%</color>";
                _hpLabel.text    = $"{current:F0} / {_maxHp:F0} ({pctStyled})";
                // Steady color for >= 33%; blink handled in Update for < 33%
                if (ratio >= 0.33f)
                    _hpLabel.color = Color.white;
            }
        }

        private void BuildCanvas()
        {
            var go = new GameObject("[BossHpBar]");
            go.transform.SetParent(transform);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150; // below intro banner (200), above gameplay

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            // Outer panel 800×60, anchored top-center
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(go.transform, false);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.60f);

            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot     = new Vector2(0.5f, 1f);
            panelRt.sizeDelta = new Vector2(800f, 60f);
            panelRt.anchoredPosition = new Vector2(0f, -8f); // 8px margin from top

            // Bar track (slightly inset inside panel)
            var trackGo = new GameObject("BarTrack");
            trackGo.transform.SetParent(panelGo.transform, false);
            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            var trackRt = trackGo.GetComponent<RectTransform>();
            // Lower half of panel — below the name label row
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 0.45f);
            trackRt.offsetMin = new Vector2(6f, 4f);
            trackRt.offsetMax = new Vector2(-6f, 0f);

            // Fill (anchors drive width — no sizeDelta needed)
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(trackGo.transform, false);
            _fillImage = fillGo.AddComponent<Image>();
            _fillImage.color = ColorFull;

            _fillRect = fillGo.GetComponent<RectTransform>();
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(1f, 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;

            // Boss name label — top half of panel
            var nameGo = new GameObject("BossName");
            nameGo.transform.SetParent(panelGo.transform, false);
            _nameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.fontSize  = 16f;
            _nameLabel.fontStyle = FontStyles.Bold;
            _nameLabel.color     = new Color(1f, 0.85f, 0.1f); // gold

            var nameRt = _nameLabel.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(4f, 0f);
            nameRt.offsetMax = new Vector2(-4f, -2f);

            // HP numbers — overlaid on the bar track
            var hpGo = new GameObject("HpNumbers");
            hpGo.transform.SetParent(trackGo.transform, false);
            _hpLabel = hpGo.AddComponent<TextMeshProUGUI>();
            _hpLabel.alignment = TextAlignmentOptions.Center;
            _hpLabel.fontSize  = 11f;
            _hpLabel.fontStyle = FontStyles.Bold;
            _hpLabel.color     = Color.white;

            var hpRt = _hpLabel.GetComponent<RectTransform>();
            hpRt.anchorMin = Vector2.zero;
            hpRt.anchorMax = Vector2.one;
            hpRt.offsetMin = Vector2.zero;
            hpRt.offsetMax = Vector2.zero;

            go.SetActive(false);
        }

#if UNITY_EDITOR
        [ContextMenu("Test BossHpBar — 75%")]
        private void TestBar()
        {
            OnEncountered(new BossEncounteredEvent("TITAN INFERNAL", 5000f, Color.red, Vector3.zero));
            OnHpChanged(new BossHpChangedEvent(0.75f));
        }
#endif
    }
}
