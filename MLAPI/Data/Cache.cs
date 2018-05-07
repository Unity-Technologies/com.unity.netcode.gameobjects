using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;

namespace MLAPI.Data
{
    internal static class Cache
    {
        internal static readonly Dictionary<string, ulong> messageAttributeHashes = new Dictionary<string, ulong>();
        internal static readonly Dictionary<ulong, string> messageAttributeNames = new Dictionary<ulong, string>();

        internal static ulong GetMessageAttributeHash(string name, AttributeMessageMode mode)
        {
            if (messageAttributeHashes.ContainsKey(name))
                return messageAttributeHashes[name];

            ulong value = 0;
            switch (mode)
            {
                case AttributeMessageMode.WovenTwoByte:
                    value = PrimitiveHasher.GetUShortHash(name);
                    break;
                case AttributeMessageMode.WovenFourByte:
                    value = PrimitiveHasher.GetUIntHash(name);
                    break;
                case AttributeMessageMode.WovenEightByte:
                    value = PrimitiveHasher.GetULongHash(name);
                    break;
                case AttributeMessageMode.Disabled:
                    value = 0;
                    break;
            }

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

        internal static void RegisterMessageAttributeName(string name, AttributeMessageMode mode)
        {
            if (messageAttributeHashes.ContainsKey(name))
                return;

            ulong value = 0;
            switch (mode)
            {
                case AttributeMessageMode.WovenTwoByte:
                    value = PrimitiveHasher.GetUShortHash(name);
                    break;
                case AttributeMessageMode.WovenFourByte:
                    value = PrimitiveHasher.GetUIntHash(name);
                    break;
                case AttributeMessageMode.WovenEightByte:
                    value = PrimitiveHasher.GetULongHash(name);
                    break;
                case AttributeMessageMode.Disabled:
                    value = 0;
                    break;
            }

            messageAttributeHashes.Add(name, value);
            messageAttributeNames.Add(value, name);
        }
    }
}
