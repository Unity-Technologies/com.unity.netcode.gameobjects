using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;

namespace MLAPI.RuntimeTests
{
    /// <summary>
    /// Used in conjunction with the RpcQueueTest to test:
    /// - Sending and Receiving a continually growing buffer up to (
    /// - Usage of the ServerRpcParams.Send.UpdateStage and ClientRpcParams.Send.UpdateStage functionality.
    /// - Rpcs receive will be invoked at the appropriate NetworkUpdateStage.
    /// </summary>
    public class GrowingRpcBufferSizesComponent : NetworkBehaviour
    {
        /// <summary>
        /// Allows the external RPCQueueTest to begin testing or stop it
        /// </summary>
        public bool EnableTesting;

        /// <summary>
        /// The maximum size of the buffer to send (defaults to maximum UNet buffer size)
        /// </summary>
        public int MaximumBufferSize = 65536;

        /// <summary>
        /// The rate at which the buffer size increases until it reaches MaximumBufferSize
        /// (the default starting buffer size is 8 bytes)
        /// </summary>
        public int BufferSizeMultiplier = 8;

        /// <summary>
        /// Is checked to determine if the test exited because it failed
        /// </summary>
        public bool TestFailed { get; internal set; }

        private bool m_WaitForValidation;
        private int m_CurrentBufferSize;

        private List<byte> m_SendBuffer;
        private List<byte> m_PreCalculatedBufferValues;

        // Start is called before the first frame update
        private void Start()
        {
            m_WaitForValidation = false;
            m_CurrentBufferSize = BufferSizeMultiplier;
            m_SendBuffer = new List<byte>(MaximumBufferSize);
            m_PreCalculatedBufferValues = new List<byte>(MaximumBufferSize+1);
            while(m_PreCalculatedBufferValues.Count <= MaximumBufferSize )
            {
                m_PreCalculatedBufferValues.Add((byte)UnityEngine.Random.Range(0, 255));
            }
        }

        /// <summary>
        /// Returns back whether the test has completed the total number of iterations
        /// </summary>
        /// <returns></returns>
        public bool IsTestComplete()
        {
            if (m_CurrentBufferSize > MaximumBufferSize || TestFailed)
            {
                return true;
            }

            return false;
        }

        // Update is called once per frame
        private void Update()
        {
            if (NetworkManager.Singleton.IsListening && EnableTesting && !IsTestComplete() && !m_WaitForValidation)
            {
                //Keep the current contents of the bufffer and fill the buffer with the delta difference of the buffer's current size and new size from the m_PreCalculatedBufferValues
                m_SendBuffer.AddRange(m_PreCalculatedBufferValues.GetRange(m_SendBuffer.Count == 0 ? 0 : m_SendBuffer.Count - 1, m_CurrentBufferSize - m_SendBuffer.Count));

                //Make sure we don't do anything until we finish validating buffer
                m_WaitForValidation = true;

                //Send the buffer
                SendBufferServerRpc(m_SendBuffer.ToArray());
            }
        }

        /// <summary>
        /// Validates the received buffer with the originating buffer
        /// </summary>
        /// <param name="buffer">the received buffer</param>
        /// <returns>true (validated) false (failed validation)</returns>
        private bool BuffersMatch(byte[] buffer)
        {
            var SourceArray = buffer;
            var OriginalArry = m_SendBuffer.ToArray();
            long TargetSize = buffer.Length;
            long LargeInt64Blocks = TargetSize >> 3; //Divide by 8
            int IndexOffset = 0;

            //process by 8 byte blocks if we can
            for (long i = 0; i < LargeInt64Blocks; i++)
            {
                if(BitConverter.ToInt64(SourceArray, IndexOffset) != BitConverter.ToInt64(OriginalArry, IndexOffset))
                {
                    return false;
                }
                IndexOffset += 8;
            }

            long Offset = LargeInt64Blocks * 8;
            long Remainder = TargetSize - Offset;

            //4 byte block
            if (Remainder >= 4)
            {
                if(BitConverter.ToInt32(SourceArray, IndexOffset) != BitConverter.ToInt32(OriginalArry, IndexOffset))
                {
                    return false;
                }
                IndexOffset += 4;
                Offset += 4;
            }

            //Remainder of bytes < 4
            if (TargetSize - Offset > 0)
            {
                for (long i = 0; i < (TargetSize - Offset); i++)
                {
                    if (SourceArray[IndexOffset + i] != OriginalArry[IndexOffset + i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Server side RPC for testing
        /// </summary>
        /// <param name="parameters">server rpc parameters</param>
        [ServerRpc]
        private void SendBufferServerRpc(byte[] buffer)
        {
            var BufferValidated = BuffersMatch(buffer);
            //Check to make sure we are even the same size, then compare
            if(buffer.Length != m_SendBuffer.Count || !BuffersMatch(buffer) )
            {
                TestFailed = true;
            }
            else
            {
                Debug.Log($"Tested buffer size of {m_SendBuffer.Count} -- {nameof(BufferValidated)}: {BufferValidated}");
                //Increasse buffer size
                m_CurrentBufferSize += m_CurrentBufferSize;
                m_WaitForValidation = false;
            }
        }
    }
}
