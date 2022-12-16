using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;


namespace TestProject.ManualTests
{

    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class LinearMotionHandler : NetworkBehaviour
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

        private struct ServerStateUpdate
        {
            public int Tick;
            public Vector3 ServerPosition;
            public Vector3 ClientPredictedPosition;
            public Vector3 PrecisionLoss;
        }

        private Dictionary<int, ServerStateUpdate> m_TickPositionTable = new Dictionary<int, ServerStateUpdate>();

        private Vector3 m_Direction;

        private void Awake()
        {
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

                GetComponent<NetworkTransform>().OnTransformStateChanged = OnServerTransformStateChanged;
            }
            else
            {
                ClientPositionVisual.SetActive(false);
                ClientPosition.enabled = true;
                ClientDelta.enabled = true;
                PredictedClient.enabled = true;
                m_ClientSideLastPosition = transform.position;

                GetComponent<NetworkTransform>().OnTransformStateChanged = OnClienTransformStateChanged;
            }
        }

        private void OnServerTransformStateChanged(ref Vector3 updatedPosition, ref Vector3 precisionLoss, int networkTick)
        {
            m_TickPositionTable.Add(networkTick, new ServerStateUpdate() { Tick = networkTick, ServerPosition = transform.position, ClientPredictedPosition = updatedPosition, PrecisionLoss = precisionLoss });
        }

        private void OnClienTransformStateChanged(ref Vector3 updatedPosition, ref Vector3 precisionLoss, int networkTick)
        {
            var currentPosition = transform.position;
            var delta = updatedPosition - currentPosition;
            ClientDelta.text = $"C-Delta: ({delta.x}, {delta.y}, {delta.z})";
            PredictedClient.text = $"Client-Next: ({updatedPosition.x}, {updatedPosition.y}, {updatedPosition.z})";
            m_ClientSideLastPosition = updatedPosition;

            DebugPositionServerRpc(updatedPosition, networkTick);
        }

        public override void OnNetworkDespawn()
        {
            GetComponent<NetworkTransform>().OnTransformStateChanged = null;
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
                ClientPosition.text = $"Client: ({transform.position.x}, {transform.position.y}, {transform.position.z})";
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

            //if (Input.GetKeyDown(KeyCode.UpArrow))
            //{
            //    m_Direction = Vector3.forward;
            //}
            //if (Input.GetKeyDown(KeyCode.DownArrow))
            //{
            //    m_Direction = Vector3.forward * -1f;
            //}
            //if (Input.GetKeyDown(KeyCode.RightArrow))
            //{
            //    m_Direction = Vector3.right;
            //}
            //if (Input.GetKeyDown(KeyCode.LeftArrow))
            //{
            //    m_Direction = Vector3.right * -1f;
            //}
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

        private int m_LastTickSent;

        [ServerRpc(RequireOwnership = false)]
        private void DebugPositionServerRpc(Vector3 position, int tick)
        {
            if (tick == m_LastTickSent)
            {
                Debug.LogWarning($"Client sent two RPCs with the same tick {tick}");
            }

            var serverState = m_TickPositionTable[tick];

            var serverPosition = serverState.ServerPosition;
            var serverClientPredictedPosition = serverState.ClientPredictedPosition;

            var clientDelta = position - serverPosition;
            var serverDelta = serverPosition - serverClientPredictedPosition;

            m_PrecisionLoss = serverState.PrecisionLoss;
            m_ServerPosition = serverPosition;
            m_ServerPredicted = serverClientPredictedPosition;
            m_ClientPosition = position;
            m_ServerDelta = serverDelta;
            m_ClientDelta = clientDelta;

            SetPositionText();

            ClientPositionVisual.transform.position = m_ClientPosition;

            m_TickPositionTable.Remove(tick);
            m_LastTickSent = tick;
        }
    }
}
