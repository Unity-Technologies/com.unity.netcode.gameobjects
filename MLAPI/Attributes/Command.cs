using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class Command : Attribute
    {
        public string channelName = "MLAPI_INTERNAL";
    }
}
