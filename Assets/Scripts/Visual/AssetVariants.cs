#nullable enable
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Port de AssetVariants.js (V5) — palette swap procédural par thème/boss/skin.
    // Traverse tous les MeshRenderer du subtree et applique la palette de la SkinDef.
    public static class AssetVariants
    {
        // Palette thème — correspondance couleur/emissive par monde.
        // Référence: THEME_TINTS in AssetVariants.js
        private static readonly (Color body, Color emissive, float emissiveIntensity)[] ThemePalette =
        {
            (HexColor(0x6b8e4e), Color.black, 0f),   // 0 plaine
            (HexColor(0x4a7032), Color.black, 0f),   // 1 foret
            (HexColor(0xc8a34a), Color.black, 0f),   // 2 desert
            (HexColor(0xff4500), HexColor(0x331100), 0.4f), // 3 volcan
            (HexColor(0xff66cc), HexColor(0x440022), 0.2f), // 4 foire
            (HexColor(0x8a3030), HexColor(0x220000), 0.2f), // 5 apocalypse
            (HexColor(0x4a90e2), HexColor(0x001144), 0.5f), // 6 espace
            (HexColor(0x00cccc), HexColor(0x004444), 0.3f), // 7 submarin
            (HexColor(0x808080), Color.black, 0f),   // 8 medieval
            (HexColor(0xff00ff), HexColor(0x440044), 0.6f), // 9 cyberpunk
        };

        /// <summary>
        /// Applique une SkinDef au subtree d'un GameObject.
        /// SkinDef.ThemeIndex 0-9 sélectionne la palette dans ThemePalette.
        /// SkinDef.UseBodyColorOverride court-circuite la palette et utilise BodyColorOverride.
        /// </summary>
        public static void ApplySkin(GameObject root, SkinDef skin)
        {
            if (skin == null) return;

            if (skin.UseBodyColorOverride)
            {
                ApplyTint(root, skin.BodyColorOverride, Color.black, 0f);
                return;
            }

            int idx = skin.ThemeIndex;
            if (idx < 0 || idx >= ThemePalette.Length) return;

            var (body, emissive, intensity) = ThemePalette[idx];
            ApplyTint(root, body, emissive, intensity);
        }

        /// <summary>
        /// Applique une teinte thème directement par index (0-9).
        /// Utilisé par Tower.Init et Enemy.Init pour le thème du niveau courant.
        /// </summary>
        public static void ApplyThemeIndex(GameObject root, int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= ThemePalette.Length) return;
            var (body, emissive, intensity) = ThemePalette[themeIndex];
            ApplyTint(root, body, emissive, intensity);
        }

        private static void ApplyTint(GameObject root, Color body, Color emissive, float emissiveIntensity)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                var mats = r.materials; // instancie des copies — intentionnel (variante unique par entité)
                for (int j = 0; j < mats.Length; j++)
                {
                    var m = mats[j];
                    if (m == null) continue;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", body);
                    if (m.HasProperty("_Color"))     m.SetColor("_Color", body);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.SetColor("_EmissionColor", emissive * emissiveIntensity);
                        if (emissiveIntensity > 0f)
                            m.EnableKeyword("_EMISSION");
                        else
                            m.DisableKeyword("_EMISSION");
                    }
                }
                r.materials = mats;
            }
        }

        private static Color HexColor(uint hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8)  & 0xFF) / 255f;
            float b = ( hex        & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
