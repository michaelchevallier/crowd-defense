#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum AchievementPredicateType { Event, Counter }

    [CreateAssetMenu(menuName = "CrowdDefense/AchievementDef", fileName = "AchievementDef")]
    public class AchievementDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string titleKey = "";
        [SerializeField] public string descKey = "";
        [SerializeField] public string iconPath = "";
        [SerializeField] public bool hidden = false;
        [SerializeField] public int points = 10;
        [SerializeField] public Sprite? icon;
        // eventKey matches TrackEvent keys: "enemy_killed", "wave_cleared", "tower_placed",
        // "gold_earned", "level_complete", "synergy_activated", "boss_killed"
        [SerializeField] public string eventKey = "";
        [SerializeField] public AchievementPredicateType predicateType = AchievementPredicateType.Event;
        [SerializeField] public int threshold = 1;
    }
}
