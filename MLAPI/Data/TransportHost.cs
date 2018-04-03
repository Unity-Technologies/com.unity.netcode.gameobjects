using System;

namespace MLAPI.Data
{
    [Serializable]
    public class TransportHost
    {
        public string Name = Guid.NewGuid().ToString().Replace("-", "");
        public int Port = 7777;
        public bool Websockets = false;
    }
}
