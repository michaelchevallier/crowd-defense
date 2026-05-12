#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/JuiceConfig", fileName = "JuiceConfig")]
    public class JuiceConfig : ScriptableObject
    {
        [Header("Tower")]
        [SerializeField] public float TowerFireShakeAmp = 0.55f;
        [SerializeField] public int TowerFireShakeMs = 100;
        [SerializeField] public float TowerFireFlashAlpha = 0.05f;

        [Header("Tower Upgrade")]
        [SerializeField] public float TowerUpgradeFlashAlpha = 0.3f;
        [SerializeField] public int TowerUpgradeFlashMs = 200;

        [Header("Castle")]
        [SerializeField] public float CastleHitShakeAmp = 0.65f;
        [SerializeField] public float CastleHitFlashAlpha = 0.1f;
        [SerializeField] public int CastleHitFlashMs = 200;
        [SerializeField] public float CastleHitFlashWarnAlpha = 0.4f;
        [SerializeField] public int CastleHitFlashWarnMs = 150;

        [Header("Boss")]
        [SerializeField] public float BossHitShakeAmp = 0.3f;
        [SerializeField] public int BossHitShakeMs = 400;
        [SerializeField] public float BossHitFlashAlpha = 0.8f;
        [SerializeField] public int BossHitFlashMs = 250;
        [SerializeField] public float BossSpawnShakeAmp = 0.8f;
        [SerializeField] public float BossSpawnShakeDur = 0.6f;
        [SerializeField] public float BossDeathFlashScale = 2.5f;

        [Header("Victory")]
        [SerializeField] public float VictoryFlashAlpha = 0.4f;
        [SerializeField] public int VictoryFlashMs = 500;
        [SerializeField] public float VictoryShakeAmp = 1.2f;

        public static JuiceConfig Get() => Resources.Load<JuiceConfig>("JuiceConfig") ?? CreateInstance<JuiceConfig>();
    }
}
