#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> m_Keys = new List<TKey>();
    [SerializeField] private List<TValue> m_Values = new List<TValue>();

    public Type Type { get; private set; }

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {
        m_Keys.Clear();
        m_Values.Clear();
        foreach (var pair in this)
        {
            m_Keys.Add(pair.Key);
            m_Values.Add(pair.Value);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        Clear();

        if (m_Keys.Count != m_Values.Count)
        {
            Debug.LogError($"[SerializableDictionary] Skipping populate during {nameof(OnAfterDeserialize)} as the key count ({m_Keys.Count}) do " +
                $"not match the ({m_Values.Count}) values. This could mean the key or value types are not serializable.");
            m_Keys.Clear();
            m_Values.Clear();
            return;
        }

        for (var i = 0; i < m_Keys.Count; i++)
        {
            Add(m_Keys[i], m_Values[i]);
        }
    }

    public SerializableDictionary(IEqualityComparer<TKey> equalityComparer) : base(equalityComparer)
    {

    }
}
#endif
