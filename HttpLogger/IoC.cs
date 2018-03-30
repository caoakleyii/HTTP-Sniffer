using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Contexts;
using HttpLogger.Repositories;
using HttpLogger.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLogger
{
    /// <summary>
    /// Defines a simple Inversion of Control class to resolve dependencies between services and repositories.
    /// </summary>
    public class IoC
    {
        /// <summary>
        /// Creates a new instance of a <see cref="IoC"/>. 
        /// </summary>
        private IoC()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IFileContext, FileContext>();
            serviceCollection.AddTransient<IHttpTracerService, HttpTracerService>();
            serviceCollection.AddTransient<IHttpTraceRepository, HttpTraceRepository>();

            this.Provider = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="IoC"/>
        /// </summary>
        public static IoC Instance => Nested.instance;

        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        private ServiceProvider Provider { get; }

        /// <summary>
        /// Resoloves a dependency for T
        /// </summary>
        /// <typeparam name="T">The service to be resolved</typeparam>
        /// <returns>The service implementation.</returns>
        public T Resolve<T>()
        {
            return this.Provider.GetService<T>();
        }

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
            internal static readonly IoC instance = new IoC();
        }
    }
}
