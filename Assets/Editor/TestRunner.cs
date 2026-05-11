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
    // Programmatic test runner used by:
    // - MenuItem "Tools/CrowdDefense/QA/Run All Tests" (interactive)
    // - CLI batch-mode via TestRunner.RunAll (Unity -executeMethod)
    // Produces a JSON report in .claude/qa/reports/test-run-{timestamp}.json
    // and a markdown summary in .claude/qa/reports/test-run-latest.md
    public static class TestRunner
    {
        private const string ReportDir = ".claude/qa/reports";

        [MenuItem("Tools/CrowdDefense/QA/Run All Tests")]
        public static void RunAll()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var collector = new ResultCollector();
            api.RegisterCallbacks(collector);

            var filter = new Filter
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };

            Debug.Log("[TestRunner] Launching EditMode + PlayMode tests…");
            api.Execute(new ExecutionSettings(filter));
        }

        [MenuItem("Tools/CrowdDefense/QA/Run EditMode Tests")]
        public static void RunEditMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCollector());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        [MenuItem("Tools/CrowdDefense/QA/Run PlayMode Tests")]
        public static void RunPlayMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCollector());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
        }

        // Reusable result collector that writes both JSON + markdown to ReportDir.
        private sealed class ResultCollector : ICallbacks
        {
            private readonly List<TestRow> _rows = new();
            private DateTime _runStarted;

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _runStarted = DateTime.Now;
                _rows.Clear();
                Debug.Log($"[TestRunner] Run started: {CountAll(testsToRun)} tests scheduled.");
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                _rows.Add(new TestRow
                {
                    Name = result.Test.FullName,
                    Status = result.TestStatus.ToString(),
                    Duration = result.Duration,
                    Message = result.Message ?? string.Empty,
                    StackTrace = result.StackTrace ?? string.Empty
                });
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                int total = _rows.Count;
                int pass = _rows.Count(r => r.Status == "Passed");
                int fail = _rows.Count(r => r.Status == "Failed");
                int skip = _rows.Count(r => r.Status == "Skipped" || r.Status == "Inconclusive");

                Directory.CreateDirectory(ReportDir);
                string stamp = _runStarted.ToString("yyyyMMdd-HHmmss");
                string jsonPath = Path.Combine(ReportDir, $"test-run-{stamp}.json");
                string mdLatest = Path.Combine(ReportDir, "test-run-latest.md");

                File.WriteAllText(jsonPath, BuildJson(result, total, pass, fail, skip));
                File.WriteAllText(mdLatest, BuildMarkdown(result, total, pass, fail, skip));

                Debug.Log($"[TestRunner] Run finished: {pass} pass / {fail} fail / {skip} skip / {total} total. " +
                          $"Reports → {jsonPath} + {mdLatest}");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(fail == 0 ? 0 : 1);
                }
            }

            private static int CountAll(ITestAdaptor t)
            {
                if (!t.IsSuite) return 1;
                int sum = 0;
                if (t.Children != null)
                    foreach (var c in t.Children) sum += CountAll(c);
                return sum;
            }

            private string BuildJson(ITestResultAdaptor result, int total, int pass, int fail, int skip)
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"started\": \"{_runStarted:O}\",");
                sb.AppendLine($"  \"finished\": \"{DateTime.Now:O}\",");
                sb.AppendLine($"  \"total\": {total},");
                sb.AppendLine($"  \"passed\": {pass},");
                sb.AppendLine($"  \"failed\": {fail},");
                sb.AppendLine($"  \"skipped\": {skip},");
                sb.AppendLine($"  \"duration\": {result.Duration:F3},");
                sb.AppendLine("  \"tests\": [");
                for (int i = 0; i < _rows.Count; i++)
                {
                    var r = _rows[i];
                    sb.Append("    {");
                    sb.Append($"\"name\": {JsonEscape(r.Name)}, ");
                    sb.Append($"\"status\": \"{r.Status}\", ");
                    sb.Append($"\"duration\": {r.Duration:F3}, ");
                    sb.Append($"\"message\": {JsonEscape(r.Message)}");
                    sb.Append("}");
                    if (i < _rows.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                return sb.ToString();
            }

            private string BuildMarkdown(ITestResultAdaptor result, int total, int pass, int fail, int skip)
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Test Run Report — latest");
                sb.AppendLine();
                sb.AppendLine($"- **Started** : {_runStarted:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"- **Duration** : {result.Duration:F2}s");
                sb.AppendLine($"- **Total** : {total} ({pass} pass / {fail} fail / {skip} skip)");
                sb.AppendLine($"- **Status** : {(fail == 0 ? "PASS" : "FAIL")}");
                sb.AppendLine();
                sb.AppendLine("## Results");
                sb.AppendLine();
                sb.AppendLine("| Test | Status | Duration |");
                sb.AppendLine("|------|--------|----------|");
                foreach (var r in _rows)
                {
                    string icon = r.Status switch
                    {
                        "Passed" => "PASS",
                        "Failed" => "FAIL",
                        _ => r.Status
                    };
                    sb.AppendLine($"| `{r.Name}` | {icon} | {r.Duration:F3}s |");
                }
                if (fail > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## Failures");
                    sb.AppendLine();
                    foreach (var r in _rows.Where(x => x.Status == "Failed"))
                    {
                        sb.AppendLine($"### `{r.Name}`");
                        sb.AppendLine("```");
                        sb.AppendLine(r.Message);
                        if (!string.IsNullOrWhiteSpace(r.StackTrace))
                        {
                            sb.AppendLine(r.StackTrace);
                        }
                        sb.AppendLine("```");
                    }
                }
                return sb.ToString();
            }

            private static string JsonEscape(string s)
            {
                if (string.IsNullOrEmpty(s)) return "\"\"";
                var sb = new StringBuilder("\"", s.Length + 8);
                foreach (var c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                            else sb.Append(c);
                            break;
                    }
                }
                sb.Append("\"");
                return sb.ToString();
            }

            private struct TestRow
            {
                public string Name;
                public string Status;
                public double Duration;
                public string Message;
                public string StackTrace;
            }
        }
    }
}
