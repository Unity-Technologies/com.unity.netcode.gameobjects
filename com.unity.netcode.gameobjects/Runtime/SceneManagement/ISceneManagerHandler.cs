using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    internal interface ISceneManagerHandler
    {
        internal struct SceneEventAction
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
