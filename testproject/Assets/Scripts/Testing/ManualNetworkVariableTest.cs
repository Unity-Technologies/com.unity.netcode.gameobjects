using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;

namespace MLAPI
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("MLAPI/ManualNetworkVariableTest")]
    public class ManualNetworkVariableTest : NetworkBehaviour
    {
        // testing NetworkList
        private NetworkList<string> m_TestList = new NetworkList<string>();
        private bool m_GotNetworkList = false;

        // testing NetworkSet
        private NetworkSet<string> m_TestSet = new NetworkSet<string>();
        private bool m_GotNetworkSet = false;

        // testing NetworkDictionary
        private NetworkDictionary<int, string> m_TestDictionary = new NetworkDictionary<int, string>();
        private bool m_GotNetworkDictionary = false;

        // testing NetworkVariable, especially ticks
        private NetworkVariable<int> m_TestVar = new NetworkVariable<int>();
        private int m_MinDelta = 0;
        private int m_MaxDelta = 0;
        private int m_LastRemoteTick = 0;
        private bool m_Valid = false;
        private string m_Problems = string.Empty;
        private int m_Count = 0;

        private const int k_EndIterations = 1000;

        private void Start()
        {
            m_TestVar.OnValueChanged += ValueChanged;
            m_TestVar.Settings.WritePermission = NetworkVariablePermission.Everyone;

            m_TestList.OnListChanged += ListChanged;
            m_TestList.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;

            m_TestSet.OnSetChanged += SetChanged;
            m_TestSet.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;

            m_TestDictionary.OnDictionaryChanged += DictionaryChanged;
            m_TestDictionary.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;

            if (IsOwner)
            {
                m_TestVar.Value = 0;
                Debug.Log("We'll be sending " + MyMessage());
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                m_TestVar.Value = m_TestVar.Value + 1;
                m_TestList.Add(MyMessage());
                ((ICollection<string>)m_TestSet).Add(MyMessage());
                m_TestDictionary[0] = MyMessage();
            }
        }

        private string MyMessage()
        {
            return "Message from " + NetworkObjectId;
        }

        private void ListChanged(NetworkListEvent<string> listEvent)
        {
            if (!IsOwner && !m_GotNetworkList)
            {
                Debug.Log("Received: " + listEvent.Value);
                m_GotNetworkList = true;
            }
        }

        private void SetChanged(NetworkSetEvent<string> setEvent)
        {
            if (!IsOwner && !m_GotNetworkSet)
            {
                Debug.Log("Received: " + setEvent.Value);
                m_GotNetworkSet = true;
            }
        }

        private void DictionaryChanged(NetworkDictionaryEvent<int, string> dictionaryEvent)
        {
            if (!IsOwner && !m_GotNetworkSet)
            {
                Debug.Log("Received: " + dictionaryEvent.Key + ":" + dictionaryEvent.Value);
                m_GotNetworkDictionary = true;
            }
        }

        private void ValueChanged(int before, int after)
        {
            if (!IsOwner && !IsServer)
            {
                // compute the delta in tick between client and server,
                // as seen from the client, when it receives a value not from itself
                if (m_TestVar.LocalTick != NetworkTickSystem.NoTick)
                {
                    int delta = m_TestVar.LocalTick - m_TestVar.RemoteTick;
                    m_Count++;

                    if (!m_Valid)
                    {
                        m_Valid = true;
                        m_MinDelta = delta;
                        m_MaxDelta = delta;
                        m_LastRemoteTick = m_TestVar.RemoteTick;
                    }
                    else
                    {
                        m_MinDelta = Math.Min(delta, m_MinDelta);
                        m_MaxDelta = Math.Max(delta, m_MaxDelta);

                        // tick should not go backward until wrap around (which should be a long time)
                        if (m_TestVar.RemoteTick == m_LastRemoteTick)
                        {
                            m_Problems += "Same remote tick receive twice\n";
                        }
                        else if (m_TestVar.RemoteTick < m_LastRemoteTick)
                        {
                            m_Problems += "Ticks went backward\n";
                        }
                    }
                }
            }

            if (m_Count == k_EndIterations)
            {
                // Let's be reasonable and allow a 5 tick difference
                // that could be due to timing difference, lag, queueing

                if (!m_GotNetworkList)
                {
                    m_Problems += "Didn't receive any NetworkList updates from other machines";
                }

                if (!m_GotNetworkSet)
                {
                    m_Problems += "Didn't receive any NetworkSet updates from other machines";
                }

                if (!m_GotNetworkDictionary)
                {
                    m_Problems += "Didn't receive any NetworkDictionary updates from other machines";
                }

                if (Math.Abs(m_MaxDelta - m_MinDelta) > 5)
                {
                    m_Problems += "Delta range: " + m_MinDelta + " + " + m_MaxDelta + "\n";
                }

                if (m_Problems == "")
                {
                    Debug.Log("**** TEST PASSED ****");
                }
                else
                {
                    Debug.Log("**** TEST FAILED ****");
                    Debug.Log(m_Problems);
                }
                enabled = false;
            }
        }
    }
}
