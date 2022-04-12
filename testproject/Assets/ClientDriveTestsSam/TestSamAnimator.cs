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
        test.OnValueChanged += (value, newValue) => Debug.Log("got new value!");
    }

    NetworkVariable<int> test = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    [ServerRpc]
    void SendServerRpc()
    {
        Debug.Log("server RPC!!!!");
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 input)
    {
        transform.position = new Vector3(transform.position.x + 5 * input.x * Time.deltaTime, transform.position.y + 5 * input.y * Time.deltaTime, transform.position.z + 5 * input.z * Time.deltaTime);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.R) && IsOwner)
        {
            SendServerRpc();
            test.Value = test.Value + 1;
        }
        if (Input.GetKeyDown(KeyCode.T) && IsOwner)
        {
            // m_NA.Animator.SetTrigger("Rotate");
            // m_NA.Animator.SetTrigger("Pulse");
            m_NA.SetTrigger("Rotate");
            m_NA.SetTrigger("Pulse");
        }

        if (Input.GetKey(KeyCode.T) && IsOwner)
        {
            var cur = m_NA.Animator.GetLayerWeight(1);
            cur += Time.deltaTime;
            m_NA.Animator.SetLayerWeight(1, cur);
        }

        if (Input.GetKey(KeyCode.UpArrow) && IsOwner)
        {
            MoveServerRpc(new Vector3(1, 0, 0));
        }
        if (Input.GetKey(KeyCode.DownArrow) && IsOwner)
        {
            MoveServerRpc(new Vector3(-1, 0, 0));
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
