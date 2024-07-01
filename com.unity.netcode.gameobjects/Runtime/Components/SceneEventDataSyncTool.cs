using System.Text;

namespace Unity.Netcode.Components
{
    public class SceneEventDataSyncTool : NetworkBehaviour
    {
        internal static SceneEventDataSyncTool Instance;

        protected StringBuilder m_LogBuilder = new StringBuilder();

        private void Awake()
        {
            Instance = this;
            SceneEventData.LogBuilder = m_LogBuilder;
        }

        protected virtual void OnSubmitSceneEventData(SceneEventDataSynchronize sceneEventDataSynchronize, RpcParams rpcParams)
        {

        }

        internal void SubmitSceneEventData(SceneEventDataSynchronize sceneEventDataSynchronize, RpcParams rpcParams)
        {
            OnSubmitSceneEventData(sceneEventDataSynchronize, rpcParams);
        }

        protected virtual void OnSubmitSceneEventLog(string sceneEventLog)
        {

        }


        internal void SubmitSceneEventLog(string sceneEventLog)
        {
            OnSubmitSceneEventLog(sceneEventLog);
        }

    }
}
