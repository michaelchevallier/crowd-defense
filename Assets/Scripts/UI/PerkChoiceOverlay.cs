#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Port of V5 showNextPerkChoice() flow.
    // Can be configured via Inspector (canvas + cardContainer + cardPrefab) or fully
    // self-bootstrapped at runtime when those fields are null (procedural UGUI build).
    public class PerkChoiceOverlay : MonoBehaviour
    {
        [SerializeField] private Canvas?         canvas;
        [SerializeField] private RectTransform?  cardContainer;
        [SerializeField] private GameObject?     cardPrefab;

        private int   _queue;
        private Hero? _hero;

        // ── Self-bootstrap: called from LevelRunner.SpawnHero when no scene object exists ──
        public static PerkChoiceOverlay EnsureInstance()
        {
            var existing = FindAnyObjectByType<PerkChoiceOverlay>();
            if (existing != null) return existing;
            var go = new GameObject("PerkChoiceOverlay");
            DontDestroyOnLoad(go);
            return go.AddComponent<PerkChoiceOverlay>();
        }

        private void Start()
        {
            if (canvas != null) canvas.enabled = false;
            StartCoroutine(LateSubscribe());
        }

        private IEnumerator LateSubscribe()
        {
            while (_hero == null)
            {
                var candidate = LevelRunner.Instance?.Hero;
                if (candidate != null)
                {
                    _hero = candidate;
                    _hero.OnLevelUp += OnLevelUp;
                    yield break;
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_hero != null) _hero.OnLevelUp -= OnLevelUp;
        }

        private void OnLevelUp(int _lvl, int _xp, int _next)
        {
            // Respect SkipNextPerk modifier (daily challenges / event modifiers)
            var ctx = RunContext.Instance;
            if (ctx != null && ctx.SkipNextPerk)
            {
                ctx.SkipNextPerk = false;
                return;
            }
            _queue++;
            if (!IsOverlayVisible()) ShowNext();
        }

        private bool IsOverlayVisible() => canvas != null && canvas.enabled;

        private void ShowNext()
        {
            if (_queue <= 0 || _hero == null || PerkSystem.Instance == null) return;
            var rs = SaveSystem.GetRunState();
            var choices = PerkSystem.Instance.RollChoices(
                _hero, 3, _hero.MaxLevel - _hero.Level, rs?.schoolId ?? "");

            if (choices.Count == 0) { _queue = 0; return; }

            EnsureCanvas();
            BuildCards(choices);

            Time.timeScale = 0f;
            if (canvas != null) canvas.enabled = true;
        }

        // ── Procedural canvas (no prefab path) ───────────────────────────────────
        private void EnsureCanvas()
        {
            if (canvas != null) return;

            var go = new GameObject("PerkPickerCanvas");
            go.transform.SetParent(transform);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder    = 200;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();

            // Dark semi-transparent backdrop
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(go.transform, false);
            var img = backdrop.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.72f);
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            // Card row container (centered)
            var row = new GameObject("CardRow");
            row.transform.SetParent(go.transform, false);
            var hg = row.AddComponent<HorizontalLayoutGroup>();
            hg.spacing             = 24f;
            hg.childAlignment      = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth  = false;
            hg.childForceExpandHeight = false;
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot     = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(900f, 340f);
            cardContainer = rowRect;

            canvas.enabled = false;
        }

        private void BuildCards(List<PerkDef> choices)
        {
            if (cardContainer == null) return;
            foreach (Transform c in cardContainer) Destroy(c.gameObject);

            foreach (var def in choices)
            {
                GameObject card;
                if (cardPrefab != null)
                {
                    card = Instantiate(cardPrefab, cardContainer);
                    var view = card.GetComponent<PerkCardView>();
                    if (view != null) view.Bind(def, _hero!, OnPicked);
                }
                else
                {
                    card = BuildProceduralCard(def);
                    card.transform.SetParent(cardContainer, false);
                }
            }
        }

        // Builds a simple UGUI card: white panel + icon + name + desc + button
        private GameObject BuildProceduralCard(PerkDef def)
        {
            var card = new GameObject($"Card_{def.id}");

            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.12f, 0.18f, 0.96f);

            var csf = card.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(260f, 320f);

            var vl = card.AddComponent<VerticalLayoutGroup>();
            vl.padding              = new RectOffset(16, 16, 20, 20);
            vl.spacing              = 10f;
            vl.childAlignment       = TextAnchor.UpperCenter;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            // Icon emoji
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(card.transform, false);
            var iconText = iconGo.AddComponent<Text>();
            iconText.text      = def.iconEmoji;
            iconText.fontSize  = 36;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color     = Color.white;
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);

            // Name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(card.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.text      = def.nameKey;
            nameText.fontSize  = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color     = new Color(1f, 0.88f, 0.3f);
            nameGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);

            // Desc
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(card.transform, false);
            var descText = descGo.AddComponent<Text>();
            descText.text      = def.descKey;
            descText.fontSize  = 13;
            descText.alignment = TextAnchor.UpperCenter;
            descText.color     = new Color(0.85f, 0.85f, 0.85f);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0f, 100f);

            // Pick button
            var btnGo = new GameObject("PickBtn");
            btnGo.transform.SetParent(card.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f);
            var btn = btnGo.AddComponent<Button>();
            var captured = def;
            bool canApply = PerkSystem.Instance?.CanApply(_hero!, def) ?? true;
            btn.interactable = canApply;
            btn.onClick.AddListener(() => OnPicked(captured));
            btnImg.color = canApply ? new Color(0.2f, 0.6f, 1f) : new Color(0.35f, 0.35f, 0.35f);
            btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);

            var btnLabelGo = new GameObject("Label");
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabel = btnLabelGo.AddComponent<Text>();
            btnLabel.text      = "Choisir";
            btnLabel.fontSize  = 15;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color     = Color.white;
            var btnLabelRect = btnLabelGo.GetComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.offsetMin = Vector2.zero;
            btnLabelRect.offsetMax = Vector2.zero;

            return card;
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
