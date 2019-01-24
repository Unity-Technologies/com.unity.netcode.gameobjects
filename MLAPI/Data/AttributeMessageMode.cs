using System;
using System.IO;
using System.Reflection;
using MLAPI.Serialization;

namespace MLAPI.Configuration
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum HashSize
    {
        VarIntTwoBytes,
        VarIntFourBytes,
        VarIntEightBytes
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

namespace MLAPI
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public delegate void RpcDelegate(uint clientId, Stream stream);

    public class ReflectionMehtod
    {
        public MethodInfo method;
        public Type[] parameterTypes;
        public object[] parameterRefs;
        
        public ReflectionMehtod(MethodInfo methodInfo)
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

        public object Invoke(object instance, Stream stream)
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

    public class ServerRPC : Attribute
    {
        public bool RequireOwnership = true;
        internal ReflectionMehtod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }
    
    public class ClientRPC : Attribute
    {
        internal ReflectionMehtod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }
    
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
