using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.NetworkingManagerComponents
{
    public static class LagCompensationManager
    {
        public static List<TrackedObject> SimulationObjects = new List<TrackedObject>();

        public static void Simulate(float secondsAgo, Action action)
        {
            if(!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Lag compensation simulations are only to be ran on the server.");
                return;
            }
            for (int i = 0; i < SimulationObjects.Count; i++)
            {
                SimulationObjects[i].ReverseTransform(secondsAgo);
            }

            action.Invoke();

            for (int i = 0; i < SimulationObjects.Count; i++)
            {
                SimulationObjects[i].ResetStateTransform();
            }
        }

        private static byte error = 0;
        public static void Simulate(int clientId, Action action)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Lag compensation simulations are only to be ran on the server.");
                return;
            }
            float milisecondsDelay = NetworkTransport.GetCurrentRTT(NetworkingManager.singleton.hostId, clientId, out error) / 2f;
            Simulate(milisecondsDelay * 1000f, action);
        }

        internal static void AddFrames()
        {
            for (int i = 0; i < SimulationObjects.Count; i++)
            {
                SimulationObjects[i].AddFrame();
            }
        }
    }
}
