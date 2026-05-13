#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SpeedControlController : UIControllerBase
    {
        // index 0=0.5x 1=1x 2=2x 3=3x — must match LevelRunner.SpeedTable
        private Button?[] _btns = new Button?[4];

        // Start (not Awake) — UIDocument.OnEnable populates rootVisualElement during scene
        // init; querying it from Awake races with that lifecycle and yields a null Root on
        // a shared HUD GO. Start fires after all OnEnables, so Root is guaranteed bound.
        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            var root = Root;
            if (root == null) return;
            _btns[0] = root.Q<Button>("speed-0");
            _btns[1] = root.Q<Button>("speed-1");
            _btns[2] = root.Q<Button>("speed-2");
            _btns[3] = root.Q<Button>("speed-3");

            for (int i = 0; i < _btns.Length; i++)
            {
                int captured = i;
                _btns[i]?.RegisterCallback<ClickEvent>(_ => SetSpeedIndex(captured));
            }

            int saved = SettingsRegistry.Instance?.GameSpeed ?? 1;
            ApplyIndex(saved, persist: false);
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("speed")))
                CycleForward();
            else if (Input.GetKeyDown(KeyBindings.GetKey("speed_adjust_up")) || Input.GetKeyDown(KeyCode.KeypadPlus))
                Shift(+1);
            else if (Input.GetKeyDown(KeyBindings.GetKey("speed_adjust_down")) || Input.GetKeyDown(KeyCode.KeypadMinus))
                Shift(-1);
        }

        private void CycleForward()
        {
            int current = SettingsRegistry.Instance?.GameSpeed ?? 1;
            SetSpeedIndex((current + 1) % _btns.Length);
        }

        private void Shift(int dir)
        {
            int current = SettingsRegistry.Instance?.GameSpeed ?? 1;
            SetSpeedIndex(Mathf.Clamp(current + dir, 0, _btns.Length - 1));
        }

        private void SetSpeedIndex(int index)
        {
            var state = LevelRunner.Instance?.State;
            bool playable = state == GameState.Lobby || state == GameState.WaveActive || state == GameState.WaveBreak;
            if (!playable) return;
            ApplyIndex(index, persist: true);
        }

        private void ApplyIndex(int index, bool persist)
        {
            index = Mathf.Clamp(index, 0, _btns.Length - 1);

            if (persist && SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.GameSpeed = index;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.SetGameSpeed(index);

            UpdateHighlight(index);
        }

        private void UpdateHighlight(int active)
        {
            for (int i = 0; i < _btns.Length; i++)
            {
                if (_btns[i] == null) continue;
                if (i == active) _btns[i]!.AddToClassList("speed-active");
                else _btns[i]!.RemoveFromClassList("speed-active");
            }
        }
    }
}
