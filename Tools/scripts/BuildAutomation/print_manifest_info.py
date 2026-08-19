"""
Prints manifest.json dependencies information.
Used to verify package overrides after Unity Editor has processed the manifest.
"""

import json
import argparse
import os
import sys

def parse_args():
    parser = argparse.ArgumentParser(description='Print Unity project manifest dependencies.')
    parser.add_argument('--manifest-path', required=True,
                       help='Absolute path to project manifest.json file.')
    return parser.parse_args()

def print_manifest_dependencies(manifest_path):
    """
    Prints all dependencies from the manifest.json file in a readable format.
    """
    if not os.path.exists(manifest_path):
        print(f"ERROR: Manifest file not found at '{manifest_path}'", file=sys.stderr)
        sys.exit(1)

    try:
        with open(manifest_path, 'r', encoding='utf-8') as f:
            manifest_data = json.load(f)

        dependencies = manifest_data.get("dependencies", {})

        print("\n" + "="*80)
        print("Project dependencies in manifest.json (after Unity Editor processing):")
        print("="*80)

        if dependencies:
            file_packages = []
            version_packages = []

            for package_name, package_version in sorted(dependencies.items()):
                if package_version.startswith("file:"):
                    file_packages.append((package_name, package_version))
                else:
                    version_packages.append((package_name, package_version))

            if file_packages:
                print("\nLocal file packages (override built-in packages):")
                for package_name, package_version in file_packages:
                    print(f"  {package_name}: {package_version}")

            if version_packages:
                print("\nVersion-based packages:")
                for package_name, package_version in version_packages:
                    print(f"  {package_name}: {package_version}")

            print(f"\nTotal dependencies: {len(dependencies)}")
            if file_packages:
                print(f"  - Local file packages: {len(file_packages)}")
                print(f"  - Version-based packages: {len(version_packages)}")
        else:
            print("\nNo dependencies found in manifest.json")

        print("="*80 + "\n")

    except json.JSONDecodeError as e:
        print(f"ERROR: Invalid JSON in manifest file: {e}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"ERROR: Could not read manifest dependencies: {e}", file=sys.stderr)
        sys.exit(1)

def main():
    args = parse_args()
    print_manifest_dependencies(args.manifest_path)

if __name__ == "__main__":
    main()
