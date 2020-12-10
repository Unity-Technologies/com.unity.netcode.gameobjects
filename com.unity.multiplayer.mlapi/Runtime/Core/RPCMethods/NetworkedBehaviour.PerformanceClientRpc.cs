using System;
using System.Collections.Generic;
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
		[Obsolete("Use InvokeClientRpcPerformance instead")]
		public void InvokeClientRpc(RpcDelegate method, List<ulong> clientIds, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethod(method.Method), clientIds, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnOwnerPerformance instead")]
		public void InvokeClientRpcOnOwner(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethod(method.Method), OwnerClientId, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnClientPerformance instead")]
		public void InvokeClientRpcOnClient(RpcDelegate method, ulong clientId, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethod(method.Method), clientId, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnEveryonePerformance instead")]
		public void InvokeClientRpcOnEveryone(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethod(method.Method), null, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnEveryoneExceptPerformance instead")]
		public void InvokeClientRpcOnEveryoneExcept(RpcDelegate method, ulong clientIdToIgnore, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethod(method.Method), stream, clientIdToIgnore, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcPerformance instead")]
		public void InvokeClientRpc(string methodName, List<ulong> clientIds, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethodName(methodName), clientIds, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnClientPerformance instead")]
		public void InvokeClientRpcOnClient(string methodName, ulong clientId, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethodName(methodName), clientId, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnOwnerPerformance instead")]
		public void InvokeClientRpcOnOwner(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethodName(methodName), OwnerClientId, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnEveryonePerformance instead")]
		public void InvokeClientRpcOnEveryone(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethodName(methodName), null, stream, channel, security);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use InvokeClientRpcOnEveryoneExceptPerformance instead")]
		public void InvokeClientRpcOnEveryoneExcept(string methodName, ulong clientIdToIgnore, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			SendClientRPCPerformance(HashMethodName(methodName), stream, clientIdToIgnore, channel, security);
		}

        #region InvokeClientRPC Performance Wrappers
		public void InvokeClientRpcPerformance(RpcDelegate method, List<ulong> clientIds, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{			
            InvokeClientRPCPerformance(HashMethod(method.Method),clientIds,stream, channel, security);
		}

		public void InvokeClientRpcOnOwnerPerformance(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			
            InvokeClientRPCPerformance(HashMethod(method.Method),new List<ulong>(){OwnerClientId},stream, channel, security);
		}

		public void InvokeClientRpcOnClientPerformance(RpcDelegate method, ulong clientId, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{			
            InvokeClientRPCPerformance(HashMethod(method.Method),new List<ulong>(){clientId},stream, channel, security);
		}



        public void InvokeClientRpcOnEveryonePerformance(RpcDelegate method, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			InvokeClientRPCPerformance(HashMethod(method.Method), InternalMessageSender.GetAllClientIds(), stream, channel, security);
		}

		public void InvokeClientRpcOnEveryoneExceptPerformance(RpcDelegate method, ulong clientIdToIgnore, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
            List<ulong> clientIds = InternalMessageSender.GetAllClientIdsExcluding(clientIdToIgnore);
            if(clientIds.Count > 0)
            {
			    InvokeClientRPCPerformance(HashMethod(method.Method),clientIds, stream, channel, security);
            }
		}

		public void InvokeClientRpcPerformance(string methodName, List<ulong> clientIds, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			InvokeClientRPCPerformance(HashMethodName(methodName), clientIds, stream, channel, security);
		}

		public void InvokeClientRpcOnClientPerformance(string methodName, ulong clientId, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			InvokeClientRPCPerformance(HashMethodName(methodName), new List<ulong>(){clientId }, stream, channel, security);
		}

		public void InvokeClientRpcOnEveryonePerformance(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			InvokeClientRPCPerformance(HashMethodName(methodName), InternalMessageSender.GetAllClientIds(), stream, channel, security);
		}

		public void InvokeClientRpcOnEveryoneExceptPerformance(string methodName, ulong clientIdToIgnore, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{
			InvokeClientRPCPerformance(HashMethodName(methodName),InternalMessageSender.GetAllClientIdsExcluding(clientIdToIgnore), stream,  channel, security);
		}

        internal void InvokeClientRPCPerformance(ulong methodHash, List<ulong> clientIds, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {

            if (IsHost)
            {
                SendClientRPCPerformance(methodHash,clientIds, stream,  channel, security);
            }
            if(clientIds != null)
            {
                //PerformanceQueueItem QueueItem = new PerformanceQueueItem();
                //QueueItem.MethodHash = methodHash;
                //QueueItem.QueueItemType = PerformanceQueueItem.PerformanceQueueItemType.ClientRPC;
                //QueueItem.NetworkId = NetworkId;
                //QueueItem.ObjectOrderIndex = NetworkedObject.GetOrderIndex(this);                
                //QueueItem.ItemStream = stream;
                //QueueItem.Channel = channel;
                //QueueItem.SendFlags = security;                
                //QueueItem.ClientIds = clientIds;
                //RPCQueueManager.AddToPerformanceRPCSendQueue(QueueItem);
            }

        }
        #endregion

        public void InvokeClientRpcOnOwnerPerformance(string methodName, Stream stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
		{

            SendClientRPCPerformance(HashMethodName(methodName), OwnerClientId, stream, channel, security);

		}
		#pragma warning restore 1591
	}
}
