using MLAPI.Data;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Core
{
    //Based on: https://twotenpvp.github.io/lag-compensation-in-unity.html
    //Modified to be used with latency rather than fixed frames and subframes. Thus it will be less accrurate but more modular.
    public class TrackedObject : MonoBehaviour
    {
        internal Dictionary<float, TrackedPointData> FrameData = new Dictionary<float, TrackedPointData>();
        internal List<float> Framekeys = new List<float>() { 0 };
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
            for (int i = 1; i < Framekeys.Count; i++)
            {
                if (Framekeys[i - 1] <= targetTime && Framekeys[i] >= targetTime)
                {
                    previousTime = Framekeys[i];
                    nextTime = Framekeys[i + 1];
                    break;
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
            LagCompensationManager.SimulationObjects.Add(this);
        }

        void OnDestroy()
        {
            LagCompensationManager.SimulationObjects.Remove(this);
        }

        internal void AddFrame()
        {
            float currentTime = Time.time;
            for (int i = 0; i < Framekeys.Count; i++)
            {
                if (currentTime - Framekeys[i] >= NetworkingManager.singleton.NetworkConfig.SecondsHistory)
                {
                    for (int j = 0; j < i; j++)
                    {
                        FrameData.Remove(Framekeys[0]);
                        //This is not good for performance. Other datatypes should be concidered.
                        Framekeys.RemoveAt(0);
                    }
                }
            }
            FrameData.Add(Time.time, new TrackedPointData()
            {
                position = transform.position,
                rotation = transform.rotation
            });
            Framekeys.Add(Time.time);
        }
    }
}
