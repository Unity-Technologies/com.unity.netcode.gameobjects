using MLAPI.Messaging;
using MLAPI.Security;
using UnityEngine;

namespace MLAPI
{
    public abstract partial class NetworkedBehaviour : MonoBehaviour
    {
        #pragma warning disable 1591
        public void InvokeClientRpcOnOwner(RpcMethod method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security);
        }

        public void InvokeClientRpcOnOwner(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security);
        }

        public void InvokeClientRpcOnOwner<T1>(RpcMethod<T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1);
        }

        public void InvokeClientRpcOnOwner<T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1);
        }

        public void InvokeClientRpcOnOwner<T1, T2>(RpcMethod<T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnOwner<T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult>(ResponseRpcMethod<TResult> method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult>(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1>(ResponseRpcMethod<TResult, T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2>(ResponseRpcMethod<TResult, T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3>(ResponseRpcMethod<TResult, T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4>(ResponseRpcMethod<TResult, T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }
        #pragma warning restore 1591
    }
}