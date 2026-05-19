#nullable enable
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Visual
{
    // Shared gradient and animation curve helpers for VfxPool.
    internal static class SharedCurves
    {
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

        internal static Material BuildAdditiveMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? ShaderUtil.GetUnlitShader();
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
