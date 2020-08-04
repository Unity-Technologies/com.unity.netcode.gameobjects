using System.Collections.Generic;
using System.IO;
using MLAPI.Connection;
using MLAPI.Logging;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing animations
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedAnimator")]
    public class NetworkedAnimator : NetworkedBehaviour
    {
        /// <summary>
        /// Is proximity enabled
        /// </summary>
        public bool EnableProximity = false;
        /// <summary>
        /// The proximity range
        /// </summary>
        public float ProximityRange = 50f;

        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private uint parameterSendBits;
        [SerializeField]
        private readonly float sendRate = 0.1f;
        private AnimatorControllerParameter[] animatorParameters;

        private int animationHash;
        private int transitionHash;
        private float sendTimer;


        // tracking - these should probably move to a Preview component. -- Comment from HLAPI. Needs clarification
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string param0;
        public string param1;
        public string param2;
        public string param3;
        public string param4;
        public string param5;
        #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member


        /// <summary>
        /// Gets or sets the animator component used for syncing the animations
        /// </summary>
        public Animator animator
        {
            get { return _animator; }
            set
            {
                _animator = value;
                ResetParameterOptions();
            }
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetParameterAutoSend(int index, bool value)
        {
            if (value)
            {
                parameterSendBits |= (uint)(1 << index);
            }
            else
            {
                parameterSendBits &= (uint)(~(1 << index));
            }
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool GetParameterAutoSend(int index)
        {
            return (parameterSendBits & (uint)(1 << index)) != 0;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void ResetParameterOptions()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogInfo("ResetParameterOptions");
            parameterSendBits = 0;
            animatorParameters = null;
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;

            CheckSendRate();

            int stateHash;
            float normalizedTime;
            if (!CheckAnimStateChanged(out stateHash, out normalizedTime))
            {
                return;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteInt32Packed(stateHash);
                    writer.WriteSinglePacked(normalizedTime);
                    WriteParameters(stream, false);

                    if (IsServer)
                    {
                        if (EnableProximity)
                        {
                            List<ulong> clientsInProximity = new List<ulong>();
                            foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                            {
                                if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                    clientsInProximity.Add(client.Key);
                            }
                            InvokeClientRpcPerformance(ApplyAnimParamMsg, clientsInProximity, stream);
                        }
                        else
                        {
                            InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimMsg, OwnerClientId, stream);
                        }
                    }
                    else
                    {
                        InvokeServerRpcPerformance(SubmitAnimMsg, stream);
                    }
                }
            }
        }

        private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime)
        {
            stateHash = 0;
            normalizedTime = 0;

            if (animator.IsInTransition(0))
            {
                AnimatorTransitionInfo animationTransitionInfo = animator.GetAnimatorTransitionInfo(0);
                if (animationTransitionInfo.fullPathHash != transitionHash)
                {
                    // first time in this transition
                    transitionHash = animationTransitionInfo.fullPathHash;
                    animationHash = 0;
                    return true;
                }
                return false;
            }

            AnimatorStateInfo animationSateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (animationSateInfo.fullPathHash != animationHash)
            {
                // first time in this animation state
                if (animationHash != 0)
                {
                    // came from another animation directly - from Play()
                    stateHash = animationSateInfo.fullPathHash;
                    normalizedTime = animationSateInfo.normalizedTime;
                }
                transitionHash = 0;
                animationHash = animationSateInfo.fullPathHash;
                return true;
            }
            return false;
        }

        private void CheckSendRate()
        {
            if (IsOwner && sendRate != 0 && sendTimer < NetworkingManager.Singleton.NetworkTime)
            {
                sendTimer = NetworkingManager.Singleton.NetworkTime + sendRate;

                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        WriteParameters(stream, true);

                        if (IsServer)
                        {
                            if (EnableProximity)
                            {
                                List<ulong> clientsInProximity = new List<ulong>();
                                foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                                {
                                    if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                        clientsInProximity.Add(client.Key);
                                }
                                InvokeClientRpcPerformance(ApplyAnimParamMsg, clientsInProximity, stream);
                            }
                            else
                            {
                                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimParamMsg, OwnerClientId, stream);
                            }
                        }
                        else
                        {
                            InvokeServerRpcPerformance(SubmitAnimParamMsg, stream);
                        }
                    }
                }
            }
        }

        private void SetSendTrackingParam(string p, int i)
        {
            p = "Sent Param: " + p;
            if (i == 0) param0 = p;
            if (i == 1) param1 = p;
            if (i == 2) param2 = p;
            if (i == 3) param3 = p;
            if (i == 4) param4 = p;
            if (i == 5) param5 = p;
        }

        private void SetRecvTrackingParam(string p, int i)
        {
            p = "Recv Param: " + p;
            if (i == 0) param0 = p;
            if (i == 1) param1 = p;
            if (i == 2) param2 = p;
            if (i == 3) param3 = p;
            if (i == 4) param4 = p;
            if (i == 5) param5 = p;
        }

        [ServerRPC]
        private void SubmitAnimMsg(ulong clientId, Stream stream)
        {
            // usually transitions will be triggered by parameters, if not, play anims directly.
            // NOTE: this plays "animations", not transitions, so any transitions will be skipped.
            // NOTE: there is no API to play a transition(?)

            if (EnableProximity)
            {
                List<ulong> clientsInProximity = new List<ulong>();
                foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                {
                    if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                        clientsInProximity.Add(client.Key);
                }
                InvokeClientRpcPerformance(ApplyAnimMsg, clientsInProximity, stream);
            }
            else
            {
                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimMsg, OwnerClientId, stream);
            }
        }

        [ClientRPC]
        private void ApplyAnimMsg(ulong clientId, Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                int stateHash = reader.ReadInt32Packed();
                float normalizedTime = reader.ReadSinglePacked();
                if (stateHash != 0)
                {
                    animator.Play(stateHash, 0, normalizedTime);
                }
                ReadParameters(stream, false);
            }
        }

        [ServerRPC]
        private void SubmitAnimParamMsg(ulong clientId, Stream stream)
        {
            if (EnableProximity)
            {
                List<ulong> clientsInProximity = new List<ulong>();
                foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                {
                    if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                        clientsInProximity.Add(client.Key);
                }
                InvokeClientRpcPerformance(ApplyAnimParamMsg, clientsInProximity, stream);
            }
            else
            {
                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimParamMsg, OwnerClientId, stream);
            }
        }

        [ClientRPC]
        private void ApplyAnimParamMsg(ulong clientId, Stream stream)
        {
            ReadParameters(stream, true);
        }

        [ServerRPC]
        private void SubmitAnimTriggerMsg(ulong clientId, Stream stream)
        {
            if (EnableProximity)
            {
                List<ulong> clientsInProximity = new List<ulong>();
                foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                {
                    if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                        clientsInProximity.Add(client.Key);
                }
                InvokeClientRpcPerformance(ApplyAnimTriggerMsg, clientsInProximity, stream);
            }
            else
            {
                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimTriggerMsg, OwnerClientId, stream);
            }
        }

        [ClientRPC]
        private void ApplyAnimTriggerMsg(ulong clientId, Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                animator.SetTrigger(reader.ReadInt32Packed());
            }
        }

        private void WriteParameters(Stream stream, bool autoSend)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                if (animatorParameters == null)
                    animatorParameters = animator.parameters;

                for (int i = 0; i < animatorParameters.Length; i++)
                {
                    if (autoSend && !GetParameterAutoSend(i))
                        continue;

                    AnimatorControllerParameter par = animatorParameters[i];
                    if (par.type == AnimatorControllerParameterType.Int)
                    {
                        writer.WriteUInt32Packed((uint)animator.GetInteger(par.nameHash));

                        SetSendTrackingParam(par.name + ":" + animator.GetInteger(par.nameHash), i);
                    }

                    if (par.type == AnimatorControllerParameterType.Float)
                    {
                        writer.WriteSinglePacked(animator.GetFloat(par.nameHash));

                        SetSendTrackingParam(par.name + ":" + animator.GetFloat(par.nameHash), i);
                    }

                    if (par.type == AnimatorControllerParameterType.Bool)
                    {
                        writer.WriteBool(animator.GetBool(par.nameHash));

                        SetSendTrackingParam(par.name + ":" + animator.GetBool(par.nameHash), i);
                    }
                }
            }
        }

        private void ReadParameters(Stream stream, bool autoSend)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (animatorParameters == null)
                    animatorParameters = animator.parameters;

                for (int i = 0; i < animatorParameters.Length; i++)
                {
                    if (autoSend && !GetParameterAutoSend(i))
                        continue;

                    AnimatorControllerParameter par = animatorParameters[i];
                    if (par.type == AnimatorControllerParameterType.Int)
                    {
                        int newValue = (int)reader.ReadUInt32Packed();
                        animator.SetInteger(par.nameHash, newValue);

                        SetRecvTrackingParam(par.name + ":" + newValue, i);
                    }

                    if (par.type == AnimatorControllerParameterType.Float)
                    {
                        float newFloatValue = reader.ReadSinglePacked();
                        animator.SetFloat(par.nameHash, newFloatValue);

                        SetRecvTrackingParam(par.name + ":" + newFloatValue, i);
                    }

                    if (par.type == AnimatorControllerParameterType.Bool)
                    {
                        bool newBoolValue = reader.ReadBool();
                        animator.SetBool(par.nameHash, newBoolValue);

                        SetRecvTrackingParam(par.name + ":" + newBoolValue, i);
                    }
                }
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="triggerName"></param>
        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="hash"></param>
        public void SetTrigger(int hash)
        {
            if (IsOwner)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteInt32Packed(hash);

                        if (IsServer)
                        {
                            if (EnableProximity)
                            {
                                List<ulong> clientsInProximity = new List<ulong>();
                                foreach (KeyValuePair<ulong, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                                {
                                    if (client.Value.PlayerObject == null || Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                        clientsInProximity.Add(client.Key);
                                }
                                InvokeClientRpcPerformance(ApplyAnimTriggerMsg, clientsInProximity, stream);
                            }
                            else
                            {
                                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimTriggerMsg, OwnerClientId, stream);
                            }
                        }
                        else
                        {
                            InvokeServerRpcPerformance(SubmitAnimTriggerMsg, stream);
                        }
                    }
                }
            }
        }
    }
}
