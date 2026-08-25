#!/bin/sh
# Verifies that Unity's API updater rewrites NGO 2.x editor API references to their NGO 3.x
# Unity.Netcode.GameObjects.Editor equivalents. macOS and Linux; see run-upgrade-test.ps1 for Windows.
#
# Runs the editor over this project in batch mode with -accept-apiupdate, then asserts that every
# Unity.Netcode.Editor reference under Assets/Editor was rewritten and that no stale reference
# survived. The 2.x sources are restored on exit so the test can be re-run.
#
#   ./run-upgrade-test.sh
#   ./run-upgrade-test.sh --unity /Applications/Unity/Hub/Editor/6000.6.0b5/Unity.app/Contents/MacOS/Unity --clean
#
#   --unity <path>          Editor binary. Defaults to $UNITY_EDITOR_PATH, then to the hub install
#                           matching ProjectSettings/ProjectVersion.txt.
#   --clean                 Delete Library and Temp first, for a cold import.
#   --keep-updated-sources  Leave the rewritten sources in place instead of restoring the originals.

set -eu

PROJECT_PATH=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
SOURCE_DIR="$PROJECT_PATH/Assets/Editor"
LOG_FILE="$PROJECT_PATH/upgrade-test.log"

UNITY_EXE=""
CLEAN=0
KEEP_UPDATED_SOURCES=0

while [ $# -gt 0 ]; do
    case "$1" in
        --unity)
            [ $# -ge 2 ] || { echo "--unity requires a path" >&2; exit 2; }
            UNITY_EXE="$2"; shift 2 ;;
        --clean)                CLEAN=1; shift ;;
        --keep-updated-sources) KEEP_UPDATED_SOURCES=1; shift ;;
        -h|--help)              sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *)                      echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

# Every 2.x type the sources reference, and what the updater is expected to turn it into.
# Frozen: this is the public editor API of develop-2.0.0, which is released and will not change.
# Extend it by hand if a public editor type is ever relocated again within 3.x.
EXPECTED_TYPES="
Unity.Netcode.Editor.HiddenScriptEditor
Unity.Netcode.Editor.UnityTransportEditor
Unity.Netcode.Editor.NetworkAnimatorEditor
Unity.Netcode.Editor.NetworkRigidbodyEditor
Unity.Netcode.Editor.NetworkRigidbody2DEditor
Unity.Netcode.Editor.NetcodeEditorBase
Unity.Netcode.Editor.NetworkBehaviourEditor
Unity.Netcode.Editor.NetworkManagerEditor
Unity.Netcode.Editor.NetworkManagerHelper
Unity.Netcode.Editor.NetworkObjectEditor
Unity.Netcode.Editor.NetworkRigidbodyBaseEditor
Unity.Netcode.Editor.NetworkTransformEditor
Unity.Netcode.Editor.NetworkPrefabsEditor
Unity.Netcode.Editor.Configuration.NetcodeForGameObjectsProjectSettings
Unity.Netcode.Editor.Configuration.NetworkPrefabProcessor
"

resolve_unity() {
    if [ -n "$UNITY_EXE" ]; then
        [ -x "$UNITY_EXE" ] || { echo "Editor not found or not executable: $UNITY_EXE" >&2; exit 1; }
        echo "$UNITY_EXE"; return
    fi
    if [ -n "${UNITY_EDITOR_PATH:-}" ]; then
        [ -x "$UNITY_EDITOR_PATH" ] || { echo "UNITY_EDITOR_PATH is not executable: $UNITY_EDITOR_PATH" >&2; exit 1; }
        echo "$UNITY_EDITOR_PATH"; return
    fi

    version=$(sed -n 's/^m_EditorVersion:[[:space:]]*\(.*\)$/\1/p' \
        "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt" | tr -d '\r' | head -n 1)
    [ -n "$version" ] || { echo "Could not read m_EditorVersion from ProjectSettings/ProjectVersion.txt" >&2; exit 1; }

    # Default hub locations differ per platform, and the macOS editor lives inside the .app bundle.
    case "$(uname -s)" in
        Darwin) candidate="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" ;;
        *)      candidate="$HOME/Unity/Hub/Editor/$version/Editor/Unity" ;;
    esac
    [ -x "$candidate" ] && { echo "$candidate"; return; }

    echo "No editor found for $version at $candidate. Pass --unity or set UNITY_EDITOR_PATH." >&2
    exit 1
}

UNITY=$(resolve_unity)
echo "Editor:  $UNITY"
echo "Project: $PROJECT_PATH"

BACKUP_DIR=$(mktemp -d "${TMPDIR:-/tmp}/ngo-apiupdater-XXXXXX")
cp -R "$SOURCE_DIR/." "$BACKUP_DIR/"

# Restore on any exit path, so an interrupted run does not leave rewritten sources behind.
# Guarded: trapping both EXIT and INT/TERM means this can be entered twice.
CLEANED=0
cleanup() {
    [ "$CLEANED" -eq 0 ] || return 0
    CLEANED=1
    if [ "$KEEP_UPDATED_SOURCES" -eq 0 ]; then
        cp -R "$BACKUP_DIR/." "$SOURCE_DIR/"
        rm -rf "$BACKUP_DIR"
    fi
}
trap cleanup EXIT INT TERM

if [ "$CLEAN" -eq 1 ]; then
    for stale in Library Temp; do
        if [ -d "$PROJECT_PATH/$stale" ]; then
            echo "Removing $stale ..."
            rm -rf "$PROJECT_PATH/$stale"
        fi
    done
fi

rm -f "$LOG_FILE"

echo "Running the editor (this imports the project and runs the API updater)..."
# The editor returns non-zero when compilation fails, which is the normal state before the updater
# rewrites the sources, so do not let set -e abort here.
EDITOR_STATUS=0
"$UNITY" \
    -batchmode -nographics -quit \
    -accept-apiupdate \
    -ignoreCompilerErrors \
    -burst-disable-compilation \
    -projectPath "$PROJECT_PATH" \
    -logFile "$LOG_FILE" || EDITOR_STATUS=$?
echo "Editor exit code: $EDITOR_STATUS"

ALL_TEXT=$(cat "$SOURCE_DIR"/*.cs)

TOTAL=0
FAILURES=0
printf '\n%-72s %8s %6s  %s\n' "TYPE" "UPDATED" "STALE" "RESULT"
for old in $EXPECTED_TYPES; do
    TOTAL=$((TOTAL + 1))
    new="Unity.Netcode.GameObjects.${old#Unity.Netcode.}"

    updated=$(printf '%s' "$ALL_TEXT" | grep -o -F "$new" | wc -l | tr -d ' ')
    # The old name only ever survives as a distinct token; a trailing word char or dot means it is
    # really part of the longer new name, so exclude those.
    stale=$(printf '%s' "$ALL_TEXT" | grep -oE "$(printf '%s' "$old" | sed 's/\./\\./g')([^A-Za-z0-9_.]|$)" | wc -l | tr -d ' ')

    if [ "$updated" -gt 0 ] && [ "$stale" -eq 0 ]; then
        result="PASS"
    else
        result="FAIL"
        FAILURES=$((FAILURES + 1))
    fi
    printf '%-72s %8s %6s  %s\n' "$old" "$updated" "$stale" "$result"
done

echo ""
if [ "$FAILURES" -eq 0 ]; then
    echo "PASS: all $TOTAL deprecated editor types were rewritten."
else
    echo "FAIL: $FAILURES of $TOTAL types were not rewritten. See $LOG_FILE"
fi

if [ "$KEEP_UPDATED_SOURCES" -eq 1 ]; then
    echo "Rewritten sources left in place under Assets/Editor (backup: $BACKUP_DIR)."
fi

[ "$FAILURES" -eq 0 ] || exit 1
exit 0
