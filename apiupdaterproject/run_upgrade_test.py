#!/usr/bin/env python3
"""
Verifies that Unity's API updater rewrites NGO 2.x editor API references to their NGO 3.x
Unity.Netcode.GameObjects.Editor equivalents. Runs on Windows, macOS and Linux.

Imports the project in batch mode with -accept-apiupdate, then asserts that every
Unity.Netcode.Editor reference under Assets/Editor was rewritten and that no stale reference
survived. The 2.x sources are restored on exit so the test can be re-run.

Note that this script can be run from anywhere; paths are resolved relative to the script itself.
"""

import argparse
import os
import platform
import re
import shutil
import subprocess
import sys
import tempfile

PROJECT_PATH = os.path.dirname(os.path.abspath(__file__))
SOURCE_DIR = os.path.join(PROJECT_PATH, 'Assets', 'Editor')
LOG_FILE = os.path.join(PROJECT_PATH, 'upgrade-test.log')

# Every 2.x type the sources reference. The 3.x name is derived, so the pair cannot drift.
# Frozen: this is the public editor API of develop-2.0.0, which is released and will not change.
# Extend it by hand if a public editor type is ever relocated again within 3.x.
EXPECTED_TYPES = [
    'Unity.Netcode.Editor.HiddenScriptEditor',
    'Unity.Netcode.Editor.UnityTransportEditor',
    'Unity.Netcode.Editor.NetworkAnimatorEditor',
    'Unity.Netcode.Editor.NetworkRigidbodyEditor',
    'Unity.Netcode.Editor.NetworkRigidbody2DEditor',
    'Unity.Netcode.Editor.NetcodeEditorBase',
    'Unity.Netcode.Editor.NetworkBehaviourEditor',
    'Unity.Netcode.Editor.NetworkManagerEditor',
    'Unity.Netcode.Editor.NetworkManagerHelper',
    'Unity.Netcode.Editor.NetworkObjectEditor',
    'Unity.Netcode.Editor.NetworkRigidbodyBaseEditor',
    'Unity.Netcode.Editor.NetworkTransformEditor',
    'Unity.Netcode.Editor.NetworkPrefabsEditor',
    'Unity.Netcode.Editor.Configuration.NetcodeForGameObjectsProjectSettings',
    'Unity.Netcode.Editor.Configuration.NetworkPrefabProcessor',
]


def find_editor_binary(path):
    """
    Accepts either the editor binary itself or an install root, and returns the binary.

    Layouts differ: unity-downloader-cli puts the Linux binary at <root>/Unity, the Hub puts it at
    <root>/Editor/Unity, and on macOS it is inside the .app bundle. Taking a root and searching means
    a caller never has to know which one they have.
    """
    if os.path.isfile(path):
        return path
    if not os.path.isdir(path):
        return None

    for relative in ('Unity', 'Unity.exe',
                     os.path.join('Editor', 'Unity'), os.path.join('Editor', 'Unity.exe'),
                     os.path.join('Unity.app', 'Contents', 'MacOS', 'Unity')):
        candidate = os.path.join(path, relative)
        if os.path.isfile(candidate):
            return candidate
    return None


def resolve_unity(explicit):
    """
    Returns the editor binary to run: the explicit path, else UNITY_EDITOR_PATH, else the default hub
    install matching ProjectSettings/ProjectVersion.txt. Each may name a binary or an install root.
    """
    if explicit:
        found = find_editor_binary(explicit)
        if not found:
            sys.exit(f"Editor not found: {explicit}")
        return found

    from_env = os.environ.get('UNITY_EDITOR_PATH')
    if from_env:
        found = find_editor_binary(from_env)
        if not found:
            sys.exit(f"UNITY_EDITOR_PATH does not name an editor: {from_env}")
        return found

    version_file = os.path.join(PROJECT_PATH, 'ProjectSettings', 'ProjectVersion.txt')
    version = None
    with open(version_file, encoding='utf-8-sig') as handle:
        for line in handle:
            match = re.match(r'^m_EditorVersion:\s*(\S+)', line)
            if match:
                version = match.group(1)
                break
    if not version:
        sys.exit(f"Could not read m_EditorVersion from {version_file}")

    # Default hub locations differ per platform; on macOS the binary is inside the .app bundle.
    system = platform.system()
    if system == 'Darwin':
        candidate = f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"
    elif system == 'Windows':
        candidate = f"C:\\Program Files\\Unity\\Hub\\Editor\\{version}\\Editor\\Unity.exe"
    else:
        candidate = os.path.join(os.path.expanduser('~'), 'Unity', 'Hub', 'Editor', version, 'Editor', 'Unity')

    found = find_editor_binary(candidate)
    if found:
        return found
    sys.exit(f"No editor found for {version} at {candidate}. Pass --unity or set UNITY_EDITOR_PATH.")


def purge_tree(path):
    """
    Deletes a directory tree, including one containing paths past MAX_PATH.

    Library/PackageCache holds paths the Win32 file APIs cannot delete, and a partial delete is worse
    than none: it leaves a project that fails to compile for unrelated reasons. On Windows, empty the
    tree with robocopy first, which is not subject to the limit, then drop the shallow remainder.
    """
    if not os.path.isdir(path):
        return

    if platform.system() == 'Windows':
        empty = tempfile.mkdtemp(prefix='ngo-empty-')
        try:
            # robocopy exits 0-7 for success; 8 and above is a real failure.
            result = subprocess.run(
                ['robocopy', empty, path, '/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NC', '/NS', '/NP'],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
            if result.returncode >= 8:
                sys.exit(f"robocopy failed purging {path} (exit {result.returncode})")
        finally:
            shutil.rmtree(empty, ignore_errors=True)

    shutil.rmtree(path)


def copy_flat(from_dir, to_dir):
    """Copies the files of a flat directory. Assets/Editor has no subdirectories."""
    os.makedirs(to_dir, exist_ok=True)
    for entry in os.listdir(from_dir):
        source = os.path.join(from_dir, entry)
        if os.path.isfile(source):
            shutil.copy2(source, os.path.join(to_dir, entry))


def read_sources():
    """Returns the concatenated text of every .cs file under Assets/Editor."""
    parts = []
    for entry in sorted(os.listdir(SOURCE_DIR)):
        if entry.endswith('.cs'):
            with open(os.path.join(SOURCE_DIR, entry), encoding='utf-8-sig') as handle:
                parts.append(handle.read())
    return '\n'.join(parts)


def run_editor(unity):
    """Imports the project in batch mode with the API updater enabled."""
    if os.path.exists(LOG_FILE):
        os.remove(LOG_FILE)

    print('Running the editor (this imports the project and runs the API updater)...')
    # List form, so paths containing spaces are passed as single arguments with no quoting of our own.
    result = subprocess.run([
        unity,
        '-batchmode', '-nographics', '-quit',
        '-accept-apiupdate',
        '-ignoreCompilerErrors',
        '-burst-disable-compilation',
        '-projectPath', PROJECT_PATH,
        '-logFile', LOG_FILE,
    ], check=False)
    print(f"Editor exit code: {result.returncode}")


def assert_rewritten():
    """Prints a per-type result table and returns the number of types that were not rewritten."""
    all_text = read_sources()

    failures = 0
    print(f"\n{'TYPE':<72} {'UPDATED':>8} {'STALE':>6}  RESULT")
    for old in EXPECTED_TYPES:
        new = old.replace('Unity.Netcode.', 'Unity.Netcode.GameObjects.', 1)

        updated = len(re.findall(re.escape(new), all_text))
        # The old name survives only as a distinct token: a trailing word character or dot means this
        # is really part of the longer new name.
        stale = len(re.findall(re.escape(old) + r'(?![\w.])', all_text))

        passed = updated > 0 and stale == 0
        if not passed:
            failures += 1
        print(f"{old:<72} {updated:>8} {stale:>6}  {'PASS' if passed else 'FAIL'}")

    return failures


def main():
    parser = argparse.ArgumentParser(
        description="Verifies that Unity's API updater migrates NGO 2.x editor API references to 3.x.")
    parser.add_argument('--unity', default='',
                        help='Editor binary. Defaults to UNITY_EDITOR_PATH, then to the hub install '
                             'matching ProjectSettings/ProjectVersion.txt.')
    parser.add_argument('--clean', action='store_true',
                        help='Delete Library and Temp first, for a cold import.')
    parser.add_argument('--keep-updated-sources', action='store_true',
                        help='Leave the rewritten sources in place instead of restoring the originals.')
    args = parser.parse_args()

    unity = resolve_unity(args.unity)
    print(f"Editor:  {unity}")
    print(f"Project: {PROJECT_PATH}")

    backup_dir = tempfile.mkdtemp(prefix='ngo-apiupdater-')
    copy_flat(SOURCE_DIR, backup_dir)

    try:
        if args.clean:
            for stale in ('Library', 'Temp'):
                target = os.path.join(PROJECT_PATH, stale)
                if os.path.isdir(target):
                    print(f"Removing {stale} ...")
                    purge_tree(target)

        run_editor(unity)
        failures = assert_rewritten()

        print('')
        if failures == 0:
            print(f"PASS: all {len(EXPECTED_TYPES)} deprecated editor types were rewritten.")
        else:
            print(f"FAIL: {failures} of {len(EXPECTED_TYPES)} types were not rewritten. See {LOG_FILE}")

        if args.keep_updated_sources:
            print(f"Rewritten sources left in place under Assets/Editor (backup: {backup_dir}).")

        return 0 if failures == 0 else 1
    finally:
        # Restore on every exit path, including Ctrl-C, so an interrupted run never leaves the
        # rewritten sources behind as the next run's input.
        if not args.keep_updated_sources:
            copy_flat(backup_dir, SOURCE_DIR)
            shutil.rmtree(backup_dir, ignore_errors=True)


if __name__ == '__main__':
    sys.exit(main())
