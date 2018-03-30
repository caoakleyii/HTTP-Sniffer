using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using HttpLogger.Contexts;
using HttpLogger.Models;

namespace HttpLogger.Repositories
{
    /// <summary>
    /// Defines the <see cref="HttpTraceRepository"/> class which implements <see cref="IHttpTraceRepository"/> to handle CRUD operations of <see cref="HttpTrace"/>
    /// </summary>
	public class HttpTraceRepository : IHttpTraceRepository
	{
        /// <summary>
        /// Gets the file database context being used to store http trace logs.
        /// </summary>
		public IFileContext Context { get; }

        /// <summary>
        /// Creates a new instance of a <see cref="HttpTraceRepository"/>
        /// </summary>
		public HttpTraceRepository() : this (IoC.Instance.Resolve<IFileContext>())
		{
			
		}

	    internal HttpTraceRepository(IFileContext fileContext)
	    {
	        this.Context = fileContext;
        }

	    /// <summary>
	    /// Stores an <see cref="HttpTrace"/> in the database defined.
	    /// </summary>
	    /// <param name="trace">The HttpTrace to be stored</param>
		public void CreateTrace(HttpTrace trace)
		{
			trace.Id = Guid.NewGuid().ToString();

			this.Context.HttpTraces.Add(trace.Id, trace);
		}

	    /// <summary>
	    /// Returns an <see cref="IDictionary{TKey,TValue}"/> of the all <see cref="HttpTrace"/> stored.
	    /// </summary>
	    /// <returns>Returns <see cref="IDictionary{TKey,TValue}"/></returns>
		public IOrderedDictionary ReadTraces()
		{
			return this.Context.HttpTraces;
		}

	    /// <summary>
	    /// Returns the specific <see cref="HttpTrace"/> associated with the Id provided.
	    /// </summary>
	    /// <param name="id">The id of the HttpTrace to be returned.</param>
	    /// <returns>Returns an <see cref="HttpTrace"/></returns>
		public HttpTrace ReadTrace(string id)
		{
			return this.Context.HttpTraces[id] as HttpTrace;
		}

	    /// <summary>
	    /// Saves the changes made to the underlying database.
	    /// </summary>
		public void SaveChanges()
		{
			this.Context.SaveChanges();
		}
	}
}
