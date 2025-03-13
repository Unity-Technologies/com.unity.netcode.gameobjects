using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum NetworkObjectStatus
{
    Spawned,
    Despawned
}

public class BaseNetcodeExtension : NetworkBehaviour, IExtensionHandler
{
    #region UNITY EDITOR
#if UNITY_EDITOR
    [HideInInspector]
    public bool BaseNetcodeExtensionExpanded;
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
                if (!IsExpandedTable.Keys.Contains(parent))
                {
                    IsExpandedTable.Add(parent, false);
                }
            }
            parent = new SerializableType(parent.Type.BaseType);
            if (parent.Type == typeof(NetworkBehaviour))
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
    #endregion

    public uint SortOrder = 500;
    public DrawHandler Draw;

    public Action<NetworkObject, NetworkObjectStatus> NetworkObjectStatusUpdate;

    /// <summary>
    /// This is controlled by the NetworkObject's <see cref="NetworkObject.DestroyWithScene"/> property.
    /// </summary>
    /// <remarks>
    /// We don't need to handle migrating the BaseNetcodeExtension since it will be auto-migrated by NGO.
    /// </remarks>
    public bool DestroyOnLoad => ShouldDestroyOnLoad();

    protected ExtendedNetworkManager m_ExtendedNetworkManager;
    protected ConnectionStates m_ConnectionState;

    protected NetworkSceneManager SceneManager => m_ExtendedNetworkManager.SceneManager;

    protected bool m_ApplicationExitPending;

    protected virtual void OnAwake()
    {

    }

    private void Awake()
    {
        Draw = new DrawHandler();
        ExtendedNetworkManager.AttachExtension(this);
        OnAwake();
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

    private bool ShouldDestroyOnLoad()
    {
        if (!NetworkObject)
        {
            return true;
        }
        return NetworkObject.DestroyWithScene;
    }

    protected override void OnNetworkPostSpawn()
    {
        NetworkObjectStatusUpdate?.Invoke(NetworkObject, NetworkObjectStatus.Spawned);
        base.OnNetworkPostSpawn();
    }

    public override void OnNetworkDespawn()
    {
        NetworkObjectStatusUpdate?.Invoke(NetworkObject, NetworkObjectStatus.Despawned);
        base.OnNetworkDespawn();
    }
}
