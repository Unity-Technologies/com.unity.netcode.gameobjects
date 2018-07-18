using System;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using MLAPI.NetworkingManagerComponents.Binary;

namespace MLAPI.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum AttributeMessageMode
    {
        WovenTwoByte,
        WovenFourByte,
        WovenEightByte
    }
    
    public delegate void RpcDelegate(uint clientId, BitReader reader);

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

        public void Invoke(object instance, BitReader reader)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                //TODO: BitReader ReadType
                parameterRefs[i] = reader.ReadObject(parameterTypes[i]);
            }
            
            method.Invoke(instance, parameterRefs);
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
