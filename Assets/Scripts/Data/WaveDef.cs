#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [Serializable]
    public struct WaveEvent
    {
        /// <summary>1-based wave index when this event fires.</summary>
        public int waveIndex;
        /// <summary>Event type string matching DynamicEventManager enum names (snake_case).</summary>
        public string eventType;
        /// <summary>Duration in seconds (clamped 3..60 by DynamicEventManager).</summary>
        public float duration;
        /// <summary>Optional float param (e.g. radius for void_pulse, count for hack).</summary>
        public float param;
    }

    public enum SpawnPattern
    {
        Linear,
        Sparse,
        Cluster,
        VFormation,
    }

    [Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyType type;
        public int count;
        public EnemyVariant variant;
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
        public SpawnPattern pattern;
    }
}
