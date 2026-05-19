#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;
using CrowdDefense.UI;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Spawns loot pickups (gem / xp_token) on enemy death.
    /// Gem  : 3% chance, +50¢, yellow emissive.
    /// XP   : 8% chance, +10 XP, blue emissive.
    /// Boss : guaranteed 5 gems + 3 xp tokens.
    /// Pickups float+spin, auto-despawn 15 s, collected by Hero proximity (&lt;1.5 m).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class LootSpawner : MonoSingleton<LootSpawner>
    {
        private const float GemChance       = 0.03f;
        private const float XpChance        = 0.08f;
        private const float PickupRadius    = 1.5f;
        private const float PickupRadiusSq  = PickupRadius * PickupRadius;
        private const float PickupLifetime  = 15f;
        private const float FloatAmplitude  = 0.18f;
        private const float FloatFrequency  = 1.4f;
        private const float SpinSpeedDeg    = 90f;

        private static readonly Color GemColor = new Color(1f,   0.88f, 0.05f);
        private static readonly Color XpColor  = new Color(0.2f, 0.55f, 1.0f);

        // ── Pickup record ──────────────────────────────────────────────────────
        private enum LootType { Gem, Xp }

        private sealed class Pickup
        {
            public GameObject Go;
            public LootType   Type;
            public Vector3    BasePos;
            public float      SpawnTime;
            public float      PhaseOffset;

            public Pickup(GameObject go, LootType type, Vector3 basePos)
            {
                Go          = go;
                Type        = type;
                BasePos     = basePos;
                SpawnTime   = Time.time;
                PhaseOffset = Random.value * Mathf.PI * 2f;
            }
        }

        private readonly System.Collections.Generic.List<Pickup> _active = new(32);

        // ── Event subscription ────────────────────────────────────────────────
        protected override void OnAwakeSingleton()
        {
            Enemy.OnDeathStatic += HandleEnemyDeath;
        }

        protected override void OnDestroySingleton()
        {
            Enemy.OnDeathStatic -= HandleEnemyDeath;
        }

        // ── Death hook ────────────────────────────────────────────────────────
        private void HandleEnemyDeath(Enemy enemy, bool isBoss)
        {
            Vector3 pos = enemy.transform.position + Vector3.up * 0.5f;

            if (isBoss)
            {
                for (int i = 0; i < 5; i++)
                    SpawnPickup(LootType.Gem, RandomNearPos(pos, 1.5f));
                for (int i = 0; i < 3; i++)
                    SpawnPickup(LootType.Xp, RandomNearPos(pos, 1.5f));
            }
            else
            {
                if (Random.value < GemChance)
                    SpawnPickup(LootType.Gem, pos);
                else if (Random.value < XpChance)
                    SpawnPickup(LootType.Xp, pos);
            }
        }

        private static Vector3 RandomNearPos(Vector3 center, float radius)
        {
            float a = Random.value * Mathf.PI * 2f;
            float r = Random.value * radius;
            return center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        // ── Spawn ─────────────────────────────────────────────────────────────
        private void SpawnPickup(LootType type, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = type == LootType.Gem ? "LootGem" : "LootXp";
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * (type == LootType.Gem ? 0.28f : 0.24f);
            Destroy(go.GetComponent<Collider>());

            Color col = type == LootType.Gem ? GemColor : XpColor;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                              ?? Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Standard")!);
            mat.color = col;
            // Emissive glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * 1.6f);
            go.GetComponent<Renderer>().material = mat;

            _active.Add(new Pickup(go, type, pos));
        }

        // ── Update: float + spin + proximity collect + auto-despawn ───────────
        private void Update()
        {
            if (_active.Count == 0) return;

            var hero = LevelRunner.Instance?.Hero;
            float now  = Time.time;
            float dt   = Time.deltaTime;
            Vector3 heroPos = hero != null ? hero.transform.position : Vector3.zero;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                if (p.Go == null) { _active.RemoveAt(i); continue; }

                // Auto-despawn
                if (now - p.SpawnTime >= PickupLifetime)
                {
                    Destroy(p.Go);
                    _active.RemoveAt(i);
                    continue;
                }

                // Float + spin
                float elapsed = now - p.SpawnTime;
                float yOff    = Mathf.Sin(elapsed * FloatFrequency * Mathf.PI * 2f + p.PhaseOffset) * FloatAmplitude;
                p.Go.transform.position = p.BasePos + new Vector3(0f, yOff + 0.35f, 0f);
                p.Go.transform.Rotate(0f, SpinSpeedDeg * dt, 0f, Space.World);

                // Hero collect
                if (hero != null &&
                    (p.Go.transform.position - heroPos).sqrMagnitude < PickupRadiusSq)
                {
                    CollectPickup(p);
                    _active.RemoveAt(i);
                }
            }
        }

        private static void CollectPickup(Pickup p)
        {
            Destroy(p.Go);
            Vector3 pos = p.Go != null ? p.Go.transform.position : Vector3.zero;

            if (p.Type == LootType.Gem)
            {
                Economy.Instance?.AddGold(50);
                FloatingPopupController.Instance?.SpawnGems(50, pos);
                AudioController.Instance?.Play("coin_pickup", 0.8f);
            }
            else
            {
                var hero = LevelRunner.Instance?.Hero;
                hero?.GainXp(10f);
                FloatingPopupController.Instance?.SpawnReward("+10 XP", pos, XpColor);
                AudioController.Instance?.Play("xp_pickup", 0.8f);
            }
        }
    }
}
