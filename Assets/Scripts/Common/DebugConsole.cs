#nullable enable
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.Common
{
    public class DebugConsole : MonoSingleton<DebugConsole>
    {
        private bool _visible;
        private string _input = "";
        private Vector2 _scrollPos;
        private readonly List<string> _history = new();
        private int _historyIndex = -1;

        private GUIStyle? _boxStyle;
        private GUIStyle? _textStyle;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _visible = !_visible;

            // History navigation
            if (_visible && _input.Length > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) && _history.Count > 0)
                {
                    _historyIndex = Mathf.Max(-1, _historyIndex - 1);
                    _input = _historyIndex >= 0 ? _history[_history.Count - 1 - _historyIndex] : "";
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) && _history.Count > 0)
                {
                    _historyIndex = Mathf.Min(_history.Count - 1, _historyIndex + 1);
                    _input = _historyIndex >= 0 ? _history[_history.Count - 1 - _historyIndex] : "";
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            _boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture2D.whiteTexture },
            };
            _textStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };

            float width = Screen.width * 0.6f;
            float height = 300f;
            float x = (Screen.width - width) * 0.5f;
            float y = 40f;

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.Box(new Rect(x, y, width, height), "", _boxStyle);

            Rect contentRect = new Rect(x + 10, y + 10, width - 20, height - 60);

            // History display
            GUI.color = Color.white;
            _scrollPos = GUI.BeginScrollView(contentRect, _scrollPos, 
                new Rect(0, 0, contentRect.width - 20, _history.Count * 20));
            for (int i = 0; i < _history.Count; i++)
            {
                GUI.Label(new Rect(0, i * 20, contentRect.width - 20, 20), _history[i], _textStyle);
            }
            GUI.EndScrollView();

            // Input field
            Rect inputRect = new Rect(x + 10, y + height - 35, width - 20, 25);
            GUI.color = Color.white;
            _input = GUI.TextField(inputRect, _input);

            // Auto-focus
            GUI.FocusControl(null);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (!string.IsNullOrWhiteSpace(_input))
                {
                    ExecuteCommand(_input);
                    _history.Add(_input);
                    _historyIndex = -1;
                    _input = "";
                }
                Event.current.Use();
            }
        }

        private void ExecuteCommand(string command)
        {
            var tokens = command.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            var cmd = tokens[0].ToLower();
            try
            {
                switch (cmd)
                {
                    case "unlockall":
                        UnlockAll();
                        AddLog("All content unlocked!");
                        break;

                    case "addgold":
                        if (tokens.Length < 2 || !int.TryParse(tokens[1], out int gold))
                            AddLog("Usage: addGold N");
                        else
                        {
                            var economy = Economy.Instance;
                            if (economy != null)
                            {
                                economy.AddGold(gold);
                                AddLog($"Added {gold} coins");
                            }
                        }
                        break;

                    case "goto":
                        if (tokens.Length < 2)
                            AddLog("Usage: goto LEVELID");
                        else
                            GotoLevel(tokens[1]);
                        break;

                    case "killall":
                        KillAllEnemies();
                        AddLog("All enemies killed!");
                        break;

                    case "sethp":
                        if (tokens.Length < 2 || !int.TryParse(tokens[1], out int hp))
                            AddLog("Usage: setHP N");
                        else
                            AddLog("Castle HP setting deferred to future version");
                        break;

                    case "spawn":
                        if (tokens.Length < 2)
                            AddLog("Usage: spawn ENEMYTYPE");
                        else
                            AddLog("Spawn deferred — use level static enemies feature");
                        break;

                    case "give":
                        if (tokens.Length < 2)
                            AddLog("Usage: give SKINNAME");
                        else
                            AddLog("Give skin deferred");
                        break;

                    case "winlevel":
                        WinLevel();
                        AddLog("Level won!");
                        break;

                    case "loselevel":
                        LoseLevel();
                        AddLog("Level lost!");
                        break;

                    case "help":
                        AddLog("Commands: unlockAll, addGold N, goto LEVELID, killAll, setHP N, spawn TYPE, give SKIN, winLevel, loseLevel");
                        break;

                    default:
                        AddLog($"Unknown command: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error: {ex.Message}");
            }
        }

        private void UnlockAll()
        {
            // CampaignDataManager removed — unlock logic deferred to progression system integration
            AddLog("unlockAll deferred — requires SaveSystem progression integration");
        }

        private void GotoLevel(string levelId)
        {
            // Load level by ID (deferred to campaign system)
            AddLog($"Goto {levelId} — deferred to level loader integration");
        }

        private void KillAllEnemies()
        {
            var runner = LevelRunner.Instance;
            if (runner == null) return;

            var pool = runner.GetComponent<EnemyPool>();
            if (pool != null)
            {
                int count = pool.ActiveCount;
                var enemies = new List<Enemy>(pool.ActiveEnemies);
                foreach (var enemy in enemies)
                    enemy.TakeDamage(enemy.MaxHp + 1);
                AddLog($"Killed {count} enemies");
            }
        }

        private void WinLevel()
        {
            var runner = LevelRunner.Instance;
            if (runner != null)
            {
                // Trigger win state
                AddLog("winLevel deferred to level result integration");
            }
        }

        private void LoseLevel()
        {
            var runner = LevelRunner.Instance;
            if (runner != null)
            {
                // Trigger loss state
                AddLog("loseLevel deferred to level result integration");
            }
        }

        private void AddLog(string msg)
        {
            _history.Add($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
            _scrollPos.y = float.MaxValue;
        }
    }
}
#endif
