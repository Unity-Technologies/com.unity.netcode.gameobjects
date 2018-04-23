using MLAPI.MonoBehaviours.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.MonoBehaviours.Prototyping
{
    //This class based on the code from the UNET HLAPI Proximity
    public class NetworkedProximity : NetworkedBehaviour
    {
        public enum CheckMethod
        {
            Physics3D,
            Physics2D
        };

        [Tooltip("The maximum range that objects will be visible at.")]
        public int Range = 10;

        [Tooltip("How often (in seconds) that this object should update the set of players that can see it.")]
        public float VisibilityUpdateInterval = 1.0f; // in seconds

        [Tooltip("Which method to use for checking proximity of players.\n\nPhysics3D uses 3D physics to determine proximity.\n\nPhysics2D uses 2D physics to determine proximity.")]
        public CheckMethod CheckType = CheckMethod.Physics3D;

        [Tooltip("Enable to force this object to be hidden from players.")]
        public bool ForceHidden = false;

        private float lastUpdateTime;

        private void Update()
        {
            if (!isServer)
                return;

            if (Time.time - lastUpdateTime > VisibilityUpdateInterval)
            {
                RebuildObservers();
                lastUpdateTime = NetworkingManager.singleton.NetworkTime;
            }
        }

        public override bool OnCheckObserver(uint newClientId)
        {
            if (ForceHidden)
                return false;
            Vector3 pos = NetworkingManager.singleton.ConnectedClients[newClientId].PlayerObject.transform.position;
            return (pos - transform.position).magnitude < Range;
        }

        public override bool OnRebuildObservers(HashSet<uint> observers)
        {
            if (ForceHidden)
            {
                // ensure player can still see themself
                if (networkedObject != null && networkedObject.isPlayerObject)
                    observers.Add(networkedObject.OwnerClientId);
                return true;
            }

            switch (CheckType)
            {
                case CheckMethod.Physics3D:
                    {
                        var hits = Physics.OverlapSphere(transform.position, Range);
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var uv = hits[i].GetComponent<NetworkedObject>();
                            if (uv != null && uv.isPlayerObject)
                                observers.Add(uv.OwnerClientId);
                        }
                        return true;
                    }
                case CheckMethod.Physics2D:
                    {
                        var hits = Physics2D.OverlapCircleAll(transform.position, Range);
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var uv = hits[i].GetComponent<NetworkedObject>();
                            if (uv != null && (uv.isPlayerObject))
                                observers.Add(uv.OwnerClientId);
                        }
                        return true;
                    }
            }
            return false;
        }
    }
}
