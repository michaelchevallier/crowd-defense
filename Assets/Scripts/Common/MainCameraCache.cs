using UnityEngine;

namespace CrowdDefense.Common
{
    public static class MainCameraCache
    {
        private static Camera _cached;

        public static Camera Main
        {
            get
            {
                if (_cached == null)
                    _cached = Camera.main;
                return _cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset() => _cached = null;
    }
}
