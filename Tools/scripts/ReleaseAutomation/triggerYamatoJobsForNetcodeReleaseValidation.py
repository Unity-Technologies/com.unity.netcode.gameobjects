"""
This script triggers .yamato/wrench/publish-trigger.yml#all_promotion_related_jobs_promotiontrigger to facilitate NGO release process
We still need to manually set up Packageworks but this script will already trigger required jobs so we don't need to wait for them
The goal is to already trigger those on Saturday when release branch is being created so on Monday we can already see the results

Additionally the job also triggers build automation job that will prepare builds for the Playtest.

Requirements:
-   A Long Lived Yamato API Token must be available as an environment variable.
"""
#!/usr/bin/env python3
import os
import sys
import requests

UTILS_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '../Utils'))
sys.path.insert(0, UTILS_DIR)
from general_utils import get_package_version_from_manifest # nopep8
from git_utils import get_latest_git_revision  # nopep8
from config import getPackageManifestPath, getNetcodeReleaseBranchName, getNetcodeProjectID # nopep8

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
    response = requests.post(YAMATO_API_URL, headers=headers, json=data)

    if response.status_code in [200, 201]:
        data = response.json()
        print(f"Successfully triggered '{data['jobDefinitionName']}' where full path is '{data['jobDefinition']['filename']}' on {branch_name} branch and {revision_sha} revision.")
    else:
        print(f"Failed to trigger job. Status: {response.status_code}", file=sys.stderr)
        print("Error:", response.text, file=sys.stderr)
        sys.exit(1)


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
            "job_name": "Build Sample for Windows with minimal supported editor (2022.3), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "2022.3" }
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
            response = requests.post(YAMATO_API_URL, headers=headers, json=data)

            if response.status_code in [200, 201]:
                print("The job was successfully triggered \n")
            else:
                print(f"Failed to trigger job. Status: {response.status_code}", file=sys.stderr)
                print("  Error:", response.text, file=sys.stderr)
                # I will continue the job since it has a limited amount of requests and I don't want to block the whole script if one of the jobs fails



def trigger_NGO_release_preparation_jobs():
    """Triggers Wrench dry run promotion josb and build automation for anticipation for Playtesting and Packageworks setup for NGO."""

    samples_to_build = [
        {
            "name": "BossRoom",
            "jobDefinition": f".yamato%2Fproject-builders%2Fproject-builders.yml%23build_BossRoom_project",
        }
    ]

    build_automation_configs = [
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
            "job_name": "Build Sample for Windows with latest functional editor (6000.2), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "6000.2" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
            ]
        },
        {
            "job_name": "Build Sample for Windows with latest editor (trunk), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "win64" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "trunk" } # latest editor
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
            "job_name": "Build Sample for MacOS with latest functional editor (6000.2), burst OFF, Mono",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "off" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "mac" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "mono" },
                { "key": "UNITY_VERSION", "value": "6000.2" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
            ]
        },
        {
            "job_name": "Build Sample for MacOS with latest editor (trunk), burst OFF, Mono",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "off" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "mac" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "mono" },
                { "key": "UNITY_VERSION", "value": "trunk" } # latest editor
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
            "job_name": "Build Sample for Android with latest functional editor (6000.2), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "android" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "6000.2" } # Editor that most our users will use (not alpha). Sometimes when testing on trunk we have weird editor issues not caused by us so the preference will be to test on latest editor that our users will use.
            ]
        },
        {
            "job_name": "Build Sample for Android with latest editor (trunk), burst ON, IL2CPP",
            "variables": [
                { "key": "BURST_ON_OFF", "value": "on" },
                { "key": "PLATFORM_WIN64_MAC_ANDROID", "value": "android" },
                { "key": "SCRIPTING_BACKEND_IL2CPP_MONO", "value": "il2cpp" },
                { "key": "UNITY_VERSION", "value": "trunk" } # latest editor
            ]
        }
    ]

    ngo_manifest_path = getPackageManifestPath()
    ngo_package_version = get_package_version_from_manifest(ngo_manifest_path)
    ngo_release_branch_name = getNetcodeReleaseBranchName(ngo_package_version)
    ngo_yamato_api_token = os.environ.get("NETCODE_YAMATO_API_KEY")

    ngo_project_ID = getNetcodeProjectID()
    revision_sha = get_latest_git_revision(ngo_release_branch_name)

    if not os.path.exists(ngo_manifest_path):
        print(f" Path does not exist: {ngo_manifest_path}")
        sys.exit(1)

    if ngo_package_version is None:
        print(f"Package version not found at {ngo_manifest_path}")
        sys.exit(1)

    if not ngo_yamato_api_token:
        print("Error: NETCODE_YAMATO_API_KEY environment variable not set.", file=sys.stderr)
        sys.exit(1)

    trigger_wrench_promotion_job_on_yamato(ngo_yamato_api_token, ngo_project_ID, ngo_release_branch_name, revision_sha)
    trigger_automated_builds_job_on_yamato(ngo_yamato_api_token, ngo_project_ID, ngo_release_branch_name, revision_sha, samples_to_build, build_automation_configs)



if __name__ == "__main__":
    trigger_NGO_release_preparation_jobs()
