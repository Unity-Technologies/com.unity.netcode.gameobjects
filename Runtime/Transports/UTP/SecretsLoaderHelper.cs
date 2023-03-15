using System;
using System.IO;
using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// Component to add to a NetworkManager if you want the certificates to be loaded from files.
    /// Mostly helpful to ease development and testing, especially with self-signed certificates
    ///
    /// Shipping code should make the calls to
    /// - SetServerSecrets
    /// - SetClientSecrets
    /// directly, instead of relying on this.
    /// </summary>
    public class SecretsLoaderHelper : MonoBehaviour
    {
        internal struct ServerSecrets
        {
            public string ServerPrivate;
            public string ServerCertificate;
        };

        internal struct ClientSecrets
        {
            public string ServerCommonName;
            public string ClientCertificate;
        };

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

            var unityTransportComponent = GetComponent<UnityTransport>();

            if (unityTransportComponent == null)
            {
                Debug.LogError($"You need to select the UnityTransport protocol, in the NetworkManager, in order for the SecretsLoaderHelper component to be useful.");
                return;
            }

            unityTransportComponent.SetServerSecrets(serverSecrets.ServerCertificate, serverSecrets.ServerPrivate);
            unityTransportComponent.SetClientSecrets(clientSecrets.ServerCommonName, clientSecrets.ClientCertificate);
        }

        [Tooltip("Hostname")]
        [SerializeField]
        private string m_ServerCommonName = "localhost";

        /// <summary>Common name of the server (typically its hostname).</summary>
        public string ServerCommonName
        {
            get => m_ServerCommonName;
            set => m_ServerCommonName = value;
        }

        [Tooltip("Client CA filepath. Useful with self-signed certificates")]
        [SerializeField]
        private string m_ClientCAFilePath = ""; // "Assets/Secure/myGameClientCA.pem"

        /// <summary>Client CA filepath. Useful with self-signed certificates</summary>
        public string ClientCAFilePath
        {
            get => m_ClientCAFilePath;
            set => m_ClientCAFilePath = value;
        }

        [Tooltip("Client CA Override. Only useful for development with self-signed certificates. Certificate content, for platforms that lack file access (WebGL)")]
        [SerializeField]
        private string m_ClientCAOverride = "";

        /// <summary>
        /// Client CA Override. Only useful for development with self-signed certificates.
        /// Certificate content, for platforms that lack file access (WebGL)
        /// </summary>
        public string ClientCAOverride
        {
            get => m_ClientCAOverride;
            set => m_ClientCAOverride = value;
        }

        [Tooltip("Server Certificate filepath")]
        [SerializeField]
        private string m_ServerCertificateFilePath = ""; // "Assets/Secure/myGameServerCertificate.pem"

        /// <summary>Server Certificate filepath</summary>
        public string ServerCertificateFilePath
        {
            get => m_ServerCertificateFilePath;
            set => m_ServerCertificateFilePath = value;
        }

        [Tooltip("Server Private Key filepath")]
        [SerializeField]
        private string m_ServerPrivateFilePath = ""; // "Assets/Secure/myGameServerPrivate.pem"

        /// <summary>Server Private Key filepath</summary>
        public string ServerPrivateFilePath
        {
            get => m_ServerPrivateFilePath;
            set => m_ServerPrivate = value;
        }

        private string m_ClientCA;

        /// <summary>CA certificate used by the client.</summary>
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

        /// <summary>Certificate used by the server.</summary>
        public string ServerCertificate
        {
            get => ReadFile(m_ServerCertificateFilePath, "Server Certificate");
            set => m_ServerCertificate = value;
        }

        private string m_ServerPrivate;

        /// <summary>Private key used by the server.</summary>
        public string ServerPrivate
        {
            get => ReadFile(m_ServerPrivateFilePath, "Server Key");
            set => m_ServerPrivate = value;
        }

        private static string ReadFile(string path, string label)
        {
            if (path == null || path == "")
            {
                return "";
            }

            var reader = new StreamReader(path);
            string fileContent = reader.ReadToEnd();
            Debug.Log((fileContent.Length > 1) ? ("Successfully loaded " + fileContent.Length + " byte(s) from " + label) : ("Could not read " + label + " file"));
            return fileContent;
        }
    }
}
