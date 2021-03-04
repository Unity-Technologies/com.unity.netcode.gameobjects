using System.Collections.Generic;
using MLAPI.Collections;
using UnityEngine;

namespace MLAPI.LagCompensation
{
    //Based on: https://twotenpvp.github.io/lag-compensation-in-unity.html
    //Modified to be used with latency rather than fixed frames and subframes. Thus it will be less accrurate but more modular.

    /// <summary>
    /// A component used for lag compensation. Each object with this component will get tracked
    /// </summary>
    [AddComponentMenu("MLAPI/TrackedObject", -98)]
    public class TrackedObject : MonoBehaviour
    {
        internal Dictionary<float, TrackedPointData> FrameData = new Dictionary<float, TrackedPointData>();
        internal FixedQueue<float> Framekeys;
        private Vector3 m_SavedPosition;
        private Quaternion m_SavedRotation;

        /// <summary>
        /// Gets the total amount of points stored in the component
        /// </summary>
        public int TotalPoints => Framekeys?.Count ?? 0;

        /// <summary>
        /// Gets the average amount of time between the points in miliseconds
        /// </summary>
        public float AvgTimeBetweenPointsMs => Framekeys == null || Framekeys.Count == 0 ? 0 : ((Framekeys.ElementAt(Framekeys.Count - 1) - Framekeys.ElementAt(0)) / Framekeys.Count) * 1000f;

        /// <summary>
        /// Gets the total time history we have for this object
        /// </summary>
        public float TotalTimeHistory => Framekeys == null ? 0 : Framekeys.ElementAt(Framekeys.Count - 1) - Framekeys.ElementAt(0);

        private int m_MaxPoints => (int)(NetworkManager.Singleton.NetworkConfig.SecondsHistory / (1f / NetworkManager.Singleton.NetworkConfig.EventTickrate));

        internal void ReverseTransform(float secondsAgo)
        {
            m_SavedPosition = transform.position;
            m_SavedRotation = transform.rotation;

            float currentTime = NetworkManager.Singleton.NetworkTime;
            float targetTime = currentTime - secondsAgo;

            float previousTime = 0f;
            float nextTime = 0f;
            for (int i = 0; i < Framekeys.Count; i++)
            {
                if (previousTime <= targetTime && Framekeys.ElementAt(i) >= targetTime)
                {
                    nextTime = Framekeys.ElementAt(i);
                    break;
                }

                previousTime = Framekeys.ElementAt(i);
            }

            float timeBetweenFrames = nextTime - previousTime;
            float timeAwayFromPrevious = currentTime - previousTime;
            float lerpProgress = timeAwayFromPrevious / timeBetweenFrames;
            transform.position = Vector3.Lerp(FrameData[previousTime].Position, FrameData[nextTime].Position, lerpProgress);
            transform.rotation = Quaternion.Slerp(FrameData[previousTime].Rotation, FrameData[nextTime].Rotation, lerpProgress);
        }

        internal void ResetStateTransform()
        {
            transform.position = m_SavedPosition;
            transform.rotation = m_SavedRotation;
        }

        private void Start()
        {
            Framekeys = new FixedQueue<float>(m_MaxPoints);
            Framekeys.Enqueue(0);

            LagCompensationManager.SimulationObjects.Add(this);
        }

        private void OnDestroy()
        {
            LagCompensationManager.SimulationObjects.Remove(this);
        }

        internal void AddFrame()
        {
            if (Framekeys.Count == m_MaxPoints) FrameData.Remove(Framekeys.Dequeue());

            FrameData.Add(NetworkManager.Singleton.NetworkTime, new TrackedPointData()
            {
                Position = transform.position,
                Rotation = transform.rotation
            });

            Framekeys.Enqueue(NetworkManager.Singleton.NetworkTime);
        }
    }
}