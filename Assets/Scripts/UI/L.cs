#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.UI
{
    public static class L
    {
        private static string _locale = "en";

        public static event Action? OnLocaleChanged;

        public static string CurrentLocale => _locale;

        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _fallback = new()
        {
            ["UI"] = new()
            {
                ["en"] = new()
                {
                    ["hud.gold_label"] = "GOLD",
                    ["hud.wave_label"] = "WAVE",
                    ["hud.hp_label"] = "HP",
                    ["hud.wave_launch"] = "Launch wave [N]",
                    ["hud.wave_launch_bonus"] = "Launch (+30c) [N]",
                    ["hud.wave_progress"] = "Wave {0} / {1}",
                    ["hud.pill_skip_text"] = "+30c  {0:F1}s  +{1}%",
                    ["hud.streak_text"] = "+{0}%",
                    ["overlay.game_over_title"] = "GAME OVER",
                    ["overlay.game_over_subtitle"] = "The castle has fallen.",
                    ["overlay.victory_title"] = "VICTORY",
                    ["overlay.victory_subtitle"] = "Level cleared.",
                    ["overlay.btn_restart"] = "Restart",
                    ["overlay.btn_retry"] = "Replay",
                    ["overlay.btn_menu"] = "Back to menu",
                    ["menu.level_select_title"] = "Select a level",
                    ["settings.title"] = "Settings",
                    ["settings.audio_section"] = "Audio",
                    ["settings.master"] = "Master",
                    ["settings.sfx"] = "SFX",
                    ["settings.music"] = "Music",
                    ["settings.mute"] = "Mute",
                    ["settings.gfx_section"] = "Graphics",
                    ["settings.quality"] = "Quality",
                    ["settings.quality_mobile"] = "Mobile",
                    ["settings.quality_desktop"] = "Desktop",
                    ["settings.quality_high"] = "High",
                    ["settings.vfx"] = "VFX",
                    ["settings.shake"] = "Camera shake",
                    ["settings.a11y_section"] = "Accessibility",
                    ["settings.colorblind"] = "Colorblind mode",
                    ["settings.reduce_motion"] = "Reduce motion",
                    ["settings.large_text"] = "Large text",
                    ["settings.lang_section"] = "Language",
                    ["settings.lang_label"] = "Locale",
                    ["settings.close"] = "Close",
                },
                ["fr"] = new()
                {
                    ["hud.gold_label"] = "OR",
                    ["hud.wave_label"] = "VAGUE",
                    ["hud.hp_label"] = "PV",
                    ["hud.wave_launch"] = "Lancer la vague [N]",
                    ["hud.wave_launch_bonus"] = "Lancer (+30c) [N]",
                    ["hud.wave_progress"] = "Vague {0} / {1}",
                    ["hud.pill_skip_text"] = "+30c  {0:F1}s  +{1}%",
                    ["hud.streak_text"] = "+{0}%",
                    ["overlay.game_over_title"] = "GAME OVER",
                    ["overlay.game_over_subtitle"] = "Le castle est tombé.",
                    ["overlay.victory_title"] = "VICTOIRE",
                    ["overlay.victory_subtitle"] = "Niveau terminé.",
                    ["overlay.btn_restart"] = "Recommencer",
                    ["overlay.btn_retry"] = "Rejouer",
                    ["overlay.btn_menu"] = "Retour au menu",
                    ["menu.level_select_title"] = "Choisir un niveau",
                    ["settings.title"] = "Paramètres",
                    ["settings.audio_section"] = "Audio",
                    ["settings.master"] = "Général",
                    ["settings.sfx"] = "Effets",
                    ["settings.music"] = "Musique",
                    ["settings.mute"] = "Muet",
                    ["settings.gfx_section"] = "Graphismes",
                    ["settings.quality"] = "Qualité",
                    ["settings.quality_mobile"] = "Mobile",
                    ["settings.quality_desktop"] = "Bureau",
                    ["settings.quality_high"] = "Élevé",
                    ["settings.vfx"] = "VFX",
                    ["settings.shake"] = "Tremblement caméra",
                    ["settings.a11y_section"] = "Accessibilité",
                    ["settings.colorblind"] = "Mode daltonien",
                    ["settings.reduce_motion"] = "Réduire le mouvement",
                    ["settings.large_text"] = "Texte large",
                    ["settings.lang_section"] = "Langue",
                    ["settings.lang_label"] = "Locale",
                    ["settings.close"] = "Fermer",
                },
            },
        };

        public static string Get(string key, string table = "UI")
        {
            if (_fallback.TryGetValue(table, out var tableDict) &&
                tableDict.TryGetValue(_locale, out var localeDict) &&
                localeDict.TryGetValue(key, out var value))
                return value;

            if (_fallback.TryGetValue(table, out var t2) &&
                t2.TryGetValue("en", out var enDict) &&
                enDict.TryGetValue(key, out var en))
                return en;

            return key;
        }

        public static string Get(string key, params object[] args)
        {
            string raw = Get(key);
            if (args == null || args.Length == 0) return raw;
            try { return string.Format(raw, args); }
            catch { return raw; }
        }

        public static void SetLocale(string code)
        {
            if (string.IsNullOrEmpty(code) || _locale == code) return;
            _locale = code;
            OnLocaleChanged?.Invoke();
        }
    }
}
