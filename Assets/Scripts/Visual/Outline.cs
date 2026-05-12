#nullable enable
using UnityEngine;
using System.Collections.Generic;

namespace CrowdDefense.Visual
{
    // Port du pattern cellShadingOutlineColor() + inverted hull de ToonMaterial.js
    // Duplique chaque MeshFilter du subtree en GO enfant "Outline" scale 1.02,
    // material noir Cull Front (back-face only → silhouette noire autour du mesh).
    public static class Outline
    {
        private static Material? _outlineMat;
        private static readonly Dictionary<Color, Material> _outlineMatCache = new();

        /// <summary>
        /// Ajoute l'effet outline inverted hull à tout le subtree du root.
        /// Appeler après MaterialController.ApplyToon (outline pas tinté toon).
        /// </summary>
        public static void ApplyToHierarchy(Transform root, float scale = 1.02f, Color? color = null)
        {
            var outlineColor = color ?? Color.black;
            var mat = GetOrCreateMaterial(outlineColor);
            if (mat == null) return;

            // One outline GO per entity root only: find the root MeshFilter (or the first
            // MeshFilter in the subtree for GLTF hierarchies where the root has no MeshFilter).
            // Skipping all child MeshFilters avoids 1 outline GO per sub-mesh on multi-mesh GLTFs.
            var rootMf = root.GetComponent<MeshFilter>() ?? root.GetComponentInChildren<MeshFilter>(true);
            if (rootMf == null || rootMf.sharedMesh == null) return;
            if (rootMf.gameObject.name == "Outline") return;

            BuildOutlineGO(rootMf, mat, scale);
        }

        private static void BuildOutlineGO(MeshFilter sourceMf, Material mat, float scale)
        {
            var go = new GameObject("Outline");
            go.transform.SetParent(sourceMf.transform, false);
            // Scale slightly larger: inverted hull protrudes beyond the source mesh
            go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // Share the mesh reference (no copy — read-only for outline)
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMf.sharedMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            // Outline must not cast shadows (it is a silhouette pass only)
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Material? GetOrCreateMaterial(Color outlineColor)
        {
            // Check cache first (per-color materials)
            if (_outlineMatCache.TryGetValue(outlineColor, out var cached))
                return cached;

            var shader = Shader.Find("CrowdDefense/OutlineInvertedHull");
            if (shader == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Outline] Shader 'CrowdDefense/OutlineInvertedHull' introuvable");
#endif
                return null;
            }

            var mat = new Material(shader);
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", 0.02f);
            _outlineMatCache[outlineColor] = mat;
            return mat;
        }
    }
}
