using UnityEngine;

namespace Unity.Netcode.Components
{
    public struct SceneEventDataSynchronize : INetworkSerializable
    {
        private FastBufferReader m_ReceivedData;
        private FastBufferWriter m_SendData;

        public byte[] GetSendData()
        {
            if (!m_SendData.IsInitialized || m_SendData.Length == 0)
            {
                Debug.LogError($"Send data is not initialized or its length is zero ({m_SendData.Length})!");
                return null;
            }
            return m_SendData.ToArray();
        }

        public NetworkSceneManager NetworkSceneManager;

        internal void DuplicateSceneEventData(SceneEventData sceneEventData, bool isReceiver = false)
        {
            m_SendData = new FastBufferWriter(NetworkMessageManager.DefaultNonFragmentedMessageMaxSize - FastBufferWriter.GetWriteSize<NetworkMessageHeader>(), Collections.Allocator.Persistent, int.MaxValue - FastBufferWriter.GetWriteSize<NetworkMessageHeader>());

            sceneEventData.Serialize(m_SendData, isReceiver);
        }

        internal void DuplicateReader(ref byte[] readerData)
        {
            m_SendData = new FastBufferWriter(NetworkMessageManager.DefaultNonFragmentedMessageMaxSize - FastBufferWriter.GetWriteSize<NetworkMessageHeader>(), Collections.Allocator.Persistent, int.MaxValue - FastBufferWriter.GetWriteSize<NetworkMessageHeader>());
            m_SendData.WriteValueSafe(readerData);
        }

        public void Dispose()
        {
            if (m_ReceivedData.IsInitialized)
            {
                m_ReceivedData.Dispose();
            }

            if (m_SendData.IsInitialized)
            {
                m_SendData.Dispose();
            }
        }


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var length = 0;
            var sendData = new byte[0];
            if (serializer.IsWriter)
            {
                length = m_SendData.Length;
                sendData = m_SendData.ToArray();
            }
            serializer.SerializeValue(ref length);
            if (serializer.IsReader)
            {
                sendData = new byte[length];
            }
            serializer.SerializeValue(ref sendData);

            if (serializer.IsReader)
            {
                m_ReceivedData = new FastBufferReader(sendData, Collections.Allocator.Persistent);
            }
        }

        public bool CompareSendDataWithReceivedData(ref byte[] sendData)
        {
            var receivedData = m_ReceivedData.ToArray();
            var length = sendData.Length > receivedData.Length ? receivedData.Length : sendData.Length;
            var areTheSame = true;
            if (sendData.Length != receivedData.Length)
            {
                Debug.LogError($"[Send != Receive] Sent data size: {sendData.Length} | Received data size {receivedData.Length}");
                areTheSame = false;
            }            
            //for (int i = 0; i < length; i++)
            //{
            //    if (sendData[i] != receivedData[i])
            //    {
            //        Debug.LogError($"[Send != Receive][Offset: {i}] Sent data[{i}]: {sendData[i]} | Received data[{i}]: {receivedData[i]}");
            //        areTheSame = false;
            //    }
            //}
            return areTheSame;
        }
    }
}
