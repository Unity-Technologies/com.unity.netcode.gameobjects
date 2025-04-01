"""Validating PR quality for Netcode PRs"""
#!/usr/bin/env python3
import sys
import json

import lib

def diff_check(diff):
    r"""
    >>> diff_check('com.unity.netcode.gameobjects/Tests')
    True
    >>> diff_check('com.unity.netcode.gameobjects/Tests/MyPath')
    True
    >>> diff_check('com.unity.netcode.gameobjects/Documentation~')
    True
    >>> diff_check('Packages/com.unity.netcode/Documentation~\nPackages/com.unity.netcode/change')
    False
    >>> diff_check('Projects/NetcodeSamples/Assets/Tests/PlayMode/SceneLoadingTests.cs')
    True
    """

    splitlines = diff.splitlines()
    has_netcode_changes = False
    has_doc_changes = False
    for line in splitlines:
        if 'com.unity.netcode.gameobjects/Tests' in line:
            return True
        if 'com.unity.netcode.gameobjects/Documentation~' in line:
            has_doc_changes = True
            continue
        if 'com.unity.netcode.gameobjects/' in line:
            has_netcode_changes = True
            continue

    return (not has_netcode_changes or (not has_netcode_changes and has_doc_changes))


def check_for_tests():
    """Invoke git diff against main then validate against criteria."""
    diff = lib.git_diff_main()

    if diff_check(diff):
        print('Success')
        sys.exit(0)


def qa_added(draft, reviews, reviewers):
    """
    >>> qa_added(True, [], [])
    True
    >>> qa_added(False, [], [])
    False
    >>> qa_added(False, [{'author' : {'login' : 'michalChrobot'}}], [])
    True
    >>> qa_added(False, [{'author' : {'login' : 'michalChrobota'}}], [])
    False
    >>> qa_added(False, [], [{'node' : {'requestedReviewer' : 'michalChrobot'}}])
    True
    >>> qa_added(False, [], [{'node' : {'requestedReviewer' : 'michalChrobota'}}])
    False
    >>> qa_added(False, [{'author' : {'login' : 'Unity-Technologies/netcode-qa'}}], [])
    True
    >>> qa_added(False, [{'author' : {'login' : 'Unity-Technologies/netcode-qaa'}}], [])
    False
    >>> qa_added(False, [], [{'node' : {'requestedReviewer' : 'Unity-Technologies/netcode-qa'}}])
    True
    >>> qa_added(False, [], [{'node' : {'requestedReviewer' : 'Unity-Technologies/netcode-qaa'}}])
    False
    """
    if draft:
        return True

    for review in reviews:
        if review['author']['login'] == 'michalChrobot':
            return True
        if review['author']['login'] == 'Unity-Technologies/netcode-qa':
            return True

    for review in reviewers:
        reviewRequests = json.dumps(review['node']['requestedReviewer'])
        if 'michalChrobot' in reviewRequests:
            return True
        if 'Unity-Technologies/netcode-qa' in reviewRequests:
            return True

    return False

def check_for_qa():
    """Check PR for having QA assigned."""
    repo_owner = 'Unity-Technologies'
    repo_name = 'com.unity.netcode.gameobjects'
    pr_number = lib.get_env('YAMATO_PR_ID')

    github = lib.GitHubGraphql()

    pr_reviewers = github.list_pr_reviewers(repo_owner, repo_name, pr_number)

    draft = pr_reviewers['isDraft']
    reviews = list(pr_reviewers['reviews']['nodes'])
    review_requests = list(pr_reviewers['reviewRequests']['edges'])

    if qa_added(draft, reviews, review_requests):
        print('Success')
        sys.exit(0)

if __name__ == '__main__':
    check_for_tests()
    check_for_qa()

    print('##yamato[result [title=Missing tests; summary=No tests or QA was detected. \
           Please add test coverage for the changes or @netcode-qa as a reviewer; \
           conclusion=failure]]')
    sys.exit(-1)
