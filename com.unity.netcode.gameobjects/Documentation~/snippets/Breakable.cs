using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// This script handles the logic for a simple "single-shot" breakable object like a pot, or
    /// other stationary items with arbitrary amounts of HP, like spawner-portal crystals.
    /// Visualization for these objects works by swapping a "broken" prefab at the moment of breakage. The broken prefab
    /// then handles the pesky details of actually falling apart.
    /// </summary>
    public class Breakable : NetworkBehaviour, IDamageable, ITargetable
    {
        [Header("Server Logic")]
        [SerializeField]
        [Tooltip("If left blank, this breakable effectively has 1 hit point")]
        IntVariable m_MaxHealth;

        [SerializeField]
        [Tooltip("If this breakable will have hit points, add a NetworkHealthState component to this GameObject")]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        [Tooltip("Indicate which special interaction behaviors are needed for this breakable")]
        IDamageable.SpecialDamageFlags m_SpecialDamageFlags;

        [Header("Visualization")]
        [SerializeField]
        private GameObject m_BrokenPrefab;

        [SerializeField]
        [Tooltip("If set, will be used instead of BrokenPrefab when new players join, skipping transition effects.")]
        private GameObject m_PrebrokenPrefab;

        [SerializeField]
        [Tooltip("We use this transform's position and rotation when creating the prefab. (Defaults to self)")]
        private Transform m_BrokenPrefabPos;

        [SerializeField]
        private GameObject[] m_UnbrokenGameObjects;

        /// <summary>
        /// Is the item broken or not?
        /// </summary>
        public NetworkVariable<bool> IsBroken;

        public bool IsNpc { get { return true; } }

        public bool IsValidTarget { get { return !IsBroken.Value; } }

        private GameObject m_CurrentBrokenVisualization;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (m_MaxHealth && m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
                }
            }

            if (IsClient)
            {
                IsBroken.OnValueChanged += OnBreakableStateChanged;

                if (IsBroken.Value == true)
                {
                    PerformBreakVisualization(true);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                IsBroken.OnValueChanged -= OnBreakableStateChanged;
            }
        }

        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (HP < 0)
            {
                if (inflicter && !inflicter.IsNpc)
                {
                    bool isNotDamagedByPlayers = (GetSpecialDamageFlags() & IDamageable.SpecialDamageFlags.NotDamagedByPlayers) == IDamageable.SpecialDamageFlags.NotDamagedByPlayers;
                    if (isNotDamagedByPlayers)
                    {
                        // a player tried to damage us, but we are immune to player damage!
                        return;
                    }
                }

                if (m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.Value =
                        Mathf.Clamp(m_NetworkHealthState.HitPoints.Value + HP, 0, m_MaxHealth.Value);
                    if (m_NetworkHealthState.HitPoints.Value <= 0)
                    {
                        Break();
                    }
                }
                else
                {
                    //any damage at all is enough to slay me.
                    Break();
                }
            }
        }

        private void Break()
        {
            IsBroken.Value = true;
            if (m_Collider)
                m_Collider.enabled = false;
        }

        public void Unbreak()
        {
            IsBroken.Value = false;
            if (m_Collider)
                m_Collider.enabled = true;
            if (m_MaxHealth && m_NetworkHealthState)
                m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
        }

        public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return m_SpecialDamageFlags;
        }

        public bool IsDamageable()
        {
            // you can damage this breakable until it's broken!
            return !IsBroken.Value;
        }

        private void OnBreakableStateChanged(bool wasBroken, bool isBroken)
        {
            if (!wasBroken && isBroken)
            {
                PerformBreakVisualization(false);
            }
            else if (wasBroken && !isBroken)
            {
                PerformUnbreakVisualization();
            }
        }

        private void PerformBreakVisualization(bool onStart)
        {
            foreach (var unbrokenGameObject in m_UnbrokenGameObjects)
            {
                if (unbrokenGameObject)
                {
                    unbrokenGameObject.SetActive(false);
                }
            }

            if (m_CurrentBrokenVisualization)
                Destroy(m_CurrentBrokenVisualization); // just a safety check, should be null when we get here

            GameObject brokenPrefab = (onStart && m_PrebrokenPrefab != null) ? m_PrebrokenPrefab : m_BrokenPrefab;
            if (brokenPrefab)
            {
                m_CurrentBrokenVisualization = Instantiate(brokenPrefab, m_BrokenPrefabPos.position, m_BrokenPrefabPos.rotation, transform);
            }
        }

        private void PerformUnbreakVisualization()
        {
            if (m_CurrentBrokenVisualization)
            {
                Destroy(m_CurrentBrokenVisualization);
            }
            foreach (var unbrokenGameObject in m_UnbrokenGameObjects)
            {
                if (unbrokenGameObject)
                {
                    unbrokenGameObject.SetActive(true);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!m_Collider)
                m_Collider = GetComponent<Collider>();
            if (!m_NetworkHealthState)
                m_NetworkHealthState = GetComponent<NetworkHealthState>();
            if (!m_BrokenPrefabPos)
                m_BrokenPrefabPos = transform;
        }
#endif
    }


}

