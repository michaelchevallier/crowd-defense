#nullable enable
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Idempotent: creates missing BossDef SO assets and fills display name / aura color.
    // Does not overwrite existing assets' inspector-tuned fields.
    // Menu: Tools/CrowdDefense/Build Boss Def Assets
    public static class BuildBossDefAssets
    {
        private const string k_Dir = "Assets/ScriptableObjects/Bosses";

        private readonly struct BossEntry
        {
            public readonly string FileName;
            public readonly string EnemyTypeId;
            public readonly string DisplayNameFr;
            public readonly int    World;
            public readonly Color  AuraColor;
            public readonly string CutsceneSub;
            public readonly float  EnragedAt;
            public readonly float  DesperateAt;
            public readonly float  EnragedSpeedMul;
            public readonly float  EnragedSummonCdMul;

            public BossEntry(string file, string etId, string name, int world, Color aura,
                string sub, float enraged = 0.5f, float desperate = 0.2f,
                float speedMul = 1.4f, float cdMul = 0.6f)
            {
                FileName = file; EnemyTypeId = etId; DisplayNameFr = name; World = world;
                AuraColor = aura; CutsceneSub = sub; EnragedAt = enraged; DesperateAt = desperate;
                EnragedSpeedMul = speedMul; EnragedSummonCdMul = cdMul;
            }
        }

        private static readonly BossEntry[] k_Entries =
        {
            new("Boss_W1_Brigand",    "brigand_boss",    "Brigand de la Plaine",  1, new Color(0.8f, 0.2f, 0.1f), "Le chef des brigands arrive !"),
            new("Boss_W2_Warlord",    "warlord_boss",    "Sorcier de la Foret",   2, new Color(0.3f, 0.7f, 0.2f), "Le Sorcier de la Foret surgit !"),
            new("Boss_W3_Corsair",    "corsair_boss",    "Capitaine Corsaire",    3, new Color(0.1f, 0.4f, 0.9f), "Le Capitaine Corsaire debarque !"),
            new("Boss_W4_Dragon",     "dragon_boss",     "Dragon de Lave",        4, new Color(1.0f, 0.3f, 0.0f), "Le Dragon de Lave s eveille !"),
            new("Boss_W5_Corsair",    "corsair_boss",    "Capitaine Corsaire",    5, new Color(0.1f, 0.4f, 0.9f), "Retour du Capitaine Corsaire !"),
            new("Boss_W6_Apocalypse", "apocalypse_boss", "L Apocalypse",          6, new Color(0.5f, 0.0f, 0.5f), "L Apocalypse est arrive !", 0.75f, 0.50f, 2.0f, 0.4f),
            new("Boss_W7_Cosmic",     "cosmic_boss",     "Entite Galactique",     7, new Color(0.4f, 0.0f, 0.8f), "L Entite Galactique materialise !"),
            new("Boss_W8_Kraken",     "kraken_boss",     "Le Kraken",             8, new Color(0.0f, 0.5f, 0.7f), "Le Kraken emerge des profondeurs !"),
            new("Boss_W9_WizardKing", "wizard_king",     "Le Sorcier-Roi",        9, new Color(0.8f, 0.6f, 0.1f), "Le Sorcier-Roi prend sa revanche !"),
            new("Boss_W10_AiHub",     "ai_hub",          "Hub IA",               10, new Color(0.0f, 0.8f, 0.8f), "Le Hub IA active ses protocoles !", 0.6f, 0.3f, 1.6f, 0.5f),
        };

        [MenuItem("Tools/CrowdDefense/Build Boss Def Assets")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(k_Dir))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Bosses");

            int created = 0;
            int skipped = 0;

            foreach (var e in k_Entries)
            {
                string path = $"{k_Dir}/{e.FileName}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<BossDef>(path);
                if (existing != null) { skipped++; continue; }

                var def = ScriptableObject.CreateInstance<BossDef>();
                def.name = e.FileName;

                // Locate EnemyType by id via linear scan (editor-only, called once)
                var etGuids = AssetDatabase.FindAssets("t:EnemyType", new[] { "Assets/ScriptableObjects/Enemies" });
                EnemyType? matched = null;
                foreach (var g in etGuids)
                {
                    var et = AssetDatabase.LoadAssetAtPath<EnemyType>(AssetDatabase.GUIDToAssetPath(g));
                    if (et != null && et.Id == e.EnemyTypeId) { matched = et; break; }
                }

                var so = new SerializedObject(def);
                if (matched != null)
                    so.FindProperty("enemyType")!.objectReferenceValue = matched;
                so.FindProperty("displayNameFr")!.stringValue    = e.DisplayNameFr;
                so.FindProperty("world")!.intValue                = e.World;
                var aura = so.FindProperty("auraColor")!;
                aura.colorValue                                   = e.AuraColor;
                so.FindProperty("cutsceneSubtitle")!.stringValue  = e.CutsceneSub;
                so.FindProperty("enragedAt")!.floatValue          = e.EnragedAt;
                so.FindProperty("desperateAt")!.floatValue        = e.DesperateAt;
                so.FindProperty("enragedSpeedMul")!.floatValue    = e.EnragedSpeedMul;
                so.FindProperty("enragedSummonCdMul")!.floatValue = e.EnragedSummonCdMul;
                so.ApplyModifiedProperties();

                AssetDatabase.CreateAsset(def, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildBossDefAssets] done — created={created} skipped(existing)={skipped}");
        }
    }
}
#endif
