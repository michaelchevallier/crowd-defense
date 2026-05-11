#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrowdDefense.Tests.Runtime.Scenarios
{
    // Sprint-gate scenario : stress 200 enemies → assert sustained FPS >= target.
    //
    // Strategy : create 200 minimal-cost GameObjects (capsule + Renderer) over 5s,
    // sample FPS frame-by-frame, assert avg FPS >= 30 (mobile floor).
    //
    // This is a synthetic stress test : it measures Unity's per-frame overhead for
    // 200 active GOs with renderers + transforms (the dominant cost in TD enemy hot
    // loop). Real enemies have additional logic but this benchmark provides a
    // reproducible baseline.
    public class ScenarioStress200Enemies
    {
        private const int EnemyCount = 200;
        private const float SampleWindowSeconds = 3f;
        private const float MobileFpsFloor = 30f;

        private readonly System.Collections.Generic.List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator Stress_200Enemies_MaintainsTargetFps()
        {
            // Allocate 200 GOs with mesh + animated transform.
            var mat = new Material(
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default"));

            for (int i = 0; i < EnemyCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = mat;
                go.transform.position = new Vector3(
                    (i % 20) * 1.5f - 15f,
                    0.5f,
                    (i / 20) * 1.5f - 7.5f);
                go.AddComponent<StressMover>();
                _spawned.Add(go);
            }

            // Warm-up : 1s let GC + transform sync.
            float warmup = 1f;
            float warmupElapsed = 0f;
            while (warmupElapsed < warmup)
            {
                warmupElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Sample FPS over window.
            float elapsed = 0f;
            int frames = 0;
            while (elapsed < SampleWindowSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                frames++;
                yield return null;
            }

            float avgFps = frames / elapsed;
            Debug.Log($"[ScenarioStress200] {EnemyCount} GOs, {frames} frames in {elapsed:F2}s → {avgFps:F1} fps avg");

            Assert.GreaterOrEqual(avgFps, MobileFpsFloor,
                $"Stress test 200 enemies must hold >= {MobileFpsFloor} fps (mobile floor). Got {avgFps:F1}.");
        }

        // Inline stress mover to add per-frame transform updates.
        private sealed class StressMover : MonoBehaviour
        {
            private float _t;
            private void Update()
            {
                _t += Time.deltaTime;
                transform.Rotate(0, 30f * Time.deltaTime, 0);
                var p = transform.position;
                p.y = 0.5f + Mathf.Sin(_t * 2f) * 0.1f;
                transform.position = p;
            }
        }
    }
}
