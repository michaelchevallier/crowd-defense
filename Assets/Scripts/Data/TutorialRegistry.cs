#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/TutorialRegistry", fileName = "TutorialRegistry")]
    public class TutorialRegistry : ScriptableObject
    {
        [SerializeField] private TutorialStepDef[] steps = {};

        public TutorialStepDef[] Steps => steps;

        private static TutorialRegistry? _instance;

        public static TutorialRegistry? Get()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<TutorialRegistry>("TutorialRegistry");
            return _instance;
        }
    }
}
