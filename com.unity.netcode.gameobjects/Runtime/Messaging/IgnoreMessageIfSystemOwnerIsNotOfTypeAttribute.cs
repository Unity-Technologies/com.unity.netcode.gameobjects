using System;

namespace Unity.Netcode
{
    internal class IgnoreMessageIfSystemOwnerIsNotOfTypeAttribute : Attribute
    {
        public Type BoundType;

        public IgnoreMessageIfSystemOwnerIsNotOfTypeAttribute(Type boundType)
        {
            BoundType = boundType;
        }
    }
}
