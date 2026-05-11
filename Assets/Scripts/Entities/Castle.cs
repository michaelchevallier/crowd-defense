#nullable enable
using System;
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public class Castle : MonoSingleton<Castle>
    {
        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;

        public event Action<int, int>? OnHPChanged;
        public event Action<Castle>?   OnCastleDied;

        // World index used for no-regen threshold (D1-04)
        private int _world = 1;

        // Armor break — temporary damage taken multiplier (siege enemies / armor break effect)
        private float _dmgTakenMul        = 1f;
        private float _dmgTakenMulUntil   = 0f;

        // Damage state meshes (assign in Inspector; null = no swap)
        [SerializeField] private Mesh? _meshIntact;   // 100–66 %
        [SerializeField] private Mesh? _meshCracked;  // 66–33 %
        [SerializeField] private Mesh? _meshRuined;   // < 33 %

        private MeshFilter?   _meshFilter;
        private DamageStage   _currentStage = DamageStage.Intact;

        private enum DamageStage { Intact, Cracked, Ruined }

        // Visual state
        private bool          _smokeActive;
        private Coroutine?    _smokeCoroutine;
        private Coroutine?    _sparksCoroutine;
        private Light?        _dangerLight;
        private float         _dangerLightPhase;
        private bool          _grayscaleApplied;

        // VFX components (optional — assigned lazily, null = skip)
        private ParticleSystem? _smokePs;

        // HP bar (world-space canvas child)
        private Transform?    _hpBarFill;
        private float         _hpBarBaseScaleX;
        private TextMesh?     _hpText;

        public void Init(int hp, int world = 1)
        {
            HP = HPMax = hp;
            _world = world;
            _meshFilter = GetComponentInChildren<MeshFilter>();
            _smokePs    = GetComponentInChildren<ParticleSystem>();

            if (PathManager.Instance?.Grid != null)
            {
                var grid = PathManager.Instance.Grid;
                if (grid.Castles.Count > 0)
                {
                    var cell = grid.Castles[0];
                    transform.position = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
                }
            }

            BuildHpBar();
            OnHPChanged?.Invoke(HP, HPMax);
        }

        // ── HP bar ──────────────────────────────────────────────────────────────

        private void BuildHpBar()
        {
            // World-space quad children: background + fill.
            const float barW = 2.0f, barH = 0.18f, barY = 1.6f;

            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "CastleHPBar_BG";
            Destroy(bgGo.GetComponent<MeshCollider>());
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.localPosition = new Vector3(0f, barY, -0.05f);
            bgGo.transform.localScale    = new Vector3(barW, barH, 1f);
            var bgRend = bgGo.GetComponent<MeshRenderer>();
            bgRend.material = BuildUnlitMaterial(new Color(0f, 0f, 0f, 0.75f), transparent: true);
            bgRend.sortingOrder = 1;

            var fillGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillGo.name = "CastleHPBar_Fill";
            Destroy(fillGo.GetComponent<MeshCollider>());
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.localPosition = new Vector3(0f, barY, -0.06f);
            fillGo.transform.localScale    = new Vector3(barW * 0.94f, barH * 0.7f, 1f);
            _hpBarFill     = fillGo.transform;
            _hpBarBaseScaleX = barW * 0.94f;
            var fillRend = fillGo.GetComponent<MeshRenderer>();
            fillRend.material = BuildUnlitMaterial(new Color(0.27f, 0.87f, 0.27f, 1f), transparent: false);
            fillRend.sortingOrder = 2;

            // HP number text above the bar — "<hp> / <hpMax>" (port V5 _hpText sprite)
            var textGo = new GameObject("CastleHPText");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0f, barY + 0.4f, -0.06f);
            textGo.transform.localScale    = Vector3.one * 0.18f;
            _hpText = textGo.AddComponent<TextMesh>();
            _hpText.anchor    = TextAnchor.MiddleCenter;
            _hpText.alignment = TextAlignment.Center;
            _hpText.fontSize  = 64;
            _hpText.fontStyle = FontStyle.Bold;
            _hpText.color     = Color.white;
            _hpText.text      = $"{HP} / {HPMax}";

            RefreshHpBar();
        }

        private void RefreshHpBar()
        {
            if (_hpText != null) _hpText.text = $"{Mathf.Max(0, HP)} / {HPMax}";
            if (_hpBarFill == null) return;
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;
            ratio = Mathf.Clamp01(ratio);

            // Scale fill width and offset pivot so it shrinks from the right
            float newScaleX = _hpBarBaseScaleX * ratio;
            _hpBarFill.localScale = new Vector3(newScaleX, _hpBarFill.localScale.y, 1f);
            _hpBarFill.localPosition = new Vector3(
                (_hpBarBaseScaleX * (ratio - 1f)) / 2f,
                _hpBarFill.localPosition.y,
                _hpBarFill.localPosition.z);

            // Colour: green > 60 %, orange > 30 %, red otherwise
            var rend = _hpBarFill.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                Color fillColor = ratio > 0.6f
                    ? new Color(0.27f, 0.87f, 0.27f)
                    : ratio > 0.3f
                        ? new Color(1f, 0.67f, 0.13f)
                        : new Color(1f, 0.2f, 0.13f);
                rend.material.color = fillColor;
            }
        }

        private static Material BuildUnlitMaterial(Color color, bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader ?? Shader.Find("Standard")!);
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);   // URP Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            return mat;
        }

        // ── Damage / regen ──────────────────────────────────────────────────────

        // Armor break — boost incoming damage for a duration (ms). Caller picks max if already active.
        public void ApplyArmorBreak(float dmgTakenMul, int durMs)
        {
            if (dmgTakenMul <= 1f || durMs <= 0) return;
            _dmgTakenMul       = Mathf.Max(_dmgTakenMul, dmgTakenMul);
            float until        = Time.time + durMs / 1000f;
            _dmgTakenMulUntil  = Mathf.Max(_dmgTakenMulUntil, until);
        }

        public void TakeDamage(int dmg)
        {
            if (IsDead || dmg <= 0) return;

            int actualDmg = dmg;

            // Armor break — amplify incoming damage while active (siege enemies / armor break synergy)
            if (_dmgTakenMulUntil > 0f)
            {
                if (Time.time < _dmgTakenMulUntil)
                    actualDmg = Mathf.RoundToInt(dmg * _dmgTakenMul);
                else
                {
                    _dmgTakenMul      = 1f;
                    _dmgTakenMulUntil = 0f;
                }
            }

            HP = Mathf.Max(0, HP - actualDmg);
            // Flag interest bank — any hit resets bank for this wave (D1-01 §3.5)
            Economy.Instance?.FlagCastleDamaged();
            // D1-02: streak broken if castle leaks during the break window
            WaveManager.Instance?.NotifyCastleDamaged();
            OnHPChanged?.Invoke(HP, HPMax);

            RefreshHpBar();
            RefreshDamageMesh();
            UpdateTint();
            TriggerHitVfx();
            UpdateDamageVfxIntensity();

            AudioController.Instance?.Play("castle_hit", 0.65f);
            JuiceFX.Instance?.Shake(0.1f, 200);
            JuiceFX.Instance?.Flash(new Color(1f, 0.2f, 0.2f, 0.4f), 150);
            VfxPool.Instance?.SpawnHitFlash(transform);

            if (HP == 0)
            {
                ApplyGrayscale();
                OnCastleDied?.Invoke(this);
                AudioController.Instance?.Play("enemy_die_boss", 1f);
                JuiceFX.Instance?.SlowMo(0.2f, 1500);
                JuiceFX.Instance?.Flash(new Color(0f, 0f, 0f, 0.7f), 1000);
                VfxPool.Instance?.SpawnExplosion(transform.position, 4f);
#if UNITY_EDITOR
                Debug.Log("[Castle] destroyed");
#endif
            }
        }

        // D1-04: no-regen W6+ (BalanceConfig.NoRegenWorldThreshold)
        public void Regen(int amount)
        {
            if (IsDead || amount <= 0) return;
            var cfg = BalanceConfig.Get();
            if (_world >= cfg.NoRegenWorldThreshold) return;
            HP = Mathf.Min(HPMax, HP + amount);
            OnHPChanged?.Invoke(HP, HPMax);
            RefreshHpBar();
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnHeal(
                amount, transform.position + Vector3.up * 2f);
        }

        // Increase max HP (forteresse_perk / set bonus "pierre") — also heals by the delta.
        public void GrantBonusHP(int bonus)
        {
            if (bonus <= 0) return;
            HPMax += bonus;
            HP    += bonus;
            OnHPChanged?.Invoke(HP, HPMax);
            RefreshHpBar();
        }

        // ── Visual helpers ──────────────────────────────────────────────────────

        private void RefreshDamageMesh()
        {
            if (_meshFilter == null) return;
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;
            DamageStage stage = ratio > 0.66f ? DamageStage.Intact
                              : ratio > 0.33f ? DamageStage.Cracked
                              :                 DamageStage.Ruined;
            if (stage == _currentStage) return;
            _currentStage = stage;
            Mesh? next = stage switch
            {
                DamageStage.Cracked => _meshCracked,
                DamageStage.Ruined  => _meshRuined,
                _                   => _meshIntact,
            };
            if (next != null) _meshFilter.sharedMesh = next;
        }

        // Red tint on all child renderers when HP < 30 %
        private void UpdateTint()
        {
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;
            if (ratio >= 0.3f) return;
            var red = new Color(1f, 0f, 0f);
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                // Skip HP bar quads
                if (rend.gameObject.name.StartsWith("CastleHPBar")) continue;
                foreach (var mat in rend.materials)
                    mat.color = Color.Lerp(mat.color, red, 0.3f);
            }
        }

        // Progressive smoke + danger light intensity scaled to HP%
        private void UpdateDamageVfxIntensity()
        {
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;

            // ── ParticleSystem smoke ──────────────────────────────────────────
            if (_smokePs != null)
            {
                var emission = _smokePs.emission;
                if (ratio > 0.66f)
                {
                    emission.rateOverTime = 0f;
                    if (_smokePs.isPlaying) _smokePs.Stop();
                }
                else if (ratio > 0.33f)
                {
                    emission.rateOverTime = 8f;
                    if (!_smokePs.isPlaying) _smokePs.Play();
                }
                else
                {
                    emission.rateOverTime = 20f;
                    if (!_smokePs.isPlaying) _smokePs.Play();
                }
            }

            // ── Danger light ──────────────────────────────────────────────────
            if (_dangerLight == null) return;

            if (ratio > 0.66f)
            {
                _dangerLight.intensity = 0f;
            }
            else if (ratio > 0.33f)
            {
                _dangerLight.intensity = 1f;
                _dangerLight.color     = new Color(1f, 0.9f, 0.3f);
            }
            else
            {
                _dangerLight.intensity = 3f;
                _dangerLight.color     = new Color(1f, 0.3f, 0.1f);

                if (_sparksCoroutine == null)
                    _sparksCoroutine = StartCoroutine(SparksLoop());
            }
        }

        // Occasional spark burst every 2 s while HP < 33 %
        private IEnumerator SparksLoop()
        {
            var wait = new WaitForSeconds(2f);
            while (!IsDead && HPMax > 0 && (float)HP / HPMax < 0.33f)
            {
                var sparkColor = new Color(1f, 0.7f, 0.1f, 0.9f);
                for (int i = 0; i < 5; i++)
                {
                    var offset = new Vector3(
                        UnityEngine.Random.Range(-0.5f, 0.5f),
                        UnityEngine.Random.Range(1.5f, 3f),
                        UnityEngine.Random.Range(-0.5f, 0.5f));
                    VfxPool.Instance?.SpawnImpact(transform.position + offset, sparkColor);
                }
                yield return wait;
            }
            _sparksCoroutine = null;
        }

        // Smoke below 50 %, danger light below 20 %
        private void TriggerHitVfx()
        {
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;

            if (ratio < 0.5f && !_smokeActive)
            {
                _smokeActive = true;
                _smokeCoroutine = StartCoroutine(SmokeLoop());
            }

            if (ratio < 0.2f && _dangerLight == null)
            {
                var lightGo = new GameObject("CastleDangerLight");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 3f, 0f);
                _dangerLight = lightGo.AddComponent<Light>();
                _dangerLight.type      = LightType.Point;
                _dangerLight.color     = new Color(1f, 0.13f, 0.13f);
                _dangerLight.intensity = 2.5f;
                _dangerLight.range     = 8f;
                _dangerLightPhase      = 0f;
            }
        }

        // Continuous smoke particles every ~400 ms while below 50 % HP
        private IEnumerator SmokeLoop()
        {
            var wait = new WaitForSeconds(0.4f);
            while (!IsDead && _smokeActive)
            {
                var offset = new Vector3(
                    UnityEngine.Random.Range(-0.6f, 0.6f),
                    2.5f,
                    UnityEngine.Random.Range(-0.6f, 0.6f));
                VfxPool.Instance?.SpawnImpact(transform.position + offset, new Color(0.33f, 0.33f, 0.33f, 0.7f));
                yield return wait;
            }
            _smokeActive = false;
        }

        // Grayscale all child renderers on death
        private void ApplyGrayscale()
        {
            if (_grayscaleApplied) return;
            _grayscaleApplied = true;
            _smokeActive = false;
            if (_smokeCoroutine != null) { StopCoroutine(_smokeCoroutine); _smokeCoroutine = null; }
            if (_sparksCoroutine != null) { StopCoroutine(_sparksCoroutine); _sparksCoroutine = null; }
            if (_smokePs != null) _smokePs.Stop();
            if (_dangerLight != null) _dangerLight.intensity = 0f;

            var gray = new Color(0.53f, 0.53f, 0.53f);
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.name.StartsWith("CastleHPBar")) continue;
                foreach (var mat in rend.materials)
                    mat.color = gray;
            }
        }

        // ── MonoBehaviour ───────────────────────────────────────────────────────

        private void Update()
        {
            // Danger light flicker animation (mirrors V5 tick)
            if (_dangerLight == null) return;
            _dangerLightPhase += Time.deltaTime * 3.5f;
            _dangerLight.intensity = 1.5f + 1.5f * (Mathf.Sin(_dangerLightPhase) * 0.5f + 0.5f);
        }
    }
}
