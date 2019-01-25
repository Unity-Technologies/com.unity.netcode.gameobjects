using System;
using System.IO;
using System.Reflection;
using MLAPI.Serialization;

namespace MLAPI.Configuration
{
    /// <summary>
    /// Represents the length of a var int encoded hash
    /// Note that the HashSize does not say anything about the actual final output due to the var int encoding
    /// It just says how many bytes the maximum will be
    /// </summary>
    public enum HashSize
    {
        /// <summary>
        /// Two byte hash
        /// </summary>
        VarIntTwoBytes,
        /// <summary>
        /// Four byte hash
        /// </summary>
        VarIntFourBytes,
        /// <summary>
        /// Eight byte hash
        /// </summary>
        VarIntEightBytes
    }
}

namespace MLAPI
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public delegate void RpcDelegate(uint clientId, Stream stream);

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

    /// <summary>
    /// Attribute used on methods to me marked as ServerRPC
    /// ServerRPC methods can be requested from a client and will execute on the server
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ServerRPC : Attribute
    {
        /// <summary>
        /// Whether or not the ServerRPC should only be run if executed by the owner of the object
        /// </summary>
        public bool RequireOwnership = true;
        internal ReflectionMethod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }
    
    /// <summary>
    /// Attribute used on methods to me marked as ClientRPC
    /// ClientRPC methods can be requested from the server and will execute on a client
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ClientRPC : Attribute
    {
        internal ReflectionMethod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }
    
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
