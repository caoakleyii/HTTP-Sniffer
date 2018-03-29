using System;
using System.Net;

namespace HttpLogger.Models
{
	public class HttpTrace
	{
		public string Id { get; set; }

		public IPAddress ClientIPAddress { get; set; }
		
		public string UserIdentifier { get; set; }

		public string UserId { get; set; }

		public DateTime RequestDate { get; set; }

		public string HttpCommand { get; set; }

		public Uri RemoteUri { get; set; }

		public HttpStatusCode StatusCode { get; set; }

		public int ContentSize { get; set; }

	}
}
