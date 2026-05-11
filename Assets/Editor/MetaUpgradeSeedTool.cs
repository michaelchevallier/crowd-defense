#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class MetaUpgradeSeedTool
    {
        private const string DefDir      = "Assets/ScriptableObjects/MetaUpgrades";
        private const string RegistryPath = "Assets/Resources/MetaUpgradeRegistry.asset";

        [MenuItem("CrowdDefense/Seed MetaUpgrade Assets", priority = 51)]
        public static void SeedAll()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", DefDir));
            AssetDatabase.Refresh();

            var defs = BuildDefs();
            foreach (var def in defs)
                SaveDef(def);

            AssetDatabase.SaveAssets();

            var registry = LoadOrCreateRegistry();
            var so       = new SerializedObject(registry);
            var defsProp = so.FindProperty("defs");
            defsProp.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var loaded = AssetDatabase.LoadAssetAtPath<MetaUpgradeDef>($"{DefDir}/{defs[i].id}.asset");
                defsProp.GetArrayElementAtIndex(i).objectReferenceValue = loaded;
            }

            var resetProp = so.FindProperty("resetCostGems");
            if (resetProp != null) resetProp.intValue = 10;
            var t2Prop = so.FindProperty("tier2UnlockWorldsCleared");
            if (t2Prop != null) t2Prop.intValue = 1;
            var t3Prop = so.FindProperty("tier3UnlockWorldsCleared");
            if (t3Prop != null) t3Prop.intValue = 2;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MetaUpgradeSeedTool] Seeded {defs.Length} MetaUpgradeDef assets + MetaUpgradeRegistry.");
        }

        private static MetaUpgradeRegistry LoadOrCreateRegistry()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MetaUpgradeRegistry>(RegistryPath);
            if (existing != null) return existing;
            existing = ScriptableObject.CreateInstance<MetaUpgradeRegistry>();
            AssetDatabase.CreateAsset(existing, RegistryPath);
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static MetaUpgradeDef[] BuildDefs() => new[]
        {
            Def("castle_hp",      "Forteresse",       "+10/+20/+30% PV chateau",         "T1", MetaUpgradeCategory.Combat,   1, new[]{ "+10% PV",      "+20% PV",      "+30% PV"      }, "castleHPMul",           0.10f),
            Def("hero_dmg",       "Lame affutee",     "+10/+20/+30% degats hero",         "S",  MetaUpgradeCategory.Combat,   1, new[]{ "+10% dmg",     "+20% dmg",     "+30% dmg"     }, "heroDamageMul",         0.10f),
            Def("coins_start",    "Bourse pleine",    "+50/+100/+200 or de depart",       "$",  MetaUpgradeCategory.Economy,  1, new[]{ "+50",          "+100",         "+200"         }, "startCoinsBonus",       50f),
            Def("xp_boost",       "Experience accrue","+25/+50/+100% XP par kill",        "X",  MetaUpgradeCategory.Utility,  1, new[]{ "+25% XP",      "+50% XP",      "+100% XP"     }, "xpMul",                 0.25f),
            Def("hero_range",     "Oeil de faucon",   "+10/+20/+30% portee hero",         "O",  MetaUpgradeCategory.Combat,   2, new[]{ "+10% portee",  "+20% portee",  "+30% portee"  }, "heroRangeMul",          0.10f),
            Def("coin_multi",     "Marchand prospere","+15/+30/+50% or par kill",         "$+", MetaUpgradeCategory.Economy,  2, new[]{ "+15% or",      "+30% or",      "+50% or"      }, "coinGainMul",           0.15f),
            Def("perk_reroll",    "Re-roll des perks","+1/+2/+3 choix de perks",          "P",  MetaUpgradeCategory.Utility,  2, new[]{ "+1 perk",      "+2 perks",     "+3 perks"     }, "perkChoiceCountBonus",  1f),
            Def("hero_fire_rate", "Cadence d'enfer",  "+10/+20/+30% cadence hero",        "F",  MetaUpgradeCategory.Combat,   3, new[]{ "+10% cadence", "+20% cadence", "+30% cadence" }, "heroFireRateMul",       0.10f),
            Def("gem_multi",      "Aimant a gemmes",  "+25/+50/+100% gemmes par boss",    "G",  MetaUpgradeCategory.Economy,  3, new[]{ "+25%",         "+50%",         "+100%"        }, "gemGainMul",            0.25f),
            Def("tower_discount", "Architecte malin", "-10/-20/-30% cout upgrades tours", "A",  MetaUpgradeCategory.Utility,  3, new[]{ "-10% cout",    "-20% cout",    "-30% cout"    }, "towerUpgradeDiscount",  0.10f),
        };

        private static MetaUpgradeDef Def(
            string id, string displayName, string description, string emoji,
            MetaUpgradeCategory cat, int tier, string[] labels,
            string effectKey, float valuePerLevel)
        {
            var def = ScriptableObject.CreateInstance<MetaUpgradeDef>();
            def.name           = id;
            def.id             = id;
            def.displayName    = displayName;
            def.description    = description;
            def.iconEmoji      = emoji;
            def.category       = cat;
            def.tier           = tier;
            def.maxLevel       = 3;
            def.costsPerLevel  = new[] { 5, 15, 40 };
            def.perLevelLabels = labels;
            def.effects        = new[] { new MetaUpgradeEffect { key = effectKey, valuePerLevel = valuePerLevel } };
            return def;
        }

        private static void SaveDef(MetaUpgradeDef def)
        {
            string path = $"{DefDir}/{def.id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MetaUpgradeDef>(path);
            if (existing != null)
            {
                existing.displayName    = def.displayName;
                existing.description    = def.description;
                existing.iconEmoji      = def.iconEmoji;
                existing.category       = def.category;
                existing.tier           = def.tier;
                existing.maxLevel       = def.maxLevel;
                existing.costsPerLevel  = def.costsPerLevel;
                existing.perLevelLabels = def.perLevelLabels;
                existing.effects        = def.effects;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(def, path);
            }
        }
    }
}
#endif
