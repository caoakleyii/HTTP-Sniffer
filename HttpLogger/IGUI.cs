using HttpLogger.Models;

namespace HttpLogger
{
    public interface IGUI
    {

        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        TraceView TraceViewModel { get; }

        /// <summary>
        /// Gets or set a value indicating whether or not to display GUI
        /// </summary>
        bool DisplayGUI { get; set; }
    }
}