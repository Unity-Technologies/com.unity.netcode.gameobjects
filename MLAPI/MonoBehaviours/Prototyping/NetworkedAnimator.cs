using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Prototyping
{
    [AddComponentMenu("MLAPI/NetworkedAnimator")]
    public class NetworkedAnimator : NetworkedBehaviour
    {
        public bool EnableProximity = false;
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
        public string param0;
        public string param1;
        public string param2;
        public string param3;
        public string param4;
        public string param5;

        public Animator animator
        {
            get { return _animator; }
            set
            {
                _animator = value;
                ResetParameterOptions();
            }
        }

        public void SetParameterAutoSend(int index, bool value)
        {
            if (value)
            {
                parameterSendBits |=  (uint)(1 << index);
            }
            else
            {
                parameterSendBits &= (uint)(~(1 << index));
            }
        }

        public bool GetParameterAutoSend(int index)
        {
            return (parameterSendBits & (uint)(1 << index)) != 0;
        }

        private bool sendMessagesAllowed
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
            parameterSendBits = 0;
            animatorParameters = null;
        }

        private void FixedUpdate()
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
                    if(EnableProximity)
                    {
                        List<int> clientsInProximity = new List<int>();
                        foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                        {
                            if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                clientsInProximity.Add(client.Key);
                        }
                        SendToClientsTarget(clientsInProximity ,"MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                    else
                        SendToNonLocalClientsTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                }
                else
                {
                    SendToServerTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
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
            if (sendMessagesAllowed && sendRate != 0 && sendTimer < Time.time)
            {
                sendTimer = Time.time + sendRate;

                using(MemoryStream stream = new MemoryStream())
                {
                    using(BinaryWriter writer = new BinaryWriter(stream))
                    {
                        WriteParameters(writer, true);
                    }
                    if (isServer)
                    {
                        if (EnableProximity)
                        {
                            List<int> clientsInProximity = new List<int>();
                            foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                            {
                                if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                    clientsInProximity.Add(client.Key);
                            }
                            SendToClientsTarget(clientsInProximity, "MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                        }
                        else
                            SendToNonLocalClientsTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                    else
                    {
                        SendToServerTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
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

        private void HandleAnimMsg(int clientId, byte[] data)
        {
            // usually transitions will be triggered by parameters, if not, play anims directly.
            // NOTE: this plays "animations", not transitions, so any transitions will be skipped.
            // NOTE: there is no API to play a transition(?)

            if(isServer)
            {
                if (EnableProximity)
                {
                    List<int> clientsInProximity = new List<int>();
                    foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                    {
                        if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                            clientsInProximity.Add(client.Key);
                    }
                    SendToClientsTarget(clientsInProximity, "MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", data);
                }
                else
                    SendToNonLocalClientsTarget("MLAPI_HandleAnimationMessage", "MLAPI_ANIMATION_UPDATE", data);
            }
            using(MemoryStream stream = new MemoryStream(data))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    int stateHash = reader.ReadInt32();
                    float normalizedTime = reader.ReadSingle();
                    if(stateHash != 0)
                    {
                        animator.Play(stateHash, 0, normalizedTime);
                    }
                    ReadParameters(reader, false);
                }
            }
        }

        private void HandleAnimParamsMsg(int clientId, byte[] data)
        {
            if (isServer)
            {
                if (EnableProximity)
                {
                    List<int> clientsInProximity = new List<int>();
                    foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                    {
                        if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                            clientsInProximity.Add(client.Key);
                    }
                    SendToClientsTarget(clientsInProximity, "MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", data);
                }
                else
                    SendToNonLocalClientsTarget("MLAPI_HandleAnimationParameterMessage", "MLAPI_ANIMATION_UPDATE", data);
            }
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ReadParameters(reader, true);
                }
            }
        }

        private void HandleAnimTriggerMsg(int clientId, byte[] data)
        {
            if (isServer)
            {
                if (EnableProximity)
                {
                    List<int> clientsInProximity = new List<int>();
                    foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                    {
                        if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                            clientsInProximity.Add(client.Key);
                    }
                    SendToClientsTarget(clientsInProximity, "MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", data);
                }
                else
                    SendToNonLocalClientsTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", data);
            }
            using (MemoryStream stream = new MemoryStream(data))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    animator.SetTrigger(reader.ReadInt32());
                }
            }
        }

        private void WriteParameters(BinaryWriter writer, bool autoSend)
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
                    writer.Write((uint)animator.GetInteger(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + animator.GetInteger(par.nameHash), i);
                }

                if (par.type == AnimatorControllerParameterType.Float)
                {
                    writer.Write(animator.GetFloat(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + animator.GetFloat(par.nameHash), i);
                }

                if (par.type == AnimatorControllerParameterType.Bool)
                {
                    writer.Write(animator.GetBool(par.nameHash));

                    SetSendTrackingParam(par.name + ":" + animator.GetBool(par.nameHash), i);
                }
            }
        }

        private void ReadParameters(BinaryReader reader, bool autoSend)
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
                    int newValue = (int)reader.ReadUInt32();
                    animator.SetInteger(par.nameHash, newValue);

                    SetRecvTrackingParam(par.name + ":" + newValue, i);
                }

                if (par.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = reader.ReadSingle();
                    animator.SetFloat(par.nameHash, newFloatValue);

                    SetRecvTrackingParam(par.name + ":" + newFloatValue, i);
                }

                if (par.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = reader.ReadBoolean();
                    animator.SetBool(par.nameHash, newBoolValue);

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
                        if (EnableProximity)
                        {
                            List<int> clientsInProximity = new List<int>();
                            foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                            {
                                if (Vector3.Distance(transform.position, client.Value.PlayerObject.transform.position) <= ProximityRange)
                                    clientsInProximity.Add(client.Key);
                            }
                            SendToClientsTarget(clientsInProximity, "MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                        }
                        else
                            SendToNonLocalClientsTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                    else
                    {
                        SendToServerTarget("MLAPI_HandleAnimationTriggerMessage", "MLAPI_ANIMATION_UPDATE", stream.ToArray());
                    }
                }
            }
        }
    }
}
