#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public enum InputEventType
    {
        TowerPlaced,
        TowerUpgraded,
        TowerSold,
    }

    [Serializable]
    public sealed class InputEvent
    {
        public float time;
        public int   frame;
        public string type  = "";
        public string data  = "";
    }

    [Serializable]
    sealed class ReplayPayload
    {
        public string  levelId = "";
        public float   recordedAt;
        public List<InputEvent> events = new();
    }

    public class ReplayRecorder : MonoSingleton<ReplayRecorder>
    {
        private const string PlayerPrefsKey = "cd.replay.last";

        private readonly List<InputEvent> _events = new();
        private string _levelId = "";

        protected override void OnAwakeSingleton()
        {
            _events.Clear();
        }

        private void Start()
        {
            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnLevelComplete += HandleLevelEnd;
                LevelRunner.Instance.OnLevelLost     += HandleLevelEnd;
            }

            if (PlacementController.Instance != null)
            {
                PlacementController.Instance.OnTowerPlaced   += HandleTowerPlaced;
                PlacementController.Instance.OnTowerUpgraded += HandleTowerUpgraded;
                PlacementController.Instance.OnTowerSold     += HandleTowerSold;
            }

            _levelId = LevelRunner.Instance?.CurrentLevel?.Id ?? "";
        }

        private void OnDestroy()
        {
            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnLevelComplete -= HandleLevelEnd;
                LevelRunner.Instance.OnLevelLost     -= HandleLevelEnd;
            }

            if (PlacementController.Instance != null)
            {
                PlacementController.Instance.OnTowerPlaced   -= HandleTowerPlaced;
                PlacementController.Instance.OnTowerUpgraded -= HandleTowerUpgraded;
                PlacementController.Instance.OnTowerSold     -= HandleTowerSold;
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private static string TowerKey(Tower t)
        {
            Vector3 p = t.transform.position;
            return $"{t.Config?.Id ?? "?"}@{p.x:F0},{p.z:F0}";
        }

        private void HandleTowerPlaced(Tower t) =>
            Record(InputEventType.TowerPlaced, TowerKey(t));

        private void HandleTowerUpgraded(Tower t, int newLevel) =>
            Record(InputEventType.TowerUpgraded, $"{TowerKey(t)}:lvl{newLevel}");

        private void HandleTowerSold(Tower t, int refund) =>
            Record(InputEventType.TowerSold, $"{TowerKey(t)}:refund{refund}");

        private void HandleLevelEnd() => Persist();

        // ── Core ──────────────────────────────────────────────────────────────

        private void Record(InputEventType type, string data)
        {
            _events.Add(new InputEvent
            {
                time  = Time.time,
                frame = Time.frameCount,
                type  = type.ToString(),
                data  = data,
            });
        }

        private void Persist()
        {
            var payload = new ReplayPayload
            {
                levelId    = _levelId,
                recordedAt = Time.realtimeSinceStartup,
                events     = _events,
            };
            string json = JsonUtility.ToJson(payload, prettyPrint: false);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ReplayRecorder] saved {_events.Count} events to PlayerPrefs key \"{PlayerPrefsKey}\"");
#endif
        }
    }
}
