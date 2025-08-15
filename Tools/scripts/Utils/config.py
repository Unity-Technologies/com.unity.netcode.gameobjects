"""
Configuration for NGO Release Automation
"""

def getDefaultRepoBranch():
    """
    Returns the name of Tools repo default branch.
    This will be used to for example push changelog update for the release.
    In general this branch is the default working branch
    """
    return 'develop'

def getNetcodeGithubRepo():
    """Returns the name of MP Tools repo."""
    return 'Unity-Technologies/com.unity.netcode.gameobjects'

def getNetcodePackageName():
    """Returns the name of the MP Tools package."""
    return 'com.unity.netcode.gameobjects'

def getPackageManifestPath():
    """Returns the path to the Netcode package manifest."""

    return 'com.unity.netcode.gameobjects/package.json'
    
def getPackageValidationExceptionsPath():
    """Returns the path to the Netcode ValidationExceptions."""

    return 'com.unity.netcode.gameobjects/ValidationExceptions.json'

def getPackageChangelogPath():
    """Returns the path to the Netcode package manifest."""

    return 'com.unity.netcode.gameobjects/CHANGELOG.md'

def getNetcodeReleaseBranchName(package_version):
    """
    Returns the branch name for the Netcode release.
    """
    return f"release/{package_version}"

def getNetcodeProjectID():
    """
    Returns the Unity project ID for the DOTS monorepo.
    Useful when for example triggering Yamato jobs
    """
    return '1201'
