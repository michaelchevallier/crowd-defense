#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "ModifierRegistry", menuName = "CrowdDefense/ModifierRegistry")]
    public class ModifierRegistry : ScriptableObject
    {
        [SerializeField] private ModifierDef[] modifiers = System.Array.Empty<ModifierDef>();

        private static ModifierRegistry? _instance;

        public static ModifierRegistry? Get()
        {
            if (_instance == null)
                _instance = Resources.Load<ModifierRegistry>("ModifierRegistry");
            return _instance;
        }

        public ModifierDef? FindById(string id)
        {
            for (int i = 0; i < modifiers.Length; i++)
                if (modifiers[i] != null && modifiers[i].Id == id)
                    return modifiers[i];
            return null;
        }

        public ModifierDef? PickRandom()
        {
            if (modifiers.Length == 0) return null;
            return modifiers[Random.Range(0, modifiers.Length)];
        }
    }
}
