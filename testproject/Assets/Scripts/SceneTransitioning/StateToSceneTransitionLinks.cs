using System;
using System.Linq;
using System.Collections.Generic;


namespace MLAPIGlobalGameState
{
    using UnityEngine;

    [Serializable]
    public class StateToSceneTransitionLinks
    {
        [SerializeField]
        private bool m_IsEnabled;

        [SerializeField]
        private Object m_SceneToLoad;

        [SerializeField]
        private GlobalGameState.GameStates m_StateToLoadScene;


        public Object sceneToLoad { get { return m_SceneToLoad; } }
        public GlobalGameState.GameStates stateToLoadScene { get { return m_StateToLoadScene; } }

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
                return results.First().sceneToLoad.name;
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
            var results = m_StateToSceneList.Where(entry => entry.sceneToLoad.name == sceneName);
            if(results != null)
            {
                return results.First().stateToLoadScene;
            }
            return GlobalGameState.GameStates.None;
        }

        public StateToSceneTransitionList()
        {
            m_StateToSceneList = new List<StateToSceneTransitionLinks>();
        }
    }
}
