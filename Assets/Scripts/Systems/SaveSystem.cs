#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [Serializable]
    public class LevelStars
    {
        public string levelId = "";
        public int stars = 0;
    }

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
        public List<LevelStars> levelStars = new();
        public List<MetaUpgradeEntry> metaUpgradeLevels = new();
        public int gems = 0;
        public int totalKills = 0;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public string lang = "fr";
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

        // ── Stars ──

        public static int GetStars(string levelId)
        {
            var data = Load();
            for (int i = 0; i < data.levelStars.Count; i++)
                if (data.levelStars[i].levelId == levelId) return data.levelStars[i].stars;
            return 0;
        }

        public static void SetStars(string levelId, int stars)
        {
            var data = Load();
            for (int i = 0; i < data.levelStars.Count; i++)
            {
                if (data.levelStars[i].levelId == levelId)
                {
                    if (stars > data.levelStars[i].stars) { data.levelStars[i].stars = stars; Save(); }
                    return;
                }
            }
            data.levelStars.Add(new LevelStars { levelId = levelId, stars = stars });
            Save();
        }

        public static int TotalStars()
        {
            var data = Load();
            int total = 0;
            for (int i = 0; i < data.levelStars.Count; i++)
                total += data.levelStars[i].stars;
            return total;
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

        // Counts how many worlds have at least 1 cleared level
        public static int WorldsCleared()
        {
            var data = Load();
            int count = 0;
            for (int w = 1; w <= 10; w++)
            {
                for (int l = 1; l <= 8; l++)
                {
                    if (data.clearedLevels.Contains($"world{w}-{l}")) { count++; break; }
                }
            }
            return count;
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

        // ── RunState ──

        public static RunState? GetRunState()
        {
            if (_cachedRun != null) return _cachedRun;
            string json = PlayerPrefs.GetString(RUN_KEY, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                _cachedRun = JsonUtility.FromJson<RunState>(json);
            }
            catch
            {
                _cachedRun = null;
            }
            return _cachedRun;
        }

        public static void SetRunState(RunState rs)
        {
            _cachedRun = rs;
            PlayerPrefs.SetString(RUN_KEY, JsonUtility.ToJson(rs));
            PlayerPrefs.Save();
        }

        public static void AppendRunPerk(string perkId)
        {
            var rs = GetRunState() ?? new RunState();
            if (!rs.heroPerks.Contains(perkId) || IsStackable(perkId))
                rs.heroPerks.Add(perkId);
            SetRunState(rs);
        }

        public static void ClearRunState()
        {
            _cachedRun = null;
            PlayerPrefs.DeleteKey(RUN_KEY);
            PlayerPrefs.Save();
        }

        private static bool IsStackable(string id) =>
            id is "range" or "fire_rate" or "pierce" or "lifesteal" or "move_speed";
    }
}
