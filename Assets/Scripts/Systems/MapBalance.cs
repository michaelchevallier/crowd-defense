#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    // Port of V5 MapBalance.js — pure computation, no Unity runtime dependencies.
    // Used by Editor auto-tune tools and optional runtime difficulty validation.
    // EnemyStats / TowerStats mirror V5 tables exactly; helpers are pure static methods.
    public static class MapBalance
    {
        // ---------------------------------------------------------------
        // Stat tables (mirror V5 ENEMY_STATS / TOWER_STATS)
        // ---------------------------------------------------------------

        public readonly struct EnemyStat
        {
            public readonly float Hp;
            public readonly float Speed;
            public readonly int   Reward;
            public readonly bool  IsFlyer;
            public readonly bool  IsBoss;

            public EnemyStat(float hp, float speed, int reward, bool isFlyer = false, bool isBoss = false)
            {
                Hp = hp; Speed = speed; Reward = reward; IsFlyer = isFlyer; IsBoss = isBoss;
            }
        }

        public readonly struct TowerStat
        {
            public readonly int   Cost;
            public readonly float Dps;
            public readonly float Range;
            public readonly bool  AntiAir;
            public readonly bool  AntiAirOnly;
            public readonly bool  Aoe;
            public readonly bool  Slow;

            public TowerStat(int cost, float dps, float range,
                bool antiAir = false, bool antiAirOnly = false, bool aoe = false, bool slow = false)
            {
                Cost = cost; Dps = dps; Range = range;
                AntiAir = antiAir; AntiAirOnly = antiAirOnly; Aoe = aoe; Slow = slow;
            }
        }

        public static readonly IReadOnlyDictionary<string, EnemyStat> EnemyStats =
            new Dictionary<string, EnemyStat>
            {
                ["basic"]            = new(3,   1.2f,  2),
                ["runner"]           = new(1,   2.4f,  2),
                ["brute"]            = new(12,  0.8f,  8),
                ["shielded"]         = new(6,   1.0f,  5),
                ["flyer"]            = new(2,   1.6f,  4, isFlyer: true),
                ["assassin"]         = new(2,   2.0f,  4),
                ["imp"]              = new(4,   1.5f,  5),
                ["skeleton_minion"]  = new(5,   1.0f,  3),
                ["cyber_basic"]      = new(4,   1.0f,  4),
                ["cyber_runner"]     = new(2,   2.2f,  3),
                ["cyber_flyer"]      = new(3,   1.6f,  4, isFlyer: true),
                ["cyber_brute"]      = new(18,  0.7f, 14),
                ["midboss"]          = new(30,  0.7f, 15, isBoss: true),
                ["boss"]             = new(60,  0.4f, 50, isBoss: true),
                ["brigand_boss"]     = new(60,  0.5f, 50, isBoss: true),
                ["warlord_boss"]     = new(120, 0.45f, 80, isBoss: true),
                ["corsair_boss"]     = new(200, 0.5f, 120, isBoss: true),
                ["dragon_boss"]      = new(300, 0.6f, 150, isFlyer: true, isBoss: true),
                ["apocalypse_boss"]  = new(500, 0.4f, 250, isBoss: true),
                ["cosmic_boss"]      = new(600, 0.5f, 300, isFlyer: true, isBoss: true),
                ["kraken_boss"]      = new(700, 0.4f, 400, isBoss: true),
                ["wizard_king"]      = new(800, 0.5f, 500, isBoss: true),
                ["ai_hub"]           = new(1000,0.4f, 700, isBoss: true),
            };

        public static readonly IReadOnlyDictionary<string, TowerStat> TowerStats =
            new Dictionary<string, TowerStat>
            {
                ["archer"]   = new(30,  3.15f, 8,  antiAir: true),
                ["tank"]     = new(50,  5.02f, 5,  antiAir: true),
                ["mage"]     = new(70,  3.68f, 7,  antiAir: true,  aoe: true),
                ["ballista"] = new(100, 5.89f, 14, antiAir: true),
                ["cannon"]   = new(100, 5.52f, 9,  aoe: true),
                ["frost"]    = new(60,  0f,    3,  slow: true),
                ["crossbow"] = new(140, 6.13f, 16, antiAir: true),
                ["skyguard"] = new(85, 14.72f, 8,  antiAir: true, antiAirOnly: true),
            };

        public const float SwarmMul = 1.4f;

        // ---------------------------------------------------------------
        // expectedDifficulty — V5 parity
        // worldIdx 1..10, levelIdx 1..8 → W1.1 = 1.0, W10.8 ≈ 8.8
        // ---------------------------------------------------------------

        public static float ExpectedDifficulty(int worldIdx, int levelIdx) =>
            Mathf.Pow(1.20f, worldIdx - 1) * Mathf.Pow(1.08f, levelIdx - 1);

        // ---------------------------------------------------------------
        // Metrics
        // ---------------------------------------------------------------

        public struct LevelMetrics
        {
            public float  PathLength;
            public float  StraightDist;
            public int    PathCells;
            public int    MapW, MapH;
            public float  TotalWaveHP;
            public float  GroundWaveHP;
            public float  FlyerWaveHP;
            public float  FlyerCount;
            public float  GroundTimeToCastle;
            public float  FlyerTimeToCastle;
            public float  AvailableDps;
            public float  AntiAirDps;
            public bool   HasAntiAir;
            public float  LevelDifficulty;
        }

        public static LevelMetrics ComputeMetrics(
            IReadOnlyList<string> mapRows, float cellSize,
            IReadOnlyList<WaveDef> waves,
            int startCoins,
            IReadOnlyList<string>? forbiddenTowers = null)
        {
            int h = mapRows.Count;
            int w = h > 0 ? mapRows[0].Length : 0;

            int pathCells = CountPathCells(mapRows, w, h);
            float pathLength = pathCells * cellSize;
            float straightDist = ComputeStraightDist(mapRows, w, h) * cellSize;

            float groundTime = pathLength / 1.2f;
            float flyerTime  = straightDist > 0f ? straightDist / 1.6f : pathLength / 1.6f;

            float totalHP = 0, groundHP = 0, flyerHP = 0, flyerCount = 0;
            foreach (var wave in waves)
            {
                foreach (var entry in wave.entries)
                {
                    if (entry.type == null) continue;
                    string typeId = entry.type.Id;
                    if (!EnemyStats.TryGetValue(typeId, out var stat)) continue;
                    float mul = stat.IsBoss ? 1f : SwarmMul;
                    float eff = entry.count * mul;
                    float hp  = stat.Hp * eff;
                    if (stat.IsFlyer) { flyerHP += hp; flyerCount += eff; }
                    else              { groundHP += hp; }
                }
            }
            totalHP = groundHP + flyerHP;

            var allowed = BuildAllowedTowers(forbiddenTowers);
            allowed.Sort((a, b) => (b.Dps / Mathf.Max(1, b.Cost)).CompareTo(a.Dps / Mathf.Max(1, a.Cost)));

            const int NTowersAvg = 4;
            float availDps = 0f;
            for (int i = 0; i < NTowersAvg && i < allowed.Count; i++)
                availDps += allowed[i].Dps;

            float aaaDps = 0f;
            bool hasAA = false;
            foreach (var t in allowed)
            {
                if (t.AntiAir) { aaaDps = t.Dps; hasAA = true; break; }
            }

            float gDiff = groundHP / Mathf.Max(1f, availDps * groundTime);
            float fDiff = hasAA ? flyerHP / Mathf.Max(1f, aaaDps * flyerTime) : (flyerHP > 0 ? float.PositiveInfinity : 0f);
            float totalDiff = gDiff + (flyerHP > 0 ? fDiff : 0f);

            return new LevelMetrics
            {
                PathLength        = pathLength,
                StraightDist      = straightDist,
                PathCells         = pathCells,
                MapW              = w, MapH = h,
                TotalWaveHP       = totalHP,
                GroundWaveHP      = groundHP,
                FlyerWaveHP       = flyerHP,
                FlyerCount        = flyerCount,
                GroundTimeToCastle = groundTime,
                FlyerTimeToCastle  = flyerTime,
                AvailableDps      = availDps,
                AntiAirDps        = aaaDps,
                HasAntiAir        = hasAA,
                LevelDifficulty   = totalDiff,
            };
        }

        // ---------------------------------------------------------------
        // Auto-tune (Editor tool helper, mutates waveDef lists in place)
        // ---------------------------------------------------------------

        public struct AutoTuneResult
        {
            public bool   Mutated;
            public string Changes;
            public float  NewRatio;
        }

        public static AutoTuneResult AutoTune(
            LevelData level, LevelMetrics metrics, float target)
        {
            float ratio = metrics.LevelDifficulty / Mathf.Max(0.0001f, target);
            var changes = new System.Text.StringBuilder();
            bool mutated = false;

            if (ratio > 1.25f)
            {
                // Too hard — boost startCoins
                // (LevelData is a ScriptableObject — caller must wrap in Undo in Editor)
                // We just compute the recommendation here, not apply it.
                int oldCoins = level.StartCoins;
                int newCoins = Mathf.Min(
                    Mathf.RoundToInt(oldCoins * Mathf.Min(1.5f, ratio)),
                    Mathf.RoundToInt(oldCoins * 1.5f));
                if (newCoins != oldCoins)
                {
                    changes.Append($"startCoins {oldCoins} -> {newCoins}");
                    mutated = true;
                }
            }
            else if (ratio < 0.45f)
            {
                float boostFactor = Mathf.Min(2.0f, 0.75f / Mathf.Max(0.05f, ratio));
                changes.Append($"wave counts +{boostFactor:F2}x");
                mutated = true;
            }

            return new AutoTuneResult
            {
                Mutated  = mutated,
                Changes  = changes.ToString(),
                NewRatio = ratio,
            };
        }

        // ---------------------------------------------------------------
        // Anti-air validation
        // ---------------------------------------------------------------

        public struct AntiAirCheck
        {
            public bool   Ok;
            public string? Reason;
            public string? Warning;
        }

        public static AntiAirCheck CheckAntiAir(LevelMetrics m)
        {
            if (m.FlyerCount == 0) return new AntiAirCheck { Ok = true };
            if (!m.HasAntiAir)
                return new AntiAirCheck
                {
                    Ok = false,
                    Reason = $"level has {m.FlyerCount} flyers but no anti-air tower allowed",
                };
            float idealFlyerCount = Mathf.Floor(m.StraightDist / 8f);
            if (idealFlyerCount > 0 &&
                Mathf.Abs(m.FlyerCount - idealFlyerCount) > idealFlyerCount * 0.5f)
                return new AntiAirCheck
                {
                    Ok      = true,
                    Warning = $"flyer count {m.FlyerCount} far from ideal {idealFlyerCount}",
                };
            return new AntiAirCheck { Ok = true };
        }

        // ---------------------------------------------------------------
        // Internal helpers
        // ---------------------------------------------------------------

        private static int CountPathCells(IReadOnlyList<string> rows, int w, int h)
        {
            int count = 0;
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    char ch = rows[r][c];
                    if (ch == '1' || ch == 'P' || ch == 'C' || ch == '~' || ch == '^') count++;
                }
            return count;
        }

        private static float ComputeStraightDist(IReadOnlyList<string> rows, int w, int h)
        {
            var portals  = new List<(int c, int r)>();
            var castles  = new List<(int c, int r)>();
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    char ch = rows[r][c];
                    if (ch == 'P') portals.Add((c, r));
                    else if (ch == 'C') castles.Add((c, r));
                }
            if (portals.Count == 0 || castles.Count == 0) return 0f;
            float total = 0f; int n = 0;
            foreach (var p in portals)
                foreach (var ca in castles)
                {
                    float dx = p.c - ca.c, dy = p.r - ca.r;
                    total += Mathf.Sqrt(dx * dx + dy * dy);
                    n++;
                }
            return n > 0 ? total / n : 0f;
        }

        private static List<TowerStat> BuildAllowedTowers(IReadOnlyList<string>? forbidden)
        {
            var list = new List<TowerStat>();
            foreach (var kv in TowerStats)
            {
                if (forbidden != null)
                {
                    bool skip = false;
                    foreach (var f in forbidden) if (f == kv.Key) { skip = true; break; }
                    if (skip) continue;
                }
                list.Add(kv.Value);
            }
            return list;
        }
    }
}
