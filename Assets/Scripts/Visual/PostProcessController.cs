#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Manages global URP post-process volume: Bloom (emissive, subtle), Vignette (HP-driven),
    // ColorAdjustments (per theme). Attach to Main.unity — auto-wires via MonoSingleton.
    [DefaultExecutionOrder(52)]
    public sealed class PostProcessController : MonoSingleton<PostProcessController>
    {
        private Volume?           _volume;
        private Bloom?            _bloom;
        private Vignette?         _vignette;
        private ColorAdjustments? _color;

        protected override void OnAwakeSingleton() => BuildVolume();

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += HandleLevelStart;
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnTotalHPChanged += OnCastleHP;
            var em = EventManager.Instance;
            if (em != null)
            {
                em.Subscribe<BossEncounteredEvent>(OnBossEncountered);
                em.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            }
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnTotalHPChanged -= OnCastleHP;
            var em = EventManager.Instance;
            if (em != null)
            {
                em.Unsubscribe<BossEncounteredEvent>(OnBossEncountered);
                em.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            }
        }

        // ── Volume bootstrap ─────────────────────────────────────────────────────

        private void BuildVolume()
        {
            var go = new GameObject("PostProcess_GlobalVolume");
            go.transform.SetParent(transform, false);

            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10;
            _volume.profile  = ScriptableObject.CreateInstance<VolumeProfile>();

            _bloom    = _volume.profile.Add<Bloom>(overrides: true);
            _vignette = _volume.profile.Add<Vignette>(overrides: true);
            _color    = _volume.profile.Add<ColorAdjustments>(overrides: true);

            // Bloom — subtle glow on emissive materials, threshold above mid-grey
            _bloom.active          = true;
            _bloom.threshold.value = 0.9f;
            _bloom.intensity.value = 0.5f;
            _bloom.scatter.value   = 0.7f;

            // Vignette — starts invisible, rises as castle HP drops below 30 %
            _vignette.active           = true;
            _vignette.color.value      = Color.black;
            _vignette.intensity.value  = 0f;
            _vignette.smoothness.value = 0.5f;

            // Color adjustments — neutral until theme applied
            _color.active = true;
        }

        // ── Castle HP hook ────────────────────────────────────────────────────────

        private void OnCastleHP(int hp, int hpMax)
        {
            if (_vignette == null) return;
            float ratio  = hpMax > 0 ? (float)hp / hpMax : 1f;
            // Vignette 0 at ≥30 % HP, rises linearly to 0.45 at 0 % HP
            float danger = Mathf.Clamp01(1f - ratio / 0.30f);
            _vignette.intensity.value = Mathf.Lerp(0f, 0.45f, danger);
        }

        // ── Theme color grading ────────────────────────────────────────────────────

        private void HandleLevelStart(LevelData level, Bounds _) => ApplyTheme(level.LevelTheme);

        public void ApplyTheme(LevelTheme theme)
        {
            if (_color == null) return;
            var (exposure, contrast, filter, saturation) = ThemeColorParams(theme);
            _color.postExposure.value = exposure;
            _color.contrast.value     = contrast;
            _color.colorFilter.value  = filter;
            _color.saturation.value   = saturation;
        }

        // ── Bloom intensity ───────────────────────────────────────────────────────

        public void SetBloomIntensity(float intensity)
        {
            if (_bloom == null) return;
            _bloom.intensity.value = intensity;
        }

        private void OnBossEncountered(BossEncounteredEvent evt)
        {
            SetBloomIntensity(2.0f);
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            SetBloomIntensity(0.5f);
        }

        // ── Red vignette flash ────────────────────────────────────────────────────

        // Flash vignette to <intensity> then fade out over <fadeOut> seconds.
        public void FlashRedVignette(float intensity, float fadeOut) =>
            StartCoroutine(RedVignetteRoutine(intensity, fadeOut));

        private IEnumerator RedVignetteRoutine(float peak, float fadeOut)
        {
            if (_vignette == null) yield break;
            var prevColor = _vignette.color.value;
            _vignette.color.value     = new Color(0.9f, 0.05f, 0.05f);
            _vignette.intensity.value = peak;

            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                _vignette.intensity.value = Mathf.Lerp(peak, 0f, t / fadeOut);
                yield return null;
            }

            // Restore HP-driven vignette colour + let OnCastleHP reassert correct intensity next frame
            _vignette.color.value = prevColor;
        }

        // Per-theme: post-exposure (EV), contrast (-100..100), color filter tint, saturation (-100..100).
        private static (float exposure, float contrast, Color filter, float saturation)
            ThemeColorParams(LevelTheme theme) => theme switch
        {
            LevelTheme.Espace     => (-0.5f, 15f,  new Color(0.75f, 0.75f, 1.00f), -15f),  // bleuté désaturé
            LevelTheme.Volcan     => ( 0.3f, 20f,  new Color(1.00f, 0.72f, 0.45f),  10f),  // chaud vif contrasté
            LevelTheme.Foret      => ( 0.1f, 10f,  new Color(0.78f, 0.96f, 0.65f),   5f),  // vert frais
            LevelTheme.Desert     => ( 0.4f, 18f,  new Color(1.00f, 0.94f, 0.72f),  12f),  // ocre éclatant
            LevelTheme.Apocalypse => (-0.3f, 25f,  new Color(0.72f, 0.62f, 0.55f), -20f),  // sépia cendré
            LevelTheme.Submarin   => (-0.2f, 12f,  new Color(0.45f, 0.85f, 1.00f),   0f),  // cyan aquatique
            LevelTheme.Medieval   => ( 0.0f,  8f,  new Color(1.00f, 0.96f, 0.82f),   5f),  // chaleur douce
            LevelTheme.Cyberpunk  => (-0.4f, 30f,  new Color(0.72f, 0.55f, 1.00f),  20f),  // violet néon contrasté
            LevelTheme.Foire      => ( 0.2f, 12f,  new Color(1.00f, 0.96f, 0.80f),  15f),  // festif lumineux
            _                     => ( 0.0f,  5f,  Color.white,                       0f),  // Plaine — neutre
        };
    }
}
