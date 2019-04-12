using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MLAPI.Logging;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static class ExternalSerializationManager
    {
        private static readonly Dictionary<Type, MethodInfo> cachedExternalSerializers = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> cachedExternalDeserializers = new Dictionary<Type, MethodInfo>();

        static ExternalSerializationManager()
        {
            CacheAllHandlers();
        }

        private static void CacheAllHandlers()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (MethodInfo method in methods)
                    {
                        if (method.IsDefined(typeof(SerializerAttribute), true))
                        {
                            ParameterInfo[] parameters = method.GetParameters();
                            
                            if (!method.IsStatic)
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("The Serializer method " + type.FullName + "." + method.Name + " has to be static.");
                            }
                            else if (method.ReturnType != typeof(void) || parameters.Length != 2 || parameters[0].ParameterType != typeof(Stream))
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("The Serializer method " + type.FullName + "." + method.Name + " has the wrong signature.");
                            }
                            else
                            {
                                cachedExternalSerializers.Add(parameters[1].ParameterType, method);
                            }
                        }
                        
                        if (method.IsDefined(typeof(DeserializerAttribute), true))
                        {
                            ParameterInfo[] parameters = method.GetParameters();

                            if (!method.IsStatic)
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("The Deserializer method " + type.FullName + "." + method.Name + " has to be static.");
                            }
                            else if (method.ReturnType == typeof(void) || parameters.Length != 1 || parameters[0].ParameterType != typeof(Stream))
                            {
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogError("The Deserializer method " + type.FullName + "." + method.Name + " has the wrong signature.");
                            }
                            else
                            {
                                cachedExternalDeserializers.Add(method.ReturnType, method);
                            }
                        }
                    }
                }
            }
        }

        internal static bool TrySerialize(Stream stream, object obj)
        {
            if (cachedExternalSerializers.ContainsKey(obj.GetType()))
            {
                cachedExternalSerializers[obj.GetType()].Invoke(null, new object[] {stream, obj});
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool TryDeserialize(Stream stream, Type type, out object obj)
        {
            if (cachedExternalDeserializers.ContainsKey(type))
            {
                obj = cachedExternalDeserializers[type].Invoke(null, new object[] {stream});
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }
    }
}