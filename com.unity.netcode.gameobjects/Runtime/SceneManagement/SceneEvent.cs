using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// Used for local notifications of various scene events.  The <see cref="NetworkSceneManager.OnSceneEvent"/> of
    /// delegate type <see cref="NetworkSceneManager.SceneEventDelegate"/> uses this class to provide
    /// scene event status.<br/>
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// <em>*** Do not start new scene events within scene event notification callbacks.</em><br/>
    /// See also: <br/>
    /// <seealso cref="SceneEventType"/>
    /// </summary>
    public class SceneEvent
    {
        /// <summary>
        /// The <see cref="UnityEngine.AsyncOperation"/> returned by <see cref="SceneManager"/><BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// </list>
        /// </summary>
        public AsyncOperation AsyncOperation;

        /// <summary>
        /// Will always be set to the current <see cref="Netcode.SceneEventType"/>
        /// </summary>
        public SceneEventType SceneEventType;

        /// <summary>
        /// If applicable, this reflects the type of scene loading or unloading that is occurring.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public LoadSceneMode LoadSceneMode;

        /// <summary>
        /// This will be set to the scene name that the event pertains to.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public string SceneName;

        /// <summary>
        /// When a scene is loaded, the Scene structure is returned.<BR/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// </list>
        /// </summary>
        public Scene Scene;

        /// <summary>
        /// The client identifier can vary depending upon the following conditions: <br/>
        /// <list type="number">
        /// <item><term><see cref="Netcode.SceneEventType"/>s that always set the <see cref="ClientId"/>
        /// to the local client identifier, are initiated (and processed locally) by the
        /// server-host, and sent to all clients to be processed.<br/>
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.Load"/></term></item>
        /// <item><term><see cref="SceneEventType.Unload"/></term></item>
        /// <item><term><see cref="SceneEventType.Synchronize"/></term></item>
        /// <item><term><see cref="SceneEventType.ReSynchronize"/></term></item>
        /// </list>
        /// </term></item>
        /// <item><term>Events that always set the <see cref="ClientId"/> to the local client identifier,
        /// are initiated (and processed locally) by a client or server-host, and if initiated
        /// by a client will always be sent to and processed on the server-host:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadComplete"/></term></item>
        /// <item><term><see cref="SceneEventType.SynchronizeComplete"/></term></item>
        /// </list>
        /// </term></item>
        /// <item><term>
        /// Events that always set the <see cref="ClientId"/> to the ServerId:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </term></item>
        /// </list>
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// List of clients that completed a loading or unloading event.<br/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public List<ulong> ClientsThatCompleted;

        /// <summary>
        /// List of clients that timed out during a loading or unloading event.<br/>
        /// This is set for the following <see cref="Netcode.SceneEventType"/>s:
        /// <list type="bullet">
        /// <item><term><see cref="SceneEventType.LoadEventCompleted"/></term></item>
        /// <item><term><see cref="SceneEventType.UnloadEventCompleted"/></term></item>
        /// </list>
        /// </summary>
        public List<ulong> ClientsThatTimedOut;
    }
}
