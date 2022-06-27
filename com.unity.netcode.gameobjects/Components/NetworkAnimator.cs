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
                m_NetworkAnimator.SendAnimTriggerClientRpc(sendEntry.AnimationTriggerMessage, sendEntry.ClientRpcParams);
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
            public ClientRpcParams ClientRpcParams;
            public NetworkAnimator.AnimationTriggerMessage AnimationTriggerMessage;
        }

        private List<TriggerUpdate> m_SendTriggerUpdates = new List<TriggerUpdate>();

        /// <summary>
        /// Invoked when a server needs to forward an update to a Trigger state
        /// </summary>
        internal void SendTriggerUpdate(NetworkAnimator.AnimationTriggerMessage animationTriggerMessage, ClientRpcParams clientRpcParams = default)
        {
            m_SendTriggerUpdates.Add(new TriggerUpdate() { ClientRpcParams = clientRpcParams, AnimationTriggerMessage = animationTriggerMessage });
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
        internal struct AnimationMessage : INetworkSerializable
        {
            // state hash per layer.  if non-zero, then Play() this animation, skipping transitions
            internal int StateHash;
            internal float NormalizedTime;
            internal int Layer;
            internal float Weight;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref StateHash);
                serializer.SerializeValue(ref NormalizedTime);
                serializer.SerializeValue(ref Layer);
                serializer.SerializeValue(ref Weight);
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

        // Animators only support up to 32 params
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

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            }

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

        public override void OnNetworkSpawn()
        {
            if (IsOwner || IsServer)
            {
                int layers = m_Animator.layerCount;
                m_TransitionHash = new int[layers];
                m_AnimationHash = new int[layers];
                m_LayerWeights = new float[layers];

                if (IsServer)
                {
                    NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                }

                // Store off our current layer weights
                for (int layer = 0; layer < m_Animator.layerCount; layer++)
                {
                    float layerWeightNow = m_Animator.GetLayerWeight(layer);
                    if (layerWeightNow != m_LayerWeights[layer])
                    {
                        m_LayerWeights[layer] = layerWeightNow;
                    }
                }

                if (IsServer)
                {
                    m_ClientSendList = new List<ulong>(128);
                    m_ClientRpcParams = new ClientRpcParams();
                    m_ClientRpcParams.Send = new ClientRpcSendParams();
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                }
            }

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
            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);

                var stateHash = st.fullPathHash;
                var normalizedTime = st.normalizedTime;
                var totalSpeed = st.speed * st.speedMultiplier;
                var adjustedNormalizedMaxTime = totalSpeed > 0.0f ? 1.0f / totalSpeed : 0.0f;
                // NOTE:
                // When synchronizing, for now we will just complete the transition and
                // synchronize the player to the next state being transitioned into
                if (m_Animator.IsInTransition(layer))
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
                else
                if (st.normalizedTime >= adjustedNormalizedMaxTime)
                {
                    continue;
                }

                var animMsg = new AnimationMessage
                {
                    StateHash = stateHash,
                    NormalizedTime = normalizedTime,
                    Layer = layer,
                    Weight = m_LayerWeights[layer]
                };
                // Server always send via client RPC
                SendAnimStateClientRpc(animMsg, m_ClientRpcParams);
            }
        }

        private void OnClientConnectedCallback(ulong playerId)
        {
            m_NetworkAnimatorStateChangeHandler.SynchronizeClient(playerId);
        }

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

            // This sends updates only if a layer change or transition is happening
            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);
                var totalSpeed = st.speed * st.speedMultiplier;
                var adjustedNormalizedMaxTime = totalSpeed > 0.0f ? 1.0f / totalSpeed : 0.0f;

                // determine if we have reached the end of our state time, if so we can skip
                if (st.normalizedTime >= adjustedNormalizedMaxTime)
                {
                    continue;
                }

                if (!CheckAnimStateChanged(out stateHash, out normalizedTime, layer))
                {
                    continue;
                }

                var animMsg = new AnimationMessage
                {
                    StateHash = stateHash,
                    NormalizedTime = normalizedTime,
                    Layer = layer,
                    Weight = m_LayerWeights[layer]
                };

                if (!IsServer && IsOwner)
                {
                    SendAnimStateServerRpc(animMsg);
                }
                else
                {
                    SendAnimStateClientRpc(animMsg);
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
        private unsafe bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layer)
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
        /// Applies the AnimationMessage state to the Animator
        /// </summary>
        private unsafe void UpdateAnimationState(AnimationMessage animationState)
        {
            if (animationState.StateHash != 0)
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
                if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
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
                UpdateAnimationState(animSnapshot);
                if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
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
            var isServerAuthoritative = IsServerAuthoritative();
            if (!isServerAuthoritative && !IsOwner || isServerAuthoritative)
            {
                UpdateAnimationState(animSnapshot);
            }
        }

        /// <summary>
        /// Server-side trigger state update request
        /// The server sets its local state and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private void SendAnimTriggerServerRpc(AnimationTriggerMessage animationTriggerMessage, ServerRpcParams serverRpcParams = default)
        {
            if (IsServerAuthoritative())
            {
                m_NetworkAnimatorStateChangeHandler.SendTriggerUpdate(animationTriggerMessage);
            }
            else
            {
                if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                {
                    return;
                }
                //  trigger the animation locally on the server...
                m_Animator.SetBool(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);

                if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
                {
                    m_ClientSendList.Clear();
                    m_ClientSendList.AddRange(NetworkManager.ConnectedClientsIds);
                    m_ClientSendList.Remove(serverRpcParams.Receive.SenderClientId);
                    m_ClientSendList.Remove(NetworkManager.ServerClientId);
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientSendList;
                    m_NetworkAnimatorStateChangeHandler.SendTriggerUpdate(animationTriggerMessage, m_ClientRpcParams);
                }
            }

        }

        /// <summary>
        /// Internally-called RPC client receiving function to update a trigger when the server wants to forward
        ///   a trigger for a client to play / reset
        /// </summary>
        /// <param name="animSnapshot">the payload containing the trigger data to apply</param>
        /// <param name="clientRpcParams">unused</param>
        [ClientRpc]
        internal void SendAnimTriggerClientRpc(AnimationTriggerMessage animationTriggerMessage, ClientRpcParams clientRpcParams = default)
        {
            var isServerAuthoritative = IsServerAuthoritative();
            if (!isServerAuthoritative && !IsOwner || isServerAuthoritative)
            {
                m_Animator.SetBool(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);
            }
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
            var isServerAuthoritative = IsServerAuthoritative();
            if (IsOwner && !isServerAuthoritative || IsServer && isServerAuthoritative)
            {
                var animTriggerMessage = new AnimationTriggerMessage() { Hash = hash, IsTriggerSet = setTrigger };
                if (IsServer)
                {
                    SendAnimTriggerClientRpc(animTriggerMessage);
                }
                else
                {
                    SendAnimTriggerServerRpc(animTriggerMessage);
                }
                //  trigger the animation locally on the server...
                m_Animator.SetBool(hash, setTrigger);
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
