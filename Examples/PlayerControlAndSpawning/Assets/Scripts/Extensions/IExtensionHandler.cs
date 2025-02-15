
using UnityEngine;
public enum ScreenSpaceRegions
{
    TopLeft,
    TopRight
}

public interface IExtensionHandler
{
    uint GetSortOrder();

    void Initialize(ExtendedNetworkManager extendedNetworkManager);

    bool HasInitialized();

    void Destroying();

    void StatusUpdate(ConnectionStates connectionState);

    void AuthorityUpdate();

    void NonAuthorityUpdate();

    public Rect GUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion);
}
