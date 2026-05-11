#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [Serializable]
    public class SkinEquipEntry
    {
        public string targetType = "";
        public string targetId = "";
        public string skinId = "";
    }

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
        public int totalGoldEarned = 0;
        public int totalWavesCleared = 0;
        public int bestStreak = 0;
        public float playtime = 0f;
        public int towersPlaced = 0;
        public int perksAcquired = 0;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public string lang = "fr";
        public bool tutorialCompleted = false;
        public string lastPlayedDate = "";
        public int worldReached = 1;
        // Skins — owned ids (default skins are always owned)
        public List<string> ownedSkins = new();
        // Active equipped skin per (targetType+targetId) key — flat list for JsonUtility
        public List<SkinEquipEntry> equippedSkins = new();
    }

    [Serializable]
    public class RunState
    {
        public List<string> heroPerks = new();
        public int    heroLevel = 1;
        public int    heroXP    = 0;
        public string schoolId  = "";
        // Run-scoped stats (accumulated this run, merged to lifetime on victory/game-over)
        public int    runKills       = 0;
        public int    runGoldEarned  = 0;
        public int    runWavesCleared = 0;
        public int    runStreak      = 0;
        public float  runPlaytime    = 0f;
        public int    runTowersPlaced = 0;
        public int    runPerksAcquired = 0;
    }

    public static class SaveSystem
    {
        private const string KEY_PREFIX     = "cd_progression_v1_slot";
        private const string RUN_KEY_PREFIX = "cd_runstate_v1_slot";
        private const int    SLOT_COUNT     = 3;

        public static int CurrentSlot { get; private set; } = 0;

        private static ProgressData?[] _cachedSlots = new ProgressData?[SLOT_COUNT];
        private static RunState?[]     _cachedRuns  = new RunState?[SLOT_COUNT];

        private static string ProgressKey(int slot) => $"{KEY_PREFIX}{slot}";
        private static string RunKey(int slot)      => $"{RUN_KEY_PREFIX}{slot}";

        public static void SelectSlot(int slot)
        {
            CurrentSlot = Mathf.Clamp(slot, 0, SLOT_COUNT - 1);
        }

        public static bool SlotHasData(int slot)
        {
            int s = Mathf.Clamp(slot, 0, SLOT_COUNT - 1);
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(ProgressKey(s), ""));
        }

        public static ProgressData? PeekSlot(int slot)
        {
            int s = Mathf.Clamp(slot, 0, SLOT_COUNT - 1);
            if (_cachedSlots[s] != null) return _cachedSlots[s];
            string json = PlayerPrefs.GetString(ProgressKey(s), "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<ProgressData>(json); }
            catch { return null; }
        }

        public static void DeleteSlot(int slot)
        {
            int s = Mathf.Clamp(slot, 0, SLOT_COUNT - 1);
            _cachedSlots[s] = null;
            _cachedRuns[s]  = null;
            PlayerPrefs.DeleteKey(ProgressKey(s));
            PlayerPrefs.DeleteKey(RunKey(s));
            PlayerPrefs.Save();
        }

        public static ProgressData Load()
        {
            int s = CurrentSlot;
            if (_cachedSlots[s] != null) return _cachedSlots[s]!;
            string json = PlayerPrefs.GetString(ProgressKey(s), "");
            if (string.IsNullOrEmpty(json))
            {
                _cachedSlots[s] = new ProgressData();
                Save();
                return _cachedSlots[s]!;
            }
            try
            {
                _cachedSlots[s] = JsonUtility.FromJson<ProgressData>(json) ?? new ProgressData();
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                PlayerPrefs.SetString(ProgressKey(s) + "_corrupted", json);
                Debug.LogWarning($"[SaveSystem] Progression slot{s} corrompue, reset silencieux: {ex.Message}");
#endif
                _cachedSlots[s] = new ProgressData();
            }
            return _cachedSlots[s]!;
        }

        public static void Save()
        {
            int s = CurrentSlot;
            if (_cachedSlots[s] == null) return;
            var data = _cachedSlots[s]!;
            data.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(ProgressKey(s), json);
            PlayerPrefs.Save();
        }

        // ── Tutorial ──

        public static bool IsTutorialCompleted() => Load().tutorialCompleted;

        public static void SetTutorialCompleted()
        {
            Load().tutorialCompleted = true;
            Save();
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
            // Track highest world reached
            if (levelId.StartsWith("world"))
            {
                var parts = levelId.Substring(5).Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int w))
                    if (w > data.worldReached) data.worldReached = w;
            }
            Save();
        }

        public static void AddKills(int count)
        {
            Load().totalKills += count;
            Save();
        }

        public static void AddGoldEarned(int amount)
        {
            if (amount <= 0) return;
            Load().totalGoldEarned += amount;
            Save();
        }

        public static void AddWavesCleared(int count)
        {
            if (count <= 0) return;
            Load().totalWavesCleared += count;
            Save();
        }

        public static void UpdateBestStreak(int streak)
        {
            var data = Load();
            if (streak > data.bestStreak) { data.bestStreak = streak; Save(); }
        }

        public static void AddPlaytime(float seconds)
        {
            if (seconds <= 0) return;
            Load().playtime += seconds;
            Save();
        }

        public static void AddTowersPlaced(int count)
        {
            if (count <= 0) return;
            Load().towersPlaced += count;
            Save();
        }

        public static void AddPerksAcquired(int count)
        {
            if (count <= 0) return;
            Load().perksAcquired += count;
            Save();
        }

        public static void ResetLifetimeStats()
        {
            var data = Load();
            data.totalKills       = 0;
            data.totalGoldEarned  = 0;
            data.totalWavesCleared = 0;
            data.bestStreak       = 0;
            data.playtime         = 0f;
            data.towersPlaced     = 0;
            data.perksAcquired    = 0;
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

        public static void ResetAll()
        {
            _cachedSlots[CurrentSlot] = new ProgressData();
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

        // ── RunState (Hero perks/level/XP across levels) ──

        public static RunState GetRunState()
        {
            int s = CurrentSlot;
            if (_cachedRuns[s] != null) return _cachedRuns[s]!;
            string json = PlayerPrefs.GetString(RunKey(s), "");
            _cachedRuns[s] = string.IsNullOrEmpty(json) ? new RunState() : JsonUtility.FromJson<RunState>(json) ?? new RunState();
            return _cachedRuns[s]!;
        }

        public static void SetRunState(RunState rs)
        {
            int s = CurrentSlot;
            _cachedRuns[s] = rs;
            PlayerPrefs.SetString(RunKey(s), JsonUtility.ToJson(rs));
            PlayerPrefs.Save();
        }

        public static void AppendRunPerk(string perkId)
        {
            var rs = GetRunState();
            rs.heroPerks.Add(perkId);
            rs.runPerksAcquired++;
            SetRunState(rs);
        }

        public static void ClearRunState()
        {
            int s = CurrentSlot;
            _cachedRuns[s] = new RunState();
            PlayerPrefs.DeleteKey(RunKey(s));
            PlayerPrefs.Save();
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

        // ── Skins ──

        public static bool IsSkinOwned(string skinId)
        {
            return Load().ownedSkins.Contains(skinId);
        }

        public static void UnlockSkin(string skinId)
        {
            var data = Load();
            if (!data.ownedSkins.Contains(skinId))
            {
                data.ownedSkins.Add(skinId);
                Save();
            }
        }

        public static string? GetEquippedSkin(string targetType, string targetId)
        {
            var list = Load().equippedSkins;
            for (int i = 0; i < list.Count; i++)
                if (list[i].targetType == targetType && list[i].targetId == targetId)
                    return list[i].skinId;
            return null;
        }

        public static void SetEquippedSkin(string targetType, string targetId, string skinId)
        {
            var list = Load().equippedSkins;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].targetType == targetType && list[i].targetId == targetId)
                {
                    list[i].skinId = skinId;
                    Save();
                    return;
                }
            }
            list.Add(new SkinEquipEntry { targetType = targetType, targetId = targetId, skinId = skinId });
            Save();
        }
    }
}
