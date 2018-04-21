using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// The attribute to use for fields that should be ignored by the BinarySerializer
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BinaryIgnore : Attribute
    {

    }
}
