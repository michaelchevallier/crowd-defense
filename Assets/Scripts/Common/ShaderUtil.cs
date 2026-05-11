#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public static class ShaderUtil
    {
        private static Shader? _litShader;
        private static Shader? _toonShader;

        public static Shader GetLitShader()
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _litShader!;
        }

        // Port de ToonMaterial.js : shader cel-shading 3 steps shadow/mid/bright
        public static Shader GetToonShader()
        {
            if (_toonShader == null)
                _toonShader = Shader.Find("CrowdDefense/ToonCelShading") ?? Shader.Find("Standard");
            return _toonShader!;
        }
    }
}
