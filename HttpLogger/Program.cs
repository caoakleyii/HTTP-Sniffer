using System;
using System.Linq;
using System.Text;
using HttpLogger.Contexts;
using HttpLogger.HttpMonitors;
using HttpLogger.Models;
using HttpLogger.Repositories;
using HttpLogger.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLogger
{
	internal class Program
	{
        private static IMonitor _monitor;
	    private static IHttpTracerService _tracerService;
	    private static ServiceProvider _container;
        private static int _threshold = 20;

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
					    _monitor = new ProxyServer();
						_monitor.Start();
						break;
					default:
						Console.WriteLine("\n Sorry, invalid option.");
						continue;
				}

				break;
			}

            _tracerService = new HttpTracerService();
		    
		    _tracerService.MonitorMostActiveRequest();
		    _tracerService.MonitorHighTraffic();
            
            DisplayUI();
		}

	    private static void DisplayUI()
	    {
	        var i = 60;
	        while (true)
	        {
                Console.SetCursorPosition(1,2);
	            Console.ForegroundColor = ConsoleColor.White;
	            Console.Write($" Now Monitoring HTTP(S) Traffic...");

                var httpTrace = GUI.Instance.TraceViewModel.CurrentTrace;

                // Display active monitoring of requests.
	            if (httpTrace != null)
	            {
	                Console.SetCursorPosition(1, 4);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
	                Console.Write(
	                    $"{httpTrace.ClientIPAddress} - - [{httpTrace.RequestDate:%d/%MMM/%yyyy:%H:%mm:%ss %z}] {httpTrace.Method} {httpTrace.RemoteUri.AbsolutePath} {httpTrace.StatusCode} {httpTrace.ContentSize}");
	            }

                // Create Notification Box
                Console.SetCursorPosition(0, 10);
	            Console.ForegroundColor = ConsoleColor.White;
	            Console.Write($"{new string('-', 39)}Traffic Notification{new string('-', 39)}");
                Console.SetCursorPosition(0, 14);
	            Console.Write(new string('-', 98));

                // Display Traffic View Notificaitons.
	            Console.SetCursorPosition(1, 11);
	            Console.ForegroundColor = GUI.Instance.TraceViewModel.TrafficVolume <= _threshold ? ConsoleColor.Green : ConsoleColor.DarkGray;
	            Console.Write($"Traffic throughput is no longer over the threshold, hits = {GUI.Instance.TraceViewModel.TrafficVolume}, triggered at {DateTime.Now}");

                Console.SetCursorPosition(1, 13);
	            Console.ForegroundColor = GUI.Instance.TraceViewModel.TrafficVolume > _threshold ? ConsoleColor.Red : ConsoleColor.DarkGray;
                Console.Write($"High traffic generated an alert - hits = {GUI.Instance.TraceViewModel.TrafficVolume}, triggered at {DateTime.Now}");
	            
	            

                // Display most hit requests and interesting facts.
	            if (!string.IsNullOrWhiteSpace(GUI.Instance.TraceViewModel.MostRequestedHost))
	            {
	                Console.SetCursorPosition(1, 17);
	                Console.ForegroundColor = ConsoleColor.Cyan;
	                Console.Write($"The most actively requested site is: {GUI.Instance.TraceViewModel.MostRequestedHost}");

	                GUI.Instance.TraceViewModel.MostRequestedHostTraces?.Take(5).ToList().ForEach(trace =>
	                {
	                    var section = trace.RemoteUri.Segments.Length > 1
	                        ? $"{trace.RemoteUri.Segments[0]}{trace.RemoteUri.Segments[1]}"
	                        : trace.RemoteUri.Segments[0];

	                    Console.WriteLine($" {GUI.Instance.TraceViewModel.MostRequestedHost}{section}");
	                });
	            }

	        }
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
