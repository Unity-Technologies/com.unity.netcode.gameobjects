# How to contribute

I'm really glad you're reading this, because we need volunteer developers to help this project evolve.

If you haven't already, come find us on [Discord](http://discord.mlapi.network).

## Submitting changes

Please send a [GitHub Pull Request to MLAPI](https://github.com/midlevel/MLAPI/pull/new/master) with a clear list of what you've done (read more about [pull requests](http://help.github.com/pull-requests/)).
Please follow our commit message conventions (below) and make sure all of your commits are atomic (one feature per commit).

## Commit Message Guidelines

We have very precise rules over how our git commit messages can be formatted.  This leads to **more
readable messages** that are easy to follow when looking through the **project history**.  But also,
we use the git commit messages to **generate the MLAPI change log**. We follow angular's message format.

### Commit Message Format
Each commit message consists of a **header**, a **body** and a **footer**.  The header has a special
format that includes a **type**, a **scope** and a **subject**:

```
<type>(<scope>): <subject>
<BLANK LINE>
<body>
<BLANK LINE>
<footer>
```

The **header** is mandatory and the **scope** of the header is optional.

Any line of the commit message cannot be longer 100 characters! This allows the message to be easier
to read on GitHub as well as in various git tools.

The footer should contain a [closing reference to an issue](https://help.github.com/articles/closing-issues-via-commit-messages/) if any.

If the change isn't backwards compatible. You have to disclose that in the footer like this. Here is an example:
```
perf(networked-vars): Improved performance by removing duplex functionality

BREAKING CHANGE: Removes duplex functionality from networked vars in order to improve performance
```
Samples:

```
docs(changelog): update changelog to beta.5
```
```
fix(release): need to depend on latest rxjs and zone.js

The version in our package.json gets copied to the one we publish, and users need the latest of these.
```

### Revert
If the commit reverts a previous commit, it should begin with `revert: `, followed by the header of the reverted commit. In the body it should say: `This reverts commit <hash>.`, where the hash is the SHA of the commit being reverted.

### Type
Must be one of the following:

* **build**: Changes that affect the build system or external dependencies (example scopes: msbuild, xUnit)
* **ci**: Changes to our CI configuration files and scripts (example scopes: AppVeyor, xUnit)
* **docs**: Documentation only changes
* **feat**: A new feature
* **fix**: A bug fix
* **perf**: A code change that improves performance
* **refactor**: A code change that neither fixes a bug nor adds a feature
* **style**: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
* **test**: Adding missing tests or correcting existing tests

Always write a clear log message for your commits. One-line messages are fine for small changes, but bigger changes should look like this:

    $ git commit -m "A brief summary of the commit
    > 
    > A paragraph describing what changed and its impact."

Thanks,
Albin Corén
