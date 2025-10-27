"""
Determines if Release conditions are met.

The script will check the following conditions:
1. **Is today a release day?**
    - The script checks if today is a specified in ReleaseConfig weekday that falls on the release cycle of the team.
    - Note that if the job is triggered manually, this condition will be bypassed.
2. **Is the [Unreleased] section of the CHANGELOG.md not empty?**
    - The script checks if the [Unreleased] section in the CHANGELOG.md contains meaningful entries.
3. **Does the release branch already exist?**
    - If the release branch for the target release already exists, the script will not run.
"""
#!/usr/bin/env python3

import sys
import os

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../ReleaseAutomation'))
sys.path.insert(0, PARENT_DIR)

import datetime
import re
from release_config import ReleaseConfig

def get_yamato_trigger_type():
    """
    Retrieves the trigger type for the current Yamato job from environment variables.
    In other words, we can check if the job was triggered manually, by a schedule, or by a PR, etc.
    This value is set to Recurring when triggered by automation
    """
    trigger_type = os.environ.get('YAMATO_TRIGGER_TYPE', 'Manual')
    
    return trigger_type


def is_release_date(weekday, release_week_cycle, anchor_date):
    """
    Checks if today is a weekday that falls on the release_week_cycle starting from anchor_date .
    Returns True if it is, False otherwise.
    """
    today = datetime.date.today()
    # Check first if today is given weekday
    # Note as for example you could run a job that utilizes the fact that weekly trigger as per https://internaldocs.unity.com/yamato_continuous_integration/usage/jobs/recurring-jobs/#cron-syntax runs every Saturday, between 2 and 8 AM UTC depending on the load
    if today.weekday() != weekday:
        return False

    # Condition 2: Must be on a release_week_cycle interval from the anchor_date.
    days_since_anchor = (today - anchor_date).days
    weeks_since_anchor = days_since_anchor / 7

    # We run on the first week of every release_week_cycle (e.g., week 0, 4, 8, ...)
    return weeks_since_anchor % release_week_cycle == 0


# Note that exceptions parameter can include changelog entries that are not considered "meaningful".
# That means that if Changelog consists ONLY of these entries, it is still considered empty.
def is_changelog_empty(changelog_path, exceptions=None):
    """
    Checks if the [Unreleased] section in the CHANGELOG.md contains meaningful entries.
    It is considered "empty" if the section only contains headers (like ### Added) but no actual content.
    """
    
    if exceptions is None:
        exceptions = []
            
    if not os.path.exists(changelog_path):
        raise FileNotFoundError(f"Changelog file not found at {changelog_path}")

    with open(changelog_path, 'r', encoding='UTF-8') as f:
        content = f.read()

    # This pattern starts where Unreleased section is placed
    # Then it matches in the first group all empty sections (only lines that are empty or start with ##)
    # The second group matches the start of the next Changelog entry (## [).
    # if both groups are matched it means that the Unreleased section is empty.
    unreleased_match = re.search(r"^## \[Unreleased\][ \t]*\n((?:.*\n)*?)(?=^## \[)", content, re.MULTILINE)
    
    if not unreleased_match:
        print("Could not find [Unreleased] section in the changelog.")
        return True  # No [Unreleased] section found, treat as empty
        
    unreleased_content = unreleased_match.group(1)
    # Split into lines and filter out headers and empty lines
    entries = [line.strip() for line in unreleased_content.splitlines()
        if line.strip() and not line.strip().startswith("###")]
    
    # If no entries, changelog is empty
    if not entries:
        print("The [Unreleased] section in the changelog is empty.")
        return True
    
    # If all entries are in exceptions, changelog is empty
    if all(entry in exceptions for entry in entries):
        print("The [Unreleased] section in the changelog has no meaningful entries (only exceptions).")
        return True
    
    return False


def verifyReleaseConditions(config: ReleaseConfig):
    """
    Function to verify if the release automation job should run.
    This function checks the following conditions:
    1. If today is a scheduled release day (based on release cycle, weekday and anchor date).
    2. If the [Unreleased] section of the CHANGELOG.md is not empty.
    3. If the release branch does not already exist.
    """

    error_messages = []
    exceptions = [
        "* Changed minimum Unity version supported to 2022.3 LTS"
    ]

    try:
        trigger_type = get_yamato_trigger_type()
        is_manual = trigger_type in {"Manual", "AdHoc"}
        
        if not is_manual and not is_release_date(config.release_weekday, config.release_week_cycle, config.anchor_date):
            error_messages.append(f"Condition not met: Today is not the scheduled release day. It should be weekday: {config.release_weekday}, every {config.release_week_cycle} weeks starting from {config.anchor_date}.")

        if is_changelog_empty(config.changelog_path, exceptions):
            error_messages.append("Condition not met: The [Unreleased] section of the changelog has no meaningful entries.")

        if config.github_manager.is_branch_present(config.release_branch_name):
            error_messages.append("Condition not met: The release branch already exists.")

        if error_messages:
            print("\n--- Release conditions not met: ---")
            for i, msg in enumerate(error_messages, 1):
                print(f"{i}. {msg}")
            print("\nJob will not run. Exiting.")
            sys.exit(0)

    except Exception as e:
        print("\n--- ERROR: Release Verification failed ---", file=sys.stderr)
        print(f"Reason: {e}", file=sys.stderr)
        sys.exit(1)
