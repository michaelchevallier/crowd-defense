#nullable enable
using System;
using UnityEngine;

namespace CrowdDefense.Data
{
    [Serializable]
    public class ChoiceDef
    {
        public string label = "";
        // Clé action : "coins+20", "castleHP-50", "pendingPerk=legendary", "skipNextPerk", etc.
        // Parsé par EventSystem.ApplyChoice au moment du choix.
        public string applyAction = "";
    }

    [CreateAssetMenu(fileName = "EventDef", menuName = "CrowdDefense/EventDef")]
    public class EventDef : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string title = "";
        [SerializeField] [TextArea(2, 5)] private string body = "";
        [SerializeField] private ChoiceDef[] choices = new ChoiceDef[2];

        public string Id => id;
        public string Title => title;
        public string Body => body;
        public ChoiceDef[] Choices => choices;
    }
}
