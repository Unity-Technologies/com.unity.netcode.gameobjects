using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
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
        /// <summary>
        /// Sends per second
        /// </summary>
        [Range(0f, 120f)]
        public float SendsPerSecond = 20;
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
        private float timeForLerp;
        private float lerpT;
        private Vector3 lerpStartPos;
        private Quaternion lerpStartRot;
        private Vector3 lerpEndPos;
        private Quaternion lerpEndRot;

        private float lastSendTime;
        private Vector3 lastSentPos;
        private Quaternion lastSentRot;
        /// <summary>
        /// Should proximity be enabled
        /// </summary>
        public bool EnableProximity = false;
        /// <summary>
        /// The distance to use for proximity
        /// </summary>
        [Tooltip("If enable proximity is turned on, on clients within this range will be recieving position updates from the server")]
        public float ProximityRange = 50;

        private static byte[] positionUpdateBuffer = new byte[24];

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
            if(AssumeSyncedSends)
            {
                timeForLerp = 1f / SendsPerSecond;
            }

            lastSentRot = transform.rotation;
            lastSentPos = transform.position;

            lerpStartPos = transform.position;
            lerpStartRot = transform.rotation;

            lerpEndPos = transform.position;
            lerpStartRot = transform.rotation;
        }

        private void Update()
        {
            if(isOwner || isLocalPlayer || (new NetId(ownerClientId).IsInvalid() && isServer))
            {
                //We own the object OR we are server and the object is not owned by anyone OR we are the object.
                if(NetworkingManager.singleton.NetworkTime - lastSendTime >= timeForLerp && (Vector3.Distance(transform.position, lastSentPos) > MinMeters || Quaternion.Angle(transform.rotation, lastSentRot) > MinDegrees))
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
                            SendToClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer, true);
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
                    lerpT += Time.unscaledDeltaTime / timeForLerp;
                    transform.position = Vector3.Lerp(lerpStartPos, lerpEndPos, lerpT);
                    transform.rotation = Quaternion.Slerp(lerpStartRot, lerpEndRot, lerpT);
                }
            }
        }

        private void OnRecieveTransformFromServer(uint clientId, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    float xPos = reader.ReadSingle();
                    float yPos = reader.ReadSingle();
                    float zPos = reader.ReadSingle();

                    float xRot = reader.ReadSingle();
                    float yRot = reader.ReadSingle();
                    float zRot = reader.ReadSingle();

                    lerpStartPos = transform.position;
                    lerpStartRot = transform.rotation;
                    lerpEndPos = new Vector3(xPos, yPos, zPos);
                    lerpEndRot = Quaternion.Euler(xRot, yRot, zRot);
                    lerpT = 0;
                }
            }
        }

        private void OnRecieveTransformFromClient(uint clientId, byte[] data)
        {
            using (MemoryStream readStream = new MemoryStream(data))
            {
                using(BinaryReader reader = new BinaryReader(readStream))
                {
                    float xPos = reader.ReadSingle();
                    float yPos = reader.ReadSingle();
                    float zPos = reader.ReadSingle();

                    float xRot = reader.ReadSingle();
                    float yRot = reader.ReadSingle();
                    float zRot = reader.ReadSingle();

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
                        using(BinaryWriter writer = new BinaryWriter(writeStream))
                        {
                            writer.Write(xPos);
                            writer.Write(yPos);
                            writer.Write(zPos);
                            writer.Write(xRot);
                            writer.Write(yRot);
                            writer.Write(zRot);
                        }
                        if(EnableProximity)
                        {
                            // For instead of Foreach?! TODO!!!
                            for (uint i = 0; i < NetworkingManager.singleton.connectedClients.Count; i++)
                            {
                                if (Vector3.Distance(NetworkingManager.singleton.connectedClients[i].PlayerObject.transform.position, transform.position) <= ProximityRange)
                                {
                                    SendToClientTarget(NetworkingManager.singleton.connectedClients[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer, true);
                                }
                            }
                        }
                        else
                        {
                            SendToNonLocalClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", positionUpdateBuffer, true);
                        }
                    }
                }
            }
        }
    }
}
