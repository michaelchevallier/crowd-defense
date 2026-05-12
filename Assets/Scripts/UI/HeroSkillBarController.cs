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
        private static readonly string[] SkillIds   = { "q", "w", "e" };

        // Pulse animation constants
        private const float PulseScalePeak  = 1.10f;
        private const int   PulsePeriodMs   = 700;   // full oscillation period in ms
        private const int   PulseTickMs     = 16;    // ~60 Hz tick

        // CSS classes for colour-coded states
        private const string ClassReady    = "skill-slot-ready";    // blue glow pulse
        private const string ClassCooldown = "skill-slot-cooldown"; // red tint

        private VisualElement?[]              _slots      = new VisualElement?[3];
        private VisualElement?[]              _overlays   = new VisualElement?[3];
        private Label?[]                      _keyLabels  = new Label?[3];
        private Label?[]                      _cdLabels   = new Label?[3];
        private IVisualElementScheduledItem?[] _pulseItems = new IVisualElementScheduledItem?[3];
        private bool[]                        _wasReady   = new bool[3];

        private VisualElement? _tooltip;
        private Label?         _tooltipName;
        private Label?         _tooltipCd;
        private Label?         _tooltipDesc;

        private Hero? _hero;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _tooltip     = root.Q<VisualElement>("skill-tooltip");
            _tooltipName = _tooltip?.Q<Label>("skill-tooltip-name");
            _tooltipCd   = _tooltip?.Q<Label>("skill-tooltip-cd");
            _tooltipDesc = _tooltip?.Q<Label>("skill-tooltip-desc");

            for (int i = 0; i < 3; i++)
            {
                _slots[i]    = root.Q<VisualElement>(SlotNames[i]);
                _overlays[i] = _slots[i]?.Q<VisualElement>("skill-cd-overlay");
                _keyLabels[i] = _slots[i]?.Q<Label>("skill-key-label");
                _cdLabels[i]  = _slots[i]?.Q<Label>("skill-cd-label");

                if (_keyLabels[i] != null)
                    _keyLabels[i]!.text = KeyLabels[i];
            }

            // Register click + hover callbacks
            for (int idx = 0; idx < 3; idx++)
            {
                int captured = idx;
                _slots[captured]?.RegisterCallback<ClickEvent>(_ => TriggerSlot(captured));
                _slots[captured]?.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(captured));
                _slots[captured]?.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());
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
            if (Input.GetKeyDown(KeyBindings.GetKey("skill_q"))) TriggerSlot(0);
            if (Input.GetKeyDown(KeyBindings.GetKey("skill_w"))) TriggerSlot(1);
            if (Input.GetKeyDown(KeyBindings.GetKey("skill_e"))) TriggerSlot(2);
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

            // Colour-coded state: blue pulse when ready, red tint when on cooldown
            if (_slots[index] != null)
            {
                bool nowReady = !onCooldown;
                if (onCooldown)
                {
                    _slots[index]!.RemoveFromClassList(ClassReady);
                    _slots[index]!.AddToClassList(ClassCooldown);
                    if (_wasReady[index])
                        StopPulse(index);
                }
                else
                {
                    _slots[index]!.RemoveFromClassList(ClassCooldown);
                    _slots[index]!.AddToClassList(ClassReady);
                    if (!_wasReady[index])
                        StartPulse(index);
                }
                _wasReady[index] = nowReady;
            }
        }

        private void ShowTooltip(int index)
        {
            if (_tooltip == null) return;

            string id = SkillIds[index];
            if (_tooltipName != null)
                _tooltipName.text = L.Get($"skill.{id}.name");

            if (_tooltipCd != null)
            {
                float remaining = _hero?.GetCooldownRemaining(index) ?? 0f;
                _tooltipCd.text = remaining > 0.01f
                    ? L.Get("skill.tooltip.cooldown", remaining)
                    : L.Get("skill.tooltip.ready");
            }

            if (_tooltipDesc != null)
                _tooltipDesc.text = L.Get($"skill.{id}.desc");

            // Position tooltip above the hovered slot
            if (_slots[index] != null)
            {
                var slotRect = _slots[index]!.worldBound;
                _tooltip.style.left = new StyleLength(new Length(slotRect.x, LengthUnit.Pixel));
                _tooltip.style.bottom = new StyleLength(new Length(Screen.height - slotRect.y + 8f, LengthUnit.Pixel));
            }

            _tooltip.RemoveFromClassList("hidden");
        }

        private void HideTooltip()
        {
            _tooltip?.AddToClassList("hidden");
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
