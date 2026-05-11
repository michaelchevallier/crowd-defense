#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum TutorialAdvanceTrigger
    {
        Manual,
        TowerPlaced,
        WaveStarted,
        EnemyKilled,
        HeroPlaced,
        CoinsCollected,
    }

    [CreateAssetMenu(menuName = "CrowdDefense/TutorialStepDef", fileName = "TutorialStepDef")]
    public class TutorialStepDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string textKey = "";
        [SerializeField] public string highlightTarget = "";
        [SerializeField] public TutorialAdvanceTrigger advanceTrigger = TutorialAdvanceTrigger.Manual;
        [SerializeField] public bool showNextButton = true;
    }
}
