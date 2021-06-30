namespace MLAPI.Logging
{
    /// <summary>
    /// Log level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Developer logging level, most verbose
        /// </summary>
        Developer,

        /// <summary>
        /// Normal logging level, medium verbose
        /// </summary>
        Normal,

        /// <summary>
        /// Error logging level, very quiet
        /// </summary>
        Error,

        /// <summary>
        /// Nothing logging level, no logging will be done
        /// </summary>
        Nothing
    }
}