"""
Creates a pull request to update the changelog for a new release using the GitHub API.
Quite often the changelog gets distorted between the time we branch for the release and the time we will branch back.
To mitigate this we want to create changelog update PR straight away and merge it fast while proceeding with the release.

This script performs the following actions:
1.  Reads the package version from the package.json file and updates the CHANGELOG.md file while also cleaning it from empty sections.
2.  Updates the package version in the package.json file by incrementing the patch version to represent the current package state.
3.  Commits the change and pushes to the branch.

Requirements:
-   A GITHUB TOKEN with 'repo' scope must be available as an environment variable.
"""
#!/usr/bin/env python3
import os
import sys
from github import GithubException

UTILS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../Utils'))
sys.path.insert(0, UTILS_DIR)
from general_utils import get_package_version_from_manifest, update_changelog, update_package_version_by_patch # nopep8
from git_utils import get_local_repo, GithubUtils # nopep8
from config import getNetcodePackageName, getPackageManifestPath, getNetcodeGithubRepo, getDefaultRepoBranch, getPackageChangelogPath # nopep8

def updateNetcodeChangelogAndPackageVersionAndPush():
    """
    The function updates the changelog and package version for NGO in anticipation of a new release.
    This means that it will clean and update the changelog for the current package version, then it will add new Unreleased section template at the top
    and finally it will update the package version in the package.json file by incrementing the patch version to signify the current state of the package.

    This assumes that at the same time you already branched off for the release. Otherwise it may be confusing
    """

    ngo_package_name = getNetcodePackageName()
    ngo_manifest_path = getPackageManifestPath()
    ngo_changelog_path = getPackageChangelogPath()
    ngo_package_version = get_package_version_from_manifest(ngo_manifest_path)
    ngo_github_repo = getNetcodeGithubRepo()
    ngo_default_repo_branch_to_push_to = getDefaultRepoBranch() # The branch to which the changes will be pushed. For our purposes it's a default repo branch.
    ngo_github_token = os.environ.get("GITHUB_TOKEN")

    print(f"Using branch: {ngo_default_repo_branch_to_push_to} for pushing changes")

    if not os.path.exists(ngo_manifest_path):
        print(f" Path does not exist: {ngo_manifest_path}")
        sys.exit(1)

    if not os.path.exists(ngo_changelog_path):
        print(f" Path does not exist: {ngo_changelog_path}")
        sys.exit(1)

    if ngo_package_version is None:
        print(f"Package version not found at {ngo_manifest_path}")
        sys.exit(1)

    if not ngo_github_token:
        print("Error: GITHUB_TOKEN environment variable not set.", file=sys.stderr)
        sys.exit(1)

    try:
        # Initialize PyGithub and get the repository object
        GithubUtils(ngo_github_token, ngo_github_repo)

        commit_message = f"Updated changelog and package version for NGO in anticipation of v{ngo_package_version} release"

        repo = get_local_repo()
        repo.git.fetch()
        repo.git.checkout(ngo_default_repo_branch_to_push_to)
        repo.git.pull("origin", ngo_default_repo_branch_to_push_to)

        # Update the changelog file with adding new [Unreleased] section
        update_changelog(ngo_package_version, ngo_package_name, add_unreleased_template=True)
        # Update the package version by patch to represent the "current package state" after release
        update_package_version_by_patch(ngo_manifest_path)

        repo.git.add(ngo_changelog_path)
        repo.git.add(ngo_manifest_path)
        repo.index.commit(commit_message, skip_hooks=True)
        repo.git.push("origin", ngo_default_repo_branch_to_push_to)

        print(f"Successfully updated and pushed the changelog on branch: {ngo_default_repo_branch_to_push_to}")

    except GithubException as e:
        print(f"An error occurred with the GitHub API: {e.status}", file=sys.stderr)
        print(f"Error details: {e.data}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"An unexpected error occurred: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    updateNetcodeChangelogAndPackageVersionAndPush()
