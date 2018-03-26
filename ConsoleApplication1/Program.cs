using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	class Program
	{
		static byte[] _byteData = new byte[32];
		static Socket _socket;
		private static bool _open = true;

		static void Main(string[] args)
		{
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

			_socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));

			_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

			var byTrue = new byte[] {1, 0, 0, 0};


			_socket.IOControl(IOControlCode.ReceiveAll, byTrue, _byteData);

			while (true)
			{
				if (!_open)
				{
					continue;
				}

				_open = false;
				_socket.BeginReceive(_byteData, 0, _byteData.Length, SocketFlags.None, Callback, null);
			}
		}

		private static void Callback(IAsyncResult ar)
		{
			ReadBytes(_byteData, _byteData.Length);
			Console.WriteLine($"Completed: {ar.IsCompleted}");
			_open = ar.IsCompleted;

		}

		private static void ReadBytes(byte[] bytes, int length)
		{
			var memoryStream = new MemoryStream(bytes, 0, length);
			var binaryReader = new BinaryReader(memoryStream);

			// First 8 bits  of the IP header contain the version and header length
			var bVersionAndHeaderLength = binaryReader.ReadByte();

			// Next 8 bits contain the differentiated services.
			var bDifferentiatedServices = binaryReader.ReadByte();

			// Next 16 bits hold the total length of the datagram
			var totalLength = binaryReader.ReadInt16();

			// Next 16 bits for identification
			var identification = binaryReader.ReadInt16();

			// Next 16 bits contain flags and fragmentation offset
			var flagsAndOffsets = binaryReader.ReadInt16();

			// Next 8 bits have the TTL value
			var ttl = binaryReader.ReadInt16();

			// Bext 8 bits have the protocol 


		}
		
	}
}
