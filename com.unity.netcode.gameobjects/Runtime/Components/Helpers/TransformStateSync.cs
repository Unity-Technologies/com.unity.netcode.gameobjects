using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

namespace Unity.Netcode
{
    public class TransformStateSync : NetworkBehaviour, INetworkUpdateSystem
    {
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

        // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
        private BufferedLinearInterpolatorQuaternion m_RotationInterpolator;
        private BufferedLinearInterpolatorVector3 m_PositionInterpolator;
        private BufferedLinearInterpolatorVector3 m_ScaleInterpolator;

        private bool m_IsMotionAuthority;
        public bool IsMotionAuthority => m_IsMotionAuthority;

        private void UpdateMotionAuthority(bool isDespawning = false)
        {
            // Clean up for despawn
            if (isDespawning)
            {
                NetworkManager.TransformStateManager.TrackTransformStateChanges(this, false);
                NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
                CurrentState = TransformStateSyncStates.NotSpawned;
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
        }

        internal override void InternalOnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion();
            m_PositionInterpolator = new BufferedLinearInterpolatorVector3();
            m_ScaleInterpolator = new BufferedLinearInterpolatorVector3();
            CurrentState = TransformStateSyncStates.Spawning;
            base.InternalOnNetworkPreSpawn(ref networkManager);
        }

        protected internal override void OnInternalOnNetworkSpawn()
        {
            // Determines if we are the motion authority
            UpdateMotionAuthority();
            InitializeInterpolators();
            NetworkManager.TransformStateManager.TrackTransformStateChanges(this, true);
            base.OnInternalOnNetworkSpawn();
        }


        public override void OnNetworkPreDespawn()
        {
            UpdateMotionAuthority(true);
            m_RotationInterpolator = null;
            m_PositionInterpolator = null;
            m_ScaleInterpolator = null;
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
            base.OnNetworkPreDespawn();
        }

        public override void OnDestroy()
        {
            NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
            base.OnDestroy();
        }

        internal void UpdateState(double time, TransformGridState state)
        {
            if (state.DirtyScale)
            {
                m_ScaleInterpolator.AddMeasurement(transform.parent, state.ScaleFloat, time);
            }

            if (state.DirtyPosition)
            {
#if DEBUG_TRANSFORMSTATE
                Debug.Log($"[{name}][NetworkObjectId: {NetworkObjectId}][{nameof(TransformStateSync)}][{nameof(UpdateState)}][Position] {state.PositionFloat}");
#endif
                m_PositionInterpolator.AddMeasurement(transform.parent, state.PositionFloat, time);
            }

            if (state.DirtyRotation)
            {
                m_RotationInterpolator.AddMeasurement(transform.parent, state.Rotation.Rotation, time);
            }
        }

        internal void UpdateState(double time, TransformIntState state)
        {
            if (state.DirtyScale)
            {
                m_ScaleInterpolator.AddMeasurement(transform.parent, transform.localScale += state.DecompScale, time);
            }

            if (state.DirtyPosition)
            {
                m_PositionInterpolator.AddMeasurement(transform.parent,state.DecompPosition + (HasParent ? transform.localScale : transform.position), time);
            }

            if (state.DirtyRotation)
            {
                var rotation = (HasParent ? transform.localRotation : transform.rotation);
                rotation.x += state.DecompRotation.x;
                rotation.y += state.DecompRotation.y;
                rotation.z += state.DecompRotation.z;
                rotation.w += state.DecompRotation.w;
                m_RotationInterpolator.AddMeasurement(transform.parent, rotation, time);
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

            //
            m_ScaleInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            m_PositionInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);
            m_RotationInterpolator.Update(cachedDeltaTime, tickLatencyAsTime, minDeltaTime, maxDeltaTime, true);

            var scale = m_ScaleInterpolator.GetInterpolatedValue();
            var position = m_PositionInterpolator.GetInterpolatedValue();
            var rotation = m_RotationInterpolator.GetInterpolatedValue();
            
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }
    }
}


