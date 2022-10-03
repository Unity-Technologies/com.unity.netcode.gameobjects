using System;
using System.IO;
using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    public class SecureAccessor : MonoBehaviour
    {
        private void Awake()
        {
            var serverSecrets = new ServerSecrets();

            try
            {
                serverSecrets.ServerCertificate = ServerCertificate;
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }

            try
            {
                serverSecrets.ServerPrivate = ServerPrivate;
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }

            var clientSecrets = new ClientSecrets();
            try
            {
                clientSecrets.ClientCertificate = ClientCA;
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }

            try
            {
                clientSecrets.ServerCommonName = ServerCommonName;
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }

            GetComponent<UnityTransport>().SetServerSecrets(serverSecrets);
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

        [Tooltip("Client CA Override. Certificate content, for platforms that lack file access (WebGL)")]
        [SerializeField]
        private string m_ClientCAOverride = "";
        public string ClientCAOverride
        {
            get => m_ClientCAOverride;
            set => m_ClientCAOverride = value;
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
            get
            {
                if (m_ClientCAOverride != "")
                {
                    return m_ClientCAOverride;
                }
                return ReadFile(m_ClientCAFilePath, "Client Certificate");
            }
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
