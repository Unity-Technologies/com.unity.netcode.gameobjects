using System;

namespace MLAPI.Serialization
{
    /// <summary>
    /// Attribute used on static methods to me marked as a custom serializer for a specific type
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class SerializerAttribute : Attribute
    {
        
    }

    /// <summary>
    /// Attribute used on static methods to me marked as a custom deserializer for a specific type
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class DeserializerAttribute : Attribute
    {
        
    }
}