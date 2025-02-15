using Unity.Netcode;
using UnityEngine;

public class BaseNetcodeExtension : NetworkBehaviour, IExtensionHandler
{
#if UNITY_EDITOR
    protected virtual void OnValidateComponent()
    {

    }

    private void OnValidate()
    {
        OnValidateComponent();
    }
#endif

    public uint SortOrder = 500;

    protected ExtendedNetworkManager m_ExtendedNetworkManager;
    protected ConnectionStates m_ConnectionState;

    protected NetworkSceneManager SceneManager => m_ExtendedNetworkManager.SceneManager;

    private bool m_IsAlignRight;

    private void Awake()
    {
        ExtendedNetworkManager.AttachExtension(this);
    }

    public override void OnDestroy()
    {
        ExtendedNetworkManager.DetachExtension(this);
        base.OnDestroy();
    }

    public uint GetSortOrder()
    {
        return SortOrder;
    }

    protected virtual void OnInitialize()
    {

    }

    public void Initialize(ExtendedNetworkManager extendedNetworkManager)
    {
        m_ExtendedNetworkManager = extendedNetworkManager;
        OnInitialize();
    }

    public bool HasInitialized()
    {
        return m_ExtendedNetworkManager != null;
    }

    public void Destroying()
    {
        ExtendedNetworkManager.DetachExtension(this);
    }

    protected virtual void OnStatusUpdate(ConnectionStates previousState, ConnectionStates currentState)
    {

    }

    public void StatusUpdate(ConnectionStates connectionState)
    {
        OnStatusUpdate(m_ConnectionState, connectionState);
        m_ConnectionState = connectionState;
    }

    protected virtual void OnAuthorityUpdate()
    {

    }

    public void AuthorityUpdate()
    {
        OnAuthorityUpdate();
    }

    protected virtual void OnNonAuthorityUpdate()
    {

    }

    public void NonAuthorityUpdate()
    {
        OnNonAuthorityUpdate();
    }

    protected virtual Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        return totalRectSize;
    }

    public Rect GUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        m_IsAlignRight = screenSpaceRegion == ScreenSpaceRegions.TopRight;
        return OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }

    protected Rect DrawLabel(Rect currentRect, string msg, float width = 400.0f)
    {
        if (m_IsAlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            width = 200.0f;
        }

        GUILayout.Label($"{msg}", GUILayout.Width(width));
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        if (m_IsAlignRight)
        {
            GUILayout.EndHorizontal();
        }

        return currentRect;
    }

    protected (Rect, string) DrawTextField(Rect currentRect, string value)
    {
        if (m_IsAlignRight)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        value = GUILayout.TextField(value);
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;

        if (m_IsAlignRight)
        {
            GUILayout.EndHorizontal();
        }

        return (currentRect, value);
    }

    protected (Rect, bool) DrawButton(Rect currentTotalRect, string text, float width = 200)
    {
        if (m_IsAlignRight)
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

        if (m_IsAlignRight)
        {
            GUILayout.EndHorizontal();
        }
        return (currentTotalRect, clicked);
    }
}
