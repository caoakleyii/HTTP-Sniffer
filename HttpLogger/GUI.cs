using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Contexts;
using HttpLogger.Models;
using HttpLogger.Repositories;
using HttpLogger.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLogger
{
    /// <summary>
    /// Defines a simple Inversion of Control class to resolve dependencies between services and repositories.
    /// </summary>
    public class GUI
    {
        /// <summary>
        /// Creates a new instance of a <see cref="GUI"/>. 
        /// </summary>
        private GUI()
        {
            this.TraceViewModel = new TraceView();
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="GUI"/>
        /// </summary>
        public static GUI Instance => Nested.instance;

        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        public TraceView TraceViewModel { get; }

        /// <summary>
        /// Pirvate nested class for a lazy loading threadsafe singleton object
        /// </summary>
        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            // http://csharpindepth.com/Articles/General/Beforefieldinit.aspx
            static Nested()
            {

            }

            /// <summary>
            /// internal instance of the FileContext.
            /// </summary>
            internal static readonly GUI instance = new GUI();
        }
    }
}
