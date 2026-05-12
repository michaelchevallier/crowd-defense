#!/usr/bin/env bash
# Runner — invokes dispatch.sh every 60s for 2h.
# Emits 1 stdout line per iteration with stats (for Monitor pickup).

set -u

REPO="/Users/mike/Work/crowd-defense"
DISPATCH="$REPO/.claude/master-dispatch/dispatch.sh"
DURATION_SEC="${1:-7200}"
MIN_AGENTS="${2:-8}"
MAX_AGENTS="${3:-12}"
INTERVAL="${4:-60}"

start_epoch=$(date +%s)
iter=0

while true; do
    now=$(date +%s)
    elapsed=$((now - start_epoch))

    if [ "$elapsed" -ge "$DURATION_SEC" ]; then
        echo "MASTER-DISPATCH-DONE elapsed=$elapsed iter=$iter"
        break
    fi

    iter=$((iter + 1))
    out=$(bash "$DISPATCH" "$iter" "$MAX_AGENTS" 2>&1 | tail -1)

    # Decision: dispatch only when below MIN_AGENTS
    active=$(echo "$out" | grep -oE "active=[0-9]+" | cut -d= -f2)
    if [ -z "$active" ]; then active=0; fi

    if [ "$active" -lt "$MIN_AGENTS" ]; then
        decision="BELOW-MIN dispatch-cycle"
    else
        decision="AT-CAPACITY observe"
    fi

    # Print compact iter status (single stdout line = single Monitor notification)
    echo "ITER=$iter elapsed=${elapsed}s active=$active $decision $(echo "$out" | sed -E 's/^\[[^]]+\] //')"

    sleep "$INTERVAL"
done
