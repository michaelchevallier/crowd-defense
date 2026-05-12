#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Procedural ParticleSystem builders + texture wiring extracted from VfxPool (LOC cap).
    // Resources path: Resources/Textures/VFX/vfx_*.png loaded once at init.
    // Unity exploits:
    //   - TextureSheetAnimation cycling on all textured emitters
    //   - Sub-emitter smoke on Explosion (AttachSubEmitter)
    //   - Noise module turbulence on Explosion and FireBreath
    //   - Collision module (World) on Spark
    internal static class VfxPoolBindings
    {
        // ── Texture loading ───────────────────────────────────────────────────

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
            // Not yet mapped (no spawn method): electric_cloud, explosion_small, glyph_dark,
            // heal_aura, lightning_bolt, poison_cloud, shield_aura, slow_aura, smoke_gray.
        }

        private static void WireTexture(ParticleSystem ps, Material baseMat, Texture2D? tex)
        {
            if (tex == null) return;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr == null) return;
            psr.material = new Material(baseMat) { mainTexture = tex };
        }

        private static void EnableTextureSheetCycle(ParticleSystem ps)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled       = true;
            tsa.numTilesX     = 1;
            tsa.numTilesY     = 1;
            tsa.animation     = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 1f);
            tsa.cycleCount    = 1;
        }

        // ── Sub-emitter attachment ────────────────────────────────────────────

        internal static void AttachSubEmitter(GameObject? parent, GameObject? smokeGo)
        {
            if (parent == null || smokeGo == null) return;
            var parentPs = parent.GetComponent<ParticleSystem>();
            if (parentPs == null) return;
            smokeGo.transform.SetParent(parent.transform, false);
            smokeGo.transform.localPosition = Vector3.zero;
            var sub = parentPs.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(smokeGo.GetComponent<ParticleSystem>(),
                ParticleSystemSubEmitterType.Death,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // ── Module builders ───────────────────────────────────────────────────

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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleBlue); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texBloodSplat); EnableTextureSheetCycle(ps); }
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
            // Noise turbulence for realistic fire/smoke dispersal
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            noise.frequency   = 0.6f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
            noise.damping     = true;
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texExplosionBig); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) WireTexture(ps, mat, _texSmokeDark);
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texCoinPop); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleGold); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleGold); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texGemPop); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texFrostBurst); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texGlyphArcane); EnableTextureSheetCycle(ps); }
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
            // Noise turbulence for realistic fire dispersal
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            noise.frequency   = 0.8f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1.2f);
            noise.damping     = true;
            SetSizeOverLifetimeFade(ps);
            if (mat != null) WireTexture(ps, mat, _texFireBurst);
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleRed); EnableTextureSheetCycle(ps); }
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
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texGlyphHoly); EnableTextureSheetCycle(ps); }
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
            // Collision module — sparks bounce off world geometry
            var collision = ps.collision;
            collision.enabled      = true;
            collision.type         = ParticleSystemCollisionType.World;
            collision.mode         = ParticleSystemCollisionMode.Collision3D;
            collision.bounce       = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            collision.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.1f);
            collision.minKillSpeed = 0.05f;
            collision.radiusScale  = 0.5f;
            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleGold); EnableTextureSheetCycle(ps); }
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
            SetColorAlphaFade(ps);
            if (mat != null) { WireTexture(ps, mat, _texSparkleBlue); EnableTextureSheetCycle(ps); }
        }

        // ── Gradient builders (shared with VfxPool) ───────────────────────────

        internal static Gradient BuildConfettiGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.22f, 0.22f), 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.1f),  0.25f),
                    new GradientColorKey(new Color(0.2f, 0.85f, 0.3f), 0.5f),
                    new GradientColorKey(new Color(0.15f, 0.55f, 1f),  0.75f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 1f),   1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        internal static Gradient BuildRainbowGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(Color.HSVToRGB(0f,   0.85f, 1f), 0f),
                    new GradientColorKey(Color.HSVToRGB(0.2f, 0.85f, 1f), 0.2f),
                    new GradientColorKey(Color.HSVToRGB(0.4f, 0.85f, 1f), 0.4f),
                    new GradientColorKey(Color.HSVToRGB(0.6f, 0.85f, 1f), 0.6f),
                    new GradientColorKey(Color.HSVToRGB(0.8f, 0.85f, 1f), 0.8f),
                    new GradientColorKey(Color.HSVToRGB(1f,   0.85f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        // ── Shared curve helpers ──────────────────────────────────────────────

        internal static void SetSizeOverLifetimeFade(ParticleSystem ps)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f, 0f, -1.5f), new Keyframe(1f, 0.15f, -1.5f, 0f)));
        }

        internal static void SetColorAlphaFade(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        internal static Material BuildAdditiveMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Hidden/InternalErrorShader")!;
            var mat = new Material(shader) { name = "VfxParticle_Additive" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 3f);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_SrcBlend", 1);
            mat.SetInt("_DstBlend", 1);
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
