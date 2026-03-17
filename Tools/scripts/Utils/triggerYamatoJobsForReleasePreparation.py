"""
This script triggers .yamato/wrench/publish-trigger.yml#all_promotion_related_jobs_promotiontrigger to facilitate package release process
We still need to manually set up Packageworks but this script will already trigger required jobs so we don't need to wait for them
The goal is to already trigger those when release branch is being created so after Packageworks setup we can already see the results

Additionally the job also triggers build automation job that will prepare builds for the Playtest.
"""
#!/usr/bin/env python3

import os
import sys

PARENT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))
sys.path.insert(0, PARENT_DIR)

import requests
from ReleaseAutomation.release_config import ReleaseConfig
from Utils.git_utils import get_latest_git_revision

YAMATO_API_URL = "https://yamato-api.cds.internal.unity3d.com/jobs"

def trigger_wrench_promotion_job_on_yamato(yamato_api_token, project_id, branch_name, revision_sha):
    """
    Triggers publish-trigger.yml#all_promotion_related_jobs_promotiontrigger job (via the REST API) to run release validation.
    This function basically query the job that NEEDS to pass in order to release via Packageworks
    Note that this will not publish/promote anything by itself but will just trigger the job that will run all the required tests and validations.

    For the arguments we need to pass the Yamato API Long Lived Token, project ID, branch name and revision SHA on which we want to trigger the job.
    """

    headers = {
        "Authorization": f"ApiKey {yamato_api_token}",
        "Content-Type": "application/json"
    }

    data = {
        "source": {
            "branchname": branch_name,
            "revision": revision_sha,
        },
        "links": {
            "project": f"/projects/{project_id}",
            "jobDefinition": f"/projects/{project_id}/revisions/{revision_sha}/job-definitions/.yamato%2Fwrench%2Fpublish-trigger.yml%23all_promotion_related_jobs_promotiontrigger"
        }
    }

    print(f"Triggering job on branch {branch_name}...\n")
    response = requests.post(YAMATO_API_URL, headers=headers, json=data, timeout=10)

    if response.status_code in [200, 201]:
        data = response.json()
        print(f"Successfully triggered '{data['jobDefinitionName']}' where full path is '{data['jobDefinition']['filename']}' on {branch_name} branch and {revision_sha} revision.")
    else:
        raise Exception(f"Failed to trigger job. Status: {response.status_code}, Error: {response.text}")


def trigger_automated_builds_job_on_yamato(yamato_api_token, project_id, branch_name, revision_sha, samples_to_build, build_automation_configs):
    """
    Triggers Yamato jobs (via the REST API) to prepare builds for Playtest.
    Build Automation is based on https://github.cds.internal.unity3d.com/unity/dots/pull/14314

    For the arguments we need to pass the Yamato API Long Lived Token, project ID, branch name and revision SHA on which we want to trigger the job.
    On top of that we should pass samples_to_build in format like

    samples_to_build = [
        {
            "name": "NetcodeSamples",
            "jobDefinition": f".yamato%2Fproject-builders%2Fproject-builders.yml%23build_NetcodeSamples_project",
        }
    ]

    Note that "name" is just a human readable name of the sample (for debug message )and "jobDefinition" is the path to the job definition in the Yamato project. This path needs to be URL encoded, so for example / or # signs need to be replaced with %2F and %23 respectively.

    You also need to pass build_automation_configs which will specify arguments for the build automation job. It should be in the following format:

    build_automation_configs = [
        {
            "job_name": "Build Sample for Windows with latest supported editor (6000.3), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "6000.3" }
            ]
        }
    ]

    Again, note that the "job_name" is used for  debug message and "variables" is a list of environment variables that will be passed to the job. Each variable should be a dictionary with "key" and "value" fields.

    The function will trigger builds for each sample in samples_to_build with each configuration in build_automation_configs.
    """

    headers = {
        "Authorization": f"ApiKey {yamato_api_token}",
        "Content-Type": "application/json"
    }

    for sample in samples_to_build:
        for config in build_automation_configs:
            data = {
                "source": {
                    "branchname": branch_name,
                    "revision": revision_sha,
                },
                "links": {
                    "project": f"/projects/{project_id}",
                    "jobDefinition": f"/projects/{project_id}/revisions/{revision_sha}/job-definitions/{sample['jobDefinition']}"
                },
                "environmentVariables": config["variables"]
            }

            print(f"Triggering the build of {sample['name']} with a configuration '{config['job_name']}' on branch {branch_name}...\n")
            response = requests.post(YAMATO_API_URL, headers=headers, json=data, timeout=10)

            if not response.status_code in [200, 201]:
                print(f"Failed to trigger job. Status: {response.status_code}", file=sys.stderr)
                print("  Error:", response.text, file=sys.stderr)
                # I will continue the job since it has a limited amount of requests and I don't want to block the whole script if one of the jobs fails



def trigger_release_preparation_jobs(config: ReleaseConfig):
    """Triggers Wrench dry run promotion jobs and build automation for anticipation for Playtesting and Packageworks setup for Netcode."""

    try:
        revision_sha = get_latest_git_revision(config.release_branch_name)

        # We don't need to trigger promotion job to run the tests for the release branch since those are being triggered automatically on release/ prefixed branches
        #trigger_wrench_promotion_job_on_yamato(config.yamato_api_token, config.yamato_project_id, config.release_branch_name, revision_sha)
        trigger_automated_builds_job_on_yamato(config.yamato_api_token, config.yamato_project_id, config.release_branch_name, revision_sha, config.yamato_samples_to_build, config.yamato_build_automation_configs)

    except Exception as e:
        print("\n--- ERROR: Job failed ---", file=sys.stderr)
        print(f"Reason: {e}", file=sys.stderr)
        sys.exit(1)
