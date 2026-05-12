#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Génère les 5 SchoolDef SO (Elementaire, Mecanique, Mystique, Bestiaire, Strategie).
    // Idempotent : met à jour si l'asset existe, ne duplique pas.
    // Place les assets dans Assets/Resources/Schools/ pour Resources.LoadAll<SchoolDef>.
    // Menu : Tools > CrowdDefense > Generate School Assets
    public static class BuildSchoolAssets
    {
        private const string k_Dir = "Assets/Resources/Schools";

        [MenuItem("Tools/CrowdDefense/Generate School Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_Dir);

            Build("elementaire", "Elementaire",
                "DPS et magie naturelle. Foudre, feu, glace — la puissance des elements.",
                new Color(0.20f, 0.75f, 0.30f), 0);

            Build("mecanique",  "Mecanique",
                "Ingenerie et fortification. Tours plus robustes, synergies d'aura.",
                new Color(0.65f, 0.50f, 0.20f), 100);

            Build("mystique",   "Mystique",
                "Vide et magie noire. Piercing, ricochet, explosions AoE.",
                new Color(0.55f, 0.20f, 0.85f), 200);

            Build("bestiaire",  "Bestiaire",
                "Sang et instinct. Lifesteal, vitesse, attaques en chaine.",
                new Color(0.85f, 0.20f, 0.20f), 300);

            Build("strategie",  "Strategie",
                "Economie et controle. Gains de pieces, auras de tour, opportunisme.",
                new Color(0.90f, 0.75f, 0.10f), 400);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildSchoolAssets] 5 schools generees dans Assets/Resources/Schools.");
        }

        private static void Build(string id, string displayName, string description, Color theme, int unlockCost)
        {
            string path = $"{k_Dir}/School_{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<SchoolDef>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<SchoolDef>();

            def!.id          = id;
            def.displayName  = displayName;
            def.description  = description;
            def.theme        = theme;
            def.unlockCost   = unlockCost;

            if (isNew) AssetDatabase.CreateAsset(def, path);
            else       EditorUtility.SetDirty(def);
        }
    }
}
#endif
