using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// Used to override the LoadSceneAsync and UnloadSceneAsync methods called
    /// within the NetworkSceneManager.
    /// </summary>
    internal interface ISceneManagerHandler
    {
        // Generic action to call when a scene is finished loading/unloading
        struct SceneEventAction
        {
            internal uint SceneEventId;
            internal Action<uint> EventAction;
            internal void Invoke()
            {
                EventAction.Invoke(SceneEventId);
            }
        }

        AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventAction sceneEventAction);

        AsyncOperation UnloadSceneAsync(Scene scene, SceneEventAction sceneEventAction);
    }
}
