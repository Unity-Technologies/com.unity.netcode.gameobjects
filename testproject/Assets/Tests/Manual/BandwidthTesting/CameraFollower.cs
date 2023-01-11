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

        public void UpdateOffset(ref Vector3 targetPosition)
        {
            transform.position = targetPosition + m_Offset;
        }

        private void LateUpdate()
        {
            if (ObjectToFollow != null)
            {
                var directionTowards = ObjectToFollow.transform.position - transform.position;
                directionTowards.Normalize();
                var targetLook = Quaternion.LookRotation(directionTowards, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetLook, Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, ObjectToFollow.transform.position + m_Offset, CameraSmoothing * Time.deltaTime);
            }
        }
    }
}
