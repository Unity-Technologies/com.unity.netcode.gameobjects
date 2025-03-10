using System;
using Unity.Netcode;
using UnityEngine;

public class OwnershipChangeNonAuthorityExtension : BaseNetcodeExtension
{
    public KeyCode OwnershipKeyCode = KeyCode.O;

    public Action<ulong, ulong> OwnershipChanged;

    protected override void OnNonAuthorityUpdate()
    {
        if (Input.GetKeyDown(OwnershipKeyCode))
        {
            ChangeOwnershipRpc();
        }
        base.OnNonAuthorityUpdate();
    }

    [Rpc(SendTo.Authority)]
    private void ChangeOwnershipRpc(RpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        OwnershipChanged?.Invoke(previous, current);
        base.OnOwnershipChanged(previous, current);
    }

    private Rect TopRightGUI(Rect totalRectSize)
    {
        totalRectSize = Draw.Label(totalRectSize, $"[{OwnershipKeyCode}] Take Ownership-RPC");
        return totalRectSize;
    }

    protected override Rect OnGUIUpdate(Rect totalRectSize, ScreenSpaceRegions screenSpaceRegion)
    {
        if (!IsSpawned || HasAuthority)
        {
            return totalRectSize;
        }
        switch (screenSpaceRegion)
        {
            case ScreenSpaceRegions.TopRight:
                {
                    totalRectSize = TopRightGUI(totalRectSize);
                    break;
                }
        }
        return base.OnGUIUpdate(totalRectSize, screenSpaceRegion);
    }
}
