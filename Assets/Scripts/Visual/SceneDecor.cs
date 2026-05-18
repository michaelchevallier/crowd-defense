#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Visual
{
    // Port of SceneDecor.js (V5) → Unity.
    // Spawns background props beyond the grid at level start; cleans up on level end.
    // Props are looked up by key from AssetRegistry (assign in Inspector) with capsule fallback.
    // LateUpdate: decor fade (occluding towers → alpha 0.3) + tower xray (occluding hero → blue ghost).
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

        [Header("Prop Registry (GLTF keys)")]
        [SerializeField] private AssetRegistry? propRegistry;

        // Mirrors V5 THEME_PALETTE — per-tier GLTF asset key arrays per theme.
        private static readonly Dictionary<LevelTheme, ThemePalette> Palettes = new()
        {
            [LevelTheme.Plaine] = new ThemePalette
            {
                BigKeys    = new[] { "nature_commontree1", "nature_commontree2", "nature_commontree3" },
                MediumKeys = new[] { "nature_bushflower" },
                SmallKeys  = new[] { "nature_flower3", "nature_flower4" },
                RockKeys   = new[] { "nature_rock1", "nature_pebble1" },
                BigCount = 6, MediumCount = 5, SmallCount = 9, RockCount = 4,
                BigScaleMin = 1.4f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Foret] = new ThemePalette
            {
                BigKeys    = new[] { "nature_pine1", "nature_pine2", "nature_pine3" },
                MediumKeys = new[] { "nature_bush", "nature_fern" },
                SmallKeys  = new[] { "nature_mushroom" },
                RockKeys   = new[] { "nature_rock1", "nature_rock2" },
                BigCount = 11, MediumCount = 9, SmallCount = 12, RockCount = 6,
                BigScaleMin = 1.2f, BigScaleMax = 1.8f,
            },
            [LevelTheme.Desert] = new ThemePalette
            {
                BigKeys    = new[] { "nature_rock3", "nature_rock2" },
                MediumKeys = new[] { "nature_rock1", "nature_rock2", "nature_rock3" },
                SmallKeys  = new[] { "nature_pebble1", "nature_pebble2" },
                RockKeys   = new[] { "nature_rock3", "nature_pebble1" },
                BigCount = 7, MediumCount = 10, SmallCount = 13, RockCount = 9,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Volcan] = new ThemePalette
            {
                BigKeys    = new[] { "nature_rock1", "nature_rock2", "nature_rock3" },
                MediumKeys = new[] { "nature_rock1", "nature_rock2", "nature_rock3" },
                SmallKeys  = new[] { "nature_pebble1", "nature_pebble2" },
                RockKeys   = new[] { "nature_rock1", "nature_rock2" },
                BigCount = 7, MediumCount = 10, SmallCount = 11, RockCount = 11,
                BigScaleMin = 1.0f, BigScaleMax = 1.7f,
            },
            [LevelTheme.Apocalypse] = new ThemePalette
            {
                BigKeys    = new[] { "decor_apocalypse_deadtree", "decor_apocalypse_wall_broken", "decor_apocalypse_wall_overgrown", "decor_apocalypse_curve_overgrown" },
                MediumKeys = new[] { "decor_apocalypse_arch_gothic", "decor_apocalypse_arch_round", "decor_apocalypse_cart", "decor_apocalypse_tent", "decor_apocalypse_bush_round" },
                SmallKeys  = new[] { "decor_apocalypse_pot_1", "decor_apocalypse_pot_2", "decor_apocalypse_pot_3", "decor_apocalypse_gascan", "decor_apocalypse_propanetank", "decor_apocalypse_beartrap" },
                RockKeys   = new[] { "nature_rock1", "decor_apocalypse_column_round", "decor_apocalypse_column_square", "decor_apocalypse_barrel" },
                BigCount = 8, MediumCount = 11, SmallCount = 12, RockCount = 12,
                BigScaleMin = 1.0f, BigScaleMax = 1.8f,
            },
            [LevelTheme.Espace] = new ThemePalette
            {
                BigKeys    = new[] { "decor_espace_planet_1", "decor_espace_planet_3", "decor_espace_planet_5" },
                MediumKeys = new[] { "decor_espace_alien_plant", "decor_espace_spike_tree", "decor_espace_satellite", "decor_espace_radar" },
                SmallKeys  = new[] { "decor_espace_crater", "decor_espace_alien", "decor_espace_planet_7", "decor_espace_planet_10" },
                RockKeys   = new[] { "nature_rock3", "decor_espace_crater" },
                BigCount = 6, MediumCount = 8, SmallCount = 10, RockCount = 6,
                BigScaleMin = 1.2f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Submarin] = new ThemePalette
            {
                BigKeys    = new[] { "decor_submarin_shipwreck", "nature_pine1" },
                MediumKeys = new[] { "decor_submarin_boat", "decor_submarin_clownfish", "decor_submarin_blacklionfish" },
                SmallKeys  = new[] { "decor_submarin_blobfish", "decor_submarin_tetra", "decor_submarin_mandarinfish", "decor_submarin_piranha" },
                RockKeys   = new[] { "nature_rock1", "nature_rock2" },
                BigCount = 5, MediumCount = 9, SmallCount = 14, RockCount = 6,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Medieval] = new ThemePalette
            {
                BigKeys    = new[] { "decor_medieval_temple", "decor_medieval_wall_towers", "decor_medieval_tower_watch", "decor_medieval_bookcase_full" },
                MediumKeys = new[] { "decor_medieval_cannon", "decor_medieval_gothic_door", "decor_medieval_round_door", "tower_house", "decor_medieval_bridge_section", "decor_medieval_bookcase_empty" },
                SmallKeys  = new[] { "decor_medieval_chest", "decor_medieval_chest_gold", "decor_medieval_flag", "decor_medieval_flag_gothic", "decor_medieval_candles", "decor_medieval_crate" },
                RockKeys   = new[] { "nature_rock1", "nature_rock2", "decor_apocalypse_column_round" },
                BigCount = 7, MediumCount = 11, SmallCount = 14, RockCount = 8,
                BigScaleMin = 1.2f, BigScaleMax = 2.0f,
            },
            [LevelTheme.Cyberpunk] = new ThemePalette
            {
                BigKeys    = new[] { "decor_cyberpunk_computer", "decor_cyberpunk_antenna_1", "decor_cyberpunk_antenna_2" },
                MediumKeys = new[] { "decor_cyberpunk_tv_1", "decor_cyberpunk_tv_2", "decor_cyberpunk_tv_3", "decor_cyberpunk_lamp_1" },
                SmallKeys  = new[] { "decor_cyberpunk_sign_1", "decor_cyberpunk_sign_2", "decor_cyberpunk_sign_3", "decor_cyberpunk_lamp_2" },
                RockKeys   = new[] { "nature_rock3", "nature_pebble2" },
                BigCount = 5, MediumCount = 11, SmallCount = 14, RockCount = 4,
                BigScaleMin = 1.0f, BigScaleMax = 1.6f,
            },
            [LevelTheme.Foire] = new ThemePalette
            {
                BigKeys    = new[] { "decor_foire_ferris_wheel", "nature_commontree1" },
                MediumKeys = new[] { "decor_foire_balloons", "decor_foire_popcorn", "nature_bushflower" },
                SmallKeys  = new[] { "nature_flower3", "nature_flower4", "decor_foire_balloons" },
                RockKeys   = new[] { "nature_pebble1", "nature_pebble2" },
                BigCount = 6, MediumCount = 10, SmallCount = 14, RockCount = 4,
                BigScaleMin = 1.2f, BigScaleMax = 1.8f,
            },
        };

        private const string PrefabRoot = "Prefabs/Decor/";
        private readonly List<GameObject> _spawnedDecor = new();

        // Registered towers for xray occlusion check (populated by Tower.Init via RegisterTower).
        private readonly List<Tower> _towers = new();

        // Per-renderer MaterialPropertyBlock pool (zero alloc per frame).
        // NOTE: must NOT use field-initializer — MaterialPropertyBlock's ctor calls CreateImpl
        // which Unity 6 forbids during MonoBehaviour construction. Initialised in Awake.
        private MaterialPropertyBlock _mpb = null!;

        // Cached hero reference (lazy, re-checked if null).
        private Hero? _hero;

        // Track xray state per tower to avoid redundant SetPropertyBlock calls.
        private readonly Dictionary<Tower, bool> _towerXRayState = new();

        // Track fade state per prop to avoid redundant SetPropertyBlock calls.
        private readonly Dictionary<GameObject, bool> _decorFadeState = new();

        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        protected override void OnAwakeSingleton()
        {
            _mpb = new MaterialPropertyBlock();
        }

        // Called by Tower.Init after spawning so SceneDecor can track it for xray.
        public void RegisterTower(Tower t)
        {
            if (!_towers.Contains(t)) _towers.Add(t);
        }

        public void UnregisterTower(Tower t)
        {
            _towers.Remove(t);
            _towerXRayState.Remove(t);
        }

        // Entry point used by LevelVisualBridge and direct callers (alias for clarity).
        public void BuildForTheme(LevelTheme theme, string levelId, Bounds gridBounds)
            => SpawnForLevel(theme, levelId, gridBounds);

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
            float border  = spread * 0.5f + 2f;

            SpawnRing(palette.BigCount,    palette.BigKeys,    0.9f, 1.1f, palette.BigScaleMin, palette.BigScaleMax, border,        spread,        rng, $"Decor_{theme}_Big");
            SpawnRing(palette.MediumCount, palette.MediumKeys, 0.5f, 0.7f, 0.7f, 1.1f,            border * 0.9f,     spread * 0.9f, rng, $"Decor_{theme}_Medium");
            SpawnRing(palette.SmallCount,  palette.SmallKeys,  0.1f, 0.3f, 0.4f, 0.7f,            border * 0.7f,     spread * 1.2f, rng, $"Decor_{theme}_Small");
            SpawnRing(palette.RockCount,   palette.RockKeys,   0.3f, 0.5f, 0.5f, 0.8f,            border * 0.8f,     spread * 1.1f, rng, $"Decor_{theme}_Rock");
        }

        public void ClearAll()
        {
            // Defensive: any of these could be null if ClearAll fires before Awake/field
            // initializers (e.g., during MonoSingleton auto-creation in an inactive parent).
            // Live editor.log captured a NullReferenceException here on level-start; the
            // fallback path was hard to reproduce but a null check costs nothing.
            if (_spawnedDecor != null)
            {
                foreach (var go in _spawnedDecor)
                {
                    if (go != null) Destroy(go);
                }
                _spawnedDecor.Clear();
            }
            _decorFadeState?.Clear();
            _towers?.Clear();
            _towerXRayState?.Clear();
            _hero = null;
        }

        private void LateUpdate()
        {
            UpdateDecorFade();
            UpdateTowerXRay();
        }

        // Decor props that are between the camera and any tower become semi-transparent (alpha 0.3).
        private void UpdateDecorFade()
        {
            var cam = MainCameraCache.Main;
            if (cam == null || _spawnedDecor.Count == 0 || _towers.Count == 0) return;

            Vector3 camPos = cam.transform.position;

            foreach (var prop in _spawnedDecor)
            {
                if (prop == null) continue;
                bool occluding = IsBetweenCamAndTower(camPos, prop.transform.position);
                _decorFadeState.TryGetValue(prop, out bool prev);
                if (occluding == prev && _decorFadeState.ContainsKey(prop)) continue;
                _decorFadeState[prop] = occluding;
                SetRendererAlpha(prop, occluding ? 0.3f : 1f);
            }
        }

        // Towers that are between the camera and the hero get a blue ghost outline (xray).
        private void UpdateTowerXRay()
        {
            var cam = MainCameraCache.Main;
            if (cam == null || _towers.Count == 0) return;

            if (_hero == null) _hero = FindAnyObjectByType<Hero>();
            if (_hero == null) return;

            Vector3 camPos  = cam.transform.position;
            Vector3 heroPos = _hero.transform.position;

            foreach (var tower in _towers)
            {
                if (tower == null) continue;
                bool occluding = IsBetweenCamAndTarget(camPos, tower.transform.position, heroPos);
                _towerXRayState.TryGetValue(tower, out bool prev);
                if (occluding == prev && _towerXRayState.ContainsKey(tower)) continue;
                _towerXRayState[tower] = occluding;
                SetTowerXRayActive(tower.gameObject, occluding);
            }
        }

        // Returns true if testPos lies within the camera-to-towerPos segment (within tolerance).
        private bool IsBetweenCamAndTower(Vector3 camPos, Vector3 testPos)
        {
            foreach (var tower in _towers)
            {
                if (tower == null) continue;
                if (IsBetweenCamAndTarget(camPos, testPos, tower.transform.position))
                    return true;
            }
            return false;
        }

        // Returns true if midPos lies roughly between camPos and targetPos (dot + distance heuristic).
        private static bool IsBetweenCamAndTarget(Vector3 camPos, Vector3 midPos, Vector3 targetPos)
        {
            Vector3 camToTarget = targetPos - camPos;
            float totalDist = camToTarget.magnitude;
            if (totalDist < 0.01f) return false;

            Vector3 dir = camToTarget / totalDist;
            Vector3 camToMid = midPos - camPos;
            float proj = Vector3.Dot(camToMid, dir);

            // midPos must be along the segment (not behind cam or beyond target).
            if (proj < 0.5f || proj > totalDist - 0.5f) return false;

            // Lateral deviation: prop must be within 1.5 world units of the line.
            Vector3 closest = camPos + dir * proj;
            return (midPos - closest).sqrMagnitude < 2.25f; // 1.5^2
        }

        // Apply alpha via MaterialPropertyBlock on all renderers of the prop (zero alloc).
        private void SetRendererAlpha(GameObject prop, float alpha)
        {
            foreach (var r in prop.GetComponentsInChildren<Renderer>())
            {
                _mpb.Clear();
                r.GetPropertyBlock(_mpb);
                // Try _BaseColor (URP Lit / ToonBase) then _Alpha fallback.
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                {
                    Color c = r.sharedMaterial.GetColor(BaseColorId);
                    c.a = alpha;
                    _mpb.SetColor(BaseColorId, c);
                }
                else
                {
                    _mpb.SetFloat(AlphaId, alpha);
                }
                r.SetPropertyBlock(_mpb);
            }
        }

        // Toggle blue ghost (xray) on a tower: tint renderers blue+transparent via MPB.
        private void SetTowerXRayActive(GameObject towerRoot, bool active)
        {
            foreach (var r in towerRoot.GetComponentsInChildren<Renderer>())
            {
                _mpb.Clear();
                r.GetPropertyBlock(_mpb);
                if (active)
                {
                    // Semi-transparent blue overlay via _BaseColor alpha channel.
                    _mpb.SetColor(BaseColorId, new Color(0.3f, 0.6f, 1f, 0.35f));
                }
                else
                {
                    // Restore: clear mpb overrides so material defaults take effect.
                    // SetPropertyBlock with empty block resets overrides.
                }
                r.SetPropertyBlock(_mpb);
            }
        }

        private void SpawnRing(
            int count,
            string[] keys,
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
                float rad   = Mathf.Deg2Rad * angle;
                float dist  = innerRadius + (float)rng.NextDouble() * (outerRadius - innerRadius);
                float x     = Mathf.Cos(rad) * dist;
                float z     = Mathf.Sin(rad) * dist;
                float y     = yMin + (float)rng.NextDouble() * (yMax - yMin);
                float s     = scaleMin + (float)rng.NextDouble() * (scaleMax - scaleMin);
                string key  = keys[(int)(rng.NextDouble() * keys.Length) % keys.Length];

                var go = SpawnProp(key, new Vector3(x, y, z), Quaternion.Euler(0f, angle + 90f, 0f), s * Vector3.one, namePrefix + $"_{i}");
                if (go != null) _spawnedDecor.Add(go);
            }
        }

        // Spawn a single prop via AssetRegistry GLTF lookup; falls back to capsule primitive.
        private GameObject? SpawnProp(string key, Vector3 pos, Quaternion rot, Vector3 scale, string goName)
        {
            GameObject? prefab = propRegistry?.Get(key);

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var col = go.GetComponent<CapsuleCollider>();
                if (col != null) Destroy(col);
            }

            go.name = goName;
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            if (!goName.Contains("_Rock")) go.AddComponent<WindSway>();
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

            var path   = PrefabRoot + theme.ToString();
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
            public string[] BigKeys    { get; init; }
            public string[] MediumKeys { get; init; }
            public string[] SmallKeys  { get; init; }
            public string[] RockKeys   { get; init; }
            public int BigCount    { get; init; }
            public int MediumCount { get; init; }
            public int SmallCount  { get; init; }
            public int RockCount   { get; init; }
            public float BigScaleMin { get; init; }
            public float BigScaleMax { get; init; }
        }
    }
}
