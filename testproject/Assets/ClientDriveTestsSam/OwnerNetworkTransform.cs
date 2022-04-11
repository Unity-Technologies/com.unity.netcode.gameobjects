using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class OwnerNetworkTransform : NetworkTransform
{
    private NetworkVariable<NetworkTransformState> m_ReplicatedNetworkStateInternal = new(
        new NetworkTransformState(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner); // THIS HERE
    protected override NetworkVariable<NetworkTransformState> m_ReplicatedNetworkState => m_ReplicatedNetworkStateInternal;
    public override bool CanCommitToTransform => IsOwner;
}