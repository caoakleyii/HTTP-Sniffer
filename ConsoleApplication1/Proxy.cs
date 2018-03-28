using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HttpLogger
{
    public class Proxy : IMonitor
    {
        private TcpListener _listener;
        private Thread _listenerThread;
        private static X509Certificate _certificate;
	    private const string SERVER_ADDR = "127.0.0.1";
	    private const int PORT = 8080;

	    [DllImport("wininet.dll")]
	    public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
	    public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
	    public const int INTERNET_OPTION_REFRESH = 37;
		
	    bool settingsReturn, refreshReturn;

		public void Start()
        {
			var ipEndpoint = new IPEndPoint(IPAddress.Parse(SERVER_ADDR), PORT);

            _listener = new TcpListener(ipEndpoint);
            _listenerThread = new Thread(Listen);

			// Turn Proxy On 
			this.SetProxy();

			// Set Trusted Cert
            this.SetTrustedCert();

			// Start Proxy
            _listenerThread.Start(_listener);
        }

        public void Stop()
        {
            //stop listening for incoming connections
            _listener.Stop();
            //wait for server to finish processing current connections...
            _listenerThread.Abort();
            _listenerThread.Join();
        }

	    private void SetProxy()
	    {
		    var reg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
		    reg.SetValue("ProxyServer", $"{SERVER_ADDR}:{PORT}");
		    reg.SetValue("ProxyEnable", 1);

		    // These lines implement the Interface in the beginning of program 
		    // They cause the OS to refresh the settings, causing IP to realy update
		    settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
		    refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

		}

	    private void SetTrustedCert()
	    {
		    // retrieve cert
		    var filename = "cert.cer";
		    var directory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
		    var path = Path.Combine(directory, filename);
		    _certificate = X509Certificate.CreateFromCertFile(path);
		}

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
                StreamReader clientStreamReader = new StreamReader(clientStream);
                String httpCmd = clientStreamReader.ReadLine();

                //break up the line into three components
                String[] splitBuffer = httpCmd.Split(new[] { ' ' }, 3);
                String method = splitBuffer[0];
                String remoteUri = splitBuffer[1];
                Version version = new Version(1, 0); //force everything to HTTP/1.0

                //this will be the web request issued on behalf of the client
                HttpWebRequest webReq;

                if (method == "CONNECT")
                {
                    //Browser wants to create a secure tunnel
                    //instead = we are going to perform a man in the middle
                    //the user's browser should warn them of the certification errors however.
                    //Please note: THIS IS ONLY FOR TESTING PURPOSES - 
                    //you are responsible for the use of this code
                    //this is the URI we'll request on behalf of the client
                    remoteUri = "https://" + splitBuffer[1];
                    //read and ignore headers
                    while (!String.IsNullOrEmpty(clientStreamReader.ReadLine())) ;

                    //tell the client that a tunnel has been established
                    StreamWriter connectStreamWriter = new StreamWriter(clientStream);
                    connectStreamWriter.WriteLine("HTTP/1.0 200 Connection established");
                    connectStreamWriter.WriteLine
                     (String.Format("Timestamp: {0}", DateTime.Now.ToString()));
                    connectStreamWriter.WriteLine("Proxy-agent: http-sniffer.net");
                    connectStreamWriter.WriteLine();
                    connectStreamWriter.Flush();

                    //now-create an https "server"
                    var sslStream = new SslStream(clientStream, false);
                    sslStream.AuthenticateAsServer(_certificate,
                     false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);

                    //HTTPS server created - we can now decrypt the client's traffic
                    //....
                }
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
    }
}
