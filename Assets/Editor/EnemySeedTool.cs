#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class EnemySeedTool
    {
        private struct EnemyData
        {
            public string id;
            public string displayName;
            public float hp;
            public float speed;
            public int damage;
            public int reward;
            public float scale;
            public Color bodyColor;
            public string walkAnim;
            public string assetKey;
            public bool isFlyer;
            public float flyHeight;
            public bool ignorePath;
            public bool isStealth;
            public int stealthCycleMs;
            public float stealthOpacity;
            public float shieldHP;
            public bool isBoss;
            public bool isMidBoss;
            public bool isApocalypseBoss;
            public bool isBrigand;
            public bool isCorsair;
            public bool isFiery;
            public bool immuneToFlyerBonus;
            public string bossName;
            public Color bossAuraColor;
            public int chargeMs;
            public int chargeCooldownMs;
            public float chargeMul;
            public bool summonsMinions;
            public int summonCooldownMs;
            public string summonType;
            public int aoeBlastMs;
            public float aoeBlastRadius;
            public int aoeBlastDamage;
            public string shaderOverlay;
            public bool canTeleport;
            public float teleportCooldown;
            public int projectileRainCount;
            public bool isBurstSummoner;
            public int burstCount;
            public float burstAngleStep;
            public bool hasTentacleSlam;
            public int tentacleCount;
            public int tentacleDamage;
            public float tentacleRadius;
        }

        private static Color Hex(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        private static List<EnemyData> BuildAllEnemies() => new List<EnemyData>
        {
            new EnemyData { id = "basic",          displayName = "Basic",           hp = 3,    speed = 1.2f,  damage = 5,  reward = 2,   scale = 0.55f, bodyColor = Hex(0xc63a10), walkAnim = "Walk",        assetKey = "zombie" },
            new EnemyData { id = "skeleton_minion", displayName = "Skeleton Minion", hp = 5,    speed = 1.0f,  damage = 6,  reward = 3,   scale = 0.55f, bodyColor = Hex(0xc0c0c0), walkAnim = "Walking_A",   assetKey = "mob_skeleton" },
            new EnemyData { id = "runner",          displayName = "Runner",          hp = 1,    speed = 2.4f,  damage = 4,  reward = 2,   scale = 0.45f, bodyColor = Hex(0x00e8ff), walkAnim = "Run",         assetKey = "goblin" },
            new EnemyData { id = "brute",           displayName = "Brute",           hp = 12,   speed = 0.8f,  damage = 12, reward = 8,   scale = 0.7f,  bodyColor = Hex(0x8a4a22), walkAnim = "Walk",        assetKey = "soldier" },
            new EnemyData { id = "shielded",        displayName = "Shielded",        hp = 2,    speed = 1.0f,  damage = 8,  reward = 5,   scale = 0.6f,  bodyColor = Hex(0xd4af37), walkAnim = "Walk",        assetKey = "knightgolden", shieldHP = 4 },
            new EnemyData { id = "midboss",         displayName = "Midboss",         hp = 30,   speed = 0.7f,  damage = 20, reward = 15,  scale = 1.0f,  bodyColor = Hex(0x6a3aa0), walkAnim = "Walk",        assetKey = "wizard",    isMidBoss = true },
            new EnemyData { id = "boss",            displayName = "Boss",            hp = 60,   speed = 0.6f,  damage = 30, reward = 50,  scale = 1.55f, bodyColor = Hex(0x2c1810), walkAnim = "Walk",        assetKey = "pirate",    isBoss = true, bossAuraColor = Hex(0xff3030) },
            new EnemyData { id = "brigand_boss",    displayName = "Brigand Boss",    hp = 80,   speed = 0.6f,  damage = 40, reward = 100, scale = 1.75f, bodyColor = Hex(0x8a1a0a), walkAnim = "Walk",        assetKey = "pirate",    isBoss = true, isBrigand = true, chargeMs = 1500, chargeCooldownMs = 6000, chargeMul = 4f, bossName = "Brigand de la Plaine", bossAuraColor = Hex(0xff8030) },
            new EnemyData { id = "assassin",        displayName = "Assassin",        hp = 2,    speed = 2.0f,  damage = 6,  reward = 4,   scale = 0.50f, bodyColor = Hex(0xa030ff), walkAnim = "Run",         assetKey = "goblin",    isStealth = true, stealthCycleMs = 2200, stealthOpacity = 0.25f },
            new EnemyData { id = "warlord_boss",    displayName = "Warlord Boss",    hp = 100,  speed = 0.55f, damage = 35, reward = 120, scale = 1.95f, bodyColor = Hex(0xff7a00), walkAnim = "Walk",        assetKey = "wizard",    isBoss = true, summonsMinions = true, summonCooldownMs = 5000, summonType = "runner", bossName = "Sorcier de la Forêt", bossAuraColor = Hex(0x9030ff) },
            new EnemyData { id = "flyer",           displayName = "Flyer",           hp = 2,    speed = 1.6f,  damage = 5,  reward = 4,   scale = 0.45f, bodyColor = Hex(0x5fbcff), walkAnim = "Walk",        assetKey = "wizard",    isFlyer = true, ignorePath = true, flyHeight = 2.5f },
            new EnemyData { id = "corsair_boss",    displayName = "Corsair Boss",    hp = 90,   speed = 0.9f,  damage = 35, reward = 110, scale = 1.95f, bodyColor = Hex(0x5fbcff), walkAnim = "Walk",        assetKey = "pirate",    isBoss = true, isCorsair = true, aoeBlastMs = 8000, aoeBlastRadius = 4.5f, aoeBlastDamage = 30, bossName = "Capitaine Corsaire", bossAuraColor = Hex(0x5fbcff) },
            new EnemyData { id = "imp",             displayName = "Imp",             hp = 4,    speed = 1.5f,  damage = 8,  reward = 5,   scale = 0.55f, bodyColor = Hex(0xff3a10), walkAnim = "Run",         assetKey = "goblin",    isFiery = true },
            new EnemyData { id = "dragon_boss",     displayName = "Dragon Boss",     hp = 130,  speed = 0.5f,  damage = 50, reward = 200, scale = 1.5f,  bodyColor = Hex(0xff3a10), walkAnim = "Fast_Flying", assetKey = "boss_volcan_dragon_v2", isBoss = true, isFlyer = true, flyHeight = 2.0f, aoeBlastMs = 6000, aoeBlastRadius = 5.0f, aoeBlastDamage = 35, summonsMinions = true, summonCooldownMs = 4500, summonType = "imp", bossName = "Dragon de Lave", immuneToFlyerBonus = true, bossAuraColor = Hex(0xff5520) },
            new EnemyData { id = "apocalypse_boss", displayName = "Apocalypse Boss", hp = 600,  speed = 0.55f, damage = 60, reward = 500, scale = 2.0f,  bodyColor = Hex(0x6a0808), walkAnim = "Walking_A",   assetKey = "boss_apocalypse", isBoss = true, isApocalypseBoss = true, bossName = "L'Apocalypse", bossAuraColor = Hex(0xff2020) },
            new EnemyData { id = "cosmic_boss",     displayName = "Cosmic Boss",     hp = 700,  speed = 0.5f,  damage = 65, reward = 550, scale = 1.7f,  bodyColor = Hex(0x8855cc), walkAnim = "Fast_Flying", assetKey = "boss_espace_ghost", isBoss = true, isApocalypseBoss = true, summonsMinions = true, summonCooldownMs = 5000, summonType = "flyer", bossName = "Entité Galactique", bossAuraColor = Hex(0x8855ff) },
            new EnemyData { id = "kraken_boss",     displayName = "Kraken Boss",     hp = 800,  speed = 0.45f, damage = 70, reward = 600, scale = 2.0f,  bodyColor = Hex(0x008888), walkAnim = "Idle",        assetKey = "boss_submarin_kraken", isBoss = true, isApocalypseBoss = true, summonsMinions = true, summonCooldownMs = 4000, summonType = "shielded", bossName = "Le Kraken", bossAuraColor = Hex(0x00cccc), shaderOverlay = "jellyfish", hasTentacleSlam = true, tentacleCount = 4, tentacleDamage = 15, tentacleRadius = 3.5f },
            new EnemyData { id = "wizard_king",     displayName = "Wizard King",     hp = 900,  speed = 0.42f, damage = 75, reward = 650, scale = 1.7f,  bodyColor = Hex(0xffd040), walkAnim = "Walking_A",   assetKey = "boss_medieval_sorcier_roi", isBoss = true, isApocalypseBoss = true, summonsMinions = true, summonCooldownMs = 4500, summonType = "assassin", bossName = "Le Sorcier-Roi", bossAuraColor = Hex(0xffd040), canTeleport = true, teleportCooldown = 8f, projectileRainCount = 5 },
            new EnemyData { id = "ai_hub",          displayName = "AI Hub",          hp = 1000, speed = 0.38f, damage = 80, reward = 700, scale = 1.7f,  bodyColor = Hex(0xff40ff), walkAnim = "Walking_A",   assetKey = "boss_cyberpunk_hub_ia", isBoss = true, isApocalypseBoss = true, summonsMinions = true, summonCooldownMs = 3500, summonType = "flyer", bossName = "Hub IA", bossAuraColor = Hex(0xff40ff), shaderOverlay = "hologram", isBurstSummoner = true, burstCount = 5, burstAngleStep = 72f },
            new EnemyData { id = "desert_runner",   displayName = "Desert Runner",   hp = 1,    speed = 2.4f,  damage = 4,  reward = 2,   scale = 0.55f, bodyColor = Hex(0xcca055), walkAnim = "Run",         assetKey = "mob_cactoro" },
            new EnemyData { id = "forest_brute",    displayName = "Forest Brute",    hp = 12,   speed = 0.8f,  damage = 18, reward = 12,  scale = 0.7f,  bodyColor = Hex(0x6a8a40), walkAnim = "Idle",        assetKey = "mob_orc" },
            new EnemyData { id = "submarin_runner", displayName = "Submarin Runner", hp = 1,    speed = 2.2f,  damage = 3,  reward = 2,   scale = 0.5f,  bodyColor = Hex(0x4a8a4a), walkAnim = "Jump",        assetKey = "mob_frog" },
            new EnemyData { id = "forest_bee",      displayName = "Forest Bee",      hp = 2,    speed = 1.6f,  damage = 5,  reward = 3,   scale = 0.45f, bodyColor = Hex(0xffcc40), walkAnim = "Fast_Flying", assetKey = "mob_armabee", isFlyer = true },
            new EnemyData { id = "plaine_pigeon",   displayName = "Plaine Pigeon",   hp = 2,    speed = 1.8f,  damage = 4,  reward = 3,   scale = 0.4f,  bodyColor = Hex(0xa0a0a0), walkAnim = "Fast_Flying", assetKey = "mob_pigeon", isFlyer = true },
            new EnemyData { id = "cyber_basic",     displayName = "Cyber Basic",     hp = 4,    speed = 1.0f,  damage = 8,  reward = 4,   scale = 0.55f, bodyColor = Hex(0xff40ff), walkAnim = "Walk",        assetKey = "mob_cyberpunk_character" },
            new EnemyData { id = "cyber_runner",    displayName = "Cyber Runner",    hp = 2,    speed = 2.2f,  damage = 5,  reward = 3,   scale = 0.5f,  bodyColor = Hex(0x40ffff), walkAnim = "Run",         assetKey = "mob_cyberpunk_2legs" },
            new EnemyData { id = "cyber_flyer",     displayName = "Cyber Flyer",     hp = 3,    speed = 1.6f,  damage = 6,  reward = 4,   scale = 0.5f,  bodyColor = Hex(0xff80ff), walkAnim = "Flying",      assetKey = "mob_cyberpunk_flying", isFlyer = true },
            new EnemyData { id = "cyber_brute",     displayName = "Cyber Brute",     hp = 18,   speed = 0.7f,  damage = 22, reward = 14,  scale = 0.8f,  bodyColor = Hex(0xa040ff), walkAnim = "Walk",        assetKey = "mob_cyberpunk_large" },
        };

        [MenuItem("Tools/CrowdDefense/Seed Enemy Types")]
        public static void SeedEnemyTypes()
        {
            const string folder = "Assets/ScriptableObjects/Enemies";
            int created = 0, skipped = 0;

            foreach (var data in BuildAllEnemies())
            {
                string assetName = IdToAssetName(data.id);
                string path = $"{folder}/{assetName}.asset";

                // Idempotent: skip if already exists
                if (AssetDatabase.LoadAssetAtPath<EnemyType>(path) != null)
                {
                    skipped++;
                    continue;
                }

                var so = ScriptableObject.CreateInstance<EnemyType>();
                ApplyData(new SerializedObject(so), data);
                AssetDatabase.CreateAsset(so, path);
                created++;
#if UNITY_EDITOR
                Debug.Log($"[EnemySeedTool] Created {path}");
#endif
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnemySeedTool] Done. created={created} skipped={skipped} (28 total).");
        }

        public static void Generate() => SeedEnemyTypes();

        private static void ApplyData(SerializedObject so, EnemyData d)
        {
            SetString(so, "id",             d.id);
            SetString(so, "displayName",    d.displayName);
            SetFloat (so, "hp",             d.hp);
            SetFloat (so, "speed",          d.speed);
            SetInt   (so, "damage",         d.damage);
            SetInt   (so, "reward",         d.reward);
            SetFloat (so, "scale",          d.scale);
            SetColor (so, "bodyColor",      d.bodyColor);
            SetString(so, "walkAnim",       d.walkAnim);
            SetString(so, "assetKey",       d.assetKey);
            SetBool  (so, "isFlyer",        d.isFlyer);
            SetFloat (so, "flyHeight",      d.flyHeight);
            SetBool  (so, "ignorePath",     d.ignorePath);
            SetBool  (so, "isStealth",      d.isStealth);
            SetInt   (so, "stealthCycleMs", d.stealthCycleMs);
            SetFloat (so, "stealthOpacity", d.stealthOpacity == 0f ? 0.25f : d.stealthOpacity);
            SetFloat (so, "shieldHP",       d.shieldHP);
            SetBool  (so, "isBoss",         d.isBoss);
            SetBool  (so, "isMidBoss",      d.isMidBoss);
            SetBool  (so, "isApocalypseBoss", d.isApocalypseBoss);
            SetBool  (so, "isBrigand",      d.isBrigand);
            SetBool  (so, "isCorsair",      d.isCorsair);
            SetBool  (so, "isFiery",        d.isFiery);
            SetBool  (so, "immuneToFlyerBonus", d.immuneToFlyerBonus);
            SetString(so, "bossName",       d.bossName);
            SetColor (so, "bossAuraColor",  d.bossAuraColor);
            SetInt   (so, "chargeMs",       d.chargeMs);
            SetInt   (so, "chargeCooldownMs", d.chargeCooldownMs);
            SetFloat (so, "chargeMul",      d.chargeMul == 0f ? 1f : d.chargeMul);
            SetBool  (so, "summonsMinions", d.summonsMinions);
            SetInt   (so, "summonCooldownMs", d.summonCooldownMs);
            SetInt   (so, "aoeBlastMs",     d.aoeBlastMs);
            SetFloat (so, "aoeBlastRadius", d.aoeBlastRadius);
            SetInt   (so, "aoeBlastDamage", d.aoeBlastDamage);
            SetString(so, "shaderOverlay",  d.shaderOverlay);
            SetBool  (so, "canTeleport",    d.canTeleport);
            SetFloat (so, "teleportCooldown", d.teleportCooldown);
            SetInt   (so, "projectileRainCount", d.projectileRainCount);
            SetBool  (so, "isBurstSummoner", d.isBurstSummoner);
            SetInt   (so, "burstCount",     d.burstCount);
            SetFloat (so, "burstAngleStep", d.burstAngleStep);
            SetBool  (so, "hasTentacleSlam", d.hasTentacleSlam);
            SetInt   (so, "tentacleCount",  d.tentacleCount);
            SetInt   (so, "tentacleDamage", d.tentacleDamage);
            SetFloat (so, "tentacleRadius", d.tentacleRadius);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string IdToAssetName(string id)
        {
            // basic → Basic, skeleton_minion → SkeletonMinion, ai_hub → AiHub
            var parts = id.Split('_');
            var sb = new System.Text.StringBuilder();
            foreach (var p in parts)
                sb.Append(char.ToUpper(p[0]) + p.Substring(1));
            return sb.ToString();
        }

        private static void SetString(SerializedObject so, string prop, string val) =>
            so.FindProperty(prop)!.stringValue = val;
        private static void SetInt(SerializedObject so, string prop, int val) =>
            so.FindProperty(prop)!.intValue = val;
        private static void SetFloat(SerializedObject so, string prop, float val) =>
            so.FindProperty(prop)!.floatValue = val;
        private static void SetBool(SerializedObject so, string prop, bool val) =>
            so.FindProperty(prop)!.boolValue = val;
        private static void SetColor(SerializedObject so, string prop, Color val) =>
            so.FindProperty(prop)!.colorValue = val;
    }
}
#endif
