using System.Collections.Generic;
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

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Hash);

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
        private const int k_MaxAnimationParams = 32;

        private int m_AnimationHash;
        private int m_TransitionHash;
        private double m_NextSendTime = 0.0f;

        // 128bytes per Animator 
        private FastBufferWriter m_ParameterWriter = new FastBufferWriter(k_MaxAnimationParams * sizeof(float), Collections.Allocator.Persistent);
        private List<(int, AnimatorControllerParameterType)> m_CachedAnimatorParameters;

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
            m_ParameterWriter.Dispose();
        }

        public override void OnNetworkSpawn()
        {
            var parameters = m_Animator.parameters;
            m_CachedAnimatorParameters = new List<(int, AnimatorControllerParameterType)>(parameters.Length);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (m_Animator.IsParameterControlledByCurve(parameter.nameHash))
                {
                    //we are ignoring parameters that are controlled by animation curves - syncing the layer states indirectly syncs the values that are driven by the animation curves
                    continue;
                }

                m_CachedAnimatorParameters.Add((parameter.nameHash, parameter.type));
            }
        }

        private void FixedUpdate()
        {
            if (!sendMessagesAllowed)
            {
                return;
            }

            CheckSendRate();

            int stateHash;
            float normalizedTime;
            if (!CheckAnimStateChanged(out stateHash, out normalizedTime))
            {
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

        private void CheckSendRate()
        {
            var networkTime = NetworkManager.ServerTime.Time;
            if (sendMessagesAllowed && m_SendRate != 0 && m_NextSendTime < networkTime)
            {
                m_NextSendTime = networkTime + m_SendRate;

                // we then sync the params we care about
                var animMsg = new AnimationParametersMessage();

                m_ParameterWriter.Seek(0);
                m_ParameterWriter.Truncate();

                if (WriteParameters(m_ParameterWriter, true))
                {
                    animMsg.Parameters = m_ParameterWriter.ToArray();
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

        private bool WriteParameters(FastBufferWriter writer, bool autoSend)
        {
            if (m_CachedAnimatorParameters == null)
            {
                return false;
            }

            for (int i = 0; i < m_CachedAnimatorParameters.Count; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                {
                    continue;
                }

                var (hash, paramType) = m_CachedAnimatorParameters[i];
                if (paramType == AnimatorControllerParameterType.Int)
                {
                    BytePacker.WriteValuePacked(writer, (uint)m_Animator.GetInteger(hash));
                }

                if (paramType == AnimatorControllerParameterType.Float)
                {

                    writer.WriteValueSafe(m_Animator.GetFloat(hash));
                }

                if (paramType == AnimatorControllerParameterType.Bool)
                {
                    writer.WriteValueSafe(m_Animator.GetBool(hash));
                }
            }

            return writer.Length > 0;
        }

        private void ReadParameters(FastBufferReader reader, bool autoSend)
        {
            if (m_CachedAnimatorParameters == null)
            {
                return;
            }

            for (int i = 0; i < m_CachedAnimatorParameters.Count; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                {
                    continue;
                }

                var (hash, paramType) = m_CachedAnimatorParameters[i];
                if (paramType == AnimatorControllerParameterType.Int)
                {
                    ByteUnpacker.ReadValuePacked(reader, out int newValue);
                    m_Animator.SetInteger(hash, newValue);
                }

                if (paramType == AnimatorControllerParameterType.Float)
                {
                    reader.ReadValueSafe(out float newFloatValue);

                    m_Animator.SetFloat(hash, newFloatValue);
                }

                if (paramType == AnimatorControllerParameterType.Bool)
                {
                    reader.ReadValueSafe(out bool newBoolValue);
                    m_Animator.SetBool(hash, newBoolValue);
                }
            }
        }

        [ClientRpc]
        private void SendParamsClientRpc(AnimationParametersMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (animSnapshot.Parameters != null)
            {
                var reader = new FastBufferReader(animSnapshot.Parameters, Collections.Allocator.Temp, animSnapshot.Parameters.Length);

                ReadParameters(reader, true);
            }
        }

        [ClientRpc]
        private void SendAnimStateClientRpc(AnimationMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (animSnapshot.StateHash != 0)
            {
                m_Animator.Play(animSnapshot.StateHash, 0, animSnapshot.NormalizedTime);
            }

            var reader = new FastBufferReader(animSnapshot.Parameters, Collections.Allocator.Temp, animSnapshot.Parameters.Length);

            ReadParameters(reader, false);
        }

        [ClientRpc]
        private void SendAnimTriggerClientRpc(AnimationTriggerMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            m_Animator.SetTrigger(animSnapshot.Hash);
        }

        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        public void SetTrigger(int hash)
        {
            var animMsg = new AnimationTriggerMessage();
            animMsg.Hash = hash;

            if (IsServer)
            {
                SendAnimTriggerClientRpc(animMsg);
            }
        }
    }
}
