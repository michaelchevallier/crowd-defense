#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WavePreviewController : MonoBehaviour
    {
        private VisualElement? wavePreview;
        private Label? wavePreviewTitle;
        private VisualElement? wavePreviewRoster;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            wavePreview = root.Q<VisualElement>("wave-preview");
            wavePreviewTitle = root.Q<Label>("wave-preview-title");
            wavePreviewRoster = root.Q<VisualElement>("wave-preview-roster");

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnBreakStateChanged += Refresh;
                WaveManager.Instance.OnWaveStart += _ => Hide();
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnBreakStateChanged -= Refresh;
                WaveManager.Instance.OnWaveStart -= _ => Hide();
            }
        }

        private void Refresh()
        {
            if (WaveManager.Instance == null || wavePreview == null) return;

            if (!WaveManager.Instance.IsWaitingForPlayerStart)
            {
                Hide();
                return;
            }

            var waveDef = WaveManager.Instance.GetNextWaveDef();
            if (waveDef == null || waveDef.Value.entries == null || waveDef.Value.entries.Count == 0)
            {
                Hide();
                return;
            }

            if (wavePreviewTitle != null)
                wavePreviewTitle.text = L.Get("hud.wave_preview_title", WaveManager.Instance.NextWaveDisplayNumber);

            BuildRoster(waveDef.Value.entries);
            SetVisible(wavePreview, true);
        }

        private void Hide()
        {
            if (wavePreview != null) SetVisible(wavePreview, false);
        }

        private void BuildRoster(List<EnemySpawnEntry> entries)
        {
            if (wavePreviewRoster == null) return;
            wavePreviewRoster.Clear();

            // Aggregate counts per EnemyType (entries may list same type multiple times)
            var counts = new Dictionary<EnemyType, int>();
            foreach (var entry in entries)
            {
                if (entry.type == null) continue;
                counts.TryGetValue(entry.type, out int existing);
                counts[entry.type] = existing + entry.count;
            }

            foreach (var kvp in counts)
            {
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
            }
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }
    }
}
