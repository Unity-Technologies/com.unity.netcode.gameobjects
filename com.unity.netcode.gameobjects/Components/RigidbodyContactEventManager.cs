#if NGO_DAMODE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Netcode.Components
{
    public interface IContactEventHandler
    {
        Rigidbody GetRigidbody();

        void ContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default);
    }

    [AddComponentMenu("Netcode/Rigidbody Contact Event Manager")]
    public class RigidbodyContactEventManager : MonoBehaviour
    {
        public static RigidbodyContactEventManager Instance { get; private set; }

        private struct JobResultStruct
        {
            public bool HasCollisionStay;
            public int ThisInstanceID;
            public int OtherInstanceID;
            public Vector3 AverageNormal;
            public Vector3 AverageCollisionStayNormal;
            public Vector3 ContactPoint;
        }

        private NativeArray<JobResultStruct> m_ResultsArray;
        private int m_Count = 0;
        private JobHandle m_JobHandle;

        private readonly Dictionary<int, Rigidbody> m_RigidbodyMapping = new Dictionary<int, Rigidbody>();
        private readonly Dictionary<int, IContactEventHandler> m_HandlerMapping = new Dictionary<int, IContactEventHandler>();

        private void OnEnable()
        {
            m_ResultsArray = new NativeArray<JobResultStruct>(16, Allocator.Persistent);
            Physics.ContactEvent += Physics_ContactEvent;
            if (Instance != null)
            {
                NetworkLog.LogError($"[Invalid][Multiple Instances] Found more than one instance of {nameof(RigidbodyContactEventManager)}: {name} and {Instance.name}");
                NetworkLog.LogError($"[Disable][Additional Instance] Disabling {name} instance!");
                gameObject.SetActive(false);
                return;
            }
            Instance = this;
        }

        public void RegisterHandler(IContactEventHandler contactEventHandler, bool register = true)
        {
            var rigidbody = contactEventHandler.GetRigidbody();
            var instanceId = rigidbody.GetInstanceID();
            if (register)
            {
                if (!m_RigidbodyMapping.ContainsKey(instanceId))
                {
                    m_RigidbodyMapping.Add(instanceId, rigidbody);
                }

                if (!m_HandlerMapping.ContainsKey(instanceId))
                {
                    m_HandlerMapping.Add(instanceId, contactEventHandler);
                }
            }
            else
            {
                m_RigidbodyMapping.Remove(instanceId);
                m_HandlerMapping.Remove(instanceId);
            }
        }

        private void OnDisable()
        {
            m_JobHandle.Complete();
            m_ResultsArray.Dispose();

            Physics.ContactEvent -= Physics_ContactEvent;

            m_RigidbodyMapping.Clear();
            Instance = null;
        }

        private bool m_HasCollisions;
        private int m_CurrentCount = 0;

        private void ProcessCollisions()
        {
            // Process all collisions
            for (int i = 0; i < m_Count; i++)
            {
                var thisInstanceID = m_ResultsArray[i].ThisInstanceID;
                var otherInstanceID = m_ResultsArray[i].OtherInstanceID;
                var rb0Valid = thisInstanceID != 0 && m_RigidbodyMapping.ContainsKey(thisInstanceID);
                var rb1Valid = otherInstanceID != 0 && m_RigidbodyMapping.ContainsKey(otherInstanceID);
                // Only notify registered rigid bodies.
                if (!rb0Valid || !rb1Valid || !m_HandlerMapping.ContainsKey(thisInstanceID))
                {
                    continue;
                }
                if (m_ResultsArray[i].HasCollisionStay)
                {
                    m_HandlerMapping[thisInstanceID].ContactEvent(m_EventId, m_ResultsArray[i].AverageNormal, m_RigidbodyMapping[otherInstanceID], m_ResultsArray[i].ContactPoint, m_ResultsArray[i].HasCollisionStay, m_ResultsArray[i].AverageCollisionStayNormal);
                }
                else
                {
                    m_HandlerMapping[thisInstanceID].ContactEvent(m_EventId, m_ResultsArray[i].AverageNormal, m_RigidbodyMapping[otherInstanceID], m_ResultsArray[i].ContactPoint);
                }
            }
        }

        private void FixedUpdate()
        {
            // Only process new collisions
            if (!m_HasCollisions && m_CurrentCount == 0)
            {
                return;
            }

            // This assures we won't process the same collision
            // set after it has been processed.
            if (m_HasCollisions)
            {
                m_CurrentCount = m_Count;
                m_HasCollisions = false;
                m_JobHandle.Complete();
            }
            ProcessCollisions();
        }

        private void LateUpdate()
        {
            m_CurrentCount = 0;
        }

        private ulong m_EventId;
        private void Physics_ContactEvent(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly pairHeaders)
        {
            m_EventId++;
            m_HasCollisions = true;
            int n = pairHeaders.Length;
            if (m_ResultsArray.Length < n)
            {
                m_ResultsArray.Dispose();
                m_ResultsArray = new NativeArray<JobResultStruct>(Mathf.NextPowerOfTwo(n), Allocator.Persistent);
            }
            m_Count = n;
            var job = new GetCollisionsJob()
            {
                PairedHeaders = pairHeaders,
                ResultsArray = m_ResultsArray
            };
            m_JobHandle = job.Schedule(n, 256);
        }

        private struct GetCollisionsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<ContactPairHeader>.ReadOnly PairedHeaders;

            public NativeArray<JobResultStruct> ResultsArray;

            public void Execute(int index)
            {
                Vector3 averageNormal = Vector3.zero;
                Vector3 averagePoint = Vector3.zero;
                Vector3 averageCollisionStay = Vector3.zero;
                int count = 0;
                int collisionStaycount = 0;
                int positionCount = 0;
                for (int j = 0; j < PairedHeaders[index].PairCount; j++)
                {
                    ref readonly var pair = ref PairedHeaders[index].GetContactPair(j);

                    if (pair.IsCollisionExit)
                    {
                        continue;
                    }

                    for (int k = 0; k < pair.ContactCount; k++)
                    {
                        ref readonly var contact = ref pair.GetContactPoint(k);
                        averagePoint += contact.Position;
                        positionCount++;
                        if (!pair.IsCollisionStay)
                        {
                            averageNormal += contact.Normal;
                            count++;
                        }
                        else
                        {
                            averageCollisionStay += contact.Normal;
                            collisionStaycount++;
                        }
                    }
                }

                if (count != 0)
                {
                    averageNormal /= count;
                }

                if (collisionStaycount != 0)
                {
                    averageCollisionStay /= collisionStaycount;
                }

                if (positionCount != 0)
                {
                    averagePoint /= positionCount;
                }

                var result = new JobResultStruct()
                {
                    ThisInstanceID = PairedHeaders[index].BodyInstanceID,
                    OtherInstanceID = PairedHeaders[index].OtherBodyInstanceID,
                    AverageNormal = averageNormal,
                    HasCollisionStay = collisionStaycount != 0,
                    AverageCollisionStayNormal = averageCollisionStay,
                    ContactPoint = averagePoint
                };

                ResultsArray[index] = result;
            }
        }
    }
}
#endif
