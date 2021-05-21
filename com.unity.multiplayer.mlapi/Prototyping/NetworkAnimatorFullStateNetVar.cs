using System;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkAnimatorFullStateNetVar")]
    public class NetworkAnimatorFullStateNetVar : NetworkBehaviour
    {
        private struct AnimParams : INetworkSerializable
        {
            public Dictionary<int, int> IntParameters;
            public Dictionary<int, float> FloatParameters;
            public Dictionary<int, bool> BoolParameters;
            public HashSet<int> TriggerParameters;
            public LayerState[] LayerStates;
            
            public KeyValuePair<int, AnimatorControllerParameterType>[] Parameters;
            
            public AnimParams(Animator animator)
            {
                var parameters = animator.parameters;
                
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

                LayerStates = new LayerState[animator.layerCount];
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
                
                //layer states
                {
                    int layerCount = serializer.IsReading ? 0 : LayerStates.Length;
                    serializer.Serialize(ref layerCount);

                    for (int paramIndex = 0; paramIndex < layerCount; paramIndex++)
                    {
                        var stateHash = serializer.IsReading ? 0 : LayerStates[paramIndex].StateHash;
                        serializer.Serialize(ref stateHash);
                        
                        var layerWeight = serializer.IsReading ? 0 : LayerStates[paramIndex].LayerWeight;
                        serializer.Serialize(ref layerWeight);

                        var normalizedStateTime = serializer.IsReading ? 0 : LayerStates[paramIndex].NormalizedStateTime;
                        serializer.Serialize(ref normalizedStateTime);


                        if (serializer.IsReading)
                        {
                            LayerStates[paramIndex] = new LayerState()
                            {
                                LayerWeight = layerWeight, StateHash = stateHash,
                                NormalizedStateTime = normalizedStateTime
                            };
                        }
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

        private NetworkVariable<AnimParams> m_AnimParams = new NetworkVariable<AnimParams>();
        
        [SerializeField]
        private Animator m_Animator;

        public Animator Animator => m_Animator;
        
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
        
        private bool IsAuthorityOverAnimator => (IsClient && AnimatorAuthority == Authority.Client && IsOwner) || (IsServer && AnimatorAuthority == Authority.Server) || AnimatorAuthority == Authority.Shared;


        public override void NetworkStart()
        {
            m_AnimParams.Settings.SendTickrate = FixedSendsPerSecond;
            m_AnimParams.Settings.SendNetworkChannel = Channel;
            
            if (IsAuthorityOverAnimator)
            {
                m_AnimParams.Value = new AnimParams(Animator);
            }

            if (AnimatorAuthority == Authority.Client)
            {
                m_AnimParams.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            }
            else if (AnimatorAuthority == Authority.Shared)
            {
                m_AnimParams.Settings.WritePermission = NetworkVariablePermission.Everyone;
            }
        }


        private void OnEnable()
        {
            // Register on value changed delegate. We can't simply check the position every fixed update because of shared authority
            // Shared authority involves writing locally but applying changes when they come from the server. You can't both read from
            // your NetworkPosition and write to it in the same FixedUpdate, you need both separate.
            // There's no conflict resolution here. If two clients try to update the same value at the same time, they'll both think they are right
            m_AnimParams.OnValueChanged += AnimParamsChanged;
        }

        public void OnDisable()
        {
            m_AnimParams.OnValueChanged -= AnimParamsChanged;
        }

        private void AnimParamsChanged(AnimParams previousvalue, AnimParams newvalue)
        {
            if (AnimatorAuthority == Authority.Client && IsClient && IsOwner)
            {
                // this should only happen for my own value changes.
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case
                return;
            }
            
            SetAnimParams(newvalue);
        }
        
        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsAuthorityOverAnimator)
            {
                if (CheckStateChange())
                {
                    m_AnimParams.Value.TriggerParameters.Clear();
                }
            }
        }
        
        private bool CheckStateChange()
        {
            bool changed = false;
            
            for (int i = 0; i < m_AnimParams.Value.LayerStates.Length; i++)
            {
                var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);

                bool didStateChange = m_AnimParams.Value.LayerStates[i].StateHash != animStateInfo.fullPathHash;
                bool enoughDelta = !didStateChange && (animStateInfo.normalizedTime - m_AnimParams.Value.LayerStates[i].NormalizedStateTime) >= 0.15f;

                float newLayerWeight = Animator.GetLayerWeight(i);
                bool layerWeightChanged = Mathf.Abs(m_AnimParams.Value.LayerStates[i].LayerWeight - newLayerWeight) > Mathf.Epsilon;
                
                if (didStateChange || enoughDelta || layerWeightChanged)
                {
                    m_AnimParams.Value.LayerStates[i] = new LayerState
                    {
                        StateHash = animStateInfo.fullPathHash,
                        NormalizedStateTime = animStateInfo.normalizedTime,
                        LayerWeight = newLayerWeight
                    };
                    changed = true;
                }
            }

            foreach (var animParam in m_AnimParams.Value.Parameters)
            {
                var animParamHash = animParam.Key;
                var animParamType = animParam.Value;
                
                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        changed = changed || m_AnimParams.Value.SetFloat(animParamHash, Animator.GetFloat(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        changed = changed || m_AnimParams.Value.SetInt(animParamHash, Animator.GetInteger(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        changed = changed || m_AnimParams.Value.SetBool(animParamHash, Animator.GetBool(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (Animator.GetBool(animParamHash))
                        {
                            changed = changed || m_AnimParams.Value.SetTrigger(animParamHash);
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
            
            for (var layerIndex = 0; layerIndex < animParams.LayerStates.Length; layerIndex++)
            {
                var layerState = animParams.LayerStates[layerIndex];
                
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
