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

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.PreUpdate:
                    {
                        if (m_NetworkAnimator.NetworkManager.IsServer)
                        {
                            // Flush any pending messages
                            FlushMessages();
                        }

                        // Apply any parameter updates
                        foreach (var parameterUpdate in m_ProcessParameterUpdates)
                        {
                            m_NetworkAnimator.UpdateParameters(parameterUpdate);
                        }
                        m_ProcessParameterUpdates.Clear();

                        // Owners check for changes
                        if (m_NetworkAnimator.IsOwner)
                        {
                            m_NetworkAnimator.CheckForAnimatorChanges();
                        }

                        foreach (var clientId in m_ClientsToSynchronize)
                        {
                            m_NetworkAnimator.ServerUpdateNewPlayer(clientId);
                        }
                        m_ClientsToSynchronize.Clear();
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
            //NetworkUpdateLoop.UnregisterAllNetworkUpdates(this);
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
            //NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.FixedUpdate);
            //if (m_NetworkAnimator.NetworkManager.IsServer)
            //{
            //    //NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
            //    //NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
            //    NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.PreLateUpdate);
            //}
        }

        internal NetworkAnimatorStateChangeHandler(NetworkAnimator networkAnimator)
        {
            m_NetworkAnimator = networkAnimator;
            NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
            //NetworkUpdateLoop.RegisterAllNetworkUpdates(this);
            //NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.FixedUpdate);
            //if (m_NetworkAnimator.NetworkManager.IsServer)
            //{
            //    ////NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreUpdate);
            //    ////NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
            //    NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.PreLateUpdate);
            //}
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

        [HideInInspector]
        [SerializeField]
        private AnimationClip[] m_AnimationClips;
        private Dictionary<int, AnimationClip> m_AnimationClipTable = new Dictionary<int, AnimationClip>();
#if UNITY_EDITOR
        private void OnValidate()
        {
            var animator = GetComponent<Animator>();
            m_AnimationClips = UnityEditor.AnimationUtility.GetAnimationClips(gameObject);
        }
#endif

        private void CleanUp()
        {
            if (m_NetworkAnimatorStateChangeHandler != null)
            {
                m_NetworkAnimatorStateChangeHandler.DeregisterUpdate();
                m_NetworkAnimatorStateChangeHandler = null;
            }

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= Server_OnClientConnectedCallback;
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
            CleanUp();
            base.OnDestroy();
        }

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
                    NetworkManager.OnClientConnectedCallback += Server_OnClientConnectedCallback;
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
            }

            m_AnimationClipTable.Clear();
            foreach (var clip in m_AnimationClips)
            {
                m_AnimationClipTable.Add(Animator.StringToHash(clip.name), clip);
            }

            var parameters = m_Animator.parameters;
            m_CachedAnimatorParameters = new NativeArray<AnimatorParamCache>(parameters.Length, Allocator.Persistent);

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
                        case AnimatorControllerParameterType.Trigger:
                            var valueTriggerBool = m_Animator.GetBool(cacheParam.Hash);
                            UnsafeUtility.WriteArrayElement(cacheParam.Value, 0, valueTriggerBool);
                            break;
                        default:
                            break;
                    }
                }

                m_CachedAnimatorParameters[i] = cacheParam;
            }
            m_NetworkAnimatorStateChangeHandler = new NetworkAnimatorStateChangeHandler(this);
        }

        internal void CheckForAnimatorChanges()
        {
            if (!IsOwner)
            {
                return;
            }
            // TODO: This could return (or build) a list of parameter hash values
            // that could be used to reduce the message size down to just the
            // parameters that changed as opposed to sending all of them
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
            // TODO: This needs to be reviewed further to determine if this provides
            // all information required.
            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);

                if (!m_Animator.HasState(layer, st.shortNameHash))
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

        public override void OnNetworkDespawn()
        {
            CleanUp();
        }

        private void Server_OnClientConnectedCallback(ulong playerId)
        {
            m_NetworkAnimatorStateChangeHandler.SynchronizeClient(playerId);
        }

        internal void ServerUpdateNewPlayer(ulong playerId)
        {
            var clientRpcParams = new ClientRpcParams();
            clientRpcParams.Send = new ClientRpcSendParams();
            clientRpcParams.Send.TargetClientIds = new List<ulong>() { playerId };

            SendParametersUpdate(clientRpcParams);

            for (int layer = 0; layer < m_Animator.layerCount; layer++)
            {
                AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layer);

                if (st.normalizedTime >= 1.0f)
                {
                    continue;
                }

                var animMsg = new AnimationMessage
                {
                    StateHash = st.fullPathHash,
                    NormalizedTime = st.normalizedTime,
                    Layer = layer,
                    Weight = m_LayerWeights[layer]
                };
                // Server always send via client RPC
                SendAnimStateClientRpc(animMsg, clientRpcParams);
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
                VerboseDebug($"[SendParametersUpdate-Owner] Update request sent to server.");
                SendParametersUpdateServerRpc(parametersMessage);
            }
            else
            {
                VerboseDebug($"[SendParametersUpdate-Server] Updating clients.");
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
        /// </summary>
        unsafe private bool CheckParametersChanged()
        {
            bool shouldUpdate = false;
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
                        VerboseDebug($"[{name}][INT] {currentValue} != {valueInt}");
                        shouldUpdate = true;
                        break;
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    var valueBool = m_Animator.GetBool(hash);
                    var currentValue = GetValue<bool>(ref cacheValue);
                    if (currentValue != valueBool)
                    {
                        VerboseDebug($"[{name}][BOOL] {currentValue} != {valueBool} -- Updating");
                        shouldUpdate = true;
                        break;
                    }
                }
                else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    var valueFloat = m_Animator.GetFloat(hash);
                    var currentValue = GetValue<float>(ref cacheValue);
                    if (currentValue != valueFloat)
                    {
                        VerboseDebug($"[{name}][FLOAT] {currentValue} != {valueFloat} -- Updating");
                        shouldUpdate = true;
                        break;
                    }
                }
                //else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterTriggerBool)
                //{
                //    var valueTriggerBool = m_Animator.GetBool(hash);
                //    var currentValue = GetValue<bool>(ref cacheValue);
                //    if (currentValue != valueTriggerBool && valueTriggerBool)
                //    {
                //        VerboseDebug($"[{name}][TRIGGER] {currentValue} != {valueTriggerBool} -- Updating");
                //        shouldUpdate = true;
                //        break;
                //    }
                //}
            }
            return shouldUpdate;
        }

        /// <summary>
        /// Checks if any of the Animator's states have changed
        /// </summary>
        unsafe private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layer)
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
        /// </summary>
        private unsafe void WriteParameters(FastBufferWriter writer, bool sendCacheState)
        {
            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
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
                else // Note: Triggers are treated like boolean values
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterBool)
                {
                    var valueBool = m_Animator.GetBool(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueBool);
                        writer.WriteValueSafe(valueBool);
                    }
                }
                else
                if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterFloat)
                {
                    var valueFloat = m_Animator.GetFloat(hash);
                    fixed (void* value = cacheValue.Value)
                    {
                        UnsafeUtility.WriteArrayElement(value, 0, valueFloat);
                        writer.WriteValueSafe(valueFloat);
                    }
                }
                //else
                //if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterTriggerBool)
                //{
                //    var valueTriggerBool = m_Animator.GetBool(hash);
                //    var currentValue = GetValue<bool>(ref cacheValue);
                //    if (valueTriggerBool)
                //    {
                //        VerboseDebug($"[{name}][TRIGGER] {currentValue} != {valueTriggerBool} -- Writing");
                //    }
                //    fixed (void* value = cacheValue.Value)
                //    {
                //        UnsafeUtility.WriteArrayElement(value, 0, valueTriggerBool);
                //        writer.WriteValueSafe(valueTriggerBool);
                //    }
                //    break;
                //}
            }
        }

        /// <summary>
        /// Applies all of the Animator's parameters
        /// </summary>
        private unsafe void ReadParameters(FastBufferReader reader)
        {
            for (int i = 0; i < m_CachedAnimatorParameters.Length; i++)
            {
                ref var cacheValue = ref UnsafeUtility.ArrayElementAsRef<AnimatorParamCache>(m_CachedAnimatorParameters.GetUnsafePtr(), i);
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
                //else if (cacheValue.Type == AnimationParamEnumWrapper.AnimatorControllerParameterTriggerBool)
                //{
                //    reader.ReadValueSafe(out bool valueTriggerBool);
                //    var currentValue = GetValue<bool>(ref cacheValue);
                //    var animatorValue = m_Animator.GetBool(cacheValue.Hash);
                //    if (valueTriggerBool && !animatorValue)
                //    {
                //        VerboseDebug($"[{NetworkManager.name}][TRIGGER][{cacheValue.Hash}] Current: {m_Animator.GetBool(hash)} | Current Cached: {currentValue} | New Value: {valueTriggerBool}");
                //        m_Animator.SetBool(hash, valueTriggerBool);
                //    }
                //    if (currentValue != valueTriggerBool)
                //    {
                //        fixed (void* value = cacheValue.Value)
                //        {
                //            UnsafeUtility.WriteArrayElement(value, 0, valueTriggerBool);
                //        }
                //    }
                //}
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
        /// Sever-side animator parameter update request
        /// The server sets its local parameters and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private unsafe void SendParametersUpdateServerRpc(ParametersUpdateMessage parametersUpdate, ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
            {
                VerboseDebug($"[{nameof(SendParametersUpdateServerRpc)}] Received update from non-owner! (Ignoring)");
                return;
            }
            VerboseDebug($"[{nameof(SendParametersUpdateServerRpc)}] Received update! (Processing)");
            UpdateParameters(parametersUpdate);
            if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
            {
                var clientRpcParams = new ClientRpcParams();
                clientRpcParams.Send = new ClientRpcSendParams();
                var clientIds = new List<ulong>(NetworkManager.ConnectedClientsIds);
                clientIds.Remove(serverRpcParams.Receive.SenderClientId);
                clientIds.Remove(NetworkManager.ServerClientId);
                clientRpcParams.Send.TargetClientIds = clientIds;
                m_NetworkAnimatorStateChangeHandler.SendParameterUpdate(parametersUpdate, clientRpcParams);
            }
        }

        /// <summary>
        /// Updates the client's animator's parameters
        /// </summary>
        [ClientRpc]
        internal unsafe void SendParametersUpdateClientRpc(ParametersUpdateMessage parametersUpdate, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
            {
                m_NetworkAnimatorStateChangeHandler.ProcessParameterUpdate(parametersUpdate);
                //UpdateParameters(parametersUpdate);
            }
        }

        /// <summary>
        /// Sever-side animation state update request
        /// The server sets its local state and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private unsafe void SendAnimStateServerRpc(AnimationMessage animSnapshot, ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
            {
                VerboseDebug($"[{nameof(SendAnimStateServerRpc)}] Received update from non-owner! (Ignoring)");
                return;
            }
            VerboseDebug($"[{nameof(SendAnimStateServerRpc)}] Received update! (Processing)");

            UpdateAnimationState(animSnapshot);
            if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
            {
                var clientRpcParams = new ClientRpcParams();
                clientRpcParams.Send = new ClientRpcSendParams();
                var clientIds = new List<ulong>(NetworkManager.ConnectedClientsIds);
                clientIds.Remove(serverRpcParams.Receive.SenderClientId);
                clientIds.Remove(NetworkManager.ServerClientId);
                clientRpcParams.Send.TargetClientIds = clientIds;
                m_NetworkAnimatorStateChangeHandler.SendAnimationUpdate(animSnapshot, clientRpcParams);
            }
        }

        /// <summary>
        /// Internally-called RPC client receiving function to update some animation state on a client
        /// </summary>
        [ClientRpc]
        private unsafe void SendAnimStateClientRpc(AnimationMessage animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            UpdateAnimationState(animSnapshot);
        }

        /// <summary>
        /// Sever-side trigger state update request
        /// The server sets its local state and then forwards the message to the remaining clients
        /// </summary>
        [ServerRpc]
        private void SendAnimTriggerServerRpc(AnimationTriggerMessage animationTriggerMessage, ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
            {
                VerboseDebug($"[{nameof(SendAnimTriggerServerRpc)}] Received update from non-owner! (Ignoring)");
                return;
            }
            VerboseDebug($"[{nameof(SendAnimTriggerServerRpc)}] Received update! (Processing)");

            //  trigger the animation locally on the server...
            m_Animator.SetBool(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);

            if (NetworkManager.ConnectedClientsIds.Count - 2 > 0)
            {
                var clientRpcParams = new ClientRpcParams();
                clientRpcParams.Send = new ClientRpcSendParams();
                var clientIds = new List<ulong>(NetworkManager.ConnectedClientsIds);
                clientIds.Remove(serverRpcParams.Receive.SenderClientId);
                clientIds.Remove(NetworkManager.ServerClientId);
                clientRpcParams.Send.TargetClientIds = clientIds;
                m_NetworkAnimatorStateChangeHandler.SendTriggerUpdate(animationTriggerMessage);
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
            if (!IsOwner)
            {
                VerboseDebug($"[{nameof(SendAnimTriggerClientRpc)}] {NetworkManager.name} received trigger value ({animationTriggerMessage.IsTriggerSet}).");
                m_Animator.SetBool(animationTriggerMessage.Hash, animationTriggerMessage.IsTriggerSet);
            }

            //if (animSnapshot.Reset)
            //{
            //    m_Animator.ResetTrigger(animSnapshot.Hash);
            //}
            //else
            //{
            //    m_Animator.SetTrigger(animSnapshot.Hash);
            //}
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
        /// <param name="reset">If true, resets the trigger</param>
        public void SetTrigger(int hash, bool setTrigger = true)
        {
            if (IsOwner)
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
                //if (reset)
                //{
                //    m_Animator.ResetTrigger(hash);
                //}
                //else
                //{
                //    m_Animator.SetTrigger(hash);
                //}
                //if (IsServer)
                //{
                //
                //}
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

        private enum LogType
        {
            Normal,
            Warning,
            Error
        }

        private void VerboseDebug(string msg, LogType logType = LogType.Normal)
        {
            if (IsSpawned)
            {
                if (NetworkManager.LogLevel == LogLevel.Normal)
                {
                    switch (logType)
                    {
                        case LogType.Normal:
                            {
                                NetworkLog.LogInfo(msg);
                                break;
                            }
                        case LogType.Warning:
                            {
                                NetworkLog.LogWarning(msg);
                                break;
                            }
                        case LogType.Error:
                            {
                                NetworkLog.LogError(msg);
                                break;
                            }
                    }
                }
            }
        }
    }
}
#endif // COM_UNITY_MODULES_ANIMATION
