#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
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

        [Header("Briefing")]
        [SerializeField] private string briefing = "";

        [Header("Cutscene")]
        [SerializeField] private string cutsceneIdAtStart = "";

        [Header("Magnet (Q3)")]
        [SerializeField] private bool allowMultiMagnet = false;

        [Header("Waves")]
        [SerializeField] private List<WaveDef> waves = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Theme => theme;
        public LevelTheme LevelTheme => LevelThemeExtensions.Parse(theme);
        public int World => world;
        public int Level => level;
        public IReadOnlyList<string> MapRows => mapRows;
        public float CellSize => cellSize;
        public int StartCoins => startCoins;
        public bool OverrideCastleHP => overrideCastleHP;
        public int CastleHPOverride => castleHPOverride;
        public string Briefing => briefing;
        public string CutsceneIdAtStart => cutsceneIdAtStart;
        public bool AllowMultiMagnet => allowMultiMagnet;
        public IReadOnlyList<WaveDef> Waves => waves;

        public int CastleHP => overrideCastleHP
            ? castleHPOverride
            : BalanceConfig.Get().CastleHPFor(world, level);

        /// Runtime only: populate waves for endless/procedural runs (not serialized).
        public void SetEndlessWaves(List<WaveDef> generatedWaves)
        {
            waves = generatedWaves;
            id = "endless";
            displayName = "Sans Fin";
            overrideCastleHP = true;
            castleHPOverride = 300;
            world = 1;
            level = 1;
            startCoins = 120;
        }

        /// Runtime only: append extra waves (used by endless mode to extend on demand).
        public void AppendWaves(List<WaveDef> extra) => waves.AddRange(extra);
    }
}
