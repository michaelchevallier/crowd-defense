#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Génère 8 ModifierDef assets depuis V5 modifiers.js (MODIFIERS).
    // Idempotent : met à jour si l'asset existe déjà.
    // Menu : Tools > CrowdDefense > Build Modifier Assets
    public static class BuildModifierAssets
    {
        private const string k_Dir          = "Assets/ScriptableObjects/Modifiers";
        private const string k_RegistryPath = "Assets/Resources/ModifierRegistry.asset";

        private readonly struct ModData
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly ModifierType Type;
            public readonly string Desc;
            public readonly string ApplyAction;

            public ModData(string id, string displayName, ModifierType type, string desc, string applyAction)
            { Id = id; DisplayName = displayName; Type = type; Desc = desc; ApplyAction = applyAction; }
        }

        // 8 modifiers from V5 modifiers.js MODIFIERS
        private static readonly ModData[] k_Defs = new ModData[]
        {
            new("dragon_breath",  "Souffle du Dragon",     ModifierType.Curse,    "-30% portee toutes tours",    "towerRangeMul*0.7"),
            new("ancestral_fog",  "Brouillard Ancestral",  ModifierType.Curse,    "-50% portee du heros",        "heroRangeMul*0.5"),
            new("magnetic_storm", "Tempete Magnetique",    ModifierType.Curse,    "Projectiles devies +/-15 deg","projectileDeviation=15"),
            new("rising_lava",    "Lave Ascendante",       ModifierType.Curse,    "1 unite chemin perd 1 PV/s",  "lavaPathDmgPerSec=1"),
            new("gold_blessing",  "Benediction de l'Or",   ModifierType.Blessing, "+50% or par kill",            "coinMul*1.5"),
            new("iron_castle",    "Benediction du Chateau",ModifierType.Blessing, "+30% PV chateau",             "castleHPMul*1.3"),
            new("swift_arrows",   "Fleches Veloces",       ModifierType.Blessing, "+25% cadence toutes tours",   "towerFireRateMul*1.25"),
            new("reinforcements", "Renforts du Royaume",   ModifierType.Blessing, "+50% or de depart",           "startCoinsMul*1.5"),
        };

        [MenuItem("Tools/CrowdDefense/Build Modifier Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_Dir);

            var defs = new ModifierDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = BuildOne(k_Defs[i]);

            BuildRegistry(defs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildModifierAssets] done — {defs.Length} ModifierDef assets.");
        }

        private static ModifierDef BuildOne(in ModData d)
        {
            string path = $"{k_Dir}/Modifier_{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ModifierDef>(path);
            ModifierDef asset;
            bool isNew = existing == null;

            if (isNew)
                asset = ScriptableObject.CreateInstance<ModifierDef>();
            else
                asset = existing!;

            var so = new SerializedObject(asset);
            so.FindProperty("id")!.stringValue           = d.Id;
            so.FindProperty("displayName")!.stringValue  = d.DisplayName;
            so.FindProperty("modifierType")!.enumValueIndex = (int)d.Type;
            so.FindProperty("desc")!.stringValue         = d.Desc;
            so.FindProperty("applyAction")!.stringValue  = d.ApplyAction;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            if (isNew)
                AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        private static void BuildRegistry(ModifierDef[] defs)
        {
            var reg = AssetDatabase.LoadAssetAtPath<ModifierRegistry>(k_RegistryPath);
            if (reg == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                reg = ScriptableObject.CreateInstance<ModifierRegistry>();
                AssetDatabase.CreateAsset(reg, k_RegistryPath);
            }

            var so  = new SerializedObject(reg);
            var arr = so.FindProperty("modifiers")!;
            arr.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }
    }
}
#endif
