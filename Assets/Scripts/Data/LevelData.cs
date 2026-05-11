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

        [Header("Map")]
        [SerializeField] private string[] mapRows = new string[0];
        [SerializeField] private float cellSize = 1f;

        [Header("Economy")]
        [SerializeField] private int startCoins = 120;
        [SerializeField] private int castleHP = 120;

        [Header("Waves")]
        [SerializeField] private List<WaveDef> waves = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Theme => theme;
        public IReadOnlyList<string> MapRows => mapRows;
        public float CellSize => cellSize;
        public int StartCoins => startCoins;
        public int CastleHP => castleHP;
        public IReadOnlyList<WaveDef> Waves => waves;
    }
}
