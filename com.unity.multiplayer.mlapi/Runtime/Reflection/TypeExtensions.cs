using System;

namespace Unity.Netcode
{
    internal static class TypeExtensions
    {
        internal static bool HasInterface(this Type type, Type interfaceType)
        {
            var ifaces = type.GetInterfaces();
            for (int i = 0; i < ifaces.Length; i++)
            {
                if (ifaces[i] == interfaceType)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsNullable(this Type type)
        {
            if (!type.IsValueType)
            {
                return true; // ref-type
            }

            return Nullable.GetUnderlyingType(type) != null;
        }
    }
}
