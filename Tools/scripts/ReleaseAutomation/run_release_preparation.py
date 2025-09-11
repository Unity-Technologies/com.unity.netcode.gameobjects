import sys
import os

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))
sys.path.insert(0, PARENT_DIR)

from ReleaseAutomation.release_config import ReleaseConfig # nopep8
from Utils.git_utils import create_branch_execute_commands_and_push # nopep8
from Utils.verifyReleaseConditions import verifyReleaseConditions # nopep8
from Utils.commitChangelogAndPackageVersionUpdates import commitChangelogAndPackageVersionUpdates # nopep8
from Utils.triggerYamatoJobsForReleasePreparation import trigger_release_preparation_jobs # nopep8

def PrepareNetcodePackageForRelease():
    try:
        config = ReleaseConfig()

        print("\nStep 1: Verifying release conditions...")
        verifyReleaseConditions(config)

        print("\nStep 2: Creating release branch...")
        create_branch_execute_commands_and_push(config)

        print("\nStep 3: Triggering Yamato validation jobs...")
        trigger_release_preparation_jobs(config)

        print("\nStep 4: Committing changelog and version updates...")
        commitChangelogAndPackageVersionUpdates(config)

    except Exception as e:
        print(f"\n--- ERROR: Netcode release process failed ---", file=sys.stderr)
        print(f"Reason: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    PrepareNetcodePackageForRelease()
