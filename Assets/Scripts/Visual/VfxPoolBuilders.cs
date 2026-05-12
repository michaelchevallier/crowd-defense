#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // ParticleSystem module builders for VfxPool.
    // All methods configure particle system modules procedurally (no prefabs).
    internal static class VfxPoolBuilders
    {
        internal static void BuildImpactModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor      = Color.white;
            main.maxParticles    = 160;
            main.duration        = 0.1f;
            main.gravityModifier = 0.6f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.1f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleBlue); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildDeathModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3.5f, 8f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startColor      = Color.white;
            main.maxParticles    = 280;
            main.duration        = 0.1f;
            main.gravityModifier = 1.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16, 28, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexBloodSplat); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildExplosionModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startColor      = new Color(1f, 0.65f, 0.15f);
            main.maxParticles    = 400;
            main.duration        = 0.15f;
            main.gravityModifier = 0.3f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 50, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.4f;
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            noise.frequency   = 0.6f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
            noise.damping     = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexExplosionBig); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildExplosionSmokeModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor      = new Color(0.3f, 0.25f, 0.2f, 0.6f);
            main.maxParticles    = 80;
            main.duration        = 0.1f;
            main.gravityModifier = -0.15f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.3f;
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.5f);
            noise.frequency = 0.3f;
            noise.damping   = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSmokeDark);
        }

        internal static void BuildCoinBurstModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
            main.startColor      = new Color(1f, 0.88f, 0.15f);
            main.maxParticles    = 160;
            main.duration        = 0.1f;
            main.gravityModifier = 2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Hemisphere;
            shape.radius       = 0.1f;
            shape.rotation     = new Vector3(-90f, 0f, 0f);
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexCoinPop); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildHitFlashModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor      = Color.white;
            main.maxParticles    = 100;
            main.duration        = 0.05f;
            main.gravityModifier = 0f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleGold); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildLevelUpModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 11f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.14f, 0.48f);
            main.startColor      = new Color(1f, 0.84f, 0f);
            main.maxParticles    = 600;
            main.duration        = 0.2f;
            main.gravityModifier = -0.5f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50, 70, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.3f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleGold); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildPerkPickupModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor      = Color.white;
            main.maxParticles    = 200;
            main.duration        = 0.1f;
            main.gravityModifier = 0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 18, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Hemisphere;
            shape.radius       = 0.2f;
            shape.rotation     = new Vector3(-90f, 0f, 0f);
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexGemPop); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildFrostModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 1.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor      = new Color(0.5f, 0.9f, 1f);
            main.maxParticles    = 400;
            main.duration        = 0.8f;
            main.gravityModifier = 0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 30, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Circle;
            shape.radius       = 1f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexFrostBurst); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildPortalModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor      = new Color(0.4f, 0.2f, 0.6f);
            main.maxParticles    = 320;
            main.duration        = 0.5f;
            main.gravityModifier = -0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 25, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled     = true;
            shape.shapeType   = ParticleSystemShapeType.Donut;
            shape.radius      = 0.7f;
            shape.donutRadius = 0.15f;
            shape.rotation    = new Vector3(90f, 0f, 0f);
            var vol = ps.velocityOverLifetime;
            vol.enabled        = true;
            vol.space          = ParticleSystemSimulationSpace.Local;
            vol.orbitalY       = new ParticleSystem.MinMaxCurve(Mathf.PI);
            vol.orbitalOffsetY = new ParticleSystem.MinMaxCurve(0f);
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexGlyphArcane); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildFireBreathModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.7f, 1.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor      = new Color(1f, 0.5f, 0.1f);
            main.maxParticles    = 400;
            main.duration        = 0.7f;
            main.gravityModifier = 0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 200f;
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 20f;
            shape.length          = 8f;
            shape.radius          = 0.15f;
            shape.radiusThickness = 1f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f),    0f),
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 0.55f),
                    new GradientColorKey(new Color(0.2f, 0.1f, 0.05f), 1f),
                },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            noise.frequency   = 0.8f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.2f);
            noise.damping     = true;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            if (mat != null) VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexFireBurst);
        }

        internal static void BuildMuzzleFlashModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.10f, 0.15f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startColor      = new Color(1f, 0.55f, 0.05f);
            main.maxParticles    = 128;
            main.duration        = 0.05f;
            main.gravityModifier = 0.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 8, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 15f;
            shape.radius    = 0.05f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleRed); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildUpgradeBurstModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor      = new Color(0.2f, 0.9f, 1f);
            main.maxParticles    = 60;
            main.duration        = 0.1f;
            main.gravityModifier = -0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 35, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.25f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexGlyphHoly); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildSparkModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor      = Color.white;
            main.maxParticles    = 64;
            main.duration        = 0.05f;
            main.gravityModifier = 0.4f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 8, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 45f;
            shape.radius          = 0.05f;
            shape.radiusThickness = 1f;
            var collision = ps.collision;
            collision.enabled      = true;
            collision.type         = ParticleSystemCollisionType.World;
            collision.mode         = ParticleSystemCollisionMode.Collision3D;
            collision.bounce       = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            collision.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.1f);
            collision.minKillSpeed = 0.05f;
            collision.radiusScale  = 0.5f;
            SharedCurves.SetSizeOverLifetimeFade(ps);
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleGold); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }

        internal static void BuildUpgradeConfettiModule(ParticleSystem ps, Material? mat)
        {
            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.1f, 1.5f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor      = Color.white;
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startRotation3D = true;
            main.maxParticles    = 60;
            main.duration        = 0.1f;
            main.gravityModifier = 1.0f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 30, 1, 0.01f) });
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 35f;
            shape.radius          = 0.15f;
            shape.radiusThickness = 0.8f;
            var rotLife = ps.rotationOverLifetime;
            rotLife.enabled      = true;
            rotLife.separateAxes = true;
            rotLife.x = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
            rotLife.y = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
            rotLife.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.4f, 0f, -0.5f), new Keyframe(1f, 0f, -0.5f, 0f)));
            SharedCurves.SetColorAlphaFade(ps);
            if (mat != null) { VfxPoolTextures.WireTexture(ps, mat, VfxPoolTextures.TexSparkleBlue); VfxPoolTextures.EnableTextureSheetCycle(ps); }
        }
    }
}
