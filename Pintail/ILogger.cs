namespace Nanoray.Pintail
{
    /// <summary>
    /// The log severity levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Troubleshooting info intended for Pintail developers.
        /// </summary>
        Trace,

        /// <summary>
        /// Troubleshooting info that may be relevant to developers using the Pintail library.
        /// </summary>
        Debug,

        /// <summary>
        /// Info that may be relevant to developers using the Pintail library.
        /// </summary>
        Info,

        /// <summary>
        /// An issue the developers using the Pintail library should be aware of, possibly also letting their users know about it.
        /// </summary>
        Warning,

        /// <summary>
        /// A message indicating something went wrong.
        /// </summary>
        Error
    }

    public interface ILogger
    {
        void Log(string message, LogLevel level);
    }
}
