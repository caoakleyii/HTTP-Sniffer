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
    public class GUI : IGUI
    {
        /// <summary>
        /// Creates a new instance of a <see cref="GUI"/>. 
        /// </summary>
        public GUI()
        {
            this.TraceViewModel = new TraceView();
        }
        
        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        public TraceView TraceViewModel { get; }

        /// <summary>
        /// Gets or set a value indicating whether or not to display GUI
        /// </summary>
        public bool DisplayGUI { get; set; }

    }

}
