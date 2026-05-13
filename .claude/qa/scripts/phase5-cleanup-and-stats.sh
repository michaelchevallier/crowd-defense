#!/bin/bash
# phase5-cleanup-and-stats.sh — invoke at end of Wave 2/3 before sprint-gate.
# - Cleanup worktrees orphans.
# - Pull rebase main (sync working tree with origin).
# - Count Phase 5 commits.
# - Verify LevelRegistry 90 entries.
# - Output to stdout for Opus consumption.

set -uo pipefail

REPO_ROOT="${REPO_ROOT:-/Users/mike/Work/crowd-defense}"
BASELINE_COMMIT="${BASELINE_COMMIT:-460a0b04}"

cd "$REPO_ROOT"

echo "===== Phase 5 cleanup & stats ====="
echo

echo "--- Worktree state ---"
git worktree list

echo
echo "--- Cleanup .claude/worktrees/agent-* (orphans) ---"
for wt in "$REPO_ROOT/.claude/worktrees/"agent-*; do
  [ -d "$wt" ] || continue
  echo "  pruning $wt"
  git worktree remove --force "$wt" 2>&1 || echo "    (already gone or busy)"
done
git worktree prune
echo

echo "--- Fetch + git status (no auto-pull) ---"
git fetch --quiet origin
echo "Local HEAD : $(git rev-parse HEAD)"
echo "Origin/main : $(git rev-parse origin/main)"
git status --short | head -10
echo

echo "--- Phase 5 commits count (from baseline $BASELINE_COMMIT) ---"
N_COMMITS=$(git log --oneline "$BASELINE_COMMIT"..origin/main 2>/dev/null | wc -l | tr -d ' ')
echo "Total commits since baseline : $N_COMMITS"

echo
echo "--- P0 markers ---"
git log --oneline "$BASELINE_COMMIT"..origin/main 2>/dev/null | grep -oE "P0-(LVL|UI)-[0-9]+" | sort -u

echo
echo "--- P1 markers ---"
git log --oneline "$BASELINE_COMMIT"..origin/main 2>/dev/null | grep -oE "P1-(LVL|UI|GP|EN|AST)-[0-9]+" | sort -u

echo
echo "--- LevelRegistry count ---"
N_REGISTRY=$(grep -oE 'world[0-9]+-[0-9]+' Assets/Resources/LevelRegistry.asset 2>/dev/null | sort -u | wc -l | tr -d ' ')
N_FILES=$(ls Assets/ScriptableObjects/Levels/*.asset 2>/dev/null | wc -l | tr -d ' ')
echo "Registry entries : $N_REGISTRY"
echo "Level .asset files : $N_FILES"
if [ "$N_REGISTRY" != "$N_FILES" ]; then echo "  WARNING : mismatch"; fi

echo
echo "--- Files > 1000 LOC introduced since Phase 5 ---"
git diff --stat "$BASELINE_COMMIT"..origin/main -- 'Assets/Scripts/*.cs' 2>/dev/null | awk -F'|' 'NF==2 {print $1}' | while read f; do
  f=$(echo "$f" | sed 's/ //g')
  [ -f "$f" ] || continue
  lines=$(wc -l <"$f" 2>/dev/null | tr -d ' ')
  if [ "$lines" -gt 1000 ]; then echo "  $f : $lines LOC"; fi
done

echo
echo "===== Done. ====="
