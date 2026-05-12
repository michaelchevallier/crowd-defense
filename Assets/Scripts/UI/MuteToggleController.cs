#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MuteToggleController : MonoBehaviour
    {
        private const string PrefKey = "cd.audio.muted";

        private Button? _btn;
        private bool _muted;

        private void Start()
        {
            _muted = PlayerPrefs.GetInt(PrefKey, 0) == 1;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _btn = root.Q<Button>("btn-mute");
            if (_btn == null) return;

            _btn.RegisterCallback<ClickEvent>(_ => Toggle());
            ApplyMute(_muted);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("mute"))) Toggle();
        }

        private void Toggle()
        {
            _muted = !_muted;
            PlayerPrefs.SetInt(PrefKey, _muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute(_muted);
            Toast.Show(_muted ? "Audio coupé" : "Audio activé", "", 1800, _muted ? "🔇" : "🔊", ToastType.Info);
        }

        private void ApplyMute(bool muted)
        {
            MusicManager.Instance?.SetMuted(muted);
            AudioController.Instance?.SetMuted(muted);
            if (_btn != null)
                _btn.text = muted ? "\U0001F507" : "\U0001F50A";
        }
    }
}
