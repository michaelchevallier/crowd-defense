#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Génère 12 EventDef assets depuis V5 events.js (RUN_EVENTS).
    // Idempotent : met à jour si l'asset existe déjà.
    // Menu : Tools > CrowdDefense > Build Event Assets
    public static class BuildEventAssets
    {
        private const string k_Dir          = "Assets/ScriptableObjects/Events";
        private const string k_RegistryPath = "Assets/Resources/EventRegistry.asset";

        private readonly struct EvData
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Body;
            public readonly string Label0;
            public readonly string Action0;
            public readonly string Label1;
            public readonly string Action1;

            public EvData(string id, string title, string body,
                          string label0, string action0,
                          string label1, string action1)
            {
                Id = id; Title = title; Body = body;
                Label0 = label0; Action0 = action0;
                Label1 = label1; Action1 = action1;
            }
        }

        // 12 events from V5 events.js RUN_EVENTS
        private static readonly EvData[] k_Defs = new EvData[]
        {
            new("ash_merchant",
                "Le Marchand de Cendres",
                "Un vieil homme propose un pouvoir rare contre 50 PV chateau. Ses yeux brillent d'une lueur etrange.",
                "Accepter (-50 PV chateau, +1 perk legendaire)", "castleHP-50|pendingPerk=legendary",
                "Refuser (+20 or)",                              "coins+20"),

            new("wandering_knight",
                "Chevalier Errant",
                "Un chevalier blesse demande de l'or pour soigner ses plaies.",
                "Donner 30 or (+15 PV chateau)", "coins-30|castleHP+15",
                "Refuser (sans effet)",           "noop"),

            new("ancient_forge",
                "La Forge Ancienne",
                "Une forge oubliee brille dans la penombre. Tu peux y tremper ton equipement.",
                "Tremper (heros XP +200)", "heroXP+200",
                "Ignorer",                 "noop"),

            new("raven_omen",
                "Presage du Corbeau",
                "Un corbeau noir te suit depuis ton dernier combat. Presage de mort... ou de fortune ?",
                "Suivre l'oiseau (50% +60 or, 50% -20 PV)", "random50:coins+60:castleHP-20",
                "Le chasser (sans effet)",                   "noop"),

            new("frozen_pond",
                "L'Etang Gele",
                "Un etang etrangement gele reflete le ciel comme un miroir. Que vois-tu dans ton reflet ?",
                "Plonger ta main (-10 PV chateau, +1 perk Givre)", "castleHP-10|pendingPerk=givre",
                "Marcher dessus (+30 or)",                          "coins+30"),

            new("starving_traveler",
                "Voyageur Affame",
                "Un voyageur affame demande a partager ton repas. Il connait bien le chemin.",
                "Partager (+5 PV chateau max permanent)", "castleHPMax+5",
                "Refuser (-5 or par perte de moral)",    "coins-5"),

            new("buried_treasure",
                "Tresor Enfoui",
                "Une carte au tresor traine sous une pierre. Le X marque l'emplacement.",
                "Suivre la carte (+80 or, -1 perk au prochain levelup)", "coins+80|skipNextPerk",
                "Bruler la carte (sans effet)",                          "noop"),

            new("haunted_shrine",
                "Sanctuaire Hante",
                "Un sanctuaire abandonne emet une plainte sourde. Une silhouette spectrale t'observe.",
                "Prier (-30 PV chateau, +1 perk Sang)", "castleHP-30|pendingPerk=sang",
                "S'enfuir (+10 PV chateau regen)",      "castleHP+10"),

            new("rival_hero",
                "Heros Rival",
                "Un autre heros bloque le chemin. Il propose un duel pour passer.",
                "Combattre (-20 PV chateau, +50 or si gagne)", "castleHP-20|random60:coins+50",
                "Le contourner (perd 1 tour gratuite)",         "skipNextStarterTower"),

            new("lava_geyser",
                "Geyser de Lave",
                "Un geyser de lave jaillit du sol. La chaleur est intense mais une lueur doree brille au coeur.",
                "Plonger ta main (-25 PV chateau, +1 perk Feu)", "castleHP-25|pendingPerk=feu",
                "Recuperer l'or (+40 or)",                       "coins+40"),

            new("merchant_caravan",
                "Caravane Marchande",
                "Une caravane marchande passe sur la route. Le marchand sourit largement.",
                "Acheter une potion (-50 or, +40 PV chateau)",             "coins-50|castleHP+40",
                "Voler la caravane (+100 or, malediction prochain combat)", "coins+100|cursedNextCombat"),

            new("wisdom_oracle",
                "Oracle de Sagesse",
                "Une vieille femme aveugle dit voir ton destin. Elle propose de te guider.",
                "Ecouter (revele les types des nodes prochains)", "revealNextRowTypes",
                "Donner une offrande (+30 or, +1 perk bonus)",   "coins-30|bonusNextPerk"),
        };

        [MenuItem("Tools/CrowdDefense/Build Event Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_Dir);

            var defs = new EventDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = BuildOne(k_Defs[i]);

            BuildRegistry(defs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildEventAssets] done — {defs.Length} EventDef assets.");
        }

        private static EventDef BuildOne(in EvData d)
        {
            string path = $"{k_Dir}/Event_{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EventDef>(path);
            EventDef asset;
            bool isNew = existing == null;

            if (isNew)
                asset = ScriptableObject.CreateInstance<EventDef>();
            else
                asset = existing!;

            var so = new SerializedObject(asset);
            so.FindProperty("id")!.stringValue    = d.Id;
            so.FindProperty("title")!.stringValue = d.Title;
            so.FindProperty("body")!.stringValue  = d.Body;

            var choices = so.FindProperty("choices")!;
            choices.arraySize = 2;

            var c0 = choices.GetArrayElementAtIndex(0);
            c0.FindPropertyRelative("label")!.stringValue       = d.Label0;
            c0.FindPropertyRelative("applyAction")!.stringValue = d.Action0;

            var c1 = choices.GetArrayElementAtIndex(1);
            c1.FindPropertyRelative("label")!.stringValue       = d.Label1;
            c1.FindPropertyRelative("applyAction")!.stringValue = d.Action1;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            if (isNew)
                AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        private static void BuildRegistry(EventDef[] defs)
        {
            var reg = AssetDatabase.LoadAssetAtPath<EventRegistry>(k_RegistryPath);
            if (reg == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                reg = ScriptableObject.CreateInstance<EventRegistry>();
                AssetDatabase.CreateAsset(reg, k_RegistryPath);
            }

            var so   = new SerializedObject(reg);
            var arr  = so.FindProperty("events")!;
            arr.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }
    }
}
#endif
