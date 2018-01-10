using MLAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MLAP
{
    public class NetworkedTransform : NetworkedBehaviour
    {
        [Range(0, 120)]
        public int SendsPerSecond = 20;
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

        private void OnValidate()
        {
            if (!AssumeSyncedSends && InterpolatePosition)
                InterpolatePosition = false;
            if (InterpolateServer && !InterpolatePosition)
                InterpolateServer = false;
        }


        void Awake()
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

        void Update()
        {
            if(isLocalPlayer)
            {
                if(Time.time - lastSendTime >= timeForLerp)
                {
                    lastSendTime = Time.time;
                }
            }
            else
            {
                lerpT += Time.deltaTime / timeForLerp;
                transform.position = Vector3.Lerp(lerpStartPos, lerpEndPos, lerpT);
                transform.rotation = Quaternion.Slerp(lerpStartRot, lerpEndRot, lerpT);
            }
        }

        private void OnRecieveTransformFromServer(int clientId, byte[] data)
        {
            if (isServer)
                return;
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    uint netId = reader.ReadUInt32();
                    if (networkId != netId)
                        return;
                    float xPos = reader.ReadSingle();
                    float yPos = reader.ReadSingle();
                    float zPos = reader.ReadSingle();
                    float xRot = reader.ReadSingle();
                    float yRot = reader.ReadSingle();
                    float zRot = reader.ReadSingle();
                    lerpStartPos = transform.position;
                    lerpStartRot = transform.rotation;
                    lerpEndPos = new Vector3(xPos, yPos, zRot);
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
                        lerpEndPos = new Vector3(xPos, yPos, zRot);
                        lerpEndRot = Quaternion.Euler(xRot, yRot, zRot);
                        lerpT = 0;
                    }
                    else
                    {
                        transform.position = new Vector3(xPos, yPos, zPos);
                        transform.rotation = Quaternion.Euler(new Vector3(xRot, yRot, zRot));
                    }
                    using (MemoryStream writeStream = new MemoryStream())
                    {
                        using(BinaryWriter writer = new BinaryWriter(writeStream))
                        {
                            writer.Write(networkId);
                            writer.Write(xPos);
                            writer.Write(yPos);
                            writer.Write(zPos);
                            writer.Write(xRot);
                            writer.Write(yRot);
                            writer.Write(zRot);
                        }
                        SendToNonLocalClients("MLAPI_OnRecieveTransformFromServer", "MLAPI_POSITION_UPDATE", writeStream.ToArray());
                    }
                }
            }
        }
    }
}
