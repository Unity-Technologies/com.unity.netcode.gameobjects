using System;
using System.Collections;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    public class ServerAnimationHandler : NetworkBehaviour
    {
        [SerializeField]
        NetworkAnimator m_NetworkAnimator;

        [SerializeField]
        VisualizationConfiguration m_VisualizationConfiguration;

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        public NetworkAnimator NetworkAnimator => m_NetworkAnimator;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Wait until next frame before registering on OnValueChanged to make sure NetworkAnimator has spawned before.
                StartCoroutine(WaitToRegisterOnLifeStateChanged());
            }
        }

        IEnumerator WaitToRegisterOnLifeStateChanged()
        {
            yield return new WaitForEndOfFrame();
            m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            if (m_NetworkLifeState.LifeState.Value != LifeState.Alive)
            {
                OnLifeStateChanged(LifeState.Alive, m_NetworkLifeState.LifeState.Value);
            }
        }

        void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            switch (newValue)
            {
                case LifeState.Alive:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.AliveStateTriggerID);
                    break;
                case LifeState.Fainted:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.FaintedStateTriggerID);
                    break;
                case LifeState.Dead:
                    NetworkAnimator.SetTrigger(m_VisualizationConfiguration.DeadStateTriggerID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                m_NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            }
        }
    }
}
