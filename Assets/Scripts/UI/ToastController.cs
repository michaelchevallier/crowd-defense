#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public enum ToastType { Generic, Achievement, Perk, Synergy, Combo, Modifier, Success, Warning, Error, Info }

    // Static facade — callers use Toast.Show(...) without needing a reference to the MonoBehaviour.
    public static class Toast
    {
        public static void Show(string title, string body, int durationMs = 3000, string? iconEmoji = null, ToastType type = ToastType.Generic) =>
            ToastController.Instance?.Enqueue(title, body, durationMs, iconEmoji, type);
    }

    [RequireComponent(typeof(UIDocument))]
    public class ToastController : MonoSingleton<ToastController>
    {
        private const float StaggerDelay    = 0.4f;
        private const float SlideInDuration = 0.22f;
        private const float FadeOutDuration = 0.25f;
        private const int   MaxStackVisible = 4;

        private VisualElement? _stack;
        private readonly Queue<ToastData> _pending = new();
        private readonly List<VisualElement> _visible = new();
        private bool _draining;

        private readonly struct ToastData
        {
            public readonly string Title;
            public readonly string Body;
            public readonly float DurationSec;
            public readonly string? IconEmoji;
            public readonly ToastType Type;
            public ToastData(string title, string body, float durationSec, string? iconEmoji, ToastType type)
            {
                Title = title; Body = body; DurationSec = durationSec; IconEmoji = iconEmoji; Type = type;
            }
        }

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[ToastController] UIDocument not found");
                return;
            }
            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[ToastController] rootVisualElement is null");
                return;
            }
            _stack = root.Q<VisualElement>("toast-stack");
        }

        public void Enqueue(string title, string body, int durationMs, string? iconEmoji, ToastType type = ToastType.Generic)
        {
            _pending.Enqueue(new ToastData(title, body, durationMs / 1000f, iconEmoji, type));
            if (!_draining)
                StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            _draining = true;
            while (_pending.Count > 0)
            {
                var data = _pending.Dequeue();
                yield return StartCoroutine(ShowToast(data));
                if (_pending.Count > 0)
                    yield return new WaitForSecondsRealtime(StaggerDelay);
            }
            _draining = false;
        }

        private IEnumerator ShowToast(ToastData data)
        {
            if (_stack == null) yield break;

            // Evict oldest visible card if stack is full
            if (_visible.Count >= MaxStackVisible && _visible.Count > 0)
            {
                var oldest = _visible[0];
                _visible.RemoveAt(0);
                StartCoroutine(EvictCard(oldest));
            }

            var card = BuildCard(data);
            _stack.Add(card);
            _visible.Add(card);

            AudioController.Instance?.Play($"toast_{data.Type.ToString().ToLowerInvariant()}");

            yield return AnimateSlideIn(card);
            yield return new WaitForSecondsRealtime(data.DurationSec);
            yield return AnimateFadeOut(card);

            _visible.Remove(card);
            _stack.Remove(card);
        }

        private IEnumerator EvictCard(VisualElement card)
        {
            yield return AnimateFadeOut(card);
            _stack?.Remove(card);
        }

        private static string TypeCssClass(ToastType t) => t switch
        {
            ToastType.Achievement => "toast-type-gold",
            ToastType.Synergy     => "toast-type-blue",
            ToastType.Perk        => "toast-type-green",
            ToastType.Combo       => "toast-type-orange",
            ToastType.Modifier    => "toast-type-purple",
            ToastType.Success     => "toast-type-success",
            ToastType.Warning     => "toast-type-warning",
            ToastType.Error       => "toast-type-error",
            ToastType.Info        => "toast-type-info",
            _                     => "toast-type-default",
        };

        private static string? DefaultIcon(ToastType t) => t switch
        {
            ToastType.Success => "✅",
            ToastType.Warning => "⚠️",
            ToastType.Error   => "❌",
            ToastType.Info    => "ℹ️",
            _                 => null,
        };

        private static VisualElement BuildCard(ToastData data)
        {
            var card = new VisualElement();
            card.AddToClassList("generic-toast");
            card.AddToClassList(TypeCssClass(data.Type));
            card.style.opacity = 0f;
            card.style.translate = new Translate(new Length(80f, LengthUnit.Pixel), 0);

            var resolvedIcon = string.IsNullOrEmpty(data.IconEmoji) ? DefaultIcon(data.Type) : data.IconEmoji;
            if (!string.IsNullOrEmpty(resolvedIcon))
            {
                var emoji = new Label(resolvedIcon);
                emoji.AddToClassList("generic-toast-emoji");
                card.Add(emoji);
            }

            var textBlock = new VisualElement();
            textBlock.AddToClassList("generic-toast-text");

            var titleEl = new Label(data.Title);
            titleEl.AddToClassList("generic-toast-title");
            textBlock.Add(titleEl);

            if (!string.IsNullOrEmpty(data.Body))
            {
                var bodyEl = new Label(data.Body);
                bodyEl.AddToClassList("generic-toast-body");
                textBlock.Add(bodyEl);
            }

            card.Add(textBlock);
            return card;
        }

        private static IEnumerator AnimateSlideIn(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < SlideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SlideInDuration);
                float eased = 1f - (1f - t) * (1f - t);
                card.style.translate = new Translate(new Length(Mathf.Lerp(80f, 0f, eased), LengthUnit.Pixel), 0);
                card.style.opacity = eased;
                yield return null;
            }
            card.style.translate = new Translate(0, 0);
            card.style.opacity = 1f;
        }

        private static IEnumerator AnimateFadeOut(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                card.style.opacity = 1f - t;
                card.style.translate = new Translate(new Length(Mathf.Lerp(0f, 40f, t), LengthUnit.Pixel), 0);
                yield return null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Generic Toast")]
        private void TestToast() => Toast.Show("Test", "Generique", 3000, null, ToastType.Generic);

        [ContextMenu("Test Achievement Toast")]
        private void TestAchievementToast() => Toast.Show("Achievement Unlock", "Premier sang !", 3000, null, ToastType.Achievement);

        [ContextMenu("Test Perk Toast")]
        private void TestPerkToast() => Toast.Show("Perk Pick", "Bouclier de givre", 3000, null, ToastType.Perk);

        [ContextMenu("Test Synergy Toast")]
        private void TestSynergyToast() => Toast.Show("Synergy Activated", "Trio de feu x3", 2500, null, ToastType.Synergy);

        [ContextMenu("Test Combo Toast")]
        private void TestComboToast() => Toast.Show("Combo x4", "Multi-kill !", 2000, null, ToastType.Combo);

        [ContextMenu("Test Modifier Toast")]
        private void TestModifierToast() => Toast.Show("Modifier Selected", "Vitesse +50%", 3000, null, ToastType.Modifier);
#endif
    }
}
