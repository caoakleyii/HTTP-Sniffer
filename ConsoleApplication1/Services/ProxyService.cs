using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using HttpLogger.Models;
using NLog;
using Org.BouncyCastle.Crypto;
using System.Net.Sockets;

namespace HttpLogger.Services
{
    /// <summary>
    /// Defines the <see cref="ProxyService"/> used to process request and responses for a proxy server.
    /// </summary>
	public class ProxyService : IDisposable
	{
        /// <summary>
        /// Compiled regex defining how to split the cookies in the clients request.
        /// </summary>
		private static readonly Regex CookieSplitRegEx = new Regex(@",(?! )", RegexOptions.Compiled);

        /// <summary>
        /// Constant defining the buffer size of the response if no content length is provided.
        /// </summary>
		private const int BUFFER_SIZE = 8192;

        /// <summary>
        /// Creates a new instance of the <see cref="ProxyService"/>
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> to be used when processing the requests and response.</param>
        /// <param name="issuerKey">The <see cref="AsymmetricKeyParameter"/> object defining the issuer key when handling TLS/SSL handshakes.</param>
		public ProxyService(TcpClient client, AsymmetricKeyParameter issuerKey)
		{
			this.TcpClient = client;
			this.ClientStream = client.GetStream();
			this.NLogger = LogManager.GetCurrentClassLogger();
			this.IssuerKey = issuerKey;
		}

        /// <summary>
        /// Gets or sets the client <see cref="Stream"/>.
        /// </summary>
	    public Stream ClientStream { get; }

        /// <summary>
        /// Gets or sets the client's <see cref="SslStream"/> if the connection is upgraded to TLS/SSL.
        /// </summary>
	    public SslStream SslStream { get; private set; }

        /// <summary>
        /// Gets or sets the client's <see cref="StreamReader"/>.
        /// </summary>
	    public StreamReader ClientStreamReader { get; private set; }

        /// <summary>
        /// Gets the <see cref="AsymmetricKeyParameter"/> object of the issuer key.
        /// </summary>
	    private AsymmetricKeyParameter IssuerKey { get; }

        /// <summary>
        /// Gets the current classes instance of <see cref="ILogger"/>.
        /// </summary>
	    private ILogger NLogger { get; }

        /// <summary>
        /// Gets or sets the <see cref="HttpWebRequest"/> object that 
        /// is used to make a request to the remote server on behalf of the client.
        /// </summary>
	    public HttpWebRequest HttpRequest { get; set; }

        /// <summary>
        /// Gets the <see cref="TcpClient"/>.
        /// </summary>
	    public TcpClient TcpClient { get; }

        /// <summary>
        /// After intercepting the request from the client, this starts the initialization of processing a request for the client.
        /// and handling an TLS/SSL handshake if required. Returning a populated <see cref="ProxyRequest"/> object defining the data needed to 
        /// process a request and response to the remote server.
        /// </summary>
        /// <returns>Returns a <see cref="ProxyRequest"/> object encapsulating the client's request.</returns>
        public ProxyRequest ProcessRequest()
		{
			var request = new ProxyRequest
			{
				IPAddress = ((IPEndPoint) this.TcpClient.Client.RemoteEndPoint).Address,
				RequestDateTime = DateTime.Now
			};

            // Initialize the request object. populating the data based on the http headers within the client stream
		    this.ClientStreamReader = new StreamReader(this.ClientStream);
		    var httpCommand = this.ClientStreamReader.ReadLine();

		    if (string.IsNullOrEmpty(httpCommand))
		    {
		        request.SuccessfulInitializaiton = false;
		        this.NLogger.Warn("Data header of a proxy request was null or empty.");
		        return request;
		    }

		    var httpCommandSplit = httpCommand.Split(' ');
		    request.Method = httpCommandSplit[0];
		    request.RemoteUri = httpCommandSplit[1];
		    request.HttpVersion = new Version(httpCommandSplit[2].Split('/')[1]);

		    request.SuccessfulInitializaiton = request.Method != "CONNECT" || this.SslHandshake(request);
            return request;
		}

        /// <summary>
        /// Process a request and response to the remote server on behalf of the client and write to the clients stream with the response from the server.
        /// </summary>
        /// <param name="request">The <see cref="ProxyRequest"/> to be handled.</param>
		public void ProcessResponse(ProxyRequest request)
		{
		    // create the web request, we are issuing on behalf of the client.
		    this.HttpRequest = (HttpWebRequest)WebRequest.Create(request.RemoteUri);
		    this.HttpRequest.Method = request.Method;
		    this.HttpRequest.ProtocolVersion = request.HttpVersion;

		    //read the request headers from the client and copy them to our request
		    this.ReadRequestHeaders(request);

		    this.HttpRequest.Proxy = null;
		    this.HttpRequest.KeepAlive = false;
		    this.HttpRequest.AllowAutoRedirect = false;
		    this.HttpRequest.AutomaticDecompression = DecompressionMethods.None;

            if (request.Method.ToUpper() == "POST")
			{
				var postBuffer = new char[request.ContentLength];
				int bytesRead;
				var totalBytesRead = 0;
				var sw = new StreamWriter(this.HttpRequest.GetRequestStream());

				while (totalBytesRead < request.ContentLength && (bytesRead = this.ClientStreamReader.ReadBlock(postBuffer, 0, request.ContentLength)) > 0)
				{
					totalBytesRead += bytesRead;
					sw.Write(postBuffer, 0, bytesRead);
				}

				sw.Close();
			}

			this.HttpRequest.Timeout = 15000;

		    if (!(this.HttpRequest.GetResponse() is HttpWebResponse response))
			{
				return;
			}

			var responseHeaders = ReadResponseHeaders(response);

			var outStream = request.IsHttps ? this.SslStream : this.ClientStream;

			var responseWriter = new StreamWriter(outStream);
			var responseStream = response.GetResponseStream();

			if (responseStream == null)
			{
				response.Close();
				responseWriter.Close();
				return;
			}

			try
			{
					
				//send the response status and response headers
				request.StatusCode = response.StatusCode;
				WriteResponseStatus(request, response.StatusCode, response.StatusDescription, responseWriter);
				WriteResponseHeaders(responseWriter, responseHeaders);


				var buffer = response.ContentLength > 0 ? new byte[response.ContentLength] : new byte[BUFFER_SIZE];

				int bytesRead;

				while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					outStream.Write(buffer, 0, bytesRead);
				}
				
				outStream.Flush();
			}
			catch (Win32Exception ex)
			{
				// connection was closed by browser/client not an issue.
				if (ex.NativeErrorCode != 10053)
					return;

				this.NLogger.Error(ex);
			}
			catch (Exception ex)
			{
				this.NLogger.Error(ex);
			}
			finally
			{
				responseStream.Close();
				response.Close();
				responseWriter.Close();
			}
		}

		#region Private Request Methods

        /// <summary>
        /// Handle the TLS/SSL hande upgrade for the request provided.
        /// </summary>
        /// <param name="request">
        /// The <see cref="ProxyRequest"/> object associated with the TLS/SSL handshake
        /// </param>
        /// <returns>Returns a <see cref="bool"/> value indicating whether or not the handshake was successful</returns>
		private bool SslHandshake(ProxyRequest request)
		{
			request.IsHttps = true;

			// Browser wants to create a secure tunnel
			// instead we are perform a man in the middle                    
			request.RemoteUri = "https://" + request.RemoteUri;

			var cert = CertificateService.GetSelfSignedCertificate(new Uri(request.RemoteUri), this.IssuerKey);

			//read and ignore headers
			while (!string.IsNullOrEmpty(this.ClientStreamReader.ReadLine()))
			{
			}
            
			//tell the client that a tunnel has been established
			var connectStreamWriter = new StreamWriter(this.ClientStream);
			connectStreamWriter.WriteLine($"HTTP/{request.HttpVersion.ToString(2)} 200 Connection established");
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
			
			if (string.IsNullOrEmpty(httpCommand))
			{
				
				this.ClientStreamReader.Close();
				this.ClientStream.Close();
				this.SslStream.Close();
				return false;
			}

			var httpCommandSplit = httpCommand.Split(' ');
			request.Method = httpCommandSplit[0];
			request.RemoteUri += httpCommandSplit[1];
			return true;
		}

        /// <summary>
        /// Reads the headers from the stream and updates <see cref="HttpRequest"/> object and <see cref="ProxyRequest"/> data model.
        /// </summary>
        /// <param name="request"></param>
		private void ReadRequestHeaders(ProxyRequest request)
		{
			string httpCmd;
			request.ContentLength = 0;

			do
			{
				httpCmd = this.ClientStreamReader.ReadLine();
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
					case "100-continue":
						//ignore
						break;
					case "content-length":
					    int.TryParse(header[1], out var contentLength);
						request.ContentLength = contentLength;
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
							this.NLogger.Error($"Could not add header {header[0]}, value: {header[1]}.  Exception message:{ex.Message}");
						}
						break;
				}
			} while (!string.IsNullOrWhiteSpace(httpCmd));

		}
        #endregion

        #region Private Response Methods

        /// <summary>
        /// Writes a response to the client with status code.
        /// </summary>
        /// <param name="request">The <see cref="ProxyRequest"/> associated with the client and response.</param>
        /// <param name="code">The <see cref="HttpStatusCode"/>. </param>
        /// <param name="description">The description of the status.</param>
        /// <param name="responseWriter">The response writer to the client.</param>
        private static void WriteResponseStatus(ProxyRequest request, HttpStatusCode code, string description, StreamWriter responseWriter)
		{
			var s = $"HTTP/{request.HttpVersion.ToString(2)} {(int)code} {description}";
			responseWriter.WriteLine(s);
		}

        /// <summary>
        /// Write response headers to the client
        /// </summary>
        /// <param name="responseWriter">The response wrtier to the client</param>
        /// <param name="headers">The headers to be written to the client.</param>
		private static void WriteResponseHeaders(StreamWriter responseWriter, List<Tuple<string, string>> headers)
		{
			if (headers != null)
			{
				foreach (Tuple<string, string> header in headers)
				    responseWriter.WriteLine($"{header.Item1}: {header.Item2}");
			}
		    responseWriter.WriteLine();
		    responseWriter.Flush();
		}

        /// <summary>
        /// Read response headers from the server.
        /// </summary>
        /// <param name="response">The <see cref="HttpWebResponse"/> from the remote server.</param>
        /// <returns>Returns a <see cref="List{T}"/> of headers.</returns>
		private static List<Tuple<string, string>> ReadResponseHeaders(HttpWebResponse response)
		{
			string value = null;
			string header = null;
			var returnHeaders = new List<Tuple<string, string>>();
			foreach (string s in response.Headers.Keys)
			{
				if (s.ToLower() == "set-cookie")
				{
					header = s;
					value = response.Headers[s];
				}
				else
					returnHeaders.Add(new Tuple<string, string>(s, response.Headers[s]));
			}

			if (!string.IsNullOrWhiteSpace(value))
			{
				response.Headers.Remove(header);
				var cookies = CookieSplitRegEx.Split(value);
				returnHeaders.AddRange(cookies.Select(cookie => new Tuple<string, string>("Set-Cookie", cookie)));
			}
			returnHeaders.Add(new Tuple<string, string>("X-Proxied-By", "http-logger.net"));
			return returnHeaders;
		}

		#endregion

	    public void Dispose()
	    {
	        this.TcpClient.Close();
            this.ClientStream.Close();
            this.ClientStreamReader.Close();
	    }
	}
}
