#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Hero : MonoBehaviour
    {
        // ── Aura pulse ────────────────────────────────────────────────────────
        private float _heroPulseT;
        private Renderer? _auraRenderer;
        private Renderer? _haloRenderer;

        // ── Animator ──────────────────────────────────────────────────────────
        private Animator? _animator;

        // ── Perk icons (world-space quads around head) ────────────────────────
        private readonly GameObject?[] _perkIcons = new GameObject?[6];

        private void UpdateAuraPulse(float dt)
        {
            if (_auraRenderer == null) return;
            _heroPulseT += dt;
            float pulse = 0.5f + 0.25f * Mathf.Sin(_heroPulseT * 2.5f);
            var c = _auraRenderer.material.color;
            c.a = pulse * (cfg?.AuraColor.a ?? 0.5f);
            _auraRenderer.material.color = c;
        }

        private void UpdateAttackAnimTimer(float dt)
        {
            if (_attackAnimTimer <= 0f) return;
            _attackAnimTimer -= dt;
            if (_attackAnimTimer <= 0f && _animator != null)
                AnimationController.SetWalking(_animator, _smoothedMoveDir.sqrMagnitude > 0.01f);
        }

        private void UpdatePerkIconsBillboard()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rot = cam.transform.rotation;
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var icon = _perkIcons[i];
                if (icon != null && icon.activeSelf) icon.transform.rotation = rot;
            }
        }

        // ── Mesh spawn ────────────────────────────────────────────────────────
        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;
            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
                Debug.LogError("[Hero] AssetRegistry not found — check Resources/AssetRegistry.asset exists");
                BuildFallbackMesh();
                return null;
            }
            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
                Debug.LogError($"[Hero] GLTF prefab MISSING for assetKey='{assetKey}' — assign prefab in AssetRegistry");
                BuildFallbackMesh();
                return null;
            }
            var inst = Object.Instantiate(prefab, transform);
            inst.name = "Mesh_" + assetKey;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;
            SetupCapeCloth(inst);
            return inst;
        }

        private static void SetupCapeCloth(GameObject meshRoot)
        {
            foreach (Transform t in meshRoot.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (!n.Contains("cape") && !n.Contains("cloak")) continue;
                var smr = t.GetComponent<SkinnedMeshRenderer>();
                if (smr == null) continue;

                var cloth = t.gameObject.AddComponent<Cloth>();
                cloth.useGravity        = true;
                cloth.damping           = 0.1f;
                cloth.stretchingStiffness = 0.5f;
                cloth.bendingStiffness    = 0.1f;

                var spheres = new ClothSphereColliderPair[5];
                var root = meshRoot.transform;
                float[] offsets = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
                for (int i = 0; i < 5; i++)
                {
                    var col = new GameObject($"CapeCollider_{i}").AddComponent<SphereCollider>();
                    col.transform.SetParent(root);
                    col.transform.localPosition = new Vector3(0f, offsets[i] * 1.4f + 0.3f, 0f);
                    col.radius = 0.18f;
                    spheres[i] = new ClothSphereColliderPair(col, null);
                }
                cloth.sphereColliders = spheres;
            }
        }

        private void BuildFallbackMesh()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale    = new Vector3(0.55f, 0.85f, 0.5f);
            var bodyRend = body.GetComponent<MeshRenderer>();
            if (bodyRend != null)
            {
                var bodyMat = new Material(ShaderUtil.GetUnlitShader());
                var bodyColor = new Color(0f, 1f, 0f);
                if (bodyMat.HasProperty("_BaseColor")) bodyMat.SetColor("_BaseColor", bodyColor);
                else if (bodyMat.HasProperty("_Color")) bodyMat.SetColor("_Color", bodyColor);
                bodyRend.material = bodyMat;
            }
            Object.Destroy(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(transform);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale    = new Vector3(0.38f, 0.35f, 0.38f);
            var headRend = head.GetComponent<MeshRenderer>();
            if (headRend != null)
            {
                var headMat = new Material(ShaderUtil.GetUnlitShader());
                var headColor = new Color(0f, 0.8f, 0f);
                if (headMat.HasProperty("_BaseColor")) headMat.SetColor("_BaseColor", headColor);
                else if (headMat.HasProperty("_Color")) headMat.SetColor("_Color", headColor);
                headRend.material = headMat;
            }
            Object.Destroy(head.GetComponent<Collider>());
        }

        // ── Perk icons world-space ────────────────────────────────────────────
        private void BuildPerkIcons()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"PerkIcon{i}";
                go.transform.SetParent(transform);
                go.transform.localScale = Vector3.one * 0.2f;
                Object.Destroy(go.GetComponent<Collider>());
                var mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard")!) { color = Color.white };
                go.GetComponent<MeshRenderer>().material = mat;
                go.SetActive(false);
                _perkIcons[i] = go;
            }
        }

        private void UpdatePerkIcons()
        {
            int count = Mathf.Min(_perks.Count, _perkIcons.Length);
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var icon = _perkIcons[i];
                if (icon == null) continue;
                icon.SetActive(i < count);
                if (i >= count) continue;
                icon.GetComponent<MeshRenderer>().material.color = PerkColor(_perks[i]);
                float angle = i * (360f / Mathf.Max(count, 1)) * Mathf.Deg2Rad;
                icon.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.35f, 1.8f, Mathf.Sin(angle) * 0.35f);
            }
        }

        private static Color PerkColor(string perkId) => perkId switch
        {
            var s when s.Contains("damage") || s.Contains("crit") || s.Contains("fireball")
                    || s.Contains("multi") || s.Contains("pierce") || s.Contains("lightning")
                    || s.Contains("combustion") || s.Contains("pyromancie") => new Color(1f, 0.2f, 0.2f),
            var s when s.Contains("defense") || s.Contains("forteresse") || s.Contains("murs")
                    || s.Contains("cristal") || s.Contains("castle") => new Color(0.3f, 0.5f, 1f),
            var s when s.Contains("heal") || s.Contains("lifesteal") || s.Contains("regen")
                    || s.Contains("wave_regen") => new Color(0.2f, 0.9f, 0.3f),
            var s when s.Contains("speed") || s.Contains("move") || s.Contains("fire_rate") => new Color(1f, 0.9f, 0.1f),
            var s when s.Contains("coin") || s.Contains("xp") || s.Contains("tower") => new Color(1f, 0.6f, 0.1f),
            _ => new Color(0.9f, 0.3f, 1f),
        };

        // ── Ground aura + halo decals ─────────────────────────────────────────
        private void BuildAuraDecals()
        {
            if (cfg == null) return;

            var auraGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            auraGo.name = "HeroAura";
            auraGo.transform.SetParent(transform);
            auraGo.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            auraGo.transform.localScale    = new Vector3(0.675f * 2f, 0.01f, 0.675f * 2f);
            Object.Destroy(auraGo.GetComponent<Collider>());
            var c = cfg.AuraColor;
            var auraMat = new Material(ShaderUtil.GetUnlitShader());
            if (auraMat.HasProperty("_BaseColor")) auraMat.SetColor("_BaseColor", c);
            else if (auraMat.HasProperty("_Color")) auraMat.SetColor("_Color", c);
            SetMaterialTransparent(auraMat, c.a);
            _auraRenderer = auraGo.GetComponent<Renderer>();
            if (_auraRenderer != null) _auraRenderer.material = auraMat;

            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloGo.name = "HeroHalo";
            haloGo.transform.SetParent(transform);
            haloGo.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            haloGo.transform.localScale    = new Vector3(1.2f * 2f, 0.01f, 1.2f * 2f);
            Object.Destroy(haloGo.GetComponent<Collider>());
            var h = cfg.HaloColor;
            var haloMat = new Material(ShaderUtil.GetUnlitShader());
            if (haloMat.HasProperty("_BaseColor")) haloMat.SetColor("_BaseColor", h);
            else if (haloMat.HasProperty("_Color")) haloMat.SetColor("_Color", h);
            SetMaterialTransparent(haloMat, h.a);
            _haloRenderer = haloGo.GetComponent<Renderer>();
            if (_haloRenderer != null) _haloRenderer.material = haloMat;
        }

        private static void SetMaterialTransparent(Material mat, float alpha)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var c = mat.color;
            c.a = alpha;
            mat.color = c;
            mat.renderQueue = 3000;
        }

        public void ApplySkin(int tier)
        {
            bool hasTint = tier > 0;
            var tintColor = tier switch
            {
                1 => new Color(0.8f, 0.8f, 0.9f),
                2 => new Color(1f, 0.85f, 0.3f),
                _ => Color.white,
            };

            var block = new MaterialPropertyBlock();

            var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            foreach (var r in skinnedRenderers)
            {
                r.GetPropertyBlock(block);
                if (hasTint)
                {
                    block.SetColor("_BaseColor", tintColor);
                    block.SetColor("_Color", tintColor);
                    if (tier == 2)
                        block.SetColor("_EmissionColor", tintColor * 0.2f);
                }
                else
                {
                    block.Clear();
                }
                r.SetPropertyBlock(block);
            }

            var meshRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var r in meshRenderers)
            {
                r.GetPropertyBlock(block);
                if (hasTint)
                {
                    block.SetColor("_BaseColor", tintColor);
                    block.SetColor("_Color", tintColor);
                    if (tier == 2)
                        block.SetColor("_EmissionColor", tintColor * 0.2f);
                }
                else
                {
                    block.Clear();
                }
                r.SetPropertyBlock(block);
            }
        }

        private void ApplyTintFromPrefs(string heroId)
        {
            string hex = PlayerPrefs.GetString(UI.HeroPickScreen.TintPrefsKey(heroId), "");
            if (string.IsNullOrEmpty(hex)) return;
            if (!ColorUtility.TryParseHtmlString(hex, out var tint)) return;

            var block = new MaterialPropertyBlock();

            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                r.SetPropertyBlock(block);
            }
            foreach (var r in GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                r.SetPropertyBlock(block);
            }
        }

        public void ApplySkinVisual(string assetKey)
        {
            var toRemove = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var ch = transform.GetChild(i);
                if (ch.name.StartsWith("Mesh_")) toRemove.Add(ch);
            }
            foreach (var ch in toRemove) Destroy(ch.gameObject);

            _animator = null;
            var meshChild = SpawnMeshChild(assetKey);
            var toonRoot  = meshChild != null ? meshChild : gameObject;
            MaterialController.ApplyToon(toonRoot, cfg?.BodyColor ?? Color.white);
            Outline.ApplyToHierarchy(toonRoot.transform);
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", "Walk");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_animator != null && !AnimationController.ValidateAnimatorSetup(_animator, "Hero_Respec"))
                Debug.LogWarning("[Hero.Respec] Animator validation failed.");
#endif
        }

        public string GetDebugStats()
        {
            return $"Lv{Level} XP:{Xp}/{XpToNext} " +
                   $"dmg×{DamageMul:F2} rng×{RangeMul:F2} fr×{FireRateMul:F2} " +
                   $"ms:{MultiShot} pc:{PierceCount} crit:{CritChance:P0} " +
                   $"ult:{_ultCooldown:F1}s perks:{_perks.Count}";
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.2f, 0.35f);
        }

        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = new Color(1f, 0.84f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, cfg.Range * RangeMul);
            if (TowerAuraRange > 0f)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.15f);
                Gizmos.DrawWireSphere(transform.position, TowerAuraRange);
            }
        }
#endif
    }
}
