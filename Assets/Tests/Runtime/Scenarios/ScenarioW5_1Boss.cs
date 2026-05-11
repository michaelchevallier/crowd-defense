#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CrowdDefense.Data;

namespace CrowdDefense.Tests.Runtime.Scenarios
{
    // Sprint-gate scenario : W5-1 boss spawn smoke check.
    //
    // Strategy : validates that boss-tier enemies are present in the level's wave
    // definitions, NOT a live run. Live boss combat run is too brittle for CI :
    // depends on tower DPS tuning + boss HP + spawn timing. Once integration is
    // stable, this can be extended.
    public class ScenarioW5_1Boss
    {
        private const string W5_1Id = "W5-1";

        [UnityTest]
        public IEnumerator W5_1_BossEnemyDeclaredInWaves()
        {
            var registry = Resources.Load<LevelRegistry>("LevelRegistry");
            if (registry == null)
            {
                Assert.Inconclusive(
                    "LevelRegistry not found in Resources/. Run Tools/CrowdDefense/Build LevelRegistry first.");
                yield break;
            }

            var w51 = registry.FindById(W5_1Id);
            if (w51 == null)
            {
                Assert.Inconclusive($"LevelData '{W5_1Id}' not found in LevelRegistry.");
                yield break;
            }

            Assert.IsNotNull(w51);
            Assert.Greater(w51!.Waves.Count, 0, "W5-1 must declare waves.");

            // Walk waves to find at least one boss-tier enemy.
            bool foundBoss = false;
            foreach (var wave in w51.Waves)
            {
                if (wave.entries == null) continue;
                foreach (var entry in wave.entries)
                {
                    if (entry.type == null) continue;
                    if (IsBossTier(entry.type))
                    {
                        foundBoss = true;
                        break;
                    }
                }
                if (foundBoss) break;
            }

            Assert.IsTrue(foundBoss,
                "W5-1 must declare at least one boss-tier enemy across its waves.");

            yield return null;
        }

        private static bool IsBossTier(EnemyType type)
        {
            // Use reflection to be tolerant to schema evolution.
            var t = typeof(EnemyType);
            foreach (var fName in new[] { "isBoss", "isMidBoss", "isApocalypseBoss" })
            {
                var f = t.GetField(fName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (f == null) continue;
                if (f.GetValue(type) is bool b && b) return true;
            }
            return false;
        }
    }
}
