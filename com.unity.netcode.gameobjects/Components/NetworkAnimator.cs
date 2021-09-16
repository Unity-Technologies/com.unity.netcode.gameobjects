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
            public List<int> TriggerParameters;
            public LayerState[] LayerStates;

            public AnimatorSnapshot(int boolCount, int floatCount, int intCount, int triggerCount, int layerStatesCount)
            {
                BoolParamArray = new KeyValuePair<int, bool>[boolCount];
                IntParamArray = new KeyValuePair<int, int>[intCount];
                FloatParamArray = new KeyValuePair<int, float>[floatCount];
                TriggerParameters = new List<int>(triggerCount);
                LayerStates = new LayerState[layerStatesCount];
            }

            public AnimatorSnapshot()
            {
                BoolParamArray = new KeyValuePair<int, bool>[0];
                IntParamArray = new KeyValuePair<int, int>[0];
                FloatParamArray = new KeyValuePair<int, float>[0];
                TriggerParameters = new List<int>(0);
                LayerStates = new LayerState[0];
            }

            private void ResetBuffersToDefaultValues()
            {
                for (int i = 0; i < BoolParamArray.Length; i++)
                {
                    BoolParamArray[i] = new KeyValuePair<int, bool>(-1, false);
                }

                for (int i = 0; i < FloatParamArray.Length; i++)
                {
                    FloatParamArray[i] = new KeyValuePair<int, float>(-1, 0);
                }

                for (int i = 0; i < IntParamArray.Length; i++)
                {
                    IntParamArray[i] = new KeyValuePair<int, int>(-1, 0);
                }

                TriggerParameters.Clear();

                Array.Clear(LayerStates, 0, LayerStates.Length);
            }

            public bool SetInt(int key, int value)
            {
                bool setOrUpdatedValue = false;

                for (int i = 0; i < IntParamArray.Length; i++)
                {
                    var kv = IntParamArray[i];

                    if (kv.Key != -1 ||
                        (kv.Key == key && kv.Value != value))
                    {
                        continue;
                    }

                    IntParamArray[i] = new KeyValuePair<int, int>(key, value);
                    setOrUpdatedValue = true;
                    break;
                }

                return setOrUpdatedValue;
            }

            public bool SetBool(int key, bool value)
            {
                bool setOrUpdatedValue = false;

                for (int i = 0; i < BoolParamArray.Length; i++)
                {
                    var kv = BoolParamArray[i];

                    if (kv.Key != -1 ||
                        (kv.Key == key && kv.Value != value))
                    {
                        continue;
                    }

                    BoolParamArray[i] = new KeyValuePair<int, bool>(key, value);
                    setOrUpdatedValue = true;
                    break;
                }

                return setOrUpdatedValue;
            }

            public bool SetFloat(int key, float value)
            {
                bool setOrUpdatedValue = false;

                for (int i = 0; i < FloatParamArray.Length; i++)
                {
                    var kv = FloatParamArray[i];

                    if (kv.Key != -1 ||
                        (kv.Key == key && Mathf.Abs(kv.Value - value) < Mathf.Epsilon))
                    {
                        continue;
                    }

                    FloatParamArray[i] = new KeyValuePair<int, float>(key, value);
                    setOrUpdatedValue = true;
                    break;
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

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                if (serializer.IsReading)
                {
                    ResetBuffersToDefaultValues();
                }

                SerializeIntParameters(serializer);
                SerializeFloatParameters(serializer);
                SerializeBoolParameters(serializer);
                SerializeTriggerParameters(serializer);
                SerializeAnimatorLayerStates(serializer);
            }

            private void SerializeAnimatorLayerStates(NetworkSerializer serializer)
            {
                int layerCount = serializer.IsReading ? 0 : LayerStates.Length;
                serializer.Serialize(ref layerCount);

                if (LayerStates.Length != layerCount)
                {
                    LayerStates = new LayerState[layerCount];
                }

                for (int paramIndex = 0; paramIndex < layerCount; paramIndex++)
                {
                    var stateHash = serializer.IsReading ? 0 : LayerStates[paramIndex].StateHash;
                    serializer.Serialize(ref stateHash);

                    var layerWeight = serializer.IsReading ? 0 : LayerStates[paramIndex].LayerWeight;
                    serializer.Serialize(ref layerWeight);

                    var normalizedStateTime = serializer.IsReading ? 0 : LayerStates[paramIndex].NormalizedStateTime;
                    serializer.Serialize(ref normalizedStateTime);

                    if (serializer.IsReading)
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

            private void SerializeTriggerParameters(NetworkSerializer serializer)
            {
                int paramCount = serializer.IsReading ? 0 : TriggerParameters.Count;
                serializer.Serialize(ref paramCount);

                for (int i = 0; i < paramCount; i++)
                {
                    var paramId = serializer.IsReading ? 0 : TriggerParameters[i];
                    serializer.Serialize(ref paramId);

                    if (serializer.IsReading)
                    {
                        TriggerParameters.Add(paramId);
                    }
                }
            }

            private void SerializeBoolParameters(NetworkSerializer serializer)
            {
                int paramCount = serializer.IsReading ? 0 : BoolParamArray.Length;
                serializer.Serialize(ref paramCount);

                if (BoolParamArray.Length != paramCount)
                {
                    BoolParamArray = new KeyValuePair<int, bool>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReading ? 0 : BoolParamArray[paramIndex].Key;
                    serializer.Serialize(ref paramId);

                    var paramBool = serializer.IsReading ? false : BoolParamArray[paramIndex].Value;
                    serializer.Serialize(ref paramBool);

                    if (serializer.IsReading)
                    {
                        BoolParamArray[paramIndex] = new KeyValuePair<int, bool>(paramId, paramBool);
                    }
                }
            }

            private void SerializeFloatParameters(NetworkSerializer serializer)
            {
                int paramCount = serializer.IsReading ? 0 : FloatParamArray.Length;
                serializer.Serialize(ref paramCount);

                if (FloatParamArray.Length != paramCount)
                {
                    FloatParamArray = new KeyValuePair<int, float>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReading ? 0 : FloatParamArray[paramIndex].Key;
                    serializer.Serialize(ref paramId);

                    var paramFloat = serializer.IsReading ? 0 : FloatParamArray[paramIndex].Value;
                    serializer.Serialize(ref paramFloat);

                    if (serializer.IsReading)
                    {
                        FloatParamArray[paramIndex] = new KeyValuePair<int, float>(paramId, paramFloat);
                    }
                }
            }

            private void SerializeIntParameters(NetworkSerializer serializer)
            {
                int paramCount = serializer.IsReading ? 0 : IntParamArray.Length;
                serializer.Serialize(ref paramCount);

                if (IntParamArray.Length != paramCount)
                {
                    IntParamArray = new KeyValuePair<int, int>[paramCount];
                }

                for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                {
                    var paramId = serializer.IsReading ? 0 : IntParamArray[paramIndex].Key;
                    serializer.Serialize(ref paramId);

                    var paramInt = serializer.IsReading ? 0 : IntParamArray[paramIndex].Value;
                    serializer.Serialize(ref paramInt);

                    if (serializer.IsReading)
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
        private Dictionary<ulong, ulong[]> m_ClientIdsExcludingThemselvesCache;

        private bool m_Initialized = false;

        /// <summary>
        /// This property tells us if the changes made to the Mecanim Animator will be synced to other peers or not.
        /// If not - then whatever local changes are done to the Mecanim Animator - they'll get overriden.
        /// </summary>
        public virtual bool WillCommitChanges => IsServer;

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

            if (!WillCommitChanges)
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
            m_ClientIdsExcludingThemselvesCache = null;
        }

        private void ServerOnClientConnectedCallback(ulong clientId)
        {
            InvalidateCachedClientIds();

            if (WillCommitChanges)
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
            if (!WillCommitChanges)
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

            if (WillCommitChanges)
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

        private ulong[] GetTargetClientIds(ulong originClientId)
        {
            if (m_ClientIdsExcludingThemselvesCache == null)
            {
                m_ClientIdsExcludingThemselvesCache = new Dictionary<ulong, ulong[]>();
            }

            if (!m_ClientIdsExcludingThemselvesCache.TryGetValue(originClientId, out var ids))
            {
                var clientIdsBarOrigin = new List<ulong>();
                foreach (var connectedClient in NetworkManager.ConnectedClientsList)
                {
                    if (connectedClient.ClientId == originClientId)
                    {
                        continue;
                    }

                    clientIdsBarOrigin.Add(connectedClient.ClientId);
                }

                ids = clientIdsBarOrigin.ToArray();
                m_ClientIdsExcludingThemselvesCache[originClientId] = ids;
            }

            return ids;
        }

        [ServerRpc]
        private void SendParamsAndLayerStatesServerRpc(AnimatorSnapshot animSnapshot, ServerRpcParams serverRpcParams = default)
        {
            if (!WillCommitChanges)
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
            if (!WillCommitChanges)
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
