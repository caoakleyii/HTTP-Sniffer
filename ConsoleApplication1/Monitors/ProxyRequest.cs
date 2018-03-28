using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Providers;
using NLog;
using Org.BouncyCastle.Crypto;

namespace HttpLogger.Monitors
{
	public class ProxyRequest
	{
		
		public ProxyRequest(Stream clientStream, AsymmetricKeyParameter issuerKey)
		{
			this.ClientStream = clientStream;
			this.Logger = LogManager.GetCurrentClassLogger();
			this.IssuerKey = issuerKey;

			this.Initialize();
		}

		public Stream ClientStream { get; }

		public SslStream SslStream { get; private set; }

		public StreamReader ClientStreamReader { get; private set; }

		public string Method { get; private set; }

		public string RemoteUri { get; private set; }

		public Version Version { get; private set; }

		public bool IsHttps { get; private set; }

		public HttpWebRequest HttpRequest { get; set; }

		public bool SuccessfulInitializaiton { get; private set; }

		private AsymmetricKeyParameter IssuerKey { get; }
		
		private ILogger Logger { get; }

		public int ContentLength { get; private set; }

		public void Process()
		{
			// create the web request, we are issuing on behalf of the client.
			this.HttpRequest = (HttpWebRequest)WebRequest.Create(this.RemoteUri);
			this.HttpRequest.Method = this.Method;
			this.HttpRequest.ProtocolVersion = this.Version;

			//read the request headers from the client and copy them to our request
			this.ReadRequestHeaders(this.ClientStreamReader);

			// TODO: See if removing this null set is possible.
			this.HttpRequest.Proxy = null;
			this.HttpRequest.KeepAlive = false;
			this.HttpRequest.AllowAutoRedirect = false;
			this.HttpRequest.AutomaticDecompression = DecompressionMethods.None;

		}

		private void Initialize()
		{
			this.ClientStreamReader = new StreamReader(this.ClientStream);
			var httpCommand = this.ClientStreamReader.ReadLine();

			if (string.IsNullOrEmpty(httpCommand))
			{
				this.SuccessfulInitializaiton = false;
				Logger.Warn("Data header of a proxy request was null or empty.");
				return;
			}

			var httpCommandSplit = httpCommand.Split(' ');
			this.Method = httpCommandSplit[0];
			this.RemoteUri = httpCommandSplit[1];
			this.Version = new Version(httpCommandSplit[2].Split('/')[1]);
			
			if (this.Method == "CONNECT")
			{
				this.SuccessfulInitializaiton = this.SslHandshake();
				return;
			}

			this.SuccessfulInitializaiton = true;
		}

		private bool SslHandshake()
		{
			this.IsHttps = true;

			// Browser wants to create a secure tunnel
			// instead we are perform a man in the middle                    
			this.RemoteUri = "https://" + this.RemoteUri;

			var cert = CertificateProvider.GetSelfSignedCertificate(new Uri(this.RemoteUri), this.IssuerKey);
			
			//read and ignore headers
			while (!string.IsNullOrEmpty(this.ClientStreamReader.ReadLine()))
			{
			}
			

			//tell the client that a tunnel has been established
			var connectStreamWriter = new StreamWriter(this.ClientStream);
			connectStreamWriter.WriteLine($"HTTP/{this.Version.ToString(2)} 200 Connection established");
			connectStreamWriter.WriteLine
			 ($"Timestamp: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
			connectStreamWriter.WriteLine("Proxy-agent: http-logger.net");
			connectStreamWriter.WriteLine();
			connectStreamWriter.Flush();

			//now-create an https "server"
			this.SslStream = new SslStream(this.ClientStream, false);
			this.SslStream.AuthenticateAsServer(cert,
			 false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);

			//HTTPS server created - we can now decrypt the client's traffic
			this.ClientStreamReader = new StreamReader(this.SslStream);

			//read the new http command.
			var httpCommand = this.ClientStreamReader.ReadLine();

			// if 
			if (string.IsNullOrEmpty(httpCommand))
			{
				Logger.Warn("Data header of an https proxy request was null or empty.");

				this.ClientStreamReader.Close();
				this.ClientStream.Close();
				this.SslStream.Close();
				return false;
			}

			var httpCommandSplit = httpCommand.Split(' ');
			this.Method = httpCommandSplit[0];
			this.RemoteUri += httpCommandSplit[1];
			return true;
		}

		private void ReadRequestHeaders(StreamReader sr)
		{
			string httpCmd;
			this.ContentLength = 0;

			do
			{
				httpCmd = sr.ReadLine();
				if (string.IsNullOrEmpty(httpCmd))
				{
					return;
				}
				var header = httpCmd.Split(new[] { ": " }, 2, StringSplitOptions.None);
				switch (header[0].ToLower())
				{
					case "host":
						this.HttpRequest.Host = header[1];
						break;
					case "user-agent":
						this.HttpRequest.UserAgent = header[1];
						break;
					case "accept":
						this.HttpRequest.Accept = header[1];
						break;
					case "referer":
						this.HttpRequest.Referer = header[1];
						break;
					case "cookie":
						this.HttpRequest.Headers["Cookie"] = header[1];
						break;
					case "proxy-connection":
					case "connection":
					case "keep-alive":
						//ignore
						break;
					case "content-length":
						int contentLength;
						int.TryParse(header[1], out contentLength);
						this.ContentLength = contentLength;
						break;
					case "content-type":
						this.HttpRequest.ContentType = header[1];
						break;
					case "if-modified-since":
						var sb = header[1].Trim().Split(';');
						DateTime d;
						if (DateTime.TryParse(sb[0], out d))
							this.HttpRequest.IfModifiedSince = d;
						break;
					case "expect":
						this.HttpRequest.Expect = header[1];
						break;
					default:
						try
						{
							this.HttpRequest.Headers.Add(header[0], header[1]);
						}
						catch (Exception ex)
						{
							this.Logger.Error($"Could not add header {header[0]}, value: {header[1]}.  Exception message:{ex.Message}");
						}
						break;
				}
			} while (!string.IsNullOrWhiteSpace(httpCmd));
			
		}
	}
}
