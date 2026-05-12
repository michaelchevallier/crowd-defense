#nullable enable
using System;
using UnityEngine;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Castle : MonoSingleton<Castle>
    {
        // Armor break — temporary damage taken multiplier (siege enemies / armor break effect)
        private float _dmgTakenMul        = 1f;
        private float _dmgTakenMulUntil   = 0f;

        // Damage state meshes (assign in Inspector; null = no swap)
        [SerializeField] private Mesh? _meshIntact;    // 100–66 %
        [SerializeField] private Mesh? _meshCracked;   // 66–33 %
        [SerializeField] private Mesh? _meshRuined;    // 33–15 %
        [SerializeField] private Mesh? _meshCritical;  // < 15 %

        private DamageStage   _currentStage = DamageStage.Intact;
        private enum DamageStage { Intact, Cracked, Ruined, Critical }

        // Overrun detection — 3+ hits in 3 s triggers red vignette + alert, 10 s cooldown
        private const int   OverrunHitThreshold  = 3;
        private const float OverrunWindowSec      = 3f;
        private const float OverrunCooldownSec    = 10f;
        private const float OverrunVignetteDurSec = 0.5f;
        private readonly System.Collections.Generic.Queue<float> _hitTimestamps = new();
        private float _overrunCooldownUntil = -1f;

        // HP bar (world-space canvas child)
        private Transform?    _hpBarFill;
        private float         _hpBarBaseScaleX;
        private TextMesh?     _hpText;

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
            WasHitThisWave = true;
            Economy.Instance?.FlagCastleDamaged();
            WaveManager.Instance?.NotifyCastleDamaged();
            OnHPChanged?.Invoke(HP, HPMax);

            RefreshHpBar();
            RefreshDamageMesh();
            UpdateTint();
            TriggerHitVfx();
            UpdateDamageVfxIntensity();
            UpdateCastleAura();

            AudioController.Instance?.Play3D("castle_hit", transform.position);
            VfxPool.Instance?.SpawnHitFlash(transform);
            CheckOverrun();

            EventManager.Instance?.Publish(new CastleHitEvent(actualDmg, HP));

            if (HP == 0)
            {
                ApplyGrayscale();
                OnCastleDied?.Invoke(this);
                AudioController.Instance?.Play("enemy_die_boss", 1f);
                JuiceFX.Instance?.SlowMo(0.2f, 1500);
                JuiceFX.Instance?.Flash(new Color(0f, 0f, 0f, 0.7f), 1000);
                VfxPool.Instance?.SpawnExplosion(transform.position, 4f);
                EventManager.Instance?.Publish(new CastleDestroyedEvent());
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
            UpdateCastleAura();
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnHeal(
                amount, transform.position + Vector3.up * 2f);
            VfxPool.Instance?.SpawnHealAura(transform.position);
        }

        // Increase max HP (forteresse_perk / set bonus "pierre") — also heals by the delta.
        public void GrantBonusHP(int bonus)
        {
            if (bonus <= 0) return;
            HPMax += bonus;
            HP    += bonus;
            OnHPChanged?.Invoke(HP, HPMax);
            RefreshHpBar();
            UpdateCastleAura();
        }

        private void BuildHpBar()
        {
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

            float newScaleX = _hpBarBaseScaleX * ratio;
            _hpBarFill.localScale = new Vector3(newScaleX, _hpBarFill.localScale.y, 1f);
            _hpBarFill.localPosition = new Vector3(
                (_hpBarBaseScaleX * (ratio - 1f)) / 2f,
                _hpBarFill.localPosition.y,
                _hpBarFill.localPosition.z);

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

        private void RefreshDamageMesh()
        {
            if (_meshFilter == null) return;
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;
            DamageStage stage = ratio > 0.66f ? DamageStage.Intact
                              : ratio > 0.33f ? DamageStage.Cracked
                              : ratio > 0.15f ? DamageStage.Ruined
                              :                 DamageStage.Critical;
            if (stage == _currentStage) return;
            _currentStage = stage;
            Mesh? next = stage switch
            {
                DamageStage.Cracked  => _meshCracked,
                DamageStage.Ruined   => _meshRuined,
                DamageStage.Critical => _meshCritical ?? _meshRuined,
                _                    => _meshIntact,
            };
            if (next != null) _meshFilter.sharedMesh = next;
        }

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId     = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock? _castleMpb;
        private Color     _currentCastleTint = Color.white;
        private Coroutine? _tintLerpCoroutine;

        private void UpdateTint()
        {
            float ratio  = HPMax > 0 ? (float)HP / HPMax : 0f;
            Color target = AssetVariants.GetCastleTint(ratio);
            if (target == _currentCastleTint) return;
            if (_tintLerpCoroutine != null) StopCoroutine(_tintLerpCoroutine);
            _tintLerpCoroutine = StartCoroutine(LerpCastleTint(target, 0.3f));
        }

        private System.Collections.IEnumerator LerpCastleTint(Color target, float dur)
        {
            _castleMpb ??= new MaterialPropertyBlock();
            Color from = _currentCastleTint;
            float t    = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                Color c = Color.Lerp(from, target, t / dur);
                _castleMpb.SetColor(_baseColorId, c);
                _castleMpb.SetColor(_colorId,     c);
                foreach (var rend in GetComponentsInChildren<Renderer>())
                {
                    if (rend.gameObject.name.StartsWith("CastleHPBar")) continue;
                    rend.SetPropertyBlock(_castleMpb);
                }
                yield return null;
            }
            _currentCastleTint = target;
            _castleMpb.SetColor(_baseColorId, target);
            _castleMpb.SetColor(_colorId,     target);
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.name.StartsWith("CastleHPBar")) continue;
                rend.SetPropertyBlock(_castleMpb);
            }
            _tintLerpCoroutine = null;
        }

        private void CheckOverrun()
        {
            float now = Time.time;

            while (_hitTimestamps.Count > 0 && now - _hitTimestamps.Peek() > OverrunWindowSec)
                _hitTimestamps.Dequeue();

            _hitTimestamps.Enqueue(now);

            if (_hitTimestamps.Count < OverrunHitThreshold) return;
            if (now < _overrunCooldownUntil) return;

            _overrunCooldownUntil = now + OverrunCooldownSec;
            _hitTimestamps.Clear();

            JuiceFX.Instance?.Flash(new Color(1f, 0f, 0f, 0.55f), Mathf.RoundToInt(OverrunVignetteDurSec * 1000f));
            AudioController.Instance?.Play("overrun_alert", 1f);
        }

        private void ApplyGrayscale()
        {
            if (_grayscaleApplied) return;
            _grayscaleApplied = true;
            _smokeActive = false;
            if (_smokeCoroutine != null) { StopCoroutine(_smokeCoroutine); _smokeCoroutine = null; }
            if (_sparksCoroutine != null) { StopCoroutine(_sparksCoroutine); _sparksCoroutine = null; }
            if (_smokePs != null) _smokePs.Stop();
            if (_firePs != null)  { _firePs.Stop(); _firePs = null; }
            if (_dangerLight != null) _dangerLight.intensity = 0f;
            foreach (var cp in _candlePs) { if (cp != null) cp.Stop(); }

            var gray = new Color(0.53f, 0.53f, 0.53f);
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.name.StartsWith("CastleHPBar")) continue;
                foreach (var mat in rend.materials)
                    mat.color = gray;
            }
        }

    }
}
