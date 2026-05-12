#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    public readonly struct HistoryEvent
    {
        public readonly float   Time;
        public readonly string  Category;
        public readonly string  Detail;

        public HistoryEvent(float time, string category, string detail)
        {
            Time     = time;
            Category = category;
            Detail   = detail;
        }
    }

    public class WaveHistoryLog : MonoSingleton<WaveHistoryLog>
    {
        private const int MaxEvents = 50;

        private readonly Queue<HistoryEvent> _events = new(MaxEvents + 1);

        public IEnumerable<HistoryEvent> Events => _events;

        public void Log(string category, string detail)
        {
            _events.Enqueue(new HistoryEvent(Time.time, category, detail));
            if (_events.Count > MaxEvents)
                _events.Dequeue();
        }
    }
}
