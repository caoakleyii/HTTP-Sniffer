using System;
using System.Net;

namespace HttpLogger.Models
{
	public class ProxyRequest
	{
		public string Method { get; set; }

		public string RemoteUri { get; set; }

		public Version Version { get; set; }

		public bool IsHttps { get; set; }
		
		public bool SuccessfulInitializaiton { get; set; }

		public int ContentLength { get; set; }

		public IPAddress IPAddress { get; set; }
		
		public HttpStatusCode StatusCode { get; set; }

		public DateTime RequestDateTime { get; set; }

	}
}
