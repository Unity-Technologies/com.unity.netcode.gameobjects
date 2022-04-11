using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


public class TestSamAnimator : NetworkBehaviour
{
    NetworkAnimator m_NA;
    // OwnerNetworkTransform m_NT;

    // Start is called before the first frame update
    void Start()
    {
        m_NA = GetComponent<NetworkAnimator>();
        // m_NT = GetComponent<OwnerNetworkTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T) && IsOwner)
        {
            m_NA.SetTrigger("Pulse");
        }

        if (Input.GetKey(KeyCode.W) && IsOwner)
        {
            var pos = transform.position;
            pos.x = pos.x + 5 * Time.deltaTime;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.S) && IsOwner)
        {
            var pos = transform.position;
            pos.x = pos.x - 5 * Time.deltaTime;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.A) && IsOwner)
        {
            var pos = transform.position;
            pos.z = pos.z - 5 * Time.deltaTime;
            transform.position = pos;
        }
        if (Input.GetKey(KeyCode.D) && IsOwner)
        {
            var pos = transform.position;
            pos.z = pos.z + 5 * Time.deltaTime;
            transform.position = pos;
        }
    }
}
