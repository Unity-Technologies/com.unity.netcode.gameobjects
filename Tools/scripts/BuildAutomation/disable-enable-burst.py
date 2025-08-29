# An example usage would be "- python Tools/CI/Netcode/BuildAutomations/disable-enable-burst.py --project-path {{ project.path }} --platform WebGL"
# This file aims to modify BurstAotSettings file which should be present under ProjectSettings folder.
# Note that this requires Burst package to be installed as well as you need to specify the platform for which you are building since there are different settings for each. (this is taken from environment variable)
# This script is not overriding existing settings file but completely replacing it

import argparse
import json
import os

# Function that parses arguments of the script
def parse_args():
    global args
    parser = argparse.ArgumentParser(description="Enable or disable Burst compilation and specify Unity project details.")

    # Add the mutually exclusive group for --disable-burst and --enable-burst
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--disable-burst', action='store_true', help='Disable Burst compilation.')
    group.add_argument('--enable-burst', action='store_true', help='Enable Burst compilation.')

    # Add additional arguments
    parser.add_argument('--project-path', required=True, help='Specify the location of the Unity project.')

    args = parser.parse_args()


# This function creates a new burst settings file with default values. Notice that this should almost always not be used since assumption is that in our case we have projects with Burst preinstalled
# For the "default" values I used values from NetcodeSamples project in DOTS-monorepo
def create_config(settings_path):
    config_name = os.path.join(settings_path, 'BurstAotSettings_{}.json'.format(resolve_target()))
    monobehaviour = {
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

    data = {'MonoBehaviour': monobehaviour}
    with open(config_name, 'w') as f:
        json.dump(data, f)
    return config_name


# Burst has specific files for each platform, so we need to resolve the target platform to get the correct settings file.
# Note that this jobs uses environment variables to pass parameters to the script.
def resolve_target():
    # Get the platform value from the environment variable
    platform_key = os.environ.get('PLATFORM_WIN64_MAC_ANDROID')

    resolved_target = platform_key
    if 'win64' == platform_key:
        resolved_target = 'StandaloneWindows'
    elif 'mac' == platform_key:
        resolved_target = 'StandaloneOSX'
    elif 'android' == platform_key:
        resolved_target = 'Android'
    else:
        raise ValueError("Unsupported platform: {}".format(platform) + "Check if you are passing correct argument for one of the supported platforms: StandaloneWindows or StandaloneLinux")

    return resolved_target


# This function either returns existing burst settings or creates new if a file was not found
def get_or_create_burst_AOT_config():
    settings_path = os.path.join(args.project_path, 'ProjectSettings')
    if not os.path.isdir(settings_path):
        os.mkdir(settings_path)
    config_names = [os.path.join(settings_path, filename) for filename in os.listdir(settings_path) if filename.startswith("BurstAotSettings_{}".format(resolve_target()))]
    if not config_names:
        return [create_config(settings_path)]
    return config_names


# Function that sets the AOT status in the burst settings file (essentially enables or disables burst compilation)
def set_burst_AOT(config_file, status):
    config = None
    with open(config_file, 'r') as f:
        config = json.load(f)

    assert config is not None, 'AOT settings not found; did the burst-enabled build finish successfully?'

    config['MonoBehaviour']['EnableBurstCompilation'] = status
    with open(config_file, 'w') as f:
        json.dump(config, f)


def main():
    parse_args()
    config_names = get_or_create_burst_AOT_config()

    platform_key = os.environ.get('PLATFORM_WIN64_MAC_ANDROID')
    print(f"Burst compilation script: Unity project path is {args.project_path}")
    print(f"Burst compilation script: Target platform is {platform_key}")

    if args.disable_burst:
        print('BURST COMPILATION: DISABLED')

        for config_name in config_names:
            set_burst_AOT(config_name, False)

    elif args.enable_burst:
        print('BURST COMPILATION: ENABLED')

        for config_name in config_names:
            set_burst_AOT(config_name, True)

    else:
        sys.exit('BURST COMPILATION: unexpected value: {}'.format(args.enable_burst))



if __name__ == '__main__':
    main()
