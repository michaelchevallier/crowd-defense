#!/bin/bash
# notify.sh — wrapper de notification superviseur multi-canal
#
# Usage: notify.sh <TIER> <TITLE> <MESSAGE> [sound]
#   TIER    : T1 (urgent, sound Submarine) | T2 (batched, no sound) | T3 (silent log only)
#   TITLE   : titre notif (max 50 chars suggéré)
#   MESSAGE : body notif (max 200 chars, mobile truncate sinon)
#   sound   : macOS sound name override (default: Submarine pour T1, silent pour T2)
#
# Canaux push selon TIER :
#   T1 → macOS osascript (avec son) + ntfy.sh (si NTFY_TOPIC env var set, priority high)
#   T2 → macOS osascript (sans son) + ntfy.sh (si configured, priority default)
#   T3 → silent, juste append _clean-log.md (pas de canal externe)
#
# Notes :
#   - PushNotification (Claude Code tool) doit être appelé séparément par Opus en tool call,
#     ce script ne peut pas l'invoquer (limitation : tool exclusif au runtime Claude).
#   - osascript bypass terminal focus → marche même si Claude Code en avant-plan.
#   - ntfy.sh : cross-device (iOS/Android/desktop), nécessite topic + app subscribe côté Mike.
#   - Pour activer ntfy : `export NTFY_TOPIC="mike-supervisor-<random>"` dans shell parent.

set -euo pipefail

TIER="${1:-T3}"
TITLE="${2:-Supervisor}"
MESSAGE="${3:-(no message)}"
SOUND_OVERRIDE="${4:-}"

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
LOG_FILE="$REPO_ROOT/.claude/supervisor/drift-reports/_clean-log.md"
TIMESTAMP="$(date '+%Y-%m-%d %Hh%M')"

case "$TIER" in
  T1)
    SOUND_DEFAULT="Submarine"
    NTFY_PRIORITY="high"
    ;;
  T2)
    SOUND_DEFAULT=""
    NTFY_PRIORITY="default"
    ;;
  T3)
    # Silent — juste log, aucun canal externe
    if [ -f "$LOG_FILE" ]; then
      echo "" >> "$LOG_FILE"
      echo "$TIMESTAMP — [T3 silent] $TITLE — $MESSAGE" >> "$LOG_FILE"
    fi
    echo "[T3] silent log only: $TITLE"
    exit 0
    ;;
  *)
    echo "ERROR: TIER invalid: $TIER (must be T1, T2, T3)" >&2
    exit 1
    ;;
esac

SOUND="${SOUND_OVERRIDE:-$SOUND_DEFAULT}"

# === Canal 1 : macOS desktop notif ===
# Preference order :
#   1. terminal-notifier (si installé via brew) — supporte -sender pour click → iTerm/Claude
#   2. osascript fallback — warning : click "Show" ouvre Script Editor (limitation macOS)
TITLE_ESCAPED="$(echo "$TITLE" | sed 's/"/\\"/g')"
MESSAGE_ESCAPED="$(echo "$MESSAGE" | sed 's/"/\\"/g')"

if command -v terminal-notifier >/dev/null 2>&1; then
  # Sender = iTerm (Mike's terminal) so click on notif opens iTerm instead of Script Editor
  if [ -n "$SOUND" ]; then
    terminal-notifier \
      -title "$TITLE" \
      -subtitle "$TIER" \
      -message "$MESSAGE" \
      -sound "$SOUND" \
      -sender "com.googlecode.iterm2" \
      -ignoreDnD > /dev/null 2>&1 || true
  else
    terminal-notifier \
      -title "$TITLE" \
      -subtitle "$TIER" \
      -message "$MESSAGE" \
      -sender "com.googlecode.iterm2" > /dev/null 2>&1 || true
  fi
else
  # Fallback osascript — note : click "Show" ouvre Script Editor (limitation macOS, pas fixable sans tool externe)
  if [ -n "$SOUND" ]; then
    osascript -e "display notification \"$MESSAGE_ESCAPED\" with title \"$TITLE_ESCAPED\" subtitle \"$TIER (don't click Show)\" sound name \"$SOUND\"" || true
  else
    osascript -e "display notification \"$MESSAGE_ESCAPED\" with title \"$TITLE_ESCAPED\" subtitle \"$TIER (don't click Show)\"" || true
  fi
fi

# === Canal 2 : ntfy.sh (si NTFY_TOPIC env var set) ===
if [ -n "${NTFY_TOPIC:-}" ]; then
  curl -s -X POST \
    -H "Title: $TITLE" \
    -H "Priority: $NTFY_PRIORITY" \
    -H "Tags: supervisor" \
    -d "$MESSAGE" \
    "https://ntfy.sh/$NTFY_TOPIC" > /dev/null || echo "WARN: ntfy.sh push failed" >&2
fi

# === Log entry pour traçabilité ===
if [ -f "$LOG_FILE" ]; then
  echo "" >> "$LOG_FILE"
  echo "$TIMESTAMP — [$TIER notified] $TITLE — $MESSAGE" >> "$LOG_FILE"
fi

echo "Notified [$TIER]: $TITLE"
