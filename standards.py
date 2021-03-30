#!/usr/bin/env python3

import os
import sys
import stat
import argparse

parser = argparse.ArgumentParser()

parser.add_argument("-i", "--hook", action="store_true")
parser.add_argument("-r", "--unhook", action="store_true")
parser.add_argument("-c", "--check", action="store_true")
parser.add_argument("-f", "--fix", action="store_true")

if len(sys.argv) == 1:
    parser.print_help(sys.stderr)
    exit(1)

args = parser.parse_args()


hook_path = "./.git/hooks/pre-push"
hook_exec = f"python3 {os.path.basename(sys.argv[0])} --check"

if args.hook:
    print("execute hook")

    if os.path.exists(hook_path):
        exit(
            f"fail: git pre-push hook file already exists: `{hook_path}`\n"
            "please make sure to backup and delete the existing pre-push hook file"
        )

    print("write git pre-push hook file contents")
    hook_file = open(hook_path, "w")
    hook_file.write(f"#!/bin/sh\n\n{hook_exec}\n")
    hook_file.close()

    print("make git pre-push hook file executable")
    hook_stat = os.stat(hook_path)
    os.chmod(hook_path, hook_stat.st_mode | stat.S_IEXEC)

    print("succeed: git pre-push hook created!")


if args.unhook:
    print("execute unhook")

    hook_path = "./.git/hooks/pre-push"
    if os.path.isfile(hook_path):
        print(f"found `{hook_path}`")
        delete = False
        hook_file = open(hook_path, "r")
        if hook_exec in hook_file.read():
            delete = True
        else:
            exit(f"fail: existing git pre-push hook file was not created by this script")
        hook_file.close()
        if delete:
            os.remove(hook_path)
            print(f"delete file: `{hook_path}`")

    print("succeed: git pre-push hook removed!")


if args.check:
    print("execute check")

    check_exec = os.system("dotnet-format ./testproject/testproject.sln --fix-whitespace --fix-style error --check")
    if check_exec != 0:
        exit(f"check_exec failed, exit code: {check_exec}")


if args.fix:
    print("execute fix")

    fix_exec = os.system("dotnet-format ./testproject/testproject.sln --fix-whitespace --fix-style error")
    if fix_exec != 0:
        exit(f"fix_exec failed, exit code: {fix_exec}")
