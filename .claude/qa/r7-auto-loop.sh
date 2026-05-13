#!/usr/bin/env bash
# R7 auto-eval loop — UnityMCP HTTP cycle (Mike requested 2026-05-13).
#
# Boucle de découverte de bugs runtime SANS attendre Mike Play mode.
# 1) initialize MCP session
# 2) refresh + wait compile
# 3) read console pre-play
# 4) manage_editor play
# 5) wait stable boot (~20s)
# 6) execute_code base mechanics (spawn enemy, place tower)
# 7) read console post-play
# 8) manage_editor stop
# 9) output rapport markdown
#
# Usage: ./r7-auto-loop.sh [--no-play] [--report-out path.md]
#
# Constraints (memory rules) :
#   - Unity 6 + URP 17.3 + WebGL2 cassé → NE PAS BUILD WEB
#   - Validate exclusivement Editor Play mode + UnityMCP HTTP
#   - Si UnityMCP HTTP DOWN → abort, request Mike start UnityMCP
set -euo pipefail

UMCP="${UMCP:-http://127.0.0.1:8080/mcp}"
REPORT_OUT="${REPORT_OUT:-.claude/qa/r7-auto-loop-$(date +%Y-%m-%d-%Hh%M).md}"
DO_PLAY=1
[[ "${1:-}" == "--no-play" ]] && DO_PLAY=0

# Health check
H=$(curl -s "${UMCP%/*}/health" || echo "")
if ! echo "$H" | grep -q '"status":"healthy"'; then
  echo "ABORT: UnityMCP HTTP server down (${UMCP%/*}/health unreachable). Start Unity Editor + UnityMCP package."
  exit 2
fi

# Initialize session
SESSION=$(curl -s -X POST "$UMCP" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","clientInfo":{"name":"r7-auto-loop","version":"1"},"capabilities":{}}}' \
  -D - 2>/dev/null | grep -i "mcp-session-id" | awk '{print $2}' | tr -d '\r\n' || true)
if [[ -z "$SESSION" ]]; then echo "ABORT: no MCP session-id returned"; exit 3; fi

mcp_call() {
  local TOOL="$1" ARGS="$2"
  curl -s -X POST "$UMCP" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json, text/event-stream" \
    -H "mcp-session-id: $SESSION" \
    -d "{\"jsonrpc\":\"2.0\",\"id\":$RANDOM,\"method\":\"tools/call\",\"params\":{\"name\":\"$TOOL\",\"arguments\":$ARGS}}" 2>/dev/null \
    | sed -n 's/^data: //p' | python3 -c "
import sys, json
for line in sys.stdin:
    line = line.strip()
    if not line: continue
    try:
        o = json.loads(line)
        if 'result' in o:
            for c in o['result'].get('content', []):
                print(c.get('text',''))
        elif 'error' in o:
            print('ERROR: ' + json.dumps(o['error']))
    except Exception as e:
        print('PARSE_ERR: ' + line[:200])
" 2>/dev/null
}

# Wait Unity ready (compile done) — retry up to 60s
echo "[1/9] wait Unity ready..."
for i in $(seq 1 12); do
  R=$(mcp_call "read_console" '{"action":"get","count":"1"}' || true)
  echo "$R" | grep -q "not ready" || break
  sleep 5
done

# Pre-play console snapshot
echo "[2/9] refresh assets..."
mcp_call "refresh_unity" '{"mode":"if_dirty","scope":"all","compile":"request","wait_for_ready":true}' > /tmp/r7-refresh.txt 2>&1 || true

echo "[3/9] read pre-play console..."
PRE=$(mcp_call "read_console" '{"action":"get","types":["error","warning"],"count":"200","format":"plain","include_stacktrace":false}')
PRE_ERRORS=$(echo "$PRE" | grep -c -i "^\[Error\]\|error CS\|error:" || true)
PRE_WARNS=$(echo "$PRE" | grep -c -i "^\[Warning\]\|warning:" || true)

if [[ $DO_PLAY -eq 1 ]]; then
  echo "[4/9] clear console + enter Play mode..."
  mcp_call "read_console" '{"action":"clear"}' > /dev/null
  PLAY_RES=$(mcp_call "manage_editor" '{"action":"play"}')
  echo "$PLAY_RES" | head -2

  echo "[5/9] wait stable boot 20s..."
  sleep 20

  echo "[6/9] execute base mechanics test..."
  TEST_CODE='
using UnityEngine;
using CrowdDefense.Systems;
using CrowdDefense.Entities;
var result = new System.Text.StringBuilder();
var lvlRunner = Object.FindFirstObjectByType<LevelRunner>();
result.AppendLine("LevelRunner: " + (lvlRunner != null ? "OK" : "NULL"));
var castle = Object.FindFirstObjectByType<Castle>();
result.AppendLine("Castle: " + (castle != null ? $"HP={castle.CurrentHp:F0}" : "NULL"));
var hero = Object.FindFirstObjectByType<Hero>();
result.AppendLine("Hero: " + (hero != null ? $"pos={hero.transform.position}" : "NULL"));
var anim = hero != null ? hero.GetComponentInChildren<Animator>() : null;
result.AppendLine("Hero Animator: " + (anim != null ? $"ctrl={anim.runtimeAnimatorController?.name} state={anim.GetCurrentAnimatorStateInfo(0).fullPathHash}" : "NULL"));
var wm = Object.FindFirstObjectByType<WaveManager>();
result.AppendLine("WaveManager: " + (wm != null ? "OK" : "NULL"));
var towerPool = Object.FindFirstObjectByType<TowerPool>();
result.AppendLine("TowerPool: " + (towerPool != null ? "OK" : "NULL"));
var enemyPool = Object.FindFirstObjectByType<EnemyPool>();
result.AppendLine("EnemyPool: " + (enemyPool != null ? "OK" : "NULL"));
var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
result.AppendLine("Cameras: " + cams.Length);
foreach (var c in cams) result.AppendLine("  - " + c.name + " pos=" + c.transform.position + " bgcol=" + c.backgroundColor);
return result.ToString();
'
  TEST_JSON=$(python3 -c "import json,sys; print(json.dumps({'action':'execute','code':sys.argv[1],'compiler':'auto'}))" "$TEST_CODE")
  TEST_RES=$(mcp_call "execute_code" "$TEST_JSON" || echo "TEST_FAIL")

  echo "[7/9] wait 5s more + read post-play console..."
  sleep 5
  POST=$(mcp_call "read_console" '{"action":"get","types":["error","warning"],"count":"300","format":"plain","include_stacktrace":false}')
  POST_ERRORS=$(echo "$POST" | grep -c -i "^\[Error\]\|error CS\|error:" || true)
  POST_WARNS=$(echo "$POST" | grep -c -i "^\[Warning\]\|warning:" || true)

  echo "[8/9] exit Play mode..."
  mcp_call "manage_editor" '{"action":"stop"}' > /dev/null
else
  PLAY_RES="(skipped --no-play)"
  TEST_RES=""
  POST=""
  POST_ERRORS=0
  POST_WARNS=0
fi

# Detect MCP failure markers vs real responses (HONEST verdict)
mcp_failed() {
  local body="$1"
  if [[ -z "$body" ]] || echo "$body" | grep -qE '"success":false|not ready|disconnected|PARSE_ERR'; then
    return 0  # failed
  fi
  return 1
}

PRE_STATUS="ok"
mcp_failed "$PRE" && PRE_STATUS="MCP_FAILURE"
POST_STATUS="ok"
PLAY_STATUS="ok"
TEST_STATUS="ok"
if [[ $DO_PLAY -eq 1 ]]; then
  mcp_failed "$PLAY_RES" && PLAY_STATUS="MCP_FAILURE"
  mcp_failed "$TEST_RES" && TEST_STATUS="MCP_FAILURE"
  mcp_failed "$POST" && POST_STATUS="MCP_FAILURE"
fi

echo "[9/9] write report $REPORT_OUT..."
{
  echo "# R7 auto-loop report — $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo
  echo "HEAD: $(cd /Users/mike/Work/crowd-defense && git rev-parse --short HEAD)"
  echo "MCP session: $SESSION"
  echo
  echo "## Pre-play console ($PRE_STATUS)"
  echo "- Errors: $PRE_ERRORS"
  echo "- Warnings: $PRE_WARNS"
  echo
  echo "<details><summary>Pre-play (top 50)</summary>"
  echo ''
  echo '```'
  echo "$PRE" | head -50
  echo '```'
  echo "</details>"
  echo
  if [[ $DO_PLAY -eq 1 ]]; then
    echo "## Play mode trigger ($PLAY_STATUS)"
    echo '```'
    echo "$PLAY_RES" | head -5
    echo '```'
    echo
    echo "## Base mechanics test ($TEST_STATUS)"
    echo '```'
    echo "$TEST_RES" | head -30
    echo '```'
    echo
    echo "## Post-play console ($POST_STATUS)"
    echo "- Errors: $POST_ERRORS"
    echo "- Warnings: $POST_WARNS"
    echo
    echo "<details><summary>Post-play (top 100)</summary>"
    echo ''
    echo '```'
    echo "$POST" | head -100
    echo '```'
    echo "</details>"
  fi
  echo
  echo "## Verdict"
  TOTAL_ERR=$((PRE_ERRORS + POST_ERRORS))
  FAILED_STAGES=""
  [[ "$PRE_STATUS" == "MCP_FAILURE" ]] && FAILED_STAGES="$FAILED_STAGES pre-play"
  [[ $DO_PLAY -eq 1 && "$PLAY_STATUS" == "MCP_FAILURE" ]] && FAILED_STAGES="$FAILED_STAGES play-trigger"
  [[ $DO_PLAY -eq 1 && "$TEST_STATUS" == "MCP_FAILURE" ]] && FAILED_STAGES="$FAILED_STAGES base-mechanics"
  [[ $DO_PLAY -eq 1 && "$POST_STATUS" == "MCP_FAILURE" ]] && FAILED_STAGES="$FAILED_STAGES post-play"
  if [[ -n "$FAILED_STAGES" ]]; then
    echo "**UNKNOWN — Unity MCP failed on stages:$FAILED_STAGES**"
    echo
    echo "Cause probable : Unity Editor busy (compile / play / scene transition) ou MCP server disconnect. Retry quand Unity stable Edit mode."
    echo "0 errors/warnings count IS NOT trustworthy — no real read happened."
  elif [[ $TOTAL_ERR -eq 0 ]]; then
    echo "**PASS — 0 errors detected via real MCP read. Warnings $((PRE_WARNS + POST_WARNS)).**"
  else
    echo "**FAIL — $TOTAL_ERR errors (pre=$PRE_ERRORS, post=$POST_ERRORS). Investigate.**"
  fi
} > "$REPORT_OUT"

echo "DONE. Report: $REPORT_OUT"
if [[ -n "$FAILED_STAGES" ]]; then
  echo "VERDICT: UNKNOWN (MCP failure)"
  exit 2
fi
TOTAL_ERR=$((PRE_ERRORS + POST_ERRORS))
[[ $TOTAL_ERR -eq 0 ]] && exit 0 || exit 1
