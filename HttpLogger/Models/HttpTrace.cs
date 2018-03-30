using System;
using System.Net;
using System.Net.Http;

namespace HttpLogger.Models
{
    /// <summary>
    /// Defines the <see cref="HttpTrace"/> data class, for the HttpTrace logs.
    /// </summary>
	public class HttpTrace
	{
        /// <summary>
        /// Gets or sets the Id of the HttpTrace instance.
        /// </summary>
		public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Client IP Address of the HTTP Request as an <see cref="IPAddress"/>.
        /// </summary>
		public IPAddress ClientIPAddress { get; set; }
		
        /// <summary>
        /// Gets or sets the User Identifier of the HTTP Request.
        /// </summary>
		public string UserIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the User Id of the HTTP request.
        /// </summary>
		public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> of the HTTP request.
        /// </summary>
		public DateTime RequestDate { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Command header within the original HTTP Request.
        /// </summary>
		public string HttpCommand { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Uri"/> of the requested uri to the remote server.
        /// </summary>
		public Uri RemoteUri { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Method of the request.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpStatusCode"/> response from the remote.
        /// </summary>
		public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the content size in bytes. 
        /// </summary>
		public int ContentSize { get; set; }

	}
}
