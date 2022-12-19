using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

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
        [Range(0.5f, 10.0f)]
        public float Speed = 5.0f;
        public Directions StartingDirection;
        [Range(0.5f, 1000.0f)]
        public float DirectionDuration = 0.5f;
        public GameObject ClientPositionVisual;
        [Tooltip("When true, it will randomly pick a new direction after DirectionDuration period of time has elapsed.")]
        public bool RandomDirections;

        public Text ServerDelta;
        public Text ServerPosition;
        public Text PredictedClient;
        public Text PrecisionLoss;
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
            m_ClientDelta = Vector3.zero;
            m_ServerDelta = Vector3.zero;

            Camera.main.transform.parent = transform;
            ServerPosition.enabled = false;
            PredictedClient.enabled = false;
            PrecisionLoss.enabled = false;
            ClientPosition.enabled = false;
            ServerDelta.enabled = false;
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
                ServerDelta.enabled = true;
                ClientDelta.enabled = true;
                TimeMoving.enabled = true;
                DirectionText.enabled = true;
                PredictedClient.enabled = true;
                PrecisionLoss.enabled = true;
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
                PredictedClient.enabled = true;
                m_ClientSideLastPosition = transform.position;
            }

        }

        private NetworkTransformStateUpdate m_NetworkTransformStateUpdate = new NetworkTransformStateUpdate();

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            base.OnNetworkTransformStateUpdate(ref networkTransformStateUpdate);
            if(!CanCommitToTransform)
            {
                m_NetworkTransformStateUpdate = networkTransformStateUpdate;
            }
        }

        private void UpdateClientPositionInfo()
        {
            var targetPosition = m_NetworkTransformStateUpdate.TargetPosition;
            var currentPosition = transform.position;
            var delta = targetPosition - currentPosition;
            ClientDelta.text = $"C-Delta: ({delta.x}, {delta.y}, {delta.z})";
            PredictedClient.text = $"Client-Next: ({targetPosition.x}, {targetPosition.y}, {targetPosition.z})";
            ClientPosition.text = $"Client: ({transform.position.x}, {transform.position.y}, {transform.position.z})";
            m_ClientSideLastPosition = targetPosition;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        private Vector3 m_ClientSideLastPosition;


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

        // We just apply our current direction with magnitude to our current position during fixed update
        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (IsServer && !m_StopMoving && (NetworkManager.ConnectedClients.Count > (IsHost ? 1 : 0)))
            {
                var position = transform.position;
                var yAxis = position.y;
                position += (m_Direction * Speed);
                position.y = yAxis;
                transform.position = Vector3.Lerp(transform.position, position, Time.fixedDeltaTime);
                transform.rotation = Quaternion.LookRotation(m_Direction);
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
                UpdateClientPositionInfo();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_StopMoving = !m_StopMoving;
            }

            if (!m_StopMoving && (NetworkManager.ConnectedClients.Count > (IsHost ? 1 : 0)))
            {
                UpdateTimeMoving();
            }

            if ((m_TotalTimeMoving - m_DirectionTimeOffset) >= DirectionDuration * 60.0f)
            {
                m_DirectionTimeOffset = m_TotalTimeMoving;
                SetNextDirection();
            }
        }

        private Vector3 m_ServerPosition;
        private Vector3 m_ServerPredicted;
        private Vector3 m_ClientPosition;
        private Vector3 m_ServerDelta;
        private Vector3 m_ClientDelta;
        private Vector3 m_PrecisionLoss;


        private void SetPositionText()
        {
            ServerPosition.text = $"Server: ({m_ServerPosition.x}, {m_ServerPosition.y}, {m_ServerPosition.z})";
            PredictedClient.text = $"Predicted: ({m_ServerPredicted.x}, {m_ServerPredicted.y}, {m_ServerPredicted.z})";
            PrecisionLoss.text = $"Prec-Loss: ({m_PrecisionLoss.x}, {m_PrecisionLoss.y}, {m_PrecisionLoss.z})";
            ClientPosition.text = $"Client: ({m_ClientPosition.x}, {m_ClientPosition.y}, {m_ClientPosition.z})";
            ServerDelta.text = $"S-Delta: ({m_ServerDelta.x}, {m_ServerDelta.y}, {m_ServerDelta.z})";
            ClientDelta.text = $"C-Delta: ({m_ClientDelta.x}, {m_ClientDelta.y}, {m_ClientDelta.z})";
        }

        protected override void OnPositionValidation(ref Vector3 position, ref AuthorityStateUpdate authorityState)
        {
            var clientDelta = position - authorityState.AuthorityPosition;
            var serverDelta = authorityState.AuthorityPosition - authorityState.PredictedPosition;

            m_PrecisionLoss = authorityState.PrecisionLoss;
            m_ServerPosition = authorityState.AuthorityPosition;
            m_ServerPredicted = authorityState.PredictedPosition;
            m_ClientPosition = position;
            m_ServerDelta = serverDelta;
            m_ClientDelta = clientDelta;

            SetPositionText();

            ClientPositionVisual.transform.position = m_ClientPosition;

            base.OnPositionValidation(ref position, ref authorityState);
        }
    }
}
