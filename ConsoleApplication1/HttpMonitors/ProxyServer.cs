using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using HttpLogger.Repositories;
using HttpLogger.Services;
using Microsoft.Win32;
using NLog;
using Org.BouncyCastle.Crypto;

namespace HttpLogger.HttpMonitors
{
    /// <summary>
    /// Defines the <see cref="ProxyServer"/> class which is used as a man in the middle approach to listen to HTTP traffic on the current machine.
    /// </summary>
    public class ProxyServer : IMonitor
    {
        
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private static volatile AsymmetricKeyParameter _issuerKey;

        private static readonly string DefaultAddress = ConfigurationManager.AppSettings["ProxyIPAddress"] ?? string.Empty;
        private static readonly int DefaultPort = int.Parse(ConfigurationManager.AppSettings["ProxyPort"]);

	    
        /// <summary>
        /// Creates a new instance of a <see cref="ProxyServer"/> server, with the default IP address and port defined within the app settings.
        /// 
        /// App Setting keys are defined as:
        /// "ProxyIPAddress" for the default IP Address
        /// "ProxyPort" for the default port number
        /// </summary>
        public ProxyServer() : this(DefaultAddress, DefaultPort, LogManager.GetCurrentClassLogger())
        {
	        
        }

	    /// <summary>
	    /// Creates a new instance of a <see cref="ProxyServer"/> server, with the provided IP addrress and port.
	    /// </summary>
	    /// <param name="address">The IP Address the proxy server will be listening on.</param>
	    /// <param name="port">The port number the proxy server will be listening on.</param>
	    /// <param name="logger"></param>
	    public ProxyServer(string address, int port, ILogger logger)
		{
			NLogger = logger;
            this.ServerAddress = address;
            this.Port = port;            
        }

		/// <summary>
		/// Gets or sets the listener <see cref="Thread"/> associated with this instance of the proxy server.
		/// </summary>
	    private Thread ListenerThread { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="TcpListener"/> associated with this instance of the proxy server.
		/// </summary>
		private TcpListener Listener { get; set; }

		/// <summary>
		/// Gets the NLog <see cref="ILogger"/> instance to handle application level logging.
		/// </summary>
		private static ILogger NLogger { get; set; }

        /// <summary>
        /// Gets the port being used by the current instance of <see cref="ProxyServer"/>.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the Server Address being used by the current instance of of <see cref="ProxyServer"/>.
        /// </summary>
        public string ServerAddress { get; }

        /// <summary>
        /// Starts the Proxy Server on a seperate thread and turns Windows proxy on.
        /// </summary>
		public void Start()
        {
			var ipEndpoint = new IPEndPoint(IPAddress.Parse(this.ServerAddress), this.Port);

            Listener = new TcpListener(ipEndpoint);
            ListenerThread = new Thread(Listen);

            Console.WriteLine("\n Issuing a self-signed trusted cert to decrypt HTTPS traffic.");
            Console.WriteLine(" To monitor HTTPS traffic, this cert will need to be accepted and saved into your trusted store.");
            Console.WriteLine(" To do so, please accept the next prompt.");
            

            // Generate CA Cert
            _issuerKey = CertificateService.GenerateCACertificate();

            if (_issuerKey == null)
            {
                Console.WriteLine("\n Unable to generate a CA Certificate. Proxying HTTP traffic only.");
            }

			// Turn Proxy On 
	        var proxyEnabled = this.SetProxy(true);
	        if (!proxyEnabled)
	        {
				Console.WriteLine("\n Unable to set your proxy options. Please enable and use this server as your proxy within your Internet Properties LAN settings.");
	        }

			// Start Proxy
            ListenerThread.Start(Listener);

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
            Listener.Stop();

            //wait for server to finish processing current connections...
            ListenerThread.Abort();
            ListenerThread.Join();            
        }

        /// <summary>
        /// Sets the proxy server settings within Windows registry.
        /// </summary>
        /// <param name="enabled">Indicates whether to enable the proxy server settings.</param>
        private bool SetProxy(bool enabled)
	    {
		    var reg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
		    if (reg == null)
		    {
			    return false;
		    }

		    reg.SetValue("ProxyServer", enabled ? $"{this.ServerAddress}:{this.Port}" : string.Empty);
			reg.SetValue("ProxyEnable", enabled ? 1 : 0);
		    

		    // These lines implement the Interface in the beginning of program 
            // They cause the OS to refresh the settings, causing IP to relay update
            const int internetOptionsSettingsChanged = 39;
            const int internetOptionRefresh = 37;
            InternetSetOption(IntPtr.Zero, internetOptionsSettingsChanged, IntPtr.Zero, 0);
		    InternetSetOption(IntPtr.Zero, internetOptionRefresh, IntPtr.Zero, 0);

		    return true;
	    }

		/// <summary>
		/// The start of the new thread. Proxy server listenings for requests from the client.
		/// </summary>
		/// <param name="obj">The TcpClient connecting to the proxy server.</param>
		private static void Listen(object obj)
        {
            var listener = (TcpListener)obj;
            try
            {
                while (true)
                {
                    listener.Start();
                    var client = listener.AcceptTcpClient();
                    while (!ThreadPool.QueueUserWorkItem(ProcessClient, client))
                    {

                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
        }

		/// <summary>
		/// Handle and process the current Tcp Client connected to the client.
		/// Creates a proxy request and response for the client.
		/// </summary>
		/// <param name="obj">The TcpClient connecting to the proxy server.</param>
        private static void ProcessClient(object obj)
        {
            var client = (TcpClient)obj;
            try
            {
                //read the first line HTTP command
                using (var proxyService = new ProxyService(client, _issuerKey))
                using (var traceService = new HttpTracerService(new HttpTraceRepository()))
                {
                    // generate a proxy request
                    var request = proxyService.ProcessRequest();

                    if (!request.SuccessfulInitializaiton)
                    {
                        return;
                    }

                    // handle response
                    proxyService.ProcessResponse(request);

                    // trace this proxy request
                    traceService.TraceProxyRequest(request);
                }
                        
            }
            catch (Exception ex)
            {
                //handle exception
				NLogger.Error(ex);
            }
            finally
            {
                client.Close();
            }
        }
		
    }
}