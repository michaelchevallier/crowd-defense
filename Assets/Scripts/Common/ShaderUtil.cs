#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public static class ShaderUtil
    {
        private static Shader? _litShader;

        public static Shader GetLitShader()
        {
            if (_litShader == null)
                _litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _litShader!;
        }
    }
}
