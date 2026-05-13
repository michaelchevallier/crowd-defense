#nullable enable
using UnityEngine;

namespace CrowdDefense.Systems
{
    public static class SupportMode
    {
        public static bool IsActive => PlayerPrefs.GetInt("support_mode_active", 0) == 1;

        public static float CastleHpMultiplier => IsActive ? 1.15f : 1f;
        public static float GoldMultiplier      => IsActive ? 1.20f : 1f;
        public static float MobHpMultiplier     => IsActive ? 0.85f : 1f;

        public static void Activate()   { PlayerPrefs.SetInt("support_mode_active", 1); PlayerPrefs.Save(); }
        public static void Deactivate() { PlayerPrefs.SetInt("support_mode_active", 0); PlayerPrefs.Save(); }
    }
}
