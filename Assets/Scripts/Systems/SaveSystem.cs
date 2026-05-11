#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [Serializable]
    public class MetaUpgradeEntry
    {
        public string id = "";
        public int level = 0;
    }

    [Serializable]
    public class ProgressData
    {
        public List<string> clearedLevels = new();
        public List<string> unlockedLevels = new() { "world1-1" };
        public int totalKills = 0;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public string lang = "fr";
        public int gems = 0;
        // JsonUtility doesn't support Dictionary — flat list serialized instead
        public List<MetaUpgradeEntry> metaUpgradeLevels = new();
    }

    [Serializable]
    public class RunState
    {
        public List<string> heroPerks = new();
        public int    heroLevel = 1;
        public int    heroXP    = 0;
        public string schoolId  = "";
    }

    public static class SaveSystem
    {
        private const string KEY     = "cd_progression_v1";
        private const string RUN_KEY = "cd_runstate_v1";
        private static ProgressData? _cached;
        private static RunState?     _cachedRun;

        public static ProgressData Load()
        {
            if (_cached != null) return _cached;
            string json = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                _cached = new ProgressData();
                Save();
                return _cached;
            }
            try
            {
                _cached = JsonUtility.FromJson<ProgressData>(json) ?? new ProgressData();
            }
            catch
            {
                _cached = new ProgressData();
            }
            return _cached;
        }

        public static void Save()
        {
            if (_cached == null) return;
            string json = JsonUtility.ToJson(_cached);
            PlayerPrefs.SetString(KEY, json);
            PlayerPrefs.Save();
        }

        public static bool IsLevelCleared(string levelId) => Load().clearedLevels.Contains(levelId);
        public static bool IsLevelUnlocked(string levelId) => Load().unlockedLevels.Contains(levelId);

        public static void MarkLevelCleared(string levelId)
        {
            var data = Load();
            if (!data.clearedLevels.Contains(levelId))
                data.clearedLevels.Add(levelId);
            string nextLevel = ComputeNextLevelId(levelId);
            if (!string.IsNullOrEmpty(nextLevel) && !data.unlockedLevels.Contains(nextLevel))
                data.unlockedLevels.Add(nextLevel);
            Save();
        }

        public static void AddKills(int count)
        {
            Load().totalKills += count;
            Save();
        }

        public static void ResetAll()
        {
            _cached = new ProgressData();
            Save();
        }

        // "worldX-Y" → "worldX-(Y+1)" jusqu'à 8, puis "world(X+1)-1" jusqu'à 10
        private static string ComputeNextLevelId(string current)
        {
            if (!current.StartsWith("world")) return "";
            var rest = current.Substring(5).Split('-');
            if (rest.Length != 2) return "";
            if (!int.TryParse(rest[0], out int w) || !int.TryParse(rest[1], out int l)) return "";
            if (l < 8) return $"world{w}-{l + 1}";
            if (w < 10) return $"world{w + 1}-1";
            return "";
        }

        // ── Gems ──

        public static int GetGems() => Load().gems;

        public static void AddGems(int amount)
        {
            if (amount <= 0) return;
            Load().gems += amount;
            Save();
        }

        public static bool SpendGems(int amount)
        {
            var data = Load();
            if (data.gems < amount) return false;
            data.gems -= amount;
            Save();
            return true;
        }

        // ── MetaUpgrades ──

        public static int GetMetaUpgradeLevel(string id)
        {
            var list = Load().metaUpgradeLevels;
            for (int i = 0; i < list.Count; i++)
                if (list[i].id == id) return list[i].level;
            return 0;
        }

        public static void SetMetaUpgradeLevel(string id, int level)
        {
            var list = Load().metaUpgradeLevels;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id) { list[i].level = level; Save(); return; }
            }
            list.Add(new MetaUpgradeEntry { id = id, level = level });
            Save();
        }

        public static void ResetMetaUpgrade(string id)
        {
            var list = Load().metaUpgradeLevels;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id) { list[i].level = 0; Save(); return; }
            }
        }

        // Count distinct worlds cleared (world X cleared = world X-8 in clearedLevels)
        public static int WorldsCleared()
        {
            var cleared = Load().clearedLevels;
            int count = 0;
            for (int w = 1; w <= 10; w++)
                if (cleared.Contains($"world{w}-8")) count++;
            return count;
        }

        // Earn gems at level end: 1 base + 1 per star + 2 first-clear bonus
        public static int ComputeGemReward(string levelId, int stars, bool isFirstClear)
        {
            int reward = 1 + stars;
            if (isFirstClear) reward += 2;
            return reward;
        }
    }
}
