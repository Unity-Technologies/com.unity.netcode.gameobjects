#!/usr/bin/env python3
"""
Verifies that Unity's API updater rewrites NGO 2.x API references to their NGO 3.x equivalents:
editor types to Unity.Netcode.GameObjects.Editor, and the runtime timing types to
Unity.Netcode.GameObjects.Timing. Runs on Windows, macOS and Linux.

Imports the project in batch mode with -accept-apiupdate, then asserts that every relocated
reference under Assets/Editor and Assets/Runtime was rewritten and that no stale reference
survived. The 2.x sources are restored on exit so the test can be re-run.

With --collision-stub, a stub assembly is added that occupies Unity.Netcode.NetworkTime and
Unity.Netcode.NetworkTimeSystem, standing in for a second package that has taken those names. The
expectation then inverts for exactly those two: the updater is driven by resolution failure, so a
name another assembly still resolves never reaches the MovedFrom data and cannot be migrated.
NetworkTickSystem is deliberately absent from the stub and must still migrate, which is what makes
the run prove both halves rather than merely fail.

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
SOURCE_DIRS = [os.path.join(PROJECT_PATH, 'Assets', 'Editor'),
               os.path.join(PROJECT_PATH, 'Assets', 'Runtime')]
LOG_FILE = os.path.join(PROJECT_PATH, 'upgrade-test.log')

# A directory whose name ends in '~' is not imported, so the stub is inert until it is copied in.
STUB_SOURCE = os.path.join(PROJECT_PATH, 'Assets', 'CollisionStub~')
STUB_TARGET = os.path.join(PROJECT_PATH, 'Assets', 'CollisionStub')

# Every 2.x type the sources reference, grouped by the move that relocated it. Stating the namespace
# pair once per move means a type's old and new names cannot drift apart.
#
# The two editor entries are frozen: they are the public editor API of develop-2.0.0, which is
# released and will not change. Extend a list only when a public type is relocated again within 3.x.
EXPECTED_MOVES = [
    ('Unity.Netcode.Editor', 'Unity.Netcode.GameObjects.Editor', [
        'HiddenScriptEditor',
        'UnityTransportEditor',
        'NetworkAnimatorEditor',
        'NetworkRigidbodyEditor',
        'NetworkRigidbody2DEditor',
        'NetcodeEditorBase',
        'NetworkBehaviourEditor',
        'NetworkManagerEditor',
        'NetworkManagerHelper',
        'NetworkObjectEditor',
        'NetworkRigidbodyBaseEditor',
        'NetworkTransformEditor',
        'NetworkPrefabsEditor',
    ]),
    ('Unity.Netcode.Editor.Configuration', 'Unity.Netcode.GameObjects.Editor.Configuration', [
        'NetcodeForGameObjectsProjectSettings',
        'NetworkPrefabProcessor',
    ]),
    ('Unity.Netcode', 'Unity.Netcode.GameObjects.Timing', [
        'NetworkTime',
        'NetworkTimeSystem',
        'NetworkTickSystem',
    ]),
]

# The names the --collision-stub assembly occupies; under it these must NOT be rewritten.
# Keep in sync with Assets/CollisionStub~/N4ECollisionStub.cs.
STUB_OCCUPIED = ['Unity.Netcode.NetworkTime', 'Unity.Netcode.NetworkTimeSystem']


def expected_pairs():
    """Yields (old fully qualified name, new fully qualified name) for every relocated type."""
    for old_namespace, new_namespace, names in EXPECTED_MOVES:
        for name in names:
            yield f"{old_namespace}.{name}", f"{new_namespace}.{name}"


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
    """Copies the files of a flat directory. The source directories have no subdirectories."""
    os.makedirs(to_dir, exist_ok=True)
    for entry in os.listdir(from_dir):
        source = os.path.join(from_dir, entry)
        if os.path.isfile(source):
            shutil.copy2(source, os.path.join(to_dir, entry))


def backup_sources(backup_root):
    """Copies every source directory into its own subdirectory of the backup root."""
    for source_dir in SOURCE_DIRS:
        copy_flat(source_dir, os.path.join(backup_root, os.path.basename(source_dir)))


def restore_sources(backup_root):
    """Restores every source directory from the backup root."""
    for source_dir in SOURCE_DIRS:
        copy_flat(os.path.join(backup_root, os.path.basename(source_dir)), source_dir)


def read_sources():
    """Returns the concatenated text of every .cs file in the source directories."""
    parts = []
    for source_dir in SOURCE_DIRS:
        for entry in sorted(os.listdir(source_dir)):
            if entry.endswith('.cs'):
                with open(os.path.join(source_dir, entry), encoding='utf-8-sig') as handle:
                    parts.append(handle.read())
    return '\n'.join(parts)


def install_stub():
    """Copies the collision stub into Assets so the editor imports it."""
    if not os.path.isdir(STUB_SOURCE):
        sys.exit(f"Collision stub not found at {STUB_SOURCE}")
    shutil.copytree(STUB_SOURCE, STUB_TARGET, dirs_exist_ok=True)
    print(f"Installed the collision stub at {STUB_TARGET}")


def remove_stub():
    """Removes the collision stub and the .meta the editor generated beside it."""
    shutil.rmtree(STUB_TARGET, ignore_errors=True)
    generated_meta = STUB_TARGET + '.meta'
    if os.path.isfile(generated_meta):
        os.remove(generated_meta)


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


def assert_rewritten(collision_stub):
    """
    Prints a per-type result table and returns the number of types whose outcome was not the expected
    one. Under the collision stub the expectation inverts for the names the stub occupies.
    """
    all_text = read_sources()

    # Both counts need a trailing-token boundary: a following word character or dot means the match
    # is really part of a longer name. Without it 'Unity.Netcode.NetworkTime' also counts every
    # 'Unity.Netcode.NetworkTimeSystem'.
    boundary = r'(?![\w.])'

    failures = 0
    print(f"\n{'TYPE':<72} {'UPDATED':>8} {'STALE':>6} {'EXPECT':>8}  RESULT")
    for old, new in expected_pairs():
        updated = len(re.findall(re.escape(new) + boundary, all_text))
        stale = len(re.findall(re.escape(old) + boundary, all_text))

        blocked = collision_stub and old in STUB_OCCUPIED
        if blocked:
            # The old name still resolves to the stub, so the reference must have been left alone.
            passed = updated == 0 and stale > 0
        else:
            passed = updated > 0 and stale == 0

        if not passed:
            failures += 1
        expect = 'blocked' if blocked else 'moved'
        print(f"{old:<72} {updated:>8} {stale:>6} {expect:>8}  {'PASS' if passed else 'FAIL'}")

    return failures


def main():
    parser = argparse.ArgumentParser(
        description="Verifies that Unity's API updater migrates NGO 2.x API references to 3.x.")
    parser.add_argument('--unity', default='',
                        help='Editor binary. Defaults to UNITY_EDITOR_PATH, then to the hub install '
                             'matching ProjectSettings/ProjectVersion.txt.')
    parser.add_argument('--clean', action='store_true',
                        help='Delete Library and Temp first, for a cold import.')
    parser.add_argument('--keep-updated-sources', action='store_true',
                        help='Leave the rewritten sources in place instead of restoring the originals.')
    parser.add_argument('--collision-stub', action='store_true',
                        help='Add an assembly occupying Unity.Netcode.NetworkTime and '
                             'NetworkTimeSystem, and assert those two are NOT migrated while '
                             'NetworkTickSystem still is.')
    args = parser.parse_args()

    unity = resolve_unity(args.unity)
    print(f"Editor:  {unity}")
    print(f"Project: {PROJECT_PATH}")

    total = sum(1 for _ in expected_pairs())
    backup_dir = tempfile.mkdtemp(prefix='ngo-apiupdater-')
    backup_sources(backup_dir)

    try:
        if args.clean:
            for stale in ('Library', 'Temp'):
                target = os.path.join(PROJECT_PATH, stale)
                if os.path.isdir(target):
                    print(f"Removing {stale} ...")
                    purge_tree(target)

        if args.collision_stub:
            install_stub()

        run_editor(unity)
        failures = assert_rewritten(args.collision_stub)

        print('')
        if failures == 0:
            print(f"PASS: all {total} relocated types behaved as expected.")
        else:
            print(f"FAIL: {failures} of {total} types did not. See {LOG_FILE}")

        if args.keep_updated_sources:
            dirs = ', '.join(os.path.basename(d) for d in SOURCE_DIRS)
            print(f"Rewritten sources left in place under Assets/{{{dirs}}} (backup: {backup_dir}).")

        return 0 if failures == 0 else 1
    finally:
        # Restore on every exit path, including Ctrl-C, so an interrupted run never leaves the
        # rewritten sources or the stub behind as the next run's input.
        if args.collision_stub:
            remove_stub()
        if not args.keep_updated_sources:
            restore_sources(backup_dir)
            shutil.rmtree(backup_dir, ignore_errors=True)


if __name__ == '__main__':
    sys.exit(main())
