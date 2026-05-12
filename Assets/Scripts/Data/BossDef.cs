#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "BossDef", menuName = "CrowdDefense/BossDef")]
    public class BossDef : ScriptableObject
    {
        [SerializeField] private EnemyType? enemyType;
        [SerializeField] private string displayNameFr = "";
        [SerializeField] private int world = 1;
        [SerializeField] private Color auraColor = Color.red;
        [SerializeField] private string cutsceneSubtitle = "";
        [SerializeField] private string[] cutsceneLines = System.Array.Empty<string>();

        [Header("Phase thresholds (HP ratios)")]
        [SerializeField, Range(0f, 1f)] private float enragedAt = 0.5f;
        [SerializeField, Range(0f, 1f)] private float desperateAt = 0.2f;

        [Header("Phase modifiers")]
        [SerializeField] private float enragedSpeedMul = 1.4f;
        [SerializeField] private float enragedSummonCdMul = 0.6f;

        public EnemyType? EnemyType => enemyType;
        public string DisplayNameFr => displayNameFr;
        public int World => world;
        public Color AuraColor => auraColor;
        public string CutsceneSubtitle => cutsceneSubtitle;
        public string[] CutsceneLines => cutsceneLines;
        public float EnragedAt => enragedAt;
        public float DesperateAt => desperateAt;
        public float EnragedSpeedMul => enragedSpeedMul;
        public float EnragedSummonCdMul => enragedSummonCdMul;
    }
}
