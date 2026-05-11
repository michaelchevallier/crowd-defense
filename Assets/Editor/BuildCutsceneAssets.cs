#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Génère 10 CutsceneDef assets (world1-world10) depuis V5 cutscenes.js.
    // Idempotent : met à jour si l'asset existe déjà.
    // Menu : Tools > CrowdDefense > Build Cutscene Assets
    public static class BuildCutsceneAssets
    {
        private const string k_Dir          = "Assets/ScriptableObjects/Cutscenes";
        private const string k_RegistryPath = "Assets/Resources/CutsceneRegistry.asset";

        private readonly struct CutData
        {
            public readonly string Id;
            public readonly string TitleKey;
            public readonly string[] Lines;
            public CutData(string id, string titleKey, string[] lines)
            { Id = id; TitleKey = titleKey; Lines = lines; }
        }

        // 10 worlds from V5 cutscenes.js
        private static readonly CutData[] k_Defs = new CutData[]
        {
            new("world1", "Milan Park - Les Portes s'ouvrent !", new[]
            {
                "Milan Park ouvre ses portes ce matin... et c'est le chaos.",
                "Des brigands envahissent l'entree du parc, bousculant les familles.",
                "La billetterie est menacee - si elle tombe, plus personne ne peut entrer.",
                "Defends la billetterie avant que la foule ne deborde. Bonne chance !",
            }),
            new("world2", "Le Sentier d'Aventure", new[]
            {
                "La billetterie tenue, tu t'enfonces dans le sentier d'aventure du parc.",
                "Un sorcier des bois s'est installe dans la cabane des contes et invoque ses sbires.",
                "Ses Assassins se fondent dans les sous-bois - tu ne les verras pas venir.",
                "Chasse le sorcier avant qu'il ne transforme toute l'attraction en cauchemar.",
            }),
            new("world3", "L'Attraction Sahara", new[]
            {
                "L'attraction Sahara du parc est prise d'assaut par des corsaires assoiffes.",
                "Ils surgissent des dunes en colonnes serrees, sabrant les oasis.",
                "Leurs creatures volantes attaquent depuis le ciel - une baliste sera essentielle.",
                "Garde les oasis ouverts au public. Le Capitaine Corsaire approche.",
            }),
            new("world4", "Le Volcan en Fureur - Foire en Lave !", new[]
            {
                "L'attraction phare du parc - le Volcan en Fureur - reveille un Dragon endormi.",
                "Des Imps de feu surgissent des crateres, transformant les allees en coulees de lave.",
                "Si le Dragon prend le controle, la Foire en Lave sera eternelle.",
                "C'est le combat pour sauver Milan Park. Ne laisse rien tomber.",
            }),
            new("world5", "La Foire en Lave - Portes Ouvertes !", new[]
            {
                "La Foire en Lave ouvre ses grilles aux visiteurs...",
                "...mais quelque chose tourne mal. Les visiteurs deviennent fous.",
                "Le Maitre de Ceremonie compte sur toi pour defendre la Grande Roue.",
            }),
            new("world6", "L'Apocalypse - Le Dernier Rempart", new[]
            {
                "Les cendres de l'Apocalypse recouvrent Milan Park. Le ciel est rouge sang.",
                "Une armee de survivants endoctrines deferlent depuis les ruines fumantes.",
                "L'Apocalypse en personne mene ses troupes - une entite que personne n'a jamais vaincue.",
                "C'est le dernier rempart. Si tu tombes, il ne reste plus rien.",
            }),
            new("world7", "L'Espace - Hors Atmosphere !", new[]
            {
                "Une anomalie gravitationnelle propulse Milan Park hors de l'atmosphere terrestre.",
                "Dans le vide spatial, des entites cosmiques surgissent de toutes parts.",
                "Les etoiles sont des ennemis. Les asteroides cachent des escouades entieres.",
                "Defends tes positions dans l'espace profond - l'Entite Galactique approche.",
            }),
            new("world8", "Les Abysses - Plongee Totale", new[]
            {
                "Milan Park s'enfonce dans les profondeurs oceaniques inconnues.",
                "Des creatures jamais vues remontent des abysses, attirees par la lumiere.",
                "Les courants sous-marins brouillent tout repere - les colonnes arrivent de partout.",
                "Au plus profond dort le Kraken. Il se reveille. Defends ou disparais.",
            }),
            new("world9", "Le Royaume Medieval - Le Sorcier-Roi", new[]
            {
                "Une fissure temporelle propulse Milan Park dans un royaume medieval en guerre.",
                "Les frontieres sont tombees. Chevaliers renegats, assassins des bois et brutes cuirassees deferlent.",
                "Sur son trone de pierre noire, le Sorcier-Roi orchestre tout - il convoque sans relache.",
                "Traverse le Royaume et affronte le Sorcier-Roi avant que son sort obscur ne scelle tout.",
            }),
            new("world10", "Neo-Tokyo - L'IA en Revolte", new[]
            {
                "Neo-Tokyo, 2147. L'intelligence artificielle centrale a pris le controle des rues.",
                "Des gangs cybernetiques, des drones et des soldats augmentes envahissent chaque couloir de neon.",
                "Le Hub IA pulse au coeur de la megapole - il coordonne tout, predit tout, contre tout.",
                "Coupe le flux d'energie du Hub ou sois absorbe dans la matrice pour toujours.",
            }),
        };

        [MenuItem("Tools/CrowdDefense/Build Cutscene Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_Dir);

            var defs = new CutsceneDef[k_Defs.Length];
            for (int i = 0; i < k_Defs.Length; i++)
                defs[i] = BuildOne(k_Defs[i]);

            BuildRegistry(defs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildCutsceneAssets] done — {defs.Length} CutsceneDef assets.");
        }

        private static CutsceneDef BuildOne(in CutData d)
        {
            string path = $"{k_Dir}/Cutscene_{d.Id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CutsceneDef>(path);
            CutsceneDef asset;
            bool isNew = existing == null;

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<CutsceneDef>();
            }
            else
            {
                asset = existing!;
            }

            var so = new SerializedObject(asset);
            so.FindProperty("id")!.stringValue       = d.Id;
            so.FindProperty("titleKey")!.stringValue  = d.TitleKey;

            var linesProp = so.FindProperty("lines")!;
            linesProp.arraySize = d.Lines.Length;
            for (int i = 0; i < d.Lines.Length; i++)
            {
                var elem = linesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("speaker")!.stringValue  = "Narrateur";
                elem.FindPropertyRelative("textKey")!.stringValue  = d.Lines[i];
                elem.FindPropertyRelative("side")!.enumValueIndex   = 0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            if (isNew)
                AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        private static void BuildRegistry(CutsceneDef[] defs)
        {
            var reg = AssetDatabase.LoadAssetAtPath<CutsceneRegistry>(k_RegistryPath);
            if (reg == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                reg = ScriptableObject.CreateInstance<CutsceneRegistry>();
                AssetDatabase.CreateAsset(reg, k_RegistryPath);
            }

            var so   = new SerializedObject(reg);
            var list = so.FindProperty("cutscenes")!;
            list.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }
    }
}
#endif
