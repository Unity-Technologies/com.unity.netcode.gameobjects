using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkedVar;

namespace AlertPillar
{
    public class MonitorPillarLogic : NetworkedBehaviour
    {
        public Material Unalerted;
        public Material Alerted;

        public NetworkedVarBool IsAlerted { get; } = new NetworkedVarBool();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (IsServer)
            {
                List<TestPlayerLogic> playerLogics = MLAPI.Spawning.SpawnManager.FindObjectsInScene<TestPlayerLogic>(gameObject.scene);
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

