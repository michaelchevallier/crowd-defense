#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public static class ShaderUtil
    {
        private static Shader? _litShader;
        private static Shader? _unlitShader;
        private static Shader? _toonShader;
        private static Shader? _toonWaterShader;
        private static Shader? _toonLavaShader;

        public static Shader GetLitShader()
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            return _litShader!;
        }

        // Lighting-independent URP shader — renders _BaseColor × _BaseMap regardless of lights.
        // Preferred for map slabs so tiles are always visible even with zero ambient or bad light setup.
        public static Shader GetUnlitShader()
        {
            if (_unlitShader == null)
                _unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? GetLitShader();
            return _unlitShader!;
        }

        // Port de ToonMaterial.js : cel-shading 3 steps shadow/mid/bright
        // Résout d'abord le nouveau Toon/Lit, fallback vers l'ancien CrowdDefense/ToonCelShading.
        // H-SHADERS : fallback URP/Unlit (not URP/Lit) so tiles render without ambient light setup.
        // Toon custom shaders exist at Assets/Shaders/Toon/ and should resolve normally; Unlit is
        // a safety net in case URP 17.3.0 fails to compile them (path tiles + water would go dark).
        public static Shader GetToonShader()
        {
            if (_toonShader == null)
                _toonShader = Shader.Find("CrowdDefense/Toon/Lit")
                           ?? Shader.Find("CrowdDefense/ToonCelShading")
                           ?? GetUnlitShader();
            return _toonShader!;
        }

        public static Shader GetToonWaterShader()
        {
            if (_toonWaterShader == null)
                _toonWaterShader = Shader.Find("CrowdDefense/Toon/Water") ?? GetUnlitShader();
            return _toonWaterShader!;
        }

        public static Shader GetToonLavaShader()
        {
            if (_toonLavaShader == null)
                _toonLavaShader = Shader.Find("CrowdDefense/Toon/Lava") ?? GetUnlitShader();
            return _toonLavaShader!;
        }
    }
}
