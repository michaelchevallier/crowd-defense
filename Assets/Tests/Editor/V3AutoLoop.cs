#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.EditorTools
{
    public static class V3AutoLoop
    {
        private static readonly List<string> _consoleErrors = new List<string>();
        private static bool _listenerRegistered = false;

        [MenuItem("Tools/CrowdDefense/QA/V3Batch/AutoLoop")]
        public static void AutoLoop()
        {
            RegisterConsoleListener();

            var validatorResult = RunValidatorCapture();
            var screenshotResult = RunScreenshotCapture();
            var errors = FlushConsoleErrors();

            // Batch -nographics produces unavoidable GPU-context errors that are not real failures.
            // Count only "real" errors against the summary; keep all in JSON for visibility.
            int batchArtifacts = 0;
            foreach (var e in errors)
                if (IsBatchModeArtifact(e)) batchArtifacts++;
            int realErrors = errors.Count - batchArtifacts;

            int totalPassed = validatorResult.passed + screenshotResult.passed;
            int totalFailed = validatorResult.failed + screenshotResult.failed + realErrors;
            string summary = totalFailed == 0
                ? $"ALL PASS ({totalPassed} checks, {batchArtifacts} batch-mode artifacts ignored)"
                : $"FAIL — {totalFailed} real issue(s) detected ({batchArtifacts} batch artifacts ignored)";

            var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var json = BuildJson(ts, validatorResult, screenshotResult, errors, summary);

            Directory.CreateDirectory("Library/V3AutoLoop");
            File.WriteAllText("Library/V3AutoLoop/latest.json", json);
            File.WriteAllText($"Library/V3AutoLoop/run-{ts}.json", json);

            Debug.Log($"[V3AutoLoop] {summary}\nJSON: Library/V3AutoLoop/latest.json");

            if (Application.isBatchMode)
                EditorApplication.Exit(totalFailed > 0 ? 1 : 0);
        }

        // ── Validator ─────────────────────────────────────────────────────────

        private static ValidatorResult RunValidatorCapture()
        {
            try
            {
                V3BatchValidator.RunAll();
            }
            catch (Exception ex)
            {
                return new ValidatorResult { passed = 0, failed = 1,
                    failures = new List<string> { $"RunAll threw: {ex.Message}" } };
            }

            return ParseValidatorReport("Library/V3BatchReports/edit-mode-latest.txt");
        }

        private static ValidatorResult ParseValidatorReport(string path)
        {
            var result = new ValidatorResult();
            if (!File.Exists(path))
            {
                result.failures.Add($"Report not found: {path}");
                result.failed = 1;
                return result;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("FAIL:"))
                {
                    result.failures.Add(line.Substring(5).Trim());
                    result.failed++;
                }
                else if (line.Contains("PASSED") && line.Contains("FAILED"))
                {
                    var m = Regex.Match(line, @"(\d+)\s+PASSED.*?(\d+)\s+FAILED");
                    if (m.Success)
                    {
                        result.passed = int.Parse(m.Groups[1].Value);
                        result.failed = int.Parse(m.Groups[2].Value);
                    }
                }
            }
            return result;
        }

        // ── Screenshots ───────────────────────────────────────────────────────

        private static ScreenshotResult RunScreenshotCapture()
        {
            try
            {
                V3ScreenshotBatch.CaptureAll();
            }
            catch (Exception ex)
            {
                return new ScreenshotResult { passed = 0, failed = 1,
                    scenes = new Dictionary<string, string> { ["error"] = ex.Message } };
            }

            return ParseScreenshotReport("Library/V3Screenshots/report.txt");
        }

        private static ScreenshotResult ParseScreenshotReport(string path)
        {
            var result = new ScreenshotResult();
            if (!File.Exists(path))
            {
                result.scenes["error"] = $"Report not found: {path}";
                result.failed = 1;
                return result;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("PASS:"))
                {
                    var scene = line.Substring(5).Trim();
                    result.scenes[scene] = "pass";
                    result.passed++;
                }
                else if (line.StartsWith("FAIL:"))
                {
                    var parts = line.Substring(5).Split(new[] { " — " }, 2, StringSplitOptions.None);
                    var scene = parts[0].Trim();
                    var reason = parts.Length > 1 ? parts[1].Trim() : "unknown";
                    result.scenes[scene] = $"fail: {reason}";
                    result.failed++;
                }
            }
            return result;
        }

        // ── Console errors ────────────────────────────────────────────────────

        private static void RegisterConsoleListener()
        {
            if (_listenerRegistered) return;
            Application.logMessageReceived += OnLog;
            _listenerRegistered = true;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _consoleErrors.Add($"[{type}] {condition}");
        }

        // Known batch -nographics artifacts that aren't real failures.
        private static readonly string[] _batchArtifactPatterns =
        {
            "RenderTexture.Create failed",
            "DrawOpaqueObjects/DrawTransparentObjects: Unable to find surface for attachment 0",
            "DrawOpaqueObjects/DrawSkybox/DrawTransparentObjects: Unable to find surface for attachment 0",
            "EndRenderPass: Not inside a Renderpass",
        };

        private static bool IsBatchModeArtifact(string error)
        {
            foreach (var pattern in _batchArtifactPatterns)
                if (error.Contains(pattern)) return true;
            return false;
        }

        private static List<string> FlushConsoleErrors()
        {
            Application.logMessageReceived -= OnLog;
            _listenerRegistered = false;
            var copy = new List<string>(_consoleErrors);
            _consoleErrors.Clear();
            return copy;
        }

        // ── JSON builder (manual — no Newtonsoft) ────────────────────────────

        private static string BuildJson(
            string ts,
            ValidatorResult vr,
            ScreenshotResult sr,
            List<string> errors,
            string summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            sb.AppendLine("  \"validator\": {");
            sb.AppendLine($"    \"passed\": {vr.passed},");
            sb.AppendLine($"    \"failed\": {vr.failed},");
            sb.AppendLine("    \"failures\": [");
            for (int i = 0; i < vr.failures.Count; i++)
                sb.AppendLine($"      {JsonString(vr.failures[i])}{(i < vr.failures.Count - 1 ? "," : "")}");
            sb.AppendLine("    ]");
            sb.AppendLine("  },");
            sb.AppendLine("  \"screenshots\": {");
            sb.AppendLine($"    \"passed\": {sr.passed},");
            sb.AppendLine($"    \"failed\": {sr.failed},");
            sb.AppendLine("    \"scenes\": {");
            var sceneKeys = new List<string>(sr.scenes.Keys);
            for (int i = 0; i < sceneKeys.Count; i++)
            {
                var k = sceneKeys[i];
                sb.AppendLine($"      {JsonString(k)}: {JsonString(sr.scenes[k])}{(i < sceneKeys.Count - 1 ? "," : "")}");
            }
            sb.AppendLine("    }");
            sb.AppendLine("  },");
            sb.AppendLine("  \"consoleErrors\": [");
            for (int i = 0; i < errors.Count; i++)
                sb.AppendLine($"    {JsonString(errors[i])}{(i < errors.Count - 1 ? "," : "")}");
            sb.AppendLine("  ],");
            sb.AppendLine($"  \"summary\": {JsonString(summary)}");
            sb.Append("}");
            return sb.ToString();
        }

        private static string JsonString(string s)
        {
            if (s == null) return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                           .Replace("\n", "\\n").Replace("\r", "\\r")
                           .Replace("\t", "\\t") + "\"";
        }

        // ── DTOs ──────────────────────────────────────────────────────────────

        private class ValidatorResult
        {
            public int passed;
            public int failed;
            public List<string> failures = new List<string>();
        }

        private class ScreenshotResult
        {
            public int passed;
            public int failed;
            public Dictionary<string, string> scenes = new Dictionary<string, string>();
        }
    }
}
