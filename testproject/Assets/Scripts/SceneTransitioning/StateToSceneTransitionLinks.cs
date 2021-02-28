using System;
using System.Linq;
using System.Collections.Generic;


namespace MLAPIGlobalGameState
{
    using UnityEngine;
    using UnityEditor;

    [Serializable]
    public class StateToSceneTransitionLinks
    {
        public enum MLAPIStates
        {
            None,
            Connecting,
            InSession,
            ExitSession
        }

        [SerializeField]
        private MLAPIStates m_MLAPIState;

        [SerializeField]
        String m_SceneToLoadName;           //Note: This is set within the SceneToStateOptionsEditor.cs file
#if (UNITY_EDITOR)
        [SerializeField]
        private SceneAsset m_SceneToLoad;
#endif

        [SerializeField]
        private GlobalGameState.GameStates m_StateToLoadScene;

        public String sceneToLoad
        {
            get
            {
#if (UNITY_EDITOR)
                return m_SceneToLoad.name; //Note: when in the editor we can use the SceneAsset
#else
                return m_SceneToLoadName;  //Note: when in a build, we have to use the name string
#endif
            }
        }
        public GlobalGameState.GameStates stateToLoadScene { get { return m_StateToLoadScene; } }

        public  MLAPIStates MLAPIState {get { return m_MLAPIState; }}

    }

    [Serializable]
    public class StateToSceneTransitionList
    {
        [SerializeField]
        private List<StateToSceneTransitionLinks> m_StateToSceneList;

        /// <summary>
        /// The list of options for the dropdown list.
        /// </summary>
        public List<StateToSceneTransitionLinks> StateToSceneList { get { return m_StateToSceneList; } set { m_StateToSceneList = value; } }

        /// <summary>
        /// GetSceneLinkedToState
        /// Returns the scene associated with the state
        /// </summary>
        /// <param name="gameState">GlobalGameState.GameStates</param>
        /// <returns>String</returns>
        public String GetSceneNameLinkedToState(GlobalGameState.GameStates gameState)
        {

            var results = m_StateToSceneList.Where(entry => entry.stateToLoadScene == gameState);
            if (results != null)
            {
                return results.First().sceneToLoad;
            }
            return String.Empty;
        }

        /// <summary>
        /// GetGameStateLinkedToScene
        /// Returns the state associated with the scene
        /// </summary>
        /// <param name="sceneName">Name of the scene</param>
        /// <returns>GlobalGameState.GameStates</returns>
        public GlobalGameState.GameStates GetGameStateLinkedToScene(String sceneName)
        {
            var results = m_StateToSceneList.Where(entry => entry.sceneToLoad == sceneName);
            if (results != null)
            {
                return results.First().stateToLoadScene;
            }
            return GlobalGameState.GameStates.None;
        }

        /// <summary>
        /// GetGameStateLinkedToScene
        /// Returns the MLAPI State associated with the scene
        /// </summary>
        /// <param name="gameState">GlobalGameState.GameStates</param>
        /// <returns>StateToSceneTransitionLinks.MLAPIStates</returns>
        public StateToSceneTransitionLinks.MLAPIStates GetGameStateToMLAPIState(GlobalGameState.GameStates gameState)
        {
            var results = m_StateToSceneList.Where(entry =>  entry.stateToLoadScene == gameState);
            if (results != null)
            {
                return results.First().MLAPIState;
            }
            return StateToSceneTransitionLinks.MLAPIStates.None;
        }

        public StateToSceneTransitionList()
        {
            m_StateToSceneList = new List<StateToSceneTransitionLinks>();
        }
    }
}
