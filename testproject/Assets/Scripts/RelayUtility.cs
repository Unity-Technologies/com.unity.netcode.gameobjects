using System.Threading.Tasks;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;

public class RelayUtility
{
    async public static Task<(RelayServerData relayServerData, string joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Response<AllocateResponseBody> allocationResponse;
        Response<JoinCodeResponseBody> createJoinCodeResponse;
        try
        {
            allocationResponse = await RelayService.AllocationsApiClient.CreateAllocationAsync(new CreateAllocationRequest(new AllocationRequest(maxConnections, region)));
        }
        catch
        {
            Debug.LogError("Relay create allocation request failed");
            throw;
        }

        var allocation = allocationResponse.Result.Data.Allocation;

        var serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
        var allocationId = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
        var connectionData = ConvertConnectionData(allocation.ConnectionData);
        var key = ConvertFromHMAC(allocation.Key);

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref connectionData, ref key);
        relayServerData.ComputeNewNonce();

        try
        {
            createJoinCodeResponse = await RelayService.AllocationsApiClient.CreateJoincodeAsync(new CreateJoincodeRequest(new JoinCodeRequest(allocationResponse.Result.Data.Allocation.AllocationId)));
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        string joinCode = createJoinCodeResponse.Result.Data.JoinCode;

        return (relayServerData, joinCode);
    }

    async public static Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
    {
        Response<JoinResponseBody> joinResponse;
        try
        {
            joinResponse = await RelayService.AllocationsApiClient.JoinRelayAsync(new JoinRelayRequest(new JoinRequest(joinCode)));
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        var allocation = joinResponse.Result.Data.Allocation;

        var serverEndpoint = NetworkEndPoint.Parse(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port);
        var allocationId = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
        var connectionData = ConvertConnectionData(allocation.ConnectionData);
        var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
        var key = ConvertFromHMAC(allocation.Key);

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData, ref hostConnectionData, ref key);
        relayServerData.ComputeNewNonce();

        return relayServerData;
    }

    private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
    {
        unsafe
        {
            fixed (byte* ptr = allocationIdBytes)
            {
                return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
            }
        }
    }

    private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
    {
        unsafe
        {
            fixed (byte* ptr = hmac)
            {
                return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
            }
        }
    }

    private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
    {
        unsafe
        {
            fixed (byte* ptr = connectionData)
            {
                return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
            }
        }
    }
}
