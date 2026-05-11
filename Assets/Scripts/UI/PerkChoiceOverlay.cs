#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Port of V5 showNextPerkChoice() flow.
    // Attach to a Canvas GameObject. Requires cardContainer + cardPrefab (has PerkCardView).
    public class PerkChoiceOverlay : MonoBehaviour
    {
        [SerializeField] private Canvas?         canvas;
        [SerializeField] private RectTransform?  cardContainer;
        [SerializeField] private GameObject?     cardPrefab;

        private int   _queue;
        private Hero? _hero;

        private void Start()
        {
            _hero = LevelRunner.Instance?.Hero;
            if (_hero != null) _hero.OnLevelUp += OnLevelUp;
            if (canvas != null) canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (_hero != null) _hero.OnLevelUp -= OnLevelUp;
        }

        private void OnLevelUp(int _lvl, int _xp, int _next)
        {
            _queue++;
            if (canvas != null && !canvas.enabled) ShowNext();
        }

        private void ShowNext()
        {
            if (_queue <= 0 || _hero == null || PerkSystem.Instance == null) return;
            var rs = SaveSystem.GetRunState();
            var choices = PerkSystem.Instance.RollChoices(
                _hero, 3, _hero.MaxLevel - _hero.Level, rs?.schoolId ?? "");

            if (choices.Count == 0) { _queue = 0; return; }

            if (cardContainer != null)
            {
                foreach (Transform c in cardContainer) Destroy(c.gameObject);
                foreach (var def in choices)
                {
                    if (cardPrefab == null) continue;
                    var card = Instantiate(cardPrefab, cardContainer);
                    var view = card.GetComponent<PerkCardView>();
                    if (view != null) view.Bind(def, _hero, OnPicked);
                }
            }

            Time.timeScale = 0f;
            if (canvas != null) canvas.enabled = true;
        }

        private void OnPicked(PerkDef def)
        {
            if (_hero == null || PerkSystem.Instance == null) return;

            PerkSystem.Instance.ApplyPerk(_hero, def);
            SaveSystem.AppendRunPerk(def.id);

            if (canvas != null) canvas.enabled = false;
            _queue--;
            Time.timeScale = 1f;

            if (_queue > 0) StartCoroutine(NextAfterDelay());
        }

        private IEnumerator NextAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.15f);
            ShowNext();
        }
    }
}
