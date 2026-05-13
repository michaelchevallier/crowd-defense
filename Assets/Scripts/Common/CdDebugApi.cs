#nullable enable
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Common
{
    // Static debug API parity with V4 Phaser window.__cd.* console API.
    // Available in Editor and Development builds only.
    public static class CdDebugApi
    {
        private const string PrefKeyDebug = "cd_debug";
        private static bool _cornerWarned;

        // __cd.runner
        public static LevelRunner? Runner => LevelRunner.Instance;

        // __cd.goto — load level by ID immediately (skips avatar/hero pick screens)
        public static void Goto(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                Debug.LogWarning("[CdDebugApi] Goto: levelId is empty");
                return;
            }
            LevelLoader.LoadLevelFast(levelId);
        }

        // __cd.debugOn — enable debug overlay flag + log
        public static void DebugOn()
        {
            PlayerPrefs.SetInt(PrefKeyDebug, 1);
            PlayerPrefs.Save();
            Debug.Log("[CdDebugApi] Debug mode ON (cd_debug=1)");
        }

        // __cd.debugOff — disable debug overlay flag
        public static void DebugOff()
        {
            PlayerPrefs.SetInt(PrefKeyDebug, 0);
            PlayerPrefs.Save();
            Debug.Log("[CdDebugApi] Debug mode OFF (cd_debug=0)");
        }

        // __cd.unlockAll — unlock all levels in the active save slot
        public static void UnlockAll()
        {
            var registry = LevelRegistry.Get();
            if (registry == null)
            {
                Debug.LogWarning("[CdDebugApi] UnlockAll: LevelRegistry not found");
                return;
            }
            var data = SaveSystem.Load();
            int added = 0;
            foreach (var level in registry.Levels)
            {
                if (level == null) continue;
                if (!data.unlockedLevels.Contains(level.Id))
                {
                    data.unlockedLevels.Add(level.Id);
                    added++;
                }
            }
            SaveSystem.Save();
            Debug.Log($"[CdDebugApi] UnlockAll: {added} levels newly unlocked ({data.unlockedLevels.Count} total)");
        }

        // __cd.toggleCornerLabels — noop if MapRenderer.ToggleCornerLabels not present
        public static void ToggleCornerLabels()
        {
#if UNITY_EDITOR
            if (!_cornerWarned)
            {
                Debug.LogWarning("[CdDebugApi] ToggleCornerLabels: MapRenderer.ToggleCornerLabels not implemented");
                _cornerWarned = true;
            }
#endif
        }

        // Convenience: check if debug flag is set
        public static bool IsDebugMode => PlayerPrefs.GetInt(PrefKeyDebug, 0) == 1;
    }
}
#endif
