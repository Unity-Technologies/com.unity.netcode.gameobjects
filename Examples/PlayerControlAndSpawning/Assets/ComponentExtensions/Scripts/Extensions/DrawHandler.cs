using UnityEngine;

public class DrawHandler
{
    public bool AlignRight;

    public Rect Label(Rect currentRect, string msg, float width = 400.0f)
    {
        if (AlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            width = 200.0f;
        }

        GUILayout.Label($"{msg}", GUILayout.Width(width));
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        if (AlignRight)
        {
            GUILayout.EndHorizontal();
        }

        return currentRect;
    }

    public (Rect, bool) Toggle(Rect currentRect, bool toggleState, string label)
    {
        if (AlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        toggleState = GUILayout.Toggle(toggleState, label);
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;

        if (AlignRight)
        {
            GUILayout.EndHorizontal();
        }

        return (currentRect, toggleState);
    }

    public (Rect, string) TextField(Rect currentRect, string value, float width = 200)
    {
        if (AlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        value = GUILayout.TextField(value, GUILayout.Width(width));
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;

        if (AlignRight)
        {
            GUILayout.EndHorizontal();
        }

        return (currentRect, value);
    }

    public (Rect, bool) Button(Rect currentTotalRect, string text, float width = 200)
    {
        if (AlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        var clicked = false;
        if (GUILayout.Button($"{text}", GUILayout.Width(width)))
        {
            var rect = GUILayoutUtility.GetLastRect();
            currentTotalRect.height += rect.height;
            clicked = true;
        }

        if (AlignRight)
        {
            GUILayout.EndHorizontal();
        }
        return (currentTotalRect, clicked);
    }
}
