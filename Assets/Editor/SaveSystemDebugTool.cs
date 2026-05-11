#nullable enable
using UnityEditor;
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Editor
{
    public static class SaveSystemDebugTool
    {
        [MenuItem("Tools/CrowdDefense/Save: Show Progress")]
        private static void ShowProgress()
        {
            var data = SaveSystem.Load();
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            Debug.Log($"[SaveSystem] cd_progression_v1:\n{json}");
        }

        [MenuItem("Tools/CrowdDefense/Save: Reset Progress")]
        private static void ResetProgress()
        {
            SaveSystem.ResetAll();
            Debug.Log("[SaveSystem] Progress reset to defaults.");
        }

        [MenuItem("Tools/CrowdDefense/Save: Mark W1-1..W1-5 Cleared")]
        private static void MarkW1Cleared()
        {
            for (int i = 1; i <= 5; i++)
                SaveSystem.MarkLevelCleared($"world1-{i}");
            var data = SaveSystem.Load();
            Debug.Log($"[SaveSystem] W1-1..W1-5 cleared. unlockedLevels count={data.unlockedLevels.Count}");
        }
    }
}
