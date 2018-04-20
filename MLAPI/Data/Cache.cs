using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MLAPI.Data
{
    internal static class Cache
    {
        internal static Dictionary<string, ulong> messageAttributeHashes = new Dictionary<string, ulong>();
        internal static Dictionary<ulong, string> messageAttributeNames = new Dictionary<ulong, string>();

        internal static ulong GetMessageAttributeHash(string name)
        {
            if (messageAttributeHashes.ContainsKey(name))
                return messageAttributeHashes[name];

            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
                ulong value = hash[0] | ((ulong)hash[1] << 8) | ((ulong)hash[2] << 16) | ((ulong)hash[3] << 24) | ((ulong)hash[4] << 32) | ((ulong)hash[5] << 40) | ((ulong)hash[6] << 48) | ((ulong)hash[7] << 56);
                //ulong value = hash[0] | ((uint)hash[1] << 8) | ((uint)hash[2] << 16) | ((uint)hash[3] << 24);
                messageAttributeHashes.Add(name, value);
                messageAttributeNames.Add(value, name);
                return value;
            }
        }

        internal static string GetAttributeMethodName(ulong hash)
        {
            if (messageAttributeNames.ContainsKey(hash))
                return messageAttributeNames[hash];
            else
                return string.Empty;
        }

        internal static void RegisterMessageAttributeName(string name)
        {
            if (messageAttributeHashes.ContainsKey(name))
                return;

            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
                ulong value = hash[0] | ((ulong)hash[1] << 8) | ((ulong)hash[2] << 16) | ((ulong)hash[3] << 24) | ((ulong)hash[4] << 32) | ((ulong)hash[5] << 40) | ((ulong)hash[6] << 48) | ((ulong)hash[7] << 56);
                //ulong value = hash[0] | ((uint)hash[1] << 8) | ((uint)hash[2] << 16) | ((uint)hash[3] << 24);
                messageAttributeHashes.Add(name, value);
                messageAttributeNames.Add(value, name);
            }
        }
    }
}
