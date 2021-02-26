using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MLAPIGlobalGameState;

[CustomPropertyDrawer(typeof(StateToSceneTransitionList), true)]
class SceneToStateOptionsEditor : PropertyDrawer
{
    private ReorderableList m_ReorderableList;

    private void Init(SerializedProperty property)
    {
        if (m_ReorderableList != null)
            return;

        SerializedProperty array = property.FindPropertyRelative("m_StateToSceneList");

        m_ReorderableList = new ReorderableList(property.serializedObject, array);
        m_ReorderableList.drawElementCallback = DrawOptionData;
        m_ReorderableList.drawHeaderCallback = DrawHeader;
        m_ReorderableList.elementHeight += 16;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init(property);

        m_ReorderableList.DoList(position);
    }

    private void DrawHeader(Rect rect)
    {
        GUI.Label(rect, "Enabled |         MLAPI State              |    State to Scene Transition Links");
    }

    private void DrawOptionData(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty itemData = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);


        SerializedProperty itemEnabled = itemData.FindPropertyRelative("m_IsEnabled");
        SerializedProperty itemMLAPIState = itemData.FindPropertyRelative("m_MLAPIState");
        SerializedProperty itemScene = itemData.FindPropertyRelative("m_SceneToLoad");
        SerializedProperty itemState = itemData.FindPropertyRelative("m_StateToLoadScene");
        SerializedProperty itemSceneName = itemData.FindPropertyRelative("m_SceneToLoadName");

        //[NSS]: This is how we get the scene name from the scene object for runtime usage (scene objects don't get exported to builds)
        if(itemScene.objectReferenceValue != null)
        {
            itemSceneName.stringValue = itemScene.objectReferenceValue.name;
            Debug.Log("Set the scene name to " + itemSceneName.stringValue);

        }

        float OriginalWidth = rect.width;
        float OrininalHeight = rect.height;
        rect.height = 18;
        rect.width = 32;
        EditorGUI.PropertyField(rect, itemEnabled, GUIContent.none);
        rect.x += rect.width + 8;
        rect.width = (OriginalWidth * 0.33f) - 32;
        EditorGUI.PropertyField(rect, itemMLAPIState, GUIContent.none);
        rect.x += rect.width + 16;
        EditorGUI.PropertyField(rect, itemScene,  GUIContent.none);
        rect.x += rect.width + 8;
        EditorGUI.PropertyField(rect, itemState,  GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Init(property);

        return m_ReorderableList.GetHeight();
    }
}
