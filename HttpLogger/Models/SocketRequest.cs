using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpLogger.Models
{
    public class SocketRequest
    {

        /// <summary>
        /// Gets or sets the HTTP method of the request.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IPAddress"/> of the client.
        /// </summary>
		public IPAddress IPAddress { get; set; }

        /// <summary>
        /// The HTTP Command found within the header of the request
        /// </summary>
        public string HttpCommand { get; set; }

        /// <summary>
        /// Gets or sets the content size of the request in bytes.
        /// </summary>
        public int ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpStatusCode"/> of the response from remote.
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> of the request.
        /// </summary>
        public DateTime RequestDateTime { get; set; }

        /// <summary>
        /// Gets or sets the URI of the remote for the request.
        /// </summary>
        public Uri RemoteUri { get;  set; }
    }
}
