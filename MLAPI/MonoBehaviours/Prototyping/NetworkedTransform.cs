using System.IO;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Prototyping
{
    public class NetworkedTransform : NetworkedBehaviour
    {
        [Range(0f, 120f)]
        public float SendsPerSecond = 20;
        [Tooltip("This assumes that the SendsPerSecond is synced across clients")]
        public bool AssumeSyncedSends = true;
        [Tooltip("This requires AssumeSyncedSends to be true")]
        public bool InterpolatePosition = true;
        [Tooltip("The transform will snap if the distance is greater than this distance")]
        public float SnapDistance = 10f;
        public bool InterpolateServer = true;
        public float MinMeters = 0.15f;
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

        public bool EnableProximity = false;
        [Tooltip("If enable proximity is turned on, on clients within this range will be recieving position updates from the server")]
        public float ProximityRange = 50;

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
        }

        private void Update()
        {
            if(isOwner || isLocalPlayer || (ownerClientId == -2 && isServer))
            {
                //We own the object OR we are server and the object is not owned by anyone OR we are the object.
                if(Time.time - lastSendTime >= timeForLerp && (Vector3.Distance(transform.position, lastSentPos) > MinMeters || Quaternion.Angle(transform.rotation, lastSentRot) > MinDegrees))
                {
                    lastSendTime = Time.time;
                    lastSentPos = transform.position;
                    lastSentRot = transform.rotation;
                    using (MemoryStream writeStream = new MemoryStream(24))
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
                            SendToClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", writeStream.GetBuffer());
                        else
                            SendToServerTarget("MLAPI_OnRecieveTransformFromClient", "MLAPI_POSITION_UPDATE", writeStream.GetBuffer());
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
                    lerpT += Time.deltaTime / timeForLerp;
                    transform.position = Vector3.Lerp(lerpStartPos, lerpEndPos, lerpT);
                    transform.rotation = Quaternion.Slerp(lerpStartRot, lerpEndRot, lerpT);
                }
            }
        }

        private void OnRecieveTransformFromServer(int clientId, byte[] data)
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

        private void OnRecieveTransformFromClient(int clientId, byte[] data)
        {
            using(MemoryStream readStream = new MemoryStream(data))
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
                    using (MemoryStream writeStream = new MemoryStream(24))
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
                            for (int i = 0; i < NetworkingManager.singleton.connectedClients.Count; i++)
                            {
                                if (Vector3.Distance(NetworkingManager.singleton.connectedClients[i].PlayerObject.transform.position, transform.position) <= ProximityRange)
                                {
                                    SendToClientTarget(NetworkingManager.singleton.connectedClients[i].ClientId, "MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", writeStream.GetBuffer());
                                }
                            }
                        }
                        else
                        {
                            SendToNonLocalClientsTarget("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", writeStream.GetBuffer());
                        }
                    }
                }
            }
        }
    }
}
