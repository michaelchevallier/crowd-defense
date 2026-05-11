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
    }
}
