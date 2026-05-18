#nullable enable
using System.Collections.Generic;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Port de applyToonToScene() dans ToonMaterial.js (Three.js MeshToonMaterial).
    // Traverse récursif tous les Renderer du subtree, remplace chaque material
    // par une instance de ToonBase.mat avec le tint BaseColor de la config SO.
    public static class MaterialController
    {
        private static Material? _toonBase;
        private static Material? _jellyfish;
        private static Material? _hologram;
        private static Material? _bossJellyfish;
        private static Material? _bossHologram;

        // Cache toon material instances by (tint, transparent, sourceTexture) to avoid leaking
        // a new Material per enemy Init/pool-reuse. Key includes Texture? so textured meshes
        // also hit the cache rather than allocating on every spawn.
        private static readonly Dictionary<(Color, bool, Texture?), Material> _toonCache = new();

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
                    Texture? srcTex = srcMats[i]?.mainTexture;
                    newMats[i] = GetCachedToon(tint, transparent, srcTex);
                }
                r.sharedMaterials = newMats;
            }
        }

        /// <summary>
        /// Mise à jour du tint seul sur les materials déjà appliqués (upgrade L3, etc.)
        /// Route through le même cache que ApplyToon — évite alloc via r.materials.
        /// </summary>
        public static void UpdateTint(GameObject root, Color tint)
        {
            if (_toonBase == null)
                _toonBase = Resources.Load<Material>("ToonBase");
            if (_toonBase == null) return;

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var srcMats = r.sharedMaterials;
                var newMats = new Material[srcMats.Length];
                bool changed = false;
                for (int i = 0; i < srcMats.Length; i++)
                {
                    var src = srcMats[i];
                    if (src == null) { newMats[i] = null!; continue; }
                    Texture? srcTex = src.mainTexture;
                    var cached = GetCachedToon(tint, false, srcTex);
                    if (!ReferenceEquals(cached, src)) changed = true;
                    newMats[i] = cached;
                }
                if (changed)
                    r.sharedMaterials = newMats;
            }
        }

        private static Material GetCachedToon(Color tint, bool transparent, Texture? srcTex)
        {
            var key = (tint, transparent, srcTex);
            if (_toonCache.TryGetValue(key, out var m)) return m;

            m = new Material(_toonBase!);
            // N29: HasProperty guard for toon variants that don't expose _BaseColor
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", tint);

            // Conserve texture originale du mesh (port de opts.map dans ToonMaterial.js)
            if (srcTex != null)
                m.mainTexture = srcTex;

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
                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", c);
            }

            _toonCache[key] = m;
            return m;
        }

        // Ajoute un GO enfant portant un Renderer avec le shader overlay boss.
        // Séparé du toon mesh pour ne pas interférer avec ApplyTint/stealth.
        // overlayKey : "jellyfish" | "hologram" — tout autre value = no-op.
        public static void ApplyShaderOverlay(GameObject root, string overlayKey, Color tint)
        {
            Material? overlayMat = overlayKey switch
            {
                "jellyfish"     => LoadCached(ref _jellyfish,     "Jellyfish"),
                "hologram"      => LoadCached(ref _hologram,      "Hologram"),
                "bossjellyfish" => LoadCached(ref _bossJellyfish, "BossJellyfish"),
                "bosshologram"  => LoadCached(ref _bossHologram,  "BossHologram"),
                "phantom"       => LoadCached(ref _bossHologram,  "BossHologram"),
                _               => null
            };

            if (overlayMat == null) return;

            // Un seul child overlay par root — évite doublons sur pool reuse
            const string childName = "_ShaderOverlay";
            var existing = root.transform.Find(childName);
            if (existing != null) return;

            var child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child.name = childName;
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one * 1.05f; // enveloppe légèrement le mesh
            // Collider inutile — overlay visuel uniquement
            Object.Destroy(child.GetComponent<Collider>());

            var r = child.GetComponent<MeshRenderer>();
            if (r == null) return;
            var m = new Material(overlayMat);
            // Teinte l'overlay vers bodyColor pour cohérence visuelle
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, m.GetColor("_BaseColor").a));
            r.material = m;
        }

        /// <summary>
        /// Replaces all Renderer materials in the subtree with a single override material.
        /// Used by SkinSystem when a SkinDef provides an AlternateMaterial.
        /// </summary>
        public static void ApplyOverrideMaterial(GameObject root, Material mat)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        }

        private static Material? LoadCached(ref Material? cache, string resourceName)
        {
            if (cache == null)
                cache = Resources.Load<Material>(resourceName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cache == null)
                Debug.LogWarning($"[MaterialController] {resourceName}.mat introuvable dans Resources/");
#endif
            return cache;
        }
    }
}
