#nullable enable
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

        private ParticleSystem? _ps;

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

            // ---- Emission ----
            var emission = _ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(30f);

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
            rend.renderMode     = ParticleSystemRenderMode.Billboard;
            rend.sortingLayerName = "Default";
            rend.sortingOrder   = 5;

            _ps.Play();
        }

        private void StopPollen()
        {
            if (_ps == null) return;
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_ps.gameObject);
            _ps = null;
        }
    }
}
