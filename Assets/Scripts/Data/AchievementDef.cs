#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/AchievementDef", fileName = "AchievementDef")]
    public class AchievementDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string titleKey = "";
        [SerializeField] public string descKey = "";
        [SerializeField] public bool hidden = false;
        [SerializeField] public int points = 10;
        [SerializeField] public Sprite? icon;
    }
}
