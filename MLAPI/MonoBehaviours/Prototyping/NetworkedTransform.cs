using System.Collections.Generic;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System.IO;
using UnityEngine;

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

        private static byte[] positionUpdateBuffer = new byte[24];

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
            if (isServer)
            {
                RegisterMessageHandler("MLAPI_OnRecieveTransformFromClient", OnRecieveTransformFromClient);
            }
            if (isClient)
            {
                RegisterMessageHandler("MLAPI_OnRecieveTransformFromServer", OnRecieveTransformFromServer);
            }

            lastSentRot = transform.rotation;
            lastSentPos = transform.position;

            lerpStartPos = transform.position;
            lerpStartRot = transform.rotation;

            lerpEndPos = transform.position;
            lerpEndRot = transform.rotation;
        }

        private void Update()
        {
            if(isOwner || isLocalPlayer || (OwnerClientId == NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId && isServer))
            {
                //We own the object OR we are server and the object is not owned by anyone OR we are the object.
                if(NetworkingManager.singleton.NetworkTime - lastSendTime >= (1f / FixedSendsPerSecond) && (Vector3.Distance(transform.position, lastSentPos) > MinMeters || Quaternion.Angle(transform.rotation, lastSentRot) > MinDegrees))
                {
                    lastSendTime = NetworkingManager.singleton.NetworkTime;
                    lastSentPos = transform.position;
                    lastSentRot = transform.rotation;
                    using (MemoryStream writeStream = new MemoryStream(positionUpdateBuffer))
                    {
                        using (BinaryWriter writer = new BinaryWriter(writeStream))
                        {
                            writer.Write(transform.position.x);
                            writer.Write(transform.position.y);
                            writer.Write(transform.position.z);

                            writer.Write(transform.rotation.eulerAngles.x);
                            writer.Write(transform.rotation.eulerAngles.y);
                            writer.Write(transform.rotation.eulerAngles.z);
                        }
                        if (isServer)
                            SendToClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                        else
                            SendToServerTarget("MLAPI_OnRecieveTransformFromClient", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                    }

                }
            }
            else
            {
                //If we are server and interpolation is turned on for server OR we are not server and interpolation is turned on
                if((isServer && InterpolateServer && InterpolatePosition) || (!isServer && InterpolatePosition))
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

        private void OnRecieveTransformFromServer(uint clientId, BitReader reader)
        {
            if (!enabled) return;
            
            byte[] data = reader.ReadByteArray();
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader bReader = new BinaryReader(stream))
                {
                    float xPos = bReader.ReadSingle();
                    float yPos = bReader.ReadSingle();
                    float zPos = bReader.ReadSingle();

                    float xRot = bReader.ReadSingle();
                    float yRot = bReader.ReadSingle();
                    float zRot = bReader.ReadSingle();

                    lerpStartPos = transform.position;
                    lerpStartRot = transform.rotation;
                    lerpEndPos = new Vector3(xPos, yPos, zPos);
                    lerpEndRot = Quaternion.Euler(xRot, yRot, zRot);
                    lerpT = 0;
                }
            }
        }

        private void OnRecieveTransformFromClient(uint clientId, BitReader reader)
        {
            if (!enabled) return;
            
            byte[] data = reader.ReadByteArray();
            using (MemoryStream readStream = new MemoryStream(data))
            {
                using (BinaryReader bReader = new BinaryReader(readStream))
                {
                    float xPos = bReader.ReadSingle();
                    float yPos = bReader.ReadSingle();
                    float zPos = bReader.ReadSingle();

                    float xRot = bReader.ReadSingle();
                    float yRot = bReader.ReadSingle();
                    float zRot = bReader.ReadSingle();
                    
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
                    using (MemoryStream writeStream = new MemoryStream(positionUpdateBuffer))
                    {
                        using (BinaryWriter writer = new BinaryWriter(writeStream))
                        {
                            writer.Write(xPos);
                            writer.Write(yPos);
                            writer.Write(zPos);
                            writer.Write(xRot);
                            writer.Write(yRot);
                            writer.Write(zRot);
                        }
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
                                    
                                    SendToClientTarget(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
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
                            SendToNonLocalClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
                        }
                    }
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
                                    
                    SendToClientTarget(NetworkingManager.singleton.ConnectedClientsList[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer);
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
