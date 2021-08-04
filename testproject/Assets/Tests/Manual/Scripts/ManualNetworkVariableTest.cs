using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
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

        private string m_Problems = string.Empty;
        private bool m_Started = false;

        private const int k_EndValue = 1000;

        private void Start()
        {
            m_TestList.SetNetworkBehaviour(this);
            m_TestSet.SetNetworkBehaviour(this);
            m_TestDictionary.SetNetworkBehaviour(this);

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
            if (!m_Started && NetworkManager.ConnectedClientsList.Count > 0)
            {
                m_Started = true;
            }
            if (m_Started)
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
            if (!IsOwner && !IsServer && m_TestVar.Value >= k_EndValue)
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

                if (m_Problems == "")
                {
                    Debug.Log("**** TEST PASSED ****");
                }
                else
                {
                    Debug.Log("**** TEST FAILED ****");
                    Debug.Log(m_Problems);
                }
            }

            if (m_TestVar.Value >= k_EndValue)
            {
                enabled = false;
            }
        }
    }
}
