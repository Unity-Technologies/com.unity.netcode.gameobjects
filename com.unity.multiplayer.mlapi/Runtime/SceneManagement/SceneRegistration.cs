using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistration", menuName = "MLAPI/SceneManagement/SceneRegistration")]
    [Serializable]
    public class SceneRegistration : ScriptableObject
    {
        [SerializeField]
        private List<SceneRegistrationEntry> m_SceneRegistrations;

#if UNITY_EDITOR
        private void OnValidate()
        {
 
        }
#endif
    }
}
