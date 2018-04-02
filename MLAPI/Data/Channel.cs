using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace MLAPI.Data
{
    [Serializable]
    public class Channel
    {
        public string Name;
        public QosType Type;
    }
}
