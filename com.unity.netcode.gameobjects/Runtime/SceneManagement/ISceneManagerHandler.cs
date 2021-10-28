using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    internal interface ISceneManagerHandler
    {
        internal delegate void LoadCompletedCallbackDelegateHandler(uint sceneEventId, string sceneName);

        AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, uint sceneEventId, LoadCompletedCallbackDelegateHandler loadCallback);

        internal delegate void UnloadCompletedCallbackDelegateHandler(uint sceneEventId);
        AsyncOperation UnloadSceneAsync(Scene scene, uint sceneEventId, UnloadCompletedCallbackDelegateHandler unloadCallback);
    }
}
