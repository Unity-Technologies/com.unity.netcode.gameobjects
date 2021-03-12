using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

public class NetVarTest : NetworkBehaviour
{
    public NetworkVariable<int> m_Count = new NetworkVariable<int>();
    private float delay = 0.0f; // The bug didn't describe how you used delay, so I assumed it was something like this

    // Start is called before the first frame update
    void Start()
    {
        if (IsServer)
        {
            m_Count.OnValueChanged = Changed;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (delay <= 0)
            {
                m_Count.Value += 1;
                delay = 0.1f;
            }

            delay -= Time.deltaTime;
        }
    }

    void Changed(int before, int after)
    {
        Debug.Log(after);
    }
}
