using System;

namespace MLAPI.Data
{
    internal static class TypeExtensions
    {
        internal static bool HasInterface(this Type type, Type interfaceType)
        {
            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i] == interfaceType)
                    return true;
            }
            return false;
        }
    }
}
