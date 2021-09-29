using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A prototype component for syncing Mecanim Animator state in a server-driven manner
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkAnimator))]
    public class NetworkAnimator : NetworkBehaviour
    {
        private class AnimatorSnapshot : INetworkSerializable, IDisposable
        {
            public KeyValuePair<int, bool>[] BoolParamArray;
            public KeyValuePair<int, float>[] FloatParamArray;
            public KeyValuePair<int, int>[] IntParamArray;

            public HashSet<int> TriggerParameters;
            public LayerState[] LayerStates;
            private const int k_InvalidKey = -1;

            public AnimatorSnapshot(int boolCount, int floatCount, int intCount, int triggerCount, int layerStatesCount)
            {
                BoolParamArray = new KeyValuePair<int, bool>[boolCount];
                IntParamArray = new KeyValuePair<int, int>[intCount];
                FloatParamArray = new KeyValuePair<int, float>[floatCount];
                TriggerParameters = new HashSet<int>(triggerCount);
                LayerStates = new LayerState[layerStatesCount];

                SetBuffersToDefaultValues();
            }

            public AnimatorSnapshot()
            {
                BoolParamArray = new KeyValuePair<int, bool>[0];
                IntParamArray = new KeyValuePair<int, int>[0];
                FloatParamArray = new KeyValuePair<int, float>[0];
                TriggerParameters = new HashSet<int>(0);
                LayerStates = new LayerState[0];
                SetBuffersToDefaultValues();
            }

            private void SetBuffersToDefaultValues()
            {
                for (int i = 0; i < BoolParamArray.Length; i++)
                {
                    BoolParamArray[i] = new KeyValuePair<int, bool>(k_InvalidKey, false);
                }

                for (int i = 0; i < FloatParamArray.Length; i++)
                {
                    FloatParamArray[i] = new KeyValuePair<int, float>(k_InvalidKey, 0);
                }

                for (int i = 0; i < IntParamArray.Length; i++)
                {
                    IntParamArray[i] = new KeyValuePair<int, int>(k_InvalidKey, 0);
                }

                TriggerParameters.Clear();

                Array.Clear(LayerStates, 0, LayerStates.Length);
            }

            public bool SetInt(int key, int value)
            {
                bool setOrUpdatedValue = false;

                int existingKvIndex = Array.FindIndex(IntParamArray, pair => pair.Key == key);

                if (existingKvIndex == -1)
                {
                    for (int i = 0; i < IntParamArray.Length; i++)
                    {
                        var kv = IntParamArray[i];

                        if (kv.Key == k_InvalidKey)
                        {
                            IntParamArray[i] = new KeyValuePair<int, int>(key, value);
                            setOrUpdatedValue = true;
                            break;
                        }
                    }
                }
                else if (IntParamArray[existingKvIndex].Value != value)
                {
                    IntParamArray[existingKvIndex] = new KeyValuePair<int, int>(key, value);
                    setOrUpdatedValue = true;
                }

                return setOrUpdatedValue;
            }

            public bool SetBool(int key, bool value)
            {
                bool setOrUpdatedValue = false;

                int existingKvIndex = Array.FindIndex(BoolParamArray, pair => pair.Key == key);

                if (existingKvIndex == -1)
                {
                    for (int i = 0; i < BoolParamArray.Length; i++)
                    {
                        var kv = BoolParamArray[i];

                        if (kv.Key == k_InvalidKey)
                        {
                            BoolParamArray[i] = new KeyValuePair<int, bool>(key, value);
                            setOrUpdatedValue = true;
                            break;
                        }
                    }
                }
                else if (BoolParamArray[existingKvIndex].Value != value)
                {
                    BoolParamArray[existingKvIndex] = new KeyValuePair<int, bool>(key, value);
                    setOrUpdatedValue = true;
                }

                return setOrUpdatedValue;
            }

            public bool SetFloat(int key, float value)
            {
                bool setOrUpdatedValue = false;

                int existingKvIndex = Array.FindIndex(FloatParamArray, pair => pair.Key == key);

                if (existingKvIndex == -1)
                {
                    for (int i = 0; i < FloatParamArray.Length; i++)
                    {
                        var kv = FloatParamArray[i];

                        if (kv.Key == k_InvalidKey)
                        {
                            FloatParamArray[i] = new KeyValuePair<int, float>(key, value);
                            setOrUpdatedValue = true;
                            break;
                        }
                    }
                }
                else if (Math.Abs(FloatParamArray[existingKvIndex].Value - value) > Mathf.Epsilon)
                {
                    FloatParamArray[existingKvIndex] = new KeyValuePair<int, float>(key, value);
                    setOrUpdatedValue = true;
                }

                return setOrUpdatedValue;
            }

            public bool SetTrigger(int key)
            {
                if (TriggerParameters.Contains(key))
                {
                    return false;
                }

                TriggerParameters.Add(key);
                return true;
            }

            public void ClearTriggers()
            {
                TriggerParameters.Clear();
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                SerializeIntParameters(serializer);
                SerializeFloatParameters(serializer);
                SerializeBoolParameters(serializer);
                SerializeTriggerParameters(serializer);
                SerializeAnimatorLayerStates(serializer);
            }

            private void SerializeAnimatorLayerStates<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int layerCount = serializer.IsReader ? 0 : LayerStates.Length;
                serializer.SerializeValue(ref layerCount);

                if (LayerStates.Length != layerCount)
                {
                    LayerStates = new LayerState[layerCount];
                }

                for (int paramIndex = 0; paramIndex < layerCount; paramIndex++)
                {
                    var stateHash = serializer.IsReader ? 0 : LayerStates[paramIndex].StateHash;
                    serializer.SerializeValue(ref stateHash);

                    var layerWeight = serializer.IsReader ? 0 : LayerStates[paramIndex].LayerWeight;
                    serializer.SerializeValue(ref layerWeight);

                    var normalizedStateTime = serializer.IsReader ? 0 : LayerStates[paramIndex].NormalizedStateTime;
                    serializer.SerializeValue(ref normalizedStateTime);

                    if (serializer.IsReader)
                    {
                        LayerStates[paramIndex] = new LayerState()
                        {
                            LayerWeight = layerWeight,
                            StateHash = stateHash,
                            NormalizedStateTime = normalizedStateTime
                        };
                    }
                }
            }

            private void SerializeTriggerParameters<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int paramCount;
                if (serializer.IsReader)
                {
                    paramCount = 0;
                    TriggerParameters = new HashSet<int>(paramCount);
                }
                else
                {
                    paramCount = TriggerParameters.Count;
                }

                serializer.SerializeValue(ref paramCount);

                foreach (var thisParamId in TriggerParameters)
                {
                    var paramId = serializer.IsReader ? 0 : thisParamId;
                    serializer.SerializeValue(ref paramId);

                    if (serializer.IsReader)
                    {
                        TriggerParameters.Add(paramId);
                    }
                }
            }

            private void SerializeBoolParameters<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int paramCount = serializer.IsReader ? 0 : BoolParamArray.Length;
                serializer.SerializeValue(ref paramCount);

                if (BoolParamArray.Length != paramCount)
                {
                    BoolParamArray = new KeyValuePair<int, bool>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReader ? 0 : BoolParamArray[paramIndex].Key;
                    serializer.SerializeValue(ref paramId);

                    var paramBool = serializer.IsReader ? false : BoolParamArray[paramIndex].Value;
                    serializer.SerializeValue(ref paramBool);

                    if (serializer.IsReader)
                    {
                        BoolParamArray[paramIndex] = new KeyValuePair<int, bool>(paramId, paramBool);
                    }
                }
            }

            private void SerializeFloatParameters<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int paramCount = serializer.IsReader ? 0 : FloatParamArray.Length;
                serializer.SerializeValue(ref paramCount);

                if (FloatParamArray.Length != paramCount)
                {
                    FloatParamArray = new KeyValuePair<int, float>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReader ? 0 : FloatParamArray[paramIndex].Key;
                    serializer.SerializeValue(ref paramId);

                    var paramFloat = serializer.IsReader ? 0 : FloatParamArray[paramIndex].Value;
                    serializer.SerializeValue(ref paramFloat);

                    if (serializer.IsReader)
                    {
                        FloatParamArray[paramIndex] = new KeyValuePair<int, float>(paramId, paramFloat);
                    }
                }
            }

            private void SerializeIntParameters<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                int paramCount = serializer.IsReader ? 0 : IntParamArray.Length;
                serializer.SerializeValue(ref paramCount);

                if (IntParamArray.Length != paramCount)
                {
                    IntParamArray = new KeyValuePair<int, int>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReader ? 0 : IntParamArray[paramIndex].Key;
                    serializer.SerializeValue(ref paramId);

                    var paramInt = serializer.IsReader ? 0 : IntParamArray[paramIndex].Value;
                    serializer.SerializeValue(ref paramInt);

                    if (serializer.IsReader)
                    {
                        IntParamArray[paramIndex] = new KeyValuePair<int, int>(paramId, paramInt);
                    }
                }
            }

            public void Dispose()
            {
                BoolParamArray = null;
                LayerStates = null;
                TriggerParameters = null;
                IntParamArray = null;
                FloatParamArray = null;
            }
        }

        private struct LayerState
        {
            public int StateHash;
            public float NormalizedStateTime;
            public float LayerWeight;
        }

        /// <summary>
        /// This constant is used to force the resync if the delta between current
        /// and last synced normalized state time goes above it
        /// </summary>
        private const float k_NormalizedTimeResyncThreshold = 0.15f;

        [SerializeField]
        private float m_SendRate = 0.1f;
        private double m_NextSendTime = 0.0f;
        private bool m_ServerRequestsAnimationResync = false;
        [SerializeField]
        private Animator m_Animator;

        private AnimatorSnapshot m_AnimatorSnapshot;
        private List<(int, AnimatorControllerParameterType)> m_CachedAnimatorParameters;

        private ulong[] m_ServerMessagingTargetClientIds;
        private Dictionary<ulong, ulong[]> m_TargetClientIdsCache;

        private bool m_Initialized = false;

        /// <summary>
        /// This property tells us if the changes made to the Mecanim Animator will be synced to other peers or not.
        /// If not - then whatever local changes are done to the Mecanim Animator - they'll get overriden.
        /// </summary>
        public virtual bool CanCommitToAnimator => IsServer;

        public override void OnNetworkSpawn()
        {
            m_Initialized = TryInitialize(m_Animator);
        }

        /// <summary>
        /// This function call will attempt to (re)initialize the NetworkAnimator object.
        /// It will succeed if NetworkObject.IsSpawned is true and if the animator is not null.
        /// </summary>
        /// <returns></returns>
        public bool TryInitialize(Animator animator)
        {
            if (!NetworkObject.IsSpawned)
            {
                return false;
            }

            if (animator == null)
            {
                return false;
            }

            if (m_Animator != null)
            {
                m_CachedAnimatorParameters = null;
                m_AnimatorSnapshot = null;
            }

            m_Animator = animator;

            Initialize();
            return true;
        }

        private void Initialize()
        {
            var parameters = m_Animator.parameters;
            m_CachedAnimatorParameters = new List<(int, AnimatorControllerParameterType)>(parameters.Length);

            int intCount = 0;
            int floatCount = 0;
            int boolCount = 0;
            int triggerCount = 0;

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
                    case AnimatorControllerParameterType.Trigger:
                        ++triggerCount;
                        break;
                }
            }

            m_AnimatorSnapshot =
                new AnimatorSnapshot(boolCount, floatCount, intCount, triggerCount, m_Animator.layerCount);

            if (!CanCommitToAnimator)
            {
                m_Animator.StopPlayback();
            }
        }

        private void OnEnable()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += ServerOnClientConnectedCallback;
            }
        }

        private void OnDisable()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= ServerOnClientConnectedCallback;
            }
        }

        private ulong[] ServerToClientMessagingTargetClientIds
        {
            get
            {
                if (m_ServerMessagingTargetClientIds == null)
                {
                    var clientIds = new List<ulong>();
                    foreach (var networkClient in NetworkManager.ConnectedClientsList)
                    {
                        if (networkClient.ClientId != NetworkManager.ServerClientId)
                        {
                            clientIds.Add(networkClient.ClientId);
                        }
                    }

                    m_ServerMessagingTargetClientIds = clientIds.ToArray();
                }

                return m_ServerMessagingTargetClientIds;
            }
        }

        private void InvalidateCachedClientIds()
        {
            m_ServerMessagingTargetClientIds = null;
            m_TargetClientIdsCache = null;
        }

        private void ServerOnClientConnectedCallback(ulong clientId)
        {
            InvalidateCachedClientIds();

            if (CanCommitToAnimator)
            {
                m_ServerRequestsAnimationResync = true;
            }

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ServerToClientMessagingTargetClientIds
                }
            };

            RequestResyncClientRpc(clientRpcParams);
        }


        [ClientRpc]
        private void RequestResyncClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (!CanCommitToAnimator)
            {
                return;
            }

            m_ServerRequestsAnimationResync = true;
        }

        private void FixedUpdate()
        {
            if (!m_Initialized)
            {
                return;
            }

            if (CanCommitToAnimator)
            {
                bool shouldSendBasedOnTime = CheckSendRate();
                bool shouldSendBasedOnChanges = StoreState();
                if (m_ServerRequestsAnimationResync || shouldSendBasedOnTime || shouldSendBasedOnChanges)
                {
                    SendAllParamsAndState();
                    m_AnimatorSnapshot.ClearTriggers();
                    m_ServerRequestsAnimationResync = false;
                }
            }
        }

        private bool CheckSendRate()
        {
            var networkTime = NetworkManager.LocalTime.FixedTime;
            if (m_SendRate != 0 && m_NextSendTime < networkTime)
            {
                m_NextSendTime = networkTime + m_SendRate;
                return true;
            }

            return false;
        }

        private bool StoreState()
        {
            bool layerStateChanged = StoreLayerState();
            bool animatorParametersChanged = StoreParameters();

            return layerStateChanged || animatorParametersChanged;
        }

        private bool StoreLayerState()
        {
            bool changed = false;

            for (int i = 0; i < m_AnimatorSnapshot.LayerStates.Length; i++)
            {
                var animStateInfo = m_Animator.GetCurrentAnimatorStateInfo(i);

                var snapshotAnimStateInfo = m_AnimatorSnapshot.LayerStates[i];
                bool didStateChange = snapshotAnimStateInfo.StateHash != animStateInfo.fullPathHash;
                bool enoughDelta = !didStateChange &&
                                   Mathf.Abs(animStateInfo.normalizedTime - snapshotAnimStateInfo.NormalizedStateTime) >= k_NormalizedTimeResyncThreshold;

                float newLayerWeight = m_Animator.GetLayerWeight(i);
                bool layerWeightChanged = Mathf.Abs(snapshotAnimStateInfo.LayerWeight - newLayerWeight) > Mathf.Epsilon;

                if (didStateChange || enoughDelta || layerWeightChanged || m_ServerRequestsAnimationResync)
                {
                    m_AnimatorSnapshot.LayerStates[i] = new LayerState
                    {
                        StateHash = animStateInfo.fullPathHash,
                        NormalizedStateTime = animStateInfo.normalizedTime,
                        LayerWeight = newLayerWeight
                    };
                    changed = true;
                }
            }

            return changed;
        }

        private bool StoreParameters()
        {
            bool changed = false;

            foreach (var animParam in m_CachedAnimatorParameters)
            {
                var animParamHash = animParam.Item1;
                var animParamType = animParam.Item2;

                switch (animParamType)
                {
                    case AnimatorControllerParameterType.Float:
                        changed = changed || m_AnimatorSnapshot.SetFloat(animParamHash, m_Animator.GetFloat(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        changed = changed || m_AnimatorSnapshot.SetInt(animParamHash, m_Animator.GetInteger(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        changed = changed || m_AnimatorSnapshot.SetBool(animParamHash, m_Animator.GetBool(animParamHash));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (m_Animator.GetBool(animParamHash))
                        {
                            changed = changed || m_AnimatorSnapshot.SetTrigger(animParamHash);
                        }
                        break;
                }
            }

            return changed;
        }

        private void SendAllParamsAndState()
        {
            if (IsServer)
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = ServerToClientMessagingTargetClientIds
                    }
                };

                SendParamsAndLayerStatesClientRpc(m_AnimatorSnapshot, clientRpcParams);
            }
            else
            {
                SendParamsAndLayerStatesServerRpc(m_AnimatorSnapshot);
            }
        }

        /* This function generates the client ids so that we can (in client mode anyway) send messages
          * to every client except:
          *
          *  - the origin client
          *  - and the server client (because we're sending this RPC from the server)
          */
        private ulong[] GetTargetClientIds(ulong originClientId)
        {

            if (m_TargetClientIdsCache == null)
            {
                m_TargetClientIdsCache = new Dictionary<ulong, ulong[]>();
            }

            if (!m_TargetClientIdsCache.TryGetValue(originClientId, out var ids))
            {
                var clientIdsBarOrigin = new List<ulong>();
                foreach (var connectedClient in NetworkManager.ConnectedClientsList)
                {
                    if (connectedClient.ClientId != originClientId &&
                        connectedClient.ClientId != NetworkManager.ServerClientId)
                    {
                        clientIdsBarOrigin.Add(connectedClient.ClientId);
                    }
                }

                ids = clientIdsBarOrigin.ToArray();

                m_TargetClientIdsCache[originClientId] = ids;
            }

            return ids;
        }

        [ServerRpc]
        private void SendParamsAndLayerStatesServerRpc(AnimatorSnapshot animSnapshot, ServerRpcParams serverRpcParams = default)
        {
            if (!CanCommitToAnimator)
            {
                ApplyAnimatorSnapshot(animSnapshot);
            }

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = GetTargetClientIds(serverRpcParams.Receive.SenderClientId)
                }
            };

            SendParamsAndLayerStatesClientRpc(animSnapshot, clientRpcParams);
        }

        [ClientRpc]
        private void SendParamsAndLayerStatesClientRpc(AnimatorSnapshot animSnapshot, ClientRpcParams clientRpcParams = default)
        {
            if (!CanCommitToAnimator && !IsHost)
            {
                ApplyAnimatorSnapshot(animSnapshot);
            }
        }

        private void ApplyAnimatorSnapshot(AnimatorSnapshot animatorSnapshot)
        {
            foreach (var intParameter in animatorSnapshot.IntParamArray)
            {
                m_Animator.SetInteger(intParameter.Key, intParameter.Value);
            }

            foreach (var floatParameter in animatorSnapshot.FloatParamArray)
            {
                m_Animator.SetFloat(floatParameter.Key, floatParameter.Value);
            }

            foreach (var boolParameter in animatorSnapshot.BoolParamArray)
            {
                m_Animator.SetBool(boolParameter.Key, boolParameter.Value);
            }

            foreach (var triggerParameter in animatorSnapshot.TriggerParameters)
            {
                m_Animator.SetTrigger(triggerParameter);
            }

            for (var layerIndex = 0; layerIndex < animatorSnapshot.LayerStates.Length; layerIndex++)
            {
                var layerState = animatorSnapshot.LayerStates[layerIndex];

                m_Animator.SetLayerWeight(layerIndex, layerState.LayerWeight);

                var currentAnimatorState = m_Animator.GetCurrentAnimatorStateInfo(layerIndex);

                bool stateChanged = currentAnimatorState.fullPathHash != layerState.StateHash;
                bool forceAnimationCatchup = !stateChanged &&
                                             Mathf.Abs(layerState.NormalizedStateTime - currentAnimatorState.normalizedTime) >= k_NormalizedTimeResyncThreshold;

                if (stateChanged || forceAnimationCatchup)
                {
                    m_Animator.Play(layerState.StateHash, layerIndex, layerState.NormalizedStateTime);
                }
            }
        }
    }
}
