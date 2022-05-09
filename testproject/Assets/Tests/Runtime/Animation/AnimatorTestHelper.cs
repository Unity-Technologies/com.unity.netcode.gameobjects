using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace TestProject.RuntimeTests
{
    public class AnimatorTestHelper : NetworkBehaviour
    {
        public static AnimatorTestHelper ServerSideInstance { get; private set; }

        public readonly static Dictionary<ulong, AnimatorTestHelper> ClientSideInstances = new Dictionary<ulong, AnimatorTestHelper>();

        public static void Initialize()
        {
            ServerSideInstance = null;
            ClientSideInstances.Clear();
        }

        private Animator m_Animator;
        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerSideInstance = this;
            }
            else
            {
                if (!ClientSideInstances.ContainsKey(NetworkManager.LocalClientId))
                {
                    ClientSideInstances.Add(NetworkManager.LocalClientId, this);
                }
            }
            base.OnNetworkSpawn();
        }

        public class ParameterValues
        {
            public float FloatValue;
            public int IntValue;
            public bool BoolValue;

            public bool ValuesMatch(ParameterValues parameters)
            {
                return parameters.BoolValue = BoolValue && parameters.FloatValue == FloatValue && parameters.IntValue == IntValue;
            }
        }

        public ParameterValues GetParameterValues()
        {
            return new ParameterValues() { FloatValue = m_Animator.GetFloat("TestFloat"), IntValue = m_Animator.GetInteger("TestInt"), BoolValue = m_Animator.GetBool("TestBool") };
        }

        public void UpdateParameters(ParameterValues parameterValues)
        {
            m_Animator.SetFloat("TestFloat", parameterValues.FloatValue);
            m_Animator.SetInteger("TestInt", parameterValues.IntValue);
            m_Animator.SetBool("TestBool", parameterValues.BoolValue);
        }
    }
}
