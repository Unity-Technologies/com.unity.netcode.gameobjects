#if UNITY_EDITOR
using System;
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

    public bool Equals(SerializableType other)
    {
        return Type == other.Type;
    }

    public SerializableType(Type type)
    {
        Type = type;
    }
    public SerializableType()
    {

    }
}
#endif
