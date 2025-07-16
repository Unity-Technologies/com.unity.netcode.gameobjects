"""
NGO release script
- Update changelogs + validation exception file based on manifest version
"""
#!/usr/bin/env python3
import datetime
import json
import os
import re

package_name = 'com.unity.netcode.gameobjects'

def update_changelog(new_version):
    changelog_entry = f'## [{new_version}] - {datetime.date.today().isoformat()}'

    print(changelog_entry)

    changelog_path = f'{package_name}/CHANGELOG.md'
    with open(changelog_path, 'rb') as f:
        changelog_text = f.read()

    changelog_text = re.sub(br'## \[Unreleased\]', bytes(changelog_entry, 'UTF-8'), changelog_text)

    with open(changelog_path, 'wb') as f:
        f.write(changelog_text)

def update_validation_exceptions(new_version):
    validation_file = f'{package_name}/ValidationExceptions.json'
    if not os.path.exists(validation_file):
        return

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

    if not updated:
        return

    with open(validation_file, "w", encoding="UTF-8") as json_file:
        json.dump(data, json_file, ensure_ascii=False, indent=2)
        json_file.write("\n")  # Add newline cause Py JSON does not
        print(f"  updated `{validation_file}`")


def get_manifest_json_version(filename):
    with open(filename, 'rb') as f:
        json_text = f.read()
        data = json.loads(json_text)

    return data['version']

if __name__ == '__main__':
    manifest_path = f'{package_name}/package.json'
    version = get_manifest_json_version(manifest_path)
    update_validation_exceptions(version)

    update_changelog(version)
