using System;
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

        public static bool IsTriggerTest;
        public static bool IsLateJoinSyncTest;

        public static void Initialize()
        {
            ServerSideInstance = null;
            ClientSideInstances.Clear();
        }

        private Animator m_Animator;
        private NetworkAnimator m_NetworkAnimator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            m_NetworkAnimator.InternalNotifyAnimationMessage = NotifyAnimSync;
        }

        internal List<NetworkAnimator.AnimationMessage> AnimationMessages = new List<NetworkAnimator.AnimationMessage>();
        private void NotifyAnimSync(NetworkAnimator.AnimationMessage animationMessage)
        {
            AnimationMessages.Add(animationMessage);
        }

        public override void OnNetworkSpawn()
        {
            if (IsTriggerTest)
            {
                Debug.Log($"[AnimatorTestHelper][{IsServer}] {NetworkManager.name}");
            }

            m_Animator = GetComponent<Animator>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
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

        public Action<bool, bool> OnCheckIsServerIsClient;

        public override void OnNetworkDespawn()
        {
            // This verifies the issue where IsServer and IsClient were
            // being reset prior to NetworkObjects being despawned during
            // the shutdown period.
            OnCheckIsServerIsClient?.Invoke(IsServer, IsClient);
            if (ClientSideInstances.ContainsKey(NetworkManager.LocalClientId))
            {
                ClientSideInstances.Remove(NetworkManager.LocalClientId);
            }
            base.OnNetworkDespawn();
        }

        public class ParameterValues
        {
            public float FloatValue;
            public int IntValue;
            public bool BoolValue;

            public bool ValuesMatch(ParameterValues parameters, bool printOutput = false)
            {
                if (printOutput)
                {
                    Debug.Log($"[ValuesMatch] Current values: {ValuesToString()}");
                    Debug.Log($"[ValuesMatch] Values to match {parameters.ValuesToString()}");
                }
                return parameters.BoolValue == BoolValue && parameters.FloatValue == FloatValue && parameters.IntValue == IntValue;
            }

            public string ValuesToString()
            {
                return $"{FloatValue}, {IntValue}, {BoolValue}";
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

        public bool GetCurrentTriggerState()
        {
            return m_Animator.GetBool("TestTrigger");
        }

        public void SetTrigger(string name = "TestTrigger")
        {
            m_NetworkAnimator.SetTrigger(name);
        }

        public void SetLateJoinParam(bool isEnabled)
        {
            m_Animator.SetBool("LateJoinTest", isEnabled);
        }

        public Animator GetAnimator()
        {
            return m_Animator;
        }
    }
}
