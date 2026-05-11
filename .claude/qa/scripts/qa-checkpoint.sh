#!/usr/bin/env bash
# qa-checkpoint.sh — QA-2 per-commit checkpoint for multi-axis swarm
#
# Usage : ./qa-checkpoint.sh <axis-name> [<commit-sha>]
#
# Runs the QA-2 protocol from .claude/coordination/qa-gates.md :
#   1. Lists files touched by the given commit (or HEAD if not specified)
#   2. Checks none are in HOT ZONES (see file-ownership.md)
#   3. Checks all touched files are within the axis's exclusive write zone
#   4. Greps for forbidden patterns : Debug.Log without #if guard, etc.
#   5. Writes a report to .claude/coordination/qa-reports/{axis}-{sha}.md
#
# Exit codes :
#   0 — PASS (no issues found)
#   1 — FAIL (hot zone violation, ownership violation, or fatal lint error)
#   2 — WARN (soft issues only — review manually)
#
# Compilation check : this script does NOT run Unity (too slow for per-commit).
# The Sub-Opus must trigger `mcp__UnityMCP__refresh_unity` + `read_console` separately
# OR rely on the Unity Editor session running in background.

set -euo pipefail

# === Args ===
AXIS="${1:-}"
SHA="${2:-HEAD}"

if [[ -z "$AXIS" ]]; then
    echo "Usage: $0 <axis-name> [<commit-sha>]" >&2
    echo "  axis-name : visual-core | audio | asset-gen | content | build | ux | qa" >&2
    exit 1
fi

# === Paths ===
REPO_ROOT="$(git rev-parse --show-toplevel)"
COORD_DIR="$REPO_ROOT/.claude/coordination"
REPORT_DIR="$COORD_DIR/qa-reports"
OWNERSHIP="$COORD_DIR/file-ownership.md"
CONTRACTS="$COORD_DIR/api-contracts.md"

mkdir -p "$REPORT_DIR"

# Resolve full SHA.
FULL_SHA=$(git rev-parse "$SHA")
SHORT_SHA="${FULL_SHA:0:7}"
REPORT="$REPORT_DIR/${AXIS}-${SHORT_SHA}.md"

# === Hot zones (from file-ownership.md) ===
HOT_ZONES=(
    "Assets/Scripts/Entities/Tower.cs"
    "Assets/Scripts/Entities/Enemy.cs"
    "Assets/Scripts/Entities/Castle.cs"
    "Assets/Scripts/Systems/WaveManager.cs"
    "Assets/Scripts/Systems/LevelRunner.cs"
    "Assets/Scripts/Systems/Economy.cs"
    "Assets/Scripts/Data/BalanceConfig.cs"
    "STATUS.md"
    "Packages/manifest.json"
)

# === Axis ownership zones (prefix-match against changed files) ===
# Files matching any of these prefixes are within the axis's exclusive zone.
# (Bash 3.2 compat — no associative arrays; case statement instead.)
get_ownership_zone() {
    case "$1" in
        visual-core)
            echo "Assets/Scripts/Visual/ Assets/Shaders/ Assets/Materials/ Assets/Prefabs/VFX/"
            ;;
        audio)
            echo "Assets/Audio/ Assets/Scripts/Systems/AudioController.cs Assets/Scripts/Data/AudioClipRegistry.cs Assets/Editor/AudioClipRegistryTool.cs Assets/ScriptableObjects/Audio/ Assets/UI/SettingsPanel/"
            ;;
        asset-gen)
            echo "tools/blender/ tools/comfy/ tools/mixamo/ Assets/Models/Enemies/ Assets/Animations/"
            ;;
        content)
            echo "Assets/ScriptableObjects/Levels/ Assets/Resources/LevelRegistry.asset docs/specs/levels/"
            ;;
        build)
            echo "Assets/Editor/BuildScript Assets/Editor/CIBuilder.cs tools/ci/ .github/workflows/ ProjectSettings/Build Build/"
            ;;
        ux)
            echo "Assets/UI/ Assets/Scripts/UI/ Assets/Resources/Localization/ Assets/Fonts/ Assets/Scripts/UI/SettingsPanel.cs"
            ;;
        qa)
            echo ".claude/qa/ Assets/Tests/ Assets/Editor/TestRunner.cs Assets/Editor/SprintGateRunner.cs Assets/Scripts/Tests/"
            ;;
        *)
            echo ""
            ;;
    esac
}

VALID_AXES="visual-core audio asset-gen content build ux qa"

# Always-allowed paths (cross-axis meta files).
ALWAYS_OK=(
    ".claude/plans/"
    ".claude/coordination/requests/"
    ".claude/coordination/qa-reports/"
    ".claude/coordination/axis-"
)

AXIS_ZONE=$(get_ownership_zone "$AXIS")
if [[ -z "$AXIS_ZONE" ]]; then
    echo "Unknown axis '$AXIS'. Valid : $VALID_AXES" >&2
    exit 1
fi

# === Collect changed files ===
# Diff vs main HEAD if axis branch, else diff vs HEAD~1.
DIFF_BASE="main"
if ! git rev-parse --verify "$DIFF_BASE" >/dev/null 2>&1; then
    DIFF_BASE="HEAD~1"
fi

CHANGED_FILES=$(git diff --name-only "$DIFF_BASE...$FULL_SHA" 2>/dev/null || git diff --name-only "$FULL_SHA~1" "$FULL_SHA")

# === Checks ===
PASS=true
WARN=false
HOT_VIOLATIONS=()
OWNERSHIP_VIOLATIONS=()
LINT_WARNINGS=()

while IFS= read -r file; do
    [[ -z "$file" ]] && continue

    # 1. Hot zone check.
    for hot in "${HOT_ZONES[@]}"; do
        if [[ "$file" == "$hot" ]]; then
            HOT_VIOLATIONS+=("$file")
            PASS=false
        fi
    done

    # 2. Ownership zone check : file must match either ALWAYS_OK or AXIS_ZONE prefixes.
    in_zone=false
    for ok in "${ALWAYS_OK[@]}"; do
        if [[ "$file" == "$ok"* ]]; then in_zone=true; break; fi
    done
    if ! $in_zone; then
        for zone in $AXIS_ZONE; do
            if [[ "$file" == "$zone"* ]]; then in_zone=true; break; fi
        done
    fi

    # Check if already flagged as hot zone violation.
    is_hot=false
    for hot in "${HOT_ZONES[@]}"; do
        [[ "$file" == "$hot" ]] && is_hot=true
    done

    if ! $in_zone && ! $is_hot; then
        OWNERSHIP_VIOLATIONS+=("$file")
        PASS=false
    fi

    # 3. Lint : Debug.Log without #if guard (warn, not block).
    # Skip Editor-only paths (Editor/ + Tests/) — these never ship in player builds.
    is_editor_only=false
    case "$file" in
        Assets/Editor/*|*/Editor/*|Assets/Tests/*) is_editor_only=true ;;
    esac
    if [[ "$file" == *.cs ]] && [[ -f "$REPO_ROOT/$file" ]] && ! $is_editor_only; then
        # Find Debug.Log lines that are NOT inside an #if UNITY_EDITOR or DEVELOPMENT_BUILD block.
        # Simple heuristic : look for Debug.Log without preceding #if line in the same paragraph.
        unguarded=$(grep -nE "^\s*(UnityEngine\.)?Debug\.Log" "$REPO_ROOT/$file" 2>/dev/null || true)
        if [[ -n "$unguarded" ]]; then
            # Count guarded vs unguarded contextually : if file contains #if UNITY_EDITOR before
            # at least one Debug.Log we accept it (loose check, full AST analysis would be overkill).
            if ! grep -q "#if UNITY_EDITOR\|#if DEVELOPMENT_BUILD" "$REPO_ROOT/$file" 2>/dev/null; then
                LINT_WARNINGS+=("$file: Debug.Log without #if UNITY_EDITOR/DEVELOPMENT_BUILD guard")
                WARN=true
            fi
        fi
    fi
done <<< "$CHANGED_FILES"

# === Determine final status ===
if ! $PASS; then
    STATUS="FAIL"
elif $WARN; then
    STATUS="WARN"
else
    STATUS="PASS"
fi

# === Write report ===
{
    echo "# QA-2 Checkpoint — $AXIS @ $SHORT_SHA"
    echo ""
    echo "- **Date** : $(date '+%Y-%m-%d %H:%M:%S')"
    echo "- **Axis** : $AXIS"
    echo "- **Commit** : \`$FULL_SHA\`"
    echo "- **Diff base** : \`$DIFF_BASE\`"
    echo "- **Status** : **$STATUS**"
    echo ""
    echo "## Changed files"
    echo ""
    if [[ -z "$CHANGED_FILES" ]]; then
        echo "_No files changed (empty diff)._"
    else
        echo "\`\`\`"
        echo "$CHANGED_FILES"
        echo "\`\`\`"
    fi
    echo ""
    echo "## Hot zone violations (BLOCK)"
    echo ""
    if [[ ${#HOT_VIOLATIONS[@]} -eq 0 ]]; then
        echo "_None._"
    else
        for v in "${HOT_VIOLATIONS[@]}"; do echo "- \`$v\`"; done
    fi
    echo ""
    echo "## Ownership violations (BLOCK)"
    echo ""
    if [[ ${#OWNERSHIP_VIOLATIONS[@]} -eq 0 ]]; then
        echo "_None._"
    else
        for v in "${OWNERSHIP_VIOLATIONS[@]}"; do echo "- \`$v\` not in axis '$AXIS' zone"; done
        echo ""
        echo "Allowed prefixes for axis \`$AXIS\` :"
        for zone in $AXIS_ZONE; do echo "- \`$zone\`"; done
    fi
    echo ""
    echo "## Lint warnings (SOFT)"
    echo ""
    if [[ ${#LINT_WARNINGS[@]} -eq 0 ]]; then
        echo "_None._"
    else
        for w in "${LINT_WARNINGS[@]}"; do echo "- $w"; done
    fi
    echo ""
    echo "## Next steps"
    echo ""
    case "$STATUS" in
        PASS) echo "- Push the commit. Sub-Opus continues."  ;;
        WARN) echo "- Review warnings. Sub-Opus may continue or fix-forward."  ;;
        FAIL) echo "- BLOCKED. Sub-Opus must revert or relocate offending files before continuing."  ;;
    esac
} > "$REPORT"

echo "[qa-checkpoint] $STATUS — report written to $REPORT"

case "$STATUS" in
    PASS) exit 0 ;;
    WARN) exit 2 ;;
    FAIL) exit 1 ;;
esac
