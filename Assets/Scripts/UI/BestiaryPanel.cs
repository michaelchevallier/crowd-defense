#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class BestiaryPanel : MonoSingleton<BestiaryPanel>
    {
        private VisualElement? _root;
        private VisualElement? _grid;
        private string?        _recentlyUnlockedId;

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            _root = doc.Q<VisualElement>("bestiary-root");
            _grid = doc.Q<VisualElement>("bestiary-grid");

            var btnBack = doc.Q<Button>("btn-bestiary-back");
            if (btnBack != null) btnBack.clicked += Hide;

            Bestiary.OnFirstKill += OnFirstKill;
        }

        protected override void OnDestroySingleton()
        {
            Bestiary.OnFirstKill -= OnFirstKill;
        }

        private void OnFirstKill(string id)
        {
            _recentlyUnlockedId = id;

            var registry = Resources.Load<EnemyRegistry>("EnemyRegistry");
            string displayName = id;
            if (registry != null)
            {
                foreach (var et in registry.Enemies)
                {
                    if (et != null && et.Id == id)
                    {
                        displayName = string.IsNullOrEmpty(et.DisplayName) ? id : et.DisplayName;
                        break;
                    }
                }
            }

            Toast.Show("Nouveau bestiaire", displayName, 3500, "📖", ToastType.Achievement);
        }

        public bool IsOpen => _root != null && !_root.ClassListContains("hidden");

        public void Show()
        {
            if (_root == null) return;
            Rebuild();
            _root.RemoveFromClassList("hidden");
        }

        public void Hide() => _root?.AddToClassList("hidden");

        private void Rebuild()
        {
            if (_grid == null) return;
            _grid.Clear();

            var registry = Resources.Load<EnemyRegistry>("EnemyRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[BestiaryPanel] EnemyRegistry not found in Resources.");
#endif
                return;
            }

            var bestiary = Bestiary.Instance;

            foreach (var et in registry.Enemies)
            {
                if (et == null) continue;
                bool unlocked = bestiary?.IsUnlocked(et.Id) ?? false;
                int  kills    = bestiary?.KillCount(et.Id) ?? 0;
                bool flash    = _recentlyUnlockedId == et.Id;

                var card = BuildCard(et, unlocked, kills);
                _grid.Add(card);

                if (flash)
                {
                    _recentlyUnlockedId = null;
                    StartCoroutine(FlashGoldBorder(card));
                }
            }
        }

        private static VisualElement BuildCard(EnemyType et, bool unlocked, int kills)
        {
            var card = new VisualElement();
            card.AddToClassList("bst-card");
            if (unlocked) card.AddToClassList("bst-card--unlocked");

            // Portrait: colored square representing BodyColor
            var portrait = new VisualElement();
            portrait.AddToClassList("bst-portrait");
            if (unlocked)
                portrait.style.backgroundColor = et.BodyColor;
            else
                portrait.style.backgroundColor = new Color(0.25f, 0.25f, 0.28f);
            card.Add(portrait);

            if (unlocked)
            {
                var name = new Label(string.IsNullOrEmpty(et.DisplayName) ? et.Id : et.DisplayName);
                name.AddToClassList("bst-name");
                card.Add(name);

                var stats = new Label($"PV {et.Hp:0}  Vit {et.Speed:0.0}  Dmg {et.Damage}  Or {et.Reward}c");
                stats.AddToClassList("bst-stats");
                card.Add(stats);

                var traits = BuildTraits(et);
                if (!string.IsNullOrEmpty(traits))
                {
                    var traitLabel = new Label(traits);
                    traitLabel.AddToClassList("bst-traits");
                    card.Add(traitLabel);
                }

                var killLabel = new Label($"Tues: {kills}");
                killLabel.AddToClassList("bst-kills");
                card.Add(killLabel);
            }
            else
            {
                var locked = new Label("Decouvrez\nen jeu");
                locked.AddToClassList("bst-locked");
                card.Add(locked);
            }

            return card;
        }

        private static string BuildTraits(EnemyType et)
        {
            var tags = new System.Collections.Generic.List<string>(4);
            if (et.IsBoss || et.IsMidBoss || et.IsApocalypseBoss) tags.Add("Boss");
            if (et.IsFlyer)   tags.Add("Volant");
            if (et.IsStealth) tags.Add("Furtif");
            if (et.ShieldHP > 0f) tags.Add($"Bouclier {et.ShieldHP:0}");
            if (et.SummonsMinions) tags.Add("Invocateur");
            if (et.IsBrigand) tags.Add("Charge");
            return string.Join(" | ", tags);
        }

        private static IEnumerator FlashGoldBorder(VisualElement card)
        {
            card.AddToClassList("bst-card--flash");
            yield return new WaitForSeconds(0.3f);
            card.RemoveFromClassList("bst-card--flash");
        }
    }
}
