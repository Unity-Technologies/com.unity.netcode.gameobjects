#if UNITY_INCLUDE_TESTS
using System;

namespace Unity.Netcode.TestHelpers.Transport
{
#if UNITY_EDITOR
    public class DontShowInTransportDropdownAttribute : Attribute
    {
    }
#endif
}
#endif
