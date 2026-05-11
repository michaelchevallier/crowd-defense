#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    // SO qui mappe chaque LevelTheme à ses matériaux de surface.
    // waterMat / lavaMat sont surchargés par thème — null = fallback couleur unie.
    // Créer une instance dans Resources/LevelThemeMaterialConfig.asset
    [CreateAssetMenu(fileName = "LevelThemeMaterialConfig",
                     menuName  = "CrowdDefense/LevelThemeMaterialConfig")]
    public class LevelThemeMaterialConfig : ScriptableObject
    {
        private static LevelThemeMaterialConfig? _instance;

        public static LevelThemeMaterialConfig? Get()
        {
            if (_instance == null)
                _instance = Resources.Load<LevelThemeMaterialConfig>("LevelThemeMaterialConfig");
            return _instance;
        }

        [Serializable]
        public struct ThemeEntry
        {
            public LevelTheme theme;
            [Tooltip("Applied to WATER cells for this theme. Null = plain color fallback.")]
            public Material? waterMat;
            [Tooltip("Applied to LAVA cells for this theme. Null = plain color fallback.")]
            public Material? lavaMat;
            [Tooltip("Applied to all standard (non-special) cells. Null = default toon lit.")]
            public Material? surfaceMat;
        }

        [SerializeField] private ThemeEntry[] entries = Array.Empty<ThemeEntry>();

        // Default materials loaded from Resources/Materials/ at runtime
        [SerializeField] private Material? defaultWaterMat;
        [SerializeField] private Material? defaultLavaMat;
        [SerializeField] private Material? defaultSurfaceMat;

        private Dictionary<LevelTheme, ThemeEntry>? _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<LevelTheme, ThemeEntry>();
            foreach (var e in entries)
                _lookup[e.theme] = e;
        }

        public Material? GetWaterMat(LevelTheme theme)
        {
            if (_lookup == null) BuildLookup();
            if (_lookup!.TryGetValue(theme, out var e) && e.waterMat != null) return e.waterMat;
            return defaultWaterMat != null
                ? defaultWaterMat
                : Resources.Load<Material>("Materials/Toon_Water");
        }

        public Material? GetLavaMat(LevelTheme theme)
        {
            if (_lookup == null) BuildLookup();
            if (_lookup!.TryGetValue(theme, out var e) && e.lavaMat != null) return e.lavaMat;
            return defaultLavaMat != null
                ? defaultLavaMat
                : Resources.Load<Material>("Materials/Toon_Lava");
        }

        public Material? GetSurfaceMat(LevelTheme theme)
        {
            if (_lookup == null) BuildLookup();
            if (_lookup!.TryGetValue(theme, out var e) && e.surfaceMat != null) return e.surfaceMat;
            return defaultSurfaceMat != null
                ? defaultSurfaceMat
                : Resources.Load<Material>("Materials/Toon_Default");
        }

        private void OnEnable() => _lookup = null;
    }
}
