using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;


namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// Checks the current package version.
    /// If the version has changed, this will run a queue of package upgrade actions.
    /// Each <see cref="PackageUpgradeAction"/> can act on the upgrade if they need to.
    /// </summary>
    internal static class PackageVersionChecker
    {
        private static readonly Queue<PackageUpgradeAction> k_Actions = new();
        private static bool s_Debug = false;

        [InitializeOnLoadMethod]
        private static void OnApplicationLoaded()
        {
            EditorApplication.update += GetSettings;
        }

        /// <summary>
        /// Waits until all settings are valid and available.
        /// Then determines if the package was updated.
        /// If the package was updated, builds and configures the queue of actions.
        /// </summary>
        private static void GetSettings()
        {
            var settings = NetcodeForGameObjectsProjectSettings.instance;
            if (!settings || !GetCurrentPackageVersion(out var currentVersion))
            {
                return;
            }

            // Remove the settings update and add the editor update
            EditorApplication.update -= GetSettings;

            // No update is required if the current version matches the previous.
            if (settings.CurrentVersion == currentVersion)
            {
                LogInfo($"Current version matches the previous version: {currentVersion}. No upgrade needed");
                return;
            }

            LogInfo($"Detected package has upgraded! Current version: {currentVersion}, project previous version: {settings.CurrentVersion}");

            // Setup for updating the editor
            EditorApplication.update += EditorUpdate;

            PackageUpgradeAction.LastSerializedVersion = settings.CurrentVersion;
            PackageUpgradeAction.CurrentPackageVersion = currentVersion;
            PackageUpgradeAction.EnableVerboseLogging = s_Debug;
            k_Actions.Enqueue(new OnValidateAllOnUpgrade());

            // Update the current version on the project settings
            settings.CurrentVersion = currentVersion;
        }

        /// <summary>
        /// Acts as an Update function for package upgrading.
        /// Will deregister itself once all the upgrades are finished.
        /// </summary>
        private static void EditorUpdate()
        {
            // Peek at the current action
            if (!k_Actions.TryPeek(out var action))
            {
                // TryPeek returns false once the queue is empty.
                // Deregister this update method and return.
                EditorApplication.update -= EditorUpdate;
                return;
            }

            // Remove from the queue once the action is finished
            if (action.IsFinished())
            {
                k_Actions.Dequeue();
            }
            else
            {
                // If not finished, process the action.
                // This acts as an Update function.
                action.Process();
            }

        }

        private static PackageInfo GetPackageInfo(string packageName)
        {
            return AssetDatabase.FindAssets("package").Select(AssetDatabase.GUIDToAssetPath).Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null).Select(PackageInfo.FindForAssetPath).Where(x => x != null).First(x => x.name == packageName);
        }

        private static bool GetCurrentPackageVersion(out NgoVersion currentVersion)
        {
            var packageInfo = GetPackageInfo("com.unity.netcode.gameobjects");
            if (packageInfo != null)
            {
                var versionSplit = packageInfo.version.Split(".");
                if (versionSplit.Length == 3)
                {
                    var major = byte.Parse(versionSplit[0]);
                    var minor = byte.Parse(versionSplit[1]);
                    var patch = byte.Parse(versionSplit[2]);
                    currentVersion = new NgoVersion()
                    {
                        Major = major,
                        Minor = minor,
                        Patch = patch
                    };
                    return true;
                }
            }

            currentVersion = default;
            return false;
        }

        private static void LogInfo(string message)
        {
            if (s_Debug)
            {
                Debug.Log(message);
            }
        }
    }

    /// <summary>
    /// Semver representation of the NGO package
    /// </summary>
    [Serializable]
    internal struct NgoVersion : IEquatable<NgoVersion>, IComparable<NgoVersion>, IFormattable
    {
        public byte Major;
        public byte Minor;
        public byte Patch;

        public bool Equals(NgoVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            return obj is NgoVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        public int CompareTo(NgoVersion other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0)
            {
                return majorComparison;
            }

            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0)
            {
                return minorComparison;
            }

            return Patch.CompareTo(other.Patch);
        }

        public override string ToString()
        {
            return $"Ngo v{Major}.{Minor}.{Patch}";
        }

        public string ToString(string format)
        {
            return ToString();
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return ToString();
        }

        public static bool operator ==(NgoVersion left, NgoVersion right) => left.Equals(right);

        public static bool operator !=(NgoVersion left, NgoVersion right) => !left.Equals(right);

        public static bool operator >(NgoVersion left, NgoVersion right) => left.CompareTo(right) > 0;

        public static bool operator <(NgoVersion left, NgoVersion right) => left.CompareTo(right) < 0;

        public static bool operator >=(NgoVersion left, NgoVersion right) => left.CompareTo(right) >= 0;

        public static bool operator <=(NgoVersion left, NgoVersion right) => left.CompareTo(right) <= 0;
    }
}
