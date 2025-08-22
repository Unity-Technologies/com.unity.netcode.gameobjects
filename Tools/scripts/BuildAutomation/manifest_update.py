# This script is used to replace a package from the project manifest.json with a local version.
# The goal is that you will trigger the build process from your branch (for example release/1.2.3) and package from this branch will be use in the project manifest.1
# Note that for now this script is assuming that such package already has an entry in the manifest
# TODO: consider if it makes sense to just add new manifest entry (to test what?)

import json
import argparse
import os

def parse_args():
    global args
    parser = argparse.ArgumentParser(description='Update a Unity project manifest to point to the local package version.')
    parser.add_argument('--manifest-path', required=True, help='The absolute path to the project manifest.json file.')
    parser.add_argument('--package-name', default="com.unity.netcode.gameobjects", help="The name of the package to modify in the manifest.")
    parser.add_argument('--local-package-path', required=True, help='The absolute file path to the local package source directory.')
    args = parser.parse_args()

def main():
    """
    Updates a project's manifest.json to use package under passed local-package-path,
    and then prints the version of that local package.
    """
    parse_args()

    # Update the target project's manifest
    try:
        with open(args.manifest_path, 'r') as f:
            manifest_data = json.load(f)

        local_path_normalized = args.local_package_path.replace(os.sep, '/')
        manifest_data["dependencies"][args.package_name] = f"file:{local_path_normalized}"

        with open(args.manifest_path, 'w') as f:
            json.dump(manifest_data, f, indent=4)

        print(f"Successfully updated manifest at '{args.manifest_path}'")
        print(f"Set '{args.package_name}' to use local package at '{args.local_package_path}'")
    except Exception as e:
        print(f"Error updating manifest: {e}")
        exit(1)

    # --- Read and report the local package's version for log confirmation---
    # This is only for debug purposes
    try:
        # Construct the path to the local package's package.json file
        local_package_json_path = os.path.join(args.local_package_path, 'package.json')

        with open(local_package_json_path, 'r') as f:
            local_package_data = json.load(f)

        # Extract the version, providing a default if not found
        local_package_version = local_package_data.get('version', 'N/A')

        print(f"--> Verified local '{args.package-name}' version is: {local_package_version}")

    except FileNotFoundError:
        print(f"Warning: Could not find package.json at '{local_package_json_path}'")
    except Exception as e:
        print(f"Error reading local package version: {e}")

if __name__ == "__main__":
    main()
