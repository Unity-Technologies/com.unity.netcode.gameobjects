using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using MLAPI.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MLAPI.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyPropertyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Disable the read only field
            GUI.enabled = false;
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height), property, label);
            GUI.enabled = true;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(SceneReadOnlyPropertyAttribute))]
    public class SceneRegistrationReadOnlyPropertyDrawer : PropertyDrawer
    {
        private const int k_ButtonWidth = 85;

        private const bool k_LoadScene = true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Disable the read only field
            GUI.enabled = false;
            try
            {
                EditorGUI.PropertyField(new Rect(position.x + k_ButtonWidth + 3, position.y, position.width - (k_ButtonWidth + 3), position.height), property, label);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }

            GUI.enabled = true;
            EditorGUI.EndProperty();

            var buttonTitle = k_LoadScene == true ? "Load Scene" : "Find Asset";
            // Add a button to open the scene containing the network manager referencing the SceneRegistration
            if (GUI.Button(new Rect(position.x, position.y, k_ButtonWidth, 20), buttonTitle))
            {
                var value = property.objectReferenceValue as SceneAsset;
                if (value != null)
                {
                    if (!k_LoadScene)
                    {
                        //Selection.SetActiveObjectWithContext(value, Selection.activeObject);
                        ProjectWindowUtil.ShowCreatedAsset(value);
                    }
                    else
                    {
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(value), OpenSceneMode.Single);
                    }
                }
            }
        }
    }




}
