using System.Collections.Generic;
using System.IO;
using MLAPI.Profiling;
using MLAPI.Serialization;
using UnityEngine;

namespace UnityEditor
{
    public class MLAPIProfiler : EditorWindow
    {
#if !UNITY_2020_2_OR_LATER
        [MenuItem("Window/MLAPI Profiler")]
        public static void ShowWindow()
        {
            GetWindow<MLAPIProfiler>();
        }
#endif

        private static GUIStyle s_WrapStyle
        {
            get
            {
                Color color = EditorStyles.label.normal.textColor;
                GUIStyle style = EditorStyles.centeredGreyMiniLabel;
                style.wordWrap = true;
                style.normal.textColor = color;
                return style;
            }
        }

        private float m_HoverAlpha = 0f;
        private float m_UpdateDelay = 1f;
        private int m_CaptureCount = 100;
        private float m_ShowMax = 0;
        private float m_ShowMin = 0;
        private AnimationCurve m_Curve = AnimationCurve.Linear(0, 0, 1, 0);
        private readonly List<ProfilerTick> m_CurrentTicks = new List<ProfilerTick>();
        private float m_LastDrawn = 0;

        private class ProfilerContainer
        {
            public ProfilerTick[] Ticks;

            public byte[] ToBytes()
            {
                var buffer = new NetworkBuffer();
                var writer = new NetworkWriter(buffer);

                writer.WriteUInt16Packed((ushort)Ticks.Length);

                for (int i = 0; i < Ticks.Length; i++)
                {
                    Ticks[i].SerializeToStream(buffer);
                }

                return buffer.ToArray();
            }

            public static ProfilerContainer FromBytes(byte[] bytes)
            {
                var container = new ProfilerContainer();
                var buffer = new NetworkBuffer(bytes);
                var reader = new NetworkReader(buffer);
                var count = reader.ReadUInt16Packed();

                container.Ticks = new ProfilerTick[count];

                for (int i = 0; i < count; i++)
                {
                    container.Ticks[i] = ProfilerTick.FromStream(buffer);
                }

                return container;
            }
        }

        private void StopRecording()
        {
            NetworkProfiler.Stop();
        }

        private void StartRecording()
        {
            if (NetworkProfiler.IsRunning) StopRecording();

            if (NetworkProfiler.Ticks != null && NetworkProfiler.Ticks.Count >= 2)
            {
                m_Curve = AnimationCurve.Constant(NetworkProfiler.Ticks.ElementAt(0).Frame, NetworkProfiler.Ticks.ElementAt(NetworkProfiler.Ticks.Count - 1).Frame, 0);
            }
            else
            {
                m_Curve = AnimationCurve.Constant(0, 1, 0);
            }

            m_LastDrawn = 0;
            NetworkProfiler.Start(m_CaptureCount);
        }

        private void ClearDrawing()
        {
            m_CurrentTicks.Clear();
            m_Curve = AnimationCurve.Constant(0, 1, 0);
            m_LastDrawn = 0;
        }

        private void ChangeRecordState()
        {
            if (NetworkProfiler.IsRunning) StopRecording();
            else StartRecording();
        }

        private TickEvent m_EventHover = null;
        private double m_LastSetup = 0;

        private void OnGUI()
        {
            bool recording = NetworkProfiler.IsRunning;
            float deltaTime = (float)(EditorApplication.timeSinceStartup - m_LastSetup);

            m_LastSetup = EditorApplication.timeSinceStartup;

            //Draw top bar
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(recording ? "Stop" : "Capture")) ChangeRecordState();
            if (GUILayout.Button("Clear")) ClearDrawing();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Import datafile"))
            {
                string path = EditorUtility.OpenFilePanel("Choose a NetworkProfiler file", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var ticks = ProfilerContainer.FromBytes(File.ReadAllBytes(path)).Ticks;
                    if (ticks.Length >= 2)
                    {
                        m_Curve = AnimationCurve.Constant(ticks[0].EventId, ticks[(ticks.Length - 1)].EventId, 0);
                        m_ShowMax = ticks.Length;
                        m_ShowMin = ticks.Length - Mathf.Clamp(100, 0, ticks.Length);
                    }
                    else
                    {
                        m_Curve = AnimationCurve.Constant(0, 1, 0);
                    }

                    m_CurrentTicks.Clear();
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        m_CurrentTicks.Add(ticks[i]);

                        uint bytes = 0;
                        if (ticks[i].Events.Count > 0)
                        {
                            for (int j = 0; j < ticks[i].Events.Count; j++)
                            {
                                var tickEvent = ticks[i].Events[j];
                                bytes += tickEvent.Bytes;
                            }
                        }

                        m_Curve.AddKey(ticks[i].EventId, bytes);
                    }
                }
            }

            if (GUILayout.Button("Export datafile"))
            {
                int max = (int)m_ShowMax;
                int min = (int)m_ShowMin;
                int ticksInRange = max - min;
                var ticks = new ProfilerTick[ticksInRange];
                for (int i = min; i < max; i++) ticks[i - min] = m_CurrentTicks[i];
                string path = EditorUtility.SaveFilePanel("Save NetworkProfiler data", "", "networkProfilerData", "");
                if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, new ProfilerContainer { Ticks = ticks }.ToBytes());
            }

            EditorGUILayout.EndHorizontal();
            float prevHis = m_CaptureCount;
            m_CaptureCount = EditorGUILayout.DelayedIntField("History count", m_CaptureCount);
            if (m_CaptureCount <= 0) m_CaptureCount = 1;
            m_UpdateDelay = EditorGUILayout.Slider("Refresh delay", m_UpdateDelay, 0.1f, 10f);
            EditorGUILayout.EndVertical();

            if (prevHis != m_CaptureCount) StartRecording();

            //Cache
            if (NetworkProfiler.IsRunning)
            {
                if (Time.unscaledTime - m_LastDrawn > m_UpdateDelay)
                {
                    m_LastDrawn = Time.unscaledTime;
                    m_CurrentTicks.Clear();
                    if (NetworkProfiler.Ticks.Count >= 2)
                    {
                        m_Curve = AnimationCurve.Constant(NetworkProfiler.Ticks.ElementAt(0).EventId, NetworkProfiler.Ticks.ElementAt(NetworkProfiler.Ticks.Count - 1).EventId, 0);
                    }

                    for (int i = 0; i < NetworkProfiler.Ticks.Count; i++)
                    {
                        var tick = NetworkProfiler.Ticks.ElementAt(i);
                        m_CurrentTicks.Add(tick);

                        uint bytes = 0;
                        if (tick.Events.Count > 0)
                        {
                            for (int j = 0; j < tick.Events.Count; j++)
                            {
                                var tickEvent = tick.Events[j];
                                bytes += tickEvent.Bytes;
                            }
                        }

                        m_Curve.AddKey(tick.EventId, bytes);
                    }
                }
            }


            //Draw Animation curve and slider
            m_Curve = EditorGUILayout.CurveField(m_Curve);
            EditorGUILayout.MinMaxSlider(ref m_ShowMin, ref m_ShowMax, 0, m_CurrentTicks.Count);
            //Verify slider values
            if (m_ShowMin < 0) m_ShowMin = 0;
            if (m_ShowMax > m_CurrentTicks.Count) m_ShowMax = m_CurrentTicks.Count;
            if (m_ShowMin <= 0 && m_ShowMax <= 0)
            {
                m_ShowMin = 0;
                m_ShowMax = m_CurrentTicks.Count;
            }

            //Draw main board
            bool hover = false;
            int nonEmptyTicks = 0;
            int largestTickCount = 0;
            int totalTicks = ((int)m_ShowMax - (int)m_ShowMin);

            for (int i = (int)m_ShowMin; i < (int)m_ShowMax; i++)
            {
                if (m_CurrentTicks[i].Events.Count > 0) nonEmptyTicks++; //Count non empty ticks
                if (m_CurrentTicks[i].Events.Count > largestTickCount) largestTickCount = m_CurrentTicks[i].Events.Count; //Get how many events the tick with most events has
            }

            int emptyTicks = totalTicks - nonEmptyTicks;

            float equalWidth = position.width / totalTicks;
            float propWidth = equalWidth * 0.3f;
            float widthPerTick = ((position.width - emptyTicks * propWidth) / nonEmptyTicks);

            float currentX = 0;
            int emptyStreak = 0;
            for (int i = (int)m_ShowMin; i < (int)m_ShowMax; i++)
            {
                var tick = m_CurrentTicks[i];
                if (tick.Events.Count == 0 && i != totalTicks - 1)
                {
                    emptyStreak++;
                    continue;
                }

                if (emptyStreak > 0 || i == totalTicks - 1)
                {
                    var dataRect = new Rect(currentX, 140f, propWidth * emptyStreak, position.height - 140f);
                    currentX += propWidth * emptyStreak;
                    if (emptyStreak >= 2) EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y, dataRect.width, dataRect.height), emptyStreak.ToString(), s_WrapStyle);
                    emptyStreak = 0;
                }

                if (tick.Events.Count > 0)
                {
                    float heightPerEvent = ((position.height - 140f) - (5f * largestTickCount)) / largestTickCount;

                    float currentY = 140f;
                    for (int j = 0; j < tick.Events.Count; j++)
                    {
                        var tickEvent = tick.Events[j];
                        var dataRect = new Rect(currentX, currentY, widthPerTick, heightPerEvent);

                        if (dataRect.Contains(Event.current.mousePosition))
                        {
                            hover = true;
                            m_EventHover = tickEvent;
                        }

                        EditorGUI.DrawRect(dataRect, TickTypeToColor(tickEvent.EventType, true));
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y, dataRect.width, dataRect.height / 2), tickEvent.EventType.ToString(), s_WrapStyle);
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y + dataRect.height / 2, dataRect.width, dataRect.height / 2), tickEvent.Bytes + "B", s_WrapStyle);

                        currentY += heightPerEvent + 5f;
                    }
                }

                EditorGUI.DrawRect(new Rect(currentX, 100, widthPerTick, 40), TickTypeToColor(tick.Type, false));
                EditorGUI.LabelField(new Rect(currentX, 100, widthPerTick, 20), tick.Type.ToString(), s_WrapStyle);
                EditorGUI.LabelField(new Rect(currentX, 120, widthPerTick, 20), tick.Frame.ToString(), s_WrapStyle);
                currentX += widthPerTick;
            }

            //Calculate alpha
            if (hover)
            {
                m_HoverAlpha += deltaTime * 10f;

                if (m_HoverAlpha > 1f) m_HoverAlpha = 1f;
                else if (m_HoverAlpha < 0f) m_HoverAlpha = 0f;
            }
            else
            {
                m_HoverAlpha -= deltaTime * 10f;
                if (m_HoverAlpha > 1f) m_HoverAlpha = 1f;
                else if (m_HoverAlpha < 0f) m_HoverAlpha = 0f;
            }

            //Draw hover thingy
            if (m_EventHover != null)
            {
                var rect = new Rect(Event.current.mousePosition, new Vector2(500, 100));
                EditorGUI.DrawRect(rect, GetEditorColorWithAlpha(m_HoverAlpha));

                float heightPerField = (rect.height - 5) / 4;
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), "EventType: " + m_EventHover.EventType, GetStyleWithTextAlpha(EditorStyles.label, m_HoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 1 + 5, rect.width, rect.height), "Size: " + m_EventHover.Bytes + "B", GetStyleWithTextAlpha(EditorStyles.label, m_HoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 2 + 5, rect.width, rect.height), "Channel: " + m_EventHover.ChannelName, GetStyleWithTextAlpha(EditorStyles.label, m_HoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 3 + 5, rect.width, rect.height), "MessageType: " + m_EventHover.MessageType, GetStyleWithTextAlpha(EditorStyles.label, m_HoverAlpha));
            }

            Repaint();
        }

        private Color TickTypeToColor(TickType type, bool alpha)
        {
            switch (type)
            {
                case TickType.Event:
                    return new Color(0.58f, 0f, 0.56f, alpha ? 0.37f : 0.7f);
                case TickType.Receive:
                    return new Color(0f, 0.85f, 0.85f, alpha ? 0.28f : 0.7f);
                case TickType.Send:
                    return new Color(0, 0.55f, 1f, alpha ? 0.06f : 0.7f);
                default:
                    return Color.clear;
            }
        }

        private Color EditorColor => EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);

        private Color GetEditorColorWithAlpha(float alpha) => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, alpha) : new Color(0.76f, 0.76f, 0.76f, alpha);

        private GUIStyle GetStyleWithTextAlpha(GUIStyle style, float alpha)
        {
            Color textColor = style.normal.textColor;
            textColor.a = alpha;
            GUIStyle newStyle = new GUIStyle(style);
            newStyle.normal.textColor = textColor;
            return newStyle;
        }
    }
}