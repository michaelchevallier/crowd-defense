#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "LevelRegistry", menuName = "CrowdDefense/LevelRegistry")]
    public class LevelRegistry : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new();

        public IReadOnlyList<LevelData> Levels => levels;

        public LevelData? FindById(string id)
        {
            for (int i = 0; i < levels.Count; i++)
                if (levels[i] != null && levels[i].Id == id) return levels[i];
            return null;
        }

        private static LevelRegistry? _instance;

        public static LevelRegistry Get()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<LevelRegistry>("LevelRegistry");
            if (_instance == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[LevelRegistry] LevelRegistry.asset not found in Resources/. Run Tools/CrowdDefense/Build LevelRegistry.");
#endif
            }
            return _instance!;
        }
    }
}
