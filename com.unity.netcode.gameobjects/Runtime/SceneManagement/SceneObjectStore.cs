using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    internal class SceneObjectStore
    {
        /// <summary>
        /// We organize our m_ObjectsPerScene by:
        /// [GlobalObjectIdHash][NetworkSceneHandle][NetworkObject]
        /// Using the local scene relative Scene.handle as a sub-key to the root dictionary allows us to
        /// distinguish between duplicate in-scene placed NetworkObjects
        /// </summary>
        private readonly Dictionary<uint, Dictionary<NetworkSceneHandle, NetworkObject>> m_ObjectsPerScene = new();

        /// <summary>
        /// Clean the store.
        /// </summary>
        internal void ClearAllStoredObjects()
        {
            m_ObjectsPerScene.Clear();
        }

        /// <summary>
        /// Should be invoked on both the client and server side after:
        /// -- A new scene has been loaded
        /// -- Before any "DontDestroyOnLoad" NetworkObjects have been added back into the scene.
        /// Added the ability to choose not to clear the scene placed objects for additive scene loading.
        /// </summary>
        internal void ProcessObjectsOnSceneLoad(Scene sceneToFilterBy, NetworkManager associatedNetworkManager, Dictionary<uint, Dictionary<NetworkSceneHandle, NetworkObject>> scenePlacedObjects)
        {
            var sceneHandle = sceneToFilterBy.handle;
            // if (associatedNetworkManager.LocalClientId == 1)
            // {
            //     Debug.Break();
            // }

            // Just add every NetworkObject found that isn't already in the list
            // With additive scenes, we can have multiple in-scene placed NetworkObjects with the same GlobalObjectIdHash value
            // During Client Side Synchronization: We add them on a FIFO basis, for each scene loaded without clearing, and then
            // at the end of scene loading we use this list to soft synchronize all in-scene placed NetworkObjects
            foreach (var obj in FindObjects.FromSceneByType<NetworkObject>(sceneToFilterBy, true))
            {
                if (!obj.AutoSpawnOnStart)
                {
                    continue;
                }

                if (obj.NetworkManagerOwner == null)
                {
                    obj.NetworkManagerOwner = associatedNetworkManager;
                }

                // We check to make sure the NetworkManager instance is the same one to be "NetcodeIntegrationTestHelpers" compatible
                if (obj.NetworkManagerOwner != associatedNetworkManager)
                {
                    continue;
                }

                var globalObjectIdHash = obj.GlobalObjectIdHash;

                // Add everything into m_ObjectsPerScene
                if (!m_ObjectsPerScene.ContainsKey(globalObjectIdHash))
                {
                    m_ObjectsPerScene.Add(globalObjectIdHash, new Dictionary<NetworkSceneHandle, NetworkObject>());
                }

                if (!m_ObjectsPerScene[globalObjectIdHash].ContainsKey(sceneHandle))
                {
                    Debug.Log($"[Client-{associatedNetworkManager.LocalClientId}] Saving object {obj.name} to sceneHandle {sceneHandle} with GlobalObjectIdHash:  {globalObjectIdHash}");
                    m_ObjectsPerScene[globalObjectIdHash].Add(sceneHandle, obj);
                }
                else if (!obj.HasBeenSpawned)
                {
                    var existing = m_ObjectsPerScene[globalObjectIdHash][sceneHandle];
                    var exitingEntryName = existing ==  null ? existing.name : "Null Entry";
                    throw new Exception($"{obj.name} tried to registered with {nameof(m_ObjectsPerScene)} which already contains " +
                        $"the same {nameof(NetworkObject.GlobalObjectIdHash)} value {globalObjectIdHash} for {exitingEntryName}!");
                }

                // Legacy path:
                // Only add active in-scene-placed objects into ScenePlacedObjects
                if (obj.isActiveAndEnabled && obj.InScenePlaced)
                {
                    if (!scenePlacedObjects.ContainsKey(globalObjectIdHash))
                    {
                        scenePlacedObjects.Add(globalObjectIdHash, new Dictionary<NetworkSceneHandle, NetworkObject>());
                    }

                    if (!scenePlacedObjects[globalObjectIdHash].ContainsKey(sceneHandle))
                    {
                        scenePlacedObjects[globalObjectIdHash].Add(sceneHandle, obj);
                    }
                    else
                    {
                        var existing = scenePlacedObjects[globalObjectIdHash][sceneHandle];
                        var exitingEntryName = existing ==  null ? existing.name : "Null Entry";
                        throw new Exception($"{obj.name} tried to registered with {nameof(NetworkSceneManager.ScenePlacedObjects)} which already contains " +
                                            $"the same {nameof(NetworkObject.GlobalObjectIdHash)} value {globalObjectIdHash} for {exitingEntryName}!");
                    }
                }
            }
        }


        /// <summary>
        /// During soft synchronization of in-scene placed NetworkObjects, this is now used by NetworkSpawnManager.CreateLocalNetworkObject
        /// </summary>
        internal NetworkObject GetSceneRelativeInSceneNetworkObject(uint globalObjectIdHash, NetworkSceneHandle sceneToCheck)
        {
            if (m_ObjectsPerScene.TryGetValue(globalObjectIdHash, out var scenePlacedObjectsForHash))
            {
                if (scenePlacedObjectsForHash.TryGetValue(sceneToCheck, out var scenePlaceObject))
                {
                    return scenePlaceObject;
                }
            }
            return null;
        }
    }
}
