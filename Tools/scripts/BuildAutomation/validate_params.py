import os
import sys

# --- Configuration ---
# A dictionary that maps each environment variable to a set of its allowed values.
# This is the single source of truth for all validation.
VALIDATION_RULES = {
    'SCRIPTING_BACKEND_IL2CPP_MONO': {'il2cpp', 'mono'},
    'BURST_ON_OFF': {'on', 'off'},
    'PLATFORM_WIN64_MAC_ANDROID': {'win64', 'mac', 'android'}
}

def main():
    """
    Validates Yamato environment variables using a rule-based dictionary.
    Exits with 1 if any variable is invalid, otherwise exits with 0.
    """
    all_params_valid = True

    for var_name, allowed_values in VALIDATION_RULES.items():
        actual_value = os.environ.get(var_name, '').lower()
        allowed_values_lower = {v.lower() for v in allowed_values}

        if actual_value not in allowed_values_lower:
            print(
                f"ERROR: Invalid {var_name}: '{actual_value}'. "
                f"Allowed values are: {list(allowed_values)}",
                file=sys.stderr
            )
            all_params_valid = False

    platform = os.environ.get('PLATFORM_WIN64_MAC_ANDROID', '').lower()
    scripting_backend = os.environ.get('SCRIPTING_BACKEND_IL2CPP_MONO', '').lower()

    if platform == 'mac' and scripting_backend == 'il2cpp':
        print(
            "ERROR: Invalid Configuration: The 'mac' platform with the 'il2cpp' "
            "Note that for now windows machine is used for building project and it's a known limitation that mac builds (via windows machine) can be done only with mono",
            file=sys.stderr
        )
        all_params_valid = False

    if platform == 'android' and scripting_backend == 'mono':
        print(
            "ERROR: Invalid Configuration: The 'android' platform with the 'mono' "
            "Note that mobile builds are not supporting mono and need il2cpp scripting backend",
            file=sys.stderr
        )
        all_params_valid = False

    if not all_params_valid:
        print("\nOne or more parameters failed validation. Halting build.", file=sys.stderr)
        sys.exit(1)

    print("All parameters are valid. Proceeding with the build.")

if __name__ == "__main__":
    main()