#if UNITY_2022_3_OR_NEWER && (RELAY_SDK_INSTALLED && !UNITY_WEBGL ) || (RELAY_SDK_INSTALLED && UNITY_WEBGL && UTP_TRANSPORT_2_0_ABOVE)
using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Integration with Unity Relay SDK and Unity Transport that support the additional buttons in the NetworkManager inspector.
    /// This code could theoretically be used at runtime, but we would like to avoid the additional dependencies in the runtime assembly of netcode for gameobjects.
    /// </summary>
    public static class NetworkManagerRelayIntegration
    {

#if UNITY_WEBGL
        private const string k_DefaultConnectionType = "wss";
#else
        private const string k_DefaultConnectionType = "dtls";
#endif

        /// <summary>
        /// Easy relay integration (host): it will initialize the unity services, sign in anonymously and start the host with a new relay allocation.
        /// Note that this will force the use of Unity Transport.
        /// </summary>
        /// <param name="networkManager">The network manager that will start the connection</param>
        /// <param name="maxConnections">Maximum number of connections to the created relay.</param>
        /// <param name="connectionType">The connection type of the <see cref="RelayServerData"/> (wss, ws, dtls or udp) </param>
        /// <returns>The join code that a potential client can use and the allocation</returns>
        /// <exception cref="ServicesInitializationException"> Exception when there's an error during services initialization </exception>
        /// <exception cref="UnityProjectNotLinkedException"> Exception when the project is not linked to a cloud project id </exception>
        /// <exception cref="CircularDependencyException"> Exception when two registered <see cref="IInitializablePackage"/> depend on the other </exception>
        /// <exception cref="AuthenticationException"> The task fails with the exception when the task cannot complete successfully due to Authentication specific errors. </exception>
        /// <exception cref="RequestFailedException"> See <see cref="IAuthenticationService.SignInAnonymouslyAsync"/></exception>
        /// <exception cref="ArgumentException">Thrown when the maxConnections argument fails validation in Relay Service SDK.</exception>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an error.</exception>
        internal static async Task<(string, Allocation)> StartHostWithRelay(this NetworkManager networkManager, int maxConnections = 5)
        {
            var codeAndAllocation = await InitializeAndCreateAllocAsync(networkManager, maxConnections, k_DefaultConnectionType);
            return networkManager.StartHost() ? codeAndAllocation : (null, null);
        }

        /// <summary>
        /// Easy relay integration (server): it will initialize the unity services, sign in anonymously and start the server with a new relay allocation.
        /// Note that this will force the use of Unity Transport.
        /// </summary>
        /// <param name="networkManager">The network manager that will start the connection</param>
        /// <param name="maxConnections">Maximum number of connections to the created relay.</param>
        /// <returns>The join code that a potential client can use and the allocation.</returns>
        /// <exception cref="ServicesInitializationException"> Exception when there's an error during services initialization </exception>
        /// <exception cref="UnityProjectNotLinkedException"> Exception when the project is not linked to a cloud project id </exception>
        /// <exception cref="CircularDependencyException"> Exception when two registered <see cref="IInitializablePackage"/> depend on the other </exception>
        /// <exception cref="AuthenticationException"> The task fails with the exception when the task cannot complete successfully due to Authentication specific errors. </exception>
        /// <exception cref="RequestFailedException"> See <see cref="IAuthenticationService.SignInAnonymouslyAsync"/></exception>
        /// <exception cref="ArgumentException">Thrown when the maxConnections argument fails validation in Relay Service SDK.</exception>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an error.</exception>
        internal static async Task<(string, Allocation)> StartServerWithRelay(this NetworkManager networkManager, int maxConnections = 5)
        {
            var codeAndAllocation = await InitializeAndCreateAllocAsync(networkManager, maxConnections, k_DefaultConnectionType);
            return networkManager.StartServer() ? codeAndAllocation : (null, null);
        }

        /// <summary>
        /// Easy relay integration (client): it will initialize the unity services, sign in anonymously, join the relay with the given join code and start the client.
        /// Note that this will force the use of Unity Transport.
        /// </summary>
        /// <param name="networkManager">The network manager that will start the connection</param>
        /// <param name="joinCode">The join code of the allocation</param>
        /// <exception cref="ServicesInitializationException"> Exception when there's an error during services initialization </exception>
        /// <exception cref="UnityProjectNotLinkedException"> Exception when the project is not linked to a cloud project id </exception>
        /// <exception cref="CircularDependencyException"> Exception when two registered <see cref="IInitializablePackage"/> depend on the other </exception>
        /// <exception cref="AuthenticationException"> The task fails with the exception when the task cannot complete successfully due to Authentication specific errors. </exception>
        /// <exception cref="RequestFailedException">Thrown when the request does not reach the Relay Allocation Service.</exception>
        /// <exception cref="ArgumentException">Thrown if the joinCode has the wrong format.</exception>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an error.</exception>
        /// <returns>True if starting the client was successful</returns>
        internal static async Task<JoinAllocation> StartClientWithRelay(this NetworkManager networkManager, string joinCode)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            GetUnityTransport(networkManager, k_DefaultConnectionType).SetRelayServerData(new RelayServerData(joinAllocation, k_DefaultConnectionType));
            return networkManager.StartClient() ? joinAllocation : null;
        }

        private static async Task<(string, Allocation)> InitializeAndCreateAllocAsync(NetworkManager networkManager, int maxConnections, string connectionType)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            GetUnityTransport(networkManager, connectionType).SetRelayServerData(new RelayServerData(allocation, connectionType));
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return (joinCode, allocation);
        }

        private static UnityTransport GetUnityTransport(NetworkManager networkManager, string connectionType)
        {
            if (!networkManager.TryGetComponent<UnityTransport>(out var transport))
            {
                transport = networkManager.gameObject.AddComponent<UnityTransport>();
            }
#if UTP_TRANSPORT_2_0_ABOVE
            transport.UseWebSockets = connectionType.StartsWith("ws"); // Probably should be part of SetRelayServerData, but not possible at this point
#endif
            networkManager.NetworkConfig.NetworkTransport = transport; // Force using UnityTransport
            return transport;
        }
    }
}
#endif
