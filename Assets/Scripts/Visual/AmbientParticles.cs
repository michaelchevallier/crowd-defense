#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Systems;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    // Drives ambient pollen particles for World 1-3 (Plaine/Foret/garden theme).
    // Place this MonoBehaviour in Main.unity; it self-manages a procedural ParticleSystem.
    // Hooks into LevelEvents — no changes to LevelRunner required.
    [DefaultExecutionOrder(55)]
    public class AmbientParticles : MonoBehaviour
    {
        private const int MaxWorldForPollen = 3;
        private const int ButterflyCount    = 3;

        private ParticleSystem? _ps;
        private GameObject?     _butterflyRoot;

        // Butterfly wing colours: soft blue, lilac, pale orange.
        private static readonly Color[] ButterflyColors =
        {
            new(0.45f, 0.75f, 1.00f, 0.80f),
            new(0.80f, 0.55f, 1.00f, 0.80f),
            new(1.00f, 0.72f, 0.30f, 0.80f),
        };

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += HandleLevelStart;
            LevelEvents.OnLevelEnd   += HandleLevelEnd;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
            LevelEvents.OnLevelEnd   -= HandleLevelEnd;
        }

        private void HandleLevelStart(LevelData level, Bounds gridBounds)
        {
            StopPollen();
            if (level.World <= MaxWorldForPollen)
                SpawnPollen(gridBounds);
        }

        private void HandleLevelEnd() => StopPollen();

        private void SpawnPollen(Bounds gridBounds)
        {
            var go = new GameObject("AmbientPollen");
            go.transform.SetParent(transform);

            // Position emitter at centre-top of the play area.
            go.transform.position = new Vector3(gridBounds.center.x, gridBounds.max.y, 0f);

            _ps = go.AddComponent<ParticleSystem>();

            // ---- Main module ----
            var main = _ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                new Color(1.00f, 0.92f, 0.20f, 0.85f),   // bright yellow
                new Color(0.90f, 0.75f, 0.10f, 0.60f));  // amber tint
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.05f);   // slow fall
            main.maxParticles     = 300;

            // ---- Emission (doubled to 60/s) ----
            var emission = _ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(60f);

            // ---- Shape — scene-wide box matching the grid area ----
            var shape = _ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(gridBounds.size.x, 1f, 1f);

            // ---- Velocity over lifetime — gentle horizontal drift ----
            var vol = _ps.velocityOverLifetime;
            vol.enabled = true;
            vol.x       = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            vol.y       = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vol.z       = new ParticleSystem.MinMaxCurve(0f, 0f);
            vol.space   = ParticleSystemSimulationSpace.World;

            // ---- Colour over lifetime — fade out last 30 % of life ----
            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var grad    = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.70f), new GradientAlphaKey(0f, 1f) });
            col.color   = new ParticleSystem.MinMaxGradient(grad);

            // ---- Renderer ----
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode       = ParticleSystemRenderMode.Billboard;
            rend.sortingLayerName = "Default";
            rend.sortingOrder     = 5;

            _ps.Play();

            SpawnButterflies(gridBounds);
        }

        // Creates ButterflyCount tiny quad sprites that wander lazily inside the grid bounds.
        // Each butterfly is a coloured quad (no texture needed) driven by a coroutine.
        private void SpawnButterflies(Bounds gridBounds)
        {
            _butterflyRoot = new GameObject("AmbientButterflies");
            _butterflyRoot.transform.SetParent(transform);

            for (int i = 0; i < ButterflyCount; i++)
            {
                var bf = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bf.name = $"Butterfly_{i}";
                bf.transform.SetParent(_butterflyRoot.transform);

                // Remove default box collider — decorative only.
                Destroy(bf.GetComponent<Collider>());

                // Scale: small elongated quad mimicking wings.
                bf.transform.localScale = new Vector3(0.28f, 0.18f, 1f);

                // Random start position inside grid (upper two-thirds for visibility).
                float x = Random.Range(gridBounds.min.x, gridBounds.max.x);
                float y = Random.Range(gridBounds.center.y, gridBounds.max.y);
                bf.transform.position = new Vector3(x, y, -0.1f);

                // Tinted unlit material.
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = ButterflyColors[i % ButterflyColors.Length];
                bf.GetComponent<Renderer>().material = mat;
                bf.GetComponent<Renderer>().sortingLayerName = "Default";
                bf.GetComponent<Renderer>().sortingOrder = 6;

                StartCoroutine(FlyButterfly(bf.transform, gridBounds));
            }
        }

        // Drifts a butterfly between random waypoints inside gridBounds.
        private IEnumerator FlyButterfly(Transform tf, Bounds bounds)
        {
            while (tf != null)
            {
                // Pick a new waypoint in the upper portion of the grid.
                float tx = Random.Range(bounds.min.x + 0.5f, bounds.max.x - 0.5f);
                float ty = Random.Range(bounds.center.y, bounds.max.y - 0.3f);
                Vector3 target = new(tx, ty, tf.position.z);

                float speed    = Random.Range(0.4f, 0.9f);
                float distance = Vector3.Distance(tf.position, target);
                float duration = distance / speed;
                float elapsed  = 0f;
                Vector3 origin = tf.position;

                // Gentle sine-wave vertical wobble while moving.
                float wobbleFreq = Random.Range(2.5f, 5f);
                float wobbleAmp  = 0.08f;

                while (elapsed < duration && tf != null)
                {
                    float t     = elapsed / duration;
                    Vector3 pos = Vector3.Lerp(origin, target, t);
                    pos.y += Mathf.Sin(elapsed * wobbleFreq) * wobbleAmp;
                    tf.position = pos;

                    // Slight scale flutter simulating wing-beat.
                    float flutter = 1f + 0.15f * Mathf.Sin(elapsed * 12f);
                    tf.localScale = new Vector3(0.28f * flutter, 0.18f, 1f);

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Brief pause at waypoint.
                yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));
            }
        }

        private void StopPollen()
        {
            if (_ps != null)
            {
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(_ps.gameObject);
                _ps = null;
            }

            if (_butterflyRoot != null)
            {
                Destroy(_butterflyRoot);
                _butterflyRoot = null;
            }
        }
    }
}
