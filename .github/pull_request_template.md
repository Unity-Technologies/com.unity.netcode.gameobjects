## Purpose of this PR
[//]: # (
Replace this block with what this PR does and why. Describe what you'd like reviewers to know, how you applied the engineering principles, and any interesting tradeoffs made. 
)

### Jira ticket
_Link to related jira ticket ([Use the smart commits](https://support.atlassian.com/bitbucket-cloud/docs/use-smart-commits/)). Short version (e.g. MTT-123) also works and gets auto-linked_

### Changelog
[//]: # (updated with all public facing changes  - API changes, UI/UX changes, behaviour changes, bug fixes. Remove if not relevant.)

- Added: The package whose Changelog should be added to should be in the header. Delete the changelog section entirely if it's not needed.
- Fixed: If you update multiple packages, create a new section with a new header for the other package.
- Removed/Deprecated/Changed: Each bullet should be prefixed with Added, Fixed, Removed, Deprecated, or Changed to indicate where the entry should go.

<!--  Uncomment and mark items off with a * if this PR deprecates any API:
### Deprecated API
- [ ] An `[Obsolete]` attribute was added along with a `(RemovedAfter yyyy-mm-dd)` entry.
- [ ] An [api updater](https://confluence.unity3d.com/display/DEV/Obsolete+API+updaters) was added.
- [ ] Deprecation of the API is explained in the CHANGELOG.
- [ ] The users can understand why this API was removed and what they should use instead.
-->

## Documentation
[//]: # (
This section is REQUIRED and should mention what documentation changes were following the changes in this PR. 
We should always evaluate if the changes in this PR require any documentation changes.
)

- No documentation changes or additions were necessary.
- Includes documentation for previously-undocumented public API entry points.
- Includes edits to existing public API documentation.

## Testing & QA (How your changes can be verified during release Playtest)
[//]: #  (
This section is REQUIRED and should describe how the changes were tested and how should they be tested when Playtesting for the release.
It can range from "edge case covered by unit tests" to "manual testing required and new sample was added".
Expectation is that PR creator does some manual testing and provides a summary of it here.)

<!-- Add any performance testing results here if relevant. -->

### Functional Testing
[//]: # (If checked, List manual tests that have been performed.)
_Manual testing :_
- [ ] `Manual testing done`

_Automated tests:_
- [ ] `Covered by existing automated tests`
- [ ] `Covered by new automated tests`

_Does the change require QA team to:_

- [ ] `Review automated tests`?
- [ ] `Execute manual tests`?
- [ ] `Provide feedback about the PR`?

If any boxes above are checked the QA team will be automatically added as a PR reviewer.

## Up-port
[//]: # (
This section is REQUIRED and should link to the PR that targets develop-3.0.0 branch. Assuming that the PR lands agains default develop-2.0.0 branch
If this is not needed, for example feature specific to NGOv2.X, then just mention this fact.
)

## Backports
[//]: # (
This section is REQUIRED and should link to the PR that targets other NGO version which is either develop or develop-2.0.0 branch
Add the following to the PR title: "\[Backport\] ..."
If this is not needed, for example feature specific to NGOv2.X, then just mention this fact.
)
