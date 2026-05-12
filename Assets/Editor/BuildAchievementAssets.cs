#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Seede 57 AchievementDef assets depuis les IDs V5 (SaveSystem.js + LevelRunner.js + RunMode.js)
    // + 6 hidden_* / wave_5_reached added in cf71283 for V4 parity.
    // Idempotent : met a jour les champs si l'asset existe deja.
    // Menu : Build > Build Achievement Registry
    public static class BuildAchievementAssets
    {
        private const string k_AchDir       = "Assets/ScriptableObjects/Achievements";
        private const string k_RegistryPath = "Assets/ScriptableObjects/Achievements/AchievementRegistry.asset";

        private readonly struct AchData
        {
            public readonly string Id;
            public readonly int Points;
            public readonly bool Hidden;
            public readonly AchievementPredicateType Predicate;
            public readonly int Threshold;
            public readonly string EventKey;

            public AchData(string id, int points = 10, bool hidden = false,
                AchievementPredicateType predicate = AchievementPredicateType.Event, int threshold = 1,
                string eventKey = "")
            {
                Id = id; Points = points; Hidden = hidden; Predicate = predicate;
                Threshold = threshold; EventKey = eventKey;
            }
        }

        // 57 achievements seeded from V5 source + game semantics
        // eventKey matches Achievements.TrackEvent("eventKey", delta) call sites
        private static readonly AchData[] k_Defs = new AchData[]
        {
            // ── V5 explicit (kills / perfection) ────────────────────────────────
            new("kills_100",             20,  false, AchievementPredicateType.Counter, 100,    "enemy_killed"),
            new("kills_1000",            30,  false, AchievementPredicateType.Counter, 1000,   "enemy_killed"),
            new("kills_10000",           50,  true,  AchievementPredicateType.Counter, 10000,  "enemy_killed"),
            new("perfect_world1",        30,  false, AchievementPredicateType.Event,   1),
            new("apocalypse_unlocked",   20,  false, AchievementPredicateType.Event,   1),
            new("apocalypse_master",     50,  true,  AchievementPredicateType.Event,   1),

            // ── V5 daily streak ──────────────────────────────────────────────────
            new("daily_streak_3",        10,  false, AchievementPredicateType.Counter, 3,      "daily_streak"),
            new("daily_streak_7",        20,  false, AchievementPredicateType.Counter, 7,      "daily_streak"),
            new("daily_streak_14",       30,  false, AchievementPredicateType.Counter, 14,     "daily_streak"),
            new("daily_streak_30",       50,  true,  AchievementPredicateType.Counter, 30,     "daily_streak"),

            // ── V5 run mode (RunMode.js) ─────────────────────────────────────────
            new("run_first_feu",         20,  false, AchievementPredicateType.Event,   1),
            new("run_first_givre",       20,  false, AchievementPredicateType.Event,   1),
            new("run_first_maconnerie",  20,  false, AchievementPredicateType.Event,   1),
            new("run_master_all_schools",50,  true,  AchievementPredicateType.Event,   1),

            // ── World completions (W1-W10) ───────────────────────────────────────
            new("world1_complete",       10,  false, AchievementPredicateType.Event,   1),
            new("world2_complete",       15,  false, AchievementPredicateType.Event,   1),
            new("world3_complete",       20,  false, AchievementPredicateType.Event,   1),
            new("world4_complete",       20,  false, AchievementPredicateType.Event,   1),
            new("world5_complete",       25,  false, AchievementPredicateType.Event,   1),
            new("world6_complete",       30,  false, AchievementPredicateType.Event,   1),
            new("world7_complete",       30,  false, AchievementPredicateType.Event,   1),
            new("world8_complete",       35,  false, AchievementPredicateType.Event,   1),
            new("world9_complete",       40,  false, AchievementPredicateType.Event,   1),
            new("world10_complete",      50,  false, AchievementPredicateType.Event,   1),

            // ── Boss kills (LevelRunner boss drops / skins.js) ───────────────────
            new("boss_killer",           30,  false, AchievementPredicateType.Event,   1),
            new("kill_brigand_boss",     20,  false, AchievementPredicateType.Event,   1),
            new("kill_corsair_boss",     20,  false, AchievementPredicateType.Event,   1),
            new("kill_warlord_boss",     20,  false, AchievementPredicateType.Event,   1),
            new("kill_dragon_boss",      25,  false, AchievementPredicateType.Event,   1),
            new("kill_kraken_boss",      25,  false, AchievementPredicateType.Event,   1),
            new("kill_wizard_king",      30,  false, AchievementPredicateType.Event,   1),
            new("kill_cosmic_boss",      35,  false, AchievementPredicateType.Event,   1),
            new("kill_apocalypse_boss",  50,  true,  AchievementPredicateType.Event,   1),

            // ── Progression milestones ──────────────────────────────────────────
            new("tutorial_done",          5,  false, AchievementPredicateType.Event,   1),
            new("first_blood",            5,  false, AchievementPredicateType.Event,   1),
            new("wave_clear_10",         10,  false, AchievementPredicateType.Counter, 10,     "wave_cleared"),
            new("wave_clear_100",        30,  true,  AchievementPredicateType.Counter, 100,    "wave_cleared"),
            new("tower_master",          20,  false, AchievementPredicateType.Counter, 50,     "tower_placed"),
            new("synergy_master",        30,  false, AchievementPredicateType.Counter, 10,     "synergy_activated"),
            new("untouched_castle",      30,  false, AchievementPredicateType.Event,   1),
            new("untouched_world",       50,  true,  AchievementPredicateType.Event,   1),

            // ── Economy ──────────────────────────────────────────────────────────
            new("million_gold",          30,  false, AchievementPredicateType.Counter, 1000000,"gold_earned"),
            new("hoarder",               20,  false, AchievementPredicateType.Event,   1),

            // ── Speedrun ─────────────────────────────────────────────────────────
            new("speedrun_w1_1",         20,  false, AchievementPredicateType.Event,   1),
            new("speedrun_any_world",    30,  false, AchievementPredicateType.Event,   1),

            // ── Meta / miscellaneous ─────────────────────────────────────────────
            new("no_sell",               20,  false, AchievementPredicateType.Event,   1),
            new("max_upgrade_tower",     15,  false, AchievementPredicateType.Event,   1),
            new("all_tower_types",       25,  false, AchievementPredicateType.Counter, 12,     "tower_type_built"),
            new("perk_collector",        20,  false, AchievementPredicateType.Counter, 20,     "perk_collected"),
            new("doctrine_active",       10,  false, AchievementPredicateType.Event,   1),
            new("legendary_skin",        30,  true,  AchievementPredicateType.Event,   1),

            // ── Hidden + wave milestone (V4 parity, cf71283) ─────────────────────
            new("hidden_bankrupt",       40,  true,  AchievementPredicateType.Event,   1,    "bankrupt_win"),
            new("hidden_boss_lover",     40,  true,  AchievementPredicateType.Counter, 5,    "boss_killed"),
            new("hidden_speedrun",       50,  true,  AchievementPredicateType.Event,   1,    "speedrun_win"),
            new("hidden_pacifist",       50,  true,  AchievementPredicateType.Event,   1,    "pacifist_win"),
            new("hidden_hoarder",        30,  true,  AchievementPredicateType.Counter, 1000, "gold_snapshot"),
            new("wave_5_reached",        10,  false, AchievementPredicateType.Event,   1),
        };

        [MenuItem("Build/Build Achievement Registry")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_AchDir);

            var defs = new AchievementDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = SaveDef(k_Defs[i]);

            UpdateRegistry(defs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#if UNITY_EDITOR
            Debug.Log($"[BuildAchievementAssets] done — {defs.Length} achievements seeded.");
#endif
        }

        private static AchievementDef SaveDef(in AchData d)
        {
            string path = $"{k_AchDir}/{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AchievementDef>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<AchievementDef>();
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            so.FindProperty("id").stringValue          = d.Id;
            so.FindProperty("titleKey").stringValue    = $"ach.{d.Id}.title";
            so.FindProperty("descKey").stringValue     = $"ach.{d.Id}.desc";
            so.FindProperty("iconPath").stringValue    = $"UI/Achievements/{d.Id}";
            so.FindProperty("hidden").boolValue        = d.Hidden;
            so.FindProperty("points").intValue         = d.Points;
            so.FindProperty("predicateType").enumValueIndex = (int)d.Predicate;
            so.FindProperty("threshold").intValue      = d.Threshold;
            so.FindProperty("eventKey").stringValue    = d.EventKey;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void UpdateRegistry(AchievementDef[] defs)
        {
            var reg = AssetDatabase.LoadAssetAtPath<AchievementRegistry>(k_RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<AchievementRegistry>();
                AssetDatabase.CreateAsset(reg, k_RegistryPath);
            }

            var so = new SerializedObject(reg);
            var prop = so.FindProperty("defs");
            prop.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }
    }
}
#endif
