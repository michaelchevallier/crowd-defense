#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Build methods for the 9 previously-unmapped VFX textures (R6-PARITY-004-IMPL).
    // Conventions: Circle/Sphere emitter shape for auras, noise turbulence for clouds.
    internal static class VfxPoolBuilders2
    {
        // electric_cloud → SpawnElectricImpact (shock tower attack, arc hit)
        internal static void BuildElectricCloudModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.20f, 0.35f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.28f);
            main.startColor      = new Color(0.5f, 0.85f, 1f);
            main.maxParticles    = 120;
            main.duration        = 0.12f;
            main.gravityModifier = 0f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.15f;
            // Noise — arcing electric jitter
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            noise.frequency = 1.5f;
            noise.damping   = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexElectricCloud); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // explosion_small → SpawnImpactSmall (small projectile / grenade hit)
        internal static void BuildExplosionSmallModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.30f);
            main.startColor      = new Color(1f, 0.70f, 0.20f);
            main.maxParticles    = 200;
            main.duration        = 0.12f;
            main.gravityModifier = 0.4f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 20, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.15f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexExplosionSmall); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // glyph_dark → SpawnGlyph (perk pickup dark / cursed variant)
        internal static void BuildGlyphDarkModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.40f, 0.70f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.30f);
            main.startColor      = new Color(0.55f, 0.15f, 0.80f);
            main.maxParticles    = 180;
            main.duration        = 0.15f;
            main.gravityModifier = -0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 18, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Hemisphere;
            shape.radius       = 0.2f;
            shape.rotation     = new Vector3(-90f, 0f, 0f);
            // TextureSheetAnimation cycling on glyph sprite sheet
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexGlyphDark); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // heal_aura → SpawnHealAura (hero heal proc, castle Regen tick)
        internal static void BuildHealAuraModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.60f, 1.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor      = new Color(0.2f, 1f, 0.4f);
            main.maxParticles    = 300;
            main.duration        = 0.6f;
            main.gravityModifier = -0.4f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.8f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexHealAura); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // lightning_bolt → SpawnLightningChain (L3 Archmage chain lightning jump)
        internal static void BuildLightningBoltModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            main.startColor      = new Color(0.7f, 0.9f, 1f);
            main.maxParticles    = 96;
            main.duration        = 0.08f;
            main.gravityModifier = 0f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.08f;
            // High-frequency noise = jittery arc look
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
            noise.frequency = 3.0f;
            noise.damping   = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexLightningBolt); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // poison_cloud → SpawnPoisonField (poison tower AoE DoT)
        internal static void BuildPoisonCloudModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startColor      = new Color(0.3f, 0.85f, 0.2f, 0.65f);
            main.maxParticles    = 300;
            main.duration        = 1.0f;
            main.gravityModifier = -0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 30, 1, 0.02f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 1f;
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            noise.frequency = 0.4f;
            noise.damping   = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexPoisonCloud); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // shield_aura → SpawnShieldAura (enemy shielded state)
        internal static void BuildShieldAuraModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.50f, 0.80f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor      = new Color(1f, 0.85f, 0.15f);
            main.maxParticles    = 200;
            main.duration        = 0.5f;
            main.gravityModifier = -0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 20, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.6f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexShieldAura); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // slow_aura → SpawnSlowField (slow tower AoE, magnet slow ring)
        internal static void BuildSlowAuraModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.80f, 1.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor      = new Color(0.4f, 0.6f, 1f, 0.75f);
            main.maxParticles    = 350;
            main.duration        = 0.8f;
            main.gravityModifier = 0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22, 28, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 1f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSlowAura); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        // smoke_gray → sub-emitter on explosion (light gray puff variant)
        internal static void BuildSmokeGrayModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.35f, 0.80f);
            main.startColor      = new Color(0.70f, 0.68f, 0.65f, 0.55f);
            main.maxParticles    = 60;
            main.duration        = 0.1f;
            main.gravityModifier = -0.18f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.25f;
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.4f);
            noise.frequency = 0.25f;
            noise.damping   = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSmokeGray);
        }
    }
}
