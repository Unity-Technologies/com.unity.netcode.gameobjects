"""
This script automates the creation of a release branch for the NGO package.

It performs the following steps:
1. Creates a new release branch named 'release/<version>'.
2. Executes the release.py script that prepares branch for release by
updating changelog and ValidationExceptions.
3. Commits all changes made by the script.
4. Pushes the new branch to the remote repository.
"""
#!/usr/bin/env python3
import sys
import os

UTILS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../Utils'))
sys.path.insert(0, UTILS_DIR)
from general_utils import get_package_version_from_manifest # nopep8
from git_utils import create_branch_execute_commands_and_push # nopep8
from config import getPackageManifestPath, getNetcodeReleaseBranchName, getNetcodeGithubRepo # nopep8

def createToolsReleaseBranch():
    """
    Creates a new release branch for the NGO package.
    It also runs release.py script that prepares the branch for release by updating the changelog, ValidationExceptions etc
    """

    ngo_manifest_path = getPackageManifestPath()
    ngo_package_version = get_package_version_from_manifest(ngo_manifest_path)
    ngo_github_repo = getNetcodeGithubRepo()
    ngo_release_branch_name = getNetcodeReleaseBranchName(ngo_package_version)
    ngo_github_token = os.environ.get("GITHUB_TOKEN")

    if not os.path.exists(ngo_manifest_path):
        print(f" Path does not exist: {ngo_manifest_path}")
        sys.exit(1)

    if ngo_package_version is None:
        print(f"Package version not found at {ngo_manifest_path}")
        sys.exit(1)

    if not ngo_github_token:
        print("Error: GITHUB_TOKEN environment variable not set.", file=sys.stderr)
        sys.exit(1)

    commit_message = f"Preparing Netcode package of version {ngo_package_version} for the release"
    command_to_run_on_release_branch = ['python', 'Tools/scripts/release.py']

    create_branch_execute_commands_and_push(ngo_github_token, ngo_github_repo, ngo_release_branch_name, commit_message, command_to_run_on_release_branch)



if __name__ == "__main__":
    createToolsReleaseBranch()
