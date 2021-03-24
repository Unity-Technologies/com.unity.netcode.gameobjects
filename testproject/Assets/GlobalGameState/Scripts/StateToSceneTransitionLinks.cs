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

        public String SceneToLoad
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
        public GlobalGameState.GameStates StateToLoadScene { get { return m_StateToLoadScene; } }

        public MLAPIStates MLAPIState { get { return m_MLAPIState; } }

    }

    [Serializable]
    public class StateToSceneTransitionList
    {
        [SerializeField]
        private List<StateToSceneTransitionLinks> m_StateToSceneList;

        /// <summary>
        /// The list of options for the drop-down list.
        /// </summary>
        public List<StateToSceneTransitionLinks> StateToSceneList { get { return m_StateToSceneList; } set { m_StateToSceneList = value; } }

        /// <summary>
        /// Returns the associated index of the InSession linked scene name
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public int GetStateToSceneNameIndex(StateToSceneTransitionLinks.MLAPIStates mlapiState, String sceneName)
        {
            //Get all of the scenes marked with MLAPIStates.InSession
            var results = m_StateToSceneList.Where(entry => entry.MLAPIState == mlapiState);
            if (results != null)
            {
                var IndexCounter = 0;
                //We need to parse these each time as the user might re-order the links
                foreach (StateToSceneTransitionLinks entry in results)
                {
                    if (entry.SceneToLoad == sceneName)
                    {
                        return IndexCounter;
                    }
                    IndexCounter++;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the scene associated with the state
        /// </summary>
        /// <param name="gameState">game state</param>
        /// <param name="currentindex">the expected index</param>
        /// <returns></returns>
        public String GetSceneNameLinkedToState(GlobalGameState.GameStates gameState, int currentindex = 0)
        {
            var results = m_StateToSceneList.Where(entry => entry.StateToLoadScene == gameState);
            if (results != null && ((results.Count() - 1) >= currentindex))
            {
                return results.ElementAt(currentindex).SceneToLoad;
            }
            return String.Empty;
        }

        /// <summary>
        /// Returns the state associated with the scene
        /// </summary>
        /// <param name="sceneName">Name of the scene</param>
        /// <returns>GlobalGameState.GameStates</returns>
        public GlobalGameState.GameStates GetGameStateLinkedToScene(String sceneName)
        {
            var results = m_StateToSceneList.Where(entry => entry.SceneToLoad == sceneName);
            if (results != null)
            {
                return results.First().StateToLoadScene;
            }
            return GlobalGameState.GameStates.None;
        }

        /// <summary>
        /// Returns the MLAPI State associated with the scene
        /// </summary>
        /// <param name="gameState">GlobalGameState.GameStates</param>
        /// <returns>StateToSceneTransitionLinks.MLAPIStates</returns>
        public StateToSceneTransitionLinks.MLAPIStates GetGameStateToMLAPIState(GlobalGameState.GameStates gameState)
        {
            var results = m_StateToSceneList.Where(entry =>  entry.StateToLoadScene == gameState);
            if (results != null)
            {
                return results.First().MLAPIState;
            }
            return StateToSceneTransitionLinks.MLAPIStates.None;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public StateToSceneTransitionList()
        {
            m_StateToSceneList = new List<StateToSceneTransitionLinks>();
        }
    }
}
