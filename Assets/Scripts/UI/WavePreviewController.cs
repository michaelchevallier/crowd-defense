#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public class WavePreviewController : MonoBehaviour
    {
        private const int MaxChips = 12;
        private const float DisplaySeconds = 5f;
        private const float FadeSeconds = 0.4f;

        private VisualElement? wavePreview;
        private Label? wavePreviewTitle;
        private VisualElement? wavePreviewRoster;

        private Coroutine? _autoHideCoroutine;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            wavePreview = root.Q<VisualElement>("wave-preview");
            wavePreviewTitle = root.Q<Label>("wave-preview-title");
            wavePreviewRoster = root.Q<VisualElement>("wave-preview-roster");

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnBreakStateChanged += Refresh;
                WaveManager.Instance.OnWaveStart += OnWaveStarted;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnBreakStateChanged -= Refresh;
                WaveManager.Instance.OnWaveStart -= OnWaveStarted;
            }
        }

        private void OnWaveStarted(int _) => HideImmediate();

        private void Refresh()
        {
            if (WaveManager.Instance == null || wavePreview == null) return;

            if (!WaveManager.Instance.IsWaitingForPlayerStart)
            {
                HideImmediate();
                return;
            }

            var waveDef = WaveManager.Instance.GetNextWaveDef();
            if (waveDef == null || waveDef.Value.entries == null || waveDef.Value.entries.Count == 0)
            {
                HideImmediate();
                return;
            }

            if (wavePreviewTitle != null)
                wavePreviewTitle.text = L.Get("hud.wave_preview_title", WaveManager.Instance.NextWaveDisplayNumber);

            BuildRoster(waveDef.Value.entries);

            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
            wavePreview.style.opacity = 1f;
            SetVisible(wavePreview, true);
            _autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
        }

        private IEnumerator AutoHideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(DisplaySeconds);
            yield return FadeOut();
        }

        private IEnumerator FadeOut()
        {
            if (wavePreview == null) yield break;
            float elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                wavePreview.style.opacity = Mathf.Lerp(1f, 0f, elapsed / FadeSeconds);
                yield return null;
            }
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }
            if (wavePreview == null) return;
            wavePreview.style.opacity = 1f;
            SetVisible(wavePreview, false);
        }

        private void BuildRoster(List<EnemySpawnEntry> entries)
        {
            if (wavePreviewRoster == null) return;
            wavePreviewRoster.Clear();

            var counts = new Dictionary<EnemyType, int>();
            foreach (var entry in entries)
            {
                if (entry.type == null) continue;
                counts.TryGetValue(entry.type, out int existing);
                counts[entry.type] = existing + entry.count;
            }

            // Bosses first, then mid-bosses, then normals
            var sorted = new List<KeyValuePair<EnemyType, int>>(counts);
            sorted.Sort((a, b) =>
            {
                int rankA = a.Key.IsBoss ? 0 : a.Key.IsMidBoss ? 1 : 2;
                int rankB = b.Key.IsBoss ? 0 : b.Key.IsMidBoss ? 1 : 2;
                return rankA.CompareTo(rankB);
            });

            int shown = 0;
            foreach (var kvp in sorted)
            {
                if (shown >= MaxChips) break;
                var type = kvp.Key;
                int count = kvp.Value;

                var chip = new VisualElement();
                chip.AddToClassList("wave-preview-chip");
                if (type.IsBoss || type.IsMidBoss)
                    chip.AddToClassList("boss-chip");

                var iconLabel = new Label(type.IconEmoji);
                iconLabel.AddToClassList("wave-preview-chip-icon");

                var countLabel = new Label($"x{count}");
                countLabel.AddToClassList("wave-preview-chip-count");

                chip.Add(iconLabel);
                chip.Add(countLabel);
                wavePreviewRoster.Add(chip);
                shown++;
            }
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }
    }
}
