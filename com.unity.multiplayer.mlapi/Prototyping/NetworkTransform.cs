using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Messaging;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkTransform")]
    public class NetworkTransform : NetworkBehaviour
    {
        internal class ClientSendInfo
        {
            public float LastSent;
            public Vector3? LastMissedPosition;
            public Quaternion? LastMissedRotation;
        }

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [Range(0, 120)]
        public float FixedSendsPerSecond = 20f;

        /// <summary>
        /// Is the sends per second assumed to be the same across all instances
        /// </summary>
        [Tooltip("This assumes that the SendsPerSecond is synced across clients")]
        public bool AssumeSyncedSends = true;

        /// <summary>
        /// Enable interpolation
        /// </summary>
        [Tooltip("This requires AssumeSyncedSends to be true")]
        public bool InterpolatePosition = true;

        /// <summary>
        /// The distance before snaping to the position
        /// </summary>
        [Tooltip("The transform will snap if the distance is greater than this distance")]
        public float SnapDistance = 10f;

        /// <summary>
        /// Should the server interpolate
        /// </summary>
        public bool InterpolateServer = true;

        /// <summary>
        /// The min meters to move before a send is sent
        /// </summary>
        public float MinMeters = 0.15f;

        /// <summary>
        /// The min degrees to rotate before a send it sent
        /// </summary>
        public float MinDegrees = 1.5f;

        /// <summary>
        /// Enables extrapolation
        /// </summary>
        public bool ExtrapolatePosition = false;

        /// <summary>
        /// The maximum amount of expected send rates to extrapolate over when awaiting new packets.
        /// A higher value will result in continued extrapolation after an object has stopped moving
        /// </summary>
        public float MaxSendsToExtrapolate = 5;

        /// <summary>
        /// The channel to send the data on
        /// </summary>
        [Tooltip("The channel to send the data on. Uses the default channel if left unspecified")]
        public string Channel = null;

        private float m_LerpTime;
        private Vector3 m_LerpStartPos;
        private Quaternion m_LerpStartRot;
        private Vector3 m_LerpEndPos;
        private Quaternion m_LerpEndRot;

        private float m_LastSendTime;
        private Vector3 m_LastSentPos;
        private Quaternion m_LastSentRot;

        private float m_LastReceiveTime;

        /// <summary>
        /// Enables range based send rate
        /// </summary>
        public bool EnableRange;

        /// <summary>
        /// Checks for missed sends without provocation. Provocation being a client inside it's normal SendRate
        /// </summary>
        public bool EnableNonProvokedResendChecks;

        /// <summary>
        /// The curve to use to calculate the send rate
        /// </summary>
        public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);

        private readonly Dictionary<ulong, ClientSendInfo> m_ClientSendInfo = new Dictionary<ulong, ClientSendInfo>();

        /// <summary>
        /// The delegate used to check if a move is valid
        /// </summary>
        /// <param name="clientId">The client id the move is being validated for</param>
        /// <param name="oldPos">The previous position</param>
        /// <param name="newPos">The new requested position</param>
        /// <returns>Returns Whether or not the move is valid</returns>
        public delegate bool MoveValidationDelegate(ulong clientId, Vector3 oldPos, Vector3 newPos);

        /// <summary>
        /// If set, moves will only be accepted if the custom delegate returns true
        /// </summary>
        public MoveValidationDelegate IsMoveValidDelegate = null;

        private void OnValidate()
        {
            if (!AssumeSyncedSends && InterpolatePosition)
                InterpolatePosition = false;
            if (InterpolateServer && !InterpolatePosition)
                InterpolateServer = false;
            if (MinDegrees < 0)
                MinDegrees = 0;
            if (MinMeters < 0)
                MinMeters = 0;
            if (EnableNonProvokedResendChecks && !EnableRange)
                EnableNonProvokedResendChecks = false;
        }

        private float GetTimeForLerp(Vector3 pos1, Vector3 pos2)
        {
            return 1f / DistanceSendrate.Evaluate(Vector3.Distance(pos1, pos2));
        }

        /// <summary>
        /// Registers message handlers
        /// </summary>
        public override void NetworkStart()
        {
            m_LastSentRot = transform.rotation;
            m_LastSentPos = transform.position;

            m_LerpStartPos = transform.position;
            m_LerpStartRot = transform.rotation;

            m_LerpEndPos = transform.position;
            m_LerpEndRot = transform.rotation;
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (NetworkManager.Singleton.NetworkTime - m_LastSendTime >= (1f / FixedSendsPerSecond) && (Vector3.Distance(transform.position, m_LastSentPos) > MinMeters || Quaternion.Angle(transform.rotation, m_LastSentRot) > MinDegrees))
                {
                    m_LastSendTime = NetworkManager.Singleton.NetworkTime;
                    m_LastSentPos = transform.position;
                    m_LastSentRot = transform.rotation;

                    if (IsServer)
                    {
                        ApplyTransformClientRpc(transform.position, transform.rotation.eulerAngles,
                            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsList.Where(c => c.ClientId != OwnerClientId).Select(c => c.ClientId).ToArray() } });
                    }
                    else
                    {
                        SubmitTransformServerRpc(transform.position, transform.rotation.eulerAngles);
                    }
                }
            }
            else
            {
                //If we are server and interpolation is turned on for server OR we are not server and interpolation is turned on
                if ((IsServer && InterpolateServer && InterpolatePosition) || (!IsServer && InterpolatePosition))
                {
                    if (Vector3.Distance(transform.position, m_LerpEndPos) > SnapDistance)
                    {
                        //Snap, set T to 1 (100% of the lerp)
                        m_LerpTime = 1f;
                    }

                    float sendDelay = (IsServer || !EnableRange || !AssumeSyncedSends || NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject == null) ? (1f / FixedSendsPerSecond) : GetTimeForLerp(transform.position, NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.transform.position);
                    m_LerpTime += Time.unscaledDeltaTime / sendDelay;

                    if (ExtrapolatePosition && Time.unscaledTime - m_LastReceiveTime < sendDelay * MaxSendsToExtrapolate)
                        transform.position = Vector3.LerpUnclamped(m_LerpStartPos, m_LerpEndPos, m_LerpTime);
                    else
                        transform.position = Vector3.Lerp(m_LerpStartPos, m_LerpEndPos, m_LerpTime);

                    if (ExtrapolatePosition && Time.unscaledTime - m_LastReceiveTime < sendDelay * MaxSendsToExtrapolate)
                        transform.rotation = Quaternion.SlerpUnclamped(m_LerpStartRot, m_LerpEndRot, m_LerpTime);
                    else
                        transform.rotation = Quaternion.Slerp(m_LerpStartRot, m_LerpEndRot, m_LerpTime);
                }
            }

            if (IsServer && EnableRange && EnableNonProvokedResendChecks)
                CheckForMissedSends();
        }

        [ClientRpc]
        private void ApplyTransformClientRpc(Vector3 position, Vector3 eulerAngles, ClientRpcParams rpcParams = default)
        {
            if (enabled)
            {
                ApplyTransformInternal(position, Quaternion.Euler(eulerAngles));
            }
        }

        private void ApplyTransformInternal(Vector3 position, Quaternion rotation)
        {
            if (!enabled)
                return;

            if (InterpolatePosition && (!IsServer || InterpolateServer))
            {
                m_LastReceiveTime = Time.unscaledTime;
                m_LerpStartPos = transform.position;
                m_LerpStartRot = transform.rotation;
                m_LerpEndPos = position;
                m_LerpEndRot = rotation;
                m_LerpTime = 0;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
        }

        [ServerRpc]
        private void SubmitTransformServerRpc(Vector3 position, Vector3 eulerAngles, ServerRpcParams rpcParams = default)
        {
            if (!enabled)
                return;

            if (IsMoveValidDelegate != null && !IsMoveValidDelegate(rpcParams.Receive.SenderClientId, m_LerpEndPos, position))
            {
                //Invalid move!
                //TODO: Add rubber band (just a message telling them to go back)
                return;
            }

            if (!IsClient)
            {
                // Dedicated server
                ApplyTransformInternal(position, Quaternion.Euler(eulerAngles));
            }

            if (EnableRange)
            {
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (!m_ClientSendInfo.ContainsKey(NetworkManager.Singleton.ConnectedClientsList[i].ClientId))
                    {
                        m_ClientSendInfo.Add(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ClientSendInfo()
                        {
                            LastMissedPosition = null,
                            LastMissedRotation = null,
                            LastSent = 0
                        });
                    }

                    ClientSendInfo info = m_ClientSendInfo[NetworkManager.Singleton.ConnectedClientsList[i].ClientId];
                    Vector3? receiverPosition = NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.transform.position);
                    Vector3? senderPosition = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.position);

                    if ((receiverPosition == null || senderPosition == null && NetworkManager.Singleton.NetworkTime - info.LastSent >= (1f / FixedSendsPerSecond)) || NetworkManager.Singleton.NetworkTime - info.LastSent >= GetTimeForLerp(receiverPosition.Value, senderPosition.Value))
                    {
                        info.LastSent = NetworkManager.Singleton.NetworkTime;
                        info.LastMissedPosition = null;
                        info.LastMissedRotation = null;

                        ApplyTransformClientRpc(position, eulerAngles,
                            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkManager.Singleton.ConnectedClientsList[i].ClientId } } });
                    }
                    else
                    {
                        info.LastMissedPosition = position;
                        info.LastMissedRotation = Quaternion.Euler(eulerAngles);
                    }
                }
            }
            else
            {
                ApplyTransformClientRpc(position, eulerAngles,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsList.Where(c => c.ClientId != OwnerClientId).Select(c => c.ClientId).ToArray() } });
            }
        }

        private void CheckForMissedSends()
        {
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                if (!m_ClientSendInfo.ContainsKey(NetworkManager.Singleton.ConnectedClientsList[i].ClientId))
                {
                    m_ClientSendInfo.Add(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ClientSendInfo()
                    {
                        LastMissedPosition = null,
                        LastMissedRotation = null,
                        LastSent = 0
                    });
                }

                ClientSendInfo info = m_ClientSendInfo[NetworkManager.Singleton.ConnectedClientsList[i].ClientId];
                Vector3? receiverPosition = NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.transform.position);
                Vector3? senderPosition = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.position);

                if ((receiverPosition == null || senderPosition == null && NetworkManager.Singleton.NetworkTime - info.LastSent >= (1f / FixedSendsPerSecond)) || NetworkManager.Singleton.NetworkTime - info.LastSent >= GetTimeForLerp(receiverPosition.Value, senderPosition.Value))
                {
                    /* why is this??? ->*/
                    Vector3? pos = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.position);
                    /* why is this??? ->*/
                    Vector3? rot = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject == null ? null : new Vector3?(NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.transform.rotation.eulerAngles);

                    if (info.LastMissedPosition != null && info.LastMissedRotation != null)
                    {
                        info.LastSent = NetworkManager.Singleton.NetworkTime;

                        ApplyTransformClientRpc(info.LastMissedPosition.Value, info.LastMissedRotation.Value.eulerAngles,
                            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkManager.Singleton.ConnectedClientsList[i].ClientId } } });

                        info.LastMissedPosition = null;
                        info.LastMissedRotation = null;
                    }
                }
            }
        }

        /// <summary>
        /// Teleports the transform to the given position and rotation
        /// </summary>
        /// <param name="position">The position to teleport to</param>
        /// <param name="rotation">The rotation to teleport to</param>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (InterpolateServer && IsServer || IsClient)
            {
                m_LerpStartPos = position;
                m_LerpStartRot = rotation;
                m_LerpEndPos = position;
                m_LerpEndRot = rotation;
                m_LerpTime = 0;
            }
        }
    }
}