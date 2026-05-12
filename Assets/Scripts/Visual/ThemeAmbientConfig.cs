#nullable enable
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    [CreateAssetMenu(menuName = "CrowdDefense/Theme Ambient Config")]
    public class ThemeAmbientConfig : ScriptableObject
    {
        public LevelTheme theme;
        public Color skyColor = Color.white;
        public Color equatorColor = Color.gray;
        public Color groundColor = Color.black;
        [Range(0f, 2f)] public float intensity = 1f;
    }
}
