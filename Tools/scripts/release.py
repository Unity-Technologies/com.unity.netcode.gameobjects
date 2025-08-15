"""
This python script makes the NGO package release ready. What it does is:
1) Update changelogs
2) Update validation exception file based on manifest version

Note that this script NEEDS TO BE RUN FROM THE ROOT of the project.
"""
#!/usr/bin/env python3
import os
import sys

UTILS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), './Utils'))
sys.path.insert(0, UTILS_DIR)
from general_utils import get_package_version_from_manifest, update_changelog, update_validation_exceptions # nopep8
from config import getNetcodePackageName, getPackageManifestPath, getPackageValidationExceptionsPath, getPackageChangelogPath # nopep8

if __name__ == '__main__':
    
    ngo_package_name = getNetcodePackageName()
    ngo_manifest_path = getPackageManifestPath()
    ngo_validation_exceptions_path = getPackageValidationExceptionsPath()
    ngo_changelog_path = getPackageChangelogPath()

    if not os.path.exists(ngo_manifest_path):
        print(f" Path does not exist: {ngo_manifest_path}")
        sys.exit(1)
        
    if not os.path.exists(ngo_changelog_path):
        print(f" Path CHANGELOG does not exist: {ngo_changelog_path}")
        sys.exit(1)

    ngo_package_version = get_package_version_from_manifest(ngo_manifest_path)

    if ngo_package_version is None:
        print(f"Package version not found at {ngo_manifest_path}")
        sys.exit(1)

    # Update the ValidationExceptions.json file
    # with the new package version OR remove it if not a release branch
    update_validation_exceptions(ngo_validation_exceptions_path, ngo_package_version)
    # Clean the CHANGELOG and add latest entry
    # package version is already know as is always corresponds to current package state
    update_changelog(ngo_changelog_path, ngo_package_version)
