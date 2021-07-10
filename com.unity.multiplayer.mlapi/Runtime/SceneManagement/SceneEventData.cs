using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using MLAPI.Serialization.Pooled;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI.Serialization;


namespace MLAPI.SceneManagement
{
    [Serializable]
    public class SceneEventData : INetworkSerializable, IDisposable
    {
        public enum SceneEventTypes
        {
            SWITCH,             //Server to client full scene switch (i.e. single mode and destroy everything)
            LOAD,               //Server to client load additive scene
            UNLOAD,             //Server to client unload additive scene
            SYNC,               //Server to client late join approval synchronization
            SWITCH_COMPLETE,    //Client to server
            LOAD_COMPLETE,      //Client to server
            UNLOAD_COMPLETE,    //Client to server
            SYNC_COMPLETE,      //Client to server
        }

        public SceneEventTypes SceneEventType;
        public LoadSceneMode LoadSceneMode;
        public Guid SwitchSceneGuid;

        public uint SceneIndex;
        public ulong TargetClientId;

        private Dictionary<uint, List<NetworkObject>> m_SceneNetworkObjects;
        private Dictionary<uint, long> m_SceneNetworkObjectDataOffsets;
        internal PooledNetworkBuffer InternalBuffer;

        /// <summary>
        /// Client Side:
        /// Gets the next scene index to be loaded for approval and/or late joining
        /// </summary>
        /// <returns></returns>
        public uint GetNextSceneSynchronizationIndex()
        {
            if (m_SceneNetworkObjectDataOffsets.ContainsKey(SceneIndex))
            {
                return SceneIndex;
            }
            return m_SceneNetworkObjectDataOffsets.First().Key;
        }

        /// <summary>
        /// Client Side:
        /// Determines if all scenes have been processed during the synchronization process
        /// </summary>
        /// <returns>true/false</returns>
        public bool IsDoneWithSynchronization()
        {
            return (m_SceneNetworkObjectDataOffsets.Count == 0);
        }

        /// <summary>
        /// Server Side:
        /// Called just before the synchronization process
        /// </summary>
        public void InitializeForSynch()
        {
            if (m_SceneNetworkObjects == null)
            {
                m_SceneNetworkObjects = new Dictionary<uint, List<NetworkObject>>();
            }
            else
            {
                m_SceneNetworkObjects.Clear();
            }
        }

        /// <summary>
        /// Server Side:
        /// Used during the synchronization process to associate NetworkObjects with scenes
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="networkObject"></param>
        public void AddNetworkObjectForSynch(uint sceneIndex, NetworkObject networkObject)
        {
            if (!m_SceneNetworkObjects.ContainsKey(sceneIndex))
            {
                m_SceneNetworkObjects.Add(sceneIndex, new List<NetworkObject>());
            }

            m_SceneNetworkObjects[sceneIndex].Add(networkObject);
        }

        /// <summary>
        /// Determines if the scene event type was intended for the client ( or server )
        /// </summary>
        /// <returns>true (client should handle this message) false (server should handle this message)</returns>
        public bool IsSceneEventClientSide()
        {
            switch (SceneEventType)
            {
                case SceneEventTypes.LOAD:
                case SceneEventTypes.SWITCH:
                case SceneEventTypes.UNLOAD:
                case SceneEventTypes.SYNC:
                    {
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Serializes this class instance
        /// </summary>
        /// <param name="writer"></param>
        private void OnWrite(NetworkWriter writer)
        {
            writer.WriteByte((byte)SceneEventType);

            writer.WriteByte((byte)LoadSceneMode);

            if (SceneEventType != SceneEventTypes.SYNC)
            {
                writer.WriteByteArray(SwitchSceneGuid.ToByteArray());
            }

            writer.WriteUInt32Packed(SceneIndex);

            if (SceneEventType == SceneEventTypes.SYNC)
            {
                writer.WriteInt32Packed(m_SceneNetworkObjects.Count());

                if (m_SceneNetworkObjects.Count() > 0)
                {
                    string msg = "Scene Associated NetworkObjects Write:\n";
                    foreach (var keypair in m_SceneNetworkObjects)
                    {
                        writer.WriteUInt32Packed(keypair.Key);
                        msg += $"Scene ID [{keypair.Key}] NumNetworkObjects:[{keypair.Value.Count}]\n";
                        writer.WriteInt32Packed(keypair.Value.Count);
                        var positionStart = writer.GetStream().Position;
                        // Size Place Holder (For offset purposes, needs to not be packed)
                        writer.WriteUInt32(0);
                        var totalBytes = 0;
                        foreach (var networkObject in keypair.Value)
                        {
                            var noStart = writer.GetStream().Position;

                            networkObject.SerializeSceneObject(writer, TargetClientId);
                            var noStop = writer.GetStream().Position;
                            totalBytes += (int)(noStop - noStart);
                            msg += $"Included: {networkObject.name} Bytes: {totalBytes} \n";
                        }
                        var positionEnd = writer.GetStream().Position;
                        var bytesWritten = (uint)(positionEnd - (positionStart + sizeof(uint)));
                        writer.GetStream().Position = positionStart;
                        // Write the total size written to the stream by NetworkObjects being serialized
                        writer.WriteUInt32(bytesWritten);
                        writer.GetStream().Position = positionEnd;
                        msg += $"Wrote [{bytesWritten}] bytes of NetworkObject data. Verification: {totalBytes}\n";
                    }

                    Debug.Log(msg);
                }
            }
        }

        /// <summary>
        /// Deserialize this class instance
        /// </summary>
        /// <param name="reader"></param>
        private void OnRead(NetworkReader reader)
        {
            var sceneEventTypeValue = reader.ReadByte();

            if (Enum.IsDefined(typeof(SceneEventTypes), sceneEventTypeValue))
            {
                SceneEventType = (SceneEventTypes)sceneEventTypeValue;
            }
            else
            {
                Debug.LogError($"Serialization Read Error: {nameof(SceneEventType)} vale {sceneEventTypeValue} is not within the range of the defined {nameof(SceneEventTypes)} enumerator!");
                // NSS TODO: Add to proposal's MTT discussion topics: Should we assert here?
            }

            var loadSceneModeValue = reader.ReadByte();

            if (Enum.IsDefined(typeof(LoadSceneMode), loadSceneModeValue))
            {
                LoadSceneMode = (LoadSceneMode)loadSceneModeValue;
            }
            else
            {
                Debug.LogError($"Serialization Read Error: {nameof(LoadSceneMode)} vale {loadSceneModeValue} is not within the range of the defined {nameof(LoadSceneMode)} enumerator!");
                // NSS TODO: Add to proposal's MTT discussion topics: Should we assert here?
            }

            if (SceneEventType != SceneEventTypes.SYNC)
            {
                SwitchSceneGuid = new Guid(reader.ReadByteArray());
            }

            SceneIndex = reader.ReadUInt32Packed();

            if (SceneEventType == SceneEventTypes.SYNC)
            {
                var keyPairCount = reader.ReadInt32Packed();

                if (keyPairCount > 0)
                {
                    if (m_SceneNetworkObjectDataOffsets == null)
                    {
                        m_SceneNetworkObjectDataOffsets = new Dictionary<uint, long>();
                    }
                    else
                    {
                        m_SceneNetworkObjectDataOffsets.Clear();
                    }

                    InternalBuffer.Position = 0;

                    using (var writer = PooledNetworkWriter.Get(InternalBuffer))
                    {
                        for (int i = 0; i < keyPairCount; i++)
                        {
                            var key = reader.ReadUInt32Packed();
                            var count = reader.ReadInt32Packed();
                            // how many bytes to read for this scene set
                            var bytesToRead = (ulong)reader.ReadUInt32();
                            // We store off the current position of the stream as it pertains to the scene relative NetworkObjects
                            m_SceneNetworkObjectDataOffsets.Add(key, InternalBuffer.Position);
                            writer.WriteInt32Packed(count);
                            writer.ReadAndWrite(reader, (long)bytesToRead);
                        }
                    }
                }
            }
        }

        public void SynchronizeSceneNetworkObjects(uint sceneId, NetworkManager networkManager)
        {
            if (m_SceneNetworkObjectDataOffsets.ContainsKey(sceneId))
            {
                // Point to the appropriate offset
                InternalBuffer.Position = m_SceneNetworkObjectDataOffsets[sceneId];

                using (var reader = PooledNetworkReader.Get(InternalBuffer))
                {
                    // Process all NetworkObjects for this scene
                    var newObjectsCount = reader.ReadInt32Packed();

                    for (int i = 0; i < newObjectsCount; i++)
                    {
                        NetworkObject.DeserializeSceneObject(InternalBuffer, reader, networkManager);
                    }
                }

                // Remove each entry after it is processed so we know when we are done
                m_SceneNetworkObjectDataOffsets.Remove(sceneId);
            }
        }

        /// <summary>
        /// INetworkSerializable implementation method for SceneEventData
        /// </summary>
        /// <param name="serializer">serializer passed in during serialization</param>
        public void NetworkSerialize(NetworkSerializer serializer)
        {
            if (serializer.IsReading)
            {
                OnRead(serializer.Reader);
            }
            else
            {
                OnWrite(serializer.Writer);
            }
        }

        /// <summary>
        /// Used to store data during an asynchronous scene loading event
        /// </summary>
        /// <param name="stream"></param>
        internal void CopyUnreadFromStream(Stream stream)
        {
            InternalBuffer.Position = 0;
            InternalBuffer.CopyUnreadFrom(stream);
            InternalBuffer.Position = 0;
        }

        /// <summary>
        /// Used to release the pooled network buffer
        /// </summary>
        public void Dispose()
        {
            if (InternalBuffer != null)
            {
                NetworkBufferPool.PutBackInPool(InternalBuffer);
                InternalBuffer = null;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SceneEventData()
        {
            InternalBuffer = NetworkBufferPool.GetBuffer();
        }
    }
}
