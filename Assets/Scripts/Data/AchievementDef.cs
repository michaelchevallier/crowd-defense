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
        [SerializeField] public AchievementPredicateType predicateType = AchievementPredicateType.Event;
        [SerializeField] public int threshold = 1;
        // For Counter predicates: matches the eventKey passed to Achievements.TrackEvent.
        [SerializeField] public string eventKey = "";
        [SerializeField] public string iconEmoji = "";

        public string IconEmoji => string.IsNullOrEmpty(iconEmoji) ? "\U0001F3C6" : iconEmoji;
    }
}
