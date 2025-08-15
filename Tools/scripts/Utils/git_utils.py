"""Helper class for Git repo operations."""
import subprocess
import sys
from git import Repo
from github import Github
from github import GithubException

class GithubUtils:
    def __init__(self, access_token, repo):
        self.github = Github(base_url="https://api.github.com",
                             login_or_token=access_token)
        self.repo = self.github.get_repo(repo)

    def is_branch_present(self, branch_name):
        try:
            self.repo.get_branch(branch_name)
            return True # Branch exists

        except GithubException as ghe:
            if ghe.status == 404:
                return False # Branch does not exist
            print(f"An error occurred with the GitHub API: {ghe.status}", file=sys.stderr)
            print(f"Error details: {ghe.data}", file=sys.stderr)
            sys.exit(1)

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
    except FileNotFoundError:
        print("Error: 'git' command not found. Is Git installed and in your PATH?", file=sys.stderr)
        sys.exit(1)
    except subprocess.CalledProcessError as e:
        print(f"Error: Failed to get revision for branch '{branch_name}'.", file=sys.stderr)
        print(f"Git stderr: {e.stderr}", file=sys.stderr)
        sys.exit(1)

def create_branch_execute_commands_and_push(github_token, github_repo, branch_name, commit_message, command_to_run=None):
    """
    Creates a new branch with the specified name, performs specified action, commits the current changes and pushes it to the repo.
    Note that command_to_run should be a single command that will be executed using subprocess.run. For multiple commands consider using a Python script file.
    """

    try:
        # Initialize PyGithub and get the repository object
        github_manager = GithubUtils(github_token, github_repo)

        if github_manager.is_branch_present(branch_name):
            print(f"Branch '{branch_name}' already exists. Exiting.")
            sys.exit(1)

        repo = get_local_repo()

        new_branch = repo.create_head(branch_name, repo.head.commit)
        new_branch.checkout()
        print(f"Created and checked out new branch: {branch_name}")

        if command_to_run:
            print(f"\nExecuting command on branch '{branch_name}': {' '.join(command_to_run)}")
            subprocess.run(command_to_run, text=True, check=True)

        print("Executed release.py script successfully.")

        repo.git.add('.')
        repo.index.commit(commit_message, skip_hooks=True)
        repo.git.push("origin", branch_name)

        print(f"Successfully created, updated and pushed new branch: {branch_name}")

    except GithubException as e:
        print(f"An error occurred with the GitHub API: {e.status}", file=sys.stderr)
        print(f"Error details: {e.data}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"An unexpected error occurred: {e}", file=sys.stderr)
        sys.exit(1)
