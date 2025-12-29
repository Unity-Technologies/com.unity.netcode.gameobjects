"""
Creates a direct commit to specified branch (in the config) to update the changelog, package version and validation exceptions for a new release using the GitHub API.
Quite often the changelog gets distorted between the time we branch for the release and the time we will branch back.
To mitigate this we want to create changelog update PR straight away and merge it fast while proceeding with the release.

This will also allow us to skip any PRs after releasing, unless, we made some changes on this branch.

"""
#!/usr/bin/env python3
import os
import sys
from github import GithubException
from git import Actor

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))
sys.path.insert(0, PARENT_DIR)

from ReleaseAutomation.release_config import ReleaseConfig
from Utils.general_utils import get_package_version_from_manifest, update_changelog, update_package_version_by_patch, update_validation_exceptions
from Utils.git_utils import get_local_repo

def createPrAfterRelease(config: ReleaseConfig):
    """
    The function updates the changelog and package version of the package in anticipation of a new release.
    This means that it will
        1) Clean and update the changelog for the current package version.
        2) Add new Unreleased section template at the top.
        3) Update the package version in the package.json file by incrementing the patch version to signify the current state of the package.
        4) Update package version in the validation exceptions to match the new package version.

    This assumes that at the same time you already branched off for the release. Otherwise it may be confusing
    """

    try:
        if not config.github_manager.is_branch_present(config.default_repo_branch):
            print(f"Branch '{config.default_repo_branch}' does not exist. Exiting.")
            sys.exit(1)

        repo = get_local_repo()
        repo.git.fetch('--prune', '--prune-tags')
        repo.git.checkout(config.default_repo_branch)
        repo.git.pull("origin", config.default_repo_branch)
        
        # Create a new branch for the release changes PR to default branch
        repo.git.checkout('-b', config.pr_branch_name)

        # Update the changelog file with adding new [Unreleased] section
        update_changelog(config.changelog_path, config.package_version, add_unreleased_template=True)
        # Update the package version by patch to represent the "current package state" after release
        updated_package_version = update_package_version_by_patch(config.manifest_path)
        update_validation_exceptions(config.validation_exceptions_path, updated_package_version)

        repo.git.add(config.changelog_path)
        repo.git.add(config.manifest_path)
        repo.git.add(config.validation_exceptions_path)

        author = Actor(config.commiter_name, config.commiter_email)
        committer = Actor(config.commiter_name, config.commiter_email)

        repo.index.commit(config.pr_commit_message, author=author, committer=committer, skip_hooks=True)
        repo.git.push("origin", config.pr_branch_name)

        github = config.github_manager
        pr = github.create_pull_request(title=config.pr_commit_message, body=config.pr_body, head=config.pr_branch_name, base=config.default_repo_branch)
        github.request_reviews(pr, config.pr_reviewers)

        print(f"Successfully updated and created the PR targeting: {config.default_repo_branch}")

    except GithubException as e:
        print(f"An error occurred with the GitHub API: {e.status}", file=sys.stderr)
        print(f"Error details: {e.data}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"An unexpected error occurred: {e}", file=sys.stderr)
        sys.exit(1)
