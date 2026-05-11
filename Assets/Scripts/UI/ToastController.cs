#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    // Static facade — callers use Toast.Show(...) without needing a reference to the MonoBehaviour.
    public static class Toast
    {
        public static void Show(string title, string body, int durationMs = 3000, string? iconEmoji = null) =>
            ToastController.Instance?.Enqueue(title, body, durationMs, iconEmoji);
    }

    [RequireComponent(typeof(UIDocument))]
    public class ToastController : MonoSingleton<ToastController>
    {
        private const float StaggerDelay    = 0.4f;
        private const float SlideInDuration = 0.22f;
        private const float FadeOutDuration = 0.25f;

        private VisualElement? _stack;
        private readonly Queue<ToastData> _pending = new();
        private bool _draining;

        private readonly struct ToastData
        {
            public readonly string Title;
            public readonly string Body;
            public readonly float DurationSec;
            public readonly string? IconEmoji;
            public ToastData(string title, string body, float durationSec, string? iconEmoji)
            {
                Title = title; Body = body; DurationSec = durationSec; IconEmoji = iconEmoji;
            }
        }

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            // Prefer dedicated stack; fall back to the achievement stack if co-located.
            _stack = doc.rootVisualElement.Q<VisualElement>("generic-toast-stack")
                  ?? doc.rootVisualElement.Q<VisualElement>("toast-stack");
        }

        public void Enqueue(string title, string body, int durationMs, string? iconEmoji)
        {
            _pending.Enqueue(new ToastData(title, body, durationMs / 1000f, iconEmoji));
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

            var card = BuildCard(data);
            _stack.Add(card);

            yield return AnimateSlideIn(card);
            yield return new WaitForSecondsRealtime(data.DurationSec);
            yield return AnimateFadeOut(card);

            _stack.Remove(card);
        }

        private static VisualElement BuildCard(ToastData data)
        {
            var card = new VisualElement();
            card.AddToClassList("generic-toast");
            card.style.opacity = 0f;
            card.style.translate = new Translate(new Length(80f, LengthUnit.Pixel), 0);

            if (!string.IsNullOrEmpty(data.IconEmoji))
            {
                var emoji = new Label(data.IconEmoji);
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
        private void TestToast() =>
            Toast.Show("Test Title", "Ceci est un test de toast generique.", 3000, null);
#endif
    }
}
