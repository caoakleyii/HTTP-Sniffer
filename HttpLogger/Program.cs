using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        private static IGUI _gui;
	    private static ServiceProvider _container;
        private static ThreadObject _threshold = new ThreadObject { ThreadStartObject = 20 };
        private const int STD_OUTPUT_HANDLE = -11;

        

        /// <summary>
        /// Get's an <see cref="IntPtr"/> to based on the handle id provided.
        /// </summary>
        /// <param name="nStdHandle"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int nStdHandle);
        
        /// <summary>
        /// Write's output to the console without needing to move the cursor or scroll.
        /// </summary>
        /// <param name="hConsoleOutput"></param>
        /// <param name="lpCharacter"></param>
        /// <param name="nLength"></param>
        /// <param name="dwWriteCoord"></param>
        /// <param name="lpNumberOfCharsWritten"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern bool WriteConsoleOutputCharacter(IntPtr hConsoleOutput,
           string lpCharacter, uint nLength, COORD dwWriteCoord,
           out uint lpNumberOfCharsWritten);

        

        private static void Main(string[] args)
        {

            ConfigureDependencies();

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
						_monitor = _container.GetService<ISocketSniff>();
                        _monitor.Start();
						break;
					case '2':
                        _monitor = _container.GetService<IProxyServer>();
						_monitor.Start();
						break;
					default:
						Console.WriteLine("\n Sorry, invalid option.");
						continue;
				}

				break;
			}

            _tracerService = _container.GetService<IHttpTracerService>();
		    
		    _tracerService.MonitorMostActiveRequest();
		    _tracerService.MonitorHighTraffic(_threshold);

            _gui = _container.GetService<IGUI>();
            DisplayUI();
        }

        /// <summary>
        /// Handle the rendering of the UI within the Console.
        /// </summary>
	    private static void DisplayUI()
	    {
            _gui.DisplayGUI = true;

            while (_gui.DisplayGUI)
	        {
               
                Console.SetCursorPosition(1, 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Close application with Ctrl+C or Ctrl+Break for proper clean up.");

                Console.SetCursorPosition(1, 2);
	            Console.ForegroundColor = ConsoleColor.White;
	            Console.Write($"Now Monitoring HTTP(S) Traffic...");

                var model = _gui.TraceViewModel;
                var httpTrace = model.CurrentTrace;

                // Display active monitoring of requests.
	            if (httpTrace != null)
	            {
	                Console.SetCursorPosition(1, 4);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
	                Console.Write(
	                    $"{httpTrace.ClientIPAddress} - - [{httpTrace.RequestDate:%d/%MMM/%yyyy:%H:%mm:%ss %z}] {httpTrace.Method} {httpTrace.RemoteUri.AbsolutePath} {httpTrace.StatusCode ?? "-"} {httpTrace.ContentSize}");
	            }

                // Display most hit requests and interesting facts.
                if (!string.IsNullOrWhiteSpace(model.MostRequestedHost))
                {
                    Console.SetCursorPosition(1, 10);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"The most actively requested site is: {model.MostRequestedHost}\n");
                    Console.Write($"{model.MostRequestedHost} makes up {model.MostRequestedPercentage} of your traffic for this current session.");

                    model.MostRequestedHostTraces?.Take(5).ToList().ForEach(trace =>
                    {
                        var section = trace.RemoteUri.Segments.Length > 1
                            ? $"{trace.RemoteUri.Segments[0]}{trace.RemoteUri.Segments[1]}"
                            : trace.RemoteUri.Segments[0];

                        Console.WriteLine($" {model.MostRequestedHost}{section}");
                    });
                }

                // Create Notification Box
                Console.SetCursorPosition(1, 17);
	            Console.ForegroundColor = ConsoleColor.White;
	            Console.Write($"{new string('-', 39)}Traffic Notification{new string('-', 39)}");
                Console.SetCursorPosition(1, 21);
	            Console.Write(new string('-', 98));

                // Display Traffic View Notificaitons.	            
                if (model.CurrentNotifaction != null)
                {
                    Console.SetCursorPosition(1, 19);
                    Console.ForegroundColor = model.CurrentNotifaction.IsOverThreshold ? ConsoleColor.Red : ConsoleColor.Green;
                    if (model.CurrentNotifaction.IsNotificationNew)
                    {
                        model.CurrentNotifaction.IsNotificationNew = false;
                        ClearLine();
                    }
                    Console.Write(model.CurrentNotifaction.Notification);
                }
	                            
	            Console.ForegroundColor = ConsoleColor.DarkGray;

                var i = 0;
                foreach (var notification in model.NotificationHistory)
                {
                    WriteConsoleOutputCharacter(GetStdHandle(STD_OUTPUT_HANDLE), notification.Notification, (uint)notification.Notification.Length, new COORD(1, (short)(22 + i)), out uint charsWritten);
                    i++;
                }
                

            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Closing app...");
        }

        /// <summary>
        /// Configure and register services and implementations.
        /// </summary>
        private  static void ConfigureDependencies()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IFileContext, FileContext>();
            serviceCollection.AddTransient<IHttpTracerService, HttpTracerService>();
            serviceCollection.AddTransient<IHttpTraceRepository, HttpTraceRepository>();
            serviceCollection.AddSingleton<IProxyServer, ProxyServer>();
            serviceCollection.AddSingleton<ISocketSniff, SocketSniff>();
            serviceCollection.AddSingleton<IGUI, GUI>();

            _container = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Helper method to clear a line in the console
        /// </summary>
        private static void ClearLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(1, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(1, currentLineCursor);
        }

        /// <summary>
        /// Event Handler for when the Console is closed through CTRL+C or CTRL+Break
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, EventArgs e)
        {
            
            _gui.DisplayGUI = false;            

            _monitor?.Stop();
            _tracerService?.Dispose();

            Environment.Exit(0);
            
        }

    }
}
