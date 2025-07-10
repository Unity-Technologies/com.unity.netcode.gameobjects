using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Logic that handles an FX-based pretend-missile.
    /// </summary>
    public class FXProjectile : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> m_ProjectileGraphics;

        [SerializeField]
        private List<GameObject> m_TargetHitGraphics;

        [SerializeField]
        private List<GameObject> m_TargetMissedGraphics;

        [SerializeField]
        [Tooltip("If this projectile plays an impact particle, how long should we stay alive for it to keep playing?")]
        private float m_PostImpactDurationSeconds = 1;

        private Vector3 m_StartPoint;
        private Transform m_TargetDestination; // null if we're a "miss" projectile (i.e. we hit nothing)
        private Vector3 m_MissDestination; // only used if m_TargetDestination is null
        private float m_FlightDuration;
        private float m_Age;
        private bool m_HasImpacted;

        public void Initialize(Vector3 startPoint, Transform target, Vector3 missPos, float flightTime)
        {
            m_StartPoint = startPoint;
            m_TargetDestination = target;
            m_MissDestination = missPos;
            m_FlightDuration = flightTime;
            m_HasImpacted = false;

            // the projectile graphics are actually already enabled in the prefab, but just in case, turn them on
            foreach (var projectileGO in m_ProjectileGraphics)
            {
                projectileGO.SetActive(true);
            }
        }

        public void Cancel()
        {
            // we could play a "poof" particle... but for now we just instantly disappear
            Destroy(gameObject);
        }

        private void Update()
        {
            m_Age += Time.deltaTime;
            if (!m_HasImpacted)
            {
                if (m_Age >= m_FlightDuration)
                {
                    Impact();
                }
                else
                {
                    // we're flying through the air. Reposition ourselves to be closer to the destination
                    float progress = m_Age / m_FlightDuration;
                    transform.position = Vector3.Lerp(m_StartPoint, m_TargetDestination ? m_TargetDestination.position : m_MissDestination, progress);
                }
            }
            else if (m_Age >= m_FlightDuration + m_PostImpactDurationSeconds)
            {
                Destroy(gameObject);
            }
        }


        private void Impact()
        {
            m_HasImpacted = true;

            foreach (var projectileGO in m_ProjectileGraphics)
            {
                projectileGO.SetActive(false);
            }

            // is it impacting an actual enemy? We allow different graphics for the "miss" case
            if (m_TargetDestination)
            {
                foreach (var hitGraphicGO in m_TargetHitGraphics)
                {
                    hitGraphicGO.SetActive(true);
                }
            }
            else
            {
                foreach (var missGraphicGO in m_TargetMissedGraphics)
                {
                    missGraphicGO.SetActive(true);
                }
            }
        }
    }
}
