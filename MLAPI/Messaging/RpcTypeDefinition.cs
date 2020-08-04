using MLAPI.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MLAPI.Messaging
{
    internal class RpcTypeDefinition
    {
        private static readonly Dictionary<Type, RpcTypeDefinition> typeLookup = new Dictionary<Type, RpcTypeDefinition>();
        private static readonly Dictionary<ulong, string> hashResults = new Dictionary<ulong, string>();

        public static RpcTypeDefinition Get(Type type)
        {
            if (typeLookup.ContainsKey(type))
            {
                return typeLookup[type];
            }
            else
            {
                RpcTypeDefinition info = new RpcTypeDefinition(type);
                typeLookup.Add(type, info);

                return info;
            }
        }

        private static ulong HashMethodNameAndValidate(string name)
        {
            ulong hash = NetworkedBehaviour.HashMethodName(name);

            if (hashResults.ContainsKey(hash))
            {
                string hashResult = hashResults[hash];

                if (hashResult != name)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Hash collision detected for RPC method. The method \"" + name + "\" collides with the method \"" + hashResult + "\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                }
            } 
            else
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

        private RpcTypeDefinition(Type type)
        {
            List<ReflectionMethod> delegateMethodsList = new List<ReflectionMethod>();
            List<MethodInfo> methods = GetAllMethods(type, typeof(NetworkedBehaviour));

            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] parameters = method.GetParameters();
                ReflectionMethod rpcMethod = ReflectionMethod.Create(method, parameters, delegateMethodsList.Count);

                if (rpcMethod == null)
                    continue;

                Dictionary<ulong, ReflectionMethod> lookupTarget = rpcMethod.serverTarget ? serverMethods : clientMethods;

                ulong nameHash = HashMethodNameAndValidate(method.Name);

                if (!lookupTarget.ContainsKey(nameHash))
                {
                    lookupTarget.Add(nameHash, rpcMethod);
                }

                if (parameters.Length > 0)
                {
                    ulong signatureHash = HashMethodNameAndValidate(NetworkedBehaviour.GetHashableMethodSignature(method));

                    if (!lookupTarget.ContainsKey(signatureHash))
                    {
                        lookupTarget.Add(signatureHash, rpcMethod);
                    }
                }

                if (rpcMethod.useDelegate)
                {
                    delegateMethodsList.Add(rpcMethod);
                }
            }

            delegateMethods = delegateMethodsList.ToArray();
        }

        internal RpcDelegate[] CreateTargetedDelegates(NetworkedBehaviour target)
        {
            if (delegateMethods.Length == 0)
                return null;

            RpcDelegate[] rpcDelegates = new RpcDelegate[delegateMethods.Length];

            for (int i = 0; i < delegateMethods.Length; i++)
            {
                rpcDelegates[i] = (RpcDelegate) Delegate.CreateDelegate(typeof(RpcDelegate), target, delegateMethods[i].method.Name);
            }

            return rpcDelegates;
        }
    }
}
