#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // R6-PARITY-011: 8 castle theme skins — option A (MaterialPropertyBlock color tints,
    // no texture import, no Material instance leak).
    // Subscribes to LevelEvents.OnLevelStart; applies a theme tint to all Castle renderers
    // except HpBar children (which manage their own color).
    [DefaultExecutionOrder(60)]
    public class CastleSkinController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        // 8 core theme tints (roof / stone mix): distinct enough to read at a glance.
        // Index mirrors LevelTheme enum order; Glacier mapped from Submarin, Marais from Apocalypse.
        private static readonly (LevelTheme theme, Color primary, Color secondary)[] ThemeSkins =
        {
            (LevelTheme.Plaine,     new Color(0.62f, 0.80f, 0.42f), new Color(0.80f, 0.78f, 0.70f)),
            (LevelTheme.Foret,      new Color(0.25f, 0.52f, 0.22f), new Color(0.58f, 0.44f, 0.28f)),
            (LevelTheme.Desert,     new Color(0.86f, 0.72f, 0.38f), new Color(0.72f, 0.56f, 0.30f)),
            (LevelTheme.Volcan,     new Color(0.78f, 0.22f, 0.06f), new Color(0.24f, 0.22f, 0.20f)),
            (LevelTheme.Apocalypse, new Color(0.46f, 0.18f, 0.18f), new Color(0.20f, 0.16f, 0.14f)),
            (LevelTheme.Espace,     new Color(0.14f, 0.28f, 0.58f), new Color(0.06f, 0.06f, 0.16f)),
            (LevelTheme.Submarin,   new Color(0.30f, 0.72f, 0.84f), new Color(0.16f, 0.44f, 0.60f)),
            (LevelTheme.Cyberpunk,  new Color(0.72f, 0.06f, 0.80f), new Color(0.10f, 0.06f, 0.18f)),
        };

        private MaterialPropertyBlock? _mpb;

        private void OnEnable()  => LevelEvents.OnLevelStart += OnLevelStart;
        private void OnDisable() => LevelEvents.OnLevelStart -= OnLevelStart;

        private void OnLevelStart(LevelData data, Bounds _)
        {
            var castle = Castle.Instance;
            if (castle == null) return;
            ApplyTheme(castle, data.LevelTheme);
        }

        // Called internally and can be called from tests / editor tooling.
        public void ApplyTheme(Castle castle, LevelTheme theme)
        {
            Color primary   = new Color(0.80f, 0.78f, 0.70f);
            Color secondary = new Color(0.60f, 0.58f, 0.52f);

            foreach (var (t, p, s) in ThemeSkins)
            {
                if (t == theme) { primary = p; secondary = s; break; }
            }

            _mpb ??= new MaterialPropertyBlock();

            var renderers = castle.GetComponentsInChildren<Renderer>(includeInactive: false);
            for (int i = 0; i < renderers.Length; i++)
            {
                var rend = renderers[i];
                // Skip HpBar and Text children
                if (rend.gameObject.name.StartsWith("CastleHPBar") ||
                    rend.gameObject.name.StartsWith("CastleHPText") ||
                    rend.gameObject.name.StartsWith("CastleGate"))
                    continue;

                // Alternate primary/secondary by renderer index for visual variety
                Color tint = (i % 2 == 0) ? primary : secondary;
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, tint);
                _mpb.SetColor(ColorId,     tint);
                rend.SetPropertyBlock(_mpb);
            }
        }
    }
}
