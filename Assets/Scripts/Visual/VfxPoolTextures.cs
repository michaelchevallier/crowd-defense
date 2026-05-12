#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Texture loading and ParticleSystem material wiring.
    // Resources path: Resources/Textures/VFX/vfx_*.png loaded once at init.
    internal static class VfxPoolTextures
    {
        private const string VfxTexPath = "Textures/VFX/";

        private static Texture2D? _texBloodSplat;
        private static Texture2D? _texCoinPop;
        private static Texture2D? _texExplosionBig;
        private static Texture2D? _texFireBurst;
        private static Texture2D? _texFrostBurst;
        private static Texture2D? _texGemPop;
        private static Texture2D? _texGlyphArcane;
        private static Texture2D? _texGlyphHoly;
        private static Texture2D? _texSparkleBlue;
        private static Texture2D? _texSparkleGold;
        private static Texture2D? _texSparkleRed;
        private static Texture2D? _texSmokeDark;
        // 9 previously unmapped textures (R6-PARITY-004-IMPL)
        private static Texture2D? _texElectricCloud;
        private static Texture2D? _texExplosionSmall;
        private static Texture2D? _texGlyphDark;
        private static Texture2D? _texHealAura;
        private static Texture2D? _texLightningBolt;
        private static Texture2D? _texPoisonCloud;
        private static Texture2D? _texShieldAura;
        private static Texture2D? _texSlowAura;
        private static Texture2D? _texSmokeGray;

        internal static void LoadAllTextures()
        {
            _texBloodSplat   = Resources.Load<Texture2D>(VfxTexPath + "vfx_blood_splat");
            _texCoinPop      = Resources.Load<Texture2D>(VfxTexPath + "vfx_coin_pop");
            _texExplosionBig = Resources.Load<Texture2D>(VfxTexPath + "vfx_explosion_big");
            _texFireBurst    = Resources.Load<Texture2D>(VfxTexPath + "vfx_fire_burst");
            _texFrostBurst   = Resources.Load<Texture2D>(VfxTexPath + "vfx_frost_burst");
            _texGemPop       = Resources.Load<Texture2D>(VfxTexPath + "vfx_gem_pop");
            _texGlyphArcane  = Resources.Load<Texture2D>(VfxTexPath + "vfx_glyph_arcane");
            _texGlyphHoly    = Resources.Load<Texture2D>(VfxTexPath + "vfx_glyph_holy");
            _texSparkleBlue  = Resources.Load<Texture2D>(VfxTexPath + "vfx_sparkle_blue");
            _texSparkleGold  = Resources.Load<Texture2D>(VfxTexPath + "vfx_sparkle_gold");
            _texSparkleRed   = Resources.Load<Texture2D>(VfxTexPath + "vfx_sparkle_red");
            _texSmokeDark    = Resources.Load<Texture2D>(VfxTexPath + "vfx_smoke_dark");
            _texElectricCloud = Resources.Load<Texture2D>(VfxTexPath + "vfx_electric_cloud");
            _texExplosionSmall = Resources.Load<Texture2D>(VfxTexPath + "vfx_explosion_small");
            _texGlyphDark    = Resources.Load<Texture2D>(VfxTexPath + "vfx_glyph_dark");
            _texHealAura     = Resources.Load<Texture2D>(VfxTexPath + "vfx_heal_aura");
            _texLightningBolt = Resources.Load<Texture2D>(VfxTexPath + "vfx_lightning_bolt");
            _texPoisonCloud  = Resources.Load<Texture2D>(VfxTexPath + "vfx_poison_cloud");
            _texShieldAura   = Resources.Load<Texture2D>(VfxTexPath + "vfx_shield_aura");
            _texSlowAura     = Resources.Load<Texture2D>(VfxTexPath + "vfx_slow_aura");
            _texSmokeGray    = Resources.Load<Texture2D>(VfxTexPath + "vfx_smoke_gray");
        }

        internal static void WireTexture(ParticleSystem ps, Material baseMat, Texture2D? tex)
        {
            if (tex == null) return;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr == null) return;
            psr.material = new Material(baseMat) { mainTexture = tex };
        }

        internal static void EnableTextureSheetCycle(ParticleSystem ps)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled       = true;
            tsa.numTilesX     = 1;
            tsa.numTilesY     = 1;
            tsa.animation     = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 1f);
            tsa.cycleCount    = 1;
        }

        // Texture references (internal, used by VfxPoolBuilders)
        internal static Texture2D? TexBloodSplat => _texBloodSplat;
        internal static Texture2D? TexCoinPop => _texCoinPop;
        internal static Texture2D? TexExplosionBig => _texExplosionBig;
        internal static Texture2D? TexFireBurst => _texFireBurst;
        internal static Texture2D? TexFrostBurst => _texFrostBurst;
        internal static Texture2D? TexGemPop => _texGemPop;
        internal static Texture2D? TexGlyphArcane => _texGlyphArcane;
        internal static Texture2D? TexGlyphHoly => _texGlyphHoly;
        internal static Texture2D? TexSparkleBlue => _texSparkleBlue;
        internal static Texture2D? TexSparkleGold => _texSparkleGold;
        internal static Texture2D? TexSparkleRed => _texSparkleRed;
        internal static Texture2D? TexSmokeDark => _texSmokeDark;
        internal static Texture2D? TexElectricCloud  => _texElectricCloud;
        internal static Texture2D? TexExplosionSmall => _texExplosionSmall;
        internal static Texture2D? TexGlyphDark      => _texGlyphDark;
        internal static Texture2D? TexHealAura        => _texHealAura;
        internal static Texture2D? TexLightningBolt  => _texLightningBolt;
        internal static Texture2D? TexPoisonCloud    => _texPoisonCloud;
        internal static Texture2D? TexShieldAura     => _texShieldAura;
        internal static Texture2D? TexSlowAura       => _texSlowAura;
        internal static Texture2D? TexSmokeGray      => _texSmokeGray;
    }
}
