using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using MLAPI.Serialization;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkAnimatorVar")]
    public class NetworkAnimatorVar : NetworkBehaviour
    {
        private struct LayerState : INetworkSerializable, IEquatable<LayerState>
        {
            public int StateHash;
            public float NormalizedStateTime;
            public byte LayerIndex;
            public float LayerWeight;
            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref StateHash);
                serializer.Serialize(ref NormalizedStateTime);
                serializer.Serialize(ref LayerIndex);
                serializer.Serialize(ref LayerWeight);
            }
            
            public bool Equals(LayerState other)
            {
                return StateHash == other.StateHash && NormalizedStateTime.Equals(other.NormalizedStateTime) && LayerIndex == other.LayerIndex && LayerWeight.Equals(other.LayerWeight);
            }

            public override bool Equals(object obj)
            {
                return obj is LayerState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = StateHash;
                    hashCode = (hashCode * 397) ^ NormalizedStateTime.GetHashCode();
                    hashCode = (hashCode * 397) ^ LayerIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ LayerWeight.GetHashCode();
                    return hashCode;
                }
            }
        }
        
         /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;
         

        /// <summary>
        /// The channel to send the data on
        /// </summary>
        [SerializeField, Tooltip("The channel to send the data on.")]
        public NetworkChannel Channel = NetworkChannel.NetworkVariable;
        
        private NetworkDictionary<int, int> m_IntParameters;
        private NetworkDictionary<int, float> m_FloatParameters;
        private NetworkDictionary<int, bool> m_BoolParameters;
        private NetworkList<LayerState> m_LayerStates;

        public float AnimationCatchupDelay = 0.2f;
    
        private KeyValuePair<int, AnimatorControllerParameterType>[] Parameters;

        [SerializeField]
        private Animator m_Animator;

        public Animator Animator => m_Animator;

        /// <summary>
        /// Server authority only allows the server to update this transform
        /// Client authority only allows the client owner to update this transform
        /// Shared authority allows everyone to update this transform
        /// </summary>
        public enum Authority
        {
            Server = 0, // default
            Client,
            Shared
        }

        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this animator
        /// </summary>
        [SerializeField, Tooltip("Defines who can update this animator.")]
        private Authority m_AnimatorAuthority = Authority.Server; // todo Luke mentioned an incoming system to manage this at the NetworkBehaviour level, lets sync on this
        
        private bool CanUpdate()
        {
            return (IsClient && m_AnimatorAuthority == Authority.Client && IsOwner) || (IsServer && m_AnimatorAuthority == Authority.Server) || m_AnimatorAuthority == Authority.Shared;
        }

        private void Awake()
        {
            var parameters = Animator.parameters;
            
            Parameters = new KeyValuePair<int, AnimatorControllerParameterType>[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                Parameters[i] = new KeyValuePair<int, AnimatorControllerParameterType>(parameter.nameHash, parameter.type);
            }

            m_IntParameters = new NetworkDictionary<int, int>();
            m_FloatParameters = new NetworkDictionary<int, float>();
            m_BoolParameters = new NetworkDictionary<int, bool>();
            m_LayerStates = new NetworkList<LayerState>();
        }

        private void OnEnable()
        {
            m_IntParameters.OnDictionaryChanged += OnIntParametersChanged;
            m_FloatParameters.OnDictionaryChanged += OnFloatParametersChanged;
            m_BoolParameters.OnDictionaryChanged += OnBoolParametersChanged;
            m_LayerStates.OnListChanged += OnLayerStatesChanged;
        }

        private void OnDisable()
        {
            m_IntParameters.OnDictionaryChanged -= OnIntParametersChanged;
            m_FloatParameters.OnDictionaryChanged -= OnFloatParametersChanged;
            m_BoolParameters.OnDictionaryChanged -= OnBoolParametersChanged;
            m_LayerStates.OnListChanged -= OnLayerStatesChanged;
        }

        public override void NetworkStart()
        {
            m_IntParameters.Settings.SendTickrate = FixedSendsPerSecond;
            m_IntParameters.Settings.SendNetworkChannel = Channel;
            m_FloatParameters.Settings.SendTickrate = FixedSendsPerSecond;
            m_FloatParameters.Settings.SendNetworkChannel = Channel;
            m_BoolParameters.Settings.SendTickrate = FixedSendsPerSecond;
            m_BoolParameters.Settings.SendNetworkChannel = Channel;
            m_LayerStates.Settings.SendTickrate = FixedSendsPerSecond;
            m_LayerStates.Settings.SendNetworkChannel = Channel;
            
            if (CanUpdate())
            {
                WriteAnimatorStateToNetVars();
                WriteAnimatorParametersToNetVars();
            }

            if (m_AnimatorAuthority == Authority.Client)
            {
                m_IntParameters.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                m_FloatParameters.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                m_BoolParameters.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                m_LayerStates.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            }
            else if (m_AnimatorAuthority == Authority.Shared)
            {
                m_IntParameters.Settings.WritePermission = NetworkVariablePermission.Everyone;
                m_FloatParameters.Settings.WritePermission = NetworkVariablePermission.Everyone;
                m_BoolParameters.Settings.WritePermission = NetworkVariablePermission.Everyone;
                m_LayerStates.Settings.WritePermission = NetworkVariablePermission.Everyone;
            }
        }
        
        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }
            
            if (CanUpdate())
            {
                Debug.Log($"Var update: {Time.frameCount}");
                WriteAnimatorStateToNetVars();
                WriteAnimatorParametersToNetVars();
            }
        }
        
        private void WriteAnimatorStateToNetVars()
        {
            for (byte i = 0; i < m_LayerStates.Count; i++)
            {
                var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);
                
                m_LayerStates[i] = new LayerState()
                {
                    StateHash = animStateInfo.fullPathHash,
                    NormalizedStateTime = animStateInfo.normalizedTime,
                    LayerIndex = i,
                    LayerWeight = Animator.GetLayerWeight(i)
                };
            }
        }

        private void WriteAnimatorParametersToNetVars()
        {
            foreach (var animParam in Parameters)
            {
                var animParamHash = animParam.Key;
                var animParamType = animParam.Value;
                
                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        m_FloatParameters[animParamHash] = Animator.GetFloat(animParamHash);
                        break;
                    case AnimatorControllerParameterType.Int:
                        m_IntParameters[animParamHash] = Animator.GetInteger(animParamHash);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        m_BoolParameters[animParamHash] = Animator.GetBool(animParamHash);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (Animator.GetBool(animParamHash))
                        {
                            Debug.Log($"var jump {Time.frameCount}");
                            SendActivatedTrigger(animParamHash);
                        }
                        break;
                }
            }
        }
        
        private void OnIntParametersChanged(NetworkDictionaryEvent<int, int> changeevent)
        {
            if (m_AnimatorAuthority == Authority.Client && IsClient && IsOwner)
            {
                // this should only happen for my own value changes.
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                return;
            }

            if (changeevent.Type == NetworkDictionaryEvent<int, int>.EventType.Add ||
                changeevent.Type == NetworkDictionaryEvent<int, int>.EventType.Value)
            {
                Animator.SetInteger(changeevent.Key, changeevent.Value);
            }
        }
        
        private void OnFloatParametersChanged(NetworkDictionaryEvent<int, float> changeevent)
        {
            if (m_AnimatorAuthority == Authority.Client && IsClient && IsOwner)
            {
                // this should only happen for my own value changes.
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                return;
            }

            if (changeevent.Type == NetworkDictionaryEvent<int, float>.EventType.Add ||
                changeevent.Type == NetworkDictionaryEvent<int, float>.EventType.Value)
            {
                Animator.SetFloat(changeevent.Key, changeevent.Value);
            }
        }
        
        private void OnBoolParametersChanged(NetworkDictionaryEvent<int, bool> changeevent)
        {
            if (m_AnimatorAuthority == Authority.Client && IsClient && IsOwner)
            {
                // this should only happen for my own value changes.
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                return;
            }

            if (changeevent.Type == NetworkDictionaryEvent<int, bool>.EventType.Add ||
                changeevent.Type == NetworkDictionaryEvent<int, bool>.EventType.Value)
            {   
                Animator.SetBool(changeevent.Key, changeevent.Value);
            }
        }
        
        private void OnLayerStatesChanged(NetworkListEvent<LayerState> changeevent)
        {
            if (m_AnimatorAuthority == Authority.Client && IsClient && IsOwner)
            {
                // this should only happen for my own value changes.
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                return;
            }

            if (changeevent.Type == NetworkListEvent<LayerState>.EventType.Add ||
                changeevent.Type == NetworkListEvent<LayerState>.EventType.Value)
            {
                var layerState = changeevent.Value;
                
                Animator.SetLayerWeight(layerState.LayerIndex, layerState.LayerWeight);
                var stateInfo = Animator.GetCurrentAnimatorStateInfo(layerState.LayerIndex);
                
                if (stateInfo.fullPathHash == layerState.StateHash)
                {
                    float delta = layerState.NormalizedStateTime - stateInfo.normalizedTime;
                    if (delta >= AnimationCatchupDelay)
                    {
                        Animator.Play(layerState.StateHash, layerState.LayerIndex, layerState.NormalizedStateTime);
                    }
                }
                else
                {
                    Animator.Play(layerState.StateHash, layerState.LayerIndex, layerState.NormalizedStateTime);
                }
            }
        }

        private void SendActivatedTrigger(int activatedTriggerParameterHash)
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
                TriggersActivatedClientRpc(activatedTriggerParameterHash, clientRpcParams);
            }
            else
            {
                TriggersActivatedServerRpc(activatedTriggerParameterHash);
            }
        }

        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void TriggersActivatedClientRpc(int activatedTriggerParameters, ClientRpcParams clientRpcParams = default)
        {
            if (CanUpdate() || IsHost)
            {
                return;
            }
            Animator.SetTrigger(activatedTriggerParameters);
        }

        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        private void TriggersActivatedServerRpc(int activatedTriggerParameters, ServerRpcParams serverRpcParams = default)
        {
            if (CanUpdate())
            {
                return;
            }
            
            Animator.SetTrigger(activatedTriggerParameters);
            
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
            
            TriggersActivatedClientRpc(activatedTriggerParameters, clientRpcParams);
        }
    }
}
