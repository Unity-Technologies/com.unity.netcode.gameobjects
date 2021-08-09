using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;

public class RelayUtility
{
    async public static Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
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

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCodeResponse = await RelayService.AllocationsApiClient.CreateJoincodeAsync(new CreateJoincodeRequest(new JoinCodeRequest(allocationResponse.Result.Data.Allocation.AllocationId)));
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        return (allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.Key, createJoinCodeResponse.Result.Data.JoinCode);
    }

    async public static Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
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

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        return (allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
    }
}
