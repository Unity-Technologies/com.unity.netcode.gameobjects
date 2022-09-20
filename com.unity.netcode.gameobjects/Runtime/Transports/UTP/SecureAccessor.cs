using System.IO;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    public class SecureAccessor : MonoBehaviour
    {
        [Tooltip("Hostname")]
        [SerializeField]
        public string  m_ServerCommonNameString = "localhost";
        public string ServerCommonNameString
        {
            get => m_ServerCommonNameString;
            set => m_ServerCommonNameString = value;
        }
        private FixedString512Bytes  m_ServerCommonName;
        public FixedString512Bytes ServerCommonName
        {
            get => (FixedString512Bytes) m_ServerCommonNameString;
        }
        [Tooltip("Client CA filepath")]
        [SerializeField]
        private string m_ClientCAFilePath = "Assets/Secure/myGameClientCA.pem";
        public string ClientCAFilePath
        {
            get => m_ClientCAFilePath;
            set => m_ClientCAFilePath = value;
        }

        [Tooltip("Server Certificate filepath")]
        [SerializeField]
        private string m_ServerCertificateFilePath = "Assets/Secure/myGameServerCertificate.pem";
        public string ServerCertificateFilePath
        {
            get => m_ServerCertificateFilePath;
            set => m_ServerCertificateFilePath = value;
        }
        [Tooltip("Server Private Keyfilepath")]
        [SerializeField]
        private string m_ServerPrivateFilePath = "Assets/Secure/myGameServerPrivate.pem";
        public string ServerPrivateFilePath
        {
            get => m_ServerPrivateFilePath;
            set => m_ServerPrivate = value;
        }

        private FixedString4096Bytes m_ClientCA;
        public FixedString4096Bytes ClientCA
        {
            get => (FixedString4096Bytes) ReadFile(m_ClientCAFilePath,"Client Certificate");
            set => m_ClientCA = value;
        }
        private FixedString4096Bytes m_ServerCertificate;
        public FixedString4096Bytes ServerCertificate
        {
            get => (FixedString4096Bytes) ReadFile(m_ServerCertificateFilePath,"Server Certificate");
            set => m_ServerCertificate = value;
        }
        private FixedString4096Bytes m_ServerPrivate;
        public FixedString4096Bytes ServerPrivate
        {
            get =>  (FixedString4096Bytes) ReadFile(m_ServerPrivateFilePath,"Server Key");
            set => m_ServerPrivate = value;
        }

        static string ReadFile(string path, string label)
        {
            StreamReader reader = new StreamReader(path);
            string fileContent = reader.ReadToEnd();
            Debug.Log((fileContent.Length > 1)?("Successfully loaded " + fileContent.Length + " byte(s) from " + label):("Could not read " + label + " file"));
            return fileContent;
        }
    }
}
