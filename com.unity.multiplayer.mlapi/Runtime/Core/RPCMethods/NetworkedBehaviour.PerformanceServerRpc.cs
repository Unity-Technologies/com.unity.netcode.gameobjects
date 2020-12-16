using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
            InvokeServerRPCPerformance(HashMethod(method.Method), stream, channel, security);
        }

        public void InvokeServerRpcPerformance(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            InvokeServerRPCPerformance(HashMethodName(methodName), stream, channel, security);
        }

        private void InvokeServerRPCPerformance(ulong methodHash,Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            //Pre-Check if this is the host, if so then call the class instance local SendServerRPCPerformance method
            //NOTE: This is a work around for an issue with having the InvokeServerRPCLocal use instance relative/local defined RPCs (i.e. rpcDefinition.serverMethods in NetworkedBehavior.cs) 
            if(IsHost)
            {
                SendServerRPCPerformance(methodHash, stream, channel, security);
            }
            else  //Otheriwse, queue up the Server directed RPC
            {
                //PerformanceQueueItem QueueItem = new PerformanceQueueItem();
                //QueueItem.MethodHash = methodHash;
                //QueueItem.QueueItemType = PerformanceQueueItem.PerformanceQueueItemType.ServerRPC;
                //QueueItem.NetworkId = NetworkId;
                //QueueItem.ObjectOrderIndex = NetworkedObject.GetOrderIndex(this);                
                //QueueItem.ItemStream = stream;
                //QueueItem.Channel = channel;
                //QueueItem.SendFlags = security;                
                //QueueItem.ClientIds = null;
                //RPCQueueManager.AddToPerformanceRPCSendQueue(QueueItem);
            }
        }

        #pragma warning restore 1591
    }
}
