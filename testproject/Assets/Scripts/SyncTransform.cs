using MLAPI.Logging;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace MLAPI
{
    /// <summary>
    /// A component for syncing variables
    /// Initial goal: allow an FPS-style snapshot
    /// with variables updating at specific place in the frame
    /// </summary>
    [AddComponentMenu("MLAPI/SyncTransform")]
    // todo: check inheriting from NetworkedBehaviour. Currently needed for IsLocalPlayer, to synchronize position
    public class SyncTransform : NetworkedBehaviour
    {
        NetworkedVar<Vector3> m_varPos = new NetworkedVar<Vector3>();
        NetworkedVarQuaternion m_varRot = new NetworkedVarQuaternion();
        const float k_epsilon = 0.001f;

        private bool interpolate = true;

        // data structures for interpolation
        Vector3[] m_PosStore = new Vector3[2];
        Quaternion[] m_RotStore = new Quaternion[2];
        float[] m_PosTimes = new float[2];
        float[] m_RotTimes = new float[2];
        float m_lastSent = 0.0f;

        SyncTransform()
        {
            m_PosTimes[0] = -1.0f;
            m_PosTimes[1] = -1.0f;
            m_RotTimes[0] = -1.0f;
            m_RotTimes[1] = -1.0f;

            m_varPos.OnValueChanged = SyncPosChanged;
            m_varRot.OnValueChanged = SyncRotChanged;
        }

        void SyncPosChanged(Vector3 before, Vector3 after)
        {
            if (!IsLocalPlayer)
            {
                m_PosTimes[0] = m_PosTimes[1];
                m_PosTimes[1] = Time.time;
                m_PosStore[0] = m_PosStore[1];
                m_PosStore[1] = after;

                if (!interpolate)
                {
                    gameObject.transform.position = after;
                }
                //Debug.Log("[1] received position from " + before + " to " + after);
            }
        }

        void SyncRotChanged(Quaternion before, Quaternion after)
        {
            // todo: this is problematic. Why couldn't this filtering be done server-side?
            if (!IsLocalPlayer)
            {
                m_RotTimes[0] = m_RotTimes[1];
                m_RotTimes[1] = Time.time;
                m_RotStore[0] = m_RotStore[1];
                m_RotStore[1] = after;

                if (!interpolate)
                {
                    gameObject.transform.rotation = after;
                }
                //Debug.Log("[2] received rotation from " + before + " to " + after);
            }
        }

        void Start()
        {
            m_varPos.Settings.WritePermission = NetworkedVarPermission.Everyone;
            m_varRot.Settings.WritePermission = NetworkedVarPermission.Everyone;
        }

        void FixedUpdate()
        {
            float now = Time.time;
            if (m_lastSent == 0.0f)
            {
                m_lastSent = now;
            }

            // if this.gameObject is local let's send its position
            if (IsLocalPlayer)
            {
                m_varPos.Value = gameObject.transform.position;
                m_varRot.Value = gameObject.transform.rotation;
            }
            else
            {
                if (!interpolate)
                {
                    return;
                }

                // let's interpolate the last received transform
                if (m_PosTimes[0] >= 0.0 && m_PosTimes[1] >= 0.0)
                {
                    var before = gameObject.transform.position;

                    if (m_PosTimes[1] - m_PosTimes[0] > k_epsilon)
                    {
                        if ((now - m_PosTimes[0]) / (m_PosTimes[1] - m_PosTimes[0]) < 2.0)
                        {
                            gameObject.transform.position = Vector3.LerpUnclamped(
                                m_PosStore[0],
                                m_PosStore[1],
                                (now - m_PosTimes[0]) / (m_PosTimes[1] - m_PosTimes[0]));
                        }
                    }
                    else
                    {
                        gameObject.transform.position = m_PosStore[1];
                    }

                    var after = gameObject.transform.position;

                    //Debug.Log("[3] Updated position from " + before + " to " + after);
                }

                if (m_RotTimes[0] >= 0.0 && m_RotTimes[1] >= 0.0)
                {
                    var before = gameObject.transform.rotation;

                    if (m_RotTimes[1] - m_RotTimes[0] > k_epsilon)
                    {
                        if ((now - m_RotTimes[0]) / (m_RotTimes[1] - m_RotTimes[0]) < 2.0)
                        {
                            gameObject.transform.rotation = Quaternion.SlerpUnclamped(
                                m_RotStore[0],
                                m_RotStore[1],
                                (now - m_RotTimes[0]) / (m_RotTimes[1] - m_RotTimes[0]));
                        }
                    }
                    else
                    {
                        gameObject.transform.rotation = m_RotStore[1];
                    }

                    var after = gameObject.transform.rotation;

                    //Debug.Log("[4] Updated rotation from " + before + " to " + after);
                }
            }
        }
    }
}
