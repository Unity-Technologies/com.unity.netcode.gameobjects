using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode.Components
{

#pragma warning disable IDE0001
    /// <summary>
    /// A subclass of <see cref="NetworkTransform"/> that supports basic client anticipation - the client
    /// can set a value on the belief that the server will update it to reflect the same value in a future update
    /// (i.e., as the result of an RPC call). This value can then be adjusted as new updates from the server come in,
    /// in three basic modes:
    ///
    /// <list type="bullet">
    ///
    /// <item><b>Snap:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="StaleDataHandling.Ignore"/> and no <see cref="OnReanticipate"/> callback),
    /// the moment a more up-to-date value is received from the authority, it will simply replace the anticipated value,
    /// resulting in a "snap" to the new value if it is different from the anticipated value.</item>
    ///
    /// <item><b>Smooth:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="Netcode.StaleDataHandling.Ignore"/> and an <see cref="OnReanticipate"/> callback that calls
    /// <see cref="Smooth"/> from the anticipated value to the authority value with an appropriate
    /// <see cref="Mathf.Lerp"/>-style smooth function), when a more up-to-date value is received from the authority,
    /// it will interpolate over time from an incorrect anticipated value to the correct authoritative value.</item>
    ///
    /// <item><b>Constant Reanticipation:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="Netcode.StaleDataHandling.Reanticipate"/> and an <see cref="OnReanticipate"/> that calculates a
    /// new anticipated value based on the current authoritative value), when a more up-to-date value is received from
    /// the authority, user code calculates a new anticipated value, possibly calling <see cref="Smooth"/> to interpolate
    /// between the previous anticipation and the new anticipation. This is useful for values that change frequently and
    /// need to constantly be re-evaluated, as opposed to values that change only in response to user action and simply
    /// need a one-time anticipation when the user performs that action.</item>
    ///
    /// </list>
    ///
    /// Note that these three modes may be combined. For example, if an <see cref="OnReanticipate"/> callback
    /// does not call either <see cref="Smooth"/> or one of the Anticipate methods, the result will be a snap to the
    /// authoritative value, enabling for a callback that may conditionally call <see cref="Smooth"/> when the
    /// difference between the anticipated and authoritative values is within some threshold, but fall back to
    /// snap behavior if the difference is too large.
    /// </summary>
#pragma warning restore IDE0001
    [DisallowMultipleComponent]
    [AddComponentMenu("Netcode/Anticipated Network Transform")]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class AnticipatedNetworkTransform : NetworkTransform
    {
        public struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private TransformState m_AuthorityTransform = new TransformState();
        private TransformState m_AnticipatedTransform = new TransformState();
        private double m_LastAnticipatedTick;
        private double m_LastAuthorityUpdateTick;

        private TransformState m_SmoothFrom;
        private TransformState m_SmoothTo;
        private float m_SmoothDuration;
        private float m_CurrentSmoothTime;

        private bool m_OutstandingAuthorityChange = false;

#if UNITY_EDITOR
        private void Reset()
        {
            // Anticipation + smoothing is a form of interpolation, and adding NetworkTransform's buffered interpolation
            // makes the anticipation get weird, so we default it to false.
            Interpolate = false;
        }
#endif

#pragma warning disable IDE0001
        /// <summary>
        /// Defines what the behavior should be if we receive a value from the server with an earlier associated
        /// tick value than the anticipation tick value.
        /// <br/><br/>
        /// If this is <see cref="Netcode.StaleDataHandling.Ignore"/>, the stale data will be ignored and the authoritative
        /// value will not replace the anticipated value until the anticipation tick is reached. <see cref="OnAuthoritativeValueChanged"/>
        /// and <see cref="OnReanticipate"/> will also not be invoked for this stale data.
        /// <br/><br/>
        /// If this is <see cref="Netcode.StaleDataHandling.Reanticipate"/>, the stale data will replace the anticipated data and
        /// <see cref="OnAuthoritativeValueChanged"/> and <see cref="OnReanticipate"/> will be invoked.
        /// In this case, the authoritativeTick value passed to <see cref="OnReanticipate"/> will be lower than
        /// the anticipationTick value, and that callback can be used to calculate a new anticipated value.
        /// </summary>
#pragma warning restore IDE0001
        public StaleDataHandling StaleDataHandling = StaleDataHandling.Reanticipate;

        /// <summary>
        /// Contains the current state of this transform on the server side
        /// </summary>
        public TransformState AuthorityState => m_AuthorityTransform;

        /// <summary>
        /// Contains the current anticipated state, which will match the values of this object's
        /// actual <see cref="MonoBehaviour.transform"/>.
        /// </summary>
        public TransformState AnticipatedState => m_AnticipatedTransform;

        /// <summary>
        /// Anticipate that, at the end of one round trip to the server, this transform will be in the given
        /// <see cref="newPosition"/>
        /// </summary>
        /// <param name="newPosition"></param>
        public void AnticipateMove(Vector3 newPosition)
        {
            transform.position = newPosition;
            m_AnticipatedTransform.Position = newPosition;
            m_LastAnticipatedTick = NetworkManager.LocalTime.TickWithPartial;
        }

        /// <summary>
        /// Anticipate that, at the end of one round trip to the server, this transform will have the given
        /// <see cref="newRotation"/>
        /// </summary>
        /// <param name="newRotation"></param>
        public void AnticipateRotate(Quaternion newRotation)
        {
            transform.rotation = newRotation;
            m_AnticipatedTransform.Rotation = newRotation;
            m_LastAnticipatedTick = NetworkManager.LocalTime.TickWithPartial;
        }

        /// <summary>
        /// Anticipate that, at the end of one round trip to the server, this transform will have the given
        /// <see cref="newScale"/>
        /// </summary>
        /// <param name="newScale"></param>
        public void AnticipateScale(Vector3 newScale)
        {
            transform.localScale = newScale;
            m_AnticipatedTransform.Scale = newScale;
            m_LastAnticipatedTick = NetworkManager.LocalTime.TickWithPartial;
        }

        protected void LateUpdate()
        {
            if (CanCommitToTransform)
            {
                var transform_ = transform;
                if (transform_.position != m_AuthorityTransform.Position || transform_.rotation != m_AuthorityTransform.Rotation || transform_.localScale != m_AuthorityTransform.Scale)
                {
                    m_AuthorityTransform = new TransformState
                    {
                        Position = transform_.position,
                        Rotation = transform_.rotation,
                        Scale = transform_.localScale
                    };
                    m_AnticipatedTransform = m_AuthorityTransform;

                    var message = new NetworkTransformAnticipationSyncMessage
                    {
                        NetworkObjectId = NetworkObjectId,
                        NetworkBehaviourId = NetworkBehaviourId,
                    };
                    var clientCount = m_CachedNetworkManager.ConnectionManager.ConnectedClientsList.Count;
                    for (int i = 0; i < clientCount; i++)
                    {
                        var clientId = m_CachedNetworkManager.ConnectionManager.ConnectedClientsList[i].ClientId;
                        if (NetworkManager.ServerClientId == clientId)
                        {
                            continue;
                        }
                        if (!NetworkObject.Observers.Contains(clientId))
                        {
                            continue;
                        }
                        NetworkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, clientId);
                    }
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (m_CurrentSmoothTime < m_SmoothDuration)
            {
                m_CurrentSmoothTime += NetworkManager.RealTimeProvider.DeltaTime;
                var transform_ = transform;
                var pct = math.min(m_CurrentSmoothTime / m_SmoothDuration, 1f);
                transform_.position = Vector3.Lerp(m_SmoothFrom.Position, m_SmoothTo.Position, pct);
                transform_.localScale = Vector3.Lerp(m_SmoothFrom.Scale, m_SmoothTo.Scale, pct);
                transform_.rotation = Quaternion.Slerp(m_SmoothFrom.Rotation, m_SmoothTo.Rotation, pct);
                m_AnticipatedTransform = new TransformState
                {
                    Position = transform_.position,
                    Rotation = transform_.rotation,
                    Scale = transform_.localScale
                };
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var transform_ = transform;
            m_AuthorityTransform = new TransformState
            {
                Position = transform_.position,
                Rotation = transform_.rotation,
                Scale = transform_.localScale
            };
            m_AnticipatedTransform = m_AuthorityTransform;
            m_OutstandingAuthorityChange = true;
        }

        internal void SetLastAuthorityUpdateTick()
        {
            m_LastAuthorityUpdateTick = NetworkManager.NetworkTickSystem.AnticipationTick;
        }

        /// <summary>
        /// Interpolate between the transform represented by <see cref="from"/> to the transform represented by
        /// <see cref="to"/> over <see cref="durationSeconds"/> of real time. The duration uses
        /// <see cref="Time.deltaTime"/>, so it is affected by <see cref="Time.timeScale"/>.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="durationSeconds"></param>
        public void Smooth(TransformState from, TransformState to, float durationSeconds)
        {
            m_AnticipatedTransform = from;

            var transform_ = transform;
            transform_.position = from.Position;
            transform_.rotation = from.Rotation;
            transform_.localScale = from.Scale;

            m_SmoothFrom = from;
            m_SmoothTo = to;
            m_SmoothDuration = durationSeconds;
            m_CurrentSmoothTime = 0;
        }

        public delegate void OnReanticipateDelegate(AnticipatedNetworkTransform anticipatedNetworkTransform, TransformState anticipatedValue, double anticipationTick, TransformState authorityValue, double authorityTick);

#pragma warning disable IDE0001
        /// <summary>
        /// Invoked whenever new data is received from the server, unless <see cref="StaleDataHandling"/> is
        /// <see cref="Netcode.StaleDataHandling.Ignore"/> and the data is determined to be stale.
        /// </summary>
#pragma warning restore IDE0001
        public OnReanticipateDelegate OnReanticipate;

        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            // this is called when new data comes from the server
            m_OutstandingAuthorityChange = true;
        }

        protected override void OnTransformUpdated()
        {
            // this is called pretty much every frame and will change the transform
            // If we've overridden the transform with an anticipated state, we need to be able to change it back
            // to the anticipated state (while updating the authority state accordingly) or else
            // call the OnReanticipate callback
            var transform_ = transform;

            var previousAnticipateddTransform = m_AnticipatedTransform;

            // Update authority state to catch any possible interpolation data
            m_AuthorityTransform.Position = transform_.position;
            m_AuthorityTransform.Rotation = transform_.rotation;
            m_AuthorityTransform.Scale = transform_.localScale;

            if (!m_OutstandingAuthorityChange)
            {
                // Keep the anticipated value unchanged, we have no updates from the server at all.
                transform_.position = previousAnticipateddTransform.Position;
                transform_.localScale = previousAnticipateddTransform.Scale;
                transform_.rotation = previousAnticipateddTransform.Rotation;
                return;
            }

            if (StaleDataHandling == StaleDataHandling.Ignore && m_LastAnticipatedTick > m_LastAuthorityUpdateTick)
            {
                // Keep the anticipated value unchanged because it is more recent than the authoritative one.
                transform_.position = previousAnticipateddTransform.Position;
                transform_.localScale = previousAnticipateddTransform.Scale;
                transform_.rotation = previousAnticipateddTransform.Rotation;
                return;
            }

            m_SmoothDuration = 0;
            m_CurrentSmoothTime = 0;
            m_OutstandingAuthorityChange = false;

            m_AnticipatedTransform = m_AuthorityTransform;

            OnReanticipate?.Invoke(this, previousAnticipateddTransform, m_LastAnticipatedTick, m_AuthorityTransform, NetworkManager.NetworkTickSystem.AnticipationTick);
        }
    }
}
