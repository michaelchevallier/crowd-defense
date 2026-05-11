#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "CutsceneRegistry", menuName = "CrowdDefense/CutsceneRegistry")]
    public class CutsceneRegistry : ScriptableObject
    {
        [SerializeField] private List<CutsceneDef> cutscenes = new();

        private static CutsceneRegistry? _instance;

        public static CutsceneRegistry? Get()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<CutsceneRegistry>("CutsceneRegistry");
            return _instance;
        }

        public CutsceneDef? FindById(string id)
        {
            for (int i = 0; i < cutscenes.Count; i++)
                if (cutscenes[i] != null && cutscenes[i].Id == id) return cutscenes[i];
            return null;
        }

        public IReadOnlyList<CutsceneDef> All => cutscenes;
    }
}
