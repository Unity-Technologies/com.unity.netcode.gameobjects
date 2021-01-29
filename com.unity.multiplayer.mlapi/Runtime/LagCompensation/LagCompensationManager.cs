using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public static readonly List<TrackedObject> SimulationObjects = new List<TrackedObject>();
        /// <summary>
        /// Simulation objects
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use SimulationObjects instead", false)]
        public static List<TrackedObject> simulationObjects => SimulationObjects;


        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(float secondsAgo, Action action)
        {
            Simulate(secondsAgo, SimulationObjects, action);
        }

        /// <summary>
        /// Turns time back a given amount of second on the given objects, invokes an action and turns it back
        /// </summary>
        /// <param name="secondsAgo">The amount of seconds</param>
        /// <param name="simulatedObjects">The object to simulate back in time</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(float secondsAgo, IList<TrackedObject> simulatedObjects, Action action)
        {
            if( simulatedObjects.Count > 0 )
            {
                if( !simulatedObjects[0].NetObject.NetManager.IsServer )
                {
                    throw new NotServerException("Only the server can perform lag compensation");
                }
            }

            for (int i = 0; i < simulatedObjects.Count; i++)
            {
                simulatedObjects[i].ReverseTransform(secondsAgo);
            }

            action.Invoke();

            for (int i = 0; i < simulatedObjects.Count; i++)
            {
                simulatedObjects[i].ResetStateTransform();
            }
        }

        /// <summary>
        /// Turns time back a given amount of seconds, invokes an action and turns it back. The time is based on the estimated RTT of a clientId
        /// </summary>
        /// <param name="networkingManager">The NetworkingManager on which the lag compensator should operator.</param>
        /// <param name="clientId">The clientId's RTT to use</param>
        /// <param name="action">The action to invoke when time is turned back</param>
        public static void Simulate(NetworkingManager networkingManager, ulong clientId, Action action)
        {
            if (!networkingManager.IsServer)
            {
                throw new NotServerException("Only the server can perform lag compensation");
            }
            
            float millisecondsDelay = networkingManager.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId) / 2f;
            Simulate(millisecondsDelay * 1000f, action);
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
