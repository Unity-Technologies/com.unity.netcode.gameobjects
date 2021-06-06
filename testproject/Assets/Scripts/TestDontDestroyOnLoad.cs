using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

public class TestDontDestroyOnLoad : NetworkBehaviour
{
    // Start is called before the first frame update
    public override void NetworkStart()
    {
        NetworkObject.DestroyWithScene = false;
    }
}
