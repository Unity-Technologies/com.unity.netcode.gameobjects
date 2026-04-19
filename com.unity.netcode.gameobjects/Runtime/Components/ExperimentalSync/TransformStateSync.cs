using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

namespace Unity.Netcode
{
    public class TransformStateSync : NetworkBehaviour, INetworkUpdateSystem
    {
        internal struct IdentifierObjectMap
        {
            public ulong NetworkObjectId;
            public ushort NetworkBehaviourId;
        }

        /// <summary>
        /// Handles providing a unique identifier for transform synchronization,
        /// while also providing the ability to recylce identifiers to keep the
        /// maximum identifier value no more than a bit higher than the maximum
        /// number of things one might spawn in a session.
        /// </summary>
        private class TransformdentifierHandler
        {
            private struct ReleaseId
            {
                public ushort Identifier;
                public float AvailabilityDelay;
            }

            private HashSet<ReleaseId> m_ReleasedIds = new HashSet<ReleaseId>();
            private ushort m_HighestIdAssigned = 0;
            // [Client Identifier][Transform Identifier][NetworkObjectId][NetworkBehaviourId]
            private Dictionary<ulong, Dictionary<ushort, IdentifierObjectMap>> m_MotionAuthorityObjectMap = new Dictionary<ulong, Dictionary<ushort, IdentifierObjectMap>>();

            internal IdentifierObjectMap GetIdentifierObjectMap(ulong clientId, ushort identifier)
            {
                if (m_MotionAuthorityObjectMap.ContainsKey(clientId))
                {
                    if (m_MotionAuthorityObjectMap[clientId].ContainsKey(identifier))
                    {
                        return m_MotionAuthorityObjectMap[clientId][identifier];
                    }
                }
                return default;
            }

            public void AddIdentifier(ulong clientId, ushort identifier, ulong networkObjectId, ushort networkBehaviourId)
            {
                if (!m_MotionAuthorityObjectMap.ContainsKey(clientId))
                {
                    m_MotionAuthorityObjectMap.Add(clientId, new Dictionary<ushort, IdentifierObjectMap>());
                }

                if (!m_MotionAuthorityObjectMap[clientId].ContainsKey(identifier))
                {
                    m_MotionAuthorityObjectMap[clientId].Add(identifier, new IdentifierObjectMap()
                    {
                        NetworkObjectId = networkObjectId,
                        NetworkBehaviourId = networkBehaviourId
                    });
                }
            }

            public void RemoveIdentifier(ulong clientId, ushort identifier)
            {
                if (m_MotionAuthorityObjectMap.ContainsKey(clientId))
                {
                    m_MotionAuthorityObjectMap[clientId].Remove(identifier);

                    if (m_MotionAuthorityObjectMap[clientId].Count == 0)
                    {
                        m_MotionAuthorityObjectMap.Remove(clientId);
                    }
                }
            }

            public ushort GetNextIdentifier()
            {
                var nextIdentifier = (ushort)0;
                ReleaseId releasedId = default;
                foreach (var entry in m_ReleasedIds)
                {
                    if (entry.AvailabilityDelay < Time.realtimeSinceStartup)
                    {
                        releasedId = entry;
                        break;
                    }
                }

                if (releasedId.Identifier > 0)
                {
                    m_ReleasedIds.Remove(releasedId);
                    nextIdentifier = releasedId.Identifier;
                }
                else
                {
                    m_HighestIdAssigned++;
                    nextIdentifier = m_HighestIdAssigned;
                }
                return nextIdentifier;
            }

            public void ReleaseIdentifier(ushort identifier)
            {
                m_ReleasedIds.Add(new ReleaseId()
                {
                    Identifier = identifier,
                    AvailabilityDelay = Time.realtimeSinceStartup + 2.0f
                });
            }
        }

        private static Dictionary<ulong, TransformdentifierHandler> s_TransformIdentifierHandlers = new Dictionary<ulong, TransformdentifierHandler>();

        internal static IdentifierObjectMap GetIdentifierObjectMap(ulong clientId, ushort identifier)
        {
            if (s_TransformIdentifierHandlers.ContainsKey(clientId))
            {
                return s_TransformIdentifierHandlers[clientId].GetIdentifierObjectMap(clientId, identifier);
            }
            return default;
        }

        /// <summary>
        /// Determines if the server or client owner pushes transform states.
        /// </summary>
        public enum AuthorityModes
        {
            /// <summary>
            /// Server pushes transform state updates.
            /// </summary>
            Server,
            /// <summary>
            /// Client owner pushes transform state updates.
            /// </summary>
            Owner,
        }

        public AuthorityModes Authority;


        public enum TransformStateSyncStates
        {
            NotSpawned,
            Spawning,
            SendingDeltas,
            ReceivingDeltas,
        }


        public TransformStateSyncStates CurrentState { get; private set; }

        private ushort m_TransformIdentifier;
        internal ushort TransformIdentifier => m_TransformIdentifier;
        public bool IsMotionAuthority => m_IsMotionAuthority;
        private bool m_IsMotionAuthority;


        // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
        private BufferedLinearInterpolatorQuaternion m_RotationInterpolator;
        private BufferedLinearInterpolatorVector3 m_ForwardInterpolator;
        private BufferedLinearInterpolatorVector3 m_PositionInterpolator;
        private BufferedLinearInterpolatorVector3 m_ScaleInterpolator;


        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref m_TransformIdentifier);
            var halfVector3 = new HalfVector3(transform.position);
            var rotation = transform.rotation;
            var rotationCompressed = (uint)0;
            if (serializer.IsWriter)
            {
                rotationCompressed = QuaternionCompressor.CompressQuaternion(ref rotation);
            }

            serializer.SerializeValue(ref halfVector3);
            serializer.SerializeValue(ref rotationCompressed);

            if (serializer.IsReader)
            {
                QuaternionCompressor.DecompressQuaternion(ref rotation, rotationCompressed);
                transform.SetPositionAndRotation(halfVector3.ToVector3(), rotation);
            }
            base.OnSynchronize(ref serializer);
        }

        /// <summary>
        /// TODO: Add editor inspector view way of configuring whether the kinematic state should
        /// be set or not and for which Rigidbody(ies).
        /// <see cref="ComponentController"/>
        /// We could use a <see cref="NetworkRigidbodyBase"/> derived component, but that
        /// requires removing the required component and making adjustments.
        /// For now, just mock the same kind of behaviour.
        /// </summary>
        private void UpdateKinematicState()
        {
            if (NetworkObject.NetworkRigidbodies.Count > 0)
            {
                NetworkObject.NetworkRigidbodies[0].SetIsKinematic(!m_IsMotionAuthority);
            }
        }

        private void UpdateMotionAuthority(bool isDespawning = false)
        {
            // Clean up for despawn
            if (isDespawning)
            {
                NetworkManager.TransformStateManager.TrackTransformStateChanges(this, false);
                NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
                CurrentState = TransformStateSyncStates.NotSpawned;
                m_IsMotionAuthority = false;
                // Exit early before updating motion authority status (doesn't matter at this point)
                return;
            }

            // Keep track of whether we were the motion authority
            var wasMotionAuthority = m_IsMotionAuthority;

            // Set motion authority status
            m_IsMotionAuthority = (Authority == AuthorityModes.Server && IsServer) || (Authority == AuthorityModes.Owner && IsOwner);

            if (!wasMotionAuthority && m_IsMotionAuthority)
            {
                // If already sending, then exit early.
                if (CurrentState == TransformStateSyncStates.SendingDeltas)
                {
                    return;
                }

                if (CurrentState == TransformStateSyncStates.ReceivingDeltas)
                {
                    NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
                }

                // Configure for sending
                CurrentState = TransformStateSyncStates.SendingDeltas;
            }
            else
            {
                // Exit early if already configured for receive (i.e. non-authority instance)
                if (CurrentState == TransformStateSyncStates.ReceivingDeltas)
                {
                    return;
                }

                // Configure for receiving
                CurrentState = TransformStateSyncStates.ReceivingDeltas;
                NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
            }

            UpdateKinematicState();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            UpdateMotionAuthority();
            base.OnOwnershipChanged(previous, current);
        }

        private void InitializeInterpolators()
        {
            var serverTime = NetworkManager.ServerTime;
            m_ScaleInterpolator.ResetTo(transform.parent, transform.localScale, serverTime.Time);
            m_PositionInterpolator.ResetTo(transform.parent, (HasParent ? transform.localPosition : transform.position), serverTime.Time);
            m_RotationInterpolator.ResetTo(transform.parent, (HasParent ? transform.localRotation : transform.rotation), serverTime.Time);
            m_ForwardInterpolator.ResetTo(transform.parent, transform.forward, serverTime.Time);
            LastPositionUpdate = (HasParent ? transform.localPosition : transform.position);
        }

        internal override void InternalOnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion();
            m_PositionInterpolator = new BufferedLinearInterpolatorVector3();
            m_ScaleInterpolator = new BufferedLinearInterpolatorVector3();
            m_ForwardInterpolator = new BufferedLinearInterpolatorVector3();

            CurrentState = TransformStateSyncStates.Spawning;

            var localClientId = networkManager.LocalClientId;

            // Always create a handler for each client
            if (!s_TransformIdentifierHandlers.ContainsKey(localClientId))
            {
                s_TransformIdentifierHandlers.Add(localClientId, new TransformdentifierHandler());
            }

            // Only the spawn authority assigns the unique identifier
            // Client-server is always the server.
            // DA is any client.
            if (NetworkObject.IsSpawnAuthority)
            {
                m_TransformIdentifier = s_TransformIdentifierHandlers[networkManager.LocalClientId].GetNextIdentifier();
            }
            base.InternalOnNetworkPreSpawn(ref networkManager);
        }

        protected internal override void OnInternalOnNetworkSpawn()
        {
            // Determines if we are the motion authority
            UpdateMotionAuthority();

            var motionAuthorityClientId = m_IsMotionAuthority ? NetworkManager.LocalClientId : Authority == AuthorityModes.Server ? NetworkManager.ServerClientId : OwnerClientId;
            // Motion authority

            if (!s_TransformIdentifierHandlers.ContainsKey(motionAuthorityClientId))
            {
                s_TransformIdentifierHandlers.Add(motionAuthorityClientId, new TransformdentifierHandler());
            }

            s_TransformIdentifierHandlers[motionAuthorityClientId].AddIdentifier(motionAuthorityClientId, TransformIdentifier, NetworkObjectId, NetworkBehaviourId);

            InitializeInterpolators();
            NetworkManager.TransformStateManager.TrackTransformStateChanges(this, true);
            var rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null && !IsMotionAuthority)
            {
                rigidBody.isKinematic = true;
            }

            base.OnInternalOnNetworkSpawn();
        }

        public override void OnNetworkPreDespawn()
        {
            if (s_TransformIdentifierHandlers.ContainsKey(OwnerClientId))
            {
                if (m_IsMotionAuthority)
                {
                    s_TransformIdentifierHandlers[OwnerClientId].ReleaseIdentifier(TransformIdentifier);
                    m_TransformIdentifier = 0;
                }
                else
                {
                    s_TransformIdentifierHandlers[OwnerClientId].RemoveIdentifier(OwnerClientId, TransformIdentifier);
                }
            }

            UpdateMotionAuthority(true);

            m_RotationInterpolator = null;
            m_PositionInterpolator = null;
            m_ScaleInterpolator = null;
            m_ForwardInterpolator = null;
            CurrentState = TransformStateSyncStates.NotSpawned;
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
            base.OnNetworkPreDespawn();
        }

        public override void OnDestroy()
        {
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
            base.OnDestroy();
        }

        internal Vector3 LastPositionUpdate;

        internal void UpdateState(double time, TransformGridState state)
        {
            if (state.DirtyScale)
            {
                m_ScaleInterpolator.AddMeasurement(transform.parent, state.ScaleFloat, time);
            }

            if (state.DirtyPosition)
            {
#if DEBUG_TRANSFORMSTATE
                Debug.Log($"[{name}][NetworkObjectId: {NetworkObjectId}][{nameof(TransformStateSync)}][{nameof(UpdateState)}][Position] ({state.PositionFloat}) | " +
                    $"{state.Position.CompressValuesAsString()} ToVector3 ({state.Position.ToVector3(state.InvPrecision)})");
#endif
                // Just keep up to date with the most current position which is used when getting the next state update
                LastPositionUpdate = state.PositionFloat;
                m_PositionInterpolator.AddMeasurement(transform.parent, state.PositionFloat, time);
            }

            if (state.DirtyRotation)
            {
                //m_ForwardInterpolator.AddMeasurement(transform.parent, state.Forward.Forward, time);
                m_RotationInterpolator.AddMeasurement(transform.parent, state.Rotation.Rotation, time);
            }
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            UpdateInterpolation();
        }

        public bool HasParent { get; private set; }

        /// <summary>
        /// TODO: Move into job
        /// </summary>
        private void UpdateInterpolation()
        {
            // Use the local time because:
            // Client-Server:
            // Local time is server time on a host or server.
            // Local time on clients takes latency into consideration.
            // Distributed authority:
            // Local time is used by the authority.
            // Local time on non-authority takes latency into consid]eration.
            var timeSystem = NetworkManager.LocalTime;
            var currentTime = timeSystem.Time;
            var cachedDeltaTime = NetworkManager.RealTimeProvider.DeltaTime;
            // Optional user defined tick offset to be used to push the "render time" (the time that will be used to determine if a state update is available)
            // back in order to provide more room for the interpolator to interpolate towards when latency conditions are impacting the frequency that state
            // updates are received.
            var tickLatency = Mathf.Max(1, NetworkManager.NetworkTimeSystem.TickLatency + InterpolationBufferTickOffset);

            // TODO: Investigate if this matters anymore
            //// If using an owner authoritative motion model
            //if (!IsServerAuthoritative())
            //{
            //    // and if we are in a client-server topology (including DAHost)
            //    if (!m_CachedNetworkManager.DistributedAuthorityMode ||
            //        (m_CachedNetworkManager.DistributedAuthorityMode && !m_CachedNetworkManager.CMBServiceConnection))
            //    {
            //        // If this instance belongs to another client (i.e. not the server/host), then add 1 to our tick latency.
            //        if (!m_CachedNetworkManager.IsServer && !NetworkObject.IsOwnedByServer)
            //        {
            //            // Account for the 2xRTT with owner authoritative
            //            tickLatency += 1;
            //        }
            //    }
            //}

            // Get the tick latency (ticks ago) as time (in the past) to process state updates in the queue.
            var tickLatencyAsTime = timeSystem.TimeTicksAgo(tickLatency).Time;

            //#if COM_UNITY_MODULES_PHYSICS || COM_UNITY_MODULES_PHYSICS2D
            //            // If using rigid body for motion, then we need to increment
            //            // our tick latency based on the number of times FixedUpdate
            //            // is executed.
            //            if (m_UseRigidbodyForMotion)
            //            {
            //                tickLatencyAsTime += m_FixedTimeFrameDelta;
            //                currentTime += m_FixedTimeFrameDelta;
            //            }
            //#endif

            // Smooth dampening and extrapolation specific:
            // We clamp between the tick rate frequency and the tick latency x tick rate frequency
            var minDeltaTime = timeSystem.FixedDeltaTimeAsDouble;

            // Maximum delta time is the maximum time we will lerp between values. If the time exceeds this due to extreme
            // latency then the value's interpolation rate will be accelerated to reach the goal and continue interpolating
            // the next state updates.
            var maxDeltaTime = tickLatency * minDeltaTime;


            m_ScaleInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            m_PositionInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            m_RotationInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            //m_ForwardInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);

            var scale = m_ScaleInterpolator.GetInterpolatedValue();
            var position = m_PositionInterpolator.GetInterpolatedValue();
            var rotation = m_RotationInterpolator.GetInterpolatedValue();
            //var forward = m_ForwardInterpolator.GetInterpolatedValue();

            //transform.position = position;
            //transform.forward = forward;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }
    }
}


