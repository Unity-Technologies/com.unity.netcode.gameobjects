using System.Collections.Generic;
using UnityEngine;

namespace MLAPI
{
    public class NetworkedClient
    {
        public int ClientId;
        public GameObject PlayerObject;
        public List<NetworkedObject> OwnedObjects = new List<NetworkedObject>();
        public byte[] AesKey;
    }
}
