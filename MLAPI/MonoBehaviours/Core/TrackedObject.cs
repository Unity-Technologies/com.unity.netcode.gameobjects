using System.Collections.Generic;
using MLAPI.Collections;
using MLAPI.Components;
using MLAPI.Internal;
using UnityEngine;

namespace MLAPI
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
        private Vector3 savedPosition;
        private Quaternion savedRotation;

        /// <summary>
        /// Gets the total amount of points stored in the component
        /// </summary>
        public int TotalPoints
        {
            get
            {
                if (Framekeys == null) return 0;
                else return Framekeys.Count;
            }
        }

        /// <summary>
        /// Gets the average amount of time between the points in miliseconds
        /// </summary>
        public float AvgTimeBetweenPointsMs
        {
            get
            {
                if (Framekeys == null || Framekeys.Count == 0) return 0;
                else return ((Framekeys.ElementAt(Framekeys.Count - 1) - Framekeys.ElementAt(0)) / Framekeys.Count) * 1000f;
            }
        }

        /// <summary>
        /// Gets the total time history we have for this object
        /// </summary>
        public float TotalTimeHistory
        {
            get
            {
                if (Framekeys == null) return 0;
                else return Framekeys.ElementAt(Framekeys.Count - 1) - Framekeys.ElementAt(0);
            }
        }

        private int maxPoints
        {
            get
            {
                return (int)(NetworkingManager.Singleton.NetworkConfig.SecondsHistory / (1f / NetworkingManager.Singleton.NetworkConfig.EventTickrate));
            }
        }

        internal void ReverseTransform(float secondsAgo)
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;

            float currentTime = NetworkingManager.Singleton.NetworkTime;
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
                else
                    previousTime = Framekeys.ElementAt(i);
            }
            float timeBetweenFrames = nextTime - previousTime;
            float timeAwayFromPrevious = currentTime - previousTime;
            float lerpProgress = timeAwayFromPrevious / timeBetweenFrames;
            transform.position = Vector3.Lerp(FrameData[previousTime].position, FrameData[nextTime].position, lerpProgress);
            transform.rotation = Quaternion.Slerp(FrameData[previousTime].rotation, FrameData[nextTime].rotation, lerpProgress);
        }

        internal void ResetStateTransform()
        {
            transform.position = savedPosition;
            transform.rotation = savedRotation;
        }

        void Start()
        {
            Framekeys = new FixedQueue<float>(maxPoints);
            Framekeys.Enqueue(0);
            LagCompensationManager.simulationObjects.Add(this);
        }

        void OnDestroy()
        {
            LagCompensationManager.simulationObjects.Remove(this);
        }

        internal void AddFrame()
        {
            if (Framekeys.Count == maxPoints)
                FrameData.Remove(Framekeys.Dequeue());

            FrameData.Add(NetworkingManager.Singleton.NetworkTime, new TrackedPointData()
            {
                position = transform.position,
                rotation = transform.rotation
            });
            Framekeys.Enqueue(NetworkingManager.Singleton.NetworkTime);
        }
    }
}
