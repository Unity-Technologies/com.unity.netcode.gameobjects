using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    public class PackageMetadata : ScriptableObject
    {
        internal static string VersionString;
        internal static FixedString32Bytes VersionFixedString;

        public static FixedString32Bytes Version
        {
            get
            {
                if (VersionFixedString.IsEmpty)
                {
                    VersionFixedString = VersionString;
                }

                return VersionFixedString;
            }
        }
    }
}
