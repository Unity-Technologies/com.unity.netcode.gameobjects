using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool CanWriteToTransform => IsClient && IsOwner;

        // private new NetworkTransformState m_LocalAuthoritativeNetworkState;

        // private Transform m_Transform;
        // void Awake()
        // {
        //     // duplicated code with ServerNetworkTransform
        //
        //     m_Transform = transform;
        //
        //     // set initial value for spawn
        //     if (CanWriteToTransform)
        //     {
        //         CommitTransformToServer(transform, NetworkManager.LocalTime.Time);
        //         // var isDirty = ApplyTransformToNetworkState(ref m_LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time, m_Transform);
        //         // ReplicateToGhosts(m_LocalAuthoritativeNetworkState, isDirty);
        //     }
        // }

        protected override void Update()
        {
            base.Update();
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                if (CanWriteToTransform)
                {
                    TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                }
            }
        }




    //     // SDK
    //     class abstract NetworkTransform
    //     {
    //         protected void CommitToServer()
    //         {
    //             CommitServerRpc();
    //         }
    //
    //         [ServerRpc]
    //         protected void CommitServerRpc(Transform t)
    //         {
    //             MyStateNetVar.Value = t;
    //         }
    //
    //         void Update()
    //         {
    //             this.transform = Interpolate(MyStateNetVar.Value);
    //         }
    //
    //         NetworkVariable<State> MyStateNetVar;
    //
    //     public Action<Transform> OnValueChanged;
    // }
    //
    //     class ClientNetworkTransform : NetworkTransform
    //     {
    //         void Update()
    //         {
    //             // read my gameObject.transform
    //             if (isClient && isOwner)
    //             {
    //                 this.CommitServerRpc(this.transform);
    //             }
    //             // check if tries to change server side and revert
    //         }
    //     }
    //
    //     class ServerNetworkTransform : NetworkTransform
    //     {
    //         void Update()
    //         {
    //             if (isServer)
    //             {
    //                 this.CommitServerRpc(this.transform);
    //             }
    //             // check if tries to change client side and revert
    //         }
    //     }
    //
    //     class AnticipationNetworkTransform : NetworkTransform
    //     {
    //         void Update()
    //         {
    //             if (IsServer)
    //             {
    //                 this.CommitServerRpc(this.transform);
    //             }
    //         }
    //     }


        /*
         *
         * - add a Commit() explicit thing to NT
- ClientNT / ServerNT / etc. have convenience link between GO.transform and Commit
- if you write to GO.transform in NT desync, maybe someday we show you that in a gfx help
Matt Walsh to Everyone (4:07 PM)
- gives users easier write their own / more liberty / NT doesn’t seize the host
Seize the transform
- Commit() should send an RPC to the server if I’m a client
- more symmetrical
         */
    }
}
