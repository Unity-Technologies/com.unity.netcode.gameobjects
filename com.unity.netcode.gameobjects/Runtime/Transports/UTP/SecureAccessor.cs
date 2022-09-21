using System.IO;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    public class SecureAccessor : MonoBehaviour
    {
        private void Awake()
        {
            ServerSecrets serverSecrets = new ServerSecrets()
            {
                ServerCertificate = ServerCertificate,
                ServerPrivate = ServerPrivate
            };

            GetComponent<UnityTransport>().SetServerSecrets(serverSecrets);

            ClientSecrets clientSecrets = new ClientSecrets()
            {
                ClientCertificate = ClientCA,
                ServerCommonName = ServerCommonName
            };

            GetComponent<UnityTransport>().SetClientSecrets(clientSecrets);
        }

        [Tooltip("Hostname")]
        [SerializeField]
        private string m_ServerCommonNameString = "localhost";
        public string ServerCommonNameString
        {
            get => m_ServerCommonNameString;
            set => m_ServerCommonNameString = value;
        }
        private string m_ServerCommonName;
        public string ServerCommonName
        {
            get => m_ServerCommonNameString;
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

        private string m_ClientCA;
        public string ClientCA
        {
            get => ReadFile(m_ClientCAFilePath, "Client Certificate");
            set => m_ClientCA = value;
        }
        private string m_ServerCertificate;
        public string ServerCertificate
        {
            get => ReadFile(m_ServerCertificateFilePath, "Server Certificate");
            set => m_ServerCertificate = value;
        }
        private string m_ServerPrivate;
        public string ServerPrivate
        {
            get => ReadFile(m_ServerPrivateFilePath, "Server Key");
            set => m_ServerPrivate = value;
        }

        private static string ReadFile(string path, string label)
        {
            var reader = new StreamReader(path);
            string fileContent = reader.ReadToEnd();
            Debug.Log((fileContent.Length > 1) ? ("Successfully loaded " + fileContent.Length + " byte(s) from " + label) : ("Could not read " + label + " file"));
            return fileContent;
        }
    }
}
