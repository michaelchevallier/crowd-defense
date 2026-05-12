#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    public class PlayerProfile : MonoSingleton<PlayerProfile>
    {
        private const string KeyName     = "player_name_v1";
        private const string KeyFirstRun = "player_first_run_v1";

        public static event Action<string>? OnNameChanged;

        public string GetName() =>
            PlayerPrefs.GetString(KeyName, "Joueur");

        public void SetName(string playerName)
        {
            string trimmed = string.IsNullOrWhiteSpace(playerName) ? "Joueur" : playerName.Trim();
            PlayerPrefs.SetString(KeyName, trimmed);
            PlayerPrefs.Save();
            OnNameChanged?.Invoke(trimmed);
        }

        public bool IsFirstRun() => PlayerPrefs.GetInt(KeyFirstRun, 0) == 0;

        public void MarkFirstRunDone()
        {
            PlayerPrefs.SetInt(KeyFirstRun, 1);
            PlayerPrefs.Save();
        }
    }
}
