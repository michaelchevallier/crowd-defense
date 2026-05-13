#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum MagicSchool { Fire, Frost, Stonework }

    [CreateAssetMenu(fileName = "MagicSchoolDef", menuName = "CrowdDefense/Data/MagicSchoolDef")]
    public class MagicSchoolDef : ScriptableObject
    {
        [SerializeField] public MagicSchool school;
        [SerializeField] public string      displayName = "";
        [SerializeField] public string      emoji       = "";
        [SerializeField] public Sprite?     icon;
        [TextArea]
        [SerializeField] public string      description = "";
        [SerializeField] public List<string> exclusivePerkIds = new();
    }
}
