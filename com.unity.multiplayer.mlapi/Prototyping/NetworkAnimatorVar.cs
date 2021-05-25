using System.Collections.Generic;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkAnimatorVar")]
    public class NetworkAnimatorVar : NetworkBehaviour
    {
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

        public Animator Animator => m_Animator;
        
        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this animator
        /// </summary>
        [Tooltip("Defines who can update this transform.")]
        public Authority AnimatorAuthority = Authority.Client;
        
        [SerializeField] private Animator m_Animator;

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        /// <summary>
        /// The channel to send the data on
        /// </summary>
        [Tooltip("The channel to send the data on.")]
        public NetworkChannel Channel = NetworkChannel.NetworkVariable;
        
        
        private NetworkVariable<AnimatorSnapshot> m_AnimatorState = new NetworkVariable<AnimatorSnapshot>();
        private KeyValuePair<int, AnimatorControllerParameterType>[] m_CachedAnimatorParameters;
        private Dictionary<int, bool> m_BoolParameters;
        private Dictionary<int, float> m_FloatParameters;
        private Dictionary<int, int> m_IntParameters;
        private HashSet<int> m_TriggerParameters;


        private bool IsAuthorityOverAnimator => (IsClient && AnimatorAuthority == Authority.Client && IsOwner) ||
                                                (IsServer && AnimatorAuthority == Authority.Server) ||
                                                AnimatorAuthority == Authority.Shared;

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsAuthorityOverAnimator)
            {
                bool didParametersChange = PollAnimatorParameters();
                bool didAnyLayerStateChange = CheckStatesChange();

                if (didParametersChange || didAnyLayerStateChange)
                {
                    var newAnimatorState = new AnimatorSnapshot(m_AnimatorState.Value);
                    
                    for (int i = 0; i < m_AnimatorState.Value.LayerCount; i++)
                    {
                        var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);
                        
                        newAnimatorState[i] = new LayerState
                        {
                            StateHash = animStateInfo.fullPathHash,
                            NormalizedStateTime = animStateInfo.normalizedTime,
                            LayerWeight = Animator.GetLayerWeight(i)
                        };
                    }

                    m_AnimatorState.Value = newAnimatorState;

                    m_TriggerParameters.Clear();
                }
            }
        }


        private void OnEnable()
        {
            m_AnimatorState.OnValueChanged += AnimParamsChanged;
        }

        public void OnDisable()
        {
            m_AnimatorState.OnValueChanged -= AnimParamsChanged;
        }

        public bool SetInt(int key, int value)
        {
            if (m_IntParameters.TryGetValue(key, out var existingValue) && existingValue == value)
            {
                return false;
            }

            m_IntParameters[key] = value;
            return true;
        }

        public bool SetBool(int key, bool value)
        {
            if (m_BoolParameters.TryGetValue(key, out var existingValue) && existingValue == value)
            {
                return false;
            }

            m_BoolParameters[key] = value;
            return true;
        }

        public bool SetFloat(int key, float value)
        {
            if (m_FloatParameters.TryGetValue(key, out var existingValue) &&
                Mathf.Abs(existingValue - value) < Mathf.Epsilon)
            {
                return false;
            }

            m_FloatParameters[key] = value;
            return true;
        }

        public bool SetTrigger(int key)
        {
            return m_TriggerParameters.Add(key);
        }

        public override void NetworkStart()
        {
            m_AnimatorState.Settings.SendTickrate = FixedSendsPerSecond;
            m_AnimatorState.Settings.SendNetworkChannel = Channel;

            if (IsAuthorityOverAnimator)
            {
                m_AnimatorState.Value = new AnimatorSnapshot(m_Animator.layerCount);
                
                var parameters = m_Animator.parameters;
                m_CachedAnimatorParameters = new KeyValuePair<int, AnimatorControllerParameterType>[parameters.Length];

                int intCount = 0;
                int floatCount = 0;
                int boolCount = 0;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    m_CachedAnimatorParameters[i] = new KeyValuePair<int, AnimatorControllerParameterType>(parameter.nameHash, parameter.type);

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

                m_IntParameters = new Dictionary<int, int>(intCount);
                m_FloatParameters = new Dictionary<int, float>(floatCount);
                m_BoolParameters = new Dictionary<int, bool>(boolCount);
                m_TriggerParameters = new HashSet<int>();
            }

            if (AnimatorAuthority == Authority.Client)
            {
                m_AnimatorState.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            }
            else if (AnimatorAuthority == Authority.Shared)
            {
                m_AnimatorState.Settings.WritePermission = NetworkVariablePermission.Everyone;
            }
        }

        private bool PollAnimatorParameters()
        {
            bool changed = false;

            foreach (var animParam in m_CachedAnimatorParameters)
            {
                var animParamHash = animParam.Key;
                var animParamType = animParam.Value;

                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        changed = changed || SetFloat(animParamHash, m_Animator.GetFloat(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        changed = changed || SetInt(animParamHash, m_Animator.GetInteger(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        changed = changed || SetBool(animParamHash, m_Animator.GetBool(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (m_Animator.GetBool(animParamHash))
                        {
                            changed = changed || SetTrigger(animParamHash);
                        }

                        break;
                }
            }

            return changed;
        }

        private bool CheckStatesChange()
        {
            for (int i = 0; i < m_AnimatorState.Value.LayerCount; i++)
            {
                var animStateInfo = m_Animator.GetCurrentAnimatorStateInfo(i);

                bool didStateChange = m_AnimatorState.Value[i].StateHash != animStateInfo.fullPathHash;
                bool enoughDelta = !didStateChange && (animStateInfo.normalizedTime - m_AnimatorState.Value[i].NormalizedStateTime) >= 0.15f;
                bool layerWeightChanged = Mathf.Abs(m_AnimatorState.Value[i].LayerWeight - m_Animator.GetLayerWeight(i)) > Mathf.Epsilon;

                if (didStateChange || enoughDelta || layerWeightChanged)
                {
                    return true;
                }
            }

            return false;
        }

        private void AnimParamsChanged(AnimatorSnapshot previousvalue, AnimatorSnapshot newvalue)
        {
            if (IsAuthorityOverAnimator)
            {
                return;
            }
            
            ApplyAnimatorSnapshot(newvalue);
        }
        
        private void ApplyAnimatorSnapshot(AnimatorSnapshot animatorSnapshot)
        {
            if (animatorSnapshot == null)
            {
                Debug.LogWarning(animatorSnapshot, this);
                return;
            }
            
            for (var layerIndex = 0; layerIndex < animatorSnapshot.LayerCount; layerIndex++)
            {
                var layerState = animatorSnapshot[layerIndex];

                m_Animator.SetLayerWeight(layerIndex, layerState.LayerWeight);

                var currentAnimatorState = m_Animator.GetCurrentAnimatorStateInfo(layerIndex);

                bool forceAnimationCatchup = Mathf.Abs(currentAnimatorState.normalizedTime - currentAnimatorState.normalizedTime) > 0.15f;
                bool stateChanged = currentAnimatorState.fullPathHash != layerState.StateHash;
                
                if (stateChanged || forceAnimationCatchup)
                {
                    m_Animator.Play(layerState.StateHash, layerIndex, layerState.NormalizedStateTime);
                }
            }
        }

        private class AnimatorSnapshot : INetworkSerializable
        {
            public LayerState this[int i]
            {
                get => m_States[i];
                set => m_States[i] = value;
            }

            public int LayerCount => m_States.Length;

            private LayerState[] m_States;
            
            public AnimatorSnapshot()
            {
                m_States = new LayerState[0];
            }
            
            public AnimatorSnapshot(int layerCount)
            {
                m_States = new LayerState[layerCount];
            }

            /// <summary>
            /// This constructor allows a snapshot to reuse the reference to m_States array from the parameter snapshot
            /// </summary>
            /// <param name="snapshot"></param>
            public AnimatorSnapshot(AnimatorSnapshot snapshot)
            {
                m_States = snapshot.m_States;
            }

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                int layerCount = serializer.IsReading ? 0 : m_States.Length;
                serializer.Serialize(ref layerCount);

                if (serializer.IsReading && m_States.Length != layerCount)
                {
                    m_States = new LayerState[layerCount];
                }
                
                for (int paramIndex = 0; paramIndex < layerCount; paramIndex++)
                {
                    var stateHash = serializer.IsReading ? 0 : m_States[paramIndex].StateHash;
                    serializer.Serialize(ref stateHash);

                    var layerWeight = serializer.IsReading ? 0 : m_States[paramIndex].LayerWeight;
                    serializer.Serialize(ref layerWeight);

                    var normalizedStateTime = serializer.IsReading ? 0 : m_States[paramIndex].NormalizedStateTime;
                    serializer.Serialize(ref normalizedStateTime);

                    if (serializer.IsReading)
                    {
                        m_States[paramIndex] = new LayerState()
                        {
                            LayerWeight = layerWeight, StateHash = stateHash,
                            NormalizedStateTime = normalizedStateTime
                        };
                    }
                }
            }
        }

        private struct LayerState
        {
            public int StateHash;
            public float NormalizedStateTime;
            public float LayerWeight;
        }
    }
}