using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Core
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
        internal LinkedList<float> Framekeys = new LinkedList<float>();
        private Vector3 savedPosition;
        private Quaternion savedRotation;

        /// <summary>
        /// Gets the total amount of points stored in the component
        /// </summary>
        public int TotalPoints
        {
            get
            {
                return Framekeys.Count;
            }
        }

        /// <summary>
        /// Gets the average amount of time between the points in miliseconds
        /// </summary>
        public float AvgTimeBetweenPointsMs
        {
            get
            {
                float totalSpan = Framekeys.Last.Value - Framekeys.First.Value;
                return (totalSpan / Framekeys.Count) * 1000f;
            }
        }

        internal void ReverseTransform(float secondsAgo)
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            float currentTime = Time.time;
            float targetTime = currentTime - secondsAgo;
            float previousTime = 0;
            float nextTime = 0;
            LinkedListNode<float> node = Framekeys.First;
            float previousValue = 0f;
            while(node != null)
            {
                if(previousValue <= targetTime && node.Value >= targetTime)
                {
                    previousTime = previousValue;
                    nextTime = node.Value;
                    break;
                }
                else
                {
                    previousValue = node.Value;
                    node = node.Next;
                }
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
            Framekeys.AddFirst(0);
            LagCompensationManager.simulationObjects.Add(this);
        }

        void OnDestroy()
        {
            Framekeys.Clear();
            FrameData.Clear();
            LagCompensationManager.simulationObjects.Remove(this);
        }

        internal void AddFrame()
        {
            float currentTime = Time.time;
            LinkedListNode<float> node = Framekeys.First;
            LinkedListNode<float> nextNode = node.Next;
            while (node != null && currentTime - node.Value >= NetworkingManager.singleton.NetworkConfig.SecondsHistory)
            {
                nextNode = node.Next;
                FrameData.Remove(node.Value);
                Framekeys.RemoveFirst();
                node = nextNode;
            }
            FrameData.Add(Time.time, new TrackedPointData()
            {
                position = transform.position,
                rotation = transform.rotation
            });
            Framekeys.AddLast(Time.time);
        }
    }
}
