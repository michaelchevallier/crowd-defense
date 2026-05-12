#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Renders the 3-slot hero skill bar (Q / W / E) bottom-center.
    /// Slots poll Hero.GetCooldownRatio(i) each frame and draw a radial-fill overlay.
    /// Slot 0 (Q) = auto-attack cooldown (read-only display).
    /// Slot 1 (W) = reserved / future skill.
    /// Slot 2 (E) = ultimate (maps to Hero.TryUlt).
    /// Wired as a sibling component on the HUD GameObject alongside HudController.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HeroSkillBarController : MonoBehaviour
    {
        private static readonly string[] SlotNames  = { "skill-slot-q", "skill-slot-w", "skill-slot-e" };
        private static readonly string[] KeyLabels  = { "Q", "W", "E" };

        // Pulse animation constants (golden glow when skill is ready)
        private const float PulseScalePeak  = 1.10f;
        private const int   PulsePeriodMs   = 700;   // full oscillation period in ms
        private const int   PulseTickMs     = 16;    // ~60 Hz tick

        private VisualElement?[]              _slots      = new VisualElement?[3];
        private VisualElement?[]              _overlays   = new VisualElement?[3];
        private Label?[]                      _keyLabels  = new Label?[3];
        private Label?[]                      _cdLabels   = new Label?[3];
        private IVisualElementScheduledItem?[] _pulseItems = new IVisualElementScheduledItem?[3];
        private bool[]                        _wasReady   = new bool[3];

        private Hero? _hero;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            for (int i = 0; i < 3; i++)
            {
                _slots[i]    = root.Q<VisualElement>(SlotNames[i]);
                _overlays[i] = _slots[i]?.Q<VisualElement>("skill-cd-overlay");
                _keyLabels[i] = _slots[i]?.Q<Label>("skill-key-label");
                _cdLabels[i]  = _slots[i]?.Q<Label>("skill-cd-label");

                if (_keyLabels[i] != null)
                    _keyLabels[i]!.text = KeyLabels[i];
            }

            // Register click callbacks
            for (int idx = 0; idx < 3; idx++)
            {
                int captured = idx;
                _slots[captured]?.RegisterCallback<ClickEvent>(_ => TriggerSlot(captured));
            }
        }

        private void Update()
        {
            if (_hero == null)
                _hero = LevelRunner.Instance?.Hero;

            if (_hero == null) return;

            HandleKeyInput();

            for (int i = 0; i < 3; i++)
                RefreshSlot(i);
        }

        private void HandleKeyInput()
        {
            // Legacy Input API (InputSystem package not installed)
            if (Input.GetKeyDown(KeyCode.Q)) TriggerSlot(0);
            if (Input.GetKeyDown(KeyCode.W)) TriggerSlot(1);
            if (Input.GetKeyDown(KeyCode.E)) TriggerSlot(2);
        }

        private void TriggerSlot(int index)
        {
            if (_hero == null) return;
            _hero.Cast(index);
        }

        private void RefreshSlot(int index)
        {
            if (_overlays[index] == null || _cdLabels[index] == null) return;

            float ratio = Mathf.Clamp01(_hero != null ? _hero.GetCooldownRatio(index) : 0f);

            // Radial fill: height % from top encodes the cooldown fraction
            _overlays[index]!.style.height = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));

            bool onCooldown = ratio > 0.01f;
            _overlays[index]!.style.display = onCooldown ? DisplayStyle.Flex : DisplayStyle.None;

            if (onCooldown && _hero != null)
            {
                float remaining = _hero.GetCooldownRemaining(index);
                _cdLabels[index]!.text = remaining > 0f ? $"{remaining:F1}" : "";
                _cdLabels[index]!.style.display = DisplayStyle.Flex;
            }
            else
            {
                _cdLabels[index]!.text = "";
                _cdLabels[index]!.style.display = DisplayStyle.None;
            }

            // Ready glow class toggle + pulse animation
            if (_slots[index] != null)
            {
                bool nowReady = !onCooldown;
                if (onCooldown)
                {
                    _slots[index]!.RemoveFromClassList("skill-slot-ready");
                    if (_wasReady[index])
                        StopPulse(index);
                }
                else
                {
                    _slots[index]!.AddToClassList("skill-slot-ready");
                    if (!_wasReady[index])
                        StartPulse(index);
                }
                _wasReady[index] = nowReady;
            }
        }

        private void StartPulse(int index)
        {
            StopPulse(index);
            if (_slots[index] == null) return;

            long elapsed = 0;
            _pulseItems[index] = _slots[index]!.schedule.Execute(() =>
            {
                elapsed += PulseTickMs;
                float t = (elapsed % PulsePeriodMs) / (float)PulsePeriodMs;
                // Sine wave: 0→1→0 over one period
                float s = 1f + (PulseScalePeak - 1f) * Mathf.Sin(t * Mathf.PI);
                _slots[index]!.transform.scale = new Vector3(s, s, 1f);
            }).Every(PulseTickMs);
        }

        private void StopPulse(int index)
        {
            _pulseItems[index]?.Pause();
            _pulseItems[index] = null;
            if (_slots[index] != null)
                _slots[index]!.transform.scale = Vector3.one;
        }
    }
}
