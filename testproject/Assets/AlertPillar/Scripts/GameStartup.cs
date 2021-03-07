using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlertPillar
{
    public class GameStartup : MonoBehaviour
    {
        private PhysicsScene m_ServerScene;
        private static bool s_RunAlready = false;

        private GameObject NetField;

        [SerializeField]
        private MLAPI.NetworkManager ClientNetManager;

        private MLAPI.NetworkManager ServerNetManager;



        // Start is called before the first frame update
        void Start()
        {
            if (s_RunAlready)
            {
                enabled = false;
                return;
            }

            ClientNetManager.OnClientConnectedCallback += (ulong clientId) =>
            {
                Debug.Log($"Client {clientId} connected!");
            };
            ClientNetManager.OnClientDisconnectCallback += (ulong clientId) =>
            {
                Debug.Log($"Client {clientId} disconnected!");
            };

            LoadSceneParameters loadParams = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
            Scene scene = SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, loadParams);
            SceneManager.sceneLoaded += (Scene loaded_scene, LoadSceneMode mode) =>
            {
                if (loaded_scene == scene)
                {
                    ConfigServeritems(loaded_scene);
                    m_ServerScene = scene.GetPhysicsScene();

                    ServerNetManager.OnServerStarted += () =>
                    {
                        Debug.Log("Server has started!");
                        ClientNetManager.StartClient();
                    };

                    ServerNetManager.OnClientConnectedCallback += (ulong clientId) =>
                    {
                        Debug.Log($"Server says Client {clientId} connected!");
                    };

                    MonitorPillarLogic[] pillars = GameObject.FindObjectsOfType<MonitorPillarLogic>();
                    Debug.Log("Number of pillars found by searching for pillar scripts: " + pillars.Length);

                    ServerNetManager.StartServer();
                }
            };

            s_RunAlready = true; //run once! Otherwise we can "recurse" with each child scene creating another child scene. 
        }


        private void ConfigServeritems(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Main Camera")
                {
                    GameObject.Destroy(go);
                }

                if (go.name == "NetworkingManager")
                {
                    go.name = "NetworkingManager (Server)";
                    ServerNetManager = go.GetComponent<MLAPI.NetworkManager>();
                }
            }
        }
    }
}

