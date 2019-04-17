using System;
using System.ComponentModel;
using System.IO;
using MLAPI.Messaging;
using MLAPI.Security;
using UnityEngine;

namespace MLAPI
{
    public abstract partial class NetworkedBehaviour : MonoBehaviour
    {
        #pragma warning disable 1591
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use InvokeServerRpcPerformance instead")]
        public void InvokeServerRpc(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethod(method.Method), stream, channel, security);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use InvokeServerRpcPerformance instead")]
        public void InvokeServerRpc(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethodName(methodName), stream, channel, security);
        }

        public void InvokeServerRpcPerformance(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethod(method.Method), stream, channel, security);
        }

        public void InvokeServerRpcPerformance(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethodName(methodName), stream, channel, security);
        }
        #pragma warning restore 1591
    }
}