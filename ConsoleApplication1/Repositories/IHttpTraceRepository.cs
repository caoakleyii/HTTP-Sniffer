using System.Collections.Generic;
using System.Collections.Specialized;
using HttpLogger.Models;

namespace HttpLogger.Repositories
{
    /// <summary>
    /// Defines the <see cref="IHttpTraceRepository"/> interface, which defines the method required of any Http Trace Repositories.
    /// </summary>
	public interface IHttpTraceRepository
	{
        /// <summary>
        /// Stores an <see cref="HttpTrace"/> in the database defined.
        /// </summary>
        /// <param name="trace">The HttpTrace to be stored</param>
		void CreateTrace(HttpTrace trace);

        /// <summary>
        /// Returns an <see cref="IDictionary{TKey,TValue}"/> of the all <see cref="HttpTrace"/> stored.
        /// </summary>
        /// <returns>Returns <see cref="IDictionary{TKey,TValue}"/></returns>
		IOrderedDictionary ReadTraces();

        /// <summary>
        /// Returns the specific <see cref="HttpTrace"/> associated with the Id provided.
        /// </summary>
        /// <param name="id">The id of the HttpTrace to be returned.</param>
        /// <returns>Returns an <see cref="HttpTrace"/></returns>
		HttpTrace ReadTrace(string id);

        /// <summary>
        /// Saves the changes made to the underlying database.
        /// </summary>
		void SaveChanges();
	}
}
