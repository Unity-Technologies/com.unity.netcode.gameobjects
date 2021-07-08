using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

public class NetworkAnimatorTestManager : MonoBehaviour
{
   public void StartHost()
   {
      GetComponent<NetworkManager>().StartHost();
   }

   public void StartClient()
   {
      GetComponent<NetworkManager>().StartClient();
   }
}
