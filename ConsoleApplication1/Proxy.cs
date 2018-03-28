using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading;
using HttpLogger.Providers;
using Microsoft.Win32;
using Org.BouncyCastle.Crypto;

namespace HttpLogger
{
    /// <summary>
    /// Defines the Proxy class which is used as a man in the middle approach to listen to HTTP traffic on the current machine.
    /// </summary>
    public class Proxy : IMonitor
    {
        private TcpListener _listener;
        private Thread _listenerThread;

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private static volatile AsymmetricKeyParameter _issuerKey;

        private static string _defaultAddress = ConfigurationManager.AppSettings["ProxyIPAddress"] ?? string.Empty;
        private static int _defaultPort = int.Parse(ConfigurationManager.AppSettings["ProxyPort"]);

        private static readonly int _bufferSize = 8192;
        private static readonly Regex _cookieSplitRegEx = new Regex(@",(?! )", RegexOptions.Compiled);       

        /// <summary>
        /// Creates a new instance of a Proxy server, with the default IP address and port defined within the app settings.
        /// 
        /// App Setting keys are defined as:
        /// "ProxyIPAddress" for the default IP Address
        /// "ProxyPort" for the default port number
        /// </summary>
        public Proxy() : this(_defaultAddress, _defaultPort)
        {
            
        }

        /// <summary>
        /// Creates a new instance of a Proxy server, with the provided IP addrress and port.
        /// </summary>
        /// <param name="address">The IP Address the proxy server will be listening on.</param>
        /// <param name="port">The port number the proxy server will be listening on.</param>
        public Proxy(string address, int port)
        {
            this.ServerAddress = address;
            this.Port = port;            
        }

        /// <summary>
        /// Gets the port being used by the current instance of <see cref="Proxy"/>.
        /// </summary>
        public int Port
        {
            get;
        }

        /// <summary>
        /// Gets the Server Address being used by the current instance of of <see cref="Proxy"/>.
        /// </summary>
        public string ServerAddress
        {
            get;
        }        

        /// <summary>
        /// Starts the Proxy Server on a seperate thread and turns Windows proxy on.
        /// </summary>
		public void Start()
        {
			var ipEndpoint = new IPEndPoint(IPAddress.Parse(this.ServerAddress), this.Port);

            _listener = new TcpListener(ipEndpoint);
            _listenerThread = new Thread(Listen);

            // Generate CA Cert
            _issuerKey = CertificateProvider.GenerateCACertificate();

			// Turn Proxy On 
			this.SetProxy(true);			

			// Start Proxy
            _listenerThread.Start(_listener);

            Console.WriteLine($"\n Proxy Server listening at {this.ServerAddress}:{this.Port}.");
        }

        /// <summary>
        /// Stops the Proxy Server, removes the Windows proxy settings, and cleans up the thread.
        /// </summary>
        public void Stop()
        {
            // remove the proxy
            this.SetProxy(false);

            //stop listening for incoming connections
            _listener.Stop();

            //wait for server to finish processing current connections...
            _listenerThread.Abort();
            _listenerThread.Join();            
        }

        /// <summary>
        /// Sets the proxy server settings within Windows registry.
        /// </summary>
        /// <param name="enabled">Indicates whether to enable the proxy server settings.</param>
        private void SetProxy(bool enabled)
	    {
		    var reg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
		    reg.SetValue("ProxyServer", enabled ? $"{this.ServerAddress}:{this.Port}" : string.Empty);
		    reg.SetValue("ProxyEnable", enabled ? 1 : 0);

            // These lines implement the Interface in the beginning of program 
            // They cause the OS to refresh the settings, causing IP to relay update
            var internetOptionsSettingsChanged = 39;
            var internetOptionRefresh = 37;
            InternetSetOption(IntPtr.Zero, internetOptionsSettingsChanged, IntPtr.Zero, 0);
		    InternetSetOption(IntPtr.Zero, internetOptionRefresh, IntPtr.Zero, 0);
		}

        /// <summary>
        /// The start of the new thread. Proxy server listenings for requests from the client.
        /// </summary>
        /// <param name="obj"></param>
		private static void Listen(Object obj)
        {
            TcpListener listener = (TcpListener)obj;
            try
            {
                while (true)
                {
                    listener.Start();
                    TcpClient client = listener.AcceptTcpClient();
                    while (!ThreadPool.QueueUserWorkItem
                (new WaitCallback(Proxy.ProcessClient), client)) ;
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
        }

        private static void ProcessClient(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            try
            {
                //read the first line HTTP command
                Stream clientStream = client.GetStream();
                Stream responseStream = clientStream;
                var clientRequestStreamReader = new StreamReader(clientStream);
                var httpCmd = clientRequestStreamReader.ReadLine();

                //break up the line into three components
                var splitBuffer = httpCmd.Split(' ');
                var method = splitBuffer[0];
                var remoteUri = splitBuffer[1];
                var version = new Version(1, 0); //force everything to HTTP/1.0

                //this will be the web request issued on behalf of the client
                HttpWebRequest webReq;

                if (method == "CONNECT")
                {
                    var cert = CertificateProvider.GetSelfSignedCertificate(new Uri($"https://{remoteUri}"), _issuerKey);

                    // Browser wants to create a secure tunnel
                    // instead = we are going to perform a man in the middle                    
                    remoteUri = "https://" + remoteUri;

                    

                    //read and ignore headers
                    while (!String.IsNullOrEmpty(clientRequestStreamReader.ReadLine())) ;

                    //tell the client that a tunnel has been established
                    StreamWriter connectStreamWriter = new StreamWriter(clientStream);
                    connectStreamWriter.WriteLine("HTTP/1.0 200 Connection established");
                    connectStreamWriter.WriteLine
                     (String.Format("Timestamp: {0}", DateTime.Now.ToString()));
                    connectStreamWriter.WriteLine("Proxy-agent: http-logger.net");
                    connectStreamWriter.WriteLine();
                    connectStreamWriter.Flush();

                    //now-create an https "server"
                    var sslStream = new SslStream(clientStream, false);
                    sslStream.AuthenticateAsServer(cert,
                     false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);

                    //HTTPS server created - we can now decrypt the client's traffic
                    clientStream = sslStream;
                    clientRequestStreamReader = new StreamReader(sslStream);
                    responseStream = sslStream;

                    //read the new http command.
                    httpCmd = clientRequestStreamReader.ReadLine();
                    if (String.IsNullOrEmpty(httpCmd))
                    {
                        clientRequestStreamReader.Close();
                        clientStream.Close();
                        sslStream.Close();
                        return;
                    }
                    splitBuffer = httpCmd.Split(' ');
                    method = splitBuffer[0];
                    remoteUri = remoteUri + splitBuffer[1];
                }

                // create the web request, we are issuing on behalf of the client.
                webReq = (HttpWebRequest)HttpWebRequest.Create(remoteUri);
                webReq.Method = method;
                webReq.ProtocolVersion = version;

                //read the request headers from the client and copy them to our request
                int contentLen = ReadRequestHeaders(clientRequestStreamReader, webReq);

                webReq.Proxy = null;
                webReq.KeepAlive = false;
                webReq.AllowAutoRedirect = false;
                webReq.AutomaticDecompression = DecompressionMethods.None;

                // handle response
                GetResponse(webReq, responseStream, method, contentLen, clientRequestStreamReader);
            }
            catch (Exception ex)
            {
                //handle exception
            }
            finally
            {
                client.Close();
            }
        }

        private static void GetResponse(HttpWebRequest webReq, Stream outStream, string method, int contentLen, StreamReader clientRequestStreamReader)
        {

            if (method.ToUpper() == "POST")
            {
                char[] postBuffer = new char[contentLen];
                int bytesRead;
                int totalBytesRead = 0;
                StreamWriter sw = new StreamWriter(webReq.GetRequestStream());
                while (totalBytesRead < contentLen && (bytesRead = clientRequestStreamReader.ReadBlock(postBuffer, 0, contentLen)) > 0)
                {
                    totalBytesRead += bytesRead;
                    sw.Write(postBuffer, 0, bytesRead);
                    
                }
                

                sw.Close();
            }

            //Console.WriteLine(String.Format("ThreadID: {2} Requesting {0} on behalf of client {1}", webReq.RequestUri, client.Client.RemoteEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId));
            webReq.Timeout = 15000;
            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)webReq.GetResponse();
            }
            catch (WebException webEx)
            {
                response = webEx.Response as HttpWebResponse;
            }
            if (response != null)
            {
                List<Tuple<String, String>> responseHeaders = ReadResponseHeaders(response);
                StreamWriter myResponseWriter = new StreamWriter(outStream);
                Stream responseStream = response.GetResponseStream();
                try
                {
                    //send the response status and response headers
                    WriteResponseStatus(response.StatusCode, response.StatusDescription, myResponseWriter);
                    WriteResponseHeaders(myResponseWriter, responseHeaders);


                    Byte[] buffer;
                    if (response.ContentLength > 0)
                        buffer = new Byte[response.ContentLength];
                    else
                        buffer = new Byte[_bufferSize];

                    int bytesRead;

                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outStream.Write(buffer, 0, bytesRead);
                    }
                    responseStream.Close();
                    outStream.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    responseStream.Close();
                    response.Close();
                    myResponseWriter.Close();
                }
            }
        }

        private static void WriteResponseStatus(HttpStatusCode code, String description, StreamWriter myResponseWriter)
        {
            String s = String.Format("HTTP/1.0 {0} {1}", (Int32)code, description);
            myResponseWriter.WriteLine(s);
        }

        private static void WriteResponseHeaders(StreamWriter myResponseWriter, List<Tuple<String, String>> headers)
        {
            if (headers != null)
            {
                foreach (Tuple<String, String> header in headers)
                    myResponseWriter.WriteLine(String.Format("{0}: {1}", header.Item1, header.Item2));
            }
            myResponseWriter.WriteLine();
            myResponseWriter.Flush();
        }

        private static List<Tuple<String, String>> ReadResponseHeaders(HttpWebResponse response)
        {
            String value = null;
            String header = null;
            List<Tuple<String, String>> returnHeaders = new List<Tuple<String, String>>();
            foreach (String s in response.Headers.Keys)
            {
                if (s.ToLower() == "set-cookie")
                {
                    header = s;
                    value = response.Headers[s];
                }
                else
                    returnHeaders.Add(new Tuple<String, String>(s, response.Headers[s]));
            }

            if (!String.IsNullOrWhiteSpace(value))
            {
                response.Headers.Remove(header);
                String[] cookies = _cookieSplitRegEx.Split(value);
                foreach (String cookie in cookies)
                    returnHeaders.Add(new Tuple<String, String>("Set-Cookie", cookie));

            }
            returnHeaders.Add(new Tuple<String, String>("X-Proxied-By", "http-logger.net"));
            return returnHeaders;
        }

        private static int ReadRequestHeaders(StreamReader sr, HttpWebRequest webReq)
        {
            String httpCmd;
            int contentLen = 0;
            do
            {
                httpCmd = sr.ReadLine();
                if (String.IsNullOrEmpty(httpCmd))
                    return contentLen;
                String[] header = httpCmd.Split(new[] { ": " }, 2, StringSplitOptions.None);
                switch (header[0].ToLower())
                {
                    case "host":
                        webReq.Host = header[1];
                        break;
                    case "user-agent":
                        webReq.UserAgent = header[1];
                        break;
                    case "accept":
                        webReq.Accept = header[1];
                        break;
                    case "referer":
                        webReq.Referer = header[1];
                        break;
                    case "cookie":
                        webReq.Headers["Cookie"] = header[1];
                        break;
                    case "proxy-connection":
                    case "connection":
                    case "keep-alive":
                        //ignore these
                        break;
                    case "content-length":
                        int.TryParse(header[1], out contentLen);
                        break;
                    case "content-type":
                        webReq.ContentType = header[1];
                        break;
                    case "if-modified-since":
                        String[] sb = header[1].Trim().Split(new[] { ';' });
                        DateTime d;
                        if (DateTime.TryParse(sb[0], out d))
                            webReq.IfModifiedSince = d;
                        break;
                    default:
                        try
                        {
                            webReq.Headers.Add(header[0], header[1]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(String.Format("Could not add header {0}.  Exception message:{1}", header[0], ex.Message));
                        }
                        break;
                }
            } while (!String.IsNullOrWhiteSpace(httpCmd));
            return contentLen;
        }
    }
}
