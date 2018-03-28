using System;
using HttpLogger.Monitors;

namespace HttpLogger
{
	internal class Program
	{
        private static IMonitor _monitor;	

        private static void Main(string[] args)
		{
            Console.CancelKeyPress += Console_CancelKeyPress;

            Console.WriteLine("\n ----==============----");
            Console.WriteLine(" Welcome to HTTP Logger");
            Console.WriteLine(" ----==============----");
            Console.WriteLine("\n Choose which type of HTTP monitoring you would like to use.");

			while (true)
			{
				Console.WriteLine(" 1. Raw Socket Monitoring");
				Console.WriteLine(" 2. Proxy Server Monitoring");
				var key = Console.ReadKey(true);

				switch (key.KeyChar)
				{
					case '1':
						_monitor = new SocketSniff();
						_monitor.Start();
						break;
					case '2':
						_monitor  = new Proxy();
						_monitor.Start();
						break;
					default:
						Console.WriteLine("\n Sorry, invalid option.");
						continue;
				}

				break;
			}

            Console.WriteLine("\n Now Monitoring HTTP Traffic");
		}

        /// <summary>
        /// Event Handler for when the Console is closed through CTRL+C or CTRL+Break
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, EventArgs e)
        {
	        _monitor?.Stop();
        }

    }
}
