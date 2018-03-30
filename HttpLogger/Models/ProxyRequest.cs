using System;
using System.Net;

namespace HttpLogger.Models
{
    /// <summary>
    /// Defines the <see cref="ProxyRequest"/> model which is used for processing the middle man request between client and remote.
    /// </summary>
	public class ProxyRequest
	{
        /// <summary>
        /// Gets or sets the HTTP method of the request.
        /// </summary>
		public string Method { get; set; }

        /// <summary>
        /// Gets or sets the URI of the remote for the request.
        /// </summary>
		public string RemoteUri { get; set; }

        /// <summary>
        /// Gets or sets the HTTP <see cref="Version"/> for the request.
        /// </summary>
		public Version HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets if the request is using HTTPS.
        /// </summary>
		public bool IsHttps { get; set; }
		
        /// <summary>
        /// Gets or sets if the initialization of the request was successful.
        /// </summary>
		public bool SuccessfulInitializaiton { get; set; }

        /// <summary>
        /// Gets or sets the content size of the request in bytes.
        /// </summary>
		public int ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPAddress"/> of the client.
        /// </summary>
		public IPAddress IPAddress { get; set; }
		
        /// <summary>
        /// Gets or sets the <see cref="HttpStatusCode"/> of the response from remote.
        /// </summary>
		public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> of the request.
        /// </summary>
		public DateTime RequestDateTime { get; set; }

	}
}
