using UnityEngine;

namespace Unity.Netcode.Editor.Configuration
{
    /// <summary>
    /// An action that can be called to act when NGO version was detected to change
    /// </summary>
    internal class PackageUpgradeAction
    {
        // Context for the current action
        public static NgoVersion LastSerializedVersion;
        public static NgoVersion CurrentPackageVersion;
        public static bool EnableVerboseLogging = true;

        /// <inheritdoc cref="IsFinished"/>
        protected virtual bool OnIsFinished() => false;

        /// <summary>
        /// Whether this upgrade action has finished processing
        /// </summary>
        /// <returns>true if finished; false otherwise</returns>
        public bool IsFinished()
        {
            return OnIsFinished();
        }

        /// <summary>
        /// Continue processing this action.
        /// This function will be called on each EditorUpdate.
        /// It will stop being called once <see cref="OnIsFinished"/> returns true
        /// </summary>
        protected virtual void OnProcess() { }

        /// <summary>
        /// Continue processing this action.
        /// </summary>
        public void Process()
        {
            OnProcess();
        }

        /// <summary>
        /// Whether or not the <see cref="LastSerializedVersion"/> is older than <see cref="toCheckAgainst"/>
        /// </summary>
        /// <param name="toCheckAgainst">The ngo version to check against for whether a upgrade is needed</param>
        /// <returns></returns>
        protected static bool PackageVersionNeedsUpgrade(NgoVersion toCheckAgainst)
        {
            return LastSerializedVersion < toCheckAgainst;
        }

        internal static void LogInfo(string msg)
        {
            if (EnableVerboseLogging)
            {
                Debug.Log(msg);
            }
        }
    }
}
