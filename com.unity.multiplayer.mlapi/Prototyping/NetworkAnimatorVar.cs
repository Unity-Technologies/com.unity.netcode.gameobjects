using System.Collections.Generic;
using System.Linq;
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
        /// </summary>
        public enum Authority
        {
            Server = 0,
            Owner,
        }

        public Animator Animator => m_Animator;
        
        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this animator
        /// </summary>
        [Tooltip("Defines who can update this transform.")]
        public Authority AnimatorAuthority = Authority.Owner;
        
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
        private List<(int, AnimatorControllerParameterType)> m_CachedAnimatorParameters;
        private Dictionary<int, bool> m_BoolParameters;
        private Dictionary<int, float> m_FloatParameters;
        private Dictionary<int, int> m_IntParameters;
        private HashSet<int> m_TriggerParameters;
        public int LayerCount => m_States.Length;

        private LayerState[] m_States;

        private bool IsAuthorityOverAnimator => (IsClient && AnimatorAuthority == Authority.Owner && IsOwner) ||
                                                (IsServer && AnimatorAuthority == Authority.Server);

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
                    for (int i = 0; i < LayerCount; i++)
                    {
                        var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);
                        
                        m_States[i] = new LayerState
                        {
                            StateHash = animStateInfo.fullPathHash,
                            NormalizedStateTime = animStateInfo.normalizedTime,
                            LayerWeight = Animator.GetLayerWeight(i)
                        };
                    }

                    m_AnimatorState.Value =  new AnimatorSnapshot(m_BoolParameters, m_FloatParameters, m_IntParameters, m_TriggerParameters, m_States);
    
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

        private bool SetInt(int key, int value)
        {
            if (m_IntParameters.TryGetValue(key, out var existingValue) && existingValue == value)
            {
                return false;
            }

            m_IntParameters[key] = value;
            return true;
        }

        private bool SetBool(int key, bool value)
        {
            if (m_BoolParameters.TryGetValue(key, out var existingValue) && existingValue == value)
            {
                return false;
            }

            m_BoolParameters[key] = value;
            return true;
        }

        private bool SetFloat(int key, float value)
        {
            if (m_FloatParameters.TryGetValue(key, out var existingValue) &&
                Mathf.Abs(existingValue - value) < Mathf.Epsilon)
            {
                return false;
            }

            m_FloatParameters[key] = value;
            return true;
        }

        private bool SetTrigger(int key)
        {
            return m_TriggerParameters.Add(key);
        }

        public override void NetworkStart()
        {
            if (AnimatorAuthority == Authority.Owner)
            {
                m_AnimatorState.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
            }else if (AnimatorAuthority == Authority.Server)
            {
                m_AnimatorState.Settings.WritePermission = NetworkVariablePermission.ServerOnly;
            }
            
            m_AnimatorState.Settings.SendTickrate = FixedSendsPerSecond;
            m_AnimatorState.Settings.SendNetworkChannel = Channel;

            if (IsAuthorityOverAnimator)
            {
                var parameters = m_Animator.parameters;
                m_CachedAnimatorParameters = new List<(int, AnimatorControllerParameterType)>(parameters.Length);

                int intCount = 0;
                int floatCount = 0;
                int boolCount = 0;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                
                    if (m_Animator.IsParameterControlledByCurve(parameter.nameHash))
                    {
                        //we are ignoring parameters that are controlled by animation curves - syncing the layer states indirectly syncs the values that are driven by the animation curves 
                        continue;
                    }

                    m_CachedAnimatorParameters.Add((parameter.nameHash, parameter.type));

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
                m_States = new LayerState[Animator.layerCount];
                
                PollAnimatorParameters();

                for (int i = 0; i < LayerCount; i++)
                {
                    var animStateInfo = Animator.GetCurrentAnimatorStateInfo(i);
                    
                    m_States[i] = new LayerState
                    {
                        StateHash = animStateInfo.fullPathHash,
                        NormalizedStateTime = animStateInfo.normalizedTime,
                        LayerWeight = Animator.GetLayerWeight(i)
                    };
                }

                m_AnimatorState.Value = new AnimatorSnapshot(m_BoolParameters, m_FloatParameters, m_IntParameters, m_TriggerParameters, m_States);

                m_TriggerParameters.Clear();
            }
        }

        private bool PollAnimatorParameters()
        {
            bool changed = false;

            foreach (var animParam in m_CachedAnimatorParameters)
            {
                var animParamHash = animParam.Item1;
                var animParamType = animParam.Item2;

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
            for (int i = 0; i < LayerCount; i++)
            {
                var animStateInfo = m_Animator.GetCurrentAnimatorStateInfo(i);

                bool didStateChange = m_States[i].StateHash != animStateInfo.fullPathHash;
                bool enoughDelta = !didStateChange && (animStateInfo.normalizedTime - m_States[i].NormalizedStateTime) >= 0.15f;
                bool layerWeightChanged = Mathf.Abs(m_States[i].LayerWeight - m_Animator.GetLayerWeight(i)) > Mathf.Epsilon;

                if (didStateChange || enoughDelta || layerWeightChanged)
                {
                    return true;
                }
            }

            return false;
        }

        private void AnimParamsChanged(AnimatorSnapshot previousvalue, AnimatorSnapshot newvalue)
        {
            if (IsAuthorityOverAnimator || newvalue == null)
            {
                return;
            }
            
            ApplyAnimatorSnapshot(newvalue);
        }

        private void ApplyAnimatorSnapshot(AnimatorSnapshot animatorSnapshot)
        {
            foreach (var intParameter in animatorSnapshot.IntParameters)
            {
                Animator.SetInteger(intParameter.Key, intParameter.Value);
            }

            foreach (var floatParameter in animatorSnapshot.FloatParameters)
            {
                Animator.SetFloat(floatParameter.Key, floatParameter.Value);
            }

            foreach (var boolParameter in animatorSnapshot.BoolParameters)
            {
                Animator.SetBool(boolParameter.Key, boolParameter.Value);
            }

            foreach (var triggerParameter in animatorSnapshot.TriggerParameters)
            {
                Animator.SetTrigger(triggerParameter);
            }
            
            for (var layerIndex = 0; layerIndex < animatorSnapshot.States.Length; layerIndex++)
            {
                var layerState = animatorSnapshot.States[layerIndex];

                m_Animator.SetLayerWeight(layerIndex, layerState.LayerWeight);

                var currentAnimatorState = m_Animator.GetCurrentAnimatorStateInfo(layerIndex);

                bool stateChanged = currentAnimatorState.fullPathHash != layerState.StateHash;
                bool forceAnimationCatchup = !stateChanged && Mathf.Abs(currentAnimatorState.normalizedTime - currentAnimatorState.normalizedTime) >= 0.15f;

                if (stateChanged || forceAnimationCatchup)
                {
                    m_Animator.Play(layerState.StateHash, layerIndex, layerState.NormalizedStateTime);
                }
            }
        }

        private class AnimatorSnapshot : INetworkSerializable
        {
            public Dictionary<int, bool> BoolParameters;
            public Dictionary<int, float> FloatParameters;
            public Dictionary<int, int> IntParameters;
            public HashSet<int> TriggerParameters;
            public LayerState[] States;
            
            public AnimatorSnapshot()
            {
                BoolParameters = new Dictionary<int, bool>(0);
                FloatParameters = new Dictionary<int, float>(0);
                IntParameters = new Dictionary<int, int>(0);
                TriggerParameters = new HashSet<int>();
                States = new LayerState[0];
            }
            
            public AnimatorSnapshot(Dictionary<int, bool> boolParameters, Dictionary<int, float> floatParameters, Dictionary<int, int> intParameters, HashSet<int> triggerParameters, LayerState[] states)
            {
                BoolParameters = boolParameters;
                FloatParameters = floatParameters;
                IntParameters = intParameters;
                TriggerParameters = triggerParameters;
                States = states;
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
                
                //layer state
                {
                    int layerCount = serializer.IsReading ? 0 : States.Length;
                    serializer.Serialize(ref layerCount);

                    if (serializer.IsReading && States.Length != layerCount)
                    {
                        States = new LayerState[layerCount];
                    }

                    for (int paramIndex = 0; paramIndex < layerCount; paramIndex++)
                    {
                        var stateHash = serializer.IsReading ? 0 : States[paramIndex].StateHash;
                        serializer.Serialize(ref stateHash);

                        var layerWeight = serializer.IsReading ? 0 : States[paramIndex].LayerWeight;
                        serializer.Serialize(ref layerWeight);

                        var normalizedStateTime = serializer.IsReading ? 0 : States[paramIndex].NormalizedStateTime;
                        serializer.Serialize(ref normalizedStateTime);

                        if (serializer.IsReading)
                        {
                            States[paramIndex] = new LayerState()
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
    }
}