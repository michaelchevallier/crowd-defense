#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    public enum RunMapNodeType
    {
        Combat,
        Elite,
        Mystery,
        Shop,
        Rest,
        Boss,
    }

    [Serializable]
    public class RunMapNode
    {
        public string id = "";
        public RunMapNodeType type;
        public int row;
        public int col;
        public List<string> edges = new();
        public bool consumed;
        // Only set for Combat/Elite nodes
        public string combatLevelId = "";
        // Only set for Elite nodes
        public float swarmMul = 1f;
        public float rewardMul = 1f;
        // Only set for Boss nodes
        public string bossId = "";
    }

    [Serializable]
    public class RunMapState
    {
        public int worldId;
        public int seed;
        public string currentNodeId = "";
        public List<string> visitedNodeIds = new();
        public List<RunMapNode> nodes = new();
        public bool complete;
    }

    // Roguelike graph generator + runtime state. Persisted via SaveSystem.
    // Mirrors V5 RunMap.js: 7 rows (r0=start, r1-r5=content, r6=boss),
    // 3-4 columns per content row, mulberry32 seeded RNG.
    public class RunMap : MonoSingleton<RunMap>
    {
        private static readonly string[][] ActLevelPools =
        {
            // Act 1 — worlds 1-2
            new[] {
                "world1-1","world1-2","world1-3","world1-4","world1-5","world1-6","world1-7",
                "world2-1","world2-2","world2-3","world2-4","world2-5","world2-6","world2-7",
            },
            // Act 2 — worlds 3-4
            new[] {
                "world3-1","world3-2","world3-3","world3-4","world3-5","world3-6","world3-7",
                "world4-1","world4-2","world4-3","world4-4","world4-5","world4-6","world4-7",
            },
            // Act 3 — worlds 5-6
            new[] {
                "world5-1","world5-2","world5-3","world5-4","world5-5","world5-6","world5-7",
                "world6-1","world6-2","world6-3","world6-4","world6-5","world6-6","world6-7",
            },
        };

        private static readonly string[] BossPool =
        {
            "brigand_boss",
            "warlord_boss",
            "corsair_boss",
            "dragon_boss",
            "apocalypse_boss",
        };

        private RunMapState? _state;

        public RunMapState? State => _state;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _state = SaveSystem.GetRunMapState();
        }

        // Generates a new map for given worldId (1-based act index) and seed.
        // Discards any existing map state.
        public void Generate(int worldId, int seed)
        {
            int act = Mathf.Clamp(worldId, 1, ActLevelPools.Length);
            var rng = Mulberry32((uint)(seed * 31 + act));
            var nodes = new List<RunMapNode>();
            var nodeMap = new Dictionary<string, RunMapNode>();

            int startCount = 1 + (int)(rng() * 2);
            for (int c = 0; c < startCount; c++)
            {
                var n = MakeNode($"act{act}-r0-c{c}", 0, c, RunMapNodeType.Combat, act, rng);
                nodes.Add(n);
                nodeMap[n.id] = n;
            }

            for (int r = 1; r <= 5; r++)
            {
                int colCount = 3 + (int)(rng() * 2);
                for (int c = 0; c < colCount; c++)
                {
                    var type = PickNodeType(rng);
                    var n = MakeNode($"act{act}-r{r}-c{c}", r, c, type, act, rng);
                    nodes.Add(n);
                    nodeMap[n.id] = n;
                }
            }

            var bossNode = new RunMapNode
            {
                id = $"act{act}-r6-boss",
                row = 6,
                col = 0,
                type = RunMapNodeType.Boss,
                bossId = PickBossForAct(seed, act),
            };
            nodes.Add(bossNode);
            nodeMap[bossNode.id] = bossNode;

            for (int r = 0; r <= 5; r++)
            {
                var currentRow = FilterByRow(nodes, r);
                var nextRow = FilterByRow(nodes, r + 1);
                foreach (var node in currentRow)
                {
                    int edgeCount = 1 + (int)(rng() * 2);
                    node.edges = PickEdgeTargets(node, nextRow, edgeCount, rng);
                }
            }

            if (!VerifyReachability(nodes, nodeMap))
                RepairReachability(nodes);

            string firstNodeId = nodes.Count > 0 ? nodes[0].id : "";

            _state = new RunMapState
            {
                worldId = worldId,
                seed = seed,
                currentNodeId = firstNodeId,
                visitedNodeIds = new List<string>(),
                nodes = nodes,
                complete = false,
            };

            SaveSystem.SetRunMapState(_state);
        }

        public RunMapNode? GetCurrentNode()
        {
            if (_state == null || string.IsNullOrEmpty(_state.currentNodeId)) return null;
            return FindNode(_state.currentNodeId);
        }

        public List<RunMapNode> GetAvailableNextNodes()
        {
            var current = GetCurrentNode();
            if (current == null || _state == null) return new List<RunMapNode>();

            var result = new List<RunMapNode>();
            foreach (var edgeId in current.edges)
            {
                var n = FindNode(edgeId);
                if (n != null && !n.consumed)
                    result.Add(n);
            }
            return result;
        }

        // Returns the start nodes (row 0) for initial path choice display.
        public List<RunMapNode> GetStartNodes()
        {
            if (_state == null) return new List<RunMapNode>();
            return FilterByRow(_state.nodes, 0);
        }

        public void MoveTo(string nodeId)
        {
            if (_state == null) return;
            var current = GetCurrentNode();
            if (current != null)
            {
                current.consumed = true;
                if (!_state.visitedNodeIds.Contains(current.id))
                    _state.visitedNodeIds.Add(current.id);
            }
            _state.currentNodeId = nodeId;
            var next = FindNode(nodeId);
            if (next != null && next.type == RunMapNodeType.Boss)
                _state.complete = true;
            SaveSystem.SetRunMapState(_state);
        }

        public bool IsComplete() => _state?.complete ?? false;

        public bool HasActiveMap() => _state != null && _state.nodes.Count > 0;

        public bool IsNodeVisited(string nodeId) =>
            _state?.visitedNodeIds.Contains(nodeId) ?? false;

        public bool IsNodeCurrent(string nodeId) =>
            _state?.currentNodeId == nodeId;

        // Returns all nodes grouped by row for UI rendering.
        public Dictionary<int, List<RunMapNode>> GetNodesByRow()
        {
            var result = new Dictionary<int, List<RunMapNode>>();
            if (_state == null) return result;
            foreach (var n in _state.nodes)
            {
                if (!result.ContainsKey(n.row))
                    result[n.row] = new List<RunMapNode>();
                result[n.row].Add(n);
            }
            return result;
        }

        // ── Private helpers ──────────────────────────────────────────────

        private RunMapNode? FindNode(string id)
        {
            if (_state == null) return null;
            for (int i = 0; i < _state.nodes.Count; i++)
                if (_state.nodes[i].id == id) return _state.nodes[i];
            return null;
        }

        private static List<RunMapNode> FilterByRow(List<RunMapNode> nodes, int row)
        {
            var result = new List<RunMapNode>();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].row == row) result.Add(nodes[i]);
            return result;
        }

        private static RunMapNode MakeNode(string id, int row, int col, RunMapNodeType type, int act, Func<float> rng)
        {
            var node = new RunMapNode { id = id, row = row, col = col, type = type, swarmMul = 1f, rewardMul = 1f };
            if (type == RunMapNodeType.Combat || type == RunMapNodeType.Elite)
            {
                int actIndex = Mathf.Clamp(act - 1, 0, ActLevelPools.Length - 1);
                var pool = ActLevelPools[actIndex];
                node.combatLevelId = pool[(int)(rng() * pool.Length) % pool.Length];
                if (type == RunMapNodeType.Elite)
                {
                    node.swarmMul = 1.3f;
                    node.rewardMul = 1.25f;
                }
            }
            return node;
        }

        private static List<string> PickEdgeTargets(RunMapNode node, List<RunMapNode> nextRow, int count, Func<float> rng)
        {
            if (nextRow.Count == 0) return new List<string>();
            var sorted = new List<RunMapNode>(nextRow);
            sorted.Sort((a, b) => Math.Abs(a.col - node.col).CompareTo(Math.Abs(b.col - node.col)));
            int candidateCount = Math.Min(3, sorted.Count);
            var candidates = sorted.GetRange(0, candidateCount);
            candidates.Sort((_, __) => rng() > 0.5f ? 1 : -1);
            int take = Math.Min(count, candidates.Count);
            var result = new List<string>(take);
            for (int i = 0; i < take; i++)
                result.Add(candidates[i].id);
            return result;
        }

        private static RunMapNodeType PickNodeType(Func<float> rng)
        {
            float r = rng();
            if (r < 0.55f) return RunMapNodeType.Combat;
            if (r < 0.65f) return RunMapNodeType.Elite;
            if (r < 0.77f) return RunMapNodeType.Mystery;
            if (r < 0.84f) return RunMapNodeType.Shop;
            if (r < 0.91f) return RunMapNodeType.Rest;
            return RunMapNodeType.Combat;
        }

        private static bool CanReach(RunMapNode node, RunMapNode target, Dictionary<string, RunMapNode> nodeMap, HashSet<string> visited)
        {
            if (node.id == target.id) return true;
            if (visited.Contains(node.id)) return false;
            visited.Add(node.id);
            foreach (var edgeId in node.edges)
            {
                if (nodeMap.TryGetValue(edgeId, out var next) && CanReach(next, target, nodeMap, visited))
                    return true;
            }
            return false;
        }

        private static bool VerifyReachability(List<RunMapNode> nodes, Dictionary<string, RunMapNode> nodeMap)
        {
            var startNodes = FilterByRow(nodes, 0);
            var bossNode = nodes.Find(n => n.row == 6);
            if (bossNode == null) return false;
            foreach (var start in startNodes)
                if (CanReach(start, bossNode, nodeMap, new HashSet<string>())) return true;
            return false;
        }

        private static void RepairReachability(List<RunMapNode> nodes)
        {
            for (int r = 1; r <= 6; r++)
            {
                var currentRow = FilterByRow(nodes, r);
                var prevRow = FilterByRow(nodes, r - 1);
                var incoming = new HashSet<string>();
                foreach (var prev in prevRow)
                    foreach (var e in prev.edges) incoming.Add(e);
                foreach (var node in currentRow)
                {
                    if (!incoming.Contains(node.id) && prevRow.Count > 0)
                        prevRow[0].edges.Add(node.id);
                }
            }
        }

        private static string PickBossForAct(int seed, int act, List<string>? bossesUsed = null)
        {
            var rng = Mulberry32((uint)(seed * 7 + act * 11));
            var available = new List<string>(BossPool);
            if (bossesUsed != null)
                available.RemoveAll(b => bossesUsed.Contains(b));
            if (available.Count == 0) return BossPool[0];
            return available[(int)(rng() * available.Count) % available.Count];
        }

        // Mulberry32 — deterministic PRNG matching V5 JS implementation.
        private static Func<float> Mulberry32(uint seed)
        {
            uint s = seed;
            return () =>
            {
                s = unchecked(s + 0x6D2B79F5u);
                uint t = unchecked(s ^ (s >> 15));
                t = unchecked((uint)((int)t * (1 | (int)s)));
                t = unchecked(t + (uint)((int)(t ^ (t >> 7)) * (61 | (int)t)));
                t ^= t >> 14;
                return (t >> 0) / 4294967296f;
            };
        }
    }
}
