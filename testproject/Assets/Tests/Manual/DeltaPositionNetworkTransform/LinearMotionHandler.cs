using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TestProject.ManualTests.LinearMotionHandler))]
public class LinearMotionHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif

namespace TestProject.ManualTests
{
    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class LinearMotionHandler : IntegrationNetworkTransform
    {
        public bool SimulateClient = false;
        [Range(0f, 100.0f)]
        public float Speed = 5.0f;
        [Range(0.1f, 1000.0f)]
        public float DirectionDuration = 0.5f;
        public GameObject ClientPositionVisual;
        [Tooltip("When true, it will randomly pick a new direction after DirectionDuration period of time has elapsed.")]
        public bool RandomDirections;

        public bool OnlyMoveBySynchedAxis;
        public bool RandomAxisSync = true;

        public Vector3 PositionOffset;

        public Text ServerPosition;
        public Text ServerDelta;
        public Text ServerCurrent;
        public Text ServerFull;
        public Text ClientPosition;
        public Text ClientDelta;
        public Text TimeMoving;
        public Text DirectionText;

        private bool m_StopMoving;
        private float m_TotalTimeMoving;
        private float m_DirectionTimeOffset;

        private Vector3 m_Direction;


        private NetworkDeltaPosition m_HalfVector3SimulatedClient = new NetworkDeltaPosition();

        private NetworkDeltaPosition m_HalfVector3Server = new NetworkDeltaPosition();

        protected override void Awake()
        {
            base.Awake();
            Camera.main.transform.parent = transform;
            transform.position += PositionOffset;
            m_ClientPosition = transform.position;
            m_HalfVector3SimulatedClient = new NetworkDeltaPosition(m_ClientPosition, 0);
            m_HalfVector3Server = new NetworkDeltaPosition(m_ClientPosition, 0);
            m_ServerPosition = transform.position;
            m_LastInterpolateState = Interpolate;
            ServerPosition.enabled = false;
            ClientPosition.enabled = false;
            ClientDelta.enabled = false;
            TimeMoving.enabled = false;
            DirectionText.enabled = false;
            ServerDelta.enabled = false;
            ServerCurrent.enabled = false;
            ServerFull.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                ServerPosition.enabled = true;
                ClientPosition.enabled = true;
                ClientDelta.enabled = true;
                TimeMoving.enabled = true;
                DirectionText.enabled = true;
                if (SimulateClient)
                {
                    m_StopMoving = true;
                    ServerDelta.enabled = true;
                    ServerCurrent.enabled = true;
                    ServerFull.enabled = true;
                    NetworkManager.NetworkTickSystem.Tick += NetworkTickSystem_Tick;
                }
                SetNextDirection(true);
                SetPositionText();
                UpdateTimeMoving();
            }
            else
            {
                ClientPositionVisual.SetActive(false);
                ClientPosition.enabled = true;
                ClientDelta.enabled = true;
            }
        }

        private bool m_LastInterpolateState;

        private void NetworkTickSystem_Tick()
        {
            if (SimulateClient)
            {
                var position = InLocalSpace ? transform.localPosition : transform.position;
                var isPositionDirty = m_LastInterpolateState != Interpolate;
                var deltaPosition = m_HalfVector3Server.GetDeltaPosition();
                ServerDelta.text = $"S-Delta:{GetVector3AsString(ref deltaPosition)}";
                var currentBasePosition = m_HalfVector3Server.GetCurrentBasePosition();
                ServerCurrent.text = $"S-Curr:{GetVector3AsString(ref currentBasePosition)}";
                var fullPosition = m_HalfVector3Server.GetFullPosition();
                ServerFull.text = $"S-Full:{GetVector3AsString(ref fullPosition)}";


                if (isPositionDirty)
                {
                    m_HalfVector3SimulatedClient = new NetworkDeltaPosition(position, NetworkManager.ServerTime.Tick);
                    m_HalfVector3Server = new NetworkDeltaPosition(position, NetworkManager.ServerTime.Tick);
                    m_ClientPosition = position;
                    OnNonAuthorityUpdatePositionServerRpc(m_ClientPosition);
                    m_LastInterpolateState = Interpolate;
                    return;
                }


                var delta = position - m_HalfVector3Server.GetFullPosition();
                for (int i = 0; i < 3; i++)
                {
                    if (Mathf.Abs(delta[i]) >= PositionThreshold)
                    {
                        isPositionDirty = true;
                        break;
                    }
                }
                if (isPositionDirty)
                {
                    m_HalfVector3Server.UpdateFrom(ref position, NetworkManager.ServerTime.Tick);
                    m_HalfVector3SimulatedClient.HalfVector3.Axis = m_HalfVector3Server.HalfVector3.Axis;
                    m_ClientPosition = m_HalfVector3SimulatedClient.ToVector3(NetworkManager.ServerTime.Tick);
                    OnNonAuthorityUpdatePositionServerRpc(m_ClientPosition);
                }
            }
        }

        private NetworkTransformStateUpdate m_NetworkTransformStateUpdate = new NetworkTransformStateUpdate();

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            base.OnNetworkTransformStateUpdate(ref networkTransformStateUpdate);
            if (!CanCommitToTransform)
            {
                m_NetworkTransformStateUpdate = networkTransformStateUpdate;
            }
        }

        private string GetVector3AsString(ref Vector3 vector)
        {
            return $"({vector.x:0.000}, {vector.y:0.000}, {vector.z:0.000})";
        }

        private void UpdateClientPositionInfo()
        {
            var targetPosition = m_NetworkTransformStateUpdate.Position;
            var currentPosition = InLocalSpace ? transform.localPosition : transform.position;
            var delta = targetPosition - currentPosition;
            if (Interpolate)
            {
                ClientDelta.text = $"C-Delta: {GetVector3AsString(ref delta)}";
            }
            else
            {
                ClientDelta.text = "--Interpolate Off--";
            }
            ClientPosition.text = $"Client: {GetVector3AsString(ref currentPosition)}";
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        private float GetRandomAxisDirection()
        {
            var randVal = Random.Range(-1.0f, 1.0f);
            if (Mathf.Abs(randVal) >= 0.5f)
            {
                return Mathf.Sign(randVal);
            }
            return 0.0f;
        }

        private void SetNextDirection(bool useCurrent = false)
        {
            m_Direction.x = GetRandomAxisDirection();
            m_Direction.y = GetRandomAxisDirection();
            m_Direction.z = GetRandomAxisDirection();

            var invalidSettings = true;
            while (invalidSettings)
            {
                if (RandomAxisSync)
                {
                    if (Mathf.Abs(m_Direction.x) > 0.5f)
                    {
                        if (Random.Range(0f, 1.0f) >= 0.50f)
                        {
                            SyncPositionX = false;
                        }
                    }
                    else
                    {
                        SyncPositionX = true;
                    }

                    if (Mathf.Abs(m_Direction.y) > 0.5f)
                    {
                        if (Random.Range(0f, 1.0f) >= 0.50f)
                        {
                            SyncPositionY = false;
                        }
                    }
                    else
                    {
                        SyncPositionY = true;
                    }

                    if (Mathf.Abs(m_Direction.z) > 0.5f)
                    {
                        if (Random.Range(0f, 1.0f) >= 0.50f)
                        {
                            SyncPositionZ = false;
                        }
                    }
                    else
                    {
                        SyncPositionZ = true;
                    }
                }

                // Just make sure at least one axis is enabled and the direction vector on that axis is 1 or -1.
                if ((SyncPositionX && Mathf.Abs(m_Direction.x) > 0.5f) || (SyncPositionY && Mathf.Abs(m_Direction.y) > 0.5f)
                    || (Mathf.Abs(m_Direction.z) > 0.5f && SyncPositionZ))
                {
                    invalidSettings = false;
                }
                else // Try again
                {
                    m_Direction.x = GetRandomAxisDirection();
                    m_Direction.y = GetRandomAxisDirection();
                    m_Direction.z = GetRandomAxisDirection();
                }
            }
            DirectionText.text = $"Dir: {m_Direction} | Sync: ({SyncPositionX},{SyncPositionY},{SyncPositionZ})";
        }

        private bool ShouldRun()
        {
            return (NetworkManager.ConnectedClients.Count > (IsHost ? 1 : 0)) || SimulateClient;
        }


        // We just apply our current direction with magnitude to our current position during fixed update
        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (IsServer && ShouldRun())
            {
                var position = transform.position;
                var rotation = transform.rotation;
                //var yAxis = position.y;
                if (!m_StopMoving)
                {
                    position += (m_Direction * Speed);
                }
                else
                {
                    position += m_DecayToStop;
                    m_DecayToStop = Vector3.Lerp(m_DecayToStop, Vector3.zero, Time.fixedDeltaTime * 16.0f);
                    if (m_DecayToStop.magnitude < 0.01f)
                    {
                        m_DecayToStop = Vector3.zero;
                    }
                }
                //position.y = yAxis;
                position = Vector3.Lerp(transform.position, position, Time.fixedDeltaTime);
                rotation = Quaternion.LookRotation(m_Direction);
                if (!OnlyMoveBySynchedAxis)
                {
                    transform.position = position;
                }
                else
                {
                    var currentPosition = transform.position;
                    if (SyncPositionX)
                    {
                        currentPosition.x = position.x;
                    }

                    if (SyncPositionY)
                    {
                        currentPosition.y = position.y;
                    }

                    if (SyncPositionZ)
                    {
                        currentPosition.z = position.z;
                    }
                    transform.position = currentPosition;
                }
                transform.rotation = rotation;
            }
        }



        private void UpdateTimeMoving()
        {
            m_TotalTimeMoving += Time.deltaTime;
            TimeMoving.text = $"Time Moving: {m_TotalTimeMoving}";
        }

        private Vector3 m_DecayToStop;
        private bool m_WasInterpolating;
        private void LateUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (!IsServer)
            {
                var position = InLocalSpace ? transform.localPosition : transform.position;
                OnNonAuthorityUpdatePositionServerRpc(position);

                UpdateClientPositionInfo();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_StopMoving = !m_StopMoving;
                if (m_StopMoving)
                {
                    SyncPositionX = true;
                    SyncPositionY = true;
                    SyncPositionZ = true;
                    m_WasInterpolating = Interpolate;
                    Interpolate = false;
                    m_DecayToStop = (m_Direction * Speed);
                }
                else
                {
                    Interpolate = m_WasInterpolating;
                    SetNextDirection();
                }
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                RandomAxisSync = !RandomAxisSync;
                if (!RandomAxisSync)
                {
                    SyncPositionX = true;
                    SyncPositionY = true;
                    SyncPositionZ = true;
                }
                else
                {
                    SetNextDirection();
                }
            }

            if (!m_StopMoving && ShouldRun())
            {
                UpdateTimeMoving();
            }

            if ((m_TotalTimeMoving - m_DirectionTimeOffset) >= DirectionDuration * 60.0f)
            {
                m_DirectionTimeOffset = m_TotalTimeMoving;
                SetNextDirection();
            }

            ClientPositionVisual.transform.position = m_ClientPosition;
            ClientPositionVisual.transform.rotation = InLocalSpace ? transform.localRotation : transform.rotation;
        }

        private Vector3 m_ServerPosition;
        private Vector3 m_ClientPosition;
        private Vector3 m_ClientDelta;


        private void SetPositionText()
        {
            ServerPosition.text = $"Server: {GetVector3AsString(ref m_ServerPosition)}";
            ClientPosition.text = $"Client: {GetVector3AsString(ref m_ClientPosition)}";
            ClientDelta.text = $"C-Delta: {GetVector3AsString(ref m_ClientDelta)}";
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnNonAuthorityUpdatePositionServerRpc(Vector3 position)
        {
            m_ClientPosition = position;
            UpdatePositionValidation();
        }

        private void UpdatePositionValidation()
        {
            m_ServerPosition = InLocalSpace ? transform.localPosition : transform.position;
            m_ClientDelta = m_ClientPosition - m_ServerPosition;

            SetPositionText();
        }
    }
}
