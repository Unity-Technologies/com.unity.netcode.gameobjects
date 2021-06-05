using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(order = 0, menuName = "Sam/create bool", fileName = "myCustomBool")]
public class BoolScriptableObject : ScriptableObject
{
    [SerializeField]
    public bool value;
}
