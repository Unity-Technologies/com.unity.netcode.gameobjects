using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Example of using a <see cref="NetworkVariable{T}"/> to drive changes
/// in state.
/// </summary>
/// <remarks>
/// This is a simple state driven door example.
/// This script was written with recommended usages patterns in mind.
/// </remarks>
public class Door : NetworkBehaviour, INetworkUpdateSystem
{
    /// <summary>
    /// The two door states.
    /// </summary>
    public enum DoorStates
    {
        Closed,
        Open
    }

    /// <summary>
    /// Initializes the door to a specific state (server side) when first spawned.
    /// </summary>
    [Tooltip("Configures the door's initial state when 1st spawned.")]
    public DoorStates InitialState = DoorStates.Closed;

    /// <summary>
    /// Used for <see cref="CanPlayerToggleState"/> example purposes.
    /// When true, only the server can open and close the door.
    /// Clients will receive a console log saying they could not open the door.
    /// </summary>
    public bool IsLocked;

    /// <summary>
    /// A simple door state where the server has write permissions and everyone has read permissions.
    /// </summary>
    private NetworkVariable<DoorStates> m_State = new NetworkVariable<DoorStates>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// The current state of the door.
    /// </summary>
    public DoorStates CurrentState => m_State.Value;

    /// <summary>
    /// Invoked while the <see cref="NetworkObject"/> is in the middle of
    /// being spawned.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // The write authority (server) does not need to know about its
        // own changes (for this example) since it is the "single point
        // of truth" for the door instance.
        if (IsServer)
        {
            // Host/Server:
            // Applies the configurable state upon spawning.
            m_State.Value = InitialState;
        }
        else
        {
            // Clients:
            // Subscribe to changes in the door's state.
            m_State.OnValueChanged += OnStateChanged;
        }
    }

    /// <summary>
    /// Invoked once the door and all associated components
    /// have finished the spawn process.
    /// </summary>
    protected override void OnNetworkPostSpawn()
    {
        // Everyone updates their door state when finished spawning the door
        // in order to assure the door reflects (visually) its current state.
        UpdateFromState();

        // Begin to start updating this NetworkBehaviour instance once all 
        // netcode related components have finished the spawn process.
        NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
        base.OnNetworkPostSpawn();
    }

    /// <summary>
    /// Example of using the <see cref="INetworkUpdateSystem"/> usage pattern
    /// where it only updates while spawned.
    /// </summary>
    /// <param name="updateStage">The current update stage being invoked.</param>
    public void NetworkUpdate(NetworkUpdateStage updateStage)
    {
        switch (updateStage)
        {
            case NetworkUpdateStage.Update:
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        Interact();
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// Invoked just before this instance runs through its de-spawn
    /// sequence. A good time to unsubscribe from things.
    /// </summary>
    public override void OnNetworkPreDespawn()
    {
        if (!IsServer)
        {
            m_State.OnValueChanged -= OnStateChanged;
        }

        // Stop updating this NetworkBehaviour instance prior to running
        // through the de-spawn process.
        NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
        base.OnNetworkPreDespawn();
    }

    /// <summary>
    /// Server makes changes to the state.
    /// Clients receive the changes in state.
    /// </summary>
    /// <remarks>
    /// When the previous state equals the current state, we are a client
    /// that is doing its 1st synchronization of this door instance.
    /// </remarks>
    /// <param name="previous">The previous <see cref="DoorStates"/> state.</param>
    /// <param name="current">The current <see cref="DoorStates"/> state.</param>
    public void OnStateChanged(DoorStates previous, DoorStates current)
    {
        UpdateFromState();
    }

    /// <summary>
    /// Invoke when the state is updated in order to apply the change
    /// in door state to the door asset itself.
    /// </summary>
    private void UpdateFromState()
    {
        switch(m_State.Value)
        {
            case DoorStates.Closed:
                {
                    // door is open:
                    //  - rotate door transform
                    //  - play animations, sound etc.
                    /// <see cref="Netcode.Components.Helpers.ComponentCont"
                    break;
                }
            case DoorStates.Open:
                {
                    // door is closed:
                    //  - rotate door transform
                    //  - play animations, sound etc.
                    break;
                }
        }
        Debug.Log($"[{name}] Door is currently {m_State.Value}.");
    }

    /// <summary>
    /// Override to apply specific checks (like a player having the right
    /// key to open the door) or make it a non-virtual class and add logic
    /// directly to this method.
    /// </summary>
    /// <param name="player">The player attempting to open the door.</param>
    /// <returns></returns>
    protected virtual bool CanPlayerToggleState(NetworkObject player)
    {
        // For this example, if the door "is locked" then clients will
        // not be able to open the door but the host-client's player can.
        return !IsLocked || player.IsOwnedByServer;
    }

    /// <summary>
    /// Invoked by either a Host or clients to interact with the door.
    /// </summary>
    public void Interact()
    {
        // Optional:
        // This is only if you want clients to be able to
        // interact with doors. A dedicated server would not
        // be able to do this since it does not have a player.
        if (IsServer && !IsHost)
        {
            // Optional to log a warning about this.
            return;
        }

        if (IsHost)
        {
            ToggleState(NetworkManager.LocalClientId);
        }
        else
        {
            // Clients send an RPC to server (write authority) who applies the
            // change in state that will be synchronized with all client observers.
            ToggleStateRpc();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DoorStates NextToggleState()
    {
        return m_State.Value == DoorStates.Open ? DoorStates.Closed : DoorStates.Open;
    }

    /// <summary>
    /// Invoked only server-side
    /// Primary method to handle toggling the door state.
    /// </summary>
    /// <param name="clientId">The client toggling the door state.</param>
    private void ToggleState(ulong clientId)
    {
        // Get the server-side client player instance
        var playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObject != null)
        {
            var nextToggleState = NextToggleState();
            if (CanPlayerToggleState(playerObject))
            {
                // Host toggles the state
                m_State.Value = nextToggleState;
                UpdateFromState();
            }
            else
            {
                ToggleStateFailRpc(nextToggleState, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            }
        }
        else
        {
            // Optional as to how you handle this. Since ToggleState is only invoked by
            // sever-side only script, this could mean many things depending upon whether
            // or not a client could interact with something and not have a player object.
            // If that is the case, then don't even bother checking for a player object.
            // If that is not the case, then there could be a timing issue between when
            // something can be "interacted with" and when a player is about to be de-spawned.
            // For this example, we just log a warning as this example was built with
            // the requirement that a client has a spawned player object that is used for
            // reference to determine if the client's player can toggle the state of the
            // door or not.
            NetworkLog.LogWarningServer($"Client-{clientId} has no spawned player object!");
        }
    }

    /// <summary>
    /// Invoked by clients.
    /// Re-directs to the common <see cref="ToggleState(ulong)"/> method.
    /// </summary>
    /// <param name="rpcParams">includes <see cref="RpcReceiveParams.SenderClientId"/> that is automatically populated for you.</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ToggleStateRpc(RpcParams rpcParams = default)
    {
        ToggleState(rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Optional:
    /// Handling when a player cannot open a door.
    /// </summary>
    /// <param name="rpcParams">includes <see cref="RpcReceiveParams.SenderClientId"/> that is automatically populated for you.</param>
    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void ToggleStateFailRpc(DoorStates doorState, RpcParams rpcParams = default)
    {
        // Provide player feedback that toggling failed.
        Debug.Log($"Failed to {doorState} the door!");
    }
}
