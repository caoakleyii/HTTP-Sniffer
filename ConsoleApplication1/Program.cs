using System;
using HttpLogger.HttpMonitors;
using HttpLogger.Repositories;
using HttpLogger.Services;

namespace HttpLogger
{
	internal class Program
	{
        private static IMonitor _monitor;
	    private static HttpTracerService _tracerService;

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
				Console.WriteLine(" 2. Proxy Server Monitoring [Allows HTTPS]");
				var key = Console.ReadKey(true);

				switch (key.KeyChar)
				{
					case '1':
						_monitor = new SocketSniff();
						_monitor.Start();
						break;
					case '2':
						_monitor  = new ProxyServer();
						_monitor.Start();
						break;
					default:
						Console.WriteLine("\n Sorry, invalid option.");
						continue;
				}

				break;
			}

		    Console.WriteLine("\n Now Monitoring HTTP(S) Traffic");

		    _tracerService = new HttpTracerService(new HttpTraceRepository());
		    
		    _tracerService.MonitorMostActiveRequest();
		    _tracerService.MonitorHighTraffic();
            
		}

        /// <summary>
        /// Event Handler for when the Console is closed through CTRL+C or CTRL+Break
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, EventArgs e)
        {
	        _monitor?.Stop();
            _tracerService?.Dispose();
        }

    }
}
