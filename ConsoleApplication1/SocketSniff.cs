using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpLogger
{
    public class SocketSniff : IMonitor
    {
        byte[] _byteData = new byte[65536];
        Socket _socket;
        private bool _open;
	    private bool _monitor;
        public void Start()
        {
            Console.WriteLine("\nEnter IP Address of Local Machine");
            var ipAddress = Console.ReadLine();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Debug, true);
            _socket.Bind(new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(ipAddress) ? "10.0.0.70" : ipAddress), 0));

            var byTrue = new byte[] { 1, 0, 0, 0 };


            _socket.IOControl(IOControlCode.ReceiveAll, byTrue, null);

            while (_monitor)
            {
                if (_open)
                {
                    continue;
                }

                _open = true;
                _socket.BeginReceive(_byteData, 0, _byteData.Length, SocketFlags.None, Callback, null);
            }

	        _socket.Close();
		}

	    public void Stop()
	    {
		    _monitor = false;
	    }

        private void Callback(IAsyncResult ar)
        {
            ReadBytes(_byteData, _byteData.Length);
            _open = ar.IsCompleted;

        }

        private void ReadBytes(byte[] bytes, int length)
        {
            byte[] data;

            using (var memoryStream = new MemoryStream(bytes, 0, length))
            using (var binaryReader = new BinaryReader(memoryStream))
            {

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

                // Next 32 bits have destination IP
                var destinationIPAddress = (uint)binaryReader.ReadInt32();
                var dataSize = totalLength - headerLength;
                data = new byte[dataSize];

                Array.Copy(bytes, headerLength, data, 0, dataSize);

                switch (protocol)
                {
                    case 6:
                        ReadTCP(data, dataSize);
                        break;
                    case 17:
                        ReadUDP(data, dataSize);
                        break;
                }
            }
        }

        private void ReadTCP(byte[] bytes, int length)
        {
            var client = new TcpClient();
            
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

                if (destinationPort != 80)
                {
                    return;
                }

                Console.WriteLine(Encoding.ASCII.GetString(bytes, dataOffsetLength, bytes.Length - dataOffsetLength));

            }
        }

        private void ReadUDP(byte[] bytes, int length)
        {
            using (var dataStream = new MemoryStream(bytes, 0, length))
            using (var dataReader = new BinaryReader(dataStream))
            {
                var sourcePort = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var destinationPort = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var dataLength = (ushort)IPAddress.NetworkToHostOrder(dataReader.ReadInt16());
                var checksum = IPAddress.NetworkToHostOrder(dataReader.ReadInt16());

                // Console.WriteLine(Encoding.ASCII.GetString(bytes, 8, length - 8));

            }
        }
    }
}
