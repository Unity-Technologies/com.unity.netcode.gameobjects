using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests
{
    public class NetworkUpdateLoopTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
        }

        private struct NetworkUpdateCallbacks
        {
            public Action OnInitialization;
            public Action OnEarlyUpdate;
            public Action OnFixedUpdate;
            public Action OnPreUpdate;
            public Action OnUpdate;
            public Action OnPreLateUpdate;
            public Action OnPostLateUpdate;
        }

        private class MyPlainScript : IDisposable, INetworkUpdateSystem
        {
            public NetworkUpdateCallbacks UpdateCallbacks;

            public void Initialize()
            {
                this.RegisterAllNetworkUpdates();
            }

            public void NetworkUpdate(NetworkUpdateStage updateStage)
            {
                // todo
            }

            public void Dispose()
            {
                this.UnregisterAllNetworkUpdates();
            }
        }

        [UnityTest]
        public IEnumerator UpdateStagesPlainStandard()
        {
            // todo
            yield return new WaitForEndOfFrame();
        }

        private struct MonoBehaviourCallbacks
        {
            public Action OnFixedUpdate;
            public Action OnUpdate;
            public Action OnLateUpdate;
            public Action OnGUI;
        }

        private class MyGameScript : MonoBehaviour, INetworkUpdateSystem
        {
            public NetworkUpdateCallbacks UpdateCallbacks;
            public MonoBehaviourCallbacks BehaviourCallbacks;

            private void Awake()
            {
                this.RegisterNetworkUpdate();
            }

            public void NetworkUpdate(NetworkUpdateStage updateStage)
            {
                // todo
            }

            private void OnDestroy()
            {
                this.UnregisterNetworkUpdate();
            }
        }

        [UnityTest]
        public IEnumerator UpdateStagesMonoBehaviour()
        {
            // todo
            yield return new WaitForEndOfFrame();
        }

        private class MyComplexScript : IDisposable, INetworkUpdateSystem
        {
            public NetworkUpdateCallbacks UpdateCallbacks;
            public MonoBehaviourCallbacks BehaviourCallbacks;

            public void Initialize()
            {
                this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
                this.RegisterNetworkUpdate(NetworkUpdateStage.FixedUpdate);
                this.RegisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);
            }

            public void NetworkUpdate(NetworkUpdateStage updateStage)
            {
                // todo
            }

            public void Dispose()
            {
                this.UnregisterAllNetworkUpdates();
            }
        }

        [UnityTest]
        public IEnumerator UpdateStagesMultipleMixed()
        {
            // todo
            yield return new WaitForEndOfFrame();
        }
    }
}