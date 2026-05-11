#!/usr/bin/env bash
set -e

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="/Users/mike/Work/crowd-defense"
LOOP_INTERVAL=480  # 8 min

while true; do
    cd "$PROJECT_PATH"

    # Check si commits récents (par rapport au dernier deploy)
    LAST_DEPLOY_HASH=$(cd /private/tmp/crowd-defense-v6 && git log --oneline -1 v6/ | head -1)
    LAST_MAIN=$(git log --oneline -1 main | head -1)

    echo "[$(date)] Last deploy: $LAST_DEPLOY_HASH"
    echo "[$(date)] Latest main: $LAST_MAIN"

    # Check si Unity batch is running
    if pgrep -f "Unity.app.*batchmode" > /dev/null; then
        echo "[$(date)] Unity batch already running, skipping"
    else
        # Lance build
        rm -f "$PROJECT_PATH/Temp/UnityLockfile" 2>/dev/null
        LOG_FILE="/tmp/auto-build-$(date +%H%M%S).log"
        echo "[$(date)] Starting build → $LOG_FILE"
        "$UNITY_PATH" -batchmode -nographics -projectPath "$PROJECT_PATH" \
            -executeMethod CrowdDefense.Editor.BatchRebuild.SetupAndBuild \
            -logFile "$LOG_FILE" -quit 2>&1 | tail -3 &
        BUILD_PID=$!
        wait $BUILD_PID

        # Deploy si success
        if grep -q "Build Succeeded" "$LOG_FILE"; then
            rsync -av --delete "$PROJECT_PATH/Builds/WebGL/" /private/tmp/crowd-defense-v6/v6/
            cd /private/tmp/crowd-defense-v6
            git add v6/
            git commit -m "deploy: auto-build $(date +%H%M)" || true
            git push origin gh-pages 2>&1 | tail -2
            echo "[$(date)] Deploy OK"
        else
            echo "[$(date)] Build FAILED, will retry next loop"
        fi
    fi

    sleep $LOOP_INTERVAL
done
