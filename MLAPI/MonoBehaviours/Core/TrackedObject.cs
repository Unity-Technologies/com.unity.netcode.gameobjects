using MLAPI.Data;
using MLAPI.NetworkingManagerComponents;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Core
{
    //Based on: https://twotenpvp.github.io/lag-compensation-in-unity.html
    //Modified to be used with latency rather than fixed frames and subframes. Thus it will be less accrurate but more modular.
    [AddComponentMenu("MLAPI/TrackedObject", -98)]
    public class TrackedObject : MonoBehaviour
    {
        internal Dictionary<float, TrackedPointData> FrameData = new Dictionary<float, TrackedPointData>();
        internal LinkedList<float> Framekeys = new LinkedList<float>();
        private Vector3 savedPosition;
        private Quaternion savedRotation;

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
            LagCompensationManager.SimulationObjects.Add(this);
        }

        void OnDestroy()
        {
            Framekeys.Clear();
            FrameData.Clear();
            LagCompensationManager.SimulationObjects.Remove(this);
        }

        internal void AddFrame()
        {
            float currentTime = Time.time;
            LinkedListNode<float> node = Framekeys.First;
            LinkedListNode<float> nextNode = node.Next;
            while (currentTime - node.Value >= NetworkingManager.singleton.NetworkConfig.SecondsHistory)
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
