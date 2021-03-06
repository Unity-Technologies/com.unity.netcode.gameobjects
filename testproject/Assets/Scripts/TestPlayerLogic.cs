using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkedVar;

namespace MLAPITest
{
    public class TestPlayerLogic : MLAPI.NetworkedBehaviour
    {
        public NetworkedVarVector3 NetworkPos { get; } = new NetworkedVarVector3();

        public NetworkedVarFloat NetworkRotation { get; } = new NetworkedVarFloat();

        private const float k_Speed = 5;
        private const float k_RotSpeedDegrees = 120;
        private const float k_MaxSpeed = 6f;

        public Material ClientMat;


        // Start is called before the first frame update
        void Start()
        {
        }

        public override void NetworkStart()
        {
            if( IsClient)
            {
                GetComponent<Renderer>().material = ClientMat;
                RecvPingServerRpc(0);
            }
        }

        private void DoServerLogic()
        {
            float rotY = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, rotY + (k_RotSpeedDegrees * Time.deltaTime), 0);

            Vector3 curPos = transform.position;
            curPos += (transform.forward * k_Speed * Time.deltaTime);
            transform.position = curPos;
        }

        // Update is called once per frame
        void Update()
        {
            if(IsServer)
            {
                DoServerLogic();
                NetworkPos.Value = transform.position;
                NetworkRotation.Value = transform.rotation.eulerAngles.y;
            }

            if(IsClient)
            {
                Camera.main.transform.LookAt(transform); // look at me!

                float deltaTime = Mathf.Min(Time.deltaTime, 0.02f);

                //we cap the client's position keeping up with the network, to show how there are two different GameObjects in play.
                float distance = (NetworkPos.Value - transform.position).magnitude;
                float max_move = k_MaxSpeed * deltaTime;
                distance = Mathf.Min(distance, max_move);
                Vector3 normal = (NetworkPos.Value - transform.position).normalized;
                Vector3 displacement = normal * distance;

                transform.position = transform.position + displacement;
                transform.rotation = Quaternion.Euler(0, NetworkRotation.Value, 0);
            }
        }

        [MLAPI.Messaging.ServerRpc]
        public void RecvPingServerRpc(int count)
        {
            Debug.Log("Server received client RPC ping, count=" + count);
            RecvPongClientRpc(count+1);
        }

        [MLAPI.Messaging.ClientRpc]
        public void RecvPongClientRpc(int count)
        {
            Debug.Log("Client received server RPC pong, count=" + count);
            if( count < 5 )
            {
                RecvPingServerRpc(count + 1);
            }
        }
    }


}

