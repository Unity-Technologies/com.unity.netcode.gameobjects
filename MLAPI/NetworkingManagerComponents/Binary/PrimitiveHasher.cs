using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    public static class PrimitiveHasher
    {
        private static Dictionary<string, ulong> ulongCachedHashes = new Dictionary<string, ulong>();
        private static Dictionary<string, ushort> ushortCachedHashes = new Dictionary<string, ushort>();
        private static Dictionary<string, uint> uintCachedHashes = new Dictionary<string, uint>();

        public static ulong GetULongHash(string input, bool cache = false)
        {
            if (cache && ulongCachedHashes.ContainsKey(input))
                return ulongCachedHashes[input];

            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                ulong value =  hash[0] | ((ulong)hash[1] << 8) | ((ulong)hash[2] << 16) | ((ulong)hash[3] << 24) | ((ulong)hash[4] << 32) | ((ulong)hash[5] << 40) | ((ulong)hash[6] << 48) | ((ulong)hash[7] << 56);
                if (cache)
                    ulongCachedHashes.Add(input, value);
                return value;
            }
        }

        public static ulong GetUIntHash(string input, bool cache = false)
        {
            if (cache && uintCachedHashes.ContainsKey(input))
                return ulongCachedHashes[input];

            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                uint value = hash[0] | ((uint)hash[1] << 8) | ((uint)hash[2] << 16) | ((uint)hash[3] << 24);
                if (cache)
                    uintCachedHashes.Add(input, value);
                return value;
            }
        }

        public static ushort GetUShortHash(string input, bool cache = false)
        {
            if (cache && ushortCachedHashes.ContainsKey(input))
                return ushortCachedHashes[input];

            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                ushort value = (ushort)(hash[0] | (ushort)(hash[1] << 8));
                if (cache)
                    ushortCachedHashes.Add(input, value);
                return value;
            }
        }
    }
}
