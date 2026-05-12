#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LeaderboardController : UIControllerBase
    {
        private VisualElement? _panelRoot;
        private Label? _titleLabel;
        private VisualElement? _listContainer;
        private Label? _emptyLabel;
        private Button? _closeBtn;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _panelRoot = Root?.Q<VisualElement>("leaderboard-root");
            _titleLabel = Root?.Q<Label>("leaderboard-title");
            _listContainer = Root?.Q<VisualElement>("leaderboard-list");
            _emptyLabel = Root?.Q<Label>("leaderboard-empty");
            _closeBtn = Root?.Q<Button>("leaderboard-close-btn");

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());

            L.OnLocaleChanged += RefreshLocale;
            RefreshLocale();
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshLocale;
        }

        private void RefreshLocale()
        {
            if (_titleLabel != null) _titleLabel.text = L.Get("leaderboard.title");
            if (_closeBtn != null)   _closeBtn.text   = L.Get("leaderboard.close");
            if (_emptyLabel != null) _emptyLabel.text = L.Get("leaderboard.empty");
        }

        public void Show()
        {
            BuildList();
            _panelRoot?.RemoveFromClassList("hidden");
        }

        public void Hide() => _panelRoot?.AddToClassList("hidden");

        private void BuildList()
        {
            if (_listContainer == null) return;
            _listContainer.Clear();

            var entries = SaveSystem.GetTopScores(10);

            if (entries == null || entries.Count == 0)
            {
                if (_emptyLabel != null) _emptyLabel.RemoveFromClassList("hidden");
                return;
            }
            if (_emptyLabel != null) _emptyLabel.AddToClassList("hidden");

            // Header row
            _listContainer.Add(MakeRow(
                L.Get("leaderboard.col_rank"),
                L.Get("leaderboard.col_name"),
                L.Get("leaderboard.col_wave"),
                L.Get("leaderboard.col_score"),
                L.Get("leaderboard.col_date"),
                "leaderboard-header-row"
            ));

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var name = string.IsNullOrEmpty(e.playerName) ? "—" : e.playerName;
                var row = MakeRow(
                    (i + 1).ToString(),
                    name,
                    e.waveReached.ToString(),
                    e.score.ToString("N0"),
                    e.date,
                    i % 2 == 0 ? "leaderboard-row" : "leaderboard-row leaderboard-row-alt"
                );
                _listContainer.Add(row);
            }
        }

        private static VisualElement MakeRow(string rank, string name, string wave, string score, string date, string cssClass)
        {
            var row = new VisualElement();
            row.AddToClassList("leaderboard-row-base");
            foreach (var cls in cssClass.Split(' '))
                if (!string.IsNullOrEmpty(cls)) row.AddToClassList(cls);

            row.Add(MakeCell(rank,  "leaderboard-cell leaderboard-cell-rank"));
            row.Add(MakeCell(name,  "leaderboard-cell leaderboard-cell-name"));
            row.Add(MakeCell(wave,  "leaderboard-cell"));
            row.Add(MakeCell(score, "leaderboard-cell"));
            row.Add(MakeCell(date,  "leaderboard-cell leaderboard-cell-date"));
            return row;
        }

        private static Label MakeCell(string text, string cssClass)
        {
            var lbl = new Label(text);
            foreach (var cls in cssClass.Split(' '))
                if (!string.IsNullOrEmpty(cls)) lbl.AddToClassList(cls);
            return lbl;
        }
    }
}
