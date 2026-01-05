"""Helper class for Git repo operations."""

import sys
import os

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../ReleaseAutomation'))
sys.path.insert(0, PARENT_DIR)

import subprocess
from git import Repo, Actor
from github import GithubException
from release_config import ReleaseConfig

def get_local_repo():
    root_dir = subprocess.check_output(['git', 'rev-parse', '--show-toplevel'],
                                       universal_newlines=True, stderr=subprocess.STDOUT).strip()
    return Repo(root_dir)


def get_latest_git_revision(branch_name):
    """Gets the latest commit SHA for a given branch using git rev-parse."""
    try:
        subprocess.run(
            ['git', 'fetch', 'origin'],
            capture_output=True,
            text=True,
            check=True
        )
        remote_branch_name = f'origin/{branch_name}'
        # Executes the git command: git rev-parse <remote_branch_name>
        result = subprocess.run(
            ['git', 'rev-parse', remote_branch_name],
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout.strip()

    except FileNotFoundError as exc:
        raise Exception("Git command not found. Please ensure Git is installed and available in your PATH.") from exc
    except subprocess.CalledProcessError as e:
        raise Exception(f"Failed to get the latest revision for branch '{branch_name}'.") from e

def create_release_branch(config: ReleaseConfig):
    """
    Creates a new branch with the specified name, performs specified action, commits the current changes and pushes it to the repo.
    Note that command_to_run_on_release_branch (within the Config) should be a single command that will be executed using subprocess.run. For multiple commands consider using a Python script file.
    
    IMPORTANT: The release branch is created from the trigger branch (the branch the job was triggered from).
    This ensures the release branch is created from the branch that was validated and will be used for the PR.
    Please double check if the target branch is different and if so the if this was intended.
    """

    try:
        if config.github_manager.is_branch_present(config.release_branch_name):
            raise Exception(f"Branch '{config.release_branch_name}' already exists.")

        repo = get_local_repo()
        trigger_branch = repo.active_branch.name
        
        # Stash any uncommitted changes to allow pull
        has_uncommitted_changes = repo.is_dirty()
        if has_uncommitted_changes:
            print("Uncommitted changes detected. Stashing before pull...")
            repo.git.stash('push', '-m', 'Auto-stash before pull for release branch creation')
        
        repo.git.fetch('--prune', '--prune-tags')
        repo.git.pull("origin", trigger_branch)

        new_branch = repo.create_head(config.release_branch_name, repo.head.commit)
        new_branch.checkout()

        if config.command_to_run_on_release_branch:
            print(f"\nExecuting command on branch '{config.release_branch_name}': {config.command_to_run_on_release_branch.__name__}")
            config.command_to_run_on_release_branch(config.manifest_path, config.changelog_path, config.validation_exceptions_path, config.package_version)

        repo.git.add(config.changelog_path)
        repo.git.add(config.manifest_path)
        repo.git.add(config.validation_exceptions_path)

        author = Actor(config.commiter_name, config.commiter_email)
        committer = Actor(config.commiter_name, config.commiter_email)

        repo.index.commit(config.release_commit_message, author=author, committer=committer, skip_hooks=True)
        repo.git.push("origin", config.release_branch_name)

        print(f"Successfully created, updated and pushed new branch: {config.release_branch_name}")

    except GithubException as e:
        raise GithubException(f"An error occurred with the GitHub API: {e.status}", data=e.data) from e
    except Exception as e:
        raise Exception(f"An unexpected error occurred: {e}") from e
