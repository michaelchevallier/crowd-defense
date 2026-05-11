#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "SkinRegistry", menuName = "CrowdDefense/SkinRegistry")]
    public class SkinRegistry : ScriptableObject
    {
        [SerializeField] private List<SkinDef> skins = new();

        private static SkinRegistry? _instance;

        public static SkinRegistry? Get()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<SkinRegistry>("SkinRegistry");
            return _instance;
        }

        public SkinDef? FindById(string id)
        {
            for (int i = 0; i < skins.Count; i++)
                if (skins[i] != null && skins[i].Id == id) return skins[i];
            return null;
        }

        public SkinDef? FindDefaultFor(SkinTargetType targetType, string targetId)
        {
            for (int i = 0; i < skins.Count; i++)
            {
                var s = skins[i];
                if (s == null) continue;
                if (s.TargetType == targetType && s.TargetId == targetId && s.IsDefault)
                    return s;
            }
            return null;
        }

        public IReadOnlyList<SkinDef> All => skins;
    }
}
