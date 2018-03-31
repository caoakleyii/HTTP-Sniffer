namespace HttpLogger.Models
{
    /// <summary>
    /// Defines a <see cref="ThreadObject"/> which encapsulates data needed for a thread and callback to handle when its done.
    /// </summary>
    public class ThreadObject
    {
        public delegate object Callback (object response);

        /// <summary>
        /// Creates a new instance of <see cref="ThreadObject"/>.
        /// Instantiates an empty callback.
        /// </summary>
        public ThreadObject()
        {
            this.ThreadCallback = o => o;
        }

        /// <summary>
        /// Gets or sets an object of data to be based to the thread.
        /// </summary>
        public object ThreadStartObject { get; set; }

        /// <summary>
        /// Gets or sets a callback delegate that should be called at the end of a thread call.
        /// </summary>
        public Callback ThreadCallback { get; set; }
    }
}
