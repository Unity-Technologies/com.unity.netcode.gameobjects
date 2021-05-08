using System;
using UnityEngine;

namespace MLAPI
{
    public enum Authority
    {
        Owner = 1,
        Server = 2,
    }

    public enum NetworkBehaviourAuthority
    {
        FromNetworkObject = 0,
        Owner = 1,
        Server = 2,
    }

    public class ForceAuthorityAttribute : Attribute
    {
        public Authority Authority { get; }

        public ForceAuthorityAttribute(Authority authority)
        {
            Authority = authority;
        }
    }
}
