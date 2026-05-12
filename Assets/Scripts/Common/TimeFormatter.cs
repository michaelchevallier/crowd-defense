#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public static class TimeFormatter
    {
        public static string FormatMMSS(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
