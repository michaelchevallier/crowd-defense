#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Entities;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WaveClearedController : MonoSingleton<WaveClearedController>
    {
        private const float SlideDownDuration = 0.4f;
        private const float HoldDuration      = 2.6f;
        private const float SlideUpDuration   = 0.4f;
        private const float CardOffscreenY    = -120f;
        private const float CardVisibleY      = 0f;

        [SerializeField] private StyleSheet? styleSheet;

        private VisualElement? _card;
        private Label?         _titleLabel;
        private Label?         _goldLabel;
        private Label?         _hpLabel;

        private int  _goldAtWaveStart;
        private bool _animating;

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            BuildUI(doc.rootVisualElement);
        }

        private void BuildUI(VisualElement root)
        {
            var container = new VisualElement();
            container.style.position       = Position.Absolute;
            container.style.top            = 0;
            container.style.left           = 0;
            container.style.right          = 0;
            container.style.bottom         = 0;
            container.style.alignItems     = Align.Center;
            container.style.justifyContent = Justify.FlexStart;
            container.style.unityFontStyleAndWeight = FontStyle.Normal;

            if (styleSheet != null)
                container.styleSheets.Add(styleSheet);

            _card = new VisualElement();

            if (styleSheet != null)
            {
                _card.AddToClassList("wave-cleared-card");
            }
            else
            {
                // Inline fallback — active even without assigned USS asset
                _card.style.marginTop        = 16;
                _card.style.width            = 400;
                _card.style.height           = 100;
                _card.style.backgroundColor  = new Color(0f, 0f, 0f, 0.70f);
                _card.style.borderTopColor   = _card.style.borderBottomColor =
                _card.style.borderLeftColor  = _card.style.borderRightColor  =
                    new Color(0.83f, 0.69f, 0.21f);
                _card.style.borderTopWidth   = _card.style.borderBottomWidth =
                _card.style.borderLeftWidth  = _card.style.borderRightWidth  = 2f;
                _card.style.borderTopLeftRadius  = _card.style.borderTopRightRadius =
                _card.style.borderBottomLeftRadius = _card.style.borderBottomRightRadius = 10;
                _card.style.flexDirection    = FlexDirection.Column;
                _card.style.alignItems       = Align.Center;
                _card.style.justifyContent   = Justify.Center;
                _card.style.paddingTop       = _card.style.paddingBottom = 10;
                _card.style.paddingLeft      = _card.style.paddingRight  = 20;
            }

            _titleLabel = new Label("Vague 1 conquise!");
            if (styleSheet != null)
            {
                _titleLabel.AddToClassList("wave-cleared-title");
            }
            else
            {
                _titleLabel.style.color      = new Color(0.83f, 0.69f, 0.21f);
                _titleLabel.style.fontSize   = 22;
                _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _titleLabel.style.marginBottom = 4;
                _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            _card.Add(_titleLabel);

            var row = new VisualElement();
            row.style.flexDirection   = FlexDirection.Row;
            row.style.justifyContent  = Justify.Center;
            row.style.width           = new StyleLength(new Length(100f, LengthUnit.Percent));

            _goldLabel = new Label("+ 0c");
            if (styleSheet != null)
            {
                _goldLabel.AddToClassList("wave-cleared-gold");
            }
            else
            {
                _goldLabel.style.color    = new Color(1f, 0.86f, 0.24f);
                _goldLabel.style.fontSize = 15;
                _goldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _goldLabel.style.marginRight = 24;
                _goldLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            row.Add(_goldLabel);

            _hpLabel = new Label("0 / 0");
            if (styleSheet != null)
            {
                _hpLabel.AddToClassList("wave-cleared-hp");
            }
            else
            {
                _hpLabel.style.color    = new Color(0.39f, 0.86f, 0.39f);
                _hpLabel.style.fontSize = 15;
                _hpLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _hpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            row.Add(_hpLabel);

            _card.Add(row);
            container.Add(_card);
            root.Add(container);

            // Start hidden above viewport
            SetCardY(CardOffscreenY);
            _card.style.opacity = 0f;
        }

        private void OnEnable()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart   += OnWaveStart;
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
            }
        }

        private void OnDisable()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart   -= OnWaveStart;
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
            }
        }

        private void OnWaveStart(int _waveIdx)
        {
            _goldAtWaveStart = Economy.Instance?.Gold ?? 0;
        }

        private void OnWaveCleared(int waveIdx)
        {
            if (_animating) return;

            int goldNow  = Economy.Instance?.Gold    ?? 0;
            int goldGain = Mathf.Max(0, goldNow - _goldAtWaveStart);
            int castleHp  = Castle.Instance?.HP    ?? 0;
            int castleMax = Castle.Instance?.HPMax ?? 0;

            ShowCard(waveIdx + 1, goldGain, castleHp, castleMax);
        }

        private void ShowCard(int waveNumber, int goldGain, int castleHp, int castleMax)
        {
            if (_card == null || _titleLabel == null || _goldLabel == null || _hpLabel == null)
                return;

            _titleLabel.text = $"Vague {waveNumber} conquise!";
            _goldLabel.text  = $"+ {goldGain}c";
            _hpLabel.text    = $"Chateau {castleHp}/{castleMax}";

            StartCoroutine(AnimateCard());
        }

        private IEnumerator AnimateCard()
        {
            _animating = true;

            yield return AnimateY(CardOffscreenY, CardVisibleY, SlideDownDuration, easeOut: true);
            yield return new WaitForSecondsRealtime(HoldDuration);
            yield return AnimateY(CardVisibleY, CardOffscreenY, SlideUpDuration, easeOut: false);

            _card!.style.opacity = 0f;
            _animating = false;
        }

        private IEnumerator AnimateY(float fromY, float toY, float duration, bool easeOut)
        {
            if (_card == null) yield break;
            float elapsed = 0f;
            _card.style.opacity = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = easeOut ? 1f - (1f - t) * (1f - t) : t * t;
                SetCardY(Mathf.Lerp(fromY, toY, eased));
                yield return null;
            }
            SetCardY(toY);
        }

        private void SetCardY(float y)
        {
            if (_card == null) return;
            _card.style.translate = new Translate(0, new Length(y, LengthUnit.Pixel));
        }

#if UNITY_EDITOR
        [ContextMenu("Test Wave Cleared Card")]
        private void TestCard() => ShowCard(3, 120, 450, 500);
#endif
    }
}
