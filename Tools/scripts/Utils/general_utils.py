"""Helper class for common operations."""
#!/usr/bin/env python3
import json
import os
import re
import datetime
import platform
import subprocess
import warnings

UNRELEASED_CHANGELOG_SECTION_TEMPLATE = r"""
## [Unreleased]

### Added


### Changed


### Deprecated


### Removed


### Fixed


### Security


### Obsolete
"""

def get_package_version_from_manifest(package_manifest_path):
    """
    Reads the package.json file and returns the version specified in it.
    """

    if not os.path.exists(package_manifest_path):
        raise FileNotFoundError(f"{package_manifest_path} couldn't be find")

    with open(package_manifest_path, 'rb') as f:
        json_text = f.read()
        data = json.loads(json_text)

    return data['version']


def update_package_version_by_patch(package_manifest_path):
    """
    Updates the package version in the package.json file.
    This function will bump the package version by a patch.

    The usual usage would be to bump package version during/after release to represent the "current package state" which progresses since the release branch was created
    """

    if not os.path.exists(package_manifest_path):
        raise FileNotFoundError(f"The file {package_manifest_path} does not exist.")

    with open(package_manifest_path, 'r', encoding='UTF-8') as f:
        package_manifest = json.load(f)

    version_parts = get_package_version_from_manifest(package_manifest_path).split('.')
    if len(version_parts) != 3:
        raise ValueError("Version format is not valid. Expected format: 'major.minor.patch'.")

    # Increment the patch version
    version_parts[2] = str(int(version_parts[2]) + 1)
    new_package_version = '.'.join(version_parts)

    package_manifest['version'] = new_package_version

    with open(package_manifest_path, 'w', encoding='UTF-8', newline='\n') as f:
        json.dump(package_manifest, f, indent=4)

    return new_package_version
        
        
def update_validation_exceptions(validation_file, package_version):
    """
    Updates the ValidationExceptions.json file with the new package version.
    """

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

            if exceptions is None:
                continue

            for exception in exceptions:
                if 'PackageVersion' in exception:
                    exception['PackageVersion'] = package_version
                    updated = True

    # If no exceptions were updated, we do not need to write the file
    if not updated:
        raise FileNotFoundError(f"No validation exceptions were updated in {validation_file}.")

    with open(validation_file, 'w', encoding='UTF-8', newline='\n') as json_file:
        json.dump(data, json_file, ensure_ascii=False, indent=2)
        json_file.write("\n")  # Add newline cause Py JSON does not



def update_changelog(changelog_path, new_version, add_unreleased_template=False):
    """
    Cleans the [Unreleased] section of the changelog by removing empty subsections,
    then replaces the '[Unreleased]' tag with the new version and release date.
    If the version header already exists, it will remove the [Unreleased] section and add any entries under the present version.
    If add_unreleased_template is specified then it will also include the template at the top of the file

    1 - Cleans the [Unreleased] section by removing empty subsections.
    2 - Checks if the version header already has its section in the changelog.
    3 - If it does, it removes the [Unreleased] section and its content.
    4 - If it does not, it replaces the [Unreleased] section with the new version and today's date.
    """

    new_changelog_entry = f'## [{new_version}] - {datetime.date.today().isoformat()}'
    version_header_to_find_if_exists = f'## [{new_version}]'

    with open(changelog_path, 'r', encoding='UTF-8') as f:
        changelog_text = f.read()

    # This pattern finds a line starting with '###', followed by its newline,
    # and then two more lines that contain only whitespace.
    # The re.MULTILINE flag allows '^' to match the start of each line.
    pattern = re.compile(r"^###.*\n\n\n", re.MULTILINE)

    # Replace every match with an empty string. The goal is to remove empty CHANGELOG subsections.
    cleaned_content = pattern.sub('', changelog_text)

    if version_header_to_find_if_exists in changelog_text:
        warnings.warn(f"A changelog entry for version '{new_version}' already exists. The script will just remove Unreleased section and its content.")
        changelog_text = re.sub(r'(?s)## \[Unreleased(.*?)(?=## \[)', '', changelog_text)
    else:
        # Replace the [Unreleased] section with the new version + cleaned subsections
        print("Latest CHANGELOG entry will be modified to: " + new_changelog_entry)
        changelog_text = re.sub(r'## \[Unreleased\]', new_changelog_entry, cleaned_content)

    # Accounting for the very top of the changelog format
    header_end_pos = changelog_text.find('(https://docs-multiplayer.unity3d.com).', 1)
    insertion_point = changelog_text.find('\n', header_end_pos)

    final_content = ""
    if add_unreleased_template:
        print("Adding [Unreleased] section template to the top of the changelog.")
        final_content = (
            changelog_text[:insertion_point] +
            f"\n{UNRELEASED_CHANGELOG_SECTION_TEMPLATE}" +
            changelog_text[insertion_point:]
        )
    else:
        final_content = (
            changelog_text[:insertion_point] +
            changelog_text[insertion_point:]
        )

    # Write the changes
    with open(changelog_path, 'w', encoding='UTF-8', newline='\n') as file:
        file.write(final_content)
