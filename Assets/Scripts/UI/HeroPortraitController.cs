#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Standalone bottom-left panel: portrait circle + XP bar (hero has no HP stat,
    // XP-to-next is the closest "health" analog) + 5 perk icon slots.
    // Auto-creates its own UIDocument at runtime; wired by LevelRunner.Start or auto-spawned.
    public class HeroPortraitController : MonoSingleton<HeroPortraitController>
    {
        private const int   MaxPerkSlots  = 5;
        private const float TickInterval  = 0.2f;

        private VisualElement? _root;
        private VisualElement? _portrait;
        private VisualElement? _hpBarFill;
        private Label?         _hpLabel;
        private Label?         _killLabel;
        private VisualElement[]? _perkSlots;
        private Label[]?         _perkLabels;

        private float _tickTimer;
        private VisualElement? _xpFlashOverlay;
        private VisualElement? _damageFlashOverlay;
        private VisualElement? _flameRing;

        protected override void OnAwakeSingleton()
        {
            // Defer full init to Wire() so PanelSettings from other UIDocuments are ready.
        }

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe<HeroDamagedEvent>(OnHeroDamaged);
            StartCoroutine(FlameRingRoutine());
        }

        private void OnDisable()
        {
            EventManager.Instance?.Unsubscribe<HeroDamagedEvent>(OnHeroDamaged);
            StopCoroutine(nameof(FlameRingRoutine));
        }

        private void OnHeroDamaged(HeroDamagedEvent _)
        {
            if (_damageFlashOverlay == null) return;
            StopCoroutine(nameof(DamageFlashRoutine));
            StartCoroutine(DamageFlashRoutine());
        }

        // Flame ring: outer border pulse orange alpha 0.3→0.6→0.3 at 1Hz via sin wave.
        private IEnumerator FlameRingRoutine()
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                // sin oscillates -1..1 at 1Hz; remap to 0.3..0.6
                float alpha = 0.45f + 0.15f * Mathf.Sin(t * Mathf.PI * 2f);
                if (_flameRing != null)
                {
                    var c = new Color(1f, 0.5f, 0f, alpha);
                    _flameRing.style.borderTopColor    = new StyleColor(c);
                    _flameRing.style.borderRightColor  = new StyleColor(c);
                    _flameRing.style.borderBottomColor = new StyleColor(c);
                    _flameRing.style.borderLeftColor   = new StyleColor(c);
                }
                yield return null;
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            _damageFlashOverlay!.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 0.5f));
            yield return new WaitForSecondsRealtime(0.15f);
            _damageFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 0f));
        }

        // Called by LevelRunner.Start after SpawnHero — all scene Awakes have run by then.
        public void Wire()
        {
            if (_root != null) { Refresh(); return; }

            var doc = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();

            // Borrow PanelSettings from the HUD UIDocument (avoids duplicating the asset reference).
            foreach (var other in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (other == doc || other.panelSettings == null) continue;
                doc.panelSettings = other.panelSettings;
                break;
            }

            var styleSheet = LoadStyleSheetFromPath("Assets/UI/HeroPortrait.uss");

            _root = BuildTree(styleSheet);
            doc.rootVisualElement.Add(_root);
            doc.sortingOrder = 1;

            Refresh();
        }

        private void Update()
        {
            _tickTimer -= Time.unscaledDeltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;
            Refresh();
        }

        private void Refresh()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (_root == null) return;

            bool visible = hero != null;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            // Portrait color: avatar selection overrides HeroType.BodyColor
            if (_portrait != null)
            {
                var avatarKey = PlayerPrefs.GetString("hero_avatar", "");
                Color portraitColor;
                if (!string.IsNullOrEmpty(avatarKey) && System.Enum.TryParse<HeroAvatar>(avatarKey, out var avatar))
                    portraitColor = HeroType.AvatarColor(avatar);
                else
                {
                    var heroDef = LevelRunner.Instance?.HeroTypeDef;
                    portraitColor = heroDef != null ? heroDef.BodyColor : Color.white;
                }
                _portrait.style.backgroundColor = new StyleColor(portraitColor);
            }

            // XP bar (treated as "HP" — ratio 0..1); clamp int.MaxValue at max level.
            bool atMaxLevel = hero!.Level >= hero.MaxLevel;
            float ratio = atMaxLevel ? 1f
                : hero.XpToNext > 0 ? Mathf.Clamp01((float)hero.Xp / hero.XpToNext) : 0f;
            if (_hpBarFill != null)
            {
                _hpBarFill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
                _hpBarFill.RemoveFromClassList("hp-bar-fill-high");
                _hpBarFill.RemoveFromClassList("hp-bar-fill-mid");
                _hpBarFill.RemoveFromClassList("hp-bar-fill-low");
                _hpBarFill.AddToClassList(ratio >= 0.5f ? "hp-bar-fill-high"
                    : ratio >= 0.25f                    ? "hp-bar-fill-mid"
                    :                                     "hp-bar-fill-low");
            }

            if (_hpLabel != null)
                _hpLabel.text = atMaxLevel ? $"XP MAX" : $"XP {hero.Xp}/{hero.XpToNext}";

            if (_killLabel != null)
                _killLabel.text = $"Tues : {hero.KillCount}";

            // Perk slots
            if (_perkSlots == null || _perkLabels == null) return;
            var perks = hero.Perks;
            for (int i = 0; i < MaxPerkSlots; i++)
            {
                bool filled = i < perks.Count;
                _perkSlots[i].RemoveFromClassList(filled ? "perk-slot-empty" : "perk-slot-filled");
                _perkSlots[i].AddToClassList(filled ? "perk-slot-filled" : "perk-slot-empty");
                _perkLabels[i].text = filled && perks[i].Length > 0
                    ? perks[i][0].ToString().ToUpper()
                    : "";
            }
        }

        public void AnimateXpGain(int amount)
        {
            if (_xpFlashOverlay == null || amount <= 0) return;
            StopCoroutine(nameof(XpFlashRoutine));
            StartCoroutine(XpFlashRoutine(amount));
        }

        private IEnumerator XpFlashRoutine(int amount)
        {
            // Flash doré sur la bar XP
            _xpFlashOverlay!.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, 0.3f));
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.3f, 0f, elapsed / 0.3f);
                _xpFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, a));
                yield return null;
            }
            _xpFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, 0f));

            // Popup "+X XP" au-dessus du hero
            var hero = LevelRunner.Instance?.Hero;
            if (hero != null)
            {
                var pos = hero.transform.position + Vector3.up * 1.8f;
                FloatingPopupController.Instance?.SpawnReward($"+{amount} XP", pos, new Color(1f, 0.84f, 0f));
            }
        }

        // Build VisualElement tree in code (no .uxml dependency).
        private VisualElement BuildTree(StyleSheet? sheet)
        {
            var root = new VisualElement();
            root.AddToClassList("hero-portrait-root");
            if (sheet != null) root.styleSheets.Add(sheet);

            // Flame ring sits behind the portrait as an absolute overlay on root.
            _flameRing = new VisualElement();
            _flameRing.style.position              = Position.Absolute;
            _flameRing.style.top                   = new StyleLength(-4f);
            _flameRing.style.left                  = new StyleLength(-4f);
            _flameRing.style.right                 = new StyleLength(-4f);
            _flameRing.style.bottom                = new StyleLength(-4f);
            _flameRing.style.borderTopWidth        = 3f;
            _flameRing.style.borderRightWidth      = 3f;
            _flameRing.style.borderBottomWidth     = 3f;
            _flameRing.style.borderLeftWidth       = 3f;
            _flameRing.style.borderTopLeftRadius     = new StyleLength(54f);
            _flameRing.style.borderTopRightRadius    = new StyleLength(54f);
            _flameRing.style.borderBottomLeftRadius  = new StyleLength(54f);
            _flameRing.style.borderBottomRightRadius = new StyleLength(54f);
            _flameRing.style.backgroundColor  = new StyleColor(Color.clear);
            _flameRing.pickingMode            = PickingMode.Ignore;
            root.Add(_flameRing);

            _portrait = new VisualElement();
            _portrait.AddToClassList("portrait");
            root.Add(_portrait);

            _damageFlashOverlay = new VisualElement();
            _damageFlashOverlay.style.position   = Position.Absolute;
            _damageFlashOverlay.style.top        = 0; _damageFlashOverlay.style.left   = 0;
            _damageFlashOverlay.style.right      = 0; _damageFlashOverlay.style.bottom  = 0;
            _damageFlashOverlay.style.borderTopLeftRadius     = new StyleLength(50f);
            _damageFlashOverlay.style.borderTopRightRadius    = new StyleLength(50f);
            _damageFlashOverlay.style.borderBottomLeftRadius  = new StyleLength(50f);
            _damageFlashOverlay.style.borderBottomRightRadius = new StyleLength(50f);
            _damageFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 0f));
            _damageFlashOverlay.pickingMode = PickingMode.Ignore;
            _portrait.Add(_damageFlashOverlay);

            var info = new VisualElement();
            info.AddToClassList("hero-portrait-info");
            root.Add(info);

            var barBg = new VisualElement();
            barBg.AddToClassList("hp-bar-bg");
            info.Add(barBg);

            _hpBarFill = new VisualElement();
            _hpBarFill.AddToClassList("hp-bar-fill");
            _hpBarFill.AddToClassList("hp-bar-fill-low");
            _hpBarFill.style.width = new StyleLength(new Length(0f, LengthUnit.Percent));
            barBg.Add(_hpBarFill);

            _xpFlashOverlay = new VisualElement();
            _xpFlashOverlay.style.position   = Position.Absolute;
            _xpFlashOverlay.style.top        = 0; _xpFlashOverlay.style.left  = 0;
            _xpFlashOverlay.style.right      = 0; _xpFlashOverlay.style.bottom = 0;
            _xpFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, 0f));
            _xpFlashOverlay.pickingMode = PickingMode.Ignore;
            barBg.Add(_xpFlashOverlay);

            _hpLabel = new Label("XP 0/20");
            _hpLabel.AddToClassList("hp-label");
            info.Add(_hpLabel);

            _killLabel = new Label("Tues : 0");
            _killLabel.AddToClassList("kill-label");
            info.Add(_killLabel);

            var perksRow = new VisualElement();
            perksRow.AddToClassList("perks-row");
            info.Add(perksRow);

            _perkSlots  = new VisualElement[MaxPerkSlots];
            _perkLabels = new Label[MaxPerkSlots];
            for (int i = 0; i < MaxPerkSlots; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("perk-slot");
                slot.AddToClassList("perk-slot-empty");
                var lbl = new Label("");
                lbl.AddToClassList("perk-slot-label");
                slot.Add(lbl);
                perksRow.Add(slot);
                _perkSlots[i]  = slot;
                _perkLabels[i] = lbl;
            }

            return root;
        }

        // Fallback: attempt to load USS directly from path (Editor only; runtime uses Resources).
        private static StyleSheet? LoadStyleSheetFromPath(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
#else
            return null;
#endif
        }
    }
}
