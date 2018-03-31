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
        private static IUIService _gui;
	    private static ServiceProvider _container;
        private static readonly ThreadObject _threshold = new ThreadObject { ThreadStartObject = 75 };
        
        private static void Main(string[] args)
        {

            ConfigureDependencies();

            Console.CancelKeyPress += Console_CancelKeyPress;

            Console.WriteLine("\n ----==============----");
            Console.WriteLine(" Welcome to HTTP Logger");
            Console.WriteLine(" ----==============----");
            Console.WriteLine("\n Choose which type of HTTP monitoring you would like to use.\n");
                        
            while (true)
			{
				Console.WriteLine(" 1. Proxy Server Monitoring [Allows HTTPS] - Recommended");
			    Console.WriteLine(" 2. Raw Socket Monitoring [HTTP Only]");
                var key = Console.ReadKey(true);

				switch (key.KeyChar)
				{
					case '1':
                        _monitor = _container.GetService<IProxyServerMonitor>();
						_monitor.Start();
						break;
				    case '2':
				        _monitor = _container.GetService<ISocketMonitor>();
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

            _gui = _container.GetService<IUIService>();
            _gui.Render();
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
            serviceCollection.AddSingleton<IProxyServerMonitor, ProxyServerMonitor>();
            serviceCollection.AddSingleton<ISocketMonitor, SocketMonitor>();
            serviceCollection.AddSingleton<IUIService, UIService>();

            _container = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Event Handler for when the Console is closed through CTRL+C or CTRL+Break
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, EventArgs e)
        {
            
            _gui.DisplayUI = false;            

            _monitor?.Stop();
            _tracerService?.Dispose();

            Environment.Exit(0);
            
        }
	}
}
