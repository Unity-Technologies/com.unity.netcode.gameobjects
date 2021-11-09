#!/usr/bin/env python3.9

import os
import sys
import glob
import argparse
import datetime
import subprocess


parser = argparse.ArgumentParser()

parser.add_argument("--check", action="store_true")
parser.add_argument("--fix", action="store_true")

parser.add_argument("--verbosity", default="minimal")
parser.add_argument("--project-path", default="testproject")
parser.add_argument("--project-glob", default="*.sln")

if len(sys.argv) == 1:
    parser.print_help(sys.stderr)
    exit(1)

args = parser.parse_args()


dotnet_min_ver = 6
ver_run = subprocess.run(["dotnet", "format", "--version"], capture_output=True)
if ver_run.returncode != 0:
    print("> dotnet format --version`")
    print("cannot execute version check command")
    print(f"please make sure to have dotnet {dotnet_min_ver}+ installed")
    print("https://dotnet.microsoft.com/download/dotnet")
    exit(1)

ver_run_str = ver_run.stdout.decode("utf-8")[:-1]
if int(ver_run_str[0]) < dotnet_min_ver:
    print("> dotnet format --version`")
    print(f"lower than minimum required version: {ver_run_str}")
    print(f"please make sure to upgrade to dotnet {dotnet_min_ver}+")
    print("https://dotnet.microsoft.com/download/dotnet")
    exit(1)


if args.check or args.fix:
    glob_match = os.path.join(args.project_path, args.project_glob)
    glob_files = glob.glob(glob_match)
    print(f"glob: found {len(glob_files)} files matching -> {glob_match}")

    if len(glob_files) == 0:
        print("glob: no project files found!")
        print("glob: \tdid you forget to generate your solution and project files in Unity?")
        print("glob: \tdid you double-check --project-path and/or --project-glob arguments?")
        exit(f"glob: failed")

    any_old = False
    for project_file in glob_files:
        file_stat = os.stat(project_file)
        check_days = 7
        modified_time = datetime.datetime.fromtimestamp(file_stat.st_mtime)
        days_ago_time = datetime.datetime.now() - datetime.timedelta(days=check_days)
        if modified_time < days_ago_time:
            any_old = True
            print(f"glob: last modified more than {check_days} days ago -> {project_file}")
    if any_old:
        print(f"glob: some project files are not modified for more than {check_days} days ago")
        print("glob: please consider regenerating project files in Unity")


if args.check:
    print("check: execute")

    any_error = False
    for project_file in glob_files:
        print(f"check: project -> {project_file}")

        print("check: whitespace")
        ws_run = subprocess.run(["dotnet", "format", project_file, "whitespace", "--no-restore", "--verify-no-changes", "--verbosity", args.verbosity])
        if ws_run.returncode != 0:
            print("check: whitespace failed")
            any_error = True

        print("check: code style")
        cs_run = subprocess.run(["dotnet", "format", project_file, "style", "--severity", "error", "--no-restore", "--verify-no-changes", "--verbosity", args.verbosity])
        if cs_run.returncode != 0:
            print("check: code style failed")
            any_error = True

    if any_error:
        exit("check: failed (see errors above)")
    else:
        print("check: succeeded")


if args.fix:
    print("fix: execute")

    any_error = False
    for project_file in glob_files:
        print(f"fix: project -> {project_file}")

        print("fix: whitespace")
        ws_run = subprocess.run(["dotnet", "format", project_file, "whitespace", "--no-restore", "--verbosity", args.verbosity])
        if ws_run.returncode != 0:
            print("fix: whitespace failed")
            any_error = True

        print("fix: code style")
        cs_run = subprocess.run(["dotnet", "format", project_file, "style", "--severity", "error", "--verify-no-changes", "--verbosity", args.verbosity])
        if cs_run.returncode != 0:
            print("fix: code style failed")
            any_error = True

    if any_error:
        exit("fix: failed (see errors above)")
    else:
        print("fix: succeeded")
