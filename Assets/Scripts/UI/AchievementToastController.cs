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
    /// Affiche les toasts d'achievement (bottom-right) via UI Toolkit.
    /// Chaque unlock empile dans une queue ; les toasts apparaissent avec 0.5s de stagger.
    /// Auto-dismiss après 3s. Slide-in depuis la droite.
    /// Requiert un UIDocument avec AchievementToast.uxml sur ce GameObject.
    /// Attacher sur un GameObject séparé du HUD (sort order supérieur).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AchievementToastController : MonoBehaviour
    {
        private const float DisplayDuration = 3f;
        private const float StaggerDelay = 0.5f;
        private const float SlideInDuration = 0.25f;
        private const float FadeOutDuration = 0.3f;

        [SerializeField] private AchievementRegistry? registry;

        private VisualElement? _stack;
        private readonly Queue<string> _pendingIds = new();
        private bool _draining;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            _stack = doc.rootVisualElement.Q<VisualElement>("toast-stack");

            if (registry == null)
                registry = Resources.Load<AchievementRegistry>("AchievementRegistry");
        }

        private void OnEnable()
        {
            Achievements.OnUnlocked += Enqueue;
        }

        private void OnDisable()
        {
            Achievements.OnUnlocked -= Enqueue;
        }

        private void Enqueue(string id)
        {
            _pendingIds.Enqueue(id);
            if (!_draining)
                StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            _draining = true;
            while (_pendingIds.Count > 0)
            {
                string id = _pendingIds.Dequeue();
                yield return StartCoroutine(ShowToast(id));
                if (_pendingIds.Count > 0)
                    yield return new WaitForSecondsRealtime(StaggerDelay);
            }
            _draining = false;
        }

        private IEnumerator ShowToast(string id)
        {
            if (_stack == null) yield break;

            var def = registry?.Get(id);

            var card = BuildCard(def, id);
            _stack.Add(card);

            // slide-in : translate X de 80 → 0
            yield return AnimateSlideIn(card);

            // hold
            yield return new WaitForSecondsRealtime(DisplayDuration);

            // fade-out + slide-out
            yield return AnimateFadeOut(card);

            _stack.Remove(card);
        }

        private VisualElement BuildCard(AchievementDef? def, string fallbackId)
        {
            var card = new VisualElement();
            card.AddToClassList("achievement-toast");

            // icon
            var iconEl = new VisualElement();
            iconEl.AddToClassList("toast-icon");
            if (def?.icon != null)
                iconEl.style.backgroundImage = new StyleBackground(def.icon);
            card.Add(iconEl);

            // text block
            var textBlock = new VisualElement();
            textBlock.AddToClassList("toast-text-block");

            var header = new Label(L.Get("achievement.toast_header"));
            header.AddToClassList("toast-header");
            textBlock.Add(header);

            string title = def != null && !string.IsNullOrEmpty(def.titleKey)
                ? L.Get(def.titleKey)
                : fallbackId;
            var titleEl = new Label(title);
            titleEl.AddToClassList("toast-title");
            textBlock.Add(titleEl);

            if (def != null && !string.IsNullOrEmpty(def.descKey))
            {
                string desc = L.Get(def.descKey);
                // Only show desc if L.Get resolved it (i.e. didn't return the key verbatim)
                if (desc != def.descKey)
                {
                    var descEl = new Label(desc);
                    descEl.AddToClassList("toast-desc");
                    textBlock.Add(descEl);
                }
            }

            card.Add(textBlock);

            // points badge
            if (def != null && def.points > 0)
            {
                var pts = new Label($"+{def.points}");
                pts.AddToClassList("toast-points");
                card.Add(pts);
            }

            return card;
        }

        private IEnumerator AnimateSlideIn(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < SlideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SlideInDuration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad
                float tx = Mathf.Lerp(80f, 0f, eased);
                card.style.translate = new Translate(new Length(tx, LengthUnit.Pixel), 0);
                card.style.opacity = eased;
                yield return null;
            }
            card.style.translate = new Translate(0, 0);
            card.style.opacity = 1f;
        }

        private IEnumerator AnimateFadeOut(VisualElement card)
        {
            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                card.style.opacity = 1f - t;
                float tx = Mathf.Lerp(0f, 40f, t);
                card.style.translate = new Translate(new Length(tx, LengthUnit.Pixel), 0);
                yield return null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Toast (fake unlock)")]
        private void TestToastFake()
        {
            Enqueue("test_achievement");
        }
#endif
    }
}
