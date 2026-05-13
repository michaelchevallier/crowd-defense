#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Dispatch facade for VfxPool module builders.
    // Extracted: VfxPoolTextures, VfxPoolBuilders, SharedCurves (charter §1 cap: <500 LOC strict).
    internal static class VfxPoolBindings
    {
        internal static void LoadAllTextures() => VfxPoolTextures.LoadAllTextures();

        internal static void AttachSubEmitter(GameObject? parent, GameObject? smokeGo)
        {
            if (parent == null || smokeGo == null) return;
            var parentPs = parent.GetComponent<ParticleSystem>();
            if (parentPs == null) return;
            // Instantiate smoke prefab to avoid SetParent error on prefab assets
            smokeGo = Object.Instantiate(smokeGo);
            smokeGo.transform.SetParent(parent.transform, false);
            smokeGo.transform.localPosition = Vector3.zero;
            var sub = parentPs.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(smokeGo.GetComponent<ParticleSystem>(),
                ParticleSystemSubEmitterType.Death,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // Builders delegation
        internal static void BuildImpactModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildImpactModule(ps, mat);
        internal static void BuildDeathModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildDeathModule(ps, mat);
        internal static void BuildExplosionModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildExplosionModule(ps, mat);
        internal static void BuildExplosionSmokeModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildExplosionSmokeModule(ps, mat);
        internal static void BuildCoinBurstModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildCoinBurstModule(ps, mat);
        internal static void BuildHitFlashModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildHitFlashModule(ps, mat);
        internal static void BuildLevelUpModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildLevelUpModule(ps, mat);
        internal static void BuildPerkPickupModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildPerkPickupModule(ps, mat);
        internal static void BuildFrostModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildFrostModule(ps, mat);
        internal static void BuildPortalModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildPortalModule(ps, mat);
        internal static void BuildFireBreathModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildFireBreathModule(ps, mat);
        internal static void BuildMuzzleFlashModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildMuzzleFlashModule(ps, mat);
        internal static void BuildUpgradeBurstModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildUpgradeBurstModule(ps, mat);
        internal static void BuildSparkModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildSparkModule(ps, mat);
        internal static void BuildUpgradeConfettiModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders.BuildUpgradeConfettiModule(ps, mat);

        // New VFX builders (R6-PARITY-004-IMPL)
        internal static void BuildElectricCloudModule(ParticleSystem ps, Material? mat)  => VfxPoolBuilders2.BuildElectricCloudModule(ps, mat);
        internal static void BuildExplosionSmallModule(ParticleSystem ps, Material? mat) => VfxPoolBuilders2.BuildExplosionSmallModule(ps, mat);
        internal static void BuildGlyphDarkModule(ParticleSystem ps, Material? mat)      => VfxPoolBuilders2.BuildGlyphDarkModule(ps, mat);
        internal static void BuildHealAuraModule(ParticleSystem ps, Material? mat)       => VfxPoolBuilders2.BuildHealAuraModule(ps, mat);
        internal static void BuildLightningBoltModule(ParticleSystem ps, Material? mat)  => VfxPoolBuilders2.BuildLightningBoltModule(ps, mat);
        internal static void BuildPoisonCloudModule(ParticleSystem ps, Material? mat)    => VfxPoolBuilders2.BuildPoisonCloudModule(ps, mat);
        internal static void BuildShieldAuraModule(ParticleSystem ps, Material? mat)     => VfxPoolBuilders2.BuildShieldAuraModule(ps, mat);
        internal static void BuildSlowAuraModule(ParticleSystem ps, Material? mat)       => VfxPoolBuilders2.BuildSlowAuraModule(ps, mat);
        internal static void BuildSmokeGrayModule(ParticleSystem ps, Material? mat)      => VfxPoolBuilders2.BuildSmokeGrayModule(ps, mat);

        // Gradients
        internal static Gradient BuildConfettiGradient() => SharedCurves.BuildConfettiGradient();
        internal static Gradient BuildRainbowGradient() => SharedCurves.BuildRainbowGradient();

        // Material builder
        internal static Material BuildAdditiveMaterial() => SharedCurves.BuildAdditiveMaterial();
    }
}
