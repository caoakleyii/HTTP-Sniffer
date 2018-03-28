using System;

namespace HttpLogger
{
	class Program
	{

		static void Main(string[] args)
		{
			IMonitor monitor;

			Console.WriteLine("Welcome to HTTP Logger!");
			while (true)
			{
				Console.WriteLine("1. Raw Socket Logging");
				Console.WriteLine("2. Proxy Man In The Middle Logging");
				var key = Console.ReadKey();

				switch (key.KeyChar)
				{
					case '1':
						monitor = new SocketSniff();
						monitor.Start();
						break;
					case '2':
						monitor  = new Proxy();
						monitor.Start();
						break;
					default:
						Console.WriteLine("\nSorry, invalid option.");
						continue;
				}

				break;
			}
			

		}
		
	}
}
