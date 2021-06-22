using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;

namespace MLAPI.Transports
{
    public unsafe struct RelayServerData
    {
        public NetworkEndPoint Endpoint;
        public ushort Nonce;
        // TODO: Should we move this ConnectionSessionId to RelayProtocolData?
        public ushort ConnectionSessionId;
        public RelayConnectionData ConnectionData;
        public RelayConnectionData HostConnectionData;
        public RelayAllocationId AllocationId;
        public RelayHMACKey HMACKey;
        public fixed byte HMAC[32]; // TODO: this shouldn't be here and should be computed on connection binding but today it's not Burst compatible.

        public RelayServerData(ref NetworkEndPoint endpoint, ushort nonce, RelayAllocationId allocationId, string connectionData, string hostConnectionData, string key)
        {
            Endpoint = endpoint;
            AllocationId = allocationId;
            Nonce = nonce;
            ConnectionSessionId = default;

            fixed (byte* connPtr = ConnectionData.Value)
            fixed (byte* hostPtr = HostConnectionData.Value)
            fixed (byte* keyPtr = HMACKey.Value)
            {
                Base64.FromBase64String(connectionData, connPtr, RelayConnectionData.k_Length);
                Base64.FromBase64String(hostConnectionData, hostPtr, RelayConnectionData.k_Length);
                Base64.FromBase64String(key, keyPtr, RelayHMACKey.k_Length);
            }

            fixed (byte* hmacPtr = HMAC)
            {
                ComputeBindHMAC(hmacPtr, Nonce, ref ConnectionData, ref HMACKey);
            }
        }

        public RelayServerData(ref NetworkEndPoint endpoint, ushort nonce, ref RelayAllocationId allocationId, ref RelayConnectionData connectionData, ref RelayConnectionData hostConnectionData, ref RelayHMACKey key)
        {
            Endpoint = endpoint;
            Nonce = nonce;
            AllocationId = allocationId;
            ConnectionData = connectionData;
            HostConnectionData = hostConnectionData;
            HMACKey = key;

            ConnectionSessionId = default;

            fixed (byte* hmacPtr = HMAC)
            {
                ComputeBindHMAC(hmacPtr, Nonce, ref connectionData, ref key);
            }
        }

        public void ComputeNewNonce()
        {
            Nonce = (ushort)(new Unity.Mathematics.Random((uint) Stopwatch.GetTimestamp())).NextUInt(1, 0xefff);

            fixed (byte* hmacPtr = HMAC)
            {
                ComputeBindHMAC(hmacPtr, Nonce, ref ConnectionData, ref HMACKey);
            }
        }

        private static void ComputeBindHMAC(byte* result, ushort nonce, ref RelayConnectionData connectionData, ref RelayHMACKey key)
        {
            var keyArray = new byte[64];

            fixed (byte* keyValue = &key.Value[0])
            {
                fixed (byte* keyArrayPtr = &keyArray[0])
                {
                    UnsafeUtility.MemCpy(keyArrayPtr, keyValue, keyArray.Length);
                }

                const int messageLength = 263;

                var messageBytes = stackalloc byte[messageLength];

                messageBytes[0] = 0xDA;
                messageBytes[1] = 0x72;
                // ... zeros
                messageBytes[5] = (byte) nonce;
                messageBytes[6] = (byte) (nonce >> 8);
                messageBytes[7] = 255;

                fixed (byte* connValue = &connectionData.Value[0])
                {
                    UnsafeUtility.MemCpy(messageBytes + 8, connValue, 255);
                }

                HMACSHA256.ComputeHash(keyValue, keyArray.Length, messageBytes, messageLength, result);
            }
        }
    }
}
