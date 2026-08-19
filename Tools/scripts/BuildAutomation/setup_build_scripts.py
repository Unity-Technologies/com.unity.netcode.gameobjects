"""
Helper script to set up build scripts in the cloned project.
Handles directory creation and file copying for build automation.
"""

import argparse
import os
import shutil

def parse_args():
    parser = argparse.ArgumentParser(description='Set up build scripts in cloned project.')
    parser.add_argument('--project-path', required=True,
                       help='Path to the cloned project root.')
    parser.add_argument('--source-dir', required=True,
                       help='Source directory containing build automation scripts.')
    return parser.parse_args()

def main():
    args = parse_args()
    target_dir = os.path.join(args.project_path, 'Assets', 'CIScripts', 'Editor')
    source_dir = args.source_dir

    os.makedirs(target_dir, exist_ok=True)

    files_to_copy = [
        'Unity.ProjectBuild.Editor.asmdef',
        'BuilderScripts.cs'
    ]

    for filename in files_to_copy:
        source_path = os.path.join(source_dir, 'Tools', 'CI', 'scripts', 'BuildAutomation', filename)
        target_path = os.path.join(target_dir, filename)
        shutil.copy(source_path, target_path)
        print(f"Copied {filename} to {target_dir}")

if __name__ == "__main__":
    main()
