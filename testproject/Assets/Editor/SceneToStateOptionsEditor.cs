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
        m_ReorderableList.elementHeight += 8;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init(property);

        m_ReorderableList.DoList(position);
    }


    /// <summary>
    /// DrawHeaderSection
    /// This draws one header "column" title
    /// </summary>
    /// <param name="header">header-column name</param>
    /// <param name="gUIStyle">the style of the header </param>
    /// <param name="rect">current rectangle space to draw</param>
    /// <param name="originalWidth">original total width of property control</param>
    /// <param name="advanceEnding">set for the last header-column of the sequence</param>
    /// <returns></returns>
    Rect DrawHeaderSection(string header,GUIStyle gUIStyle, Rect rect,float originalWidth, bool advanceEnding, bool isFirst = true)
    {
        Rect localRect = rect;
        //For the opening bar we use a 22 pixel width
        localRect.width = gUIStyle.fontSize*0.65f;
        GUI.Label(localRect, "|", gUIStyle);

        //Advance our position with an space
        localRect.x += localRect.width;

        float estSize = (header.Length * (gUIStyle.fontSize))*0.35f;
        float propertySize = ((originalWidth * 0.28f) -localRect.width);
        if (!isFirst)
        {
            propertySize = originalWidth <= estSize ? estSize : (propertySize < LastColumnSize ? LastColumnSize : propertySize);
        }
        else
        {
            propertySize = originalWidth <= estSize ? estSize : propertySize;
        }

        localRect.width = propertySize;
        GUI.Label(localRect, header, gUIStyle);

        //For 2 of the 3 titles, we want to advance.  The last one we do not.
        if(advanceEnding)
        {
            //Advance our position with an 8 pixel space
            localRect.x += localRect.width;//(originalWidth * 0.01f);
        }
        return localRect;
    }

    /// <summary>
    /// DrawHeader
    /// Draws the header of this property box/region
    /// </summary>
    /// <param name="rect"></param>
    private void DrawHeader(Rect rect)
    {
        float OriginalWidth = rect.width;
        rect.width = (OriginalWidth * 0.24f);

        GUIStyle gUIStyle = new GUIStyle( GUI.skin.label);
        gUIStyle.alignment = TextAnchor.MiddleCenter;


        if(OriginalWidth > 0)
        {
            rect = DrawHeaderSection("MLAPI State", gUIStyle, rect, OriginalWidth, true);
            rect = DrawHeaderSection("Scene to link", gUIStyle, rect, OriginalWidth, true, false);
            rect = DrawHeaderSection("to game state", gUIStyle, rect, OriginalWidth, false, false);
        }
    }

    float LastColumnSize;
    /// <summary>
    /// DrawOptionData
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="index"></param>
    /// <param name="isActive"></param>
    /// <param name="isFocused"></param>
    private void DrawOptionData(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty itemData = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);

        SerializedProperty itemMLAPIState = itemData.FindPropertyRelative("m_MLAPIState");
        SerializedProperty itemScene = itemData.FindPropertyRelative("m_SceneToLoad");
        SerializedProperty itemState = itemData.FindPropertyRelative("m_StateToLoadScene");
        SerializedProperty itemSceneName = itemData.FindPropertyRelative("m_SceneToLoadName");

        //[NSS]: This is how we get the scene name from the scene object for runtime usage (scene objects don't get exported to builds)
        if(itemScene.objectReferenceValue != null)
        {
            itemSceneName.stringValue = itemScene.objectReferenceValue.name;
        }

        float OriginalWidth = rect.width;
        if(OriginalWidth > 0)
        {
            rect.height = 18;
            rect.width = (OriginalWidth * 0.275f);
            EditorGUI.PropertyField(rect, itemMLAPIState, GUIContent.none);
            rect.x += rect.width + 2;
            LastColumnSize = (OriginalWidth * 0.30f);
            rect.width = LastColumnSize;
            EditorGUI.PropertyField(rect, itemScene,  GUIContent.none);
            rect.x += rect.width + 2;
            EditorGUI.PropertyField(rect, itemState,  GUIContent.none);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Init(property);

        return m_ReorderableList.GetHeight();
    }
}
