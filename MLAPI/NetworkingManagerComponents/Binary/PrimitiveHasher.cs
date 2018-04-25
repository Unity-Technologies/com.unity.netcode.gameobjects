using MLAPI.NetworkingManagerComponents.Cryptography;
using System.Collections.Generic;
using System.Text;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    /// <summary>
    /// A helper class for hashing strings to primitive types
    /// </summary>
    public static class PrimitiveHasher
    {
        private static Dictionary<string, ulong> ulongCachedHashes = new Dictionary<string, ulong>();
        private static Dictionary<string, ushort> ushortCachedHashes = new Dictionary<string, ushort>();
        private static Dictionary<string, uint> uintCachedHashes = new Dictionary<string, uint>();

        /// <summary>
        /// Gets a ulong hash from a string
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cache">Wheter or not cache the hash</param>
        /// <returns>The hash result</returns>
        public static ulong GetULongHash(string input, bool cache = false)
        {
            if (cache && ulongCachedHashes.ContainsKey(input))
                return ulongCachedHashes[input];

            ulong value = MessageDigest.SHA1_Opt(Encoding.UTF8.GetBytes(input)).CastULong();
            if (cache)
                ulongCachedHashes.Add(input, value);
            return value;
        }

        /// <summary>
        /// Gets a uint hash from a string
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cache">Wheter or not cache the hash</param>
        /// <returns>The hash result</returns>
        public static ulong GetUIntHash(string input, bool cache = false)
        {
            if (cache && uintCachedHashes.ContainsKey(input))
                return uintCachedHashes[input];

            uint value = MessageDigest.SHA1_Opt(Encoding.UTF8.GetBytes(input)).CastUInt();
            if (cache)
                uintCachedHashes.Add(input, value);
            return value;
        }

        /// <summary>
        /// Gets a ushort hash from a string
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="cache">Wheter or not cache the hash</param>
        /// <returns>The hash result</returns>
        public static ushort GetUShortHash(string input, bool cache = false)
        {
            if (cache && ushortCachedHashes.ContainsKey(input))
                return ushortCachedHashes[input];

            ushort value = MessageDigest.SHA1_Opt(Encoding.UTF8.GetBytes(input)).CastUShort();
            if (cache)
                ushortCachedHashes.Add(input, value);
            return value;
        }
    }
}
