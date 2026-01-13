#!/bin/sh

set -e # fail on first error

SCRIPT_DIR=$(dirname "$0")
dotnet run --project $SCRIPT_DIR/CI/NGO.Cookbook.csproj "$@"