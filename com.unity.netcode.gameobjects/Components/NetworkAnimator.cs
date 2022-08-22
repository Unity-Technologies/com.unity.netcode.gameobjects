#if COM_UNITY_MODULES_ANIMATION
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode.Components
{
    internal class NetworkAnimatorStateChangeHandler : INetworkUpdateSystem
    {
        private NetworkAnimator m_NetworkAnimator;

        /// <summary>
        /// This removes sending RPCs from within RPCs when the
        /// server is forwarding updates from clients to clients
        /// As well this handles newly connected client synchronization
        /// of the existing Animator's state.
        /// </summary>
        private void FlushMessages()
        {
            foreach (var clientId in m_ClientsToSynchronize)
            {
                m_NetworkAnimator.ServerSynchronizeNewPlayer(clientId);
            }
            m_ClientsToSynchronize.Clear();

            foreach (var sendEntry in m_SendParameterUpdates)
            {
                m_NetworkAnimator.SendParametersUpdateClientRpc(sendEntry.ParametersUpdateMessage, sendEntry.ClientRpcParams);
            }
            m_SendParameterUpdates.Clear();

            foreach (var sendEntry in m_SendTriggerUpdates)
            {
                if (!sendEntry.SendToServer)
                {
                    m_NetworkAnimator.SendAnimTriggerClientRpc(sendEntry.AnimationTriggerMessage, sendEntry.ClientRpcParams);
                }
                else
                {
                    m_NetworkAnimator.SendAnimTriggerServerRpc(sendEntry.AnimationTriggerMessage);
                }
            }
            m_SendTriggerUpdates.Clear();
        }

        /// <inheritdoc />
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.PreUpdate:
                    {
                        // Only the server forwards messages and synchronizes players
                        if (m_NetworkAnimator.NetworkManager.IsServer)
                        {
                            // Flush any pending messages
                            FlushMessages();
                        }

                        // Everyone applies any parameters updated
                        foreach (var parameterUpdate in m_ProcessParameterUpdates)
                        {
                            m_NetworkAnimator.UpdateParameters(parameterUpdate);
                        }
                        m_ProcessParameterUpdates.Clear();

                        // Only owners check for Animator changes
                        if (m_NetworkAnimator.IsOwner && !m_NetworkAnimator.IsServerAuthoritative() || m_NetworkAnimator.IsServerAuthoritative() && m_NetworkAnimator.NetworkManager.IsServer)
                        {
                            m_NetworkAnimator.CheckForAnimatorChanges();
                        }

                        ProcessAnimationMessageQueue();
                        break;
                    }
            }
        }

        /// <summary>
        /// Clients that need to be synchronized to the relative Animator
        /// </summary>
        private List<ulong> m_ClientsToSynchronize = new List<ulong>();

        /// <summary>
        /// When a new client is connected, they are added to the
        /// m_ClientsToSynchronize list.
        /// </summary>
        internal void SynchronizeClient(ulong clientId)
        {
            m_ClientsToSynchronize.Add(clientId);
        }

        /// <summary>
        /// A pending outgoing Animation update for (n) clients
        /// </summary>
        private struct AnimationUpdate
        {
            public ClientRpcParams ClientRpcParams;
            public NetworkAnimator.AnimationMessage AnimationMessage;
        }

        private List<AnimationUpdate> m_SendAnimationUpdates = new List<AnimationUpdate>();

        /// <summary>
        /// Invoked when a server needs to forwarding an update to the animation state
        /// </summary>
        internal void SendAnimationUpdate(NetworkAnimator.AnimationMessage animationMessage, ClientRpcParams clientRpcParams = default)
        {
            m_SendAnimationUpdates.Add(new AnimationUpdate() { ClientRpcParams = clientRpcParams, AnimationMessage = animationMessage });
        }

        private struct ParameterUpdate
        {
            public ClientRpcParams ClientRpcParams;
            public NetworkAnimator.ParametersUpdateMessage ParametersUpdateMessage;
        }

        private List<ParameterUpdate> m_SendParameterUpdates = new List<ParameterUpdate>();

        /// <summary>
        /// Invoked when a server needs to forwarding an update to the parameter state
        /// </summary>
        internal void SendParameterUpdate(NetworkAnimator.ParametersUpdateMessage parametersUpdateMessage, ClientRpcParams clientRpcParams = default)
        {
            m_SendParameterUpdates.Add(new ParameterUpdate() { ClientRpcParams = clientRpcParams, ParametersUpdateMessage = parametersUpdateMessage });
        }

        private List<NetworkAnimator.ParametersUpdateMessage> m_ProcessParameterUpdates = new List<NetworkAnimator.ParametersUpdateMessage>();
        internal void ProcessParameterUpdate(NetworkAnimator.ParametersUpdateMessage parametersUpdateMessage)
        {
            m_ProcessParameterUpdates.Add(parametersUpdateMessage);
        }

        private struct TriggerUpdate
        {
            public bool SendToServer;
            public ClientRpcParams ClientRpcParams;
            public NetworkAnimator.AnimationTriggerMessage AnimationTriggerMessage;
        }

        private List<TriggerUpdate> m_SendTriggerUpdates = new List<TriggerUpdate>();

        /// <summary>
        /// Invoked when a server needs to forward an update to a Trigger state
        /// </summary>
        internal void QueueTriggerUpdateToClient(NetworkAnimator.AnimationTriggerMessage animationTriggerMessage, ClientRpcParams clientRpcParams = default)
        {
            m_SendTriggerUpdates.Add(new TriggerUpdate() { ClientRpcParams = clientRpcParams, AnimationTriggerMessage = animationTriggerMessage });
        }

        internal void QueueTriggerUpdateToServer(NetworkAnimator.AnimationTriggerMessage animationTriggerMessage)
        {
            m_SendTriggerUpdates.Add(new TriggerUpdate() { AnimationTriggerMessage = animationTriggerMessage, SendToServer = true });
        }

        private Queue<NetworkAnimator.AnimationMessage> m_AnimationMessageQueue = new Queue<NetworkAnimator.AnimationMessage>();

        // We initialize the first animation message as having been processed so it will pull from the queue
        private NetworkAnimator.AnimationMessage m_AnimationMessageBeingProcessed = new NetworkAnimator.AnimationMessage() { HasBeenProcessed = true };

        /// <summary>
        ///  This processes any inbound pending <see cref="NetworkAnimator.AnimationMessage"/>s
        ///  that need processing.
        /// </summary>
        /// <remarks>
        /// Currently we have no way to link triggers to transition states
        /// <see cref="NetworkAnimator.m_LastTriggerHash"/>
        /// </remarks>
        private void ProcessAnimationMessageQueue()
        {
            // Early exit if nothing to process
            if (m_AnimationMessageQueue.Count == 0)
            {
                return;
            }

            if (m_AnimationMessageBeingProcessed.HasBeenProcessed)
            {
                m_AnimationMessageBeingProcessed = m_AnimationMessageQueue.Dequeue();
            }

            // Process all non-transition animation states that haven't been processed in
            // one pass
            for (int i = 0; i < m_AnimationMessageBeingProcessed.AnimationStates.Count; i++)
            {
                var animationState = m_AnimationMessageBeingProcessed.AnimationStates[i];
                if (animationState.HasBeenProcessed || animationState.Transition)
                {
                    continue;
                }

                m_NetworkAnimator.UpdateAnimationState(animationState);
                animationState.HasBeenProcessed = true;
                // Apply the changes
                m_AnimationMessageBeingProcessed.AnimationStates[i] = animationState;
            }

            // The last thing we apply is triggers and their transition states
            m_AnimationMessageBeingProcessed.HasBeenProcessed = !ProcessTransitionSynchronization();
        }

        /// <summary>
        /// This is primarily for late joining client trigger and transition synchronization
        /// </summary>
        private bool ProcessTransitionSynchronization()
        {
            bool continueProcessing = false;
            for (int i = 0; i < m_AnimationMessageBeingProcessed.AnimationStates.Count; i++)
            {
                var animationState = m_AnimationMessageBeingProcessed.AnimationStates[i];
                // Skip things already processed or are not transition states
                if (animationState.HasBeenProcessed || !animationState.Transition)
                {
                    continue;
                }

                // Only occurs for newly joined clients
                if (!animationState.TriggerProcessed)
                {
                    if (animationState.TriggerHashValues != null && animationState.TriggerHashValues.Count > 0)
                    {
                        // Set all triggers associated with the state
                        foreach (var triggerHash in animationState.TriggerHashValues)
                        {
                            // Set trigger
                            m_NetworkAnimator.SetTrigger(triggerHash);
                        }
                    }

                    // Then set this to false to signify we are done
                    animationState.TriggerProcessed = true;

                    // Apply the changes
                    m_AnimationMessageBeingProcessed.AnimationStates[i] = animationState;

                    // We need to mark this as true so the AnimationMessage doesn't get
                    // marked as having been fully processed (still need to sync the state)
                    continueProcessing = true;
                    /// <see cref="NetworkAnimator.AnimationState"/> and NetworkAnimator.m_LastTriggerHash
                    break;
                }
                else
                {
                    // Synchronize the triggered transition state
                    m_NetworkAnimator.UpdateAnimationState(animationState);
                    animationState.HasBeenProcessed = true;
                    // Apply the changes
                    m_AnimationMessageBeingProcessed.AnimationStates[i] = animationState;
                }
            }
            return continueProcessing;
        }

        internal void AddAnimationMessageToProcessQueue(NetworkAnimator.AnimationMessage message)
        {
            m_AnimationMessageQueue.Enqueue(message);
        }

        internal void DeregisterUpdate()
        {
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
        }

        internal NetworkAnimatorStateChangeHandler(NetworkAnimator networkAnimator)
        {
            m_NetworkAnimator = networkAnimator;
            NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
        }
    }



    /// <summary>
    /// NetworkAnimator enables remote synchronization of <see cref="UnityEngine.Animator"/> state for on network objects.
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkAnimator))]
    [RequireComponent(typeof(Animator))]
    public class NetworkAnimator : NetworkBehaviour
    {
        internal struct AnimationState : INetworkSerializable
        {
            // Not to be serialized, used for processing the animation state
            internal bool HasBeenProcessed;

            internal int StateHash;
            internal float NormalizedTime;
            internal int Layer;
            internal float Weight;

            // Not to be serialized, used for processing the animation state
            internal bool TriggerProcessed;

            // For synchronizing transitions
            internal bool Transition;
            internal List<int> TriggerHashValues;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsWriter)
                {
                    var writer = serializer.GetFastBufferWriter();
                    BytePacker.WriteValuePacked(writer, StateHash);
                    BytePacker.WriteValuePacked(writer, NormalizedTime);
                    BytePacker.WriteValuePacked(writer, Layer);
                    BytePacker.WriteValuePacked(writer, Weight);
                    BytePacker.WriteValuePacked(writer, Transition);
                    if (Transition)
                    {
                        BytePacker.WriteValuePacked(writer, TriggerHashValues.Count);
                        foreach (var triggerHash in TriggerHashValues)
                        {
                            BytePacker.WriteValuePacked(writer, triggerHash);
                        }
                    }
                }
                else
                {
                    var reader = serializer.GetFastBufferReader();
                    ByteUnpacker.ReadValuePacked(reader, out StateHash);
                    ByteUnpacker.ReadValuePacked(reader, out NormalizedTime);
                    ByteUnpacker.ReadValuePacked(reader, out Layer);
                    ByteUnpacker.ReadValuePacked(reader, out Weight);
                    ByteUnpacker.ReadValuePacked(reader, out Transition);
                    if (Transition)
                    {
                        var count = 0;
                        ByteUnpacker.ReadValuePacked(reader, out count);
                        TriggerHashValues = new List<int>(count);
                        var triggerHash = 0;
                        for (int i = 0; i < count; i++)
                        {
                            ByteUnpacker.ReadValuePacked(reader, out triggerHash);
                            TriggerHashValues.Add(triggerHash);
                        }
                    }
                }
            }
        }

        internal struct AnimationMessage : INetworkSerializable
        {
            // Not to be serialized, used for processing the animation message
            internal bool HasBeenProcessed;

            // state hash per layer.  if non-zero, then Play() this animation, skipping transitions
            internal List<AnimationState> AnimationStates;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsReader)
                {
                    if (AnimationStates == null)
                    {
                        AnimationStates = new List<AnimationState>();
                    }
                    else if (AnimationStates.Count > 0)
                    {
                        AnimationStates.Clear();
                    }
                }
                var count = AnimationStates.Count;
                serializer.SerializeValue(ref count);

                var animationState = new AnimationState();
                for (int i = 0; i < count; i++)
                {
                    if (serializer.IsWriter)
                    {
                        animationState = AnimationStates[i];
                    }
                    serializer.SerializeNetworkSerializable(ref animationState);
                    if (serializer.IsReader)
                    {
                        AnimationStates.Add(animationState);
                    }
                }
            }
        }

        internal struct ParametersUpdateMessage : INetworkSerializable
        {
            internal byte[] Parameters;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Parameters);
            }
        }

        internal struct AnimationTriggerMessage : INetworkSerializable
        {
            internal int Hash;
            internal bool IsTriggerSet;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Hash);
                serializer.SerializeValue(ref IsTriggerSet);
            }
        }

        [SerializeField] private Animator m_Animator;

        public Animator Animator
        {
            get { return m_Animator; }
            set
            {
                m_Animator = value;
            }
        }

        internal bool IsServerAuthoritative()
        {
            return OnIsServerAuthoritative();
        }

        /// <summary>
        /// Override this method and return false to switch to owner authoritative mode
        /// </summary>
        protected virtual bool OnIsServerAuthoritative()
        {
            return true;
        }

        // Animators only support up to 32 parameters
        // TODO: Look into making this a range limited property
        private const int k_MaxAnimationParams = 32;

        private int[] m_TransitionHash;
        private int[] m_AnimationHash;
        private float[] m_LayerWeights;
        private static byte[] s_EmptyArray = new byte[] { };
        private NetworkAnimatorStateChangeHandler m_NetworkAnimatorStateChangeHandler;

        private unsafe struct AnimatorParamCache
        {
            internal int Hash;
            internal int Type;
            internal fixed byte Value[4]; // this is a max size of 4 bytes
        }

        // 128 bytes per Animator
        private FastBufferWriter m_ParameterWriter = new FastBufferWriter(k_MaxAnimationParams * sizeof(float), Allocator.Persistent);

        private NativeArray<AnimatorParamCache> m_CachedAnimatorParameters;

        // We cache these values because UnsafeUtility.EnumToInt uses direct IL that allows a non-boxing conversion
        private struct AnimationParamEnumWrapper
        {
            internal static readonly int AnimatorControllerParameterInt;
            internal static readonly int AnimatorControllerParameterFloat;
            internal static readonly int AnimatorControllerParameterBool;
            internal static readonly int AnimatorControllerParameterTriggerBool;

            static AnimationParamEnumWrapper()
            {
                AnimatorControllerParameterInt = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Int);
                AnimatorControllerParameterFloat = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Float);
                AnimatorControllerParameterBool = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Bool);
                AnimatorControllerParameterTriggerBool = UnsafeUtility.EnumToInt(AnimatorControllerParameterType.Trigger);
            }
        }

        private void Cleanup()
        {
            if (m_NetworkAnimatorStateChangeHandler != null)
            {
                m_NetworkAnimatorStateChangeHandler.DeregisterUpdate();
                m_NetworkAnimatorStateChangeHandler = null;
            }

            // The safest way to be assured that you get a NetworkManager instance when also
            // taking integration testing into consideration (multiple NetworkManager instances)
            var networkManager = HasNetworkObject ? NetworkManager : NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;

            if (m_CachedAnimatorParameters != null && m_CachedAnimatorParameters.IsCreated)
            {
                m_CachedAnimatorParameters.Dispose();
            }
            if (m_ParameterWriter.IsInitialized)
            {
                m_ParameterWriter.Dispose();
            }
        }

        public override void OnDestroy()
        {
            Cleanup();
            base.OnDestroy();
        }

        private List<int> m_ParametersToUpdate;
        private List<ulong> m_ClientSendList;
        private ClientRpcParams m_ClientRpcParams;
        private List<AnimationState> m_AnimationMessageStates;

        public override void OnNetworkSpawn()
        {
            int layers = m_Animator.layerCount;

            // Initializing the below arrays for everyone handles an issue
            // when running in owner authoritative mode and the owner changes.
            m_TransitionHash = new int[layers];
            m_AnimationHash = new int[layers];
            m_LayerWeights = new float[layers];

            if (IsServer)
            {
                m_ClientSendList = new List<ulong>(128);
                m_ClientRpcParams = new ClientRpcParams();
                m_ClientRpcParams.Send = new ClientRpcSendParams();
                m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            }

            // !! Note !!
            // Do not clear this list. We re-use the AnimationState entries
            // initialized below
            m_AnimationMessageStates = new List<AnimationState>();

            // Store off our current layer weights and create our animation
            // state entries per layer.
            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                m_AnimationMessageStates.Add(new AnimationState());
                float layerWeightNow = m_Animator.GetLayerWeight(layer);
                if (layerWeightNow != m_LayerWeights[layer])
                {
                    m_LayerWeights[layer] = layerWeightNow;
                }
            }

            // Build our reference parameter values to detect when they change
            var parameters = m_Animator.parameters;
            m_CachedAnimatorParameters = new NativeArray<AnimatorParamCache>(parameters.Length, Allocator.Persistent);
            m_ParametersToUpdate = new List<int>(parameters.Length);
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (m_Animator.IsParameterControlledByCurve(parameter.nameHash))
                {
                    // we are ignoring parameters that are controlled by animation curves - syncing the layer
                    //  states indirectly syncs the values that are driven by the animation curves
                    continue;
                }

                var cacheParam = new AnimatorParamCache
                {
                    Type = UnsafeUtility.EnumToInt(parameter.type),
                    Hash = parameter.nameHash
                };

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
                        default:
                            break;
                    }
                }

                m_CachedAnimatorParameters[i] = cacheParam;
            }
            m_NetworkAnimatorStateChangeHandler = new NetworkAnimatorStateChangeHandler(this);
        }

        public override void OnNetworkDespawn()
        {
            Cleanup();
        }

        /// <summary>
        /// Synchronizes newly joined players
        /// </summary>
        internal void ServerSynchronizeNewPlayer(ulong playerId)
        {
            m_ClientSendList.Clear();
            m_ClientSendList.Add(playerId);
            m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;

            // With synchronization we send all parameters
            m_ParametersToUpdate.Clear();
            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
                m_ParametersToUpdate.Add(i);
            }
            SendParametersUpdate(m_ClientRpcParams);

            var animationMessage = new AnimationMessage
            {
                // Assign the existing m_AnimationMessageStates list
                AnimationStates = m_AnimationMessageStates
            };

            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);
                var stateHash = st.fullPathHash;
                var normalizedTime = st.normalizedTime;

                var isInTransition = m_Animator.IsInTransition(layer);
                // NOTE:
                // When synchronizing, for now we will just complete the transition and
                // synchronize the player to the next state being transitioned into
                if (isInTransition)
                {
                    var tt = m_Animator.GetAnimatorTransitionInfo(layer);
                    var nextState = m_Animator.GetNextAnimatorStateInfo(layer);

                    if (nextState.length > 0)
                    {
                        var nextStateTotalSpeed = nextState.speed * nextState.speedMultiplier;
                        var nextStateAdjustedLength = nextState.length * nextStateTotalSpeed;
                        // TODO: We need to get the transition curve for the target state as well as some
                        // reasonable RTT estimate in order to get a more precise normalized synchronization time
                        var transitionTime = Mathf.Min(tt.duration, tt.duration * tt.normalizedTime) * 0.5f;
                        normalizedTime = Mathf.Min(1.0f, transitionTime > 0.0f ? transitionTime / nextStateAdjustedLength : 0.0f);
                    }
                    else
                    {
                        normalizedTime = 0.0f;
                    }
                    stateHash = nextState.fullPathHash;
                }
                var animMsg = m_AnimationMessageStates[layer];

                if (isInTransition)
                {
                    animMsg.TriggerHashValues = new List<int>();
                    foreach (var triggerEntry in m_CurrentTriggers)
                    {
                        if (triggerEntry.Value.ContainsKey(layer))
                        {
                            var triggerStateInfo = triggerEntry.Value[layer];
                            if (triggerStateInfo.CurrentState.fullPathHash == stateHash || triggerStateInfo.NextState.fullPathHash == stateHash)
                            {
                                if (!animMsg.TriggerHashValues.Contains(triggerEntry.Key))
                                {
                                    animMsg.TriggerHashValues.Add(triggerEntry.Key);
                                }
                            }
                        }
                    }
                }
                animMsg.Transition = isInTransition;
                animMsg.StateHash = stateHash;
                animMsg.NormalizedTime = normalizedTime;
                animMsg.Layer = layer;
                animMsg.Weight = m_LayerWeights[layer];

                m_AnimationMessageStates[layer] = animMsg;
            }
            if (animationMessage.AnimationStates.Count > 0)
            {
                // Server always send via client RPC
                SendAnimStateClientRpc(animationMessage, m_ClientRpcParams);
            }
        }

        /// <summary>
        /// Required for the server to synchronize newly joining players
        /// </summary>
        private void OnClientConnectedCallback(ulong playerId)
        {
            m_NetworkAnimatorStateChangeHandler.SynchronizeClient(playerId);
        }

        /// <summary>
        /// Checks for changes in both Animator parameters and state.
        /// </summary>
        internal void CheckForAnimatorChanges()
        {
            if (!IsOwner && !IsServerAuthoritative() || IsServerAuthoritative() && !IsServer)
            {
                return;
            }

            if (CheckParametersChanged())
            {
                SendParametersUpdate();
            }

            if (m_Animator.runtimeAnimatorController == null)
            {
                return;
            }

            int stateHash;
            float normalizedTime;

            var animationMessage = new AnimationMessage
            {
                // Assign the existing m_AnimationMessageStates list
                AnimationStates = m_AnimationMessageStates
            };

            // This sends updates only if a layer change or transition is happening
            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);
                var totalSpeed = st.speed * st.speedMultiplier;
                var adjustedNormalizedMaxTime = totalSpeed > 0.0f ? 1.0f / totalSpeed : 0.0f;

                if (!CheckAnimStateChanged(out stateHash, out normalizedTime, layer))
                {
                    continue;
                }
                var isInTransition = m_Animator.IsInTransition(layer);
                var animationState = new AnimationState
                {
                    Transition = isInTransition,
                    StateHash = stateHash,
                    NormalizedTime = normalizedTime,
                    Layer = layer,
                    Weight = m_LayerWeights[layer]
                };

                if (isInTransition)
                {
                    animationState.TriggerHashValues = new List<int>();
                    foreach (var triggerEntry in m_CurrentTriggers)
                    {
                        if (triggerEntry.Value.ContainsKey(layer))
                        {
                            var triggerStateInfo = triggerEntry.Value[layer];
                            if (triggerStateInfo.CurrentState.fullPathHash == stateHash || triggerStateInfo.NextState.fullPathHash == stateHash)
                            {
                                if (!animationState.TriggerHashValues.Contains(triggerEntry.Key))
                                {
                                    animationState.TriggerHashValues.Add(triggerEntry.Key);
                                }
                            }
                        }
                    }
                }

                animationMessage.AnimationStates.Add(animationState);
            }

            // Make sure there is something to send
            if (animationMessage.AnimationStates.Count > 0)
            {
                if (!IsServer && IsOwner)
                {
                    SendAnimStateServerRpc(animationMessage);
                }
                else
                {
                    SendAnimStateClientRpc(animationMessage);
                }
            }
        }

        private void SendParametersUpdate(ClientRpcParams clientRpcParams = default, bool sendDirect = false)
        {
            m_ParameterWriter.Seek(0);
            m_ParameterWriter.Truncate();

            WriteParameters(m_ParameterWriter, sendDirect);

            var parametersMessage = new ParametersUpdateMessage
            {
                Parameters = m_ParameterWriter.ToArray()
            };

            if (!IsServer)
            {
                SendParametersUpdateServerRpc(parametersMessage);
            }
            else
            {
                if (sendDirect)
                {
                    SendParametersUpdateClientRpc(parametersMessage, clientRpcParams);
                }
                else
                {
                    m_NetworkAnimatorStateChangeHandler.SendParameterUpdate(parametersMessage, clientRpcParams);
                }
            }
        }

        /// <summary>
        /// Helper function to get the cached value
        /// </summary>
        unsafe private T GetValue<T>(ref AnimatorParamCache animatorParamCache)
        {
            T currentValue;
            fixed (void* value = animatorParamCache.Value)
            {
                currentValue = UnsafeUtility.ReadArrayElement<T>(value, 0);
            }
            return currentValue;
        }

        /// <summary>
        /// Checks if any of the Animator's parameters have changed
        /// If so, it fills out m_ParametersToUpdate with the indices of the parameters
        /// that have changed.  Returns true if any parameters changed.
        /// </summary>
        unsafe private bool CheckParametersChanged()
        {
            m_ParametersToUpdate.Clear();
            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
                var hash = cacheValue.Hash;
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
                {
                    var valueInt = m_Animator.GetInteger(hash);
                    var currentValue = GetValue<int>(ref cacheValue);
                    if (currentValue != valueInt)
                    {
                        m_ParametersToUpdate.Add(i);
                        continue;
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    var valueBool = m_Animator.GetBool(hash);
                    var currentValue = GetValue<bool>(ref cacheValue);
                    if (currentValue != valueBool)
                    {
                        m_ParametersToUpdate.Add(i);
                        continue;
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    var valueFloat = m_Animator.GetFloat(hash);
                    var currentValue = GetValue<float>(ref cacheValue);
                    if (currentValue != valueFloat)
                    {
                        m_ParametersToUpdate.Add(i);
                        continue;
                    }
                }
            }
            return m_ParametersToUpdate.Count > 0;
        }

        /// <summary>
        /// Checks if any of the Animator's states have changed
        /// </summary>
        private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layer)
        {
            stateHash = 0;
            normalizedTime = 0;

            float layerWeightNow = m_Animator.GetLayerWeight(layer);
            if (layerWeightNow != m_LayerWeights[layer])
            {
                m_LayerWeights[layer] = layerWeightNow;
                return true;
            }

            if (m_Animator.IsInTransition(layer))
            {
                AnimatorTransitionInfo tt = m_Animator.GetAnimatorTransitionInfo(layer);
                if (tt.fullPathHash != m_TransitionHash[layer])
                {
                    // first time in this transition for this layer
                    m_TransitionHash[layer] = tt.fullPathHash;
                    m_AnimationHash[layer] = 0;
                    return true;
                }
            }
            else
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);
                if (st.fullPathHash != m_AnimationHash[layer])
                {
                    // first time in this animation state
                    if (m_AnimationHash[layer] != 0)
                    {
                        // came from another animation directly - from Play()
                        stateHash = st.fullPathHash;
                        normalizedTime = st.normalizedTime;
                    }
                    m_TransitionHash[layer] = 0;
                    m_AnimationHash[layer] = st.fullPathHash;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Writes all of the Animator's parameters
        /// This uses the m_ParametersToUpdate list to write out only
        /// the parameters that have changed
        /// </summary>
        private unsafe void WriteParameters(FastBufferWriter writer, bool sendCacheState)
        {
            // Write how many parameter entries we are going to write
            BytePacker.WriteValuePacked(writer, (uint)m_ParametersToUpdate.Count);
            foreach (var parameterIndex in m_ParametersToUpdate)
            {
                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), parameterIndex);
                var hash = cacheValue.Hash;
                BytePacker.WriteValuePacked(writer, (uint)parameterIndex);
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
                {
                    var valueInt = m_Animator.GetInteger(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueInt);
                        BytePacker.WriteValuePacked(writer, (uint)valueInt);
                    }
                }
                else // Note: Triggers are treated like boolean values
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    var valueBool = m_Animator.GetBool(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueBool);
                        BytePacker.WriteValuePacked(writer, valueBool);
                    }
                }
                else
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    var valueFloat = m_Animator.GetFloat(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueFloat);
                        BytePacker.WriteValuePacked(writer, valueFloat);
                    }
                }
            }
        }

        /// <summary>
        /// Reads all parameters that were updated and applies the values
        /// </summary>
        private unsafe void ReadParameters(FastBufferReader reader)
        {
            ByteUnpacker.ReadValuePacked(reader, out uint totalParametersToRead);
            var totalParametersRead = 0;

            while (totalParametersRead < totalParametersToRead)
            {
                ByteUnpacker.ReadValuePacked(reader, out uint parameterIndex);
                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), (int)parameterIndex);
                var hash = cacheValue.Hash;
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterInt)
                {
                    ByteUnpacker.ReadValuePacked(reader, out uint newValue);
                    m_Animator.SetInteger(hash, (int)newValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newValue);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    ByteUnpacker.ReadValuePacked(reader, out bool newBoolValue);
                    m_Animator.SetBool(hash, newBoolValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newBoolValue);
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    ByteUnpacker.ReadValuePacked(reader, out float newFloatValue);
                    m_Animator.SetFloat(hash, newFloatValue);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, newFloatValue);
                    }
                }
                totalParametersRead++;
            }
        }

        /// <summary>
        /// Applies the ParametersUpdateMessage state to the Animator
        /// </summary>
        internal unsafe void UpdateParameters(ParametersUpdateMessage parametersUpdate)
        {
            if (parametersUpdate.Parameters != null && parametersUpdate.Parameters.Length != 0)
            {
                // We use a fixed value here to avoid the copy of data from the byte buffer since we own the data
                fixed (byte* parameters = parametersUpdate.Parameters)
                {
                    var reader = new FastBufferReader(parameters, Allocator.None, parametersUpdate.Parameters.Length);
                    ReadParameters(reader);
                }
            }
        }

        /// <summary>
        /// Applies the AnimationState state to the Animator
        /// </summary>
        internal void UpdateAnimationState(AnimationState animationState)
        {
            if (animationState.StateHash == 0)
            {
                return;
            }

            var currentState = m_Animator.GetCurrentAnimatorStateInfo(animationState.Layer);

            // Currently:
            // In order to update a state our new state should not be the current relative state
            // or should be a trigger to transition being synchronized for a late joining player
            // TODO Future:
            // Possibly investigate synchronizing time based on tick frequency. This would require
            // a public property that allows users to define the frequency (in ticks), per instance,
            // that any currently playing animation will be time synchronized.
            if (currentState.fullPathHash != animationState.StateHash || animationState.Transition && animationState.TriggerProcessed)
            {
                m_Animator.Play(animationState.StateHash, animationState.Layer, animationState.NormalizedTime);
            }

            m_Animator.SetLayerWeight(animationState.Layer, animationState.Weight);
        }

        /// <summary>
        /// Server-side animator parameter update request
        /// The server sets its local parameters and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private unsafe void SendParametersUpdateServerRpc(ParametersUpdateMessage parametersUpdate, ServerRpcParams serverRpcParams = default)
        {
            if (IsServerAuthoritative())
            {
                m_NetworkAnimatorStateChangeHandler.SendParameterUpdate(parametersUpdate);
            }
            else
            {
                if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                {
                    return;
                }
                UpdateParameters(parametersUpdate);
                if (NetworkManager.ConnectedClientsIds.Count > (IsHost ? 2 : 1))
                {
                    m_ClientSendList.Clear();
                    m_ClientSendList.AddRange(NetworkManager.ConnectedClientsIds);
                    m_ClientSendList.Remove(serverRpcParams.Receive.SenderClientId);
                    m_ClientSendList.Remove(NetworkManager.ServerClientId);
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                    m_NetworkAnimatorStateChangeHandler.SendParameterUpdate(parametersUpdate, m_ClientRpcParams);
                }
            }
        }

        /// <summary>
        /// Updates the client's animator's parameters
        /// </summary>
        [ClientRpc]
        internal unsafe void SendParametersUpdateClientRpc(ParametersUpdateMessage parametersUpdate, ClientRpcParams clientRpcParams = default)
        {
            var isServerAuthoritative = IsServerAuthoritative();
            if (!isServerAuthoritative && !IsOwner || isServerAuthoritative)
            {
                m_NetworkAnimatorStateChangeHandler.ProcessParameterUpdate(parametersUpdate);
            }
        }

        /// <summary>
        /// Server-side animation state update request
        /// The server sets its local state and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private unsafe void SendAnimStateServerRpc(AnimationMessage animSnapshot, ServerRpcParams serverRpcParams = default)
        {
            if (IsServerAuthoritative())
            {
                m_NetworkAnimatorStateChangeHandler.SendAnimationUpdate(animSnapshot);
            }
            else
            {
                if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                {
                    return;
                }
                m_NetworkAnimatorStateChangeHandler.AddAnimationMessageToProcessQueue(animSnapshot);
                if (NetworkManager.ConnectedClientsIds.Count > (IsHost ? 2 : 1))
                {
                    m_ClientSendList.Clear();
                    m_ClientSendList.AddRange(NetworkManager.ConnectedClientsIds);
                    m_ClientSendList.Remove(serverRpcParams.Receive.SenderClientId);
                    m_ClientSendList.Remove(NetworkManager.ServerClientId);
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                    m_NetworkAnimatorStateChangeHandler.SendAnimationUpdate(animSnapshot, m_ClientRpcParams);
                }
            }
        }

        /// <summary>
        /// Internally-called RPC client receiving function to update some animation state on a client
        /// </summary>
        [ClientRpc]
        private unsafe void SendAnimStateClientRpc(AnimationMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (IsServer)
            {
                return;
            }
            var isServerAuthoritative = IsServerAuthoritative();
            if (!isServerAuthoritative && !IsOwner || isServerAuthoritative)
            {
                m_NetworkAnimatorStateChangeHandler.AddAnimationMessageToProcessQueue(animSnapshot);
            }
        }

        /// <summary>
        /// Server-side trigger state update request
        /// The server sets its local state and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        internal void SendAnimTriggerServerRpc(AnimationTriggerMessage animationTriggerMessage, ServerRpcParams serverRpcParams = default)
        {
            if (IsServerAuthoritative())
            {
                m_NetworkAnimatorStateChangeHandler.QueueTriggerUpdateToClient(animationTriggerMessage);
            }
            else
            {
                if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                {
                    return;
                }
                //  trigger the animation locally on the server...
                InternalSetTrigger(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);

                if (NetworkManager.ConnectedClientsIds.Count > (IsHost ? 2 : 1))
                {
                    m_ClientSendList.Clear();
                    m_ClientSendList.AddRange(NetworkManager.ConnectedClientsIds);
                    m_ClientSendList.Remove(serverRpcParams.Receive.SenderClientId);
                    m_ClientSendList.Remove(NetworkManager.ServerClientId);
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                    m_NetworkAnimatorStateChangeHandler.QueueTriggerUpdateToClient(animationTriggerMessage, m_ClientRpcParams);
                }
            }
        }

        /// <summary>
        /// For late joining players:
        /// Tracks the last trigger to better synchronize late joining clients.
        /// </summary>
        /// <remarks>
        /// This is a semi-work-around because we don't currently have a way to link triggers to transitions.
        /// Since there could be, theoretically, multiple triggers active that each have their own layer
        /// relative state transition, this "semi-work-around" will only handle synchronizing the very last
        /// trigger and more complex setups could very well not get all triggers synchronized.
        /// TODO: Determine (possibly editor side) a way to link transition hashes with trigger hashes.
        /// </remarks>
        private int m_LastTriggerHash;


        internal struct TriggerStateInfo
        {
            public bool IsDirty;
            public AnimatorStateInfo CurrentState;
            public AnimatorStateInfo NextState;
        }

        /// <summary>
        /// Links triggers with layer state transitions.
        /// This only tracks the initial transition from one state to the next when a trigger fires
        /// ** This does not track the transitions back to the original state **
        /// </summary>
        /// <remarks>
        ///[TriggerNameHash][Layer Index][TriggerStateInfo]
        /// </remarks>
        private Dictionary<int, Dictionary<int, TriggerStateInfo>> m_CurrentTriggers = new Dictionary<int, Dictionary<int, TriggerStateInfo>>();

        /// <summary>
        /// Upon authority changing a trigger, this will link the trigger with the next state which typically is the transition
        /// TODO: For next minor revision update, look into building this table on the editor side.
        /// </summary>
        internal void UpdatePendingTriggerStates()
        {
            foreach (var keyPair in m_CurrentTriggers)
            {
                var hash = keyPair.Key;

                // Check for the layers with states in transition
                for (int i = 0; i < m_Animator.layerCount; i++)
                {
                    // Skip any triggers that have no layer indices set yet.
                    if (!keyPair.Value.ContainsKey(i) || !keyPair.Value[i].IsDirty)
                    {
                        continue;
                    }

                    var triggerStateInfo = keyPair.Value[i];
                    if (m_Animator.IsInTransition(i))
                    {
                        triggerStateInfo.NextState = m_Animator.GetNextAnimatorStateInfo(i);
                        Debug.Log($"[{hash}][TriggerState][ADD] Current ({triggerStateInfo.CurrentState.fullPathHash}) | Next ({triggerStateInfo.NextState.fullPathHash})");
                    }
                    triggerStateInfo.IsDirty = false;
                    keyPair.Value[i] = triggerStateInfo;
                }
            }
        }

        /// <summary>
        /// See above <see cref="m_LastTriggerHash"/>
        /// </summary>
        private void InternalSetTrigger(int hash, bool isSet = true)
        {
            m_LastTriggerHash = hash;

            // We re-use what we allocate
            if (!m_CurrentTriggers.ContainsKey(hash))
            {
                m_CurrentTriggers.Add(hash, new Dictionary<int, TriggerStateInfo>());
            }

            // Get the current states for all layers and mark them dirty for processing
            for (int i = 0; i < m_Animator.layerCount; i++)
            {
                var triggerStates = m_CurrentTriggers[hash];
                var currentState = m_Animator.GetCurrentAnimatorStateInfo(i);

                // We re-use what we create for the duration of this NetworkAnimator instance
                if (!triggerStates.ContainsKey(i))
                {
                    var triggerStateInfo = new TriggerStateInfo()
                    {
                        IsDirty = true,
                        CurrentState = currentState,
                    };
                    triggerStates.Add(i, triggerStateInfo);
                }
                else
                {
                    var triggerStateEntry = triggerStates[i];
                    triggerStateEntry.IsDirty = true;
                    triggerStateEntry.CurrentState = currentState;
                    triggerStates[i] = triggerStateEntry;
                }
            }

            // Set the trigger value
            m_Animator.SetBool(hash, isSet);
        }

        /// <summary>
        /// Internally-called RPC client receiving function to update a trigger when the server wants to forward
        ///   a trigger for a client to play / reset
        /// </summary>
        /// <param name="animationTriggerMessage">the payload containing the trigger data to apply</param>
        /// <param name="clientRpcParams">unused</param>
        [ClientRpc]
        internal void SendAnimTriggerClientRpc(AnimationTriggerMessage animationTriggerMessage, ClientRpcParams clientRpcParams = default)
        {
            InternalSetTrigger(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);
        }

        /// <summary>
        /// Sets the trigger for the associated animation
        /// </summary>
        /// <param name="triggerName">The string name of the trigger to activate</param>
        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }

        /// <inheritdoc cref="SetTrigger(string)" />
        /// <param name="hash">The hash for the trigger to activate</param>
        /// <param name="setTrigger">sets (true) or resets (false) the trigger. The default is to set it (true).</param>
        public void SetTrigger(int hash, bool setTrigger = true)
        {
            // MTT-3564:
            // After fixing the issue with trigger controlled Transitions being synchronized twice,
            // it exposed additional issues with this logic.  Now, either the owner or the server can
            // update triggers. Since server-side RPCs are immediately invoked, for a host a trigger
            // will happen when SendAnimTriggerClientRpc is called.  For a client owner, we call the
            // SendAnimTriggerServerRpc and then trigger locally when running in owner authority mode.
            if (IsOwner || IsServer)
            {
                var animTriggerMessage = new AnimationTriggerMessage() { Hash = hash, IsTriggerSet = setTrigger };
                if (IsServer)
                {
                    /// <see cref="UpdatePendingTriggerStates"/> as to why we queue
                    m_NetworkAnimatorStateChangeHandler.QueueTriggerUpdateToClient(animTriggerMessage);
                    if (!IsHost)
                    {
                        InternalSetTrigger(hash);
                    }
                }
                else
                {
                    /// <see cref="UpdatePendingTriggerStates"/> as to why we queue
                    m_NetworkAnimatorStateChangeHandler.QueueTriggerUpdateToServer(animTriggerMessage);
                    if (!IsServerAuthoritative())
                    {
                        InternalSetTrigger(hash);
                    }
                }
            }
        }

        /// <summary>
        /// Resets the trigger for the associated animation.  See <see cref="SetTrigger(string)">SetTrigger</see> for more on how triggers are special
        /// </summary>
        /// <param name="triggerName">The string name of the trigger to reset</param>
        public void ResetTrigger(string triggerName)
        {
            ResetTrigger(Animator.StringToHash(triggerName));
        }

        /// <inheritdoc cref="ResetTrigger(string)" path="summary" />
        /// <param name="hash">The hash for the trigger to activate</param>
        public void ResetTrigger(int hash)
        {
            SetTrigger(hash, false);
        }
    }
}
#endif // COM_UNITY_MODULES_ANIMATION
