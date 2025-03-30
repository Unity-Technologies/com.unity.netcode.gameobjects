using System.Collections.Generic;
using Unity.Netcode.Components;


public class AuthorityTrackedNetworkTransform : NetworkTransform
{
    #region static methods and properties
    public static int InstanceCount => s_AuthInstances.Count + s_NonAuthInstances.Count;
    public static int OwnedInstances => s_AuthInstances.Count;
    private static List<AuthorityTrackedNetworkTransform> s_NonAuthInstances = new List<AuthorityTrackedNetworkTransform>();
    private static List<AuthorityTrackedNetworkTransform> s_AuthInstances = new List<AuthorityTrackedNetworkTransform>();
    public static System.Action<AuthorityTrackedNetworkTransform> InstanceSpawned;

    public static List<AuthorityTrackedNetworkTransform> AuthorityInstances => s_AuthInstances;

    private static void LogMessage(string message)
    {
        ExtendedNetworkManager.Instance.LogMessage(message);
    }

    public static void ToggleSmoothLerp(bool isEnabled)
    {
        foreach (var instance in s_NonAuthInstances)
        {
            if (instance.IsSpawned && !instance.IsOwner)
            {
                instance.PositionLerpSmoothing = isEnabled;
            }
        }

        var enabled = isEnabled ? "Enabled" : "Disabled";
        LogMessage($"Smooth Lerp is now {enabled}.");
    }

    public static void ChangeInterplationType(InterpolationTypes interpolationType)
    {
        foreach (var instance in s_NonAuthInstances)
        {
            if (instance.IsSpawned && !instance.IsOwner)
            {
                instance.PositionInterpolationType = interpolationType;
            }
        }
        LogMessage($"InterpolationType changed to: {interpolationType}.");
    }

    public static void UpdateSmoothLerp(float value)
    {
        foreach (var instance in s_NonAuthInstances)
        {
            if (instance.IsSpawned && !instance.IsOwner)
            {
                instance.PositionMaxInterpolationTime = value;
            }
        }
        LogMessage($"Maximum smooth lerp time is now {value}.");
    }

    public static void DespawnOwnedObjects()
    {
        if (s_AuthInstances.Count == 0)
        {
            LogMessage($"No {nameof(AuthorityTrackedNetworkTransform)} instances are spawned that are owned by this client!");
        }
        else
        {
            LogMessage($"Despawning {s_AuthInstances.Count} {nameof(AuthorityTrackedNetworkTransform)} instances.");
        }
        for (int i = s_AuthInstances.Count - 1; i >= 0; i--)
        {
            var instance = s_AuthInstances[i];
            if (instance.IsSpawned && instance.IsOwner)
            {
                instance.NetworkObject.Despawn();
            }
        }
        s_AuthInstances.Clear();
    }
    #endregion

    public bool TrackAuthorityInstances;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!TrackAuthorityInstances)
        {
            return;
        }

        if (HasAuthority)
        {
            s_AuthInstances.Add(this);
        }
        else
        {
            s_NonAuthInstances.Add(this);
        }
    }

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        if (!TrackAuthorityInstances)
        {
            return;
        }
        InstanceSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        if (!TrackAuthorityInstances)
        {
            return;
        }
        if (s_AuthInstances.Contains(this))
        {
            s_AuthInstances.Remove(this);
        }
        else if (s_NonAuthInstances.Contains(this))
        {
            s_NonAuthInstances.Remove(this);
        }
        base.OnNetworkDespawn();
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (!TrackAuthorityInstances)
        {
            return;
        }
        if (s_NonAuthInstances.Contains(this))
        {
            s_NonAuthInstances.Remove(this);
        }
        else if (!s_AuthInstances.Contains(this))
        {
            s_AuthInstances.Add(this);
        }
    }

    public override void OnLostOwnership()
    {
        if (!TrackAuthorityInstances)
        {
            return;
        }
        if (!s_NonAuthInstances.Contains(this))
        {
            s_NonAuthInstances.Add(this);
        }
        if (s_AuthInstances.Contains(this))
        {
            s_AuthInstances.Remove(this);
        }
        base.OnLostOwnership();
    }
}

