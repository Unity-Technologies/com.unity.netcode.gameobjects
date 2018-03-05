using System;
using System.IO;
using UnityEngine;

namespace MLAPI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class NetworkedAnimator : NetworkedBehaviour
    {
        // configuration
        [SerializeField] Animator   m_Animator;
        [SerializeField] uint       m_ParameterSendBits;
        [SerializeField] float m_SendRate = 0.1f;

        AnimatorControllerParameter[] m_AnimatorParameters;

        // properties
        public Animator animator
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
                m_ParameterSendBits |=  (uint)(1 << index);
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

        int                     m_AnimationHash;
        int                     m_TransitionHash;
        float                   m_SendTimer;

        // tracking - these should probably move to a Preview component.
        public string   param0;
        public string   param1;
        public string   param2;
        public string   param3;
        public string   param4;
        public string   param5;

        bool sendMessagesAllowed
        {
            get
            {
                return isOwner || isLocalPlayer;
            }
        }

        public override void NetworkStart()
        {
            RegisterMessageHandler("MLAPI_HandleAnimationMessage", HandleAnimMsg);
            RegisterMessageHandler("MLAPI_HandleAnimationParameterMessage", HandleAnimParamsMsg);
            RegisterMessageHandler("MLAPI_HandleAnimationTriggerMessage", HandleAnimTriggerMsg);
        }

        public void ResetParameterOptions()
        {
            Debug.Log("ResetParameterOptions");
            m_ParameterSendBits = 0;
            m_AnimatorParameters = null;
        }

        void FixedUpdate()
        {
            if (!sendMessagesAllowed)
                return;

            CheckSendRate();

            int stateHash;
            float normalizedTime;
            if (!CheckAnimStateChanged(out stateHash, out normalizedTime))
            {
                return;
            }
            using(MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(stateHash);
                    writer.Write(normalizedTime);
                    WriteParameters(writer, false);
                }
                if(isServer)
                {
                    SendToNonLocalClientsTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray(), true);
                }
                else
                {
                    SendToServerTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                }
            }
        }

        bool CheckAnimStateChanged(out int stateHash, out float normalizedTime)
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

        void CheckSendRate()
        {
            if (sendMessagesAllowed && m_SendRate != 0 && m_SendTimer < Time.time)
            {
                m_SendTimer = Time.time + m_SendRate;

                using(MemoryStream stream = new MemoryStream())
                {
                    using(BinaryWriter writer = new BinaryWriter(stream))
                    {
                        WriteParameters(writer, true);
                    }
                    if (isServer)
                    {
                        SendToNonLocalClientsTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray(), true);
                    }
                    else
                    {
                        SendToServerTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                }
            }
        }

        void SetSendTrackingParam(string p, int i)
        {
            p = "Sent Param: " + p;
            if (i == 0) param0 = p;
            if (i == 1) param1 = p;
            if (i == 2) param2 = p;
            if (i == 3) param3 = p;
            if (i == 4) param4 = p;
            if (i == 5) param5 = p;
        }

        void SetRecvTrackingParam(string p, int i)
        {
            p = "Recv Param: " + p;
            if (i == 0) param0 = p;
            if (i == 1) param1 = p;
            if (i == 2) param2 = p;
            if (i == 3) param3 = p;
            if (i == 4) param4 = p;
            if (i == 5) param5 = p;
        }

        internal void HandleAnimMsg(int clientId, byte[] data)
        {
            // usually transitions will be triggered by parameters, if not, play anims directly.
            // NOTE: this plays "animations", not transitions, so any transitions will be skipped.
            // NOTE: there is no API to play a transition(?)

            //isServer AND the message is not from ourselves. This prevents a stack overflow. Infinite call to itself.
            if(isServer)
            {
                SendToNonLocalClientsTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", data, true);
            }
            using(MemoryStream stream = new MemoryStream(data))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    int stateHash = reader.ReadInt32();
                    float normalizedTime = reader.ReadSingle();
                    if(stateHash != 0)
                    {
                        m_Animator.Play(stateHash, 0, normalizedTime);
                    }
                    ReadParameters(reader, false);
                }
            }
        }

        internal void HandleAnimParamsMsg(int clientId, byte[] data)
        {
            if (isServer)
            {
                SendToNonLocalClientsTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", data, true);
            }
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ReadParameters(reader, true);
                }
            }
        }

        internal void HandleAnimTriggerMsg(int clientId, byte[] data)
        {
            if (isServer)
            {
                SendToNonLocalClientsTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", data, true);
            }
            using (MemoryStream stream = new MemoryStream(data))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    m_Animator.SetTrigger(reader.ReadInt32());
                }
            }
        }

        void WriteParameters(BinaryWriter writer, bool autoSend)
        {
            if (m_AnimatorParameters == null) m_AnimatorParameters = m_Animator.parameters;
            for (int i = 0; i < m_AnimatorParameters.Length; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                    continue;

                AnimatorControllerParameter par = m_AnimatorParameters[i];
                if (par.type == AnimatorControllerParameterType.Int)
                {
                    writer.Write((uint)m_Animator.GetInteger(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + m_Animator.GetInteger(par.nameHash), i);
                }

                if (par.type == AnimatorControllerParameterType.Float)
                {
                    writer.Write(m_Animator.GetFloat(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + m_Animator.GetFloat(par.nameHash), i);
                }

                if (par.type == AnimatorControllerParameterType.Bool)
                {
                    writer.Write(m_Animator.GetBool(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + m_Animator.GetBool(par.nameHash), i);
                }
            }
        }

        void ReadParameters(BinaryReader reader, bool autoSend)
        {
            if (m_AnimatorParameters == null) m_AnimatorParameters = m_Animator.parameters;
            for (int i = 0; i < m_AnimatorParameters.Length; i++)
            {
                if (autoSend && !GetParameterAutoSend(i))
                    continue;

                AnimatorControllerParameter par = m_AnimatorParameters[i];
                if (par.type == AnimatorControllerParameterType.Int)
                {
                    int newValue = (int)reader.ReadUInt32();
                    m_Animator.SetInteger(par.nameHash, newValue);

                    SetRecvTrackingParam(par.name + ":" + newValue, i);
                }

                if (par.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = reader.ReadSingle();
                    m_Animator.SetFloat(par.nameHash, newFloatValue);

                    SetRecvTrackingParam(par.name + ":" + newFloatValue, i);
                }

                if (par.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = reader.ReadBoolean();
                    m_Animator.SetBool(par.nameHash, newBoolValue);

                    SetRecvTrackingParam(par.name + ":" + newBoolValue, i);
                }
            }
        }

        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        public void SetTrigger(int hash)
        {
            if (isLocalPlayer || isOwner)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(hash);
                    }
                    if (isServer)
                    {
                        SendToNonLocalClientsTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray(), true);
                    }
                    else
                    {
                        SendToServerTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                }
            }
        }

        public override void OnGainedOwnership()
        {
            ResetParameterOptions();
        }

        public override void OnLostOwnership()
        {
            ResetParameterOptions();
        }
    }
}
