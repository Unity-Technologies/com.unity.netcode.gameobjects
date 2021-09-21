using System;

namespace Unity.Netcode
{
    /// <summary>
    /// This attribute is to support testing. When this attribute is applied to a message,
    /// when reflection is iterating the message types, it will ignore the affected message
    /// unless the type of the system owner passed to the messaging system is the same as the
    /// type provided here.
    /// </summary>
    internal class IgnoreMessageIfSystemOwnerIsNotOfTypeAttribute : Attribute
    {
        public Type BoundType;

        public IgnoreMessageIfSystemOwnerIsNotOfTypeAttribute(Type boundType)
        {
            BoundType = boundType;
        }
    }
}
