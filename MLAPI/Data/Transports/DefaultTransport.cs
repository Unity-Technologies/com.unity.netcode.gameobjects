namespace MLAPI.Transports
{
    /// <summary>
    /// Supported built in transport
    /// </summary>
    public enum DefaultTransport
    {
        /// <summary>
        /// Unity's UNET transport
        /// </summary>
        UNET,
        /// <summary>
        /// MLAPI.Relay transport (UNET internally)
        /// </summary>
        MLAPI_Relay,
        /// <summary>
        /// Custom transport
        /// </summary>
        Custom
    }
}
