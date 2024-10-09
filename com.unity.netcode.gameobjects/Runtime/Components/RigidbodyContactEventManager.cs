#if COM_UNITY_MODULES_PHYSICS
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Netcode.Components
{
    public struct ContactEventHandlerInfo
    {
        public bool ProvideNonRigidBodyContactEvents;
        public bool HasContactEventPriority;
    }

    public interface IContactEventHandler
    {
        Rigidbody GetRigidbody();

        void ContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default);
    }

    public interface IContactEventHandlerWithInfo : IContactEventHandler
    {
        ContactEventHandlerInfo GetContactEventHandlerInfo();
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
        private readonly Dictionary<int, ContactEventHandlerInfo> m_HandlerInfo = new Dictionary<int, ContactEventHandlerInfo>();

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

                if (!m_HandlerInfo.ContainsKey(instanceId))
                {
                    var handlerInfo = new ContactEventHandlerInfo()
                    {
                        HasContactEventPriority = true,
                        ProvideNonRigidBodyContactEvents = false,
                    };
                    var handlerWithInfo = contactEventHandler as IContactEventHandlerWithInfo;

                    if (handlerWithInfo != null)
                    {
                        handlerInfo = handlerWithInfo.GetContactEventHandlerInfo();
                    }
                    m_HandlerInfo.Add(instanceId, handlerInfo);
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
            foreach (var contactEventHandler in m_HandlerMapping)
            {
                var handlerWithInfo = contactEventHandler.Value as IContactEventHandlerWithInfo;

                if (handlerWithInfo != null)
                {
                    m_HandlerInfo[contactEventHandler.Key] = handlerWithInfo.GetContactEventHandlerInfo();
                }
            }

            ContactEventHandlerInfo contactEventHandlerInfo0;
            ContactEventHandlerInfo contactEventHandlerInfo1;

            // Process all collisions
            for (int i = 0; i < m_Count; i++)
            {
                var thisInstanceID = m_ResultsArray[i].ThisInstanceID;
                var otherInstanceID = m_ResultsArray[i].OtherInstanceID;
                var contactHandler0 = (IContactEventHandler)null;
                var contactHandler1 = (IContactEventHandler)null;
                var preferredContactHandler = (IContactEventHandler)null;
                var preferredContactHandlerNonRigidbody = false;
                var preferredRigidbody = (Rigidbody)null;
                var otherContactHandler = (IContactEventHandler)null;
                var otherRigidbody = (Rigidbody)null;

                var otherContactHandlerNonRigidbody = false;

                if (m_RigidbodyMapping.ContainsKey(thisInstanceID))
                {
                    contactHandler0 = m_HandlerMapping[thisInstanceID];
                    contactEventHandlerInfo0 = m_HandlerInfo[thisInstanceID];
                    if (contactEventHandlerInfo0.HasContactEventPriority)
                    {
                        preferredContactHandler = contactHandler0;
                        preferredContactHandlerNonRigidbody = contactEventHandlerInfo0.ProvideNonRigidBodyContactEvents;
                        preferredRigidbody = m_RigidbodyMapping[thisInstanceID];
                    }
                    else
                    {
                        otherContactHandler = contactHandler0;
                        otherContactHandlerNonRigidbody = contactEventHandlerInfo0.ProvideNonRigidBodyContactEvents;
                        otherRigidbody = m_RigidbodyMapping[thisInstanceID];
                    }
                }

                if (m_RigidbodyMapping.ContainsKey(otherInstanceID))
                {
                    contactHandler1 = m_HandlerMapping[otherInstanceID];
                    contactEventHandlerInfo1 = m_HandlerInfo[otherInstanceID];
                    if (contactEventHandlerInfo1.HasContactEventPriority && preferredContactHandler == null)
                    {
                        preferredContactHandler = contactHandler1;
                        preferredContactHandlerNonRigidbody = contactEventHandlerInfo1.ProvideNonRigidBodyContactEvents;
                        preferredRigidbody = m_RigidbodyMapping[otherInstanceID];
                    }
                    else
                    {
                        otherContactHandler = contactHandler1;
                        otherContactHandlerNonRigidbody = contactEventHandlerInfo1.ProvideNonRigidBodyContactEvents;
                        otherRigidbody = m_RigidbodyMapping[otherInstanceID];
                    }
                }

                if (preferredContactHandler == null)
                {
                    if (otherContactHandler != null)
                    {
                        preferredContactHandler = otherContactHandler;
                        preferredContactHandlerNonRigidbody = otherContactHandlerNonRigidbody;
                        preferredRigidbody = otherRigidbody;
                        otherContactHandler = null;
                        otherContactHandlerNonRigidbody = false;
                        otherRigidbody = null;
                    }
                }

                if (preferredContactHandler == null || (preferredContactHandler != null && otherContactHandler == null && !preferredContactHandlerNonRigidbody))
                {
                    continue;
                }

                if (m_ResultsArray[i].HasCollisionStay)
                {
                    preferredContactHandler.ContactEvent(m_EventId, m_ResultsArray[i].AverageNormal, otherRigidbody, m_ResultsArray[i].ContactPoint, m_ResultsArray[i].HasCollisionStay, m_ResultsArray[i].AverageCollisionStayNormal);
                }
                else
                {
                    preferredContactHandler.ContactEvent(m_EventId, m_ResultsArray[i].AverageNormal, otherRigidbody, m_ResultsArray[i].ContactPoint);
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
                for (int j = 0; j < PairedHeaders[index].pairCount; j++)
                {
                    ref readonly var pair = ref PairedHeaders[index].GetContactPair(j);

                    if (pair.isCollisionExit)
                    {
                        continue;
                    }

                    for (int k = 0; k < pair.contactCount; k++)
                    {
                        ref readonly var contact = ref pair.GetContactPoint(k);
                        averagePoint += contact.position;
                        positionCount++;
                        if (!pair.isCollisionStay)
                        {
                            averageNormal += contact.normal;
                            count++;
                        }
                        else
                        {
                            averageCollisionStay += contact.normal;
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
                    ThisInstanceID = PairedHeaders[index].bodyInstanceID,
                    OtherInstanceID = PairedHeaders[index].otherBodyInstanceID,
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
