using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class CameraFollower : MonoBehaviour
    {
        public GameObject ObjectToFollow;
        public float CameraSmoothing;
        private Vector3 m_Offset;

        private void Awake()
        {
            m_Offset = transform.position;

        }

        private void FixedUpdate()
        {
            if (ObjectToFollow != null)
            {
                transform.LookAt(ObjectToFollow.transform, Vector3.up);
                transform.position = Vector3.Lerp(transform.position, ObjectToFollow.transform.position + m_Offset, CameraSmoothing * Time.deltaTime);
            }
        }
    }
}
