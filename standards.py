#!/usr/bin/env python3

import os
import sys
import stat
import glob
import argparse
import datetime


parser = argparse.ArgumentParser()

parser.add_argument("--hook", action="store_true")
parser.add_argument("--unhook", action="store_true")
parser.add_argument("--check", action="store_true")
parser.add_argument("--fix", action="store_true")
parser.add_argument("--yamato", action="store_true")

parser.add_argument("--verbosity", default="minimal")
parser.add_argument("--tool-path", default="dotnet-format")
parser.add_argument("--project-path", default="testproject")
parser.add_argument("--project-glob", default="*.sln")

if len(sys.argv) == 1:
    parser.print_help(sys.stderr)
    exit(1)

args = parser.parse_args()


hook_path = "./.git/hooks/pre-push"
hook_exec = f"python3 {os.path.basename(sys.argv[0])} --check"

if args.hook:
    print("hook: execute")

    if os.path.exists(hook_path):
        print(f"hook: git pre-push hook file already exists: `{hook_path}`")
        print("hook: please make sure to backup and delete the existing pre-push hook file")
        exit("hook: failed")

    print("hook: write git pre-push hook file contents")
    hook_file = open(hook_path, "w")
    hook_file.write(f"#!/bin/sh\n\n{hook_exec}\n")
    hook_file.close()

    print("hook: make git pre-push hook file executable")
    hook_stat = os.stat(hook_path)
    os.chmod(hook_path, hook_stat.st_mode | stat.S_IEXEC)

    print("hook: succeeded")


if args.unhook:
    print("unhook: execute")

    hook_path = "./.git/hooks/pre-push"
    if os.path.isfile(hook_path):
        print(f"unhook: found file -> `{hook_path}`")
        delete = False
        hook_file = open(hook_path, "r")
        if hook_exec in hook_file.read():
            delete = True
        else:
            print("unhook: existing git pre-push hook file was not created by this script")
            exit("unhook: failed")
        hook_file.close()
        if delete:
            os.remove(hook_path)
            print(f"unhook: delete file -> `{hook_path}`")

    print("unhook: succeeded")


if args.check or args.fix or args.yamato:
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
        any_error = 0 != os.system(f"{args.tool_path} {project_file} --fix-whitespace --fix-style error --check --verbosity {args.verbosity}") or any_error

    if any_error:
        exit("check: failed")

    print("check: succeeded")


if args.fix:
    print("fix: execute")

    any_error = False
    for project_file in glob_files:
        print(f"fix: project -> {project_file}")
        any_error = 0 != os.system(f"{args.tool_path} {project_file} --fix-whitespace --fix-style error --verbosity {args.verbosity}") or any_error

    if any_error:
        exit("fix: failed")

    print("fix: succeeded")

if args.yamato:
    print("yamato: execute")

    for project_file in glob_files:
        print(f"yamato: project -> {project_file}")
        yamato_exec = os.system(f"{args.tool_path} {project_file} --fix-style error --check --verbosity {args.verbosity}")
        if yamato_exec != 0:
            exit(f"yamato: failed, exit code -> {yamato_exec}")

    print("yamato: succeeded")
