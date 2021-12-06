# .NET Tools for Unity Netcode

## Netcode.Standards

C# coding standards tool for Netcode that relies on `.editorconfig` ruleset and `dotnet format` tool

### How to Install & Uninstall

Build (Pack)

```zsh
dotnet pack dotnet-tools/netcode.standards
```

Install

```zsh
dotnet tool install --global --add-source ./dotnet-tools/netcode.standards netcode.standards
```

Check

```zsh
netcode.standards --help
```

Uninstall

```zsh
dotnet tool uninstall --global netcode.standards
```

### How to Use

#### Commands

Check

```zsh
# check for standards issues without touching files
netcode.standards --check
```

Fix
```zsh
# try to fix standards issues and save file changes
netcode.standards --fix
```

#### Options

Specifying at least one of `--check` or `--fix` is required.

However, you can also specify other options to configure the tool.

|Option|Description|
|:-|:-|
|`--project <project>`|Target project folder [default: testproject]|
|`--pattern <pattern>`|Search pattern string [default: *.sln]|
|`--verbosity <verbosity>`|Logs verbosity level [default: normal]|
|`-?`, `-h`, `--help`|Show help and usage information|
