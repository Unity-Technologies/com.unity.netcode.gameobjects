"""
This python script makes the NGO package release ready. What it does is:
1) Update changelogs
2) Update validation exception file based on manifest version

Note that this script NEEDS TO BE RUN FROM THE ROOT of the project.
"""
#!/usr/bin/env python3
import json
import os
import re
import sys
import subprocess
import platform

from Utils.general_utils import get_package_version_from_manifest, update_changelog, update_validation_exceptions, regenerate_wrench # nopep8

def make_package_release_ready(manifest_path, changelog_path, validation_exceptions_path, package_version):

    if not os.path.exists(manifest_path):
        print(f" Path does not exist: {manifest_path}")
        sys.exit(1)

    if not os.path.exists(changelog_path):
        print(f" Path does not exist: {changelog_path}")
        sys.exit(1)

    if package_version is None:
        print(f"Package version not found at {manifest_path}")
        sys.exit(1)

    # Update the ValidationExceptions.json file
    # with the new package version OR remove it if not a release branch
    update_validation_exceptions(validation_exceptions_path, package_version)
    # Clean the CHANGELOG and add latest entry
    # package version is already know as explained in
    # https://github.cds.internal.unity3d.com/unity/dots/pull/14318
    update_changelog(changelog_path, package_version)
    # Make sure that the wrench scripts are up to date
    regenerate_wrench()


if __name__ == '__main__':
    manifest_path = 'com.unity.netcode.gameobjects/package.json'
    changelog_path = 'com.unity.netcode.gameobjects/CHANGELOG.md'
    validation_exceptions_path = './ValidationExceptions.json'
    package_version = get_package_version_from_manifest(manifest_path)

    make_package_release_ready(manifest_path, changelog_path, validation_exceptions_path, package_version)
