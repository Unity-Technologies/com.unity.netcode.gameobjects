using System;
using System.Collections.Generic;
using MLAPI.Logging;

namespace MLAPI.Components
{
    /// <summary>
    /// The main class for controlling lag compensation
    /// </summary>
    public static class LagCompensationManager
    {
        /// <summary>
        /// Simulation objects
        /// </summary>
        public static readonly List<TrackedObject> simulationObjects = new List<TrackedObject>();

        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(float secondsAgo, Action action)
        {
            if(!NetworkingManager.Singleton.IsServer)
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
            if (!NetworkingManager.Singleton.IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Lag compensation simulations are only to be ran on the server");
                return;
            }
            float milisecondsDelay = NetworkingManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRTT(clientId, out error) / 2f;
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
