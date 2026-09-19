using System.Linq;
using Unity.Netcode;

public class GenericBallLogic : NetworkBehaviour
{
    public bool LogSpawnInfo;
    private void Log(string message)
    {
        if (LogSpawnInfo)
        {
            ExtendedNetworkManager.Instance.LogMessage(message);
        }        
    }

    public override void OnNetworkSpawn()
    {
        name = $"GenericBall-{NetworkObjectId}";
        Log($"[{name}] Spawned complete.");
        base.OnNetworkSpawn();
    }

    protected override void OnNetworkPostSpawn()
    {
        Log($"[{name}] Spawned complete.");
        UpdateColor();
        base.OnNetworkPostSpawn();
    }

    private void UpdateColor()
    {
        var ownerPlayer = NetworkManager.SpawnManager.GetPlayerNetworkObjects(OwnerClientId).Where((c)=> c.IsPlayerObject).First();
        if (ownerPlayer != null) 
        {
            var playerColor = ownerPlayer.GetComponent<PlayerColor>();
            playerColor.SetObjectColor(NetworkObject);
        }
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        UpdateColor();
        base.OnOwnershipChanged(previous, current);
    }

    public override void OnNetworkDespawn()
    {
        Log($"[{name}] De-Spawning...");
        base.OnNetworkDespawn();
    }
}
