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
        GUI.Label(rect, "State to Scene Transition Links");
    }

    private void DrawOptionData(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty itemData = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty itemScene = itemData.FindPropertyRelative("m_SceneToLoad");
        SerializedProperty itemState = itemData.FindPropertyRelative("m_StateToLoadScene");

        rect.height = EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(rect, itemScene, GUIContent.none);
        rect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(rect, itemState, GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Init(property);

        return m_ReorderableList.GetHeight();
    }
}
