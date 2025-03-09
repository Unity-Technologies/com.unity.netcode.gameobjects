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
    public DrawHandler Draw;

    protected ExtendedNetworkManager m_ExtendedNetworkManager;
    protected ConnectionStates m_ConnectionState;

    protected NetworkSceneManager SceneManager => m_ExtendedNetworkManager.SceneManager;

    protected bool m_ApplicationExitPending;

    private void Awake()
    {
        Draw = new DrawHandler();
        ExtendedNetworkManager.AttachExtension(this);
    }

    public override void OnDestroy()
    {
        ExtendedNetworkManager.DetachExtension(this);
        base.OnDestroy();
    }

    protected virtual void OnApplicationExitPending()
    {
    }

    public void ApplicationExitPending()
    {
        m_ApplicationExitPending = true;
        OnApplicationExitPending();
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

    protected virtual bool OnIsAuthorityInstance()
    {
        return HasAuthority;
    }

    public bool IsAuthorityInstance()
    {
        return OnIsAuthorityInstance();
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
        if (m_ApplicationExitPending)
        {
            return totalRectSize;
        }
        Draw.AlignRight = screenSpaceRegion == ScreenSpaceRegions.TopRight;
        return OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
