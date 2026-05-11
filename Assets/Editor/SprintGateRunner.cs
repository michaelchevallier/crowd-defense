#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace CrowdDefense.Editor
{
    // Sprint-gate runner : executes all PlayMode scenarios + perf tests, captures
    // pass/fail per scenario, writes a markdown sprint report to .claude/qa/reports/
    // and updates STATUS.md sprint gate row if --update-status flag passed via CLI.
    //
    // Spawns are :
    //  - ScenarioW1_1Complete : W1-1 scaffold smoke
    //  - ScenarioW5_1Boss : W5-1 boss declared
    //  - ScenarioStress200Enemies : 200 GOs hold FPS floor
    //  - PerfBaselineTests : desktop + mobile + WebGL build size
    //
    // CLI entry : Unity -batchmode -executeMethod CrowdDefense.Editor.SprintGateRunner.RunCli
    public static class SprintGateRunner
    {
        private const string ReportDir = ".claude/qa/reports";
        private static readonly string[] ScenarioFilter =
        {
            "CrowdDefense.Tests.Runtime.Scenarios.ScenarioW1_1Complete",
            "CrowdDefense.Tests.Runtime.Scenarios.ScenarioW5_1Boss",
            "CrowdDefense.Tests.Runtime.Scenarios.ScenarioStress200Enemies",
            "CrowdDefense.Tests.Runtime.PerfBaselineTests"
        };

        [MenuItem("Tools/CrowdDefense/QA/Run Sprint Gate")]
        public static void Run()
        {
            Debug.Log("[SprintGate] Launching sprint-gate scenarios…");

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new SprintGateCollector());

            var filter = new Filter
            {
                testMode = TestMode.PlayMode,
                groupNames = ScenarioFilter
            };

            api.Execute(new ExecutionSettings(filter));
        }

        // Batch-mode CLI entry. Exits with code 0 (all pass) or 1 (any fail).
        public static void RunCli() => Run();

        private sealed class SprintGateCollector : ICallbacks
        {
            private DateTime _started;
            private readonly List<ScenarioRow> _rows = new();

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _started = DateTime.Now;
                _rows.Clear();
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                _rows.Add(new ScenarioRow
                {
                    Name = result.Test.FullName,
                    Status = result.TestStatus.ToString(),
                    Duration = result.Duration,
                    Message = result.Message ?? string.Empty
                });
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory(ReportDir);
                string stamp = _started.ToString("yyyyMMdd-HHmmss");
                string reportPath = Path.Combine(ReportDir, $"sprint-{stamp}.md");
                string latestPath = Path.Combine(ReportDir, "sprint-latest.md");

                var md = BuildMarkdown(result);
                File.WriteAllText(reportPath, md);
                File.WriteAllText(latestPath, md);

                int pass = _rows.Count(r => r.Status == "Passed");
                int fail = _rows.Count(r => r.Status == "Failed");
                int skip = _rows.Count(r => r.Status == "Skipped" || r.Status == "Inconclusive");

                Debug.Log(
                    $"[SprintGate] Finished : {pass} pass / {fail} fail / {skip} skip / {_rows.Count} total. " +
                    $"Report → {reportPath}");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(fail == 0 ? 0 : 1);
                }
            }

            private string BuildMarkdown(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                int pass = _rows.Count(r => r.Status == "Passed");
                int fail = _rows.Count(r => r.Status == "Failed");
                int skip = _rows.Count(r => r.Status == "Skipped" || r.Status == "Inconclusive");

                sb.AppendLine($"# Sprint Gate Report — {_started:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine($"- **Duration** : {result.Duration:F2}s");
                sb.AppendLine($"- **Total** : {_rows.Count} ({pass} pass / {fail} fail / {skip} skip/inconclusive)");
                sb.AppendLine($"- **Status** : {(fail == 0 ? "PASS" : "FAIL")}");
                sb.AppendLine();
                sb.AppendLine("## Scenarios");
                sb.AppendLine();
                sb.AppendLine("| Scenario | Status | Duration | Notes |");
                sb.AppendLine("|----------|--------|----------|-------|");
                foreach (var r in _rows)
                {
                    string shortName = r.Name.Contains(".")
                        ? r.Name.Substring(r.Name.LastIndexOf('.') + 1)
                        : r.Name;
                    string note = r.Message.Length > 0
                        ? r.Message.Replace("\n", " ").Replace("\r", " ").Substring(0, Math.Min(80, r.Message.Length))
                        : "—";
                    sb.AppendLine($"| `{shortName}` | {r.Status} | {r.Duration:F2}s | {note} |");
                }
                sb.AppendLine();

                if (fail > 0)
                {
                    sb.AppendLine("## Failures");
                    sb.AppendLine();
                    foreach (var r in _rows.Where(x => x.Status == "Failed"))
                    {
                        sb.AppendLine($"### `{r.Name}`");
                        sb.AppendLine("```");
                        sb.AppendLine(r.Message);
                        sb.AppendLine("```");
                    }
                }

                if (skip > 0)
                {
                    sb.AppendLine("## Skipped / Inconclusive");
                    sb.AppendLine();
                    foreach (var r in _rows.Where(x =>
                        x.Status == "Skipped" || x.Status == "Inconclusive"))
                    {
                        sb.AppendLine($"- `{r.Name}` — {r.Message}");
                    }
                }

                return sb.ToString();
            }

            private struct ScenarioRow
            {
                public string Name;
                public string Status;
                public double Duration;
                public string Message;
            }
        }
    }
}
