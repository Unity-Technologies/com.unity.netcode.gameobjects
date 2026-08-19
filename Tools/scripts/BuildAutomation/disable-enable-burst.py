"""
Script to enable or disable Burst compilation for Unity projects.
Modifies BurstAotSettings files located under the ProjectSettings folder.

Note:
- Burst package must be installed.
- Platform is specified via PLATFORM_WIN64_MAC_ANDROID env var.
- The script replaces the settings file entirely.
"""

import argparse
import json
import os

PLATFORM_MAP = {
    'win64': 'StandaloneWindows',
    'mac': 'StandaloneOSX',
    'android': 'Android'
}

DEFAULT_BURST_CONFIG = {
    'Version': 4,
    'EnableBurstCompilation': True,
    'EnableOptimisations': True,
    'EnableSafetyChecks': False,
    'EnableDebugInAllBuilds': False,
    'CpuMinTargetX32': 0,
    'CpuMaxTargetX32': 0,
    'CpuMinTargetX64': 0,
    'CpuMaxTargetX64': 0,
    'CpuTargetsX32': 6,
    'CpuTargetsX64': 72,
    'OptimizeFor': 0
}

def parse_args():
    parser = argparse.ArgumentParser(description="Enable or disable Burst compilation")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--disable-burst', action='store_true', help='Disable Burst compilation.')
    group.add_argument('--enable-burst', action='store_true', help='Enable Burst compilation.')
    parser.add_argument('--project-path', required=True, help='Project location')
    return parser.parse_args()

def resolve_target():
    platform_key = os.environ.get('PLATFORM_WIN64_MAC_ANDROID', '').lower()
    target = PLATFORM_MAP.get(platform_key)
    if not target:
        raise ValueError(f"Unsupported platform: {platform_key}. Supported: {list(PLATFORM_MAP.keys())}")
    return target

def create_config(settings_path, target):
    config_path = os.path.join(settings_path, f"BurstAotSettings_{target}.json")
    with open(config_path, 'w', encoding='UTF-8', newline='\n') as f:
        json.dump({'MonoBehaviour': DEFAULT_BURST_CONFIG}, f)
    return config_path

def get_or_create_burst_config(project_path):
    settings_path = os.path.join(project_path, 'ProjectSettings')
    os.makedirs(settings_path, exist_ok=True)

    target = resolve_target()
    prefix = f"BurstAotSettings_{target}"
    configs = [os.path.join(settings_path, f) for f in os.listdir(settings_path)
               if f.startswith(prefix) and f.endswith('.json')]

    return configs if configs else [create_config(settings_path, target)]

def set_burst_status(config_file, enabled):
    with open(config_file, 'r', encoding='utf-8') as f:
        config = json.load(f)

    if not config or 'MonoBehaviour' not in config:
        raise AssertionError('AOT settings not found')

    config['MonoBehaviour']['EnableBurstCompilation'] = enabled
    with open(config_file, 'w', encoding='UTF-8', newline='\n') as f:
        json.dump(config, f, indent=2)

def main():
    args = parse_args()
    configs = get_or_create_burst_config(args.project_path)
    platform = os.environ.get('PLATFORM_WIN64_MAC_ANDROID', 'unknown').lower()

    print(f"Burst compilation script: Unity project path is {args.project_path}")
    print(f"Burst compilation script: Target platform is {platform}")

    status = args.enable_burst
    status_text = "ENABLED" if status else "DISABLED"
    print(f'BURST COMPILATION: {status_text}')

    for config_file in configs:
        set_burst_status(config_file, status)

if __name__ == '__main__':
    main()
