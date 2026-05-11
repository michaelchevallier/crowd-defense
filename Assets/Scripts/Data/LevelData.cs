#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum CastleLossMode { Any, All }

    [CreateAssetMenu(fileName = "LevelData", menuName = "CrowdDefense/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private string theme = "plaine";

        [Header("World / Level (for CastleHPFor formula)")]
        [SerializeField] private int world = 1;
        [SerializeField] private int level = 1;

        [Header("Map")]
        [SerializeField] private string[] mapRows = new string[0];
        [SerializeField] private float cellSize = 1f;

        [Header("Economy")]
        [SerializeField] private int startCoins = 120;

        [Header("Castle HP")]
        [SerializeField] private bool overrideCastleHP = false;
        [SerializeField] private int castleHPOverride = 200;
        [SerializeField] private CastleLossMode lossMode = CastleLossMode.Any;

        [Header("Magnet (Q3)")]
        [SerializeField] private bool allowMultiMagnet = false;

        [Header("Waves")]
        [SerializeField] private List<WaveDef> waves = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Theme => theme;
        public int World => world;
        public int Level => level;
        public IReadOnlyList<string> MapRows => mapRows;
        public float CellSize => cellSize;
        public int StartCoins => startCoins;
        public bool OverrideCastleHP => overrideCastleHP;
        public int CastleHPOverride => castleHPOverride;
        public CastleLossMode LossMode => lossMode;
        public bool AllowMultiMagnet => allowMultiMagnet;
        public IReadOnlyList<WaveDef> Waves => waves;

        // Legacy property : resolved HP for single-castle POC backward-compat.
        // Use LevelRunner.Instance.ResolveCastleHP() in new code.
        public int CastleHP => overrideCastleHP
            ? castleHPOverride
            : BalanceConfig.Get().CastleHPFor(world, level);
    }
}
