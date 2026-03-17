"""Netcode configuration for the release process automation."""

import datetime
import sys
import os
from github import Github
from github import GithubException

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))
sys.path.insert(0, PARENT_DIR)

from Utils.general_utils import get_package_version_from_manifest
from release import make_package_release_ready

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
            raise Exception(f"An error occurred with the GitHub API: {ghe.status}", data=ghe.data)
            
    def create_pull_request(self, title, body, head, base):
        try:
            return self.repo.create_pull(title=title, body=body, head=head, base=base)

        except GithubException as ghe:
            raise Exception(f"Failed to create pull request: {ghe.status}", ghe.data) from ghe
    
    def request_reviews(self, pr, reviewers):
        if not reviewers:
            return
    
        try:
            pr.create_review_request(reviewers=reviewers)
        except GithubException as ghe:
            raise Exception(f"Failed to request reviews: {ghe.status}", ghe.data) from ghe


class ReleaseConfig:
    """A simple class to hold all shared configuration."""
    def __init__(self):
        self.manifest_path = 'com.unity.netcode.gameobjects/package.json'
        self.changelog_path = 'com.unity.netcode.gameobjects/CHANGELOG.md'
        self.validation_exceptions_path = 'com.unity.netcode.gameobjects/ValidationExceptions.json'
        self.github_repo = 'Unity-Technologies/com.unity.netcode.gameobjects'
        self.default_repo_branch = 'develop' # Changelog and package version change will be pushed to this branch
        self.yamato_project_id = '1201'
        self.command_to_run_on_release_branch = make_package_release_ready

        self.release_weekday = 6  # Sunday
        self.release_week_cycle = 4  # Release every 4 weeks
        self.anchor_date = datetime.date(2025, 7, 20) # Anchor date for the release cycle (previous release Sunday)

        self.package_version = get_package_version_from_manifest(self.manifest_path)
        self.release_branch_name = f"release/{self.package_version}" # Branch from which we want to release
        
        self.release_commit_message = f"Updated changelog and package version for Netcode in anticipation of v{self.package_version} release"
        
        self.pr_branch_name = f"netcode-update-after-{self.package_version}-release-branch-creation" # Branch from which we will create PR to default branch with relevant changes after release branch is created
        self.pr_commit_message = f"chore: Updated aspects of Netcode package in anticipation of v{self.package_version} release"
        self.pr_body = f"This PR was created in sync with branching of {self.release_branch_name}. It includes changes that should land on the default Netcode branch ({self.default_repo_branch}) to reflect the new state of the package after the v{self.package_version} release:\n" \
            f"1) Updated CHANGELOG.md by adding new [Unreleased] section template at the top and cleaning the Changelog for the current release.\n" \
            f"2) Updated package version in package.json by incrementing the patch version to signify the current state of the package.\n" \
            f"3) Updated package version in ValidationExceptions.json to match the new package version.\n\n" \
            f"Please review and merge this PR to keep the default branch up to date with the latest package state after the release. Those changes can land immediately OR after the release was finalized but make sure that the Changelog will be merged correctly as sometimes some discrepancies may be introduced due to new entries being introduced meantime\n"
        self.pr_reviewers = ["michal-chrobot"]

        GITHUB_TOKEN_NAME = "NETCODE_GITHUB_TOKEN"
        YAMATO_API_KEY_NAME = "NETCODE_YAMATO_API_KEY"
        self.github_token = os.environ.get(GITHUB_TOKEN_NAME)
        self.yamato_api_token = os.environ.get(YAMATO_API_KEY_NAME)
        self.commiter_name = "netcode-automation"
        self.commiter_email = "svc-netcode-sdk@unity3d.com"

        self.yamato_samples_to_build = [
            {
                "name": "BossRoom",
                "jobDefinition": f".yamato%2Fproject-builders%2Fproject-builders.yml%23build_BossRoom_project",
            }
        ]

        self.yamato_build_automation_configs = [
            {
                "job_name": "Build Sample for Windows with minimal supported editor (2022.3), burst ON, IL2CPP",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "on" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                    { "key": "UNITY_VERSION", "value": "2022.3" } # Minimal supported editor
                ]
            },
            {
                "job_name": "Build Sample for Windows with latest functional editor (6000.3), burst ON, IL2CPP",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "on" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                    { "key": "UNITY_VERSION", "value": "6000.3" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
                ]
            },
            {
                "job_name": "Build Sample for MacOS with minimal supported editor (2022.3), burst OFF, Mono",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "off" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "mac" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "mono" },
                    { "key": "UNITY_VERSION", "value": "2022.3" } # Minimal supported editor
                ]
            },
            {
                "job_name": "Build Sample for MacOS with latest functional editor (6000.3), burst OFF, Mono",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "off" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "mac" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "mono" },
                    { "key": "UNITY_VERSION", "value": "6000.3" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
                ]
            },
            {
                "job_name": "Build Sample for Android with minimal supported editor (2022.3), burst ON, IL2CPP",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "on" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "android" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                    { "key": "UNITY_VERSION", "value": "2022.3" } # Minimal supported editor
                ]
            },
            {
                "job_name": "Build Sample for Android with latest functional editor (6000.3), burst ON, IL2CPP",
                "variables": [
                    { "key": "BURST_ON_OFF", "value": "on" },
                    { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "android" },
                    { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                    { "key": "UNITY_VERSION", "value": "6000.3" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
                ]
            }
        ]

        error_messages = []
        if not os.path.exists(self.manifest_path):
            error_messages.append(f" Path does not exist: {self.manifest_path}")

        if not os.path.exists(self.changelog_path):
            error_messages.append(f" Path does not exist: {self.changelog_path}")

        if not os.path.exists(self.validation_exceptions_path):
            error_messages.append(f" Path does not exist: {self.validation_exceptions_path}")

        if not callable(self.command_to_run_on_release_branch):
            error_messages.append("command_to_run_on_release_branch is not a function! Actual value:", self.command_to_run_on_release_branch)

        if self.package_version is None:
            error_messages.append(f"Package version not found at {self.manifest_path}")

        if not self.github_token:
            error_messages.append(f"Error: {GITHUB_TOKEN_NAME} environment variable not set.")

        if not self.yamato_api_token:
            error_messages.append(f"Error: {YAMATO_API_KEY_NAME} environment variable not set.")

        # Initialize PyGithub and get the repository object
        self.github_manager = GithubUtils(self.github_token, self.github_repo)

        if not self.github_manager.is_branch_present(self.default_repo_branch):
            error_messages.append(f"Branch '{self.default_repo_branch}' does not exist.")

        if self.github_manager.is_branch_present(self.release_branch_name):
            error_messages.append(f"Branch '{self.release_branch_name}' is already present in the repo.")

        if error_messages:
            summary = "Failed to initialize NetcodeReleaseConfig due to invalid setup:\n" + "\n".join(f"- {msg}" for msg in error_messages)
            raise ValueError(summary)
