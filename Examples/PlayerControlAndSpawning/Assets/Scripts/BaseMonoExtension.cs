using Unity.Netcode;
using UnityEngine;

public class BaseMonoExtension : MonoBehaviour, IExtensionHandler
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

    protected ExtendedNetworkManager m_ExtendedNetworkManager;
    protected ConnectionStates m_ConnectionState;

    protected NetworkSceneManager SceneManager => m_ExtendedNetworkManager.SceneManager;

    private void Awake()
    {
        ExtendedNetworkManager.AttachExtension(this);
    }

    private void OnDestroy()
    {
        ExtendedNetworkManager.DetachExtension(this);
    }

    protected virtual void OnInitialize()
    {

    }

    public bool HasInitialized()
    {
        return m_ExtendedNetworkManager != null;
    }

    public void Destroying()
    {
        ExtendedNetworkManager.DetachExtension(this);
    }

    public void Initialize(ExtendedNetworkManager extendedNetworkManager)
    {
        m_ExtendedNetworkManager = extendedNetworkManager;
        OnInitialize();
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
        return OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }

    protected Rect DrawLabel(Rect currentRect, string msg)
    {
        GUILayout.Label($"{msg}");
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        return currentRect;
    }

    protected (Rect, string) DrawTextField(Rect currentRect, string value)
    {
        value = GUILayout.TextField(value);
        var rect = GUILayoutUtility.GetLastRect();
        currentRect.height += rect.height;
        return (currentRect, value);
    }

    protected (Rect, bool) DrawButton(Rect currentTotalRect, string text, float width = 200)
    {
        var clicked = false;
        if (GUILayout.Button($"{text}", GUILayout.Width(width)))
        {
            var rect = GUILayoutUtility.GetLastRect();
            currentTotalRect.height += rect.height;
            clicked = true;
        }
        return (currentTotalRect, clicked);
    }
}
