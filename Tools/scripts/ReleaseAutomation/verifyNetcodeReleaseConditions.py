"""
Determines if NGO release automation job should run.

The script will check the following conditions:
1. **Is today a release Saturday?**
    - The script checks if today is a Saturday that falls on the 4-week cycle for Netcode releases.
2. **Is the [Unreleased] section of the CHANGELOG.md not empty?**
    - The script checks if the [Unreleased] section in the CHANGELOG.md contains meaningful entries.
3. **Does the release branch already exist?**
    - If the release branch for the target release already exists, the script will not run.
    - For this you need to use separate function, see verifyNetcodeReleaseConditions definition
"""
#!/usr/bin/env python3
import datetime
import re
import sys
import os

UTILS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../Utils'))
sys.path.insert(0, UTILS_DIR)
from general_utils import get_package_version_from_manifest # nopep8
from git_utils import GithubUtils  # nopep8
from config import getPackageManifestPath, getNetcodeGithubRepo, getPackageChangelogPath, getNetcodeReleaseBranchName # nopep8

def is_release_date(weekday, release_week_cycle, anchor_date):
    """
    Checks if today is a weekday that falls on the release_week_cycle starting from anchor_date .
    Returns True if it is, False otherwise.
    """
    today = datetime.date.today()
    # Condition 1: Must be a given weekday
    # Note as for example you could run a job that utilizes the fact that weekly trigger as per https://internaldocs.unity.com/yamato_continuous_integration/usage/jobs/recurring-jobs/#cron-syntax runs every Saturday, between 2 and 8 AM UTC depending on the load
    if today.weekday() != weekday:
        return False

    # Condition 2: Must be on a release_week_cycle interval from the anchor_date.
    days_since_anchor = (today - anchor_date).days
    weeks_since_anchor = days_since_anchor / 7

    # We run on the first week of every release_week_cycle (e.g., week 0, 4, 8, ...)
    if weeks_since_anchor % release_week_cycle == 0:
        return True

    return False

def is_changelog_empty(changelog_path):
    """
    Checks if the [Unreleased] section in the CHANGELOG.md contains meaningful entries.
    It is considered "empty" if the section only contains headers (like ### Added) but no actual content.
    """
    if not os.path.exists(changelog_path):
        print(f"Error: Changelog file not found at {changelog_path}")
        sys.exit(1)

    with open(changelog_path, 'r', encoding='UTF-8') as f:
        content = f.read()

    # This pattern starts where Unreleased section is placed
    # Then it matches in the first group all empty sections (only lines that are empty or start with ##)
    # The second group matches the start of the next Changelog entry (## [).
    # if both groups are matched it means that the Unreleased section is empty.
    pattern = re.compile(r"^## \[Unreleased\]\n((?:^###.*\n|^\s*\n)*)(^## \[)", re.MULTILINE)
    match = pattern.search(content)

    # If we find a match for the "empty unreleased changelog entry" pattern, it means the changelog IS empty.
    if match:
        print("Found an [Unreleased] section containing no release notes.")
        return True

    # If the pattern does not match, it means there must be meaningful content.
    return False

def verifyNetcodeReleaseConditions():
    """
    Checks conditions and exits with appropriate status code.
    """

    tools_manifest_path = getPackageManifestPath()
    tools_changelog_path = getPackageChangelogPath()
    tools_package_version = get_package_version_from_manifest(tools_manifest_path)
    tools_github_repo = getNetcodeGithubRepo()
    tools_release_branch_name = getNetcodeReleaseBranchName(tools_package_version)
    tools_github_token = os.environ.get("GITHUB_TOKEN")

    # An anchor date that was a Saturday. This is used to establish the 4-week cycle.
    # You can set this to any past Saturday that you want to mark as the start of a cycle (week 0). We use 2025-07-19 as starting point (previous release date).
    anchor_saturday = datetime.date(2025, 7, 19)

    print("--- Checking conditions for NGO release  ---")

    if not os.path.exists(tools_manifest_path):
        print(f" Path does not exist: {tools_manifest_path}")
        sys.exit(1)

    if not os.path.exists(tools_changelog_path):
        print(f" Path does not exist: {tools_changelog_path}")
        sys.exit(1)

    if tools_package_version is None:
        print(f"Package version not found at {tools_manifest_path}")
        sys.exit(1)

    if not tools_github_token:
        print("Error: GITHUB_TOKEN environment variable not set.", file=sys.stderr)
        sys.exit(1)

    if not is_release_date(weekday=5, release_week_cycle=4, anchor_date=anchor_saturday):
        print("Condition not met: Today is not the scheduled release Saturday.")
        print("Job will not run. Exiting.")
        sys.exit(1)

    print("Condition met: Today is a scheduled release Saturday.")

    if is_changelog_empty(tools_changelog_path):
        print("Condition not met: The [Unreleased] section of the changelog is empty.")
        print("Job will not run. Exiting.")
        sys.exit(1)

    print("Condition met: The changelog contains entries to be released.")

    # Initialize PyGithub and get the repository object
    github_manager = GithubUtils(tools_github_token, tools_github_repo)

    if github_manager.is_branch_present(tools_release_branch_name):
        print("Condition not met: The release branch already exists.")
        print("Job will not run. Exiting.")
        sys.exit(1)

    print("Condition met: The release branch does not yet exist.")

    print("\nAll conditions met. The release preparation job can proceed.")
    sys.exit(0)

if __name__ == "__main__":
    verifyNetcodeReleaseConditions()
