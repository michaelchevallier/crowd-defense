#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Port de applyToonToScene() dans ToonMaterial.js (Three.js MeshToonMaterial).
    // Traverse récursif tous les Renderer du subtree, remplace chaque material
    // par une instance de ToonBase.mat avec le tint BaseColor de la config SO.
    public static class MaterialController
    {
        private static Material? _toonBase;

        // Réinitialiser la ref cached si nécessaire (tests / hot-reload Editor)
        public static void ResetCache() => _toonBase = null;

        /// <summary>
        /// Applique le shader ToonCelShading à tous les Renderer du subtree root.
        /// Conserve la texture mainTexture originale du material source si présente.
        /// </summary>
        public static void ApplyToon(GameObject root, Color tint, bool transparent = false)
        {
            if (_toonBase == null)
                _toonBase = Resources.Load<Material>("ToonBase");

            if (_toonBase == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[MaterialController] ToonBase.mat introuvable dans Resources/");
#endif
                return;
            }

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var srcMats = r.sharedMaterials;
                var newMats = new Material[srcMats.Length];
                for (int i = 0; i < srcMats.Length; i++)
                {
                    var m = new Material(_toonBase);
                    m.SetColor("_BaseColor", tint);

                    // Conserve texture originale du mesh (port de opts.map dans ToonMaterial.js)
                    var src = srcMats[i];
                    if (src != null && src.mainTexture != null)
                        m.mainTexture = src.mainTexture;

                    if (transparent)
                    {
                        // Stealth : transparent surface mode (cf Enemy stealth opacity)
                        m.SetFloat("_Surface", 1f);
                        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        m.SetFloat("_ZWrite", 0f);
                        m.renderQueue = 3000;
                        Color c = tint;
                        c.a = 0.45f; // stealth initial opacity
                        m.SetColor("_BaseColor", c);
                    }

                    newMats[i] = m;
                }
                r.materials = newMats;
            }
        }

        /// <summary>
        /// Mise à jour du tint seul sur les materials déjà appliqués (upgrade L3, etc.)
        /// </summary>
        public static void UpdateTint(GameObject root, Color tint)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", tint);
        }
    }
}
