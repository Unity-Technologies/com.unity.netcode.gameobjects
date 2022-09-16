using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    public class MultiSceneManagerHandler : ISceneManagerHandler
    {
        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            // Always load additively
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            sceneEventAction.Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
            return operation;
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
            return operation;
        }
    }
}
