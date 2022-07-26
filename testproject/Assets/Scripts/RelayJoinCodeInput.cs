using UnityEngine;
using UnityEngine.UI;

public class RelayJoinCodeInput : MonoBehaviour
{
    public ConnectionModeScript ConnectionScript;
    private InputField m_TextInput;

    private void Start()
    {
        m_TextInput = GetComponent<InputField>();
    }

    private void Update()
    {
        if (m_TextInput.IsInteractable())
        {
            if (!string.IsNullOrEmpty(ConnectionScript.RelayJoinCode))
            {
                m_TextInput.text = ConnectionScript.RelayJoinCode;
                m_TextInput.readOnly = true;
            }
        }
    }

    public void SetJoinCode()
    {
        ConnectionScript.RelayJoinCode = m_TextInput.text;
    }
}
