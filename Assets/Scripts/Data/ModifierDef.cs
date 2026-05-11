#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum ModifierType { Curse, Blessing }

    [CreateAssetMenu(fileName = "ModifierDef", menuName = "CrowdDefense/ModifierDef")]
    public class ModifierDef : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private Sprite? icon;
        [SerializeField] private ModifierType modifierType = ModifierType.Curse;
        [SerializeField] [TextArea(1, 3)] private string desc = "";
        // Action appliquée au RunContext au moment de l'activation.
        // Même syntaxe que ChoiceDef.applyAction.
        [SerializeField] private string applyAction = "";

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite? Icon => icon;
        public ModifierType ModifierType => modifierType;
        public string Desc => desc;
        public string ApplyAction => applyAction;
    }
}
