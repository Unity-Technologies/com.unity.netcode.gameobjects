using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    /// <summary>
    /// The main class for controlling lag compensation
    /// </summary>
    public static class LagCompensationManager
    {
        internal static List<TrackedObject> simulationObjects = new List<TrackedObject>();
        /// <summary>
        /// Simulation objects
        /// </summary>
        public static List<TrackedObject> SimulationObjects
        {
            get
            {
                return simulationObjects;
            }
        }

        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(float secondsAgo, Action action)
        {
            if(!NetworkingManager.singleton.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Lag compensation simulations are only to be ran on the server");
                return;
            }
            for (int i = 0; i < simulationObjects.Count; i++)
            {
                simulationObjects[i].ReverseTransform(secondsAgo);
            }

            action.Invoke();

            for (int i = 0; i < simulationObjects.Count; i++)
            {
                simulationObjects[i].ResetStateTransform();
            }
        }

        private static byte error = 0;
        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back. The time is based on the estimated RTT of a clientId
        /// </summary>
        /// <param name="clientId">The clientId's RTT to use</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(uint clientId, Action action)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Lag compensation simulations are only to be ran on the server");
                return;
            }
            float milisecondsDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetCurrentRTT(clientId, out error) / 2f;
            Simulate(milisecondsDelay * 1000f, action);
        }

        internal static void AddFrames()
        {
            for (int i = 0; i < simulationObjects.Count; i++)
            {
                simulationObjects[i].AddFrame();
            }
        }
    }
}
