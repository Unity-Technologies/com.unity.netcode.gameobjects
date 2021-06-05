using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CustomNetSO", menuName = "Sam/Network SO", order = 0)]
public class MyNetworkedScriptableObject : NetworkScriptableObject
{
    [SerializeField]
    public NetworkVariableString myText = new NetworkVariableString(settings:new NetworkVariableSettings(){WritePermission = NetworkVariablePermission.Everyone});
}
