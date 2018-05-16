using MLAPI.Data.NetworkProfiler;
using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    public class MLAPIProfiler : EditorWindow
    {
        [MenuItem("Window/MLAPI Profiler")]
        public static void ShowWindow()
        {
            GetWindow<MLAPIProfiler>();
        }

        GUIStyle wrapStyle
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

        float hoverAlpha = 0f;
        float updateDelay = 1f;
        int captureCount = 100;
        float showMax = 0;
        float showMin = 0;
        AnimationCurve curve = AnimationCurve.Constant(0, 1, 0);
        readonly List<ProfilerTick> currentTicks = new List<ProfilerTick>();
        float lastDrawn = 0;
        class ProfilerContainer
        {
            public ProfilerTick[] ticks;
        }

        private void StopRecording()
        {
            NetworkProfiler.Stop();
        }

        private void StartRecording()
        {
            if (NetworkProfiler.IsRunning)
                StopRecording();

            if (NetworkProfiler.Ticks != null && NetworkProfiler.Ticks.Count >= 2)
                curve = AnimationCurve.Constant(NetworkProfiler.Ticks.ElementAt(0).Frame, NetworkProfiler.Ticks.ElementAt(NetworkProfiler.Ticks.Count - 1).Frame, 0);
            else
                curve = AnimationCurve.Constant(0, 1, 0);

            lastDrawn = 0;
            NetworkProfiler.Start(captureCount);
        }

        private void ClearDrawing()
        {
            curve = AnimationCurve.Constant(0, 1, 0);
            lastDrawn = 0;
        }

        private void ChangeRecordState()
        {
            if (NetworkProfiler.IsRunning) StopRecording();
            else StartRecording();
        }

        TickEvent eventHover = null;
        double lastSetup = 0;
        private void OnGUI()
        {
            bool recording = NetworkProfiler.IsRunning;
            float deltaTime = (float)(EditorApplication.timeSinceStartup - lastSetup);
            lastSetup = EditorApplication.timeSinceStartup;

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
                    ProfilerTick[] ticks = BinarySerializer.Deserialize<ProfilerContainer>(File.ReadAllBytes(path)).ticks;
                    if (ticks.Length >= 2)
                    {
                        curve = AnimationCurve.Constant(ticks[0].EventId, ticks[(ticks.Length - 1)].EventId, 0);
                        showMax = ticks.Length;
                        showMin = ticks.Length - Mathf.Clamp(100, 0, ticks.Length);
                    }
                    else
                        curve = AnimationCurve.Constant(0, 1, 0);
                    currentTicks.Clear();
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        currentTicks.Add(ticks[i]);

                        uint bytes = 0;
                        if (ticks[i].Events.Count > 0)
                        {
                            for (int j = 0; j < ticks[i].Events.Count; j++)
                            {
                                TickEvent tickEvent = ticks[i].Events[j];
                                bytes += tickEvent.Bytes;
                            }
                        }
                        curve.AddKey(ticks[i].EventId, bytes);
                    }
                }
            }

            if (GUILayout.Button("Export datafile"))
            {
                int max = (int)showMax;
                int min = (int)showMin;
                int ticksInRange = max - min;
                ProfilerTick[] ticks = new ProfilerTick[ticksInRange];
                for (int i = min; i < max; i++) ticks[i - min] = currentTicks[i];
                string path = EditorUtility.SaveFilePanel("Save NetworkProfiler data", "", "networkProfilerData", "");
                if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, BinarySerializer.Serialize(new ProfilerContainer() { ticks = ticks }));
            }

            EditorGUILayout.EndHorizontal();
            float prevHis = captureCount;
            captureCount = EditorGUILayout.DelayedIntField("History count", captureCount);
            if (captureCount <= 0)
                captureCount = 1;
            updateDelay = EditorGUILayout.Slider("Refresh delay", updateDelay, 0.1f, 10f);
            EditorGUILayout.EndVertical();

            if (prevHis != captureCount) StartRecording();

            //Cache
            if (NetworkProfiler.IsRunning)
            {
                if (Time.unscaledTime - lastDrawn > updateDelay)
                {
                    lastDrawn = Time.unscaledTime;
                    currentTicks.Clear();
                    if (NetworkProfiler.Ticks.Count >= 2)
                        curve = AnimationCurve.Constant(NetworkProfiler.Ticks.ElementAt(0).EventId, NetworkProfiler.Ticks.ElementAt(NetworkProfiler.Ticks.Count - 1).EventId, 0);

                    for (int i = 0; i < NetworkProfiler.Ticks.Count; i++)
                    {
                        ProfilerTick tick = NetworkProfiler.Ticks.ElementAt(i);
                        currentTicks.Add(tick);

                        uint bytes = 0;
                        if (tick.Events.Count > 0)
                        {
                            for (int j = 0; j < tick.Events.Count; j++)
                            {
                                TickEvent tickEvent = tick.Events[j];
                                bytes += tickEvent.Bytes;
                            }
                        }
                        curve.AddKey(tick.EventId, bytes);
                    }
                }
            }


            //Draw Animation curve and slider
            curve = EditorGUILayout.CurveField(curve);
            EditorGUILayout.MinMaxSlider(ref showMin, ref showMax, 0, currentTicks.Count);
            //Verify slider values
            if (showMin < 0)
                showMin = 0;
            if (showMax > currentTicks.Count)
                showMax = currentTicks.Count;
            if (showMin <= 0 && showMax <= 0)
            {
                showMin = 0;
                showMax = currentTicks.Count;
            }

            //Draw main board
            bool hover = false;
            int nonEmptyTicks = 0;
            int largestTickCount = 0;
            int totalTicks = ((int)showMax - (int)showMin);

            for (int i = (int)showMin; i < (int)showMax; i++)
            {
                if (currentTicks[i].Events.Count > 0) nonEmptyTicks++; //Count non empty ticks
                if (currentTicks[i].Events.Count > largestTickCount) largestTickCount = currentTicks[i].Events.Count; //Get how many events the tick with most events has
            }
            int emptyTicks = totalTicks - nonEmptyTicks;

            float equalWidth = position.width / totalTicks;
            float propWidth = equalWidth * 0.3f;
            float widthPerTick = ((position.width - emptyTicks * propWidth) / nonEmptyTicks);

            float currentX = 0;
            int emptyStreak = 0;
            for (int i = (int)showMin; i < (int)showMax; i++)
            {
                ProfilerTick tick = currentTicks[i];
                if (tick.Events.Count == 0 && i != totalTicks - 1)
                {
                    emptyStreak++;
                    continue;
                }
                else if (emptyStreak > 0 || i == totalTicks - 1)
                {
                    Rect dataRect = new Rect(currentX, 100, propWidth * emptyStreak, position.height - 100);
                    currentX += propWidth * emptyStreak;
                    if (emptyStreak >= 4) EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y, dataRect.width, dataRect.height), emptyStreak.ToString(), wrapStyle);
                    emptyStreak = 0;
                }

                if (tick.Events.Count > 0)
                {
                    float heightPerEvent = ((position.height - 100f) - (5f * largestTickCount)) / largestTickCount;
                    float heightPerTotalBackground = ((position.height - 100f) - (5f * largestTickCount)) / tick.Events.Count;

                    float currentY = 100;
                    for (int j = 0; j < tick.Events.Count; j++)
                    {
                        TickEvent tickEvent = tick.Events[j];
                        Rect dataRect = new Rect(currentX, currentY, widthPerTick, heightPerEvent);

                        if (dataRect.Contains(Event.current.mousePosition))
                        {
                            hover = true;
                            eventHover = tickEvent;
                        }

                        if (j == tick.Events.Count - 1)
                            dataRect.height -= 45f;
                        EditorGUI.DrawRect(dataRect, TickTypeToColor(tickEvent.EventType));
                        float heightPerField = heightPerEvent / 12f;
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y + heightPerField * -3f, dataRect.width, dataRect.height), "EventType: " + tickEvent.EventType.ToString(), wrapStyle);
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y + heightPerField * -1f, dataRect.width, dataRect.height), "Size: " + tickEvent.Bytes + "B", wrapStyle);
                        string channelName = tickEvent.ChannelName.Length > 5 ? tickEvent.ChannelName.Remove(5, tickEvent.ChannelName.Length - 5) + "..." : tickEvent.ChannelName;
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y + heightPerField * 1f, dataRect.width, dataRect.height), "Channel: " + channelName, wrapStyle);
                        string messageType = tickEvent.MessageType.Length > 5 ? tickEvent.MessageType.Remove(5, tickEvent.MessageType.Length - 5) + "..." : tickEvent.MessageType;
                        EditorGUI.LabelField(new Rect(dataRect.x, dataRect.y + heightPerField * 3f, dataRect.width, dataRect.height), "MessageType: " + messageType, wrapStyle);

                        currentY += heightPerEvent + 5f;
                    }
                }
                EditorGUI.DrawRect(new Rect(currentX, position.height - 40, widthPerTick, 40), TickTypeToColor(tick.Type));
                EditorGUI.LabelField(new Rect(currentX, position.height - 40, widthPerTick, 20), "TickType: " + tick.Type.ToString(), wrapStyle);
                EditorGUI.LabelField(new Rect(currentX, position.height - 20, widthPerTick, 20), "Frame: " + tick.Frame.ToString(), wrapStyle);
                currentX += widthPerTick;
            }

            //Calculate alpha
            if (hover)
            {
                hoverAlpha += deltaTime * 10f;

                if (hoverAlpha > 1f) hoverAlpha = 1f;
                else if (hoverAlpha < 0f) hoverAlpha = 0f;
            }
            else
            {
                hoverAlpha -= deltaTime * 10f;
                if (hoverAlpha > 1f) hoverAlpha = 1f;
                else if (hoverAlpha < 0f) hoverAlpha = 0f;
            }

            //Draw hover thingy
            if (eventHover != null)
            {
                Rect rect = new Rect(Event.current.mousePosition, new Vector2(500, 100));
                EditorGUI.DrawRect(rect, GetEditorColorWithAlpha(hoverAlpha));

                float heightPerField = (rect.height - 5) / 4;
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), "EventType: " + eventHover.EventType.ToString(), GetStyleWithTextAlpha(EditorStyles.label, hoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 1 + 5, rect.width, rect.height), "Size: " + eventHover.Bytes + "B", GetStyleWithTextAlpha(EditorStyles.label, hoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 2 + 5, rect.width, rect.height), "Channel: " + eventHover.ChannelName, GetStyleWithTextAlpha(EditorStyles.label, hoverAlpha));
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y + heightPerField * 3 + 5, rect.width, rect.height), "MessageType: " + eventHover.MessageType, GetStyleWithTextAlpha(EditorStyles.label, hoverAlpha));
            }

            Repaint();
        }

        private Color TickTypeToColor(TickType type)
        {
            switch (type)
            {
                case TickType.Event:
                    return new Color(0.58f, 0f, 0.56f, 0.37f);
                case TickType.Receive:
                    return new Color(0f, 0.85f, 0.85f, 0.28f);
                case TickType.Send:
                    return new Color(0, 0.55f, 1f, 0.06f);
                default:
                    return Color.clear;
            }
        }

        private Color EditorColor
        {
            get
            {
                return EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);
            }
        }

        private Color GetEditorColorWithAlpha(float alpha)
        {
            return EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, alpha) : new Color(0.76f, 0.76f, 0.76f, alpha);
        }

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
