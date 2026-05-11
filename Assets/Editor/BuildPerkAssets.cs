#nullable enable
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    // Génère les 17 PerkDef standard + 6 school PerkDef + 6 PerkSetBonusDef + 1 PerkRegistry.
    // Source canonique : V5 perks.js + schools.js.
    // Idempotent : met à jour les champs si l'asset existe déjà (ne duplique pas).
    // unlockLevel: 0 = always available, 5 = mid-game, 10 = late-game.
    // Menu : Tools > CrowdDefense > Generate Perk Assets
    public static class BuildPerkAssets
    {
        private const string k_PerkDir         = "Assets/ScriptableObjects/Perks";
        private const string k_PerkStandardDir = "Assets/ScriptableObjects/Perks/Standard";
        private const string k_PerkSchoolDir   = "Assets/ScriptableObjects/Perks/School";
        private const string k_PerkSetBonusDir = "Assets/ScriptableObjects/Perks/SetBonus";
        private const string k_ResourceDir     = "Assets/Resources";

        [MenuItem("Tools/CrowdDefense/Generate Perk Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(k_PerkDir);
            Directory.CreateDirectory(k_PerkStandardDir);
            Directory.CreateDirectory(k_PerkSchoolDir);
            Directory.CreateDirectory(k_PerkSetBonusDir);
            Directory.CreateDirectory(k_ResourceDir);

            var standard    = BuildStandardPerks();
            var school      = BuildSchoolPerks();
            var setBonuses  = BuildSetBonuses();

            BuildRegistry(standard, school, setBonuses);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildPerkAssets] done — {standard.Length} standard, {school.Length} school, {setBonuses.Length} set bonuses.");
        }

        // ── Standard perks (V5 PERKS array, 17 entries) ──────────────────────────

        private static PerkDef[] BuildStandardPerks()
        {
            var perks = new PerkDef[]
            {
                MakePerk("range",           "Oeil de l'aigle",    "Portée +35% / Cadence -10%",   "🎯", PerkCategory.Offensive, PerkTag.Foudre,
                    stackable: true,   range: 0.35f,  downFireRate: -0.10f),
                MakePerk("fire_rate",       "Doigts vifs",        "Cadence +40% / Dégâts -15%",   "⚡", PerkCategory.Offensive, PerkTag.Feu,
                    stackable: true,   maxStacks: 3,  fireRate: 0.40f, downDamage: -0.15f),
                MakePerk("damage",          "Frappe puissante",   "Dégâts +50% / Portée -15%",    "💥", PerkCategory.Offensive, PerkTag.Sang,
                    stackable: false,  damage: 0.50f, downRange: -0.15f),
                MakePerk("multi_shot",      "Tir double",         "Tire 2 projectiles / -35% dmg","🏹", PerkCategory.Offensive, PerkTag.Feu,
                    stackable: false,  multiShot: 1,  downDamage: -0.35f),
                MakePerk("crit",            "Coup critique",      "30% crit x2 / Portée -25%",    "🎲", PerkCategory.Offensive, PerkTag.Foudre,
                    stackable: false,  critChance: 0.30f, critMul: 2.0f, critStaggerMs: 300, downRange: -0.25f),
                MakePerk("pierce",          "Fleche percante",    "Pierce +1 ennemi / -15% dmg",  "📌", PerkCategory.Offensive, PerkTag.Foudre,
                    stackable: true,   pierceCount: 1, downDamage: -0.15f),
                MakePerk("coin_gain",       "Pillage royal",      "Or/kill +60% / -10% dmg",      "🪙", PerkCategory.Economy,   PerkTag.Or,
                    stackable: false,  coinGain: 0.60f, downDamage: -0.10f),
                MakePerk("lifesteal",       "Pacte du sang",      "+1 HP château/kill / -20% portée","❤️", PerkCategory.Economy, PerkTag.Sang,
                    stackable: true,   lifesteal: 1,  downRange: -0.20f),
                MakePerk("move_speed",      "Frappe en mouvement","Tirs en mouvement +1 pierce",  "👟", PerkCategory.Mobility,  PerkTag.Or,
                    stackable: true,   moveAttackPierceBonus: 1),
                MakePerk("wave_regen",      "Benediction royale", "+5 HP château/vague / -10% or","🛡️", PerkCategory.Economy,   PerkTag.Pierre,
                    stackable: false,  waveRegen: 5f, downCoinReward: -0.10f),
                MakePerk("fireball",        "Boule de feu",       "Tirs explosent AoE (r3u)",     "🔥", PerkCategory.Transform, PerkTag.Feu,
                    stackable: false,  transform: true, fireball: true, fireballRadius: 3f, fireballDmgMul: 0.8f, unlockLevel: 5),
                MakePerk("ricochet",        "Chaine",             "Tirs rebondissent x3 (-15%/b)","🔗", PerkCategory.Transform, PerkTag.Vide,
                    stackable: false,  transform: true, ricochet: true, ricochetBounces: 3, ricochetDecay: 0.85f, unlockLevel: 5),
                MakePerk("lightning",       "Foudre divine",      "Chaque tir frappe 2 cibles",   "⚡", PerkCategory.Transform, PerkTag.Vide,
                    stackable: false,  transform: true, lightning: true, lightningTargets: 2, lightningDmgMul: 0.7f, unlockLevel: 5),
                MakePerk("surveillant",     "Surveillant",        "Tours 8u : +30% cadence / -10% dmg","👁️", PerkCategory.Support, PerkTag.Pierre,
                    stackable: false,  towerFireRateAura: 1.3f, towerAuraRange: 8f, downDamage: -0.10f, unlockLevel: 5),
                MakePerk("architecte",      "Architecte",         "Tours -20% / Or/kill -10%",    "📐", PerkCategory.Support,   PerkTag.Or,
                    stackable: false,  towerCostMul: 0.80f, coinGain: -0.10f, unlockLevel: 5),
                MakePerk("marchand_mort",   "Marchand de mort",   "1ère tour par run gratuite",   "🏗️", PerkCategory.Support,   PerkTag.Pierre,
                    stackable: false,  firstTowerFree: true),
                MakePerk("pierce_explode",  "Carreau explosif",   "Explose sur dernier ennemi",   "💣", PerkCategory.Transform, PerkTag.Vide,
                    stackable: false,  transform: true, pierceExplode: true, pierceExplodeRadius: 2f, pierceExplodeDmgMul: 1.0f, unlockLevel: 10),
            };

            foreach (var p in perks) SaveAsset(p, $"{k_PerkStandardDir}/Perk_{p.id}.asset");
            return perks;
        }

        // ── School perks (V5 SCHOOL_PERKS, 6 entries) ───────────────────────────

        private static PerkDef[] BuildSchoolPerks()
        {
            var perks = new PerkDef[]
            {
                MakeSchoolPerk("combustion",    "Combustion",        "Kills laissent trail feu 2s","🔥", PerkCategory.Transform, PerkTag.Feu,    "feu",        combustion: true,     unlockLevel: 5),
                MakeSchoolPerk("pyromancie",    "Pyromancie",        "10% chance proj bonus/kill", "🔥", PerkCategory.Transform, PerkTag.Feu,    "feu",        pyromancie: true,     unlockLevel: 5),
                MakeSchoolPerk("glaciation",    "Glaciation",        "30% slow 2s sur cible",      "🧊", PerkCategory.Transform, PerkTag.Vide,   "givre",      glaciation: true,     unlockLevel: 5),
                MakeSchoolPerk("cristal_glace", "Cristal de Glace",  "Tours slow -15% ennemis",    "❄️", PerkCategory.Transform, PerkTag.Vide,   "givre",      cristalGlace: true,   unlockLevel: 5),
                MakeSchoolPerk("forteresse_perk","Forteresse Royale","+50% PV château max",        "🏰", PerkCategory.Transform, PerkTag.Pierre, "maconnerie", forteressePerk: true, unlockLevel: 5),
                MakeSchoolPerk("murs_pierre",   "Murs de Pierre",    "Tours stagger 0.5s/kill",    "🧱", PerkCategory.Transform, PerkTag.Pierre, "maconnerie", mursPierre: true,     unlockLevel: 5),
            };

            foreach (var p in perks) SaveAsset(p, $"{k_PerkSchoolDir}/SchoolPerk_{p.id}.asset");
            return perks;
        }

        // ── Set bonuses (V5 SET_BONUSES, 6 entries) ──────────────────────────────

        private static PerkSetBonusDef[] BuildSetBonuses()
        {
            var bonuses = new PerkSetBonusDef[]
            {
                MakeBonus(PerkTag.Foudre, "Tempete",    "+15% crit",          addCritChance: 0.15f),
                MakeBonus(PerkTag.Sang,   "Carmin",     "+1 PV château/kill", addLifesteal: 1),
                MakeBonus(PerkTag.Pierre, "Forteresse", "+20% PV château max",castleHPMaxMul: 1.2f),
                MakeBonus(PerkTag.Feu,    "Brasier",    "+20% cadence",       fireRateMul: 0.80f),
                MakeBonus(PerkTag.Vide,   "Neant",      "1 proj/4 = AoE 1u",  aoeOnNthProjectile: 4),
                MakeBonus(PerkTag.Or,     "Pactole",    "+30% or par kill",    coinGainMul: 1.3f),
            };

            foreach (var b in bonuses) SaveAsset(b, $"{k_PerkSetBonusDir}/SetBonus_{b.tag}.asset");
            return bonuses;
        }

        // ── Registry assembly ────────────────────────────────────────────────────

        private static void BuildRegistry(PerkDef[] standard, PerkDef[] school, PerkSetBonusDef[] setBonuses)
        {
            const string path = "Assets/Resources/PerkRegistry.asset";
            var reg = AssetDatabase.LoadAssetAtPath<PerkRegistry>(path);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<PerkRegistry>();
                AssetDatabase.CreateAsset(reg, path);
            }

            var so = new UnityEditor.SerializedObject(reg);
            SetArray(so, "standard",    standard);
            SetArray(so, "schoolPerks", school);
            SetArray(so, "setBonuses",  setBonuses);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static PerkDef MakePerk(
            string id, string name, string desc, string icon,
            PerkCategory cat, PerkTag tag,
            bool stackable = false, int maxStacks = 0,
            bool transform = false,
            float range = 0f, float fireRate = 0f, float damage = 0f,
            float moveSpeed = 0f, int moveAttackPierceBonus = 0,
            float coinGain = 0f, float critChance = 0f, float critMul = 0f,
            int critStaggerMs = 0, int multiShot = 0, int pierceCount = 0,
            int lifesteal = 0, float waveRegen = 0f,
            bool fireball = false, float fireballRadius = 2f, float fireballDmgMul = 0.8f,
            bool ricochet = false, int ricochetBounces = 3, float ricochetDecay = 0.85f,
            bool lightning = false, int lightningTargets = 2, float lightningDmgMul = 0.7f,
            bool pierceExplode = false, float pierceExplodeRadius = 2f, float pierceExplodeDmgMul = 1f,
            float towerCostMul = 1f, bool firstTowerFree = false,
            float towerFireRateAura = 1f, float towerAuraRange = 0f,
            float downRange = 0f, float downDamage = 0f,
            float downFireRate = 0f, float downCoinReward = 0f,
            int unlockLevel = 0)
        {
            var p = ScriptableObject.CreateInstance<PerkDef>();
            p.id = id; p.nameKey = name; p.descKey = desc; p.iconEmoji = icon;
            p.category = cat; p.tag = tag;
            p.stackable = stackable; p.maxStacks = maxStacks; p.transform = transform;
            p.range = range; p.fireRate = fireRate; p.damage = damage;
            p.moveSpeed = moveSpeed; p.moveAttackPierceBonus = moveAttackPierceBonus;
            p.coinGain = coinGain; p.critChance = critChance; p.critMul = critMul;
            p.critStaggerMs = critStaggerMs; p.multiShot = multiShot; p.pierceCount = pierceCount;
            p.lifesteal = lifesteal; p.waveRegen = waveRegen;
            p.fireball = fireball; p.fireballRadius = fireballRadius; p.fireballDmgMul = fireballDmgMul;
            p.ricochet = ricochet; p.ricochetBounces = ricochetBounces; p.ricochetDecay = ricochetDecay;
            p.lightning = lightning; p.lightningTargets = lightningTargets; p.lightningDmgMul = lightningDmgMul;
            p.pierceExplode = pierceExplode; p.pierceExplodeRadius = pierceExplodeRadius; p.pierceExplodeDmgMul = pierceExplodeDmgMul;
            p.towerCostMul = towerCostMul; p.firstTowerFree = firstTowerFree;
            p.towerFireRateAura = towerFireRateAura; p.towerAuraRange = towerAuraRange;
            p.downRange = downRange; p.downDamage = downDamage;
            p.downFireRate = downFireRate; p.downCoinReward = downCoinReward;
            p.unlockLevel = unlockLevel;
            return p;
        }

        private static PerkDef MakeSchoolPerk(
            string id, string name, string desc, string icon,
            PerkCategory cat, PerkTag tag, string school,
            bool combustion = false, bool pyromancie = false, bool glaciation = false,
            bool cristalGlace = false, bool forteressePerk = false, bool mursPierre = false,
            int unlockLevel = 0)
        {
            var p = ScriptableObject.CreateInstance<PerkDef>();
            p.id = id; p.nameKey = name; p.descKey = desc; p.iconEmoji = icon;
            p.category = cat; p.tag = tag; p.school = school;
            p.stackable = false; p.transform = true;
            p.combustion = combustion; p.pyromancie = pyromancie; p.glaciation = glaciation;
            p.cristalGlace = cristalGlace; p.forteressePerk = forteressePerk; p.mursPierre = mursPierre;
            p.unlockLevel = unlockLevel;
            return p;
        }

        private static PerkSetBonusDef MakeBonus(
            PerkTag tag, string name, string desc,
            float addCritChance = 0f, int addLifesteal = 0,
            float castleHPMaxMul = 1f, float fireRateMul = 1f,
            int aoeOnNthProjectile = 0, float coinGainMul = 1f)
        {
            var b = ScriptableObject.CreateInstance<PerkSetBonusDef>();
            b.tag = tag; b.nameKey = name; b.descKey = desc; b.threshold = 3;
            b.addCritChance = addCritChance; b.addLifesteal = addLifesteal;
            b.castleHPMaxMul = castleHPMaxMul; b.fireRateMul = fireRateMul;
            b.aoeOnNthProjectile = aoeOnNthProjectile; b.coinGainMul = coinGainMul;
            return b;
        }

        private static void SaveAsset(ScriptableObject asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath(path, asset.GetType());
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(asset);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        private static void SetArray(UnityEditor.SerializedObject so, string propName, Object[] items)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
#endif
