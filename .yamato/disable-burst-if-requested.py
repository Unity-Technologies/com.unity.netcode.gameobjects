import argparse
import json
import os


args = None
platform_plugin_definition = None


def resolve_target(platform):
    resolved_target = platform
    if 'StandaloneWindows' in platform:
        resolved_target = 'StandaloneWindows'
    elif 'StandaloneLinux' in platform:
        resolved_target = 'StandaloneLinux64'

    return resolved_target


def create_config(settings_path, platform):
    config_name = os.path.join(settings_path, 'BurstAotSettings_{}.json'.format(resolve_target(platform)))
    monobehaviour = {
        'm_Enabled': True,
        'm_EditorHideFlags': 0,
        'm_Name': "",
        'm_EditorClassIdentifier': 'Unity.Burst.Editor:Unity.Burst.Editor:BurstPlatformAotSettings',
        'DisableOptimisations': False,
        'DisableSafetyChecks': True,
        'DisableBurstCompilation': False
    }

    data = {'MonoBehaviour': monobehaviour}
    with open(config_name, 'w') as f:
        json.dump(data, f)
    return config_name


def get_or_create_AOT_config(project_path, platform):
    settings_path = os.path.join(project_path, 'ProjectSettings')
    if not os.path.isdir(settings_path):
        os.mkdir(settings_path)
    config_names = [os.path.join(settings_path, filename) for filename in os.listdir(settings_path) if filename.startswith("BurstAotSettings_{}".format(resolve_target(platform)))]
    if not config_names:
        return [create_config(settings_path, platform)]
    return config_names


def disable_AOT(project_path, platform):
    config_names = get_or_create_AOT_config(project_path, platform)
    for config_name in config_names:
        set_AOT(config_name, True)


def enable_AOT(project_path, platform):
    config_names = get_or_create_AOT_config(project_path, platform)
    for config_name in config_names:
        set_AOT(config_name, False)


def set_AOT(config_file, status):
    config = None
    with open(config_file, 'r') as f:
        config = json.load(f)

    assert config is not None, 'AOT settings not found; did the burst-enabled build finish successfully?'

    config['MonoBehaviour']['DisableBurstCompilation'] = status
    with open(config_file, 'w') as f:
        json.dump(config, f)


def main():
    enable_burst = os.environ.get('ENABLE_BURST_COMPILATION', 'true').strip().lower()
    if enable_burst == 'true':
        print('BURST COMPILATION: ENABLED')
    elif enable_burst == 'false':
        print('BURST COMPILATION: DISABLED')
        disable_AOT(args.project_path, args.platform)
    else:
        sys.exit('BURST COMPILATION: unexpected value: {}'.format(enable_burst))


def parse_args():
    global args
    parser = argparse.ArgumentParser(description='This tool disables burst AOT compilation')
    parser.add_argument('--project-path', help='Specify the location of the unity project.')
    parser.add_argument('--platform', help="Platform to be used to run the build.")
    args = parser.parse_args()


if __name__ == '__main__':
    parse_args()
    main()