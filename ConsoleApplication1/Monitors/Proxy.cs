using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading;
using HttpLogger.Providers;
using Microsoft.Win32;
using NLog;
using Org.BouncyCastle.Crypto;

namespace HttpLogger.Monitors
{
    /// <summary>
    /// Defines the Proxy class which is used as a man in the middle approach to listen to HTTP traffic on the current machine.
    /// </summary>
    public class Proxy : IMonitor
    {
        
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private static volatile AsymmetricKeyParameter _issuerKey;

        private static readonly string DefaultAddress = ConfigurationManager.AppSettings["ProxyIPAddress"] ?? string.Empty;
        private static readonly int DefaultPort = int.Parse(ConfigurationManager.AppSettings["ProxyPort"]);

	    
        /// <summary>
        /// Creates a new instance of a <see cref="Proxy"/> server, with the default IP address and port defined within the app settings.
        /// 
        /// App Setting keys are defined as:
        /// "ProxyIPAddress" for the default IP Address
        /// "ProxyPort" for the default port number
        /// </summary>
        public Proxy() : this(DefaultAddress, DefaultPort, LogManager.GetCurrentClassLogger())
        {
	        
        }

	    /// <summary>
	    /// Creates a new instance of a <see cref="Proxy"/> server, with the provided IP addrress and port.
	    /// </summary>
	    /// <param name="address">The IP Address the proxy server will be listening on.</param>
	    /// <param name="port">The port number the proxy server will be listening on.</param>
	    /// <param name="logger"></param>
	    public Proxy(string address, int port, ILogger logger)
		{
			Logger = logger;
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
		private static ILogger Logger { get; set; }

        /// <summary>
        /// Gets the port being used by the current instance of <see cref="Proxy"/>.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the Server Address being used by the current instance of of <see cref="Proxy"/>.
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

            // Generate CA Cert
            _issuerKey = CertificateProvider.GenerateCACertificate();

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
        /// <param name="obj"></param>
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
		/// 
		/// </summary>
		/// <param name="obj"></param>
        private static void ProcessClient(object obj)
        {
            var client = (TcpClient)obj;
            try
            {
                //read the first line HTTP command
                var proxyRequest = new ProxyRequest(client.GetStream(), _issuerKey);

	            if (!proxyRequest.SuccessfulInitializaiton)
	            {
		            return;
	            }

				proxyRequest.Process();

                // handle response
                var proxyResponse = new ProxyResponse();
				proxyResponse.Process(proxyRequest);
	            
            }
            catch (Exception ex)
            {
                //handle exception
				Logger.Error(ex);
            }
            finally
            {
                client.Close();
            }
        }
		
    }
}
