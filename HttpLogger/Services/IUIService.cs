using HttpLogger.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLogger.Services
{
    public interface IUIService
    {
        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        TraceView TraceViewModel { get; }

        /// <summary>
        /// Gets or set a value indicating whether or not to display GUI
        /// </summary>
        bool DisplayUI { get; set; }

        /// <summary>
        /// Handle the rendering of the UI within the Console.
        /// </summary>
        void Render();
    }
}