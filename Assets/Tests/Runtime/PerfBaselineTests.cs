#nullable enable
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrowdDefense.Tests.Runtime
{
    // Perf baseline measurements written to .claude/qa/reports/perf-baseline.md
    // for tracking across sessions. These are *synthetic* baselines : minimal-cost
    // GO + renderer + transform updates, NOT the full enemy AI logic. They serve
    // as a regression detector for Unity's per-frame overhead on the host machine.
    //
    // Thresholds :
    //  - Desktop floor : 60 fps with 50 enemies + 10 towers (synthetic GOs)
    //  - Mobile floor  : 30 fps with same scene
    //  - WebGL build size target : < 30 MB raw build folder (post-Brotli, server-side
    //    compression brings the wire-transfer size to ~6-15 MB)
    public class PerfBaselineTests
    {
        private const string ReportPath = ".claude/qa/reports/perf-baseline.md";

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
        public IEnumerator Perf_50Enemies_10Towers_DesktopFloor60Fps()
        {
            yield return MeasureAndAssert(
                enemyCount: 50,
                towerCount: 10,
                minFps: 60f,
                label: "desktop_50e_10t");
        }

        [UnityTest]
        public IEnumerator Perf_50Enemies_10Towers_MobileFloor30Fps()
        {
            // Same scene, lower floor — passes on most machines but flags regressions
            // when desktop pass but mobile fail (signals a heavier-than-expected change).
            yield return MeasureAndAssert(
                enemyCount: 50,
                towerCount: 10,
                minFps: 30f,
                label: "mobile_50e_10t");
        }

        [Test]
        public void Perf_WebGLBuildSize_UnderTarget()
        {
            // Inspect the most recent WebGL build under Builds/WebGL/ or Build/WebGL/.
            // This test is *informational* : if no build exists, mark Inconclusive.
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "..", "Builds", "WebGL"),
                Path.Combine(Application.dataPath, "..", "Build", "WebGL"),
            };

            string? found = null;
            foreach (var c in candidates)
            {
                if (Directory.Exists(c)) { found = c; break; }
            }

            if (found == null)
            {
                Assert.Inconclusive(
                    "No WebGL build found. Run BuildScript.BuildWebGL to populate, then re-run this test.");
                return;
            }

            long totalBytes = DirectorySizeBytes(found);
            long totalMb = totalBytes / (1024 * 1024);
            const long maxMb = 30;

            AppendReportLine(
                $"- WebGL build size : {totalMb} MB (path : {found}, target : < {maxMb} MB)");

            Assert.LessOrEqual(totalMb, maxMb,
                $"WebGL build size {totalMb} MB exceeds target {maxMb} MB.");
        }

        // === Internals ===

        private IEnumerator MeasureAndAssert(int enemyCount, int towerCount, float minFps, string label)
        {
            BuildScene(enemyCount, towerCount);

            // Warm-up.
            float warmup = 0.5f;
            float elapsed = 0f;
            while (elapsed < warmup) { elapsed += Time.unscaledDeltaTime; yield return null; }

            // Sample 2s window.
            float window = 2f;
            elapsed = 0f;
            int frames = 0;
            while (elapsed < window)
            {
                elapsed += Time.unscaledDeltaTime;
                frames++;
                yield return null;
            }

            float avgFps = frames / elapsed;
            AppendReportLine(
                $"- {label} : {avgFps:F1} fps avg ({frames} frames in {elapsed:F2}s, min target {minFps:F0} fps)");

            Debug.Log($"[Perf] {label} → {avgFps:F1} fps avg (target >= {minFps})");

            Assert.GreaterOrEqual(avgFps, minFps,
                $"Perf {label} below floor {minFps} fps. Got {avgFps:F1}.");
        }

        private void BuildScene(int enemyCount, int towerCount)
        {
            var mat = new Material(
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default"));

            for (int i = 0; i < enemyCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = mat;
                go.transform.position = new Vector3(
                    (i % 10) * 1.5f - 7.5f, 0.5f, (i / 10) * 1.5f - 3.75f);
                _spawned.Add(go);
            }

            for (int i = 0; i < towerCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = mat;
                go.transform.position = new Vector3(i * 2f - 9f, 0.5f, -6f);
                _spawned.Add(go);
            }
        }

        private static long DirectorySizeBytes(string path)
        {
            long sum = 0;
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { sum += new FileInfo(f).Length; }
                catch { /* ignore unreadable */ }
            }
            return sum;
        }

        private static void AppendReportLine(string line)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
                if (!File.Exists(ReportPath))
                {
                    File.WriteAllText(ReportPath,
                        $"# Perf Baseline — {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
                }
                File.AppendAllText(ReportPath, line + "\n");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PerfBaseline] Failed to write report : {e.Message}");
            }
        }
    }
}
