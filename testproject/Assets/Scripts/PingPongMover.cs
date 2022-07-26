using UnityEngine;

public class PingPongMover : MonoBehaviour
{
    public Vector3 Direction;
    public float Time;

    private Vector3 m_StartPosition;

    // Start is called before the first frame update
    private void Start()
    {
        m_StartPosition = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        var t = Mathf.PingPong(UnityEngine.Time.time, Time);
        var offset = Vector3.Lerp(Vector3.zero, Direction, t);
        transform.position = m_StartPosition + offset;
    }
}
