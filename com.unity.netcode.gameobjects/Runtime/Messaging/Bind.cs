using System;

namespace Unity.Netcode
{
    public class Bind : Attribute
    {
        public Type BoundType;

        public Bind(Type boundType)
        {
            BoundType = boundType;
        }
    }
}