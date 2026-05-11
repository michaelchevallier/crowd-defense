#!/usr/bin/env bash
# Unity Watchdog: kills hanging Unity batch + relaunches.
# Triggers if:
#  - Unity batchmode PID > 10 min with log file > 500 MB (runaway shader stripping loop)
#  - OR Unity batchmode PID > 10 min with no log activity (frozen)

set -e

PROJECT_PATH="/Users/mike/Work/crowd-defense"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
LOG_DIR="/tmp"
MAX_LOG_SIZE=$((500 * 1024 * 1024))  # 500 MB
MAX_AGE_SECONDS=600  # 10 min
RELAUNCH_AFTER_KILL=1

now=$(date +%s)

# Find Unity batch processes
UNITY_PIDS=$(pgrep -f "Unity.app.*batchmode" || true)

if [ -z "$UNITY_PIDS" ]; then
    echo "[$(date)] No Unity batch running. OK."
    exit 0
fi

for pid in $UNITY_PIDS; do
    # Get process start time
    started=$(ps -p $pid -o lstart= 2>/dev/null)
    if [ -z "$started" ]; then continue; fi
    started_epoch=$(date -j -f "%a %b %e %H:%M:%S %Y" "$started" +%s 2>/dev/null || echo 0)
    age=$((now - started_epoch))

    # Find associated log file
    log_arg=$(ps -p $pid -o command= | grep -oE "logFile [^ ]+" | awk '{print $2}')
    log_size=0
    if [ -n "$log_arg" ] && [ -f "$log_arg" ]; then
        log_size=$(stat -f%z "$log_arg" 2>/dev/null || echo 0)
    fi

    echo "[$(date)] Unity PID $pid age=${age}s log=${log_size}B"

    HANG=0
    if [ $age -gt $MAX_AGE_SECONDS ] && [ $log_size -gt $MAX_LOG_SIZE ]; then
        echo "[$(date)] HANG DETECTED: log runaway ($log_size bytes)"
        HANG=1
    elif [ $age -gt $MAX_AGE_SECONDS ]; then
        # Check log activity in last 60s
        if [ -n "$log_arg" ] && [ -f "$log_arg" ]; then
            mtime_epoch=$(stat -f%m "$log_arg" 2>/dev/null || echo 0)
            silence=$((now - mtime_epoch))
            if [ $silence -gt 120 ]; then
                echo "[$(date)] HANG DETECTED: log silent ${silence}s"
                HANG=1
            fi
        fi
    fi

    if [ $HANG -eq 1 ]; then
        echo "[$(date)] KILLING $pid + all Unity helpers"
        pkill -9 -f "Unity.app.*batchmode" 2>/dev/null || true
        pkill -9 -f "UnityShaderCompiler" 2>/dev/null || true
        pkill -9 -f "Unity.ILPP" 2>/dev/null || true
        sleep 2
        rm -f "$PROJECT_PATH/Temp/UnityLockfile" 2>/dev/null || true

        if [ $RELAUNCH_AFTER_KILL -eq 1 ]; then
            LOG_FILE="$LOG_DIR/unity-watchdog-relaunch-$(date +%H%M%S).log"
            echo "[$(date)] RELAUNCHING via watchdog → $LOG_FILE"
            nohup "$UNITY_PATH" -batchmode -nographics \
                -projectPath "$PROJECT_PATH" \
                -executeMethod CrowdDefense.Editor.BatchRebuild.SetupAndBuild \
                -logFile "$LOG_FILE" -quit > /dev/null 2>&1 &
            disown
        fi
    fi
done
