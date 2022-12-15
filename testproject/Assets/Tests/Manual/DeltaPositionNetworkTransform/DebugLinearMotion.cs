//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
//using Unity.Netcode.Components;


namespace TestProject.ManualTests
{

    /// <summary>
    /// Used with GenericObjects to randomly move them around
    /// </summary>
    public class DebugLinearMotion : NetworkBehaviour
    {
        [Range(0.5f, 10.0f)]
        public float Speed = 5.0f;
        public Directions StartingDirection;
        [Range(1, 1000)]
        public int DirectionDuration = 1;
        public GameObject ClientPositionVisual;

        public Text ServerDelta;
        public Text ServerPosition;
        public Text ClientPosition;
        public Text ClientDelta;
        public Text TimeMoving;
        public Text DirectionText;
        public Text PrecisionOffset;

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

        private Dictionary<int, Vector3> m_TickPositionTable = new Dictionary<int, Vector3>();

        private Vector3 m_Direction;

        private void Awake()
        {
            m_ServerPosition = transform.position;
            m_ClientPosition = transform.position;
            m_ClientDelta = Vector3.zero;
            m_ServerDelta = Vector3.zero;

            Camera.main.transform.parent = transform;
            ServerPosition.enabled = false;
            ClientPosition.enabled = false;
            ServerDelta.enabled = false;
            ClientDelta.enabled = false;
            TimeMoving.enabled = false;
            DirectionText.enabled = false;
            PrecisionOffset.enabled = false;
            //GetComponent<NetworkTransform>().enabled = false;
        }

        private int m_LastTick;
        private Vector3 m_LastPosition;
        public override void OnNetworkSpawn()
        {
            NetworkManager.NetworkTickSystem.Tick += NetworkTickSystem_Tick;

            if (IsServer)
            {
                ServerPosition.enabled = true;
                ClientPosition.enabled = true;
                ServerDelta.enabled = true;
                ClientDelta.enabled = true;
                TimeMoving.enabled = true;
                DirectionText.enabled = true;
                PrecisionOffset.enabled = true;
                m_CurrentDirection = StartingDirection;
                SetNextDirection(true);
                SetPositionText();
                UpdateTimeMoving();
                m_LastPosition = transform.position;
            }
            else
            {
                ClientPositionVisual.SetActive(false);
                ClientPosition.enabled = true;
                ClientDelta.enabled = true;
                m_ClientSideLastPosition = transform.position;
            }
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
            base.OnNetworkDespawn();
        }

        private Vector3 m_ClientSideLastPosition;

        private Vector3 m_DeltaPrecisionOffset;

        private void NetworkTickSystem_Tick()
        {
            if (!IsSpawned)
            {
                return;
            }
            if (m_TickPositionQueue.Count > 0)
            {
                var tickPosition = m_TickPositionQueue.Dequeue();
                ProcessQueueItem(ref tickPosition);
            }

            if (!IsServer)
            {
                var delta = transform.position - m_ClientSideLastPosition;
                ClientPosition.text = $"Client: ({transform.position.x}, {transform.position.y}, {transform.position.z})";
                ClientDelta.text = $"C-Delta: ({delta.x}, {delta.y}, {delta.z})";
                m_ClientSideLastPosition = transform.position;
                if (delta.magnitude > 0.000001f)
                {
                    DebugPositionServerRpc(transform.position, NetworkManager.NetworkTickSystem.ServerTime.Tick);
                }
            }
            else
            {
                var delta = transform.position - m_LastPosition;
                if (Mathf.Abs(delta.x) > 0.001f || Mathf.Abs(delta.y) > 0.001f || Mathf.Abs(delta.z) > 0.001f)
                {
                    m_LastTick = NetworkManager.NetworkTickSystem.ServerTime.Tick;
                    m_TickPositionTable.Add(NetworkManager.NetworkTickSystem.ServerTime.Tick, transform.position);

                    if (m_TickPositionTable.ContainsKey(m_LastTick - 1))
                    {
                        var previousPosition = m_TickPositionTable[m_LastTick - 1];
                        var nextPosition = m_TickPositionTable[m_LastTick];
                        var deltaPosition = (nextPosition - previousPosition) - m_DeltaPrecisionOffset;
                        var decompressedDelta = Vector3.zero;
                        Vector3DeltaCompressor.CompressDelta(ref deltaPosition, ref m_CompressedVector3Delta);
                        Vector3DeltaCompressor.DecompressDelta(ref decompressedDelta, ref m_CompressedVector3Delta);
                        m_DeltaPrecisionOffset = deltaPosition - decompressedDelta;
                        PrecisionOffset.text = $"Precision Offset: ({m_DeltaPrecisionOffset.x},{m_DeltaPrecisionOffset.y},{m_DeltaPrecisionOffset.z})";
                        previousPosition += decompressedDelta;
                        DebugPositionServerRpc(previousPosition, m_LastTick);
                        m_LastPosition = transform.position;
                    }
                }
            }
        }


        private void SetNextDirection(bool useCurrent = false)
        {
            if (!useCurrent)
            {
                int currentDirection = (int)m_CurrentDirection;
                currentDirection++;
                var directions = System.Enum.GetValues(typeof(Directions));
                currentDirection = currentDirection % (directions.Length - 1);
                m_CurrentDirection = (Directions)currentDirection;
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
            if (IsServer && !m_StopMoving)
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

        private CompressedVector3Delta m_CompressedVector3Delta = new CompressedVector3Delta();

        private void LateUpdate()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_StopMoving = !m_StopMoving;
                if (!m_StopMoving)
                {
                    m_LastTick = NetworkManager.NetworkTickSystem.ServerTime.Tick - 1;
                }
            }

            if (!m_StopMoving)
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
        private Vector3 m_ClientPosition;
        private Vector3 m_ServerDelta;
        private Vector3 m_ClientDelta;


        private void SetPositionText()
        {
            ServerPosition.text = $"Server: ({m_ServerPosition.x}, {m_ServerPosition.y}, {m_ServerPosition.z})";
            ClientPosition.text = $"Client: ({m_ClientPosition.x}, {m_ClientPosition.y}, {m_ClientPosition.z})";
            ServerDelta.text = $"S-Delta: ({m_ServerDelta.x}, {m_ServerDelta.y}, {m_ServerDelta.z})";
            ClientDelta.text = $"C-Delta: ({m_ClientDelta.x}, {m_ClientDelta.y}, {m_ClientDelta.z})";
        }


        private struct TickPosition
        {
            public int Tick;
            public Vector3 Position;
        }


        private Queue<TickPosition> m_TickPositionQueue = new Queue<TickPosition>();

        //private Coroutine m_TickPositionCoroutine;

        [ServerRpc(RequireOwnership = false)]
        private void DebugPositionServerRpc(Vector3 position, int tick)
        {
            m_TickPositionQueue.Enqueue(new TickPosition() { Tick = tick, Position = position });
            //if (m_TickPositionCoroutine == null)
            //{
            //    m_TickPositionCoroutine = StartCoroutine(ProcessPositionQueue());
            //}
        }

        //private  IEnumerator ProcessPositionQueue()
        //{
        //    var waitTime = new WaitForSeconds(1.0f / NetworkManager.NetworkConfig.TickRate);

        //    while(m_TickPositionQueue.Count > 0)
        //    {
        //        yield return waitTime;
        //        var tickPosition = m_TickPositionQueue.Dequeue();
        //        ProcessQueueItem(ref tickPosition);
        //    }

        //    m_TickPositionCoroutine = null;

        //    yield break;

        //}

        private void ProcessQueueItem(ref TickPosition tickPosition)
        {
            var serverPosition = Vector3.zero;
            var previousServerPosition = Vector3.zero;
            var tickToCompare = tickPosition.Tick;
            var previousTick = tickToCompare - 1;
            if (!m_TickPositionTable.ContainsKey(tickToCompare))
            {
                tickToCompare = tickPosition.Tick;
                previousTick = tickToCompare - 1;
            }
            if (!m_TickPositionTable.ContainsKey(previousTick))
            {
                previousTick = tickToCompare;
            }

            serverPosition = m_TickPositionTable[tickToCompare];
            previousServerPosition = m_TickPositionTable[previousTick];

            var clientDelta = tickPosition.Position - serverPosition;
            var serverDelta = serverPosition - previousServerPosition;

            m_ServerPosition = serverPosition;
            m_ClientPosition = tickPosition.Position;
            m_ServerDelta = serverDelta;
            m_ClientDelta = clientDelta;

            SetPositionText();

            ClientPositionVisual.transform.position = m_ClientPosition;

            m_TickPositionTable.Remove(previousTick);
        }
    }
}
