"""
This script replaces 'file:' references in a Unity project's manifest.json with the latest
released versions from Unity's package registry while using Unity Package Vision API:
https://package-vision.prd.cds.internal.unity3d.com/PackageData/{package_name}

Usage:
    python resolve_file_references.py --manifest-path <path_to_manifest.json> [--exclude <package1> <package2> ...] [--dry-run]
"""

import json
import argparse
import sys
import urllib.request
import urllib.error
from typing import Optional


# Unity Package Vision API endpoint for dots-monorepo packages
PACKAGE_VISION_API = "https://package-vision.prd.cds.internal.unity3d.com/PackageData"


def parse_args():
    parser = argparse.ArgumentParser(
        description="Replace 'file:' references in a Unity manifest.json with the latest released versions."
    )
    parser.add_argument(
        "--manifest-path",
        required=True,
        help="The path to the project's manifest.json file."
    )
    parser.add_argument(
        "--exclude",
        nargs="*",
        default=[],
        help="Package names to exclude from resolution (keep as file: references)."
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the changes without modifying the manifest file."
    )
    return parser.parse_args()


def get_latest_version_from_api(package_name: str) -> Optional[str]:
    """
    Fetches the latest released version of a package from the Unity Package Vision API.

    The API returns package data with a 'productionRegistryData' field containing version info.
    We need to find the latest version by sorting semantically.

    Returns None if the package is not found or an error occurs.
    """
    url = f"{PACKAGE_VISION_API}/{package_name}"

    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            data = json.loads(response.read().decode("utf-8"))

            # The API returns data with productionRegistryData containing versions
            production_data = data.get("productionRegistryData", {})
            versions = production_data.get("versions", {})

            if not versions:
                print(f"Warning: No versions found for package '{package_name}'")
                return None

            # Get all version strings and find the latest one
            version_list = list(versions.keys())
            latest_version = find_latest_version(version_list)

            return latest_version

    except urllib.error.HTTPError as e:
        if e.code == 404:
            print(f"Warning: Package '{package_name}' not found in Package Vision API (404)")
        else:
            print(f"Warning: HTTP error fetching '{package_name}': {e.code} {e.reason}")
        return None
    except urllib.error.URLError as e:
        print(f"Warning: Network error fetching '{package_name}': {e.reason}")
        return None
    except json.JSONDecodeError as e:
        print(f"Warning: Failed to parse JSON response for '{package_name}': {e}")
        return None
    except Exception as e:
        print(f"Warning: Unexpected error fetching '{package_name}': {e}")
        return None


def parse_version(version_str: str) -> tuple:
    """
    Parse a semver version string into a tuple for comparison.

    Handles formats like:
    - "1.4.2" -> (1, 4, 2, "", 0)
    - "1.4.2-pre.1" -> (1, 4, 2, "pre", 1)
    - "0.1.0-preview.25" -> (0, 1, 0, "preview", 25)

    Returns a tuple that can be compared for sorting.
    Pre-release versions sort before release versions.
    """
    # Split on hyphen to separate version from pre-release tag
    parts = version_str.split("-", 1)
    main_version = parts[0]
    prerelease = parts[1] if len(parts) > 1 else ""

    try:
        version_nums = tuple(int(x) for x in main_version.split("."))
    except ValueError:
        # If parsing fails, treat as very old version
        version_nums = (0, 0, 0)

    # Pad to 3 elements
    version_nums = version_nums + (0,) * (3 - len(version_nums))

    # Pre-release sorting:
    # - Empty string (release) should sort after pre-release
    # - We use a tuple: (is_release, prerelease_tag, prerelease_num)
    if not prerelease:
        prerelease_tuple = (1, "", 0)
    else:
        prerelease_parts = prerelease.rsplit(".", 1)
        prerelease_tag = prerelease_parts[0]
        try:
            prerelease_num = int(prerelease_parts[1]) if len(prerelease_parts) > 1 else 0
        except ValueError:
            prerelease_num = 0
        prerelease_tuple = (0, prerelease_tag, prerelease_num)

    return version_nums + prerelease_tuple


def find_latest_version(versions: list) -> str:
    """
    Find the latest version from a list of version strings.
    Prefers release versions over pre-release versions.
    """
    if not versions:
        return None

    # Sort versions by parsed version tuple (highest first)
    sorted_versions = sorted(versions, key=parse_version, reverse=True)
    return sorted_versions[0]


def load_manifest(manifest_path: str) -> dict:
    """Load and parse the manifest.json file."""
    try:
        with open(manifest_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        print(f"Error: Manifest file not found at '{manifest_path}'")
        sys.exit(1)
    except json.JSONDecodeError as e:
        print(f"Error: Failed to parse manifest JSON: {e}")
        sys.exit(1)


def save_manifest(manifest_path: str, manifest_data: dict):
    """Save manifest data to file."""
    try:
        with open(manifest_path, "w", encoding="utf-8", newline="\n") as f:
            json.dump(manifest_data, f, indent=4)
        print(f"\nSuccessfully updated manifest at '{manifest_path}'")
    except Exception as e:
        print(f"\nError writing manifest: {e}")
        sys.exit(1)


def process_dependencies(dependencies: dict, exclude: list) -> tuple:
    """Process dependencies and resolve file: references. Returns (changes_made, errors)."""
    changes_made = []
    errors = []

    for package_name, version_ref in list(dependencies.items()):
        if not version_ref.startswith("file:"):
            continue

        if package_name in exclude:
            print(f"Skipping excluded package: {package_name} (keeping as '{version_ref}')")
            continue

        print(f"Fetching latest version for: {package_name}...")
        latest_version = get_latest_version_from_api(package_name)

        if latest_version:
            changes_made.append(f"  {package_name}: '{version_ref}' -> '{latest_version}'")
            dependencies[package_name] = latest_version
        else:
            errors.append(f"  {package_name}: Could not resolve version (keeping as '{version_ref}')")

    return changes_made, errors


def print_results(changes_made: list, errors: list):
    """Print the results of dependency resolution."""
    if changes_made:
        print("\nChanges to be made:")
        for change in changes_made:
            print(change)
    else:
        print("\nNo changes needed.")

    if errors:
        print("\nWarning: Failed to resolve the following packages:")
        for error in errors:
            print(error)
        print("\nThese packages will keep their file: references.")


def main():
    args = parse_args()
    manifest_data = load_manifest(args.manifest_path)
    dependencies = manifest_data.get("dependencies", {})

    changes_made, errors = process_dependencies(dependencies, args.exclude)
    print_results(changes_made, errors)

    if args.dry_run:
        print("\n[Dry run] No changes written to file.")
    elif changes_made:
        save_manifest(args.manifest_path, manifest_data)


if __name__ == "__main__":
    main()
