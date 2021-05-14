using System;
using System.Collections.Generic;
using MLAPI.Timing;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkTimeSample : MonoBehaviour
{
    private int k_Tickrate = 100;

    [SerializeField]
    private Graph m_TimeDifferenceGraph;

    [SerializeField]
    private Graph m_ClientSpeedUpGraph;

    [SerializeField]
    private Graph m_ServerSpeedUpGraph;

    [SerializeField]
    private Graph m_RttGraph;

    [SerializeField]
    private GameObject m_OwnPlayerT;
    [SerializeField]
    private GameObject m_OtherPlayerT;

    [SerializeField]
    private GameObject m_OwnPlayerNt;
    [SerializeField]
    private GameObject m_OtherPlayerNt;

    [SerializeField]
    private Color m_LabelColor = Color.black;

    private float m_CurrentRtt = 500f / 1000f;

    //config
    public float Jitter { get; set; } = 200f / 1000f;

    public float Rtt { get; set; } = 500f / 1000f;

    // Shared
    private NetworkTime m_ServerTime;
    private LatencySimulationQueue<Message> m_MessageQueue = new LatencySimulationQueue<Message>();
    private Dictionary<int, float> m_ReceivedPositions = new Dictionary<int, float>();
    // player position server (invisible
    private float m_ServerPlayerPosition;
    private GUIStyle m_LabelTextStyle;

    // New Network Time
    private DummyNetworkStats m_NetworkStats = new DummyNetworkStats();
    private NetworkTimeSystem m_NetworkTimeClient;

    // Old Network Time
    private float m_TimeResyncInterval = 5f;
    private float m_LastResync = float.NegativeInfinity;
    private float m_TimeOffset = 0f;

    private float m_ConfigJitter;
    private float m_ConfigRtt;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(new Vector2(10, 10), new Vector2(200, 200)));

        m_LabelTextStyle.normal.textColor = m_LabelColor;

        GUILayout.Label("Jitter", m_LabelTextStyle);
        var jitter = m_ConfigJitter;
        var newJitterText = GUILayout.TextField(jitter.ToString());
        if (float.TryParse(newJitterText, out jitter))
        {
            m_ConfigJitter = jitter;
        }

        GUILayout.Label("Rtt", m_LabelTextStyle);
        var rtt = m_ConfigRtt;
        var newRttText = GUILayout.TextField(rtt.ToString());
        if (float.TryParse(newRttText, out rtt))
        {
            m_ConfigRtt = rtt;
        }

        if (GUILayout.Button("Apply"))
        {
            Jitter = m_ConfigJitter / 1000f;
            Rtt = m_ConfigRtt / 1000f;
        }

        GUILayout.EndArea();
    }

    // Start is called before the first frame update
    private void Start()
    {
        m_ConfigJitter = Jitter * 1000f;
        m_ConfigRtt = Rtt * 1000f;

        m_LabelTextStyle = new GUIStyle(GUIStyle.none);
        UpdateRtt();
        Time.fixedDeltaTime = 1f / k_Tickrate;
        m_ServerTime = new NetworkTime(k_Tickrate, Time.fixedTime);
        m_NetworkTimeClient = new NetworkTimeSystem(k_Tickrate, false, m_NetworkStats);
        m_NetworkTimeClient.InitializeClient(m_ServerTime.Tick);

        m_ClientSpeedUpGraph.Max = 1.02f;
        m_ClientSpeedUpGraph.Min = 0.98f;

        m_ServerSpeedUpGraph.Max = 1.02f;
        m_ServerSpeedUpGraph.Min = 0.98f;

        m_RttGraph.Max = 400f;
    }

    private void UpdateRtt()
    {
        m_CurrentRtt = Rtt + Random.Range(-Jitter / 2f, Jitter / 2f);
    }

    private void FixedUpdate()
    {
        UpdateRtt();

        m_ServerTime += Time.deltaTime;
        m_MessageQueue.Time = m_ServerTime.Time;

        // Update server player position then send message
        m_ServerPlayerPosition = Mathf.PingPong((m_ServerTime.Time) / 5f, 1) * 10 - 5;
        m_MessageQueue.SendMessage(m_CurrentRtt / 2f, new Message(m_ServerTime.Tick, m_ServerPlayerPosition));

        ReceiveMessages();

        float posX = 0;

        // ___ New Network Time

        // own player
        m_NetworkTimeClient.AdvanceNetworkTime(Time.fixedDeltaTime);
        //Debug.Log($"predicted: {m_NetworkTimeClient.PredictedTime.Time} server: {m_NetworkTimeClient.ServerTime.Time} actual server: {m_ServerTime.Time}");
        m_NetworkStats.Rtt = m_CurrentRtt;
        posX = Mathf.PingPong((m_NetworkTimeClient.PredictedTime.Time) / 5f, 1) * 10 - 5;
        m_OwnPlayerNt.transform.position = new Vector3(posX, m_OwnPlayerNt.transform.position.y, 0);

        // other player
        posX = GetPositionForTick(m_NetworkTimeClient.ServerTime.Tick - 1);
        m_OtherPlayerNt.transform.position = new Vector3(posX, m_OtherPlayerNt.transform.position.y, 0);


        // ___ Old Network Time provider

        // time resync
        if (m_LastResync + m_TimeResyncInterval < m_ServerTime.Time)
        {
            m_LastResync = m_ServerTime.Time;
            m_TimeOffset = m_CurrentRtt / 2f;
        }

        float clientTime = m_ServerTime.Time + m_TimeOffset;

        // own player
        posX = Mathf.PingPong((clientTime) / 5f, 1) * 10 - 5;
        m_OwnPlayerT.transform.position = new Vector3(posX, m_OwnPlayerT.transform.position.y, 0);

        // other player

        // borrow last received tick from new system for simplicity
        posX = GetPositionForTick(m_NetworkStats.LastReceivedSnapshotTick.Tick);
        m_OtherPlayerT.transform.position = new Vector3(posX, m_OtherPlayerT.transform.position.y, 0);


        ClientNetworkTimeProvider clientTimeProvider = (ClientNetworkTimeProvider)m_NetworkTimeClient.NetworkTimeProvider;
        m_TimeDifferenceGraph.UpdateGraph((m_NetworkTimeClient.PredictedTime - m_NetworkTimeClient.ServerTime).Time);
        m_ClientSpeedUpGraph.UpdateGraph(clientTimeProvider.PredictedTimeScale);
        m_ServerSpeedUpGraph.UpdateGraph(clientTimeProvider.ServerTimeScale);
        m_RttGraph.UpdateGraph(m_CurrentRtt * 1000f);
    }

    private void ReceiveMessages()
    {
        var list = m_MessageQueue.ReceiveMessages();

        foreach (var message in list)
        {
            if (message.Tick > m_NetworkStats.LastReceivedSnapshotTick.Tick)
            {
                m_NetworkStats.LastReceivedSnapshotTick = new NetworkTime(k_Tickrate, message.Tick);
            }

            m_ReceivedPositions[message.Tick] = message.Position;
        }
    }

    private float GetPositionForTick(int tick)
    {
        if (m_ReceivedPositions.TryGetValue(tick, out float value))
        {
            return value;
        }

        int last = int.MinValue;
        int secondLast = int.MinValue;

        foreach (var positions in m_ReceivedPositions)
        {
            if (positions.Key > last)
            {
                secondLast = last;
                last = positions.Key;
            }
        }

        if (secondLast != int.MinValue /*&& Mathf.Abs(tick - last) <= 10*/ )
        {
            //Debug.Log($"dif:{tick - last} cur {tick} last {last}");

            // interpolate
            float v0 = m_ReceivedPositions[secondLast];
            float v1 = m_ReceivedPositions[last];

            float t = (float)(tick - secondLast) / (last - secondLast);

            return Mathf.LerpUnclamped(v0, v1, t);
        }

        Debug.LogError("Unable to interpolate");
        return -400f;

        //throw new InvalidOperationException();
    }

    private struct Message
    {
        public int Tick { get; set; }
        public float Position { get; set; }

        public Message(int tick, float position)
        {
            Tick = tick;
            Position = position;
        }
    }

    private class LatencySimulationQueue<T>
    {
        private List<(float, T)> m_MessageQueue = new List<(float, T)>();

        public float Time { get; set; }

        public void SendMessage(float artificialDelay, T message)
        {
            m_MessageQueue.Add((Time + artificialDelay, message));
        }

        public List<T> ReceiveMessages()
        {
            var list = new List<T>();
            for (var i = m_MessageQueue.Count - 1; i >= 0; i--)
            {
                var message = m_MessageQueue[i];
                if (message.Item1 < Time)
                {
                    m_MessageQueue.RemoveAt(i);
                    list.Add(message.Item2);
                }
            }

            return list;
        }
    }

    private class DummyNetworkStats: INetworkStats
    {
        public float Rtt { get; set; }

        public NetworkTime LastReceivedSnapshotTick { get; set; }

        public float GetRtt()
        {
            return Rtt;
        }

        public NetworkTime GetLastReceivedSnapshotTick()
        {
            return LastReceivedSnapshotTick.ToFixedTime();
        }
    }
}
