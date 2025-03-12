#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableType : IEquatable<SerializableType>
{
    [HideInInspector]
    [SerializeField]
    private string m_AssemblyQualifiedName;
    private Type m_Type;
    public Type Type
    {
        get
        {
            if (m_Type == null & !string.IsNullOrEmpty(m_AssemblyQualifiedName))
            {
                m_Type = Type.GetType(m_AssemblyQualifiedName);
            }
            return m_Type;
        }
        set
        {
            m_Type = value;
            m_AssemblyQualifiedName = value.AssemblyQualifiedName;
        }
    }

    public override int GetHashCode()
    {
        return m_AssemblyQualifiedName.GetHashCode();
    }

    public bool Equals(SerializableType other)
    {
        var isEqual = Type.Equals(other.Type);
        return isEqual;
    }

    public SerializableType(Type type)
    {
        Type = type;
    }
    public SerializableType()
    {

    }
}

public class SerializableTypeComparer : IEqualityComparer<SerializableType>
{
    public bool Equals(SerializableType first, SerializableType second)
    {
        if (first is null || second is null)
        {
            return false;
        }
        return first.Equals(second);
    }

    public int GetHashCode(SerializableType serializableType)
    {
        return serializableType.GetHashCode();
    }
}
#endif
