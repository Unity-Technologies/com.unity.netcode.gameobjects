#!/bin/bash

set -eo pipefail

echo "Building and installing netcode standards"
dotnet pack dotnet-tools/netcode.standards
dotnet tool update --global --add-source ./dotnet-tools/netcode.standards netcode.standards

echo "Installing git hooks"
cp env/git-hooks/* ./.git/hooks/