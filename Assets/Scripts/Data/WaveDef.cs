#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyType type;
        public int count;
    }

    [Serializable]
    public struct WaveDef
    {
        public List<EnemySpawnEntry> entries;
        public int spawnRateMs;
        public int breakMs;
        /// <summary>
        /// Which portal index enemies spawn from.
        /// -1 = round-robin across all available paths.
        /// </summary>
        [SerializeField] public int portalIdx;
        /// <summary>
        /// Endless mode: multiplicateur HP+Damage appliqué lors du spawn (1.15^wave).
        /// 1f = normal (tous les niveaux classiques).
        /// </summary>
        public float scaleMul;
    }
}
