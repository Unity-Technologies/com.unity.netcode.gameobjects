namespace Unity.Netcode
{
    /// <summary>
    /// The different types of scene events communicated between a server and client. <br/>
    /// Used by <see cref="NetworkSceneManager"/> for <see cref="SceneEventMessage"/> messages.<br/>
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// See also: <br/>
    /// <seealso cref="SceneEvent"/>
    /// </summary>
    public enum SceneEventType : byte
    {
        /// <summary>
        /// Load a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client are notified a load scene event started
        /// </summary>
        Load,
        /// <summary>
        /// Unload a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client are notified an unload scene event started.
        /// </summary>
        Unload,
        /// <summary>
        /// Synchronizes current game session state for newly approved clients<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Server and Client receives a local notification (<em>server receives the ClientId being synchronized</em>).
        /// </summary>
        Synchronize,
        /// <summary>
        /// Game session re-synchronization of NetworkObjects that were destroyed during a <see cref="Synchronize"/> event<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification<br/>
        /// <em>Note: This will be removed once snapshot and buffered messages are finalized as it will no longer be needed at that point.</em>
        /// </summary>
        ReSynchronize,
        /// <summary>
        /// All clients have finished loading a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to Client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification containing the clients that finished
        /// as well as the clients that timed out(<em>if any</em>).
        /// </summary>
        LoadEventCompleted,
        /// <summary>
        /// All clients have unloaded a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to Client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification containing the clients that finished
        /// as well as the clients that timed out(<em>if any</em>).
        /// </summary>
        UnloadEventCompleted,
        /// <summary>
        /// A client has finished loading a scene<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        LoadComplete,
        /// <summary>
        /// A client has finished unloading a scene<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        UnloadComplete,
        /// <summary>
        /// A client has finished synchronizing from a <see cref="Synchronize"/> event<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        SynchronizeComplete,
    }
}
