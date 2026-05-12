#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using CrowdDefense.Common;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    // Runtime-created overlay joystick for mobile camera pan.
    // Auto-shows on mobile if the "joystick_enabled" setting is on (default on mobile).
    // Uses Canvas + UnityEngine.UI (not UIToolkit) so it can intercept touch events
    // independently from the rest of the UI.
    [RequireComponent(typeof(Canvas))]
    public sealed class VirtualJoystick : MonoSingleton<VirtualJoystick>
    {
        private const string KJoystickEnabled = "cd.input.joystick";
        private const float  BackgroundRadius  = 40f;   // half of 80px background
        private const float  StickRadius       = 20f;   // half of 40px stick knob
        private const float  DeadZone          = 0.15f; // ignore input below this threshold

        private Canvas?                   _canvas;
        private UnityEngine.UI.Image?     _background;
        private UnityEngine.UI.Image?     _stick;
        private RectTransform?            _stickRect;
        private RectTransform?            _bgRect;
        private bool                      _touching;
        private int                       _touchId = -1;
        private Vector2                   _panDirection;

        // Public so SettingsPanelController can toggle
        public bool Enabled
        {
            get => PlayerPrefs.GetInt(KJoystickEnabled, IsMobilePlatform() ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KJoystickEnabled, value ? 1 : 0);
                PlayerPrefs.Save();
                if (_canvas != null) _canvas.enabled = value;
            }
        }

        protected override void OnAwakeSingleton()
        {
            BuildUI();
            _canvas!.enabled = Enabled;
        }

        private void BuildUI()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;

            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Background circle — bottom-left anchor, 80×80
            var bgGo = new GameObject("JoystickBg");
            bgGo.transform.SetParent(transform, false);
            _background = bgGo.AddComponent<UnityEngine.UI.Image>();
            _background.color = new Color(1f, 1f, 1f, 0.25f);
            _bgRect = _background.rectTransform;
            _bgRect.sizeDelta = new Vector2(BackgroundRadius * 2f, BackgroundRadius * 2f);
            _bgRect.anchorMin = _bgRect.anchorMax = Vector2.zero;
            _bgRect.pivot = new Vector2(0.5f, 0.5f);
            _bgRect.anchoredPosition = new Vector2(80f, 80f);  // 80px from bottom-left corner

            // Stick knob — 40×40, child of background
            var stickGo = new GameObject("JoystickStick");
            stickGo.transform.SetParent(bgGo.transform, false);
            _stick = stickGo.AddComponent<UnityEngine.UI.Image>();
            _stick.color = new Color(1f, 1f, 1f, 0.60f);
            _stickRect = _stick.rectTransform;
            _stickRect.sizeDelta = new Vector2(StickRadius * 2f, StickRadius * 2f);
            _stickRect.anchorMin = _stickRect.anchorMax = new Vector2(0.5f, 0.5f);
            _stickRect.anchoredPosition = Vector2.zero;

            // EventSystem if absent
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.enabled) return;

            HandleTouches();

            if (_touching && _panDirection.sqrMagnitude > 0.001f)
            {
                var cam = CameraController.Instance;
                if (cam != null) cam.SetPan(_panDirection);
            }
            else
            {
                CameraController.Instance?.SetPan(Vector2.zero);
            }
        }

        private void HandleTouches()
        {
            // Find a new touch that lands on the background
            if (!_touching)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase != TouchPhase.Began) continue;
                    if (!IsTouchOnBackground(t.position)) continue;
                    _touchId  = t.fingerId;
                    _touching = true;
#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
#endif
                    break;
                }
                return;
            }

            // Track the owned touch
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId != _touchId) continue;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    ResetStick();
                    return;
                }

                Vector2 bgCenter = GetBgScreenCenter();
                Vector2 delta    = t.position - bgCenter;
                float   maxDist  = BackgroundRadius;
                if (delta.magnitude > maxDist) delta = delta.normalized * maxDist;

                Vector2 raw = delta / maxDist;             // normalized -1..1
                _panDirection = ApplyDeadzoneCurve(raw);
                if (_stickRect != null)
                    _stickRect.anchoredPosition = delta;
                return;
            }

            // Touch finger lost (e.g. all touches cleared)
            ResetStick();
        }

        private bool IsTouchOnBackground(Vector2 screenPos)
        {
            if (_bgRect == null) return false;
            Vector2 center = GetBgScreenCenter();
            return Vector2.Distance(screenPos, center) <= BackgroundRadius * 1.5f;
        }

        private Vector2 GetBgScreenCenter()
        {
            if (_bgRect == null) return Vector2.zero;
            // anchoredPosition is already in screen pixels for ConstantPixelSize scaler at scale 1
            return _bgRect.anchoredPosition;
        }

        // Dead zone + sub-quadratic smooth curve so small inputs don't move the camera.
        // Remaps [DeadZone..1] → [0..1] then applies Pow(x, 2) for gentle ramp.
        private static Vector2 ApplyDeadzoneCurve(Vector2 raw)
        {
            float mag = raw.magnitude;
            if (mag < DeadZone) return Vector2.zero;
            float remapped = (mag - DeadZone) / (1f - DeadZone); // 0..1
            float curved   = Mathf.Pow(remapped, 2f);            // sub-quadratic
            return raw / mag * curved;
        }

        private void ResetStick()
        {
            _touching     = false;
            _touchId      = -1;
            _panDirection = Vector2.zero;
            if (_stickRect != null) _stickRect.anchoredPosition = Vector2.zero;
        }

        private static bool IsMobilePlatform() =>
            Application.isMobilePlatform || Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer;
    }
}
