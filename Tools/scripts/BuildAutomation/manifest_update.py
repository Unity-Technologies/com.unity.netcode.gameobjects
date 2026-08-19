"""
Replaces a package in project manifest.json with a local version.
Used when triggering builds from a branch (e.g., release/1.2.3) to use the package from that branch.

Note: This script assumes the package already has an entry in the manifest.
"""

import json
import argparse
import os
import sys
import shutil

def parse_args():
    parser = argparse.ArgumentParser(description='Update Unity project manifest to use local package version.')
    parser.add_argument('--manifest-path', required=True,
                       help='Absolute path to project manifest.json file.')
    parser.add_argument('--package-name', required=True,
                       help="Name of the package to modify in the manifest.")
    parser.add_argument('--local-package-path', required=True,
                       help='Absolute path to local package source directory.')
    parser.add_argument('--remove-folder', default='',
                       help='Relative folder path to remove from cloned repo root (e.g., "Packages"). '
                            'Path is resolved relative to the cloned project root.')
    parser.add_argument('--cloned-project-root', required=True,
                       help='Absolute path to the cloned project root (e.g., C:/ClonedProject). '
                            'If not provided, will be derived from manifest-path by finding the repo root.')
    return parser.parse_args()


def update_manifest(manifest_path, package_name, local_package_path):
    with open(manifest_path, 'r', encoding='utf-8') as f:
        manifest_data = json.load(f)

    # Get old value for comparison
    old_value = manifest_data.get("dependencies", {}).get(package_name, "<not present>")

    local_path_normalized = local_package_path.replace(os.sep, '/')
    manifest_data.setdefault("dependencies", {})[package_name] = f"file:{local_path_normalized}"

    with open(manifest_path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(manifest_data, f, indent=4)

    print("\n" + "="*80)
    print("PACKAGE SUBSTITUTION")
    print("="*80)
    print(f"Package: {package_name}")
    print(f"  Old value: {old_value}")
    print(f"  New value: file:{local_path_normalized}")
    print(f"[OK] Successfully updated manifest at '{manifest_path}'")
    print("="*80 + "\n")

def print_manifest_dependencies(manifest_path):
    """
    Prints all dependencies from the manifest.json file.
    """
    try:
        with open(manifest_path, 'r', encoding='utf-8') as f:
            manifest_data = json.load(f)

        dependencies = manifest_data.get("dependencies", {})
        if dependencies:
            print("\n" + "="*80)
            print("FINAL MANIFEST CONTENT")
            print("="*80)
            for package_name, package_version in sorted(dependencies.items()):
                print(f"  {package_name}: {package_version}")
            print("="*80 + "\n")
        else:
            print("\nNo dependencies found in manifest.json\n")
    except Exception as e:
        print(f"Warning: Could not read manifest dependencies: {e}")


def remove_folder(folder_path, cloned_project_root):
    """
    Removes a folder from the cloned project root.
    """
    if not folder_path:
        return

    if not cloned_project_root:
        print("Warning: Cannot determine cloned project root, skipping folder removal.")
        return

    if not os.path.isdir(cloned_project_root):
        print(f"Warning: Cloned project root not found: {cloned_project_root}, skipping folder removal.")
        return

    # Resolve the folder path relative to cloned project root
    folder_path = folder_path.strip().strip('/').strip('\\')
    absolute_folder_path = os.path.join(cloned_project_root, folder_path)

    print("\n" + "="*80)
    print("REMOVING FOLDER FROM CLONED REPOSITORY")
    print("="*80)
    print(f"Cloned project root: {cloned_project_root}")
    print(f"Folder to remove: {folder_path}")
    print(f"Absolute path: {absolute_folder_path}")

    if os.path.exists(absolute_folder_path) and os.path.isdir(absolute_folder_path):
        try:
            shutil.rmtree(absolute_folder_path)
            print(f"Successfully removed folder: {absolute_folder_path}")
        except Exception as e:
            print(f"ERROR: Failed to remove folder {absolute_folder_path}: {e}", file=sys.stderr)
            raise
    else:
        print(f"  - Folder not found (skipping): {absolute_folder_path}")

    print("="*80 + "\n")

def get_package_version(package_path):
    package_json_path = os.path.join(package_path, 'package.json')
    try:
        with open(package_json_path, 'r', encoding='utf-8') as f:
            return json.load(f).get('version', 'N/A')
    except FileNotFoundError:
        print(f"Warning: Could not find package.json at '{package_json_path}'")
        return None
    except Exception as e:
        print(f"Error reading local package version: {e}")
        return None

def main():
    args = parse_args()

    try:
        # First, remove folder that should not override registry packages
        if args.remove_folder:
            remove_folder(args.remove_folder, args.cloned_project_root)

        # Then proceed with package substitution
        update_manifest(args.manifest_path, args.package_name, args.local_package_path)

        # Print final manifest content
        print_manifest_dependencies(args.manifest_path)

        version = get_package_version(args.local_package_path)
        if version:
            print(f"[OK] Verified local '{args.package_name}' version is: {version}\n")
    except Exception as e:
        print(f"Error updating manifest: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
