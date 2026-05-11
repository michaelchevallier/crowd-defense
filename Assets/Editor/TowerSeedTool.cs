#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class TowerSeedTool
    {
        private struct SynergyRaw
        {
            public SynergyType type;
            public string from;
            public string effectKey;
            public float effectValue;
            public float range;
        }

        private struct TowerData
        {
            public string id;
            public string displayName;
            public string icon;
            public int unlockWorld;
            public int cost;
            public float damage;
            public float range;
            public int fireRateMs;
            public float projectileSpeed;
            public float aoe;
            public int pierce;
            public TowerBehavior behavior;
            public int clusterCount;
            public int cooldownMs;
            public float slowMul;
            public int slowDurationMs;
            public float buffMul;
            public float coinMul;
            public float pullSlow;
            public bool parabolic;
            public bool flyerOnly;
            public float flyerDmgMul;
            public bool canHitFlyers;
            public bool hasArmorBreak;
            public float armorBreakDmgTakenMul;
            public int armorBreakDurMs;
            public Color bodyColor;
            public Color projectileColor;
            public float sizeMultiplier;
            public List<SynergyRaw> synergies;
        }

        private static Color Hex(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        private static List<TowerData> BuildAllTowers()
        {
            return new List<TowerData>
            {
                new TowerData {
                    id = "tank", displayName = "Tank", icon = "\U0001f6e1️",
                    unlockWorld = 1, cost = 50,
                    damage = 0.69f, range = 5f, fireRateMs = 220, projectileSpeed = 26f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.Attack,
                    bodyColor = Hex(0x8a4a22), projectileColor = Hex(0xff8855), sizeMultiplier = 0.85f,
                    synergies = new List<SynergyRaw>()
                },
                new TowerData {
                    id = "mage", displayName = "Mage", icon = "\U0001f52e",
                    unlockWorld = 1, cost = 70,
                    damage = 2.76f, range = 7f, fireRateMs = 1200, projectileSpeed = 14f,
                    aoe = 2.0f, pierce = 0,
                    behavior = TowerBehavior.Attack,
                    canHitFlyers = true,
                    bodyColor = Hex(0x6a3aa0), projectileColor = Hex(0xa050ff), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "skyguard", effectKey = "flyerDmgBonus", effectValue = 1.5f, range = 5f }
                    }
                },
                new TowerData {
                    id = "ballista", displayName = "Baliste", icon = "\U0001f3af",
                    unlockWorld = 2, cost = 100,
                    damage = 5.52f, range = 14f, fireRateMs = 1500, projectileSpeed = 30f,
                    aoe = 0f, pierce = 2,
                    behavior = TowerBehavior.Attack,
                    canHitFlyers = true,
                    bodyColor = Hex(0x4a4a4a), projectileColor = Hex(0xcccccc), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "portal", effectKey = "pierceMega", effectValue = 1f, range = 5f }
                    }
                },
                new TowerData {
                    id = "mine", displayName = "Mine", icon = "\U0001f4a3",
                    unlockWorld = 2, cost = 60,
                    damage = 5.75f, range = 1.8f, fireRateMs = 0, projectileSpeed = 0f,
                    aoe = 2.5f, pierce = 0,
                    behavior = TowerBehavior.Cluster, clusterCount = 3, cooldownMs = 12000,
                    bodyColor = Hex(0xaa2020), projectileColor = Hex(0xff3030), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "cannon", effectKey = "cascadeRadius", effectValue = 3.0f, range = 5f }
                    }
                },
                new TowerData {
                    id = "cannon", displayName = "Catapulte", icon = "\U0001fab8",
                    unlockWorld = 3, cost = 100,
                    damage = 6.9f, range = 9f, fireRateMs = 2000, projectileSpeed = 16f,
                    aoe = 3.0f, pierce = 0,
                    behavior = TowerBehavior.Attack, parabolic = true,
                    bodyColor = Hex(0x4a3a2a), projectileColor = Hex(0xff7530), sizeMultiplier = 1.2f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "frost", effectKey = "slowOnHit_mul", effectValue = 0.5f, range = 4f }
                    }
                },
                new TowerData {
                    id = "fan", displayName = "Soufflerie", icon = "\U0001f300",
                    unlockWorld = 3, cost = 70,
                    damage = 0.58f, range = 5f, fireRateMs = 0, projectileSpeed = 0f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.Slow, slowMul = 0.5f, slowDurationMs = 800,
                    bodyColor = Hex(0x88ccee), projectileColor = Hex(0xa8e0ff), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.ApplyToEnemy, from = "", effectKey = "slow_mul", effectValue = 0.5f, range = 5f }
                    }
                },
                new TowerData {
                    id = "frost", displayName = "Glacier", icon = "❄️",
                    unlockWorld = 3, cost = 60,
                    damage = 0f, range = 3f, fireRateMs = 0, projectileSpeed = 0f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.Slow, slowMul = 0.5f, slowDurationMs = 4000,
                    bodyColor = Hex(0x88ccee), projectileColor = Hex(0xc0e8ff), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.ApplyToEnemy, from = "", effectKey = "slow_mul", effectValue = 0.5f, range = 3f }
                    }
                },
                new TowerData {
                    id = "crossbow", displayName = "Baliste géante", icon = "\U0001f3ef",
                    unlockWorld = 4, cost = 140,
                    damage = 6.9f, range = 16f, fireRateMs = 1800, projectileSpeed = 32f,
                    aoe = 0f, pierce = 4,
                    behavior = TowerBehavior.Attack,
                    canHitFlyers = true,
                    bodyColor = Hex(0x6a4a2a), projectileColor = Hex(0xc0e8ff), sizeMultiplier = 0.95f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "mage", effectKey = "propagateAoE_radius", effectValue = 1f, range = 5f },
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "frost", effectKey = "appliesSlow_mul", effectValue = 0.7f, range = 4f },
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "fan", effectKey = "knockbackOnHit", effectValue = 0.001f, range = 4f }
                    }
                },
                new TowerData {
                    id = "portal", displayName = "Portail", icon = "\U0001f30c",
                    unlockWorld = 4, cost = 130,
                    damage = 0f, range = 5.5f, fireRateMs = 0, projectileSpeed = 0f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.BuffAura, buffMul = 1.5f,
                    bodyColor = Hex(0x6a3aa0), projectileColor = Hex(0xb088ff), sizeMultiplier = 1.04f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.Aura, from = "", effectKey = "pierceBonus", effectValue = 1f, range = 5.5f },
                        new SynergyRaw { type = SynergyType.Aura, from = "", effectKey = "dmgMul", effectValue = 1.5f, range = 5.5f }
                    }
                },
                new TowerData {
                    id = "magnet", displayName = "Aimant", icon = "\U0001f9f2",
                    unlockWorld = 4, cost = 100,
                    damage = 0f, range = 6.5f, fireRateMs = 0, projectileSpeed = 0f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.CoinPull, coinMul = 2.0f, pullSlow = 0.7f,
                    bodyColor = Hex(0xff4488), projectileColor = Hex(0xff66aa), sizeMultiplier = 0.85f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.Passive, from = "", effectKey = "coinMul", effectValue = 2.0f, range = 6.5f },
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "tank", effectKey = "pullToTank", effectValue = 1f, range = 4f }
                    }
                },
                new TowerData {
                    id = "skyguard", displayName = "Garde-Ciel", icon = "\U0001f680",
                    unlockWorld = 3, cost = 85,
                    damage = 5.52f, range = 12f, fireRateMs = 600, projectileSpeed = 28f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.Attack,
                    flyerOnly = true, flyerDmgMul = 1.5f, canHitFlyers = true,
                    bodyColor = Hex(0x4a5a6a), projectileColor = Hex(0x88ccff), sizeMultiplier = 1.0f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "frost", effectKey = "freezeOnHit_durMs", effectValue = 800f, range = 4f }
                    }
                },
                new TowerData {
                    id = "acid", displayName = "Acide", icon = "\U0001f9ea",
                    unlockWorld = 2, cost = 110,
                    damage = 0.5f, range = 7f, fireRateMs = 800, projectileSpeed = 22f,
                    aoe = 0f, pierce = 0,
                    behavior = TowerBehavior.Attack,
                    hasArmorBreak = true, armorBreakDmgTakenMul = 1.4f, armorBreakDurMs = 8000,
                    bodyColor = Hex(0x66cc22), projectileColor = Hex(0x88ff44), sizeMultiplier = 0.85f,
                    synergies = new List<SynergyRaw> {
                        new SynergyRaw { type = SynergyType.CrossEffect, from = "ballista", effectKey = "propagateDebuff", effectValue = 1f, range = 5f }
                    }
                },
            };
        }

        [MenuItem("Tools/CrowdDefense/Seed Tower Assets")]
        public static void SeedTowerAssets()
        {
            const string folder = "Assets/ScriptableObjects/Towers";

            foreach (var data in BuildAllTowers())
            {
                string path = $"{folder}/{Capitalize(data.id)}.asset";

                TowerType? existing = AssetDatabase.LoadAssetAtPath<TowerType>(path);
                TowerType so;
                bool isNew;
                if (existing != null)
                {
                    so = existing;
                    isNew = false;
                }
                else
                {
                    so = ScriptableObject.CreateInstance<TowerType>();
                    isNew = true;
                }

                SerializedObject serialized = new SerializedObject(so);
                SetString(serialized, "id", data.id);
                SetString(serialized, "displayName", data.displayName);
                SetString(serialized, "icon", data.icon);
                SetInt(serialized, "unlockWorld", data.unlockWorld);
                SetInt(serialized, "cost", data.cost);
                SetFloat(serialized, "damage", data.damage);
                SetFloat(serialized, "range", data.range);
                SetInt(serialized, "fireRateMs", data.fireRateMs);
                SetFloat(serialized, "projectileSpeed", data.projectileSpeed);
                SetFloat(serialized, "aoe", data.aoe);
                SetInt(serialized, "pierce", data.pierce);
                SetEnum(serialized, "behavior", (int)data.behavior);
                SetInt(serialized, "clusterCount", data.clusterCount);
                SetInt(serialized, "cooldownMs", data.cooldownMs);
                SetFloat(serialized, "slowMul", data.slowMul);
                SetInt(serialized, "slowDurationMs", data.slowDurationMs);
                SetFloat(serialized, "buffMul", data.buffMul);
                SetFloat(serialized, "coinMul", data.coinMul);
                SetFloat(serialized, "pullSlow", data.pullSlow);
                SetBool(serialized, "parabolic", data.parabolic);
                SetBool(serialized, "flyerOnly", data.flyerOnly);
                SetFloat(serialized, "flyerDmgMul", data.flyerDmgMul);
                SetBool(serialized, "canHitFlyers", data.canHitFlyers);
                SetBool(serialized, "hasArmorBreak", data.hasArmorBreak);
                SetArmorBreak(serialized, data.armorBreakDmgTakenMul, data.armorBreakDurMs);
                SetColor(serialized, "bodyColor", data.bodyColor);
                SetColor(serialized, "projectileColor", data.projectileColor);
                SetFloat(serialized, "sizeMultiplier", data.sizeMultiplier);
                SetSynergies(serialized, data.synergies);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (isNew)
                {
                    AssetDatabase.CreateAsset(so, path);
                    Debug.Log($"[TowerSeedTool] Created {path}");
                }
                else
                {
                    EditorUtility.SetDirty(so);
                    Debug.Log($"[TowerSeedTool] Updated {path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TowerSeedTool] Done. 12 tower assets seeded.");
        }

        [MenuItem("Tools/CrowdDefense/Re-import Archer Asset")]
        public static void ReimportArcherAsset()
        {
            const string path = "Assets/ScriptableObjects/Towers/Archer.asset";
            TowerType? so = AssetDatabase.LoadAssetAtPath<TowerType>(path);
            if (so == null)
            {
                Debug.LogError("[TowerSeedTool] Archer.asset not found at " + path);
                return;
            }

            SerializedObject serialized = new SerializedObject(so);

            // Preserve existing values, only fill new fields with defaults
            EnsureString(serialized, "icon", "\U0001f3f9");
            EnsureFloat(serialized, "flyerDmgMul", 1f);
            EnsureFloat(serialized, "slowMul", 1f);
            EnsureFloat(serialized, "buffMul", 1f);
            EnsureFloat(serialized, "coinMul", 1f);
            EnsureFloat(serialized, "pullSlow", 1f);

            // Archer synergy: crossbow frost multiShotBonus
            var synProp = serialized.FindProperty("synergies");
            if (synProp != null && synProp.arraySize == 0)
            {
                synProp.arraySize = 1;
                var elem = synProp.GetArrayElementAtIndex(0);
                elem.FindPropertyRelative("type").enumValueIndex = (int)SynergyType.CrossEffect;
                elem.FindPropertyRelative("from").stringValue = "frost";
                elem.FindPropertyRelative("effectKey").stringValue = "multiShotBonus";
                elem.FindPropertyRelative("effectValue").floatValue = 1f;
                elem.FindPropertyRelative("range").floatValue = 4f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            Debug.Log("[TowerSeedTool] Archer.asset re-imported on new schema.");
        }

        private static string Capitalize(string s) =>
            s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static void SetString(SerializedObject so, string prop, string val) =>
            so.FindProperty(prop)!.stringValue = val;

        private static void SetInt(SerializedObject so, string prop, int val) =>
            so.FindProperty(prop)!.intValue = val;

        private static void SetFloat(SerializedObject so, string prop, float val) =>
            so.FindProperty(prop)!.floatValue = val;

        private static void SetBool(SerializedObject so, string prop, bool val) =>
            so.FindProperty(prop)!.boolValue = val;

        private static void SetEnum(SerializedObject so, string prop, int val) =>
            so.FindProperty(prop)!.enumValueIndex = val;

        private static void SetColor(SerializedObject so, string prop, Color val)
        {
            var p = so.FindProperty(prop)!;
            p.colorValue = val;
        }

        private static void SetArmorBreak(SerializedObject so, float mul, int dur)
        {
            var ab = so.FindProperty("armorBreak")!;
            ab.FindPropertyRelative("dmgTakenMul")!.floatValue = mul;
            ab.FindPropertyRelative("durMs")!.intValue = dur;
        }

        private static void SetSynergies(SerializedObject so, List<SynergyRaw> synergies)
        {
            var prop = so.FindProperty("synergies")!;
            prop.arraySize = synergies.Count;
            for (int i = 0; i < synergies.Count; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("type")!.enumValueIndex = (int)synergies[i].type;
                elem.FindPropertyRelative("from")!.stringValue = synergies[i].from;
                elem.FindPropertyRelative("effectKey")!.stringValue = synergies[i].effectKey;
                elem.FindPropertyRelative("effectValue")!.floatValue = synergies[i].effectValue;
                elem.FindPropertyRelative("range")!.floatValue = synergies[i].range;
            }
        }

        private static void EnsureString(SerializedObject so, string prop, string defaultVal)
        {
            var p = so.FindProperty(prop);
            if (p != null && string.IsNullOrEmpty(p.stringValue))
                p.stringValue = defaultVal;
        }

        private static void EnsureFloat(SerializedObject so, string prop, float defaultVal)
        {
            var p = so.FindProperty(prop);
            if (p != null && p.floatValue == 0f)
                p.floatValue = defaultVal;
        }
    }
}
