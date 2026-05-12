#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // R6-PARITY-011: 4 faction gold pickup VFX variants.
    // Adds SpawnCoinBurst(pos, CoinFaction) overload — tints the coin burst pool particle
    // with a faction color overlay (gold base + faction hue) via MaterialPropertyBlock-safe
    // ApplyTint path already in VfxPool.
    public enum CoinFaction
    {
        Nature,     // Foret/plaine — green-gold
        Feu,        // Volcan — red-gold
        Glace,      // Espace/submarin — blue-gold
        Ombre,      // Cyberpunk/apocalypse — purple-gold
    }

    public partial class VfxPool
    {
        // Faction tints: gold base blended with faction hue.
        private static readonly Color CoinTintNature = new Color(0.65f, 0.96f, 0.30f);  // lime-gold
        private static readonly Color CoinTintFeu    = new Color(1.00f, 0.38f, 0.10f);  // orange-red
        private static readonly Color CoinTintGlace  = new Color(0.30f, 0.80f, 1.00f);  // ice-blue
        private static readonly Color CoinTintOmbre  = new Color(0.78f, 0.26f, 1.00f);  // purple

        /// Faction-tinted coin burst (R6-PARITY-011 — 4 vfx gold pickup skins).
        public void SpawnCoinBurst(Vector3 pos, CoinFaction faction)
        {
            Color tint = faction switch
            {
                CoinFaction.Nature => CoinTintNature,
                CoinFaction.Feu    => CoinTintFeu,
                CoinFaction.Glace  => CoinTintGlace,
                CoinFaction.Ombre  => CoinTintOmbre,
                _                  => new Color(1f, 0.88f, 0.15f),
            };
            SpawnFrom(_coinBurstPool, pos, tint);
        }
    }
}
