using System.Collections.Generic;
using UnityEngine;
#if USING_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace Unity.Netcode
{
    /// <summary>
    /// Maintains the mapping between Addressable scene addresses and the <c>uint</c> hash values
    /// that <see cref="NetworkSceneManager"/> uses to identify scenes on the wire.
    /// </summary>
    /// <remarks>
    /// Addressable scenes are keyed by their address (a string). This registry hashes that address
    /// using the same <see cref="XXHash.Hash32(string)"/> algorithm used for build-settings scene
    /// paths, so an Addressable scene can travel on the wire in the exact same <c>uint</c> hash form
    /// with no protocol change.<br/>
    /// The address to hash mapping must be identical on the server (or session owner) and all clients.
    /// Populate it either automatically via <see cref="AutoScanCatalog"/> or explicitly via
    /// <see cref="Register(string)"/> before starting the <see cref="NetworkManager"/>.
    /// </remarks>
    internal class AddressableSceneRegistry
    {
        private readonly Dictionary<uint, string> m_HashToAddress = new Dictionary<uint, string>();
        private readonly Dictionary<string, uint> m_AddressToHash = new Dictionary<string, uint>();

        private readonly NetworkSceneManager m_SceneManager;

        internal AddressableSceneRegistry(NetworkSceneManager sceneManager)
        {
            m_SceneManager = sceneManager;
        }

        /// <summary>
        /// Computes the hash used to identify an Addressable scene from its address.
        /// </summary>
        internal static uint HashFromAddress(string address)
        {
            return XXHash.Hash32(address);
        }

        /// <summary>
        /// Registers an Addressable scene address, returning the hash that identifies it on the wire.
        /// If the address is already registered, the existing hash is returned.
        /// </summary>
        /// <param name="address">the Addressable address (key) of the scene</param>
        /// <returns>the <c>uint</c> hash used to identify the scene on the wire</returns>
        internal uint Register(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[{nameof(AddressableSceneRegistry)}] Attempted to register a null or empty Addressable scene address!");
                return 0;
            }

            if (m_AddressToHash.TryGetValue(address, out var existing))
            {
                return existing;
            }

            var hash = HashFromAddress(address);

            // Guard against colliding with a build-settings scene hash. Build-settings scenes take
            // precedence since they are resolved first; warn so the (extremely rare) collision is visible.
            if (m_SceneManager != null && m_SceneManager.HashToBuildIndex.ContainsKey(hash))
            {
                Debug.LogWarning($"[{nameof(AddressableSceneRegistry)}] Addressable scene address '{address}' hashes to {hash}, " +
                    $"which collides with a build-settings scene. The build-settings scene will take precedence. " +
                    $"Consider renaming the Addressable address to avoid the collision.");
                return hash;
            }

            m_HashToAddress[hash] = address;
            m_AddressToHash[address] = hash;
            return hash;
        }

        /// <summary>
        /// Registers an Addressable scene using an explicit hash/address pair, bypassing hash computation.
        /// Used when a client receives the server's Addressable scene table over the wire, so that the
        /// client resolves exactly the hashes the server sent (independent of any local hashing).
        /// </summary>
        internal void RegisterWithHash(uint hash, string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            // Build-settings scenes take precedence; skip (the server would have skipped it as well).
            if (m_SceneManager != null && m_SceneManager.HashToBuildIndex.ContainsKey(hash))
            {
                return;
            }

            m_HashToAddress[hash] = address;
            m_AddressToHash[address] = hash;
        }

        /// <summary>
        /// The number of registered Addressable scenes.
        /// </summary>
        internal int Count => m_HashToAddress.Count;

        /// <summary>
        /// The hash to address mapping of all registered Addressable scenes. Used to serialize the table
        /// so clients can resolve Addressable scene hashes without independent pre-registration.
        /// </summary>
        internal Dictionary<uint, string> HashToAddress => m_HashToAddress;

        /// <summary>
        /// Returns true if the provided hash corresponds to a registered Addressable scene.
        /// </summary>
        internal bool IsAddressableScene(uint hash)
        {
            return m_HashToAddress.ContainsKey(hash);
        }

        /// <summary>
        /// Returns true if the provided address corresponds to a registered Addressable scene.
        /// </summary>
        internal bool IsAddressableScene(string address)
        {
            return !string.IsNullOrEmpty(address) && m_AddressToHash.ContainsKey(address);
        }

        /// <summary>
        /// Attempts to resolve a registered Addressable scene hash to its address.
        /// </summary>
        internal bool TryGetAddress(uint hash, out string address)
        {
            return m_HashToAddress.TryGetValue(hash, out address);
        }

        /// <summary>
        /// Attempts to resolve a registered Addressable scene address to its hash.
        /// </summary>
        internal bool TryGetHash(string address, out uint hash)
        {
            return m_AddressToHash.TryGetValue(address, out hash);
        }

        /// <summary>
        /// Clears all registered Addressable scenes.
        /// </summary>
        internal void Clear()
        {
            m_HashToAddress.Clear();
            m_AddressToHash.Clear();
        }

#if USING_ADDRESSABLES
        /// <summary>
        /// Registers an Addressable scene from an <see cref="AssetReference"/> using its runtime key.
        /// </summary>
        internal uint Register(AssetReference sceneReference)
        {
            if (sceneReference == null || !sceneReference.RuntimeKeyIsValid())
            {
                Debug.LogError($"[{nameof(AddressableSceneRegistry)}] Attempted to register an invalid {nameof(AssetReference)}!");
                return 0;
            }
            return Register(sceneReference.RuntimeKey.ToString());
        }

        /// <summary>
        /// Automatically scans the Addressables content catalog for scene resource locations and
        /// registers each of their primary keys (addresses). This provides a zero-configuration way
        /// to make every Addressable scene networkable, provided the catalog is identical across
        /// server and clients (which it is for a shared build).
        /// </summary>
        internal void AutoScanCatalog()
        {
            // Iterate every loaded resource locator and, for each of its keys, ask for scene
            // (SceneInstance) locations. Keys that resolve to a scene are registered by their
            // primary key (address). We register by PrimaryKey so the resulting hash is identical
            // across all peers regardless of which alias/label was used to look it up.
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (locator.Locate(key, typeof(SceneInstance), out var locations) && locations != null)
                    {
                        foreach (var location in locations)
                        {
                            RegisterLocation(location);
                        }
                    }
                }
            }
        }

        private void RegisterLocation(IResourceLocation location)
        {
            if (location == null || string.IsNullOrEmpty(location.PrimaryKey))
            {
                return;
            }
            Register(location.PrimaryKey);
        }
#endif
    }
}
