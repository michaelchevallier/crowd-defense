#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Seede 7 DoctrineDef assets from V5 doctrine schools.
    // Idempotent: updates fields if asset already exists.
    // Called from SetupMainScene.EnsureRegistries().
    public static class BuildDoctrineAssets
    {
        private const string k_DocDir = "Assets/ScriptableObjects/Doctrines";

        private readonly struct DocData
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string IconEmoji;
            public readonly int GemCost;
            public readonly string Description;
            public readonly DoctrineModifier[] Modifiers;

            public DocData(string id, string displayName, string iconEmoji, int gemCost, string desc, params DoctrineModifier[] mods)
            {
                Id = id;
                DisplayName = displayName;
                IconEmoji = iconEmoji;
                GemCost = gemCost;
                Description = desc;
                Modifiers = mods;
            }
        }

        // 7 doctrines from V5 doctrine schools
        private static readonly DocData[] k_Defs = new DocData[]
        {
            new("sentinel", "Sentinelle", "sentinel", 10,
                "Portée des tours +10%, PV château +15%.",
                new DoctrineModifier { key = "TowerDamageMul", value = 1.10f },
                new DoctrineModifier { key = "CastleHPBase", value = 1.15f }),

            new("berserker", "Berserker", "berserker", 10,
                "Dégâts tours +15%, PV château -15%.",
                new DoctrineModifier { key = "TowerDamageMul", value = 1.15f },
                new DoctrineModifier { key = "CastleHPBase", value = 0.85f }),

            new("merchant", "Marchand", "merchant", 10,
                "Intérêt banque +50%, bonus vague rapide +40%, aimant pièces +20%.",
                new DoctrineModifier { key = "BankInterestRate", value = 1.50f },
                new DoctrineModifier { key = "SkipBonusGold", value = 1.40f },
                new DoctrineModifier { key = "MagnetCoinMul", value = 1.20f }),

            new("paladin", "Paladin", "paladin", 10,
                "PV château +30%, revente tours +10%.",
                new DoctrineModifier { key = "CastleHPBase", value = 1.30f },
                new DoctrineModifier { key = "SellRefundRatio", value = 1.10f }),

            new("saboteur", "Saboteur", "saboteur", 10,
                "Bonus streak +40%, multiplicateur de horde -10%.",
                new DoctrineModifier { key = "StreakBonusPerWave", value = 1.40f },
                new DoctrineModifier { key = "SwarmMul", value = 0.90f }),

            new("alchemist", "Alchimiste", "alchemist", 10,
                "Intérêt banque +80%, portée aimant +30%.",
                new DoctrineModifier { key = "BankInterestRate", value = 1.80f },
                new DoctrineModifier { key = "MagnetRange", value = 1.30f }),

            new("trickster", "Imposteur", "trickster", 10,
                "Bonus vague rapide +80%, dégâts tours -10%.",
                new DoctrineModifier { key = "SkipBonusGold", value = 1.80f },
                new DoctrineModifier { key = "TowerDamageMul", value = 0.90f }),
        };

        public static void Generate()
        {
            Directory.CreateDirectory(k_DocDir);

            var defs = new DoctrineDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = SaveDef(k_Defs[i]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#if UNITY_EDITOR
            Debug.Log($"[BuildDoctrineAssets] done — {defs.Length} doctrines seeded.");
#endif
        }

        private static DoctrineDef SaveDef(in DocData d)
        {
            string path = $"{k_DocDir}/{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DoctrineDef>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<DoctrineDef>();
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            so.FindProperty("id").stringValue = d.Id;
            so.FindProperty("displayName").stringValue = d.DisplayName;
            so.FindProperty("iconEmoji").stringValue = d.IconEmoji;
            so.FindProperty("gemCost").intValue = d.GemCost;
            so.FindProperty("description").stringValue = d.Description;
            
            var modProp = so.FindProperty("modifiers");
            if (modProp != null)
            {
                modProp.arraySize = d.Modifiers.Length;
                for (int j = 0; j < d.Modifiers.Length; j++)
                {
                    var elem = modProp.GetArrayElementAtIndex(j);
                    elem.FindPropertyRelative("key").stringValue = d.Modifiers[j].key;
                    elem.FindPropertyRelative("value").floatValue = d.Modifiers[j].value;
                }
            }
            
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
#endif
