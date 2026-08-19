"""
Validates Yamato environment variables based on predefined rules.
Checks individual variable values and invalid combinations (e.g., incompatible platform/backend).
Exits with non-zero status if validation fails, halting the build process.
"""

import os
import sys

VALIDATION_RULES = {
    'SCRIPTING_BACKEND_IL2CPP_MONO': {'il2cpp', 'mono'},
    'BURST_ON_OFF': {'on', 'off'},
    'PLATFORM_WIN64_MAC_ANDROID': {'win64', 'mac', 'android'}
}

INVALID_COMBINATIONS = [
    ('mac', 'il2cpp', "Mac platform with il2cpp is not supported yet. "
                      "Windows build machines can only build Mac with mono."),
    ('android', 'mono', "Android platform with mono is not supported. "
                        "Mobile builds require il2cpp scripting backend.")
]

def validate_variables():
    errors = []
    for var_name, allowed_values in VALIDATION_RULES.items():
        value = os.environ.get(var_name, '').lower()
        if value not in {v.lower() for v in allowed_values}:
            errors.append(f"ERROR: Invalid {var_name}: '{value}'. "
                         f"Allowed values: {list(allowed_values)}")
    return errors

def validate_combinations():
    errors = []
    platform = os.environ.get('PLATFORM_WIN64_MAC_ANDROID', '').lower()
    scripting_backend = os.environ.get('SCRIPTING_BACKEND_IL2CPP_MONO', '').lower()

    for invalid_platform, invalid_backend, message in INVALID_COMBINATIONS:
        if platform == invalid_platform and scripting_backend == invalid_backend:
            errors.append(f"ERROR: Invalid Configuration: {message}")
    return errors

def main():
    errors = validate_variables() + validate_combinations()

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        print("\nOne or more parameters failed validation. Halting build.", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
