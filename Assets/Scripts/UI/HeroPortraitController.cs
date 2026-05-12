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
        private Label?         _levelLabel;
        private Label?         _killLabel;
        private VisualElement[]? _perkSlots;
        private Label[]?         _perkLabels;

        private float _tickTimer;
        private VisualElement? _xpFlashOverlay;
        private VisualElement? _damageFlashOverlay;
        private VisualElement? _flameRing;
        private VisualElement? _hpCircle;
        private VisualElement? _tooltip;

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

            // XP bar — blue fill, ratio 0..1.
            bool atMaxLevel = hero!.Level >= hero.MaxLevel;
            float ratio = atMaxLevel ? 1f
                : hero.XpToNext > 0 ? Mathf.Clamp01((float)hero.Xp / hero.XpToNext) : 0f;
            if (_hpBarFill != null)
            {
                _hpBarFill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
            }

            // HP circle ring: green (1.0) → yellow (0.5) → red (0.0)
            if (_hpCircle != null)
            {
                Color ringColor = ratio > 0.5f
                    ? Color.Lerp(new Color(1f, 0.85f, 0f), new Color(0.1f, 0.9f, 0.1f), (ratio - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(1f, 0.85f, 0f), ratio * 2f);
                ringColor.a = 0.85f;
                _hpCircle.style.borderTopColor    = new StyleColor(ringColor);
                _hpCircle.style.borderRightColor  = new StyleColor(ringColor);
                _hpCircle.style.borderBottomColor = new StyleColor(ringColor);
                _hpCircle.style.borderLeftColor   = new StyleColor(ringColor);
            }

            if (_levelLabel != null)
                _levelLabel.text = atMaxLevel ? $"Niv MAX" : $"Niv {hero.Level}";

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

            // HP circle ring: absolute inside portrait, between bg and damage flash.
            _hpCircle = new VisualElement();
            _hpCircle.style.position              = Position.Absolute;
            _hpCircle.style.top                   = new StyleLength(2f);
            _hpCircle.style.left                  = new StyleLength(2f);
            _hpCircle.style.right                 = new StyleLength(2f);
            _hpCircle.style.bottom                = new StyleLength(2f);
            _hpCircle.style.borderTopWidth        = 4f;
            _hpCircle.style.borderRightWidth      = 4f;
            _hpCircle.style.borderBottomWidth     = 4f;
            _hpCircle.style.borderLeftWidth       = 4f;
            _hpCircle.style.borderTopLeftRadius     = new StyleLength(50f);
            _hpCircle.style.borderTopRightRadius    = new StyleLength(50f);
            _hpCircle.style.borderBottomLeftRadius  = new StyleLength(50f);
            _hpCircle.style.borderBottomRightRadius = new StyleLength(50f);
            _hpCircle.style.backgroundColor       = new StyleColor(Color.clear);
            _hpCircle.pickingMode                 = PickingMode.Ignore;
            _portrait.Add(_hpCircle);

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

            // Tooltip: appears right of portrait on hover, hidden by default.
            _tooltip = new VisualElement();
            _tooltip.style.position          = Position.Absolute;
            _tooltip.style.left              = new StyleLength(84f);
            _tooltip.style.top               = new StyleLength(0f);
            _tooltip.style.backgroundColor   = new StyleColor(new Color(0.05f, 0.05f, 0.1f, 0.92f));
            _tooltip.style.borderTopWidth    = 1f; _tooltip.style.borderRightWidth  = 1f;
            _tooltip.style.borderBottomWidth = 1f; _tooltip.style.borderLeftWidth   = 1f;
            _tooltip.style.borderTopColor    = new StyleColor(new Color(1f, 0.5f, 0f, 0.7f));
            _tooltip.style.borderRightColor  = new StyleColor(new Color(1f, 0.5f, 0f, 0.7f));
            _tooltip.style.borderBottomColor = new StyleColor(new Color(1f, 0.5f, 0f, 0.7f));
            _tooltip.style.borderLeftColor   = new StyleColor(new Color(1f, 0.5f, 0f, 0.7f));
            _tooltip.style.borderTopLeftRadius     = new StyleLength(6f);
            _tooltip.style.borderTopRightRadius    = new StyleLength(6f);
            _tooltip.style.borderBottomLeftRadius  = new StyleLength(6f);
            _tooltip.style.borderBottomRightRadius = new StyleLength(6f);
            _tooltip.style.paddingTop    = new StyleLength(6f); _tooltip.style.paddingBottom = new StyleLength(6f);
            _tooltip.style.paddingLeft   = new StyleLength(8f); _tooltip.style.paddingRight  = new StyleLength(8f);
            _tooltip.style.minWidth      = new StyleLength(160f);
            _tooltip.style.display       = DisplayStyle.None;
            _tooltip.pickingMode         = PickingMode.Ignore;
            root.Add(_tooltip);

            root.RegisterCallback<PointerEnterEvent>(_ => RefreshTooltip());
            root.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (_tooltip != null) _tooltip.style.display = DisplayStyle.None;
            });

            var info = new VisualElement();
            info.AddToClassList("hero-portrait-info");
            root.Add(info);

            var barBg = new VisualElement();
            barBg.AddToClassList("hp-bar-bg");
            info.Add(barBg);

            _hpBarFill = new VisualElement();
            _hpBarFill.AddToClassList("hp-bar-fill");
            _hpBarFill.AddToClassList("xp-bar-fill");
            _hpBarFill.style.width = new StyleLength(new Length(0f, LengthUnit.Percent));
            barBg.Add(_hpBarFill);

            _xpFlashOverlay = new VisualElement();
            _xpFlashOverlay.style.position   = Position.Absolute;
            _xpFlashOverlay.style.top        = 0; _xpFlashOverlay.style.left  = 0;
            _xpFlashOverlay.style.right      = 0; _xpFlashOverlay.style.bottom = 0;
            _xpFlashOverlay.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f, 0f));
            _xpFlashOverlay.pickingMode = PickingMode.Ignore;
            barBg.Add(_xpFlashOverlay);

            _levelLabel = new Label("Niv 1");
            _levelLabel.AddToClassList("xp-level-label");
            info.Add(_levelLabel);

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

        private void RefreshTooltip()
        {
            if (_tooltip == null) return;
            var hero = LevelRunner.Instance?.Hero;
            var cfg  = LevelRunner.Instance?.HeroTypeDef;
            if (hero == null || cfg == null)
            {
                _tooltip.style.display = DisplayStyle.None;
                return;
            }

            _tooltip.Clear();
            bool atMax = hero.Level >= hero.MaxLevel;

            void Row(string label, string value)
            {
                var row = new VisualElement();
                row.style.flexDirection  = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom   = new StyleLength(2f);

                var lbl = new Label(label);
                lbl.style.color    = new StyleColor(new Color(0.7f, 0.7f, 0.75f));
                lbl.style.fontSize = new StyleLength(10f);
                lbl.style.unityFontStyleAndWeight = FontStyle.Normal;

                var val = new Label(value);
                val.style.color    = new StyleColor(Color.white);
                val.style.fontSize = new StyleLength(10f);
                val.style.marginLeft = new StyleLength(8f);
                val.style.unityFontStyleAndWeight = FontStyle.Bold;

                row.Add(lbl); row.Add(val);
                _tooltip.Add(row);
            }

            // Title row: avatar name + level
            var title = new Label(HeroType.AvatarName(
                System.Enum.TryParse<HeroAvatar>(PlayerPrefs.GetString("hero_avatar", ""), out var av) ? av : HeroAvatar.Warrior));
            title.style.color    = new StyleColor(new Color(1f, 0.75f, 0.2f));
            title.style.fontSize = new StyleLength(11f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = new StyleLength(4f);
            _tooltip.Add(title);

            float effectiveDmg   = cfg.Damage * hero.DamageMul;
            float effectiveRange = cfg.Range  * hero.RangeMul;
            float atkSpeedSec    = cfg.FireRateMs * hero.FireRateMul / 1000f;
            float moveSpd        = cfg.MoveSpeed * hero.MoveSpeedMul;
            float critPct        = hero.CritChance * 100f;
            float ultRemaining   = hero.UltimateCooldownRemaining;

            Row("Niveau",         atMax ? "MAX" : hero.Level.ToString());
            Row("XP",             atMax ? "MAX" : $"{hero.Xp} / {hero.XpToNext}");
            Row("DMG",            $"{effectiveDmg:F1}");
            Row("Vitesse atk",    $"{atkSpeedSec:F2}s");
            Row("Vitesse dep",    $"{moveSpd:F1}");
            Row("Portee",         $"{effectiveRange:F1}");
            Row("Crit",           $"{critPct:F0}%");
            Row("Ult cooldown",   ultRemaining > 0f ? $"{ultRemaining:F0}s" : "PRET");
            Row("Kills",          hero.KillCount.ToString());

            _tooltip.style.display = DisplayStyle.Flex;
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
