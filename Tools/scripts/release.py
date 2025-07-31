"""
This python script makes the NGO package release ready. What it does is:
1) Update changelogs
2) Update validation exception file based on manifest version

Note that this script NEEDS TO BE RUN FROM THE ROOT of the project.
"""
#!/usr/bin/env python3
import datetime
import json
import os
import re
import subprocess
import platform

package_name = 'com.unity.netcode.gameobjects'

def update_changelog(new_version):
    """
    Cleans the [Unreleased] section of the changelog by removing empty subsections,
    then replaces the '[Unreleased]' tag with the new version and release date.
    """

    changelog_entry = f'## [{new_version}] - {datetime.date.today().isoformat()}'
    changelog_path = f'{package_name}/CHANGELOG.md'
    print("Latest CHANGELOG entry will be modified to: " + changelog_entry)

    with open(changelog_path, 'r', encoding='UTF-8') as f:
        changelog_text = f.read()

    # This pattern finds a line starting with '###', followed by its newline,
    # and then two more lines that contain only whitespace.
    # The re.MULTILINE flag allows '^' to match the start of each line.
    pattern = re.compile(r"^###.*\n\n\n", re.MULTILINE)

    # Replace every match with an empty string. The goal is to remove empty CHANGELOG subsections.
    cleaned_content = pattern.sub('', changelog_text)

    # Replace the [Unreleased] section with the new version + cleaned subsections
    changelog_text = re.sub(r'## \[Unreleased\]', changelog_entry, cleaned_content)

    # Write the changes
    with open(changelog_path, 'w', encoding='UTF-8', newline='\n') as file:
        file.write(changelog_text)


def update_validation_exceptions(new_version):
    """
    Updates the ValidationExceptions.json file with the new package version.
    """

    validation_file = f'{package_name}/ValidationExceptions.json'

    # If files do not exist, exit
    if not os.path.exists(validation_file):
        return

    # Update the PackageVersion in the exceptions
    with open(validation_file, 'rb') as f:
        json_text = f.read()
        data = json.loads(json_text)
        updated = False
        for exceptionElements in ["WarningExceptions", "ErrorExceptions"]:
            exceptions = data.get(exceptionElements)

            if exceptions is not None:
                for exception in exceptions:
                    if 'PackageVersion' in exception:
                        exception['PackageVersion'] = new_version
                        updated = True

    # If no exceptions were updated, we do not need to write the file
    if not updated:
        return

    with open(validation_file, 'w', encoding='UTF-8', newline='\n') as json_file:
        json.dump(data, json_file, ensure_ascii=False, indent=2)
        json_file.write("\n")  # Add newline cause Py JSON does not
        print(f"  updated `{validation_file}`")


def get_manifest_json_version(filename):
    """
    Reads the package.json file and returns the version specified in it.
    """
    with open(filename, 'rb') as f:
        json_text = f.read()
        data = json.loads(json_text)

    return data['version']


if __name__ == '__main__':
    manifest_path = f'{package_name}/package.json'
    package_version = get_manifest_json_version(manifest_path)

    # Update the ValidationExceptions.json file
    # with the new package version OR remove it if not a release branch
    update_validation_exceptions(package_version)
    # Clean the CHANGELOG and add latest entry
    update_changelog(package_version)
