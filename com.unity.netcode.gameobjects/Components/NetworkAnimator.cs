using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkAnimator))]
    [RequireComponent(typeof(Animator))]
    public class NetworkAnimator : NetworkBehaviour
    {
        internal struct AnimationMessage : INetworkSerializable
        {
            public int StateHash;      // if non-zero, then Play() this animation, skipping transitions
            public float NormalizedTime;
            public byte[] Parameters;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref StateHash);
                serializer.SerializeValue(ref NormalizedTime);
                serializer.SerializeValue(ref Parameters);
            }
        }

        internal struct AnimationParametersMessage : INetworkSerializable
        {
            public byte[] Parameters;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Parameters);
            }
        }

        internal struct AnimationTriggerMessage : INetworkSerializable
        {
            public int Hash;
            public bool Reset;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Hash);
                serializer.SerializeValue(ref Reset);
            }
        }

        [SerializeField] private Animator m_Animator;
        [SerializeField] private uint m_ParameterSendBits;
        [SerializeField] private float m_SendRate = 0.1f;

        public Animator Animator
        {
            get { return m_Animator; }
            set
            {
                m_Animator = value;
                ResetParameterOptions();
            }
        }

        /* 
         * AutoSend is the ability to select which parameters linked to this animator 
         * get replicated on a regular basis regardless of a state change. The thinking
         * behind this is that many of the parameters people use are usually booleans 
         * which result in a state change and thus would cause a full sync of state. 
         * Thus if you really care about a parameter syncing then you need to be explict
         * by selecting it in the inspector when an NetworkAnimator is selected.
         */
        public void SetParameterAutoSend(int index, bool value)
        {
            if (value)
            {
                m_ParameterSendBits |= (uint)(1 << index);
            }
            else
            {
                m_ParameterSendBits &= (uint)(~(1 << index));
            }
        }

        public bool GetParameterAutoSend(int index)
        {
            return (m_ParameterSendBits & (uint)(1 << index)) != 0;
        }

        // Animators only support up to 32 params
        public static int K_MaxAnimationParams = 32;

        private int m_TransitionHash;
        private double m_NextSendTime = 0.0f;

        private int m_AnimationHash;
        public int AnimationHash { get => m_AnimationHash; }

        private unsafe struct AnimatorParamCache
        {
            public int Hash;
            public int Type;
            public fixed byte Value[4]; // this is a max size of 4 bytes
        }

        // 128bytes per Animator 
        private FastBufferWriter m_ParameterWriter = new FastBufferWriter(K_MaxAnimationParams * sizeof(float), Allocator.Persistent);
        private NativeArray<AnimatorParamCache> m_CachedAnimatorParameters;

        // We cache these values because UnsafeUtility.EnumToInt use direct IL that allows a nonboxing conversion
        private struct AnimationParamEnumWrapper
        {
            public static readonly int AnimatorControllerParameterInt;
            public static readonly int AnimatorControllerParameterFloat;
            public static readonly int AnimatorControllerParameterBool;

            static AnimationParamEnumWrapper()
            {
                AnimatorControllerParameterInt = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Int);
                AnimatorControllerParameterFloat = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Float);
                AnimatorControllerParameterBool = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Bool);
            }
        }

        internal void ResetParameterOptions()
        {

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfoServer("ResetParameterOptions");
            }

            m_ParameterSendBits = 0;
        }

        private bool sendMessagesAllowed
        {
            get
            {
                return IsServer && NetworkObject.IsSpawned;
            }
        }

        public override void OnDestroy()
        {
            if (m_CachedAnimatorParameters.IsCreated)
            {
                m_CachedAnimatorParameters.Dispose();
            }

            m_ParameterWriter.Dispose();
        }

        public override void OnNetworkSpawn()
        {
            var parameters = m_Animator.parameters;
            m_CachedAnimatorParameters = new NativeArray<AnimatorParamCache>(parameters.Length, Allocator.Persistent);

            m_AnimationHash = -1;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (m_Animator.IsParameterControlledByCurve(parameter.nameHash))
                {
                    //we are ignoring parameters that are controlled by animation curves - syncing the layer states indirectly syncs the values that are driven by the animation curves
                    continue;
                }

                var cacheParam = new AnimatorParamCache();

                cacheParam.Type = UnsafeUtility.EnumToInt(parameter.type);
                cacheParam.Hash = parameter.nameHash;
                unsafe
                {
                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            var value = m_Animator.GetFloat(cacheParam.Hash);
                            UnsafeUtility.WriteArrayElement(cacheParam.Value, 0, value);
                            break;
                        case AnimatorControllerParameterType.Int:
                            var valueInt = m_Animator.GetInteger(cacheParam.Hash);
                            UnsafeUtility.WriteArrayElement(cacheParam.Value, 0, valueInt);

                            break;
                        case AnimatorControllerParameterType.Bool:
                            var valueBool = m_Animator.GetBool(cacheParam.Hash);
                            UnsafeUtility.WriteArrayElement(cacheParam.Value, 0, valueBool);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                        default:
                            break;
                    }
                }

                m_CachedAnimatorParameters[i] = cacheParam;
            }
        }

        private void FixedUpdate()
        {
            if (!sendMessagesAllowed)
            {
                return;
            }

            int stateHash;
            float normalizedTime;
            if (!CheckAnimStateChanged(out stateHash, out normalizedTime))
            {
                // We only want to check and send if we don't have any other state to since
                // as we will sync all params as part of the state sync
                CheckAndSend();

                return;
            }

            var animMsg = new AnimationMessage();
            animMsg.StateHash = stateHash;
            animMsg.NormalizedTime = normalizedTime;

            m_ParameterWriter.Seek(0);
            m_ParameterWriter.Truncate();

            WriteParameters(m_ParameterWriter, false);
            animMsg.Parameters = m_ParameterWriter.ToArray();

            SendAnimStateClientRpc(animMsg);
        }

        private void CheckAndSend()
        {
            var networkTime = NetworkManager.ServerTime.Time;
            if (sendMessagesAllowed && m_SendRate != 0 && m_NextSendTime < networkTime)
            {
                m_NextSendTime = networkTime + m_SendRate;

                m_ParameterWriter.Seek(0);
                m_ParameterWriter.Truncate();

                if (WriteParameters(m_ParameterWriter, true))
                {
                    // we then sync the params we care about
                    var animMsg = new AnimationParametersMessage()
                    {
                        Parameters = m_ParameterWriter.ToArray()
                    };

                    SendParamsClientRpc(animMsg);
                }
            }
        }

        private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime)
        {
            stateHash = 0;
            normalizedTime = 0;

            if (m_Animator.IsInTransition(0))
            {
                AnimatorTransitionInfo tt = m_Animator.GetAnimatorTransitionInfo(0);
                if (tt.fullPathHash != m_TransitionHash)
                {
                    // first time in this transition
                    m_TransitionHash = tt.fullPathHash;
                    m_AnimationHash = 0;
                    return true;
                }
                return false;
            }

            AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (st.fullPathHash != m_AnimationHash)
            {
                // first time in this animation state
                if (m_AnimationHash != 0)
                {
                    // came from another animation directly - from Play()
                    stateHash = st.fullPathHash;
                    normalizedTime = st.normalizedTime;
                }
                m_TransitionHash = 0;
                m_AnimationHash = st.fullPathHash;
                return true;
            }
            return false;
        }

        /* $AS TODO: Right now we are not checking for changed values this is because
        the read side of this function doesn't have similar logic which would cause
        an overflow read because it doesn't know if the value is there or not. So 
        there needs to be logic to track which indexes changed in order for there 
        to be proper value change checking. Will revist in 1.1.0.
        */
        private unsafe bool WriteParameters(FastBufferWriter writer, bool autoSend)
        {
            if (m_CachedAnimatorParameters == null)
            {
                return false;
            }

            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                {
                    continue;
                }

                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
                var hash = cacheValue.Hash;

                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
                {
                    var valueInt = m_Animator.GetInteger(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueInt);
                        BytePacker.WriteValuePacked(writer, (uint)valueInt);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    var valueBool = m_Animator.GetBool(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueBool);
                        writer.WriteValueSafe(valueBool);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    var valueFloat = m_Animator.GetFloat(hash);
                    fixed (void* value = cacheValue.Value)
                    {

                        UnsafeUtility.WriteArrayElement(value, 0, valueFloat);
                        writer.WriteValueSafe(valueFloat);
                    }
                }
            }

            // If we do not write any values to the writer then we should not send any data
            return writer.Length > 0;
        }

        private unsafe void ReadParameters(FastBufferReader reader, bool autoSend)
        {
            if (m_CachedAnimatorParameters == null)
            {
                return;
            }

            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                {
                    continue;
                }
                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
                var hash = cacheValue.Hash;

                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
                {
                    ByteUnpacker.ReadValuePacked(reader, out int newValue);
                    m_Animator.SetInteger(hash, newValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newValue);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    reader.ReadValueSafe(out bool newBoolValue);
                    m_Animator.SetBool(hash, newBoolValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newBoolValue);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    reader.ReadValueSafe(out float newFloatValue);
                    m_Animator.SetFloat(hash, newFloatValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newFloatValue);
                    }
                }
            }
        }

        [ClientRpc]
        private unsafe void SendParamsClientRpc(AnimationParametersMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (animSnapshot.Parameters != null)
            {
                // We use a fixed value here to avoid the copy of data from the byte buffer since we own the data
                fixed (byte* parameters = animSnapshot.Parameters)
                {
                    var reader = new FastBufferReader(parameters, Allocator.None, animSnapshot.Parameters.Length);
                    ReadParameters(reader, true);
                }
            }
        }

        [ClientRpc]
        private unsafe void SendAnimStateClientRpc(AnimationMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (animSnapshot.StateHash != 0)
            {
                m_AnimationHash = animSnapshot.StateHash;
                m_Animator.Play(animSnapshot.StateHash, 0, animSnapshot.NormalizedTime);
            }

            if (animSnapshot.Parameters != null && animSnapshot.Parameters.Length != 0)
            {
                // We use a fixed value here to avoid the copy of data from the byte buffer since we own the data
                fixed (byte* parameters = animSnapshot.Parameters)
                {
                    var reader = new FastBufferReader(parameters, Allocator.None, animSnapshot.Parameters.Length);
                    ReadParameters(reader, false);
                }
            }
        }

        [ClientRpc]
        private void SendAnimTriggerClientRpc(AnimationTriggerMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (animSnapshot.Reset)
            {
                m_Animator.ResetTrigger(animSnapshot.Hash);
            }
            else
            {
                m_Animator.SetTrigger(animSnapshot.Hash);
            }
        }

        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        public void SetTrigger(int hash, bool reset = false)
        {
            var animMsg = new AnimationTriggerMessage();
            animMsg.Hash = hash;
            animMsg.Reset = reset;

            if (IsServer)
            {
                SendAnimTriggerClientRpc(animMsg);
            }
        }

        public void ResetTrigger(string triggerName)
        {
            ResetTrigger(Animator.StringToHash(triggerName));
        }

        public void ResetTrigger(int hash)
        {
            SetTrigger(hash, true);
        }
    }
}
