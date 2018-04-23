using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;

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

            ulong value = PrimitiveHasher.GetULongHash(name);
            messageAttributeHashes.Add(name, value);
            messageAttributeNames.Add(value, name);
            return value;
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

            ulong value = PrimitiveHasher.GetULongHash(name);
            messageAttributeHashes.Add(name, value);
            messageAttributeNames.Add(value, name);
        }
    }
}
