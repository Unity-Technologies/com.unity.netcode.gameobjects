using System;
using System.IO;
using System.Reflection;
using MLAPI.Logging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Messaging
{
    internal class ReflectionMethod
    {
        internal static ReflectionMethod Create(MethodInfo method, ParameterInfo[] parameters, int index)
        {
            RPCAttribute[] attributes = (RPCAttribute[]) method.GetCustomAttributes(typeof(RPCAttribute), true);
            if (attributes.Length == 0) return null;
            if (attributes.Length > 1 && LogHelper.CurrentLogLevel <= LogLevel.Normal)
            {
                LogHelper.LogWarning("Having more than one ServerRPC or ClientRPC attribute per method is not supported.");
            }
            if (method.ReturnType != typeof(void) && !SerializationManager.IsTypeSupported(method.ReturnType) && LogHelper.CurrentLogLevel <= LogLevel.Error)
            {
                LogHelper.LogWarning("Invalid return type of RPC. Has to be either void or RpcResponse<T> with a serializable type");
            }
            return new ReflectionMethod(method, parameters, attributes[0], index);
        }

        public readonly MethodInfo method;
        private readonly bool? requireOwnership;
        public readonly bool useDelegate;
        private readonly int index;
        private readonly Type[] parameterTypes;
        private readonly object[] parameterRefs;

        internal ReflectionMethod(MethodInfo method, ParameterInfo[] parameters, RPCAttribute attribute, int index)
        {
            this.method = method;
            this.index = index;
            requireOwnership = (attribute as ServerRPCAttribute)?.RequireOwnership;
            useDelegate =
                parameters.Length == 2 && method.ReturnType == typeof(void) &&
                parameters[0].ParameterType == typeof(ulong) && parameters[1].ParameterType == typeof(Stream); parameterTypes = new Type[parameters.Length];
            if (!useDelegate)
            {
                parameterRefs = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++) {
                    parameterTypes[i] = parameters[i].ParameterType;
                }
            }
        }

        internal bool ForServer => requireOwnership != null;

        internal object Invoke(NetworkedBehaviour target, ulong senderClientId, Stream stream)
        {
            if (requireOwnership == true && senderClientId != target.OwnerClientId)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only owner can invoke ServerRPC that is marked to require ownership");
                return null;
            }
            target.executingRpcSender = senderClientId;
            if (stream.Position == 0)
            {
                return useDelegate ? InvokeDelegate(target, senderClientId, stream) : InvokeReflected(target, stream);
            } else
            {
                // Create a new stream so that the stream they get ONLY contains user data and not MLAPI headers
                using (PooledBitStream userStream = PooledBitStream.Get())
                {
                    userStream.CopyUnreadFrom(stream);
                    userStream.Position = 0;
                    return useDelegate ? InvokeDelegate(target, senderClientId, stream) : InvokeReflected(target, stream);
                }
            }
        }

        internal object InvokeReflected(NetworkedBehaviour instance, Stream stream)
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

        internal object InvokeDelegate(NetworkedBehaviour target, ulong senderClientId, Stream stream)
        {
            target.rpcDelegates[index](senderClientId, stream);
            return null;
        }

    }
}
