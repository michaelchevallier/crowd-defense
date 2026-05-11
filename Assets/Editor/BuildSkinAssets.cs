#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Seede 5 SkinDef assets: 1 default Hero + 4 optional Hero skins.
    // Idempotent: updates fields if asset already exists.
    // Called from SetupMainScene.EnsureRegistries().
    public static class BuildSkinAssets
    {
        private const string k_SkinDir = "Assets/ScriptableObjects/Skins";

        private readonly struct SkinData
        {
            public readonly string Id;
            public readonly string DisplayNameKey;
            public readonly string DescriptionKey;
            public readonly SkinTargetType TargetType;
            public readonly string TargetId;
            public readonly SkinUnlockType UnlockType;
            public readonly float DamageMul;
            public readonly float RangeMul;
            public readonly float FireRateMul;

            public SkinData(string id, string dispKey, string descKey, SkinTargetType targetType, string targetId,
                SkinUnlockType unlockType = SkinUnlockType.Default, float dmg = 1f, float rng = 1f, float fr = 1f)
            {
                Id = id;
                DisplayNameKey = dispKey;
                DescriptionKey = descKey;
                TargetType = targetType;
                TargetId = targetId;
                UnlockType = unlockType;
                DamageMul = dmg;
                RangeMul = rng;
                FireRateMul = fr;
            }
        }

        // 5 base skins: 1 default + 4 unlockable Hero skins
        private static readonly SkinData[] k_Defs = new SkinData[]
        {
            new("knight_default", "skin.knight.default.name", "skin.knight.default.desc",
                SkinTargetType.Hero, "Knight", SkinUnlockType.Default),

            new("knight_ranger", "skin.knight.ranger.name", "skin.knight.ranger.desc",
                SkinTargetType.Hero, "Knight", SkinUnlockType.Purchase, 1.1f, 1.2f, 1f),

            new("knight_warrior", "skin.knight.warrior.name", "skin.knight.warrior.desc",
                SkinTargetType.Hero, "Knight", SkinUnlockType.Purchase, 1.3f, 0.9f, 1f),

            new("knight_mage", "skin.knight.mage.name", "skin.knight.mage.desc",
                SkinTargetType.Hero, "Knight", SkinUnlockType.Purchase, 1f, 1.1f, 1.2f),

            new("knight_paladin", "skin.knight.paladin.name", "skin.knight.paladin.desc",
                SkinTargetType.Hero, "Knight", SkinUnlockType.Purchase, 0.95f, 1f, 0.8f),
        };

        public static void Generate()
        {
            Directory.CreateDirectory(k_SkinDir);

            var defs = new SkinDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = SaveDef(k_Defs[i]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#if UNITY_EDITOR
            Debug.Log($"[BuildSkinAssets] done — {defs.Length} skins seeded.");
#endif
        }

        private static SkinDef SaveDef(in SkinData d)
        {
            string path = $"{k_SkinDir}/{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SkinDef>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<SkinDef>();
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            so.FindProperty("id").stringValue = d.Id;
            so.FindProperty("displayNameKey").stringValue = d.DisplayNameKey;
            so.FindProperty("descriptionKey").stringValue = d.DescriptionKey;
            so.FindProperty("targetType").enumValueIndex = (int)d.TargetType;
            so.FindProperty("targetId").stringValue = d.TargetId;
            so.FindProperty("unlockType").enumValueIndex = (int)d.UnlockType;
            so.FindProperty("damageMul").floatValue = d.DamageMul;
            so.FindProperty("rangeMul").floatValue = d.RangeMul;
            so.FindProperty("fireRateMul").floatValue = d.FireRateMul;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
#endif
