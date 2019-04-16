using System;
using System.IO;
using System.Reflection;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Messaging
{
    internal class ReflectionMethod
    {
        private MethodInfo method;
        private Type[] parameterTypes;
        private object[] parameterRefs;
        
        public ReflectionMethod(MethodInfo methodInfo)
        {
            method = methodInfo;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            parameterTypes = new Type[parameters.Length];
            parameterRefs = new object[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }
        }

        internal object Invoke(object instance, Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterRefs[i] = reader.ReadObjectPacked(parameterTypes[i]);
                }

                return method.Invoke(instance, parameterRefs);
            }
        }
    }
}