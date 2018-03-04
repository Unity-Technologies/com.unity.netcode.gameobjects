using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace MLAPI.MonoBehaviours.Core
{
    public static class LagCompensationManager
    {
        public static List<TrackedObject> SimulationObjects = new List<TrackedObject>();

        public static void Simulate(float secondsAgo, Action action)
        {
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
