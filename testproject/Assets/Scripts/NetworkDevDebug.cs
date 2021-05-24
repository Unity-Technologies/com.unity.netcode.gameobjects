using MLAPI.Prototyping;
using UnityEngine;

namespace MLAPI
{
    public class NetworkDevDebug : MonoBehaviour
    {
        public GameObject SourceToMove;
        // public GameObject TargetToParent;

        private NetworkTransform m_NetworkTransform;
        private Vector3 m_CachedPosition = Vector3.zero;

        private void Awake()
        {
            m_NetworkTransform = GetComponent<NetworkTransform>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (SourceToMove.transform.parent == transform)
                {
                    SourceToMove.transform.parent = null;
                    SourceToMove.transform.position = m_CachedPosition;
                }
                else
                {
                    m_CachedPosition = SourceToMove.transform.position;
                    SourceToMove.transform.parent = transform;
                    SourceToMove.transform.localPosition = Vector3.zero;
                }
            }

            if (m_NetworkTransform != null && m_NetworkTransform.CanUpdateTransform())
            {
                transform.Translate(0, 0, Time.deltaTime * 4); // move forward
                transform.Rotate(0, Time.deltaTime * 48, 0); // turn a little
            }
        }
    }
}
