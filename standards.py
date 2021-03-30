#!/usr/bin/env python3

import os
import sys
import stat
import argparse

parser = argparse.ArgumentParser()

parser.add_argument("-i", "--install", action="store_true")
parser.add_argument("-r", "--remove", action="store_true")
parser.add_argument("-c", "--check", action="store_true")
parser.add_argument("-f", "--fix", action="store_true")

if len(sys.argv) == 1:
    parser.print_help(sys.stderr)
    exit(1)

args = parser.parse_args()


if args.install:
    print("execute install")

    version_exec = os.system("dotnet-format --version")
    if version_exec != 0:
        exit(
            "cannot execute `dotnet-format --version` command\n"
            "please make sure to have `dotnet-format` installed\n"
            "https://github.com/dotnet/format#how-to-install"
        )

    hook_path = "./.git/hooks/pre-push"
    if os.path.exists(hook_path):
        exit(
            f"git pre-push hook file already exists: `{hook_path}`\n"
            "please make sure to backup and delete pre-push hook file\n"
            "installation WILL NOT continue and override existing file"
        )

    print("write git pre-push hook file contents")
    hook_file = open(hook_path, "w")
    hook_file.write(f"#!/bin/sh\n\npython3 {os.path.basename(sys.argv[0])} --check\n")
    hook_file.close()

    print("make git pre-push hook file executable")
    hook_stat = os.stat(hook_path)
    os.chmod(hook_path, hook_stat.st_mode | stat.S_IEXEC)

    print("installation complete!")


if args.remove:
    print("execute remove")
    # todo


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
