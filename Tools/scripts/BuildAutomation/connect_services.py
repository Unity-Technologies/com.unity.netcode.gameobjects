"""
Modifies ProjectSettings.asset to connect the Unity project to services like Relay.
Updates cloudProjectId, organizationId, and projectName fields.
Default values are provided but can be overridden via command-line.
If no override is specified, checks if services are already connected before applying defaults.
"""

import re
import argparse

def parse_args():
    parser = argparse.ArgumentParser(description='Update ProjectSettings.asset to connect to services.')
    parser.add_argument('--project-settings-path', required=True,
                       help='Absolute path to ProjectSettings.asset file.')
    parser.add_argument('--cloud-project-ID', default=None,
                       help='Cloud project ID to connect to. If provided, --organization-ID and --project-name must also be provided.')
    parser.add_argument('--organization-ID', default=None,
                       help='Organization ID for the cloud project. If provided, --cloud-project-ID and --project-name must also be provided.')
    parser.add_argument('--project-name', default=None,
                       help='Project name to connect to. If provided, --cloud-project-ID and --organization-ID must also be provided.')
    args = parser.parse_args()

    # Validate that if any of the three service arguments is provided, all three must be provided
    service_args = [args.cloud_project_ID, args.organization_ID, args.project_name]
    provided_count = sum(1 for arg in service_args if arg is not None)

    if 0 < provided_count < 3:
        parser.error('If any of --cloud-project-ID, --organization-ID, or --project-name is provided, all three must be provided.')

    return args

def get_current_value(content, field_name):
    """Extract the current value of a field from the ProjectSettings.asset content."""
    # Match field_name: followed by optional spaces/tabs (NOT newlines) and value on the same line
    # Use ^ and $ anchors with MULTILINE flag to match line by line
    # Use [ \t]* instead of \s* to avoid matching newlines (which would capture next line's content)
    pattern = rf"^\s*{field_name}:[ \t]*(.*)$"
    match = re.search(pattern, content, re.MULTILINE)
    if match:
        value = match.group(1).strip()
        # Return value if it's non-empty and not a placeholder/empty indicator
        if value and value not in ['0', '""', "''", '{}']:
            return value
    return None

def main():
    args = parse_args()

    # Default values
    DEFAULT_CLOUD_PROJECT_ID = "ec34c3ba-b009-4677-8e50-d2772e1b87dc"
    DEFAULT_ORGANIZATION_ID = "ericktest" #3573857280227
    DEFAULT_PROJECT_NAME = "cmb"

    # Read the current file content
    with open(args.project_settings_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Check current values in the file
    current_cloud_id = get_current_value(content, "cloudProjectId")
    current_org_id = get_current_value(content, "organizationId")
    current_project_name = get_current_value(content, "projectName")
    current_cloud_enabled = get_current_value(content, "cloudEnabled")

    # Print original values
    print("\n" + "="*80)
    print("UNITY SERVICES CONFIGURATION")
    print("="*80)
    print(f"File: {args.project_settings_path}")
    print("\nOriginal values in ProjectSettings.asset:")
    print(f"  cloudProjectId: {current_cloud_id if current_cloud_id else '<empty>'}")
    print(f"  organizationId: {current_org_id if current_org_id else '<empty>'}")
    print(f"  projectName: {current_project_name if current_project_name else '<empty>'}")
    print(f"  cloudEnabled: {current_cloud_enabled if current_cloud_enabled else '<empty>'}")

    # Check if all three fields are already set (have non-empty values)
    all_fields_set = (current_cloud_id is not None and
                      current_org_id is not None and
                      current_project_name is not None)

    # Priority order:
    # 1) If script params provided (all 3 required) -> use them
    # 2) If ALL fields already have values in file -> keep them unchanged
    # 3) Otherwise (no params AND at least 1 field empty) -> apply defaults

    if args.cloud_project_ID is not None:
        # Priority 1: Command-line arguments provided
        cloud_project_id = args.cloud_project_ID
        organization_id = args.organization_ID
        project_name = args.project_name
        source = "command-line arguments"
        print("\n[Priority 1] Using command-line arguments (all three provided)")
    elif all_fields_set:
        # Priority 2: All fields already configured - keep existing values
        cloud_project_id = current_cloud_id
        organization_id = current_org_id
        project_name = current_project_name
        source = "existing values (already configured)"
        print("\n[Priority 2] Keeping existing values (all fields already have values)")
    else:
        # Priority 3: No params and at least one field is empty - apply defaults
        cloud_project_id = DEFAULT_CLOUD_PROJECT_ID
        organization_id = DEFAULT_ORGANIZATION_ID
        project_name = DEFAULT_PROJECT_NAME
        source = "default values"
        print("\n[Priority 3] Applying defaults (no params provided and at least one field was empty)")
        if current_cloud_id is None:
            print("  - cloudProjectId was empty")
        if current_org_id is None:
            print("  - organizationId was empty")
        if current_project_name is None:
            print("  - projectName was empty")

    # Build replacements dictionary
    replacements = {
        r"cloudProjectId:.*": f"cloudProjectId: {cloud_project_id}",
        r"organizationId:.*": f"organizationId: {organization_id}",
        r"projectName:.*": f"projectName: {project_name}",
        r"cloudEnabled:.*": "cloudEnabled: 1"
    }

    # Apply replacements
    for pattern, replacement in replacements.items():
        content = re.sub(pattern, replacement, content)

    # Write back to file
    with open(args.project_settings_path, 'w', encoding='UTF-8', newline='\n') as f:
        f.write(content)

    print("\nFinal values written:")
    print(f"  cloudProjectId: {cloud_project_id}")
    print(f"  organizationId: {organization_id}")
    print(f"  projectName: {project_name}")
    print("  cloudEnabled: 1")
    print(f"\nSource: {source}")
    print("="*80 + "\n")

if __name__ == "__main__":
    main()
