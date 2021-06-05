using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TextSetter : MonoBehaviour
{
    [SerializeField]
    private MyNetworkedScriptableObject m_Source;
    private Text m_T;
    // Start is called before the first frame update
    void Start()
    {
        m_T = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        m_T.text = m_Source.myText.Value;
    }
}
