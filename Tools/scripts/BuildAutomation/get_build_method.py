"""
Executes Unity build command based on platform and scripting backend.
Determines the appropriate BuilderScripts method and runs Unity with the correct parameters.
"""

import os
import sys
import subprocess
import argparse

BUILD_CONFIGS = {
    ('win64', 'il2cpp'): {
        'method': 'BuilderScripts.BuildWinIl2cpp',
        'buildTarget': 'win64'
    },
    ('win64', 'mono'): {
        'method': 'BuilderScripts.BuildWinMono',
        'buildTarget': 'win64'
    },
    ('mac', 'il2cpp'): {
        'method': 'BuilderScripts.BuildMacIl2cpp',
        'buildTarget': 'osx'
    },
    ('mac', 'mono'): {
        'method': 'BuilderScripts.BuildMacMono',
        'buildTarget': 'osx'
    },
    ('android', 'il2cpp'): {
        'method': 'BuilderScripts.BuildAndroidIl2cpp',
        'buildTarget': 'android'
    }
}

def execute_unity_build(project_path, unity_path='C:/TestingEditor/Unity.exe'):
    platform = os.environ.get('PLATFORM_WIN64_MAC_ANDROID', '').lower()
    backend = os.environ.get('SCRIPTING_BACKEND_IL2CPP_MONO', '').lower()

    config = BUILD_CONFIGS.get((platform, backend))
    if not config:
        print(f"ERROR: Invalid combination: platform={platform}, backend={backend}", file=sys.stderr)
        sys.exit(1)

    cmd = [
        unity_path,
        '-projectPath', project_path,
        '-buildTarget', config['buildTarget'],
        '-executeMethod', config['method'],
        '-batchmode',
        '-logFile', './artifacts/UnityLog.txt',
        '-automated',
        '-crash-report-folder', './artifacts/CrashArtifacts',
        '-quit'
    ]

    print(f"Executing: {' '.join(cmd)}")
    result = subprocess.run(cmd, check=False)
    sys.exit(result.returncode)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Execute Unity build command.')
    parser.add_argument('--project-path', required=True, help='Path to Unity project.')
    parser.add_argument('--unity-path', default='C:/TestingEditor/Unity.exe', help='Path to Unity executable.')
    args = parser.parse_args()
    execute_unity_build(args.project_path, args.unity_path)
