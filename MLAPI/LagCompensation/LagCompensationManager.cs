using System;
using System.Collections.Generic;
using MLAPI.Exceptions;

namespace MLAPI.LagCompensation
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
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can perform lag compensation");
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

        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back. The time is based on the estimated RTT of a clientId
        /// </summary>
        /// <param name="clientId">The clientId's RTT to use</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(ulong clientId, Action action)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                throw new NotServerException("Only the server can perform lag compensation");
            }
            
            float millisecondsDelay = NetworkingManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId) / 2f;
            Simulate(millisecondsDelay * 1000f, action);
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
