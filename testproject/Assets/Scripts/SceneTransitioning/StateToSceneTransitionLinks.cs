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
        [SerializeField]
        private bool m_IsEnabled;

        public enum MLAPIStates
        {
            None,
            Connecting,
            InSession,
            ExitSession
        }

        [SerializeField]
        private MLAPIStates m_MLAPIState;


        private String  SceneToLoadName;

        [SerializeField]
        String m_SceneToLoadName;
#if (UNITY_EDITOR)
        [SerializeField]
        private SceneAsset m_SceneToLoad;
#endif

        [SerializeField]
        private GlobalGameState.GameStates m_StateToLoadScene;

        public bool IsEnabled { get { return m_IsEnabled; } }

        public String sceneToLoad
        {
            get
            {
#if (UNITY_EDITOR)
                return m_SceneToLoad.name;
#else
                return m_SceneToLoadName;
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
        /// <param name="gameState"></param>
        /// <returns></returns>
        public String GetSceneNameLinkedToState(GlobalGameState.GameStates gameState)
        {

            var results = m_StateToSceneList.Where(entry => entry.stateToLoadScene == gameState);
            if(results != null)
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
            if(results != null)
            {
                return results.First().stateToLoadScene;
            }
            return GlobalGameState.GameStates.None;
        }


        /// <summary>
        /// GetGameStateLinkedToScene
        /// Returns the state associated with the scene
        /// </summary>
        /// <param name="sceneName">Name of the scene</param>
        /// <returns>GlobalGameState.GameStates</returns>
        public StateToSceneTransitionLinks.MLAPIStates GetGameStateToMLAPIState(GlobalGameState.GameStates gameState)
        {
            var results = m_StateToSceneList.Where(entry =>  entry.stateToLoadScene == gameState);
            if(results != null)
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
