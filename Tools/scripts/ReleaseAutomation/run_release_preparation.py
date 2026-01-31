"""Automation for package release process."""

import sys
import os

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))
sys.path.insert(0, PARENT_DIR)

from ReleaseAutomation.release_config import ReleaseConfig
from Utils.git_utils import create_release_branch
from Utils.verifyReleaseConditions import verifyReleaseConditions
from Utils.createPrAfterRelease import createPrAfterRelease
from Utils.triggerYamatoJobsForReleasePreparation import trigger_release_preparation_jobs

def PrepareNetcodePackageForRelease():
    try:
        config = ReleaseConfig()

        print("\nStep 1: Verifying release conditions...")
        verifyReleaseConditions(config)
    except Exception as e:
        print("\n--- Release conditions were not met ---", file=sys.stderr)
        print(f"Reason: {e}", file=sys.stderr)
        sys.exit(0) # In this case we want the job not to fail because this will be an intended blocking behavior
    
    
    
    try:
        print("\nStep 2: Creating release branch...")
        create_release_branch(config)

        print("\nStep 3: Triggering Yamato validation jobs...")
        trigger_release_preparation_jobs(config)

        print("\nStep 4: Creating PR with needed changes to default branch...")
        createPrAfterRelease(config)

    except Exception as e:
        print("\n--- ERROR: Netcode release process failed ---", file=sys.stderr)
        print(f"Reason: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    PrepareNetcodePackageForRelease()
