#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SpeedControlController : MonoBehaviour
    {
        private Button? btn1;
        private Button? btn2;
        private Button? btn3;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[SpeedControl] No UIDocument component found.");
#endif
                return;
            }

            var root = doc.rootVisualElement;
            btn1 = root.Q<Button>("speed-1");
            btn2 = root.Q<Button>("speed-2");
            btn3 = root.Q<Button>("speed-3");

            if (btn1 == null || btn2 == null || btn3 == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SpeedControl] Buttons not found: btn1={btn1}, btn2={btn2}, btn3={btn3}");
#endif
                return;
            }

            btn1.RegisterCallback<ClickEvent>(_ => SetSpeed(1));
            btn2.RegisterCallback<ClickEvent>(_ => SetSpeed(2));
            btn3.RegisterCallback<ClickEvent>(_ => SetSpeed(3));

            int saved = SettingsRegistry.Instance?.GameSpeed ?? 1;
            ApplySpeed(saved, persist: false);
        }

        private void OnDestroy()
        {
            // Reset timeScale so overlay / menu scenes aren't left fast
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                CycleSpeed(+1);
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                CycleSpeed(-1);
        }

        private void CycleSpeed(int dir)
        {
            int current = SettingsRegistry.Instance?.GameSpeed ?? 1;
            int next = Mathf.Clamp(current + dir, 1, 3);
            SetSpeed(next);
        }

        private void SetSpeed(int speed)
        {
            if (LevelRunner.Instance?.State != GameState.Play) return;
            ApplySpeed(speed, persist: true);
        }

        private void ApplySpeed(int speed, bool persist)
        {
            if (persist && SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.GameSpeed = speed;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.SetGameSpeed(speed);

            UpdateButtonHighlight(speed);
        }

        private void UpdateButtonHighlight(int speed)
        {
            SetActive(btn1, speed == 1);
            SetActive(btn2, speed == 2);
            SetActive(btn3, speed == 3);
        }

        private static void SetActive(Button? btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("speed-active");
            else btn.RemoveFromClassList("speed-active");
        }
    }
}
