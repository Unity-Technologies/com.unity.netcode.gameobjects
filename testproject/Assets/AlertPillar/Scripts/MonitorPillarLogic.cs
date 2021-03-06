using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;

namespace AlertPillar
{
    public class MonitorPillarLogic : NetworkBehaviour
    {
        public Material Unalerted;
        public Material Alerted;

        public NetworkVariableBool IsAlerted { get; } = new NetworkVariableBool();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (IsServer)
            {
                List<TestPlayerLogic> playerLogics = MLAPI.Spawning.NetworkSpawnManager.FindObjectsInScene<TestPlayerLogic>(gameObject.scene);
                if(playerLogics.Count > 0 )
                {
                    float distanceToPlayer = (playerLogics[0].transform.position - transform.position).magnitude;
                    IsAlerted.Value = distanceToPlayer < 3;
                }
            }
            if (IsClient)
            {
                GetComponent<Renderer>().material = IsAlerted.Value ? Alerted : Unalerted;
            }
        }
    }
}

