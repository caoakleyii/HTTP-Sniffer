namespace HttpLogger.HttpMonitors
{
    /// <summary>
    /// Defines the <see cref="IMonitor"/> interface, which defines the method required of HTTP Monitoring implementations.
    /// </summary>
	public interface IMonitor
	{
        /// <summary>
        /// Starts the HTTP Monitoring process.
        /// </summary>
		void Start();

        /// <summary>
        /// Stops the HTTP Monitoring process and closes any open resources.
        /// </summary>
		void Stop();
	}
}