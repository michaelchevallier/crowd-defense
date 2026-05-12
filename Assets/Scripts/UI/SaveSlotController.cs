#nullable enable
using System;
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SaveSlotController : MonoBehaviour
    {
        public static SaveSlotController? Instance { get; private set; }

        public static event Action<int>? OnSlotSelected;

        private VisualElement? _root;
        private VisualElement? _slotRoot;
        private VisualElement? _confirmDialog;
        private Label?         _confirmLabel;
        private Button?        _confirmYes;
        private Button?        _confirmNo;

        private int _pendingDeleteSlot = -1;

        private const int SlotCount = 3;

        private struct SlotRefs
        {
            public VisualElement Info;
            public Label Date;
            public Label World;
            public Label Gems;
            public Label Time;
            public Label Empty;
            public Button Continue;
            public Button NewGame;
            public Button Delete;
        }

        private SlotRefs[] _slots = new SlotRefs[SlotCount];

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; L.OnLocaleChanged -= RefreshAll; }

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null)
            {
                Debug.LogError("[SaveSlotController] UIDocument not found");
                return;
            }
            _root = uiDoc.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[SaveSlotController] rootVisualElement is null — UXML failed to load");
                return;
            }
            _slotRoot = _root.Q<VisualElement>("saveslot-root");

            _confirmDialog = _root.Q<VisualElement>("confirm-dialog");
            _confirmLabel  = _root.Q<Label>("confirm-label");
            _confirmYes    = _root.Q<Button>("confirm-yes-btn");
            _confirmNo     = _root.Q<Button>("confirm-no-btn");

            for (int i = 0; i < SlotCount; i++)
            {
                int captured = i;
                _slots[i] = new SlotRefs
                {
                    Info     = _root.Q<VisualElement>($"slot-info-{i}"),
                    Date     = _root.Q<Label>($"slot-date-{i}"),
                    World    = _root.Q<Label>($"slot-world-{i}"),
                    Gems     = _root.Q<Label>($"slot-gems-{i}"),
                    Time     = _root.Q<Label>($"slot-time-{i}"),
                    Empty    = _root.Q<Label>($"slot-empty-{i}"),
                    Continue = _root.Q<Button>($"slot-continue-{i}"),
                    NewGame  = _root.Q<Button>($"slot-new-{i}"),
                    Delete   = _root.Q<Button>($"slot-delete-{i}"),
                };
                _slots[i].Continue?.RegisterCallback<ClickEvent>(_ => SelectSlotAndContinue(captured));
                _slots[i].NewGame?.RegisterCallback<ClickEvent>(_ => StartNewGame(captured));
                _slots[i].Delete?.RegisterCallback<ClickEvent>(_ => RequestDelete(captured));
            }

            _confirmYes?.RegisterCallback<ClickEvent>(_ => ConfirmDelete());
            _confirmNo?.RegisterCallback<ClickEvent>(_ => CancelDelete());

            L.OnLocaleChanged += RefreshAll;
        }

        public void Show()
        {
            RefreshAll();
            _slotRoot?.RemoveFromClassList("hidden");
        }

        public void Hide() => _slotRoot?.AddToClassList("hidden");

        private void RefreshAll()
        {
            for (int i = 0; i < SlotCount; i++)
                RefreshSlot(i);
            ApplyLocalizedLabels();
        }

        private void RefreshSlot(int slot)
        {
            ref var refs = ref _slots[slot];
            var data = SaveSystem.PeekSlot(slot);
            bool hasData = data != null;

            if (refs.Info  != null) refs.Info.style.display  = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            if (refs.Empty != null) refs.Empty.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasData && data != null)
            {
                if (refs.Date  != null) refs.Date.text  = data.lastPlayedDate;

                // Show mid-level resume info (level + wave) when available, fallback to world reached
                if (refs.World != null)
                {
                    // Temporarily switch slot to peek MidLevelStateData without disturbing CurrentSlot
                    int savedCurrent = SaveSystem.CurrentSlot;
                    SaveSystem.SelectSlot(slot);
                    var mid = SaveSystem.LoadRunState();
                    SaveSystem.SelectSlot(savedCurrent);

                    refs.World.text = mid != null && !string.IsNullOrEmpty(mid.levelId)
                        ? L.Get("saveslot.level_wave_label", mid.levelId, mid.waveIdx)
                        : L.Get("saveslot.world_label", data.worldReached);
                }

                if (refs.Gems  != null) refs.Gems.text  = L.Get("saveslot.gems_label", data.gems);
                if (refs.Time  != null)
                {
                    int totalMin = Mathf.RoundToInt(data.playtime / 60f);
                    refs.Time.text = L.Get("saveslot.time_label", totalMin / 60, totalMin % 60);
                }
            }

            if (refs.Continue != null) refs.Continue.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            if (refs.NewGame  != null) refs.NewGame.style.display  = hasData ? DisplayStyle.None : DisplayStyle.Flex;
            if (refs.Delete   != null) refs.Delete.style.display   = hasData ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyLocalizedLabels()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var label = _root?.Q<Label>($"slot-label-{i}");
                if (label != null) label.text = L.Get("saveslot.slot_label", i + 1);

                if (_slots[i].Empty    != null) _slots[i].Empty.text    = L.Get("saveslot.empty");
                if (_slots[i].Continue != null) _slots[i].Continue.text = L.Get("saveslot.continue");
                if (_slots[i].NewGame  != null) _slots[i].NewGame.text  = L.Get("saveslot.new_game");
                if (_slots[i].Delete   != null) _slots[i].Delete.text   = L.Get("saveslot.delete");
            }

            if (_confirmLabel != null) _confirmLabel.text = L.Get("saveslot.confirm_delete");
            if (_confirmYes   != null) _confirmYes.text   = L.Get("saveslot.confirm_yes");
            if (_confirmNo    != null) _confirmNo.text    = L.Get("saveslot.confirm_no");
        }

        private void SelectSlotAndContinue(int slot)
        {
            SaveSystem.SelectSlot(slot);
            Hide();
            OnSlotSelected?.Invoke(slot);
        }

        private void StartNewGame(int slot)
        {
            SaveSystem.DeleteSlot(slot);
            SaveSystem.SelectSlot(slot);
            Hide();
            OnSlotSelected?.Invoke(slot);
        }

        private void RequestDelete(int slot)
        {
            _pendingDeleteSlot = slot;
            _confirmDialog?.RemoveFromClassList("hidden");
        }

        private void ConfirmDelete()
        {
            if (_pendingDeleteSlot >= 0)
            {
                SaveSystem.DeleteSlot(_pendingDeleteSlot);
                RefreshSlot(_pendingDeleteSlot);
            }
            _pendingDeleteSlot = -1;
            _confirmDialog?.AddToClassList("hidden");
        }

        private void CancelDelete()
        {
            _pendingDeleteSlot = -1;
            _confirmDialog?.AddToClassList("hidden");
        }
    }
}
