#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public static class ShaderUtil
    {
        private static Shader? _litShader;
        private static Shader? _toonShader;
        private static Shader? _toonWaterShader;
        private static Shader? _toonLavaShader;

        public static Shader GetLitShader()
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _litShader!;
        }

        // Port de ToonMaterial.js : cel-shading 3 steps shadow/mid/bright
        // Résout d'abord le nouveau Toon/Lit, fallback vers l'ancien CrowdDefense/ToonCelShading.
        // R2-recovery : Standard shader renders MAGENTA on URP — fallback URP/Lit instead.
        public static Shader GetToonShader()
        {
            if (_toonShader == null)
                _toonShader = Shader.Find("CrowdDefense/Toon/Lit")
                           ?? Shader.Find("CrowdDefense/ToonCelShading")
                           ?? GetLitShader();
            return _toonShader!;
        }

        public static Shader GetToonWaterShader()
        {
            if (_toonWaterShader == null)
                _toonWaterShader = Shader.Find("CrowdDefense/Toon/Water") ?? GetLitShader();
            return _toonWaterShader!;
        }

        public static Shader GetToonLavaShader()
        {
            if (_toonLavaShader == null)
                _toonLavaShader = Shader.Find("CrowdDefense/Toon/Lava") ?? GetLitShader();
            return _toonLavaShader!;
        }
    }
}
