#nullable enable

namespace CrowdDefense.Data
{
    public enum LevelTheme
    {
        Plaine,
        Foret,
        Desert,
        Volcan,
        Apocalypse,
        Espace,
        Submarin,
        Medieval,
        Cyberpunk,
        Foire,
    }

    // Maps the legacy string-based theme field to the typed enum.
    public static class LevelThemeExtensions
    {
        public static LevelTheme Parse(string raw) => raw.ToLowerInvariant() switch
        {
            "foret" or "forest" => LevelTheme.Foret,
            "desert" => LevelTheme.Desert,
            "volcan" or "volcano" => LevelTheme.Volcan,
            "apocalypse" => LevelTheme.Apocalypse,
            "espace" or "space" => LevelTheme.Espace,
            "submarin" or "underwater" => LevelTheme.Submarin,
            "medieval" => LevelTheme.Medieval,
            "cyberpunk" => LevelTheme.Cyberpunk,
            "foire" or "fair" => LevelTheme.Foire,
            _ => LevelTheme.Plaine,
        };
    }
}
