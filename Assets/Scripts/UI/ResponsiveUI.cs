using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [DefaultExecutionOrder(-100)]
    public class ResponsiveUI : MonoBehaviour
    {
        static ResponsiveUI _instance;

        [SerializeField] PanelSettings[] _panelSettings = {};

        const int BaselineWidth = 1920;
        int _lastWidth;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start() => Apply(Screen.width);

        void Update()
        {
            if (Screen.width == _lastWidth) return;
            Apply(Screen.width);
        }

        void Apply(int width)
        {
            _lastWidth = width;
#if UNITY_EDITOR
            if (width != BaselineWidth)
                Debug.Log($"[ResponsiveUI] Screen {width}px — scaling from baseline {BaselineWidth}px");
#endif
            foreach (var ps in _panelSettings)
            {
                if (ps == null) continue;
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(BaselineWidth, 1080);
                ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                ps.match = 0.5f;
            }
        }
    }
}
