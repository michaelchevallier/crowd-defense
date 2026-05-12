#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        // ── Init ──────────────────────────────────────────────────────────────
        public void Init(EnemyType type, int assignedPathIdx = 0, float endlessMul = 1f)
        {
            cfg = type;
            _targetedByCount = 0;
            HideReticle();
            IsDead       = false;
            _dying       = false;
            currentSpeedMul   = 1f;
            StealthAlpha      = 1f;
            summonTimer       = 0f;
            blastTimer        = 0f;
            chargeTimer       = 0f;
            _chargeActive     = false;
            _chargeActiveTimer = 0f;
            _enragedSpeedMul  = 1f;
            _enragedSummonCdMul = 1f;
            _enragedSelfTriggered = false;
            _dmgTakenMul      = 1f;
            _dmgTakenMulUntil = 0f;
            _variantSpeedMul  = 1f;
            _regenPerSec      = 0f;
            _dmgReduction     = 0f;
            _hitFlashTimer    = 0f;
            _freezeUntilTime  = 0f;
            _frozenTinted     = false;
            _burnUntilTime    = 0f;
            _bossEncounteredPublished = false;
            _bossPhase        = 0;
            _bossPhaseEmission = Color.black;
            _apocPhase        = 0;
            StopEnrageVFX();
            StopEnrageRing();
            _enrageActive     = false;
            _minionsSummoned  = false;
            _invulUntilTime   = 0f;
            _summonHordePending = false;
            _summonHordeTime  = 0f;
            _damageMul        = 1f;
            _diffRewardMul    = 1f;
            _aoePulseTimer    = 0f;
            _dustTimer        = 0f;
            _fieryTimer       = 0f;
            _stepTimer        = 0f;
            _fireBreathTimer  = 0f;
            _fireBreathTelegraphActive = false;
            _chargeWindUpActive = false;
            _chargeWindUpTimer  = 0f;
            _apocImpSummonTimer = 0f;
            _apocImpSummonEndTime = 0f;
            _static           = false;
            _staticRotY       = 0f;
            _wasWalking       = false;
            _lastDamageDirection = Vector3.back;
            _isElite = false;
            _chaseHero = !type.IsBoss && Random.value < 0.1f;
            if (_popInCoroutine != null) { StopCoroutine(_popInCoroutine); _popInCoroutine = null; }

            // D1-04 mob pressure: scale HP and speed by world pressure
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            var pressure = BalanceConfig.Get().GetPressure(currentWorld);
            hp       = type.Hp * pressure.mobHpMul * endlessMul;
            maxHp    = hp;
            pressureSpeedMul = pressure.mobSpeedMul;

            shieldHp = type.ShieldHP * endlessMul;
            _damageMul     = endlessMul;
            _diffRewardMul = 1.0f;
            pathIdx  = assignedPathIdx;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            float typeScale  = type.Scale;
            _bossBaseScale   = typeScale;
            transform.localScale = Vector3.zero;
            _popInCoroutine  = StartCoroutine(SpawnPopIn(typeScale, type.IsBoss));

            rend      = GetComponent<MeshRenderer>();
            baseColor = type.BodyColor;

            // Check active skin before spawning GLTF mesh
            string assetKey  = type.AssetKey;
            Color  bodyColor = type.BodyColor;
            Material? skinMat = null;

            var activeSkin = SkinSystem.Instance?.GetActiveSkin(SkinTargetType.Enemy, type.Id);
            if (activeSkin != null)
            {
                if (activeSkin.AlternateGLTF != null)
                    assetKey = activeSkin.Id;
                if (activeSkin.AlternateMaterial != null)
                    skinMat = activeSkin.AlternateMaterial;
                if (activeSkin.UseBodyColorOverride)
                    bodyColor = activeSkin.BodyColorOverride;
            }

            _meshChild = activeSkin?.AlternateGLTF != null
                ? SpawnSkinMeshChild(activeSkin.AlternateGLTF)
                : SpawnMeshChild(assetKey);
            baseColor = bodyColor;

            // Cache renderers once — hot path (UpdateStealth + hit flash run every frame per enemy)
            var meshRoot = _meshChild != null ? _meshChild : gameObject;
            _cachedRenderers = meshRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb ??= new MaterialPropertyBlock();

            // Cel-shading toon material
            var toonRoot = meshRoot;
            if (skinMat != null)
                MaterialController.ApplyOverrideMaterial(toonRoot, skinMat);
            else
                MaterialController.ApplyToon(toonRoot, bodyColor, type.IsStealth);
            // If GLTF spawned, disable the root capsule MeshRenderer (keep collider)
            if (_meshChild != null && rend != null)
                rend.enabled = false;

            // Outline silhouette
            Outline.ApplyToHierarchy(toonRoot.transform);

            // Boss shader overlay (jellyfish / hologram)
            if (!string.IsNullOrEmpty(type.ShaderOverlay) && type.ShaderOverlay != "none")
                MaterialController.ApplyShaderOverlay(toonRoot, type.ShaderOverlay, type.BodyColor);

            // AssetVariants palette swap post-toon
            if (activeSkin != null && activeSkin.ThemeIndex >= 0)
                AssetVariants.ApplyThemeIndex(toonRoot, activeSkin.ThemeIndex);
            else if (activeSkin != null && activeSkin.UseBodyColorOverride)
                AssetVariants.ApplySkin(toonRoot, activeSkin);

            // Colorblind Deuteranopia palette swap (no-op when mode is off)
            Visual.ColorblindPalette.ApplyToGameObject(toonRoot);

            // Animations Mechanim: Idle + Walk via bool isWalking
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", type.WalkAnim);
            _hasSpeedParam = false;
            if (_animator != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Runtime validation: ensure animator is properly configured (controller loaded, state set, etc.)
                if (!AnimationController.ValidateAnimatorSetup(_animator, $"Enemy_{type.Id}"))
                    Debug.LogWarning($"[Enemy.Init] {type.Id} animator validation failed — may show T-pose or stuck animation.");
#endif
                foreach (var p in _animator.parameters)
                    if (p.name == "Speed") { _hasSpeedParam = true; break; }
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius    = 0.3f;
                col.height    = 1f;
            }

            // Reset shield halo
            if (shieldHalo != null)
                shieldHalo.SetActive(false);
            if (shieldHp > 0f)
            {
                if (shieldHalo == null)
                    BuildShieldHalo();
                else
                    shieldHalo.SetActive(true);
                VfxPool.Instance?.SpawnShieldAura(transform.position);
            }

            // Boss aura ring
            EnsureBossAura();

            // World-space HP bar
            BuildHpBar();
            BuildDebuffIcons();

            // Position + path setup
            pathManager = PathManager.Instance;
            if (type.IsFlyer)
            {
                if (Castle.Instance != null)
                    transform.position = new Vector3(transform.position.x, type.FlyHeight, transform.position.z);
                return;
            }

            if (pathManager == null || pathManager.Paths.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Enemy] No PathManager or no paths");
#endif
                ReleaseToPool();
                return;
            }
            if (pathManager.WaypointCountOnPath(pathIdx) < 2)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[Enemy] Path {pathIdx} too short");
#endif
                ReleaseToPool();
                return;
            }
            transform.position = pathManager.GetWaypointOnPath(pathIdx, 0) + Vector3.up * 0.5f;

            // Chase-hero tint: slight red overlay so player can spot the threat
            if (_chaseHero && _cachedRenderers != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                var red = new Color(1f, 0.35f, 0.35f);
                _mpb.SetColor(_baseColorId, red);
                _mpb.SetColor(_colorId,     red);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }

            if (type.IsBoss)
            {
                StartCoroutine(BossSpawnCinematic());
            }
        }
    }
}
