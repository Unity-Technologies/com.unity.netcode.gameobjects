using MLAPI.Configuration;
using MLAPI.Hashing;
using MLAPI.Logging;
using MLAPI.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MLAPI.Messaging
{
    internal class NetworkedTypeInfo
    {
        private static readonly Dictionary<Type, NetworkedTypeInfo> infos = new Dictionary<Type, NetworkedTypeInfo>();
        private static readonly Dictionary<ulong, string> hashResults = new Dictionary<ulong, string>();

        public static NetworkedTypeInfo Obtain(Type type)
        {
            if (infos.TryGetValue(type, out NetworkedTypeInfo info)) return info;
            info = new NetworkedTypeInfo(type);
            infos.Add(type, info);
            return info;
        }

        private static ulong HashMethodNameAndValidate(string name)
        {
            var hash = NetworkedBehaviour.HashMethodName(name);
            if (hashResults.TryGetValue(hash, out string value))
            {
                if (value != name && LogHelper.CurrentLogLevel <= LogLevel.Error)
                {
                    LogHelper.LogError($"Hash collision detected for RPC method. The method \"{name}\" collides with the method \"{value}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                }
            } else
            {
                hashResults.Add(hash, name);
            }
            return hash;
        }

        private static List<MethodInfo> GetAllMethods(Type type, Type limitType)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            while (type != null && type != limitType)
            {
                list.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            return list;
        }

        public readonly Dictionary<ulong, ReflectionMethod> serverMethods = new Dictionary<ulong, ReflectionMethod>();
        public readonly Dictionary<ulong, ReflectionMethod> clientMethods = new Dictionary<ulong, ReflectionMethod>();
        private readonly ReflectionMethod[] delegateMethods;

        private NetworkedTypeInfo(Type type)
        {
            var delegateMethodsList = new List<ReflectionMethod>();
            var methods = GetAllMethods(type, typeof(NetworkedBehaviour));
            for (var i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                var parameters = method.GetParameters();
                var rpcMethod = ReflectionMethod.Create(method, parameters, delegateMethodsList.Count);
                if (rpcMethod == null) continue;
                var table = rpcMethod.ForServer ? serverMethods : clientMethods;
                table.Add(HashMethodNameAndValidate(method.Name), rpcMethod);
                if (parameters.Length > 0) table.Add(HashMethodNameAndValidate(NetworkedBehaviour.GetHashableMethodSignature(method)), rpcMethod);
                if (rpcMethod.useDelegate) delegateMethodsList.Add(rpcMethod);
            }
            delegateMethods = delegateMethodsList.ToArray();
        }

        public RpcDelegate[] InitializeRpcDelegates(NetworkedBehaviour target)
        {
            if (delegateMethods.Length == 0) return null;
            var rpcDelegates = new RpcDelegate[delegateMethods.Length];
            for (var i = 0; i < delegateMethods.Length; i++)
            {
                var delegateMethod = delegateMethods[i];
                rpcDelegates[i] = (RpcDelegate) Delegate.CreateDelegate(typeof(RpcDelegate), target, delegateMethod.method.Name);
            }
            return rpcDelegates;
        }
    }
}
