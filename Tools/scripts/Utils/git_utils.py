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


def get_trigger_branch(repo, default_branch, exclude_branches=None):
    """
    Gets the trigger branch name, handling detached HEAD state in CI environments.
    
    In CI environments, the repository might be checked out at a specific commit (detached HEAD).
    This function tries multiple methods to determine the branch:
    1. Check if HEAD is attached to a branch (but skip if it's a release branch or excluded branch)
    2. Check environment variables (YAMATO_BRANCH, CI_COMMIT_REF_NAME, etc.)
    3. Use git commands to find which remote branch contains the current commit
    4. Fall back to the default branch if nothing else works
    
    Args:
        repo: GitPython Repo object
        default_branch: Default branch name to fall back to
        exclude_branches: Optional list of branch names to exclude (e.g., release branches)
        
    Returns:
        str: The branch name
    """
    exclude_branches = exclude_branches or []
    current_branch = None
    
    try:
        # Try to get the active branch name (works when HEAD is attached)
        current_branch = repo.active_branch.name
        # If we're on a release branch or excluded branch, don't use it - use other methods
        if current_branch.startswith('release/') or current_branch in exclude_branches:
            print(f"Current branch '{current_branch}' is a release/excluded branch, using other methods to find trigger branch...")
            current_branch = None
        else:
            return current_branch
    except (TypeError, ValueError):
        # HEAD is detached, try other methods
        pass
    
    # Method 1: Check environment variables
    # Yamato might set branch info in environment variables
    trigger_branch = os.environ.get('YAMATO_BRANCH') or \
                     os.environ.get('CI_COMMIT_REF_NAME') or \
                     os.environ.get('GITHUB_REF_NAME') or \
                     os.environ.get('BRANCH_NAME')
    
    if trigger_branch:
        # Remove 'refs/heads/' prefix if present
        trigger_branch = trigger_branch.replace('refs/heads/', '')
        print(f"Found trigger branch from environment variable: {trigger_branch}")
        return trigger_branch
    
    # Method 2: Try to find which remote branch contains the current commit
    try:
        current_commit = repo.head.commit.hexsha
        # Fetch all remote branches
        repo.git.fetch('origin', '--prune', '--prune-tags')
        
        # Try to find which remote branch points to this commit
        result = subprocess.run(
            ['git', 'branch', '-r', '--contains', current_commit],
            capture_output=True,
            text=True,
            check=True
        )
        
        branches = [b.strip() for b in result.stdout.strip().split('\n') if b.strip()]
        # Filter out release branches and excluded branches
        valid_branches = []
        for branch_line in branches:
            branch = branch_line.replace('origin/', '').strip()
            if branch and not branch.startswith('release/') and branch not in exclude_branches:
                valid_branches.append(branch)
        
        # Prefer default branch, then other valid branches
        for branch in valid_branches:
            if branch == default_branch:
                print(f"Found trigger branch from remote branches: {branch}")
                return branch
        
        # If default branch not found, use the first valid branch
        if valid_branches:
            branch = valid_branches[0]
            print(f"Found trigger branch from remote branches: {branch}")
            return branch
    except Exception as e:
        print(f"Warning: Could not determine branch from remote branches: {e}")
    
    # Method 3: Fall back to default branch
    print(f"Warning: Could not determine trigger branch, falling back to default branch: {default_branch}")
    return default_branch

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
        trigger_branch = get_trigger_branch(repo, config.default_repo_branch)
        print(f"\nTrigger branch: {trigger_branch}")
        
        # If we're in detached HEAD state, checkout the trigger branch first
        try:
            repo.active_branch.name
        except (TypeError, ValueError):
            # HEAD is detached, checkout the trigger branch
            print(f"HEAD is detached, checking out trigger branch '{trigger_branch}'...")
            repo.git.checkout(trigger_branch)
        
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
