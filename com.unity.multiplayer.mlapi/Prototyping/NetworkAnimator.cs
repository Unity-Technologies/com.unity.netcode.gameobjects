using System;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkAnimator")]
    public class NetworkAnimator : NetworkBehaviour
    {
        private struct AnimParams : INetworkSerializable
        {
            public Dictionary<int, int> IntParameters;
            public Dictionary<int, float> FloatParameters;
            public Dictionary<int, bool> BoolParameters;
            public HashSet<int> TriggerParameters;

            public KeyValuePair<int, AnimatorControllerParameterType>[] Parameters;
            
            public AnimParams(AnimatorControllerParameter[] parameters)
            {
                Parameters = new KeyValuePair<int, AnimatorControllerParameterType>[parameters.Length];

                int intCount = 0;
                int floatCount = 0;
                int boolCount = 0;
                
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    Parameters[i] = new KeyValuePair<int, AnimatorControllerParameterType>(parameter.nameHash, parameter.type);

                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            ++floatCount;
                            break;
                        case AnimatorControllerParameterType.Int:
                            ++intCount;
                            break;
                        case AnimatorControllerParameterType.Bool:
                            ++boolCount;
                            break;
                    }
                }

                IntParameters = new Dictionary<int, int>(intCount);
                FloatParameters = new Dictionary<int, float>(floatCount);
                BoolParameters = new Dictionary<int, bool>(boolCount);
                TriggerParameters = new HashSet<int>();
            }

            public bool SetInt(int key, int value)
            {
                if (IntParameters.TryGetValue(key, out var existingValue) && existingValue==value)
                {
                    return false;
                }

                IntParameters[key] = value;
                return true;
            }
            
            public bool SetBool(int key, bool value)
            {
                if (BoolParameters.TryGetValue(key, out var existingValue) && existingValue==value)
                {
                    return false;
                }

                BoolParameters[key] = value;
                return true;
            }
            
            public bool SetFloat(int key, float value)
            {
                if (FloatParameters.TryGetValue(key, out var existingValue) && Mathf.Abs(existingValue - value) < Mathf.Epsilon)
                {
                    return false;
                }

                FloatParameters[key] = value;
                return true;
            }

            public bool SetTrigger(int key)
            {
                return TriggerParameters.Add(key);
            }

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                //int parameters
                {
                    int paramCount = serializer.IsReading ? 0 : IntParameters.Count;
                    serializer.Serialize(ref paramCount);
                
                    var paramArray = serializer.IsReading ? new KeyValuePair<int, int>[paramCount] : IntParameters.ToArray();
                    
                    for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                    {
                        var paramId = serializer.IsReading ? 0 : paramArray[paramIndex].Key;
                        serializer.Serialize(ref paramId);
                        
                        var paramInt = serializer.IsReading ? 0 : paramArray[paramIndex].Value;
                        serializer.Serialize(ref paramInt);
                
                        if (serializer.IsReading)
                        {
                            paramArray[paramIndex] = new KeyValuePair<int,int>(paramId, paramInt);
                        }
                    }
                
                    if (serializer.IsReading)
                    {
                        IntParameters = paramArray.ToDictionary(pair => pair.Key, pair => pair.Value);
                    }
                }

                //float parameters
                {
                    int paramCount = serializer.IsReading ? 0 : FloatParameters.Count;
                    serializer.Serialize(ref paramCount);

                    var paramArray = serializer.IsReading ? new KeyValuePair<int, float>[paramCount] : FloatParameters.ToArray();
                    for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                    {
                        var paramId = serializer.IsReading ? 0 : paramArray[paramIndex].Key;
                        serializer.Serialize(ref paramId);
                        
                        var paramFloat = serializer.IsReading ? 0 : paramArray[paramIndex].Value;
                        serializer.Serialize(ref paramFloat);

                        if (serializer.IsReading)
                        {
                            paramArray[paramIndex] = new KeyValuePair<int,float>(paramId, paramFloat);
                        }
                    }

                    if (serializer.IsReading)
                    {
                        FloatParameters = paramArray.ToDictionary(pair => pair.Key, pair => pair.Value);
                    }
                }
                
                //bool parameters
                {
                    int paramCount = serializer.IsReading ? 0 : BoolParameters.Count;
                    serializer.Serialize(ref paramCount);

                    var paramArray = serializer.IsReading ? new KeyValuePair<int, bool>[paramCount] : BoolParameters.ToArray();
                    for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                    {
                        var paramId = serializer.IsReading ? 0 : paramArray[paramIndex].Key;
                        serializer.Serialize(ref paramId);
                        
                        var paramBool = serializer.IsReading ? false : paramArray[paramIndex].Value;
                        serializer.Serialize(ref paramBool);

                        if (serializer.IsReading)
                        {
                            paramArray[paramIndex] = new KeyValuePair<int,bool>(paramId, paramBool);
                        }
                    }

                    if (serializer.IsReading)
                    {
                        BoolParameters = paramArray.ToDictionary(pair => pair.Key, pair => pair.Value);
                    }
                }
                
                //trigger parameters
                {
                    int paramCount = serializer.IsReading ? 0 : TriggerParameters.Count;
                    serializer.Serialize(ref paramCount);

                    var paramArray = serializer.IsReading ? new int[paramCount] : TriggerParameters.ToArray();
                    for (int i = 0; i < paramCount; i++)
                    {
                        var paramId = serializer.IsReading ? 0 : paramArray[i];
                        serializer.Serialize(ref paramId);

                        if (serializer.IsReading)
                        {
                            paramArray[i] = paramId;
                        }
                    }

                    if (serializer.IsReading)
                    {
                        TriggerParameters = new HashSet<int>(paramArray);
                    }
                }
            }
        }
        
        private struct LayerState : INetworkSerializable
        {
            public int StateHash;
            public float NormalizedStateTime;
            public float LayerWeight;
            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref StateHash);
                serializer.Serialize(ref NormalizedStateTime);
                serializer.Serialize(ref LayerWeight);
            }
        }

        /// <summary>
        /// Server authority only allows the server to update this animator
        /// Client authority only allows the client owner to update this animator
        /// Shared authority allows everyone to update this animator
        /// </summary>
        public enum Authority
        {
            Server = 0,
            Client,
            Shared
        }

        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this animator
        /// </summary>
        [Tooltip("Defines who can update this transform.")]
        public Authority AnimatorAuthority = Authority.Client; // todo Luke mentioned an incoming system to manage this at the NetworkBehaviour level, lets sync on this
        
        private LayerState[] m_LayersToStates;
        private AnimParams m_AnimParams;

        public float SendRate = 0.1f;
        private float m_NextSendTime = 0.0f;
        private bool m_ServerRequestsAnimationResync = false;
        [SerializeField]
        private Animator m_Animator;

        public Animator Animator => m_Animator;

        private void Awake()
        {
            m_AnimParams = new AnimParams(Animator.parameters);
            m_LayersToStates = new LayerState[Animator.layerCount];
        }

        private void OnEnable()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += ServerOnClientConnectedCallback;
            }
        }

        private void OnDisable()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= ServerOnClientConnectedCallback;
            }
        }

        private void ServerOnClientConnectedCallback(ulong clientId)
        {
            if (IsAuthorityOverAnimator)
            {
                m_ServerRequestsAnimationResync = true;
            }
            
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.ConnectedClientsList
                        .Where(c => c.ClientId != NetworkManager.ServerClientId)
                        .Select(c => c.ClientId)
                        .ToArray()
                }
            };
                
            RequestResyncClientRpc(clientRpcParams);
        }


        [ClientRpc]
        private void RequestResyncClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!IsAuthorityOverAnimator)
            {
                return;
            }

            m_ServerRequestsAnimationResync = true;
        }


        private bool IsAuthorityOverAnimator => (IsClient && AnimatorAuthority == Authority.Client && IsOwner) || (IsServer && AnimatorAuthority == Authority.Server) || AnimatorAuthority == Authority.Shared;

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsAuthorityOverAnimator)
            {
                bool shouldSendBasedOnTime = CheckSendRate();
                bool shouldSendBasedOnChanges = CheckStateChange();
                if (m_ServerRequestsAnimationResync || shouldSendBasedOnTime || shouldSendBasedOnChanges)
                {
                    SendAllParamsAndState();
                    m_AnimParams.TriggerParameters.Clear();
                    m_ServerRequestsAnimationResync = false;
                }
            }
        }
        
        private bool CheckSendRate()
        {
            var networkTime = NetworkManager.NetworkTime;
            if (SendRate != 0 && m_NextSendTime < networkTime)
            {
                m_NextSendTime = networkTime + SendRate;
                return true;
            }

            return false;
        }

        private bool CheckStateChange()
        {
            bool changed = false;
            
            for (int i = 0; i < m_LayersToStates.Length; i++)
            {
                var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);

                bool didStateChange = m_LayersToStates[i].StateHash != animStateInfo.fullPathHash;
                bool enoughDelta = !didStateChange && (animStateInfo.normalizedTime - m_LayersToStates[i].NormalizedStateTime) >= 0.15f;

                float newLayerWeight = Animator.GetLayerWeight(i);
                bool layerWeightChanged = Mathf.Abs(m_LayersToStates[i].LayerWeight - newLayerWeight) > Mathf.Epsilon;
                
                if (didStateChange || enoughDelta || layerWeightChanged)
                {
                    m_LayersToStates[i] = new LayerState
                    {
                        StateHash = animStateInfo.fullPathHash,
                        NormalizedStateTime = animStateInfo.normalizedTime,
                        LayerWeight = newLayerWeight
                    };
                    changed = true;
                }
            }

            foreach (var animParam in m_AnimParams.Parameters)
            {
                var animParamHash = animParam.Key;
                var animParamType = animParam.Value;
                
                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        changed = changed || m_AnimParams.SetFloat(animParamHash, Animator.GetFloat(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        changed = changed || m_AnimParams.SetInt(animParamHash, Animator.GetInteger(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        changed = changed || m_AnimParams.SetBool(animParamHash, Animator.GetBool(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (Animator.GetBool(animParamHash))
                        {
                            changed = changed || m_AnimParams.SetTrigger(animParamHash);
                        }
                        break;
                }
            }
            
            return changed;
        }

        private void SetAnimParams(AnimParams animParams)
        {
            foreach (var intParameter in animParams.IntParameters)
            {
                Animator.SetInteger(intParameter.Key, intParameter.Value);
            }

            foreach (var floatParameter in animParams.FloatParameters)
            {
                Animator.SetFloat(floatParameter.Key, floatParameter.Value);
            }

            foreach (var boolParameter in animParams.BoolParameters)
            {
                Animator.SetBool(boolParameter.Key, boolParameter.Value);
            }

            foreach (var triggerParameter in animParams.TriggerParameters)
            {
                Animator.SetTrigger(triggerParameter);
            }
        }

        private void SendAllParamsAndState()
        {
            if (IsServer)
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = NetworkManager.ConnectedClientsList
                            .Where(c => c.ClientId != NetworkManager.ServerClientId)
                            .Select(c => c.ClientId)
                            .ToArray()
                    }
                };
                
                SendParamsAndLayerStatesClientRpc(m_AnimParams, m_LayersToStates, clientRpcParams);
            }
            else
            {
                SendParamsAndLayerStatesServerRpc(m_AnimParams, m_LayersToStates);
            }
        }
        
        [ServerRpc]
        private void SendParamsAndLayerStatesServerRpc(AnimParams animParams, LayerState[] layerStates, ServerRpcParams serverRpcParams = default)
        {
            if (IsOwner)
            {
                return;
            }

            SetLayerStates(layerStates);
            SetAnimParams(animParams);
            
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = NetworkManager.ConnectedClientsList
                        .Where(c => c.ClientId != serverRpcParams.Receive.SenderClientId)
                        .Select(c => c.ClientId)
                        .ToArray()
                }
            };
            
            SendParamsAndLayerStatesClientRpc(animParams, layerStates, clientRpcParams);
        }

        [ClientRpc]
        private void SendParamsAndLayerStatesClientRpc(AnimParams animParams, LayerState[] layerStates,ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner || IsHost)
            {
                return;
            }

            SetLayerStates(layerStates);
            SetAnimParams(animParams);
        }

        private void SetLayerStates(LayerState[] layerStates)
        {
            for (var layerIndex = 0; layerIndex < layerStates.Length; layerIndex++)
            {
                var layerState = layerStates[layerIndex];
                
                Animator.SetLayerWeight(layerIndex, layerState.LayerWeight);

                var currentAnimatorState = Animator.GetCurrentAnimatorStateInfo(layerIndex);

                bool forceAnimationCatchup = Mathf.Abs(currentAnimatorState.normalizedTime - currentAnimatorState.normalizedTime) > 0.15f;
                bool stateChanged = currentAnimatorState.fullPathHash != layerState.StateHash;

                if (stateChanged || forceAnimationCatchup)
                {
                    Animator.Play(layerState.StateHash, layerIndex, layerState.NormalizedStateTime);
                }
            }
        }
    }
}
