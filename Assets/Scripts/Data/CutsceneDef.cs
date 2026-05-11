#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum PortraitSide { Left, Right }

    [Serializable]
    public class CutsceneLine
    {
        [SerializeField] public string speaker = "";
        [SerializeField] public string textKey = "";
        [SerializeField] public Sprite? portrait;
        [SerializeField] public PortraitSide side = PortraitSide.Left;
    }

    [CreateAssetMenu(fileName = "CutsceneDef", menuName = "CrowdDefense/CutsceneDef")]
    public class CutsceneDef : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string titleKey = "";
        [SerializeField] private List<CutsceneLine> lines = new();

        public string Id => id;
        public string TitleKey => titleKey;
        public IReadOnlyList<CutsceneLine> Lines => lines;
    }
}
