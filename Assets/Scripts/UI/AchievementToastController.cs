#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Affiche les toasts d'achievement (top-right) via UI Toolkit.
    /// Chaque unlock empile verticalement (Y+90px). Slide-in depuis X+300,
    /// hold 3s, slide-out vers X+300. Audio "achievement_chime" pitch 1.2.
    /// Attacher sur le même GameObject que le HudController.
    /// </summary>
    public class AchievementToastController : MonoBehaviour
    {
        private const float SlideInDuration  = 0.4f;
        private const float DisplayDuration  = 3.0f;
        private const float SlideOutDuration = 0.4f;
        private const float StaggerDelay     = 0.15f;
        private const int   MaxQueueSize     = 5;
        private const float ToastWidth       = 280f;
        private const float ToastHeight      = 80f;
        private const float StackOffsetY     = 90f;
        private const float SlideFromX       = 300f;
        private const float AnchorRight      = 20f;
        private const float AnchorTop        = 100f;

        private static readonly Color BgColor     = new(0.15f, 0.12f, 0.05f, 0.95f);
        private static readonly Color BorderColor  = new(1f, 0.85f, 0.2f, 1f);
        private static readonly Color HeaderColor  = new(1f, 0.85f, 0.2f, 0.85f);
        private static readonly Color TitleColor   = new(1f, 1f, 1f, 1f);

        [SerializeField] private AchievementRegistry? registry;

        private VisualElement? _root;
        private readonly Queue<(string id, string displayName)> _pendingIds = new();
        private bool _draining;
        private int  _activeCount;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>()
                   ?? FindFirstObjectByType<UIDocument>();
            _root = doc?.rootVisualElement;

            if (registry == null)
                registry = Resources.Load<AchievementRegistry>("AchievementRegistry");
        }

        private void OnEnable()  => Achievements.OnUnlocked += HandleUnlocked;
        private void OnDisable() => Achievements.OnUnlocked -= HandleUnlocked;

        private void HandleUnlocked(string id)
        {
            var def = registry?.Get(id);
            string displayName = def != null && !string.IsNullOrEmpty(def.titleKey)
                ? L.Get(def.titleKey)
                : id;
            ShowAchievementToast(id, displayName);
        }

        public void ShowAchievementToast(string achievementId, string displayName)
        {
            if (_pendingIds.Count >= MaxQueueSize) return;
            _pendingIds.Enqueue((achievementId, displayName));
            if (!_draining)
                StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            _draining = true;
            while (_pendingIds.Count > 0)
            {
                var (id, name) = _pendingIds.Dequeue();
                StartCoroutine(ShowToast(id, name));
                yield return new WaitForSecondsRealtime(StaggerDelay);
            }
            _draining = false;
        }

        private IEnumerator ShowToast(string id, string displayName)
        {
            if (_root == null) yield break;

            var def = registry?.Get(id);
            string iconEmoji = def != null ? def.IconEmoji : "\U0001F3C6";

            int slotIndex = _activeCount;
            _activeCount++;

            var card = BuildCard(iconEmoji, displayName, slotIndex);
            _root.Add(card);

            AudioController.Instance?.PlayPitched("achievement_chime", volMul: 0.8f, pitch: 1.2f);

            yield return AnimateSlideIn(card);
            yield return new WaitForSecondsRealtime(DisplayDuration);
            yield return AnimateSlideOut(card);

            _root.Remove(card);
            _activeCount = Mathf.Max(0, _activeCount - 1);

            // Shift remaining cards up
            RebuildSlotPositions();
        }

        private VisualElement BuildCard(string iconEmoji, string displayName, int slotIndex)
        {
            var card = new VisualElement();
            card.AddToClassList("achievement-toast");

            // Position: top-right anchor, stacked vertically
            card.style.position  = Position.Absolute;
            card.style.right     = AnchorRight;
            card.style.top       = AnchorTop + slotIndex * StackOffsetY;
            card.style.width     = ToastWidth;
            card.style.height    = ToastHeight;

            // Gold background + border
            card.style.backgroundColor = BgColor;
            card.style.borderTopColor    = BorderColor;
            card.style.borderBottomColor = BorderColor;
            card.style.borderLeftColor   = BorderColor;
            card.style.borderRightColor  = BorderColor;
            card.style.borderTopWidth    = 2f;
            card.style.borderBottomWidth = 2f;
            card.style.borderLeftWidth   = 2f;
            card.style.borderRightWidth  = 2f;
            card.style.borderTopLeftRadius     = new Length(8, LengthUnit.Pixel);
            card.style.borderTopRightRadius    = new Length(8, LengthUnit.Pixel);
            card.style.borderBottomLeftRadius  = new Length(8, LengthUnit.Pixel);
            card.style.borderBottomRightRadius = new Length(8, LengthUnit.Pixel);

            card.style.flexDirection  = FlexDirection.Row;
            card.style.alignItems     = Align.Center;
            card.style.paddingLeft    = 12f;
            card.style.paddingRight   = 12f;
            card.style.paddingTop     = 8f;
            card.style.paddingBottom  = 8f;

            // Slide start
            card.style.translate = new Translate(new Length(SlideFromX, LengthUnit.Pixel), 0);
            card.style.opacity   = 0f;

            // Icon label (trophy emoji, 40x40)
            var icon = new Label(iconEmoji);
            icon.style.width         = 40f;
            icon.style.height        = 40f;
            icon.style.fontSize      = 28f;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.flexShrink    = 0f;
            icon.style.marginRight   = 10f;
            card.Add(icon);

            // Text block
            var textBlock = new VisualElement();
            textBlock.style.flexDirection = FlexDirection.Column;
            textBlock.style.flexGrow      = 1f;
            textBlock.style.justifyContent = Justify.Center;

            var header = new Label("SUCCES DEBLOQUE");
            header.AddToClassList("toast-header");
            header.style.color     = HeaderColor;
            header.style.fontSize  = 12f;
            header.style.unityFontStyleAndWeight = FontStyle.Italic;
            textBlock.Add(header);

            var title = new Label(displayName);
            title.AddToClassList("toast-title");
            title.style.color    = TitleColor;
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            textBlock.Add(title);

            card.Add(textBlock);
            return card;
        }

        private void RebuildSlotPositions()
        {
            // No-op: cards are positioned absolutely at creation; active count drives next slot.
            // Individual coroutines own their card lifetime so no reindex needed.
        }

        private static IEnumerator AnimateSlideIn(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < SlideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / SlideInDuration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad
                card.style.translate = new Translate(
                    new Length(Mathf.Lerp(SlideFromX, 0f, eased), LengthUnit.Pixel), 0);
                card.style.opacity = eased;
                yield return null;
            }
            card.style.translate = new Translate(0, 0);
            card.style.opacity   = 1f;
        }

        private static IEnumerator AnimateSlideOut(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < SlideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SlideOutDuration);
                float eased = t * t; // ease-in quad
                card.style.translate = new Translate(
                    new Length(Mathf.Lerp(0f, SlideFromX, eased), LengthUnit.Pixel), 0);
                card.style.opacity = 1f - t;
                yield return null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Toast (fake unlock)")]
        private void TestToastFake() => ShowAchievementToast("test_id", "Premier Sang !");

        [ContextMenu("Test Queue (3 toasts)")]
        private void TestQueue()
        {
            ShowAchievementToast("test_1", "Premier Sang !");
            ShowAchievementToast("test_2", "Solide Defenseur");
            ShowAchievementToast("test_3", "Seigneur de Guerre");
        }
#endif
    }
}
