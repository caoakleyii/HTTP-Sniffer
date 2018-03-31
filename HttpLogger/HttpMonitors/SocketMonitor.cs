using HttpLogger.Models;
using HttpLogger.Services;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HttpLogger.HttpMonitors
{
    /// <summary>
    /// Defines a <see cref="SocketMonitor"/> which monitors traffic on a socket directly.
    /// </summary>
    public class SocketMonitor : ISocketMonitor
    {
        private readonly byte[] _byteData = new byte[65536];
        
        private bool _open;
	    private bool _monitor;
        private string _localIPAddress;
        private readonly string[] _dataHeaderSplitter = { "\r\n" };

        /// <summary>
        /// Gets or sets the <see cref="IHttpTracerService"/> implementation
        /// </summary>
        public IHttpTracerService HttpTracerService { get; set; }

        /// <summary>
		/// Gets or sets the listener <see cref="Thread"/> associated with this instance of the proxy server.
		/// </summary>
	    private Thread ListenerThread { get; set; }

        /// <summary>
        /// The <see cref="Socket"/> we are lisetning on.
        /// </summary>
        private Socket Socket { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SocketMonitor"/> monitoring data on a socket directly.
        /// </summary>
        /// <param name="httpTracerService"></param>
        public SocketMonitor(IHttpTracerService httpTracerService)
        {
            this.HttpTracerService = httpTracerService;
        }

        /// <summary>
        /// Starts the an asynchronous Socket Sniffer.
        /// </summary>
        public void Start()
        {   
            // create dummy socket to find machines IP address.
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                    _localIPAddress = endPoint.Address.ToString();
            }

            this.ListenerThread = new Thread(Listen);

            Console.Clear();
            Console.CursorVisible = false;

            this.ListenerThread.Start();

        }

        /// <summary>
        /// Open the socket and listen.
        /// </summary>
        private void Listen()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(_localIPAddress) ? "127.0.0.1" : _localIPAddress), 0));

            Console.WriteLine($" Proxy Server listening at {_localIPAddress}.");

            var byTrue = new byte[] { 1, 0, 0, 0 };
            var byOut = new byte[] { 0, 0, 0, 0 };


            Socket.IOControl(IOControlCode.ReceiveAll, byTrue, byOut);

            _monitor = true;
            while (_monitor)
            {
                if (_open)
                {
                    continue;
                }

                _open = true;
                Socket.BeginReceive(_byteData, 0, _byteData.Length, SocketFlags.None, Callback, null);
            }
        }

        /// <summary>
        /// Handles the closure of the socket sniffer.
        /// </summary>
        public void Stop()
	    {
		    _monitor = false;

            this.Socket.Close();

            this.ListenerThread.Abort();
            this.ListenerThread.Join();
	    }

        /// <summary>
        /// Callback to handle when a request is received.
        /// </summary>
        /// <param name="ar"></param>
        private void Callback(IAsyncResult ar)
        {
            ReadBytes(_byteData, _byteData.Length);
            _open = !ar.IsCompleted;

        }

        /// <summary>
        /// Read bytes coming in on the socket, and if it's a TCP request handle it.
        /// </summary>
        /// <param name="bytes">Byte array containing data on the socket</param>
        /// <param name="length">Content length</param>
        private void ReadBytes(byte[] bytes, int length)
        {
            using (var memoryStream = new MemoryStream(bytes, 0, length))
            using (var binaryReader = new BinaryReader(memoryStream))
            {

                var request = new SocketRequest();

                // First 8 bits  of the IP header contain the version and header length
                var versionAndHeaderLength = binaryReader.ReadByte();
                var headerLength = (versionAndHeaderLength & 0x0F) * 4;
                var version = versionAndHeaderLength >> 4;

                // Next 8 bits contain the diffe-rentiated services.
                var bDifferentiatedServices = binaryReader.ReadByte();

                // Next 16 bits hold the total length of the datagram
                var totalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Next 16 bits for identification
                var identification = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Next 16 bits contain flags and fragmentation offset
                var flagsAndOffsets = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Next 8 bits have the TTL value
                var ttl = binaryReader.ReadByte();

                // Next 8 bits have the protocol 
                var protocol = binaryReader.ReadByte();

                // Next 16 bits contain the checksum of the header
                var checksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                // Next 32 bits have source IP
                var sourceIPAddress = (uint)binaryReader.ReadInt32();
                request.IPAddress = new IPAddress(sourceIPAddress);

                // Next 32 bits have destination IP
                var destinationIPAddress = (uint)binaryReader.ReadInt32();
                var dataSize = totalLength - headerLength;
                var data = new byte[dataSize];

                Array.Copy(bytes, headerLength, data, 0, dataSize);

                // currently only monitoring traffic over TCP, however
                // this could be extended to monitor UDP as well.
                if (protocol == 6)
                {
                    ReadTCP(data, dataSize, request);
                }
            }
        }

        /// <summary>
        /// Read TCP Data, and trace it if it's a HTTP Request.
        /// </summary>
        /// <param name="bytes">Byte array of the data</param>
        /// <param name="length">Content length</param>
        /// <param name="request">SocketRequest used to handle tracing.</param>
        private void ReadTCP(byte[] bytes, int length, SocketRequest request)
        {
            using (var dataStream = new MemoryStream(bytes, 0, length))
            using (var dataReader = new BinaryReader(dataStream))
            {
                var sourcePort = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var destinationPort = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var sequenceNumber = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt32());
                var ackFields = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt32());
                var dataOffsetAndFlag = dataReader.ReadByte();
                var dataOffsetLength = (dataOffsetAndFlag >> 4) * 4;
                var flags = dataReader.ReadByte();
                var windowSize = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var checksum = IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var urgentPointer = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());                
                var data = Encoding.ASCII.GetString(bytes, dataOffsetLength, bytes.Length - dataOffsetLength);

                if (!data.Contains("HTTP/1")) return;

                var dataArray = data.Split(_dataHeaderSplitter, StringSplitOptions.None);
                request.HttpCommand = dataArray[0];

                var httpCommandArray = request.HttpCommand.Split(' ');
                request.Method = httpCommandArray[0];

                var path = httpCommandArray[1];
                var host = dataArray[1].Split(' ')[1];

                request.RemoteUri = new Uri($"http://{host}{path}");
                request.RequestDateTime = DateTime.Now;
                request.ContentLength = length;

                // Trace and Log this request.
                this.HttpTracerService.TraceSocketRequest(request);

            }
        }
    }
}
