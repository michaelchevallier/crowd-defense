#!/usr/bin/env bash
# Master Dispatcher — spawns claude -p sub-agents in worktrees.
# Single-iteration script; runner loop invokes it every 60s.
#
# Args:
#   $1 — iteration number
#   $2 — max parallel target (default 10)

set -u

ITER="${1:-0}"
TARGET="${2:-10}"
REPO="/Users/mike/Work/crowd-defense"
TASKS_FILE="$REPO/.claude/master-dispatch/tasks.txt"
STATE_DIR="$REPO/.claude/master-dispatch"
LOG_DIR="$STATE_DIR/logs"
DISPATCHED_FILE="$STATE_DIR/dispatched.txt"
STATUS_FILE="$REPO/.claude/coordination/master-dispatch-status.md"

mkdir -p "$LOG_DIR"
touch "$DISPATCHED_FILE"

# === 1. Count active agents (output mtime < 5 min in Claude subagent jsonl) ===
SUBAGENT_DIR="/Users/mike/.claude/projects/-Users-mike-Work-crowd-defense"
ACTIVE_COUNT=0
ACTIVE_IDS=""
if [ -d "$SUBAGENT_DIR" ]; then
    while IFS= read -r f; do
        ACTIVE_COUNT=$((ACTIVE_COUNT + 1))
        name=$(basename "$f" .jsonl)
        ACTIVE_IDS="$ACTIVE_IDS $name"
    done < <(find "$SUBAGENT_DIR" -name "agent-*.jsonl" -mmin -5 2>/dev/null)
fi

# Also count our own claude -p spawns from logs/
SELF_SPAWNED=0
if [ -d "$LOG_DIR" ]; then
    SELF_SPAWNED=$(find "$LOG_DIR" -name "spawn-*.log" -mmin -5 | wc -l | tr -d ' ')
fi

TOTAL_ACTIVE=$((ACTIVE_COUNT + SELF_SPAWNED))

# === 2. Check git log for already-merged tasks ===
ALREADY_MERGED_KEYS=""
RECENT_LOG=$(git -C "$REPO" log --oneline -100 2>/dev/null || true)

# === 3. Pick tasks to dispatch ===
NEEDED=$((TARGET - TOTAL_ACTIVE))
DISPATCHED_THIS_ITER=0
SKIPPED_ALREADY=0
DISPATCHED_NEW=""

if [ "$NEEDED" -gt 0 ]; then
    while IFS='|' read -r priority key title brief agent_type; do
        [ -z "$priority" ] && continue
        [ "$DISPATCHED_THIS_ITER" -ge "$NEEDED" ] && break

        # Skip if already dispatched this session
        if grep -qxF "$key" "$DISPATCHED_FILE" 2>/dev/null; then
            continue
        fi

        # Skip if a recent commit matches the title keywords
        # Extract first 3 distinctive words from title
        title_keywords=$(echo "$title" | awk -F'[ :]' '{print $3, $4, $5}' | tr -d '|()' | head -c 60)
        if [ -n "$title_keywords" ] && echo "$RECENT_LOG" | grep -qiF "$title_keywords"; then
            SKIPPED_ALREADY=$((SKIPPED_ALREADY + 1))
            echo "$key" >> "$DISPATCHED_FILE"
            continue
        fi

        # === Spawn worktree + claude -p ===
        wt_name="md-${key}-${ITER}"
        wt_path="$REPO/.claude/worktrees/$wt_name"

        # Create worktree from current main HEAD
        if ! git -C "$REPO" worktree add "$wt_path" -b "$wt_name" 2>"$LOG_DIR/spawn-$key.err"; then
            echo "[$(date)] worktree create failed for $key" >> "$LOG_DIR/dispatch.log"
            continue
        fi

        # Compose prompt for the sub-agent
        prompt_file="$LOG_DIR/prompt-$key.md"
        cat > "$prompt_file" <<EOF
Tu es un sub-Sonnet ${agent_type} dispatché par le Master Opus pour le sprint Crowd Defense.

# Ticket : $title

# Brief
$brief

# Contraintes
- Tu es dans un worktree : $wt_path
- Modifie uniquement les fichiers nécessaires
- Build verify : compile clean (Unity batch mode via auto-build-loop pas requis sauf doute)
- Commit atomique : titre + Co-Authored-By: Claude Sonnet (Master-Dispatch)
- Push sur ta branche \`$wt_name\` ensuite Mike merge
- Si bloqué > 5 min : retourne un rapport court de blocage + ne commit rien

# Workflow
1. Read CLAUDE.md root + STATUS.md (skim)
2. Read fichiers listés dans le brief
3. Implémente fix minimal
4. git add -A && git commit -m "<type>(<scope>): <quoi>"
5. git push origin $wt_name (ou skip push, juste commit)
6. Retourne rapport 80 mots : files modifiés + commit hash + status

GO.
EOF

        log_file="$LOG_DIR/spawn-$key.log"
        echo "[$(date)] DISPATCH $key (type=$agent_type wt=$wt_name)" >> "$LOG_DIR/dispatch.log"

        # Launch claude -p in background, isolated to worktree cwd
        (
            cd "$wt_path" || exit 1
            timeout 1800 claude -p --permission-mode bypassPermissions \
                --append-system-prompt "$(cat "$prompt_file")" \
                "Exécute le ticket. Commit + push quand fini." \
                > "$log_file" 2>&1
            exit_code=$?
            echo "[$(date)] DONE $key exit=$exit_code" >> "$LOG_DIR/dispatch.log"
        ) &

        echo "$key" >> "$DISPATCHED_FILE"
        DISPATCHED_THIS_ITER=$((DISPATCHED_THIS_ITER + 1))
        DISPATCHED_NEW="$DISPATCHED_NEW $key"

        # small stagger to avoid race on worktree creation
        sleep 0.3
    done < "$TASKS_FILE"
fi

# === 4. Update status file ===
cat > "$STATUS_FILE" <<EOF
# Master Dispatch Status

> Last update : $(date)
> Iteration : $ITER

## Stats

- Target parallel : $TARGET
- Active sub-agents (mtime < 5 min) : $TOTAL_ACTIVE (parent=$ACTIVE_COUNT self-spawned=$SELF_SPAWNED)
- Dispatched this iter : $DISPATCHED_THIS_ITER
- Skipped (already merged) : $SKIPPED_ALREADY
- Cumulative dispatched : $(wc -l < "$DISPATCHED_FILE" | tr -d ' ')

## Dispatched this iter
$DISPATCHED_NEW

## Active subagent IDs
$ACTIVE_IDS

## ETA iso V4
Estimate : iso V4 ≈ 50-65% currently. Each successful merge bumps ~2-5%. Need ~10-15 more clean P0/P1 merges to reach 80%+ iso V4. ETA depends on Mike merge cadence — at 3-5 merges/hr post-dispatch, target T+4-6 hr.
EOF

echo "[$(date)] Iter $ITER: active=$TOTAL_ACTIVE dispatched=$DISPATCHED_THIS_ITER skipped=$SKIPPED_ALREADY"
