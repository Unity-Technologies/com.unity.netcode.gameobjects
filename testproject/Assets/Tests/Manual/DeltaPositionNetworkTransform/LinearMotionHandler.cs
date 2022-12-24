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
        [Range(0.5f, 10.0f)]
        public float Speed = 5.0f;
        public Directions StartingDirection;
        [Range(0.1f, 1000.0f)]
        public float DirectionDuration = 0.5f;
        public GameObject ClientPositionVisual;
        [Tooltip("When true, it will randomly pick a new direction after DirectionDuration period of time has elapsed.")]
        public bool RandomDirections;

        public Text ServerPosition;
        public Text ClientPosition;
        public Text ClientDelta;
        public Text TimeMoving;
        public Text DirectionText;

        public enum Directions
        {
            Forward,
            ForwardRight,
            Right,
            BackwardRight,
            Backward,
            BackwardLeft,
            Left,
            ForwardLeft,
        }

        private Directions m_CurrentDirection;
        private float m_DirectionTimeOffset;

        private Vector3 m_Direction;

        protected override void Awake()
        {
            base.Awake();

            m_ServerPosition = transform.position;
            m_ClientPosition = transform.position;
            m_HalfVector3Simulated = new HalfVector3(m_ClientPosition);
            Camera.main.transform.parent = transform;
            ServerPosition.enabled = false;
            ClientPosition.enabled = false;
            ClientDelta.enabled = false;
            TimeMoving.enabled = false;
            DirectionText.enabled = false;
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
                m_CurrentDirection = StartingDirection;
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

        private NetworkTransformStateUpdate m_NetworkTransformStateUpdate = new NetworkTransformStateUpdate();

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            base.OnNetworkTransformStateUpdate(ref networkTransformStateUpdate);
            if (!CanCommitToTransform)
            {
                m_NetworkTransformStateUpdate = networkTransformStateUpdate;
            }
        }

        private void UpdateClientPositionInfo()
        {
            var targetPosition = m_NetworkTransformStateUpdate.Position;
            var currentPosition = InLocalSpace ? transform.localPosition : transform.position;
            var delta = targetPosition - currentPosition;
            if (Interpolate)
            {
                ClientDelta.text = $"C-Delta: ({delta.x}, {delta.y}, {delta.z})";
            }
            else
            {
                ClientDelta.text = "--Interpolate Off--";
            }
            ClientPosition.text = $"Client: ({currentPosition.x}, {currentPosition.y}, {currentPosition.z})";
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }


        private void SetNextDirection(bool useCurrent = false)
        {
            if (!useCurrent)
            {
                var directions = System.Enum.GetValues(typeof(Directions));
                // If not using random directions, then move to the next
                // or roll over.
                if (!RandomDirections)
                {
                    int currentDirection = (int)m_CurrentDirection;
                    currentDirection++;

                    currentDirection = currentDirection % (directions.Length - 1);
                    m_CurrentDirection = (Directions)currentDirection;
                }
                else
                {
                    m_CurrentDirection = (Directions)Random.Range(0, directions.Length - 1);
                }
            }

            DirectionText.text = $"Direction: {m_CurrentDirection}";

            switch (m_CurrentDirection)
            {
                case Directions.Forward:
                    {
                        m_Direction = Vector3.forward;
                        break;
                    }
                case Directions.ForwardRight:
                    {
                        m_Direction = Vector3.forward + Vector3.right;
                        break;
                    }
                case Directions.Right:
                    {
                        m_Direction = Vector3.right;
                        break;
                    }
                case Directions.BackwardRight:
                    {
                        m_Direction = Vector3.back + Vector3.right;
                        break;
                    }
                case Directions.Backward:
                    {
                        m_Direction = Vector3.back;
                        break;
                    }
                case Directions.BackwardLeft:
                    {
                        m_Direction = Vector3.back + Vector3.left;
                        break;
                    }
                case Directions.Left:
                    {
                        m_Direction = Vector3.left;
                        break;
                    }
                case Directions.ForwardLeft:
                    {
                        m_Direction = Vector3.forward + Vector3.left;
                        break;
                    }
            }

        }

        private bool ShouldRun()
        {
            return (NetworkManager.ConnectedClients.Count > (IsHost ? 1 : 0)) || SimulateClient;
        }


        private HalfVector3 m_HalfVector3Simulated = new HalfVector3();
        // We just apply our current direction with magnitude to our current position during fixed update
        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (IsServer && !m_StopMoving && ShouldRun())
            {
                var position = transform.position;
                var rotation = transform.rotation;
                var yAxis = position.y;
                position += (m_Direction * Speed);
                position.y = yAxis;
                position = Vector3.Lerp(transform.position, position, Time.fixedDeltaTime);
                rotation = Quaternion.LookRotation(m_Direction);
                transform.position = position;
                transform.rotation = rotation;

                if (SimulateClient)
                {
                    m_HalfVector3Simulated.FromVector3(ref position);
                    m_ClientPosition = m_HalfVector3Simulated.ToVector3();
                    OnNonAuthorityUpdatePositionServerRpc(m_ClientPosition);
                }
            }
        }

        private bool m_StopMoving;
        private float m_TotalTimeMoving;

        private void UpdateTimeMoving()
        {
            m_TotalTimeMoving += Time.deltaTime;
            TimeMoving.text = $"Time Moving: {m_TotalTimeMoving}";
        }

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
            ServerPosition.text = $"Server: ({m_ServerPosition.x}, {m_ServerPosition.y}, {m_ServerPosition.z})";
            ClientPosition.text = $"Client: ({m_ClientPosition.x}, {m_ClientPosition.y}, {m_ClientPosition.z})";
            ClientDelta.text = $"C-Delta: ({m_ClientDelta.x}, {m_ClientDelta.y}, {m_ClientDelta.z})";
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
