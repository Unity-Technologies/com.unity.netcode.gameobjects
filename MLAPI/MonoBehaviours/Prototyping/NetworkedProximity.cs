using MLAPI.MonoBehaviours.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Prototyping
{
    /// <summary>
    /// A prototype component to set observers based on distance
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedProximity")]
    public class NetworkedProximity : NetworkedBehaviour
    {
        /// <summary>
        /// Method to use for checking distance
        /// </summary>
        public enum CheckMethod
        {
            /// <summary>
            /// Checks in 3d space
            /// </summary>
            Physics3D,
            /// <summary>
            /// Checks in 2d space
            /// </summary>
            Physics2D
        };

        /// <summary>
        /// The range a client has to be within to be added as an observer
        /// </summary>
        [Tooltip("The maximum range that objects will be visible at.")]
        public int Range = 10;

        /// <summary>
        /// The delay in seconds between visibility checks
        /// </summary>
        [Tooltip("How often (in seconds) that this object should update the set of players that can see it.")]
        public float VisibilityUpdateInterval = 1.0f; // in seconds

        /// <summary>
        /// Filter to check objects only on specific layers.
        /// </summary>
        [Tooltip("Filter to check objects only on specific layers.")]
        public LayerMask layerMask;

        /// If enabled, the object will always be hidden from players.
        /// </summary>
        [Tooltip("Enable to force this object to be hidden from players.")]
        public bool ForceHidden = false;

        /// <summary>
        /// The method to use for checking distance
        /// </summary>
        [Tooltip("Which method to use for checking proximity of players.\n\nPhysics3D uses 3D physics to determine proximity.\n\nPhysics2D uses 2D physics to determine proximity.")]
        public CheckMethod CheckType = CheckMethod.Physics3D;

        /// <summary>
        /// Specifies whether this query should hit Triggers.
        /// </summary>
        [Tooltip("Specifies whether this query should hit Triggers (Physics3D only).")]
        public QueryTriggerInteraction queryTriggerInteraction3D = QueryTriggerInteraction.UseGlobal;

        /// <summary>
        /// Min / Max depth range (2D only).
        /// </summary>
        [Tooltip("Min / Max depth range (Physics2D only).")]
        public MinMax2D depth2D;

        /// <summary>
        private float lastUpdateTime;

        //private void FixedUpdate()
        //{
        //	if (!isServer)
        //		return;

        //	if (Time.time - lastUpdateTime > VisibilityUpdateInterval)
        //	{
        //		RebuildObservers();
        //		lastUpdateTime = NetworkingManager.singleton.NetworkTime;
        //	}
        //}

        /// <summary>
        /// Called when a new client connects
        /// </summary>
        /// <param name="newClientId">The clientId of the new client</param>
        /// <returns>Wheter or not the object should be visible</returns>
        public override bool OnCheckObserver(uint newClientId)
        {
            if (ForceHidden)
                return false;
            Vector3 pos = NetworkingManager.singleton.ConnectedClients[newClientId].PlayerObject.transform.position;
            return (pos - transform.position).magnitude < Range;
        }

        private Collider[] colliders = new Collider[32];
        private Collider2D[] colliders2d = new Collider2D[32];

        /// <summary>
        /// Called when observers are to be rebuilt
        /// </summary>
        /// <param name="observers">The observers to use</param>
        /// <returns>Wheter or not we changed anything</returns>
        public override bool OnRebuildObservers(HashSet<uint> observers)
        {
            // This implementation is an example. 
            // Not efficient. We remove all old observers as the API doesn't clear them for us.
            // The reason it's not cleared is so that you don't have to iterate over your things if you simply
            // Have an event driven system where you want to remove a player. Ex if they leave a zone
            observers.Clear();

            if (ForceHidden)
            {
                // ensure player can still see themself
                if (networkedObject != null && networkedObject.isPlayerObject)
                    observers.Add(networkedObject.OwnerClientId);
                return true;
            }

            if (Time.time - lastUpdateTime > VisibilityUpdateInterval)
            {
                switch (CheckType)
                {
                    case CheckMethod.Physics3D:
                        {
                            int hits = Physics.OverlapSphereNonAlloc(transform.position, Range, colliders, layerMask, queryTriggerInteraction3D);
                            //We check if it's equal to since the OverlapSphereNonAlloc only returns what it actually wrote, not what it found.
                            if (hits >= colliders.Length)
                            {
                                //Resize colliders array
                                colliders = new Collider[(int)((hits + 2) * 1.3f)];
                                hits = Physics.OverlapSphereNonAlloc(transform.position, Range, colliders, layerMask, queryTriggerInteraction3D);
                            }
                            for (int i = 0; i < hits; i++)
                            {
                                var uv = colliders[i].GetComponent<NetworkedObject>();
                                if (uv != null && uv.isPlayerObject)
                                    observers.Add(uv.OwnerClientId);
                            }
                            lastUpdateTime = NetworkingManager.singleton.NetworkTime;
                            return true;
                        }
                    case CheckMethod.Physics2D:
                        {
                            int hits = Physics2D.OverlapCircleNonAlloc(transform.position, Range, colliders2d, layerMask, depth2D.minDepth, depth2D.maxDepth);
                            //We check if it's equal to since the OverlapSphereNonAlloc only returns what it actually wrote, not what it found.
                            if (hits >= colliders.Length)
                            {
                                //Resize colliders array
                                colliders2d = new Collider2D[(int)((hits + 2) * 1.3f)];
                                hits = Physics2D.OverlapCircleNonAlloc(transform.position, Range, colliders2d, layerMask, depth2D.minDepth, depth2D.maxDepth);
                            }
                            for (int i = 0; i < hits; i++)
                            {
                                var uv = colliders2d[i].GetComponent<NetworkedObject>();
                                if (uv != null && (uv.isPlayerObject))
                                    observers.Add(uv.OwnerClientId);
                            }
                            lastUpdateTime = NetworkingManager.singleton.NetworkTime;
                            return true;
                        }
                }
            }
            return false;
        }
    }
}

[System.Serializable]
public class MinMax2D
{
    /// <summary>
    /// Only include objects with a Z coordinate (depth) greater than or equal to this value.
    /// </summary>
    [Tooltip("Only include objects with a Z coordinate (depth) greater than or equal to this value.")]
    public float minDepth = -Mathf.Infinity;

    /// <summary>
    /// Only include objects with a Z coordinate (depth) less than or equal to this value.
    /// </summary>
    [Tooltip("Only include objects with a Z coordinate (depth) less than or equal to this value.")]
    public float maxDepth = Mathf.Infinity;
}
