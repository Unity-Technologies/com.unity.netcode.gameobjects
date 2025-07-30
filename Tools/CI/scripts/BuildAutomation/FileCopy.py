import os
import shutil
import sys

def main():
    """
    Cleans and prepares the 'Assets/Scripts/Editor' directory for build automation scripts.
    It deletes the directory if it exists, recreates it, and copies in the necessary
    assembly definition and C# script files.
    """
    # --- 1. Argument Validation ---
    if len(sys.argv) < 2:
        print("Error: Missing required argument.")
        print("Usage: python prepare_build_scripts.py <path_to_project_root>")
        sys.exit(1)

    project_root = sys.argv[1]

    # --- 2. Define File Paths ---
    # The target directory inside the Unity project
    target_dir = os.path.join(project_root, 'Assets', 'Scripts', 'Editor')

    # The source files for build automation
    source_asmdef = 'Tools/CI/scripts/BuildAutomation/Unity.ProjectBuild.Editor.asmdef'
    source_script = 'Tools/CI/scripts/BuildAutomation/BuilderScripts.cs'

    print(f"Preparing build scripts for project at: {project_root}")
    print(f"Target editor script directory: {target_dir}")

    # --- 3. Clean and Recreate Directory ---
    try:
        if os.path.exists(target_dir):
            print(f"Directory '{target_dir}' exists. Removing it.")
            shutil.rmtree(target_dir)

        print(f"Creating directory: {target_dir}")
        os.makedirs(target_dir)

    except OSError as e:
        print(f"Error managing directory: {e}")
        sys.exit(1)

    # --- 4. Copy Build Automation Files ---
    try:
        print(f"Copying '{source_asmdef}' to '{target_dir}'")
        shutil.copy(source_asmdef, target_dir)

        print(f"Copying '{source_script}' to '{target_dir}'")
        shutil.copy(source_script, target_dir)

    except IOError as e:
        print(f"Error copying files: {e}")
        sys.exit(1)

    print("\nSuccessfully prepared build automation scripts.")

if __name__ == "__main__":
    main()

