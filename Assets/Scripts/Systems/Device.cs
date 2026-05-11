#nullable enable
using UnityEngine;

namespace CrowdDefense.Systems
{
    public static class Device
    {
        public static bool IsMobile =>
            SystemInfo.deviceType == DeviceType.Handheld ||
            Application.isMobilePlatform;

        public static bool IsTouch => Input.touchSupported;

        // Portrait when screen height > width (typical phone mode)
        public static bool IsPortrait => Screen.height > Screen.width;

        // Small screen — same threshold as V5 Device.js (768px short edge)
        public static bool IsSmallScreen =>
            Mathf.Min(Screen.width, Screen.height) < 768;
    }
}
