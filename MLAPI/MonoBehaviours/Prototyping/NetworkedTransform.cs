using System.Collections.Generic;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System.IO;
using UnityEngine;
using MLAPI.Data;
using System.Linq;

namespace MLAPI.MonoBehaviours.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedTransform")]
    public class NetworkedTransform : NetworkedBehaviour
    {
        public class ClientSendInfo
        {
            public uint clientId;
            public float lastSent;
            public Vector3? lastMissedPosition;
            public Quaternion? lastMissedRotation;
        }

        [Range(0, 120)]
        public float FixedSendsPerSecond = 20f;
        /// <summary>
        /// Is the sends per second assumed to be the same across all instances
        /// </summary>
        [Tooltip("This assumes that the SendsPerSecond is synced across clients")]
        public bool AssumeSyncedSends = true;
        /// <summary>
        /// Enable interpolation
        /// </summary>
        [Tooltip("This requires AssumeSyncedSends to be true")]
        public bool InterpolatePosition = true;
        /// <summary>
        /// The distance before snaping to the position
        /// </summary>
        [Tooltip("The transform will snap if the distance is greater than this distance")]
        public float SnapDistance = 10f;
        /// <summary>
        /// Should the server interpolate
        /// </summary>
        public bool InterpolateServer = true;
        /// <summary>
        /// The min meters to move before a send is sent
        /// </summary>
        public float MinMeters = 0.15f;
        /// <summary>
        /// The min degrees to rotate before a send it sent
        /// </summary>
        public float MinDegrees = 1.5f;
        public bool ExtrapolatePosition = true;
        private float lerpT;
        private Vector3 lerpStartPos;
        private Quaternion lerpStartRot;
        private Vector3 lerpEndPos;
        private Quaternion lerpEndRot;

        private float lastSendTime;
        private Vector3 lastSentPos;
        private Quaternion lastSentRot;
        
        public bool EnableRange;
        public bool EnableNonProvokedResendChecks;
        public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);
        private readonly Dictionary<uint, ClientSendInfo> clientSendInfo = new Dictionary<uint, ClientSendInfo>();

        public delegate bool MoveValidationDelegate(Vector3 oldPos, Vector3 newPos);

        public MoveValidationDelegate IsMoveValidDelegate = null;

        private void OnValidate()
        {
            if (!AssumeSyncedSends && InterpolatePosition)
                InterpolatePosition = false;
            if (InterpolateServer && !InterpolatePosition)
                InterpolateServer = false;
            if (MinDegrees < 0)
                MinDegrees = 0;
            if (MinMeters < 0)
                MinMeters = 0;
            if (EnableNonProvokedResendChecks && !EnableRange)
                EnableNonProvokedResendChecks = false;
        }
        
        private float GetTimeForLerp(Vector3 pos1, Vector3 pos2)
        {
            return 1f / DistanceSendrate.Evaluate(Vector3.Distance(pos1, pos2));
        }
        
        /// <summary>
        /// Registers message handlers
        /// </summary>
        public override void NetworkStart()
        {
            lastSentRot = transform.rotation;
            lastSentPos = transform.position;

            lerpStartPos = transform.position;
            lerpStartRot = transform.rotation;

            lerpEndPos = transform.position;
            lerpEndRot = transform.rotation;
        }

        private void Update()
        {
			if (isOwner)
            {
                if (NetworkingManager.singleton.NetworkTime - lastSendTime >= (1f / FixedSendsPerSecond) && (Vector3.Distance(transform.position, lastSentPos) > MinMeters || Quaternion.Angle(transform.rotation, lastSentRot) > MinDegrees))
                {
                    lastSendTime = NetworkingManager.singleton.NetworkTime;
                    lastSentPos = transform.position;
                    lastSentRot = transform.rotation;
                    using (PooledBitStream writeStream = PooledBitStream.Get())
                    {
                        BitWriter writer = new BitWriter(writeStream);
                        writer.WriteSinglePacked(transform.position.x);
                        writer.WriteSinglePacked(transform.position.y);
                        writer.WriteSinglePacked(transform.position.z);

                        writer.WriteSinglePacked(transform.rotation.eulerAngles.x);
                        writer.WriteSinglePacked(transform.rotation.eulerAngles.y);
                        writer.WriteSinglePacked(transform.rotation.eulerAngles.z);

                        if (isServer)
                            InvokeClientRpcOnEveryone(OnRecieveTransformFromServer, writeStream);
                        else
                            InvokeServerRpc(OnRecieveTransformFromClient, writeStream);
                    }

                }
            }
            else
            {
                //If we are server and interpolation is turned on for server OR we are not server and interpolation is turned on
                if ((isServer && InterpolateServer && InterpolatePosition) || (!isServer && InterpolatePosition))
                {
                    if(Vector3.Distance(transform.position, lerpEndPos) > SnapDistance)
                    {
                        //Snap, set T to 1 (100% of the lerp)
                        lerpT = 1f;
                    }

                    if (isServer || !EnableRange || !AssumeSyncedSends)
                        lerpT += Time.unscaledDeltaTime / FixedSendsPerSecond;
                    else
                    {
                        Vector3 myPos = NetworkingManager.singleton.ConnectedClients[NetworkingManager.singleton.LocalClientId].PlayerObject.transform.position;
                        lerpT += Time.unscaledDeltaTime / GetTimeForLerp(transform.position, myPos);
                    }

                    if (ExtrapolatePosition)
                        transform.position = Vector3.LerpUnclamped(lerpStartPos, lerpEndPos, lerpT);
                    else
                        transform.position = Vector3.Lerp(lerpStartPos, lerpEndPos, lerpT);

                    if (ExtrapolatePosition)
                        transform.rotation = Quaternion.SlerpUnclamped(lerpStartRot, lerpEndRot, lerpT);
                    else
                        transform.rotation = Quaternion.Slerp(lerpStartRot, lerpEndRot, lerpT);
                }
            }

            if (isServer && EnableRange && EnableNonProvokedResendChecks) CheckForMissedSends();
        }

        [ClientRPC]
        private void OnRecieveTransformFromServer(uint clientId, Stream stream)
        {
            if (!enabled) return;
            BitReader reader = new BitReader(stream);

            float xPos = reader.ReadSinglePacked();
            float yPos = reader.ReadSinglePacked();
            float zPos = reader.ReadSinglePacked();

            float xRot = reader.ReadSinglePacked();
            float yRot = reader.ReadSinglePacked();
            float zRot = reader.ReadSinglePacked();

            lerpStartPos = transform.position;
            lerpStartRot = transform.rotation;
            lerpEndPos = new Vector3(xPos, yPos, zPos);
            lerpEndRot = Quaternion.Euler(xRot, yRot, zRot);
            lerpT = 0;
        }

        [ServerRPC]
        private void OnRecieveTransformFromClient(uint clientId, Stream stream)
        {
            if (!enabled) return;
            BitReader reader = new BitReader(stream);

            float xPos = reader.ReadSinglePacked();
            float yPos = reader.ReadSinglePacked();
            float zPos = reader.ReadSinglePacked();

            float xRot = reader.ReadSinglePacked();
            float yRot = reader.ReadSinglePacked();
            float zRot = reader.ReadSinglePacked();

            if (IsMoveValidDelegate != null && !IsMoveValidDelegate(lerpEndPos, new Vector3(xPos, yPos, zPos)))
            {
                //Invalid move!
                //TODO: Add rubber band (just a message telling them to go back)
                return;
            }

            if (InterpolateServer)
            {
                lerpStartPos = transform.position;
                lerpStartRot = transform.rotation;
                lerpEndPos = new Vector3(xPos, yPos, zPos);
                lerpEndRot = Quaternion.Euler(xRot, yRot, zRot);
                lerpT = 0;
            }
            else
            {
                transform.position = new Vector3(xPos, yPos, zPos);
                transform.rotation = Quaternion.Euler(new Vector3(xRot, yRot, zRot));
            }

            using (PooledBitStream writeStream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(writeStream);

                writer.WriteSinglePacked(xPos);
                writer.WriteSinglePacked(yPos);
                writer.WriteSinglePacked(zPos);

                writer.WriteSinglePacked(xRot);
                writer.WriteSinglePacked(yRot);
                writer.WriteSinglePacked(zRot);

                if (EnableRange)
                {
                    // For instead of Foreach?! TODO!!!
                    for (int i = 0; i < NetworkingManager.singleton.ConnectedClientsList.Count; i++)
                    {
                        if (!clientSendInfo.ContainsKey(NetworkingManager.singleton.ConnectedClientsList[i].ClientId))
                        {
                            clientSendInfo.Add(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, new ClientSendInfo()
                            {
                                clientId = NetworkingManager.singleton.ConnectedClientsList[i].ClientId,
                                lastMissedPosition = null,
                                lastMissedRotation = null,
                                lastSent = 0
                            });
                        }

                        ClientSendInfo info = clientSendInfo[NetworkingManager.singleton.ConnectedClientsList[i].ClientId];
                        Vector3 receiverPosition = NetworkingManager.singleton.ConnectedClientsList[i].PlayerObject.transform.position;
                        Vector3 senderPosition = NetworkingManager.singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.position;

                        if (NetworkingManager.singleton.NetworkTime - info.lastSent >= GetTimeForLerp(receiverPosition, senderPosition))
                        {
                            info.lastSent = NetworkingManager.singleton.NetworkTime;
                            info.lastMissedPosition = null;
                            info.lastMissedRotation = null;

                            InvokeClientRpcOnClient(OnRecieveTransformFromServer, NetworkingManager.singleton.ConnectedClientsList[i].ClientId, writeStream);
                            //SendToClientTarget(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                        }
                        else
                        {
                            info.lastMissedPosition = new Vector3(xPos, yPos, zPos);
                            info.lastMissedRotation = Quaternion.Euler(xRot, yRot, zRot);
                        }
                    }
                }
                else
                {
                    List<uint> clientIds = new List<uint>(NetworkingManager.singleton.ConnectedClientsList.Select(x => x.ClientId).ToList());
                    clientIds.Remove(OwnerClientId); //TODO: SORT WITH BETTER OVERLOADS
                    InvokeClientRpc(OnRecieveTransformFromServer, clientIds, writeStream);
                    //SendToNonLocalClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                }
            }
        }

        private void CheckForMissedSends()
        {
            for (int i = 0; i < NetworkingManager.singleton.ConnectedClientsList.Count; i++)
            {
                if (!clientSendInfo.ContainsKey(NetworkingManager.singleton.ConnectedClientsList[i].ClientId))
                {
                    clientSendInfo.Add(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, new ClientSendInfo()
                    {
                        clientId = NetworkingManager.singleton.ConnectedClientsList[i].ClientId,
                        lastMissedPosition = null,
                        lastMissedRotation = null,
                        lastSent = 0
                    });
                }
                ClientSendInfo info = clientSendInfo[NetworkingManager.singleton.ConnectedClientsList[i].ClientId];
                Vector3 receiverPosition = NetworkingManager.singleton.ConnectedClientsList[i].PlayerObject.transform.position;
                Vector3 senderPosition = NetworkingManager.singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.position;
                                
                if (NetworkingManager.singleton.NetworkTime - info.lastSent >= GetTimeForLerp(receiverPosition, senderPosition))
                {
                    info.lastSent = NetworkingManager.singleton.NetworkTime;
                    info.lastMissedPosition = null;
                    info.lastMissedRotation = null;
                    

                    //SendToClientTarget(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                }
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {   
            if (InterpolateServer && isServer || isClient)
            {
                lerpStartPos = position;
                lerpStartRot = rotation;
                lerpEndPos = position;
                lerpEndRot = rotation;
                lerpT = 0;
            }
        }
    }
}
