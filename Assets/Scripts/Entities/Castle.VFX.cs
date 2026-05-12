#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Castle : MonoSingleton<Castle>
    {
        protected void BuildCastleAura()
        {
            _castleAura = transform.Find("CastleAura")?.GetComponent<Light>();
            if (_castleAura == null)
            {
                var go = new GameObject("CastleAura");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                _castleAura = go.AddComponent<Light>();
                _castleAura.type      = LightType.Point;
                _castleAura.range     = 5f;
                _castleAura.intensity = 2f;
                _castleAura.color     = Color.white;
            }
            UpdateCastleAura();
        }

        protected void UpdateCastleAura()
        {
            if (_castleAura == null) return;
            var pct = HPMax > 0 ? (float)HP / HPMax : 0f;
            _castleAura.intensity = Mathf.Lerp(0.5f, 2f, pct);
            _castleAura.color     = pct < 0.3f
                ? Color.red
                : Color.Lerp(Color.red, Color.white, pct);
        }

        protected void UpdateDamageVfxIntensity()
        {
            float ratio = HPMax > 0 ? (float)HP / HPMax : 0f;

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
                else if (ratio > 0.15f)
                {
                    emission.rateOverTime = 20f;
                    if (!_smokePs.isPlaying) _smokePs.Play();
                }
                else
                {
                    emission.rateOverTime = 35f;
                    if (!_smokePs.isPlaying) _smokePs.Play();
                }
            }

            if (ratio <= 0.15f && !_firePsSpawned)
            {
                _firePsSpawned = true;
                _firePs        = SpawnFirePs();
                JuiceFX.Instance?.Flash(new Color(0.9f, 0.1f, 0f, 0.3f), 800);
            }

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
            else if (ratio > 0.15f)
            {
                _dangerLight.intensity = 3f;
                _dangerLight.color     = new Color(1f, 0.3f, 0.1f);

                if (_sparksCoroutine == null)
                    _sparksCoroutine = StartCoroutine(SparksLoop());
            }
            else
            {
                _dangerLight.intensity = 5f;
                _dangerLight.color     = new Color(1f, 0.1f, 0f);

                if (_sparksCoroutine == null)
                    _sparksCoroutine = StartCoroutine(SparksLoop());
            }
        }

        private ParticleSystem SpawnFirePs()
        {
            var go = new GameObject("CastleFirePs");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop           = true;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor     = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.05f), new Color(1f, 0.2f, 0f));
            main.gravityModifier = -0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 15f;
            shape.radius    = 0.3f;

            ps.Play();
            return ps;
        }

        protected void SpawnCandleParticles()
        {
            Vector3[] corners =
            {
                new Vector3( 0.6f, 0.15f,  0.6f),
                new Vector3(-0.6f, 0.15f,  0.6f),
                new Vector3( 0.6f, 0.15f, -0.6f),
                new Vector3(-0.6f, 0.15f, -0.6f),
            };

            for (int i = 0; i < corners.Length; i++)
            {
                var go = new GameObject($"CastleCandle_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = corners[i];

                var ps = go.AddComponent<ParticleSystem>();

                var main = ps.main;
                main.loop            = true;
                main.startLifetime   = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
                main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.startColor      = new ParticleSystem.MinMaxGradient(
                                           new Color(1f, 0.6f, 0.05f),
                                           new Color(1f, 0.25f, 0f));
                main.gravityModifier = -0.15f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = ps.emission;
                emission.rateOverTime = 10f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle     = 8f;
                shape.radius    = 0.03f;

                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f,
                    new AnimationCurve(
                        new Keyframe(0f, 0.4f, 0f, 1.5f),
                        new Keyframe(0.4f, 1f, 1.5f, -1.5f),
                        new Keyframe(1f, 0f, -1.5f, 0f)));

                var col = ps.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                        new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.5f),
                        new GradientColorKey(new Color(0.6f, 0.1f, 0f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.9f, 0f),
                        new GradientAlphaKey(0.7f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    });
                col.color = new ParticleSystem.MinMaxGradient(grad);

                ps.Play();
                _candlePs[i] = ps;
            }
        }

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

        protected void TriggerHitVfx()
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

    }
}
