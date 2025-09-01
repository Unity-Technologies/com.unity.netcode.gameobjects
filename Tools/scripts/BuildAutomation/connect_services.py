# This script modifies ProjectSettings.asset in order to connect the project to Services (like Relay). This is needed for Netcode related builds and can be probably skipped in other cases
# As a Netcode team we usually use (during Playtesting) the following settings

# Note that cloudProjectId, projectName and organizationId are defined as secrets

# Notice that those parameters are included as defaults but in your yml file you can override them with your own values.

import sys
import re
import argparse
import os

def parse_args():
    global args
    parser = argparse.ArgumentParser(description='Update ProjectSettings.asset in order to properly connect to services.')
    parser.add_argument('--project-settings-path', required=True, help='The absolute path to the project ProjectSettings.asset file that contains all of the services settings.')
    parser.add_argument('--cloud-project-ID', default=os.getenv("CLOUDPROJECTID"), help='ID of a cloud project to which we want to connect to.')
    parser.add_argument('--organization-ID', default=os.getenv("ORGANIZATIONID"), help="ID of the organization to which the cloud project belongs.")
    parser.add_argument('--project-name', default=os.getenv("PROJECTNAME"), help='Name of the project to which we want to connect to.')
    args = parser.parse_args()

def main():
    """
    Modifies ProjectSettings.asset in order to connect the project to Services
    """
    parse_args()

    with open(args.project_settings_path, 'r') as f:
        content = f.read()

    # Use regex to replace the values. This is safer than simple string replacement.
    content = re.sub(r"cloudProjectId:.*", f"cloudProjectId: {args.cloud_project_ID}", content)
    content = re.sub(r"organizationId:.*", f"organizationId: {args.organization_ID}", content)
    content = re.sub(r"projectName:.*", f"projectName: {args.project_name}", content)
    # Ensure the project is marked as connected
    content = re.sub(r"cloudEnabled:.*", "cloudEnabled: 1", content)

    with open(args.project_settings_path, 'w') as f:
        f.write(content)

    print(f"[Linker] Successfully updated {args.project_settings_path} with Project ID: {args.cloud_project_ID}, Org ID: {args.organization_ID}, Project Name: {args.project_name}")

if __name__ == "__main__":
    main()


