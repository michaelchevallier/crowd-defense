#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using CrowdDefense.Data;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Maps TowerType / EnemyType / HeroType SO assetKey fields to the correct V5 GLTF keys,
    /// then rebuilds the AssetRegistry entries from Models/ directory.
    /// </summary>
    public static class BuildAssetRegistryMappings
    {
        // V5 AssetLoader.js MANIFEST — tower id (SO.Id) → registry key
        private static readonly Dictionary<string, string> TowerIdToKey = new()
        {
            { "archer",   "tower_archer"   },
            { "ballista", "tower_ballista" },
            { "cannon",   "tower_cannon"   },
            { "crossbow", "tower_crossbow" },
            { "fan",      "tower_fan"      },
            { "frost",    "tower_frost"    },
            { "mage",     "tower_mage"     },
            { "magnet",   "tower_magnet"   },
            { "mine",     "tower_mine"     },
            { "portal",   "tower_portal"   },
            { "tank",     "tower_tank"     },
            // acid and skyguard have no dedicated GLTF in V5 — reuse nearest visual
            { "acid",     "tower_mage"     },
            { "skyguard", "tower_ballista" },
        };

        // Enemy id → registry key (covers cases not already set in SO assets)
        private static readonly Dictionary<string, string> EnemyIdToKey = new()
        {
            { "basic",          "zombie"           },
            { "runner",         "goblin"           },
            { "brute",          "mob_orc"          },
            { "flyer",          "mob_armabee"      },
            { "shielded",       "soldier"          },
            { "assassin",       "mob_ninja"        },
            { "imp",            "mob_frog"         },
            { "desert_runner",  "mob_cactoro"      },
            { "forest_bee",     "mob_armabee"      },
            { "forest_brute",   "mob_orc"          },
            { "plaine_pigeon",  "mob_pigeon"       },
            { "skeleton_minion","mob_skeleton"     },
            { "submarin_runner","mob_squidle"      },
            { "cyber_basic",    "mob_cyberpunk_character" },
            { "cyber_runner",   "mob_cyberpunk_2legs"    },
            { "cyber_brute",    "mob_cyberpunk_large"    },
            { "cyber_flyer",    "mob_cyberpunk_flying"   },
            { "boss",           "boss_volcan_dragon_v2"  },
            { "midboss",        "mob_orc"                },
            { "dragon_boss",    "boss_volcan_dragon_v2"  },
            { "kraken_boss",    "boss_submarin_kraken"   },
            { "wizard_king",    "boss_medieval_sorcier_roi" },
            { "ai_hub",         "boss_cyberpunk_hub_ia"  },
            { "cosmic_boss",    "boss_espace_ghost"      },
            { "apocalypse_boss","boss_apocalypse"        },
            { "warlord_boss",   "boss_apocalypse_orc_skull" },
            { "brigand_boss",   "knightgolden"           },
            { "corsair_boss",   "pirate"                 },
        };

        // Hero id → registry key
        private static readonly Dictionary<string, string> HeroIdToKey = new()
        {
            { "knight", "knight" },
            { "mage", "mage" },
            { "ranger", "ranger" },
            { "barbarian", "barbarian" },
            { "rogue", "rogue" },
        };

        [MenuItem("Tools/CrowdDefense/Build AssetRegistry Mappings")]
        public static void Generate()
        {
            int towerCount   = WireAssetKeys<TowerType>(
                "Assets/ScriptableObjects/Towers", TowerIdToKey, "assetKey");
            int enemyCount   = WireAssetKeys<EnemyType>(
                "Assets/ScriptableObjects/Enemies", EnemyIdToKey, "assetKey");
            int heroCount    = WireAssetKeys<HeroType>(
                "Assets/ScriptableObjects/Heroes", HeroIdToKey, "assetKey");

            // Re-scan Models/ and repopulate AssetRegistry entries
            AssetRegistryTool.BuildAssetRegistry();

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildAssetRegistryMappings] assetKey wired — towers:{towerCount} enemies:{enemyCount} heroes:{heroCount}");
        }

        private static int WireAssetKeys<T>(
            string folder,
            Dictionary<string, string> idToKey,
            string fieldName) where T : ScriptableObject
        {
            int count = 0;
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;

                // Get id via reflection-free SerializedObject approach
                var so = new SerializedObject(asset);
                var idProp  = so.FindProperty("id");
                var keyProp = so.FindProperty(fieldName);
                if (idProp == null || keyProp == null) continue;

                string id = idProp.stringValue;
                if (string.IsNullOrEmpty(id)) continue;

                // Only override if mapping exists AND current value is empty or already matches
                if (!idToKey.TryGetValue(id, out string? mappedKey)) continue;

                string current = keyProp.stringValue;
                if (current == mappedKey) continue;

                // Preserve manually-set assetKeys that differ from our mapping
                // (non-empty override = intentional, skip)
                if (!string.IsNullOrEmpty(current)) continue;

                keyProp.stringValue = mappedKey;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                count++;
            }
            return count;
        }
    }
}
#endif
