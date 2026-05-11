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
        private static Shader? _toonSnowShader;

        public static Shader GetLitShader()
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _litShader!;
        }

        // Port de ToonMaterial.js : cel-shading 3 steps shadow/mid/bright
        // Résout d'abord le nouveau Toon/Lit, fallback vers l'ancien CrowdDefense/ToonCelShading
        public static Shader GetToonShader()
        {
            if (_toonShader == null)
                _toonShader = Shader.Find("CrowdDefense/Toon/Lit")
                           ?? Shader.Find("CrowdDefense/ToonCelShading")
                           ?? Shader.Find("Standard");
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

        public static Shader GetToonSnowShader()
        {
            if (_toonSnowShader == null)
                _toonSnowShader = Shader.Find("CrowdDefense/Toon/Snow") ?? GetLitShader();
            return _toonSnowShader!;
        }

        // Réinitialise le cache (hot-reload Editor)
        public static void ResetCache()
        {
            _litShader = null;
            _toonShader = null;
            _toonWaterShader = null;
            _toonLavaShader = null;
            _toonSnowShader = null;
        }
    }
}
