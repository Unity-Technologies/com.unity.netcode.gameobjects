using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseMonoExtension : MonoBehaviour, IExtensionHandler
{
#if UNITY_EDITOR
    [HideInInspector]
    public SerializableDictionary<SerializableType, bool> IsExpandedTable = new SerializableDictionary<SerializableType, bool>(new SerializableTypeComparer());
    protected virtual void OnValidateComponent()
    {

    }

    private void OnValidate()
    {
        var typeList = new List<SerializableType>();
        var parent = new SerializableType(GetType());
        while (parent != null)
        {
            typeList.Add(parent);
            if (!IsExpandedTable.ContainsKey(parent))
            {
                IsExpandedTable.Add(parent, false);
            }
            parent = new SerializableType(parent.Type.BaseType);
            if (parent.Type == typeof(MonoBehaviour))
            {
                break;
            }
        }
        foreach (var type in IsExpandedTable.Keys)
        {
            if (typeList.Contains(type))
            {
                typeList.Remove(type);
            }
        }
        foreach (var type in typeList)
        {
            IsExpandedTable.Remove(type);
        }

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

    private void OnDestroy()
    {
        ExtendedNetworkManager.DetachExtension(this);
    }

    protected virtual void OnApplicationExitPending()
    {
    }

    public void ApplicationExitPending()
    {
        m_ApplicationExitPending = true;
        OnApplicationExitPending();
    }

    protected virtual void OnInitialize()
    {

    }

    public uint GetSortOrder()
    {
        return SortOrder;
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

    protected virtual bool OnIsAuthorityInstance()
    {
        return m_ExtendedNetworkManager.IsAuthorityInstance();
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
