#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    // Port of SceneDecor.js (V5) → Unity.
    // Spawns background props beyond the grid at level start; cleans up on level end.
    // Props are looked up by name from Resources or from the Inspector palette arrays.
    // X-ray fade and camera culling from V5 are deferred (hero not in Unity yet).
    public class SceneDecor : MonoSingleton<SceneDecor>
    {
        [Header("Background Prefabs per Theme")]
        [SerializeField] private GameObject? plainePrefab;
        [SerializeField] private GameObject? foretPrefab;
        [SerializeField] private GameObject? desertPrefab;
        [SerializeField] private GameObject? volcanPrefab;
        [SerializeField] private GameObject? apocalypsePrefab;
        [SerializeField] private GameObject? espacePrefab;
        [SerializeField] private GameObject? submarinPrefab;
        [SerializeField] private GameObject? medievalPrefab;
        [SerializeField] private GameObject? cyberpunkPrefab;
        [SerializeField] private GameObject? foirePrefab;

        // Per-theme prop spawn parameters, mirroring V5 THEME_PALETTE.
        private static readonly Dictionary<LevelTheme, ThemePalette> Palettes = new()
        {
            [LevelTheme.Plaine] = new ThemePalette
            {
                BigCount = 6, MediumCount = 5, SmallCount = 9, RockCount = 4,
                BigScaleMin = 1.4f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Foret] = new ThemePalette
            {
                BigCount = 11, MediumCount = 9, SmallCount = 12, RockCount = 6,
                BigScaleMin = 1.2f, BigScaleMax = 1.8f,
            },
            [LevelTheme.Desert] = new ThemePalette
            {
                BigCount = 7, MediumCount = 10, SmallCount = 13, RockCount = 9,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Volcan] = new ThemePalette
            {
                BigCount = 7, MediumCount = 10, SmallCount = 11, RockCount = 11,
                BigScaleMin = 1.0f, BigScaleMax = 1.7f,
            },
            [LevelTheme.Apocalypse] = new ThemePalette
            {
                BigCount = 8, MediumCount = 11, SmallCount = 12, RockCount = 12,
                BigScaleMin = 1.0f, BigScaleMax = 1.8f,
            },
            [LevelTheme.Espace] = new ThemePalette
            {
                BigCount = 6, MediumCount = 8, SmallCount = 10, RockCount = 6,
                BigScaleMin = 1.2f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Submarin] = new ThemePalette
            {
                BigCount = 5, MediumCount = 9, SmallCount = 14, RockCount = 6,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Medieval] = new ThemePalette
            {
                BigCount = 7, MediumCount = 11, SmallCount = 14, RockCount = 8,
                BigScaleMin = 1.2f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Cyberpunk] = new ThemePalette
            {
                BigCount = 5, MediumCount = 11, SmallCount = 14, RockCount = 4,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Foire] = new ThemePalette
            {
                BigCount = 6, MediumCount = 10, SmallCount = 14, RockCount = 4,
                BigScaleMin = 1.2f, BigScaleMax = 1.8f,
            },
        };

        // Fallback resource paths for background prefabs
        private const string PrefabRoot = "Prefabs/Decor/";

        private readonly List<GameObject> _spawnedDecor = new();

        // Spawn all background decor for a given theme, using a deterministic seed
        // derived from the level id (mirrors V5 hashLevelId).
        public void SpawnForLevel(LevelTheme theme, string levelId, Bounds gridBounds)
        {
            ClearAll();

            var bgPrefab = ResolveBackgroundPrefab(theme);
            if (bgPrefab != null)
            {
                var bg = Instantiate(bgPrefab, Vector3.zero, Quaternion.identity, transform);
                bg.name = $"Decor_BG_{theme}";
                _spawnedDecor.Add(bg);
            }

            if (!Palettes.TryGetValue(theme, out var palette)) return;

            var rng = new System.Random(HashLevelId(levelId));
            float spread = Mathf.Max(gridBounds.size.x, gridBounds.size.z) * 0.75f;
            float border = spread * 0.5f + 2f;

            SpawnRing(palette.BigCount,    0.9f, 1.1f, palette.BigScaleMin, palette.BigScaleMax, border, spread, rng, $"Decor_{theme}_Big");
            SpawnRing(palette.MediumCount, 0.5f, 0.7f, 0.7f, 1.1f, border * 0.9f, spread * 0.9f, rng, $"Decor_{theme}_Medium");
            SpawnRing(palette.RockCount,   0.3f, 0.5f, 0.5f, 0.8f, border * 0.8f, spread * 1.1f, rng, $"Decor_{theme}_Rock");
        }

        public void ClearAll()
        {
            foreach (var go in _spawnedDecor)
            {
                if (go != null) Destroy(go);
            }
            _spawnedDecor.Clear();
        }

        // Spawns `count` primitive capsules as placeholder props arranged in a ring outside the grid.
        // When real GLTF props arrive in AssetRegistry they can be swapped here.
        private void SpawnRing(
            int count,
            float yMin, float yMax,
            float scaleMin, float scaleMax,
            float innerRadius, float outerRadius,
            System.Random rng,
            string namePrefix)
        {
            if (count <= 0) return;
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i + (float)(rng.NextDouble() * step * 0.6 - step * 0.3);
                float rad = Mathf.Deg2Rad * angle;
                float dist = innerRadius + (float)rng.NextDouble() * (outerRadius - innerRadius);
                float x = Mathf.Cos(rad) * dist;
                float z = Mathf.Sin(rad) * dist;
                float y = yMin + (float)rng.NextDouble() * (yMax - yMin);
                float s = scaleMin + (float)rng.NextDouble() * (scaleMax - scaleMin);

                var go = SpawnProp(new Vector3(x, y, z), Quaternion.Euler(0f, angle + 90f, 0f), s * Vector3.one, namePrefix + $"_{i}");
                if (go != null) _spawnedDecor.Add(go);
            }
        }

        // Spawn a single prop at world position. Currently uses a capsule placeholder;
        // replace with AssetRegistry lookup once GLTF decor assets are imported.
        private GameObject? SpawnProp(Vector3 pos, Quaternion rot, Vector3 scale, string goName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = goName;
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            // Remove physics collider — decor is pure visual
            var col = go.GetComponent<CapsuleCollider>();
            if (col != null) Destroy(col);
            return go;
        }

        private GameObject? ResolveBackgroundPrefab(LevelTheme theme)
        {
            var fromInspector = theme switch
            {
                LevelTheme.Plaine     => plainePrefab,
                LevelTheme.Foret      => foretPrefab,
                LevelTheme.Desert     => desertPrefab,
                LevelTheme.Volcan     => volcanPrefab,
                LevelTheme.Apocalypse => apocalypsePrefab,
                LevelTheme.Espace     => espacePrefab,
                LevelTheme.Submarin   => submarinPrefab,
                LevelTheme.Medieval   => medievalPrefab,
                LevelTheme.Cyberpunk  => cyberpunkPrefab,
                LevelTheme.Foire      => foirePrefab,
                _ => null
            };
            if (fromInspector != null) return fromInspector;

            var path = PrefabRoot + theme.ToString();
            var loaded = Resources.Load<GameObject>(path);
#if UNITY_EDITOR
            if (loaded == null)
                Debug.LogWarning($"[SceneDecor] Background prefab not found at Resources/{path}. " +
                                 "Create it via Assets/Prefabs/Decor/ or assign in Inspector.");
#endif
            return loaded;
        }

        // FNV-1a 32-bit — same algorithm as V5 hashLevelId.
        private static int HashLevelId(string id)
        {
            uint h = 2166136261u;
            foreach (char c in id)
            {
                h ^= (uint)c;
                h *= 16777619u;
            }
            return (int)(h & 0x7fffffff);
        }

        private readonly struct ThemePalette
        {
            public int BigCount    { get; init; }
            public int MediumCount { get; init; }
            public int SmallCount  { get; init; }
            public int RockCount   { get; init; }
            public float BigScaleMin { get; init; }
            public float BigScaleMax { get; init; }
        }
    }
}
