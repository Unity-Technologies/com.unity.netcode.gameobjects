using System.Linq;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedAnimator")]
    public class NetworkedAnimator : NetworkedBehaviour
    {
        private struct AnimParams : INetworkSerializable
        {
            public Dictionary<int, (AnimatorControllerParameterType Type, object Boxed)> Parameters;

            public void NetworkSerialize(BitSerializer serializer)
            {
                int paramCount = serializer.IsReading ? 0 : Parameters.Count;
                serializer.Serialize(ref paramCount);

                var paramArray = serializer.IsReading ? new KeyValuePair<int, (AnimatorControllerParameterType Type, object Boxed)>[paramCount] : Parameters.ToArray();
                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    int paramId = serializer.IsReading ? 0 : paramArray[paramIndex].Key;
                    serializer.Serialize(ref paramId);

                    byte paramType = serializer.IsReading ? (byte)0 : (byte)paramArray[paramIndex].Value.Type;
                    serializer.Serialize(ref paramType);

                    object paramBoxed = null;
                    switch (paramType)
                    {
                        case (byte)AnimatorControllerParameterType.Float:
                            float paramFloat = serializer.IsReading ? 0 : (float)paramArray[paramIndex].Value.Boxed;
                            serializer.Serialize(ref paramFloat);
                            paramBoxed = paramFloat;
                            break;
                        case (byte)AnimatorControllerParameterType.Int:
                            int paramInt = serializer.IsReading ? 0 : (int)paramArray[paramIndex].Value.Boxed;
                            serializer.Serialize(ref paramInt);
                            paramBoxed = paramInt;
                            break;
                        case (byte)AnimatorControllerParameterType.Bool:
                            bool paramBool = serializer.IsReading ? false : (bool)paramArray[paramIndex].Value.Boxed;
                            serializer.Serialize(ref paramBool);
                            paramBoxed = paramBool;
                            break;
                    }

                    if (serializer.IsReading)
                    {
                        paramArray[paramIndex] = new KeyValuePair<int, (AnimatorControllerParameterType, object)>(paramId, ((AnimatorControllerParameterType)paramType, paramBoxed));
                    }
                }

                if (serializer.IsReading)
                {
                    Parameters = paramArray.ToDictionary(pair => pair.Key, pair => pair.Value);
                }
            }
        }

        public float sendRate = 0.1f;

        [SerializeField]
        private Animator m_Animator;

        public Animator animator => m_Animator;

        [HideInInspector]
        [SerializeField]
        private uint m_TrackedParamFlags = 0;

        public void SetParamTracking(int paramIndex, bool isTracking)
        {
            if (paramIndex >= 32) return;

            if (isTracking)
            {
                m_TrackedParamFlags |= (uint)(1 << paramIndex);
            }
            else
            {
                m_TrackedParamFlags &= (uint)~(1 << paramIndex);
            }
        }

        public bool GetParamTracking(int paramIndex)
        {
            if (paramIndex >= 32) return false;

            return (m_TrackedParamFlags & (uint)(1 << paramIndex)) != 0;
        }

        public void ResetTrackedParams()
        {
            m_TrackedParamFlags = 0;
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (CheckSendRate())
            {
                SendTrackedParams();
            }

            if (CheckStateChange(out int animStateHash, out float animStateTime))
            {
                SendAllParamsAndState(animStateHash, animStateTime);
            }
        }

        private float m_NextSendTime = 0.0f;

        private bool CheckSendRate()
        {
            var networkTime = MLAPI.NetworkManager.Singleton.NetworkTime;
            if (sendRate != 0 && m_NextSendTime < networkTime)
            {
                m_NextSendTime = networkTime + sendRate;
                return true;
            }

            return false;
        }

        private int m_LastAnimStateHash = 0;

        private bool CheckStateChange(out int outAnimStateHash, out float outAnimStateTime)
        {
            var animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            var animStateHash = animStateInfo.fullPathHash;
            var animStateTime = animStateInfo.normalizedTime;
            if (animStateHash != m_LastAnimStateHash)
            {
                m_LastAnimStateHash = animStateHash;

                outAnimStateHash = animStateHash;
                outAnimStateTime = animStateTime;
                return true;
            }

            outAnimStateHash = 0;
            outAnimStateTime = 0;
            return false;
        }

        private AnimParams GetAnimParams(bool trackedOnly = false)
        {
            var animParams = new AnimParams();
            animParams.Parameters = new Dictionary<int, (AnimatorControllerParameterType, object)>(32);
            for (int paramIndex = 0; paramIndex < 32 && paramIndex < animator.parameters.Length; paramIndex++)
            {
                if (trackedOnly && !GetParamTracking(paramIndex)) continue;

                var animParam = animator.parameters[paramIndex];
                var animParamHash = animParam.nameHash;
                var animParamType = animParam.type;

                object animParamBoxed = null;
                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        animParamBoxed = animator.GetFloat(animParamHash);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animParamBoxed = animator.GetInteger(animParamHash);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animParamBoxed = animator.GetBool(animParamHash);
                        break;
                }

                animParams.Parameters.Add(animParamHash, (animParamType, animParamBoxed));
            }

            return animParams;
        }

        private void SetAnimParams(AnimParams animParams)
        {
            foreach (var animParam in animParams.Parameters)
            {
                switch (animParam.Value.Type)
                {
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(animParam.Key, (float)animParam.Value.Boxed);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(animParam.Key, (int)animParam.Value.Boxed);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(animParam.Key, (bool)animParam.Value.Boxed);
                        break;
                }
            }
        }

        private void SendTrackedParams()
        {
            var animParams = GetAnimParams(/* trackedOnly = */ true);

            if (IsServer)
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = MLAPI.NetworkManager.Singleton.ConnectedClientsList
                            .Where(c => c.ClientId != MLAPI.NetworkManager.Singleton.ServerClientId)
                            .Select(c => c.ClientId)
                            .ToArray()
                    }
                };
                UpdateTrackedParamsClientRpc(animParams, clientRpcParams);
            }
            else
            {
                UpdateTrackedParamsServerRpc(animParams);
            }
        }

        private void SendAllParamsAndState(int animStateHash, float animStateTime)
        {
            var animParams = GetAnimParams();

            if (IsServer)
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = MLAPI.NetworkManager.Singleton.ConnectedClientsList
                            .Where(c => c.ClientId != MLAPI.NetworkManager.Singleton.ServerClientId)
                            .Select(c => c.ClientId)
                            .ToArray()
                    }
                };
                UpdateAnimStateClientRpc(animStateHash, animStateTime, clientRpcParams);
                UpdateTrackedParamsClientRpc(animParams, clientRpcParams);
            }
            else
            {
                UpdateAnimStateServerRpc(animStateHash, animStateTime);
                UpdateTrackedParamsServerRpc(animParams);
            }
        }

        [ServerRpc]
        private void UpdateTrackedParamsServerRpc(AnimParams animParams, ServerRpcParams serverRpcParams = default)
        {
            if (IsOwner) return;
            SetAnimParams(animParams);

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = MLAPI.NetworkManager.Singleton.ConnectedClientsList
                        .Where(c => c.ClientId != serverRpcParams.Receive.SenderClientId)
                        .Select(c => c.ClientId)
                        .ToArray()
                }
            };
            UpdateTrackedParamsClientRpc(animParams, clientRpcParams);
        }

        [ClientRpc]
        private void UpdateTrackedParamsClientRpc(AnimParams animParams, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) return;
            SetAnimParams(animParams);
        }

        [ServerRpc]
        private void UpdateAnimStateServerRpc(int animStateHash, float animStateTime, ServerRpcParams serverRpcParams = default)
        {
            if (IsOwner) return;

            animator.Play(animStateHash, 0, animStateTime);

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = MLAPI.NetworkManager.Singleton.ConnectedClientsList
                        .Where(c => c.ClientId != serverRpcParams.Receive.SenderClientId)
                        .Select(c => c.ClientId)
                        .ToArray()
                }
            };
            UpdateAnimStateClientRpc(animStateHash, animStateTime, clientRpcParams);
        }

        [ClientRpc]
        private void UpdateAnimStateClientRpc(int animStateHash, float animStateTime, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) return;

            animator.Play(animStateHash, 0, animStateTime);
        }
    }
}