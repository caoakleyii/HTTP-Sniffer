using System;
using System.Linq;
using System.Runtime.InteropServices;
using HttpLogger.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLogger.Services
{
    /// <summary>
    /// Defines a simple Inversion of Control class to resolve dependencies between services and repositories.
    /// </summary>
    public class UIService : IUIService
    {
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


        /// <summary>
        /// Creates a new instance of a <see cref="UIService"/>. 
        /// </summary>
        public UIService()
        {
            this.TraceViewModel = new TraceView();
        }
        
        /// <summary>
        /// The <see cref="ServiceProvider"/> that's configured with a collection of dependencies and implementations.
        /// </summary>
        public TraceView TraceViewModel { get; }

        /// <summary>
        /// Gets or set a value indicating whether or not to display GUI
        /// </summary>
        public bool DisplayUI { get; set; }

        /// <summary>
        /// Handle the rendering of the UI within the Console.
        /// </summary>
	    public void Render()
        {
            this.DisplayUI = true;
            var lastTraceId = string.Empty;
            while (this.DisplayUI)
            {

                Console.SetCursorPosition(1, 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Close application with Ctrl+C or Ctrl+Break for proper clean up.");

                Console.SetCursorPosition(1, 2);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Now Monitoring HTTP(S) Traffic...");

                var model = this.TraceViewModel;
                var httpTrace = model.CurrentTrace;

                // Display active monitoring of requests.
                if (httpTrace != null)
                {
                    if (httpTrace.Id != lastTraceId)
                    {
                        // clear 
                        for (var x = 9; x > 3; x--)
                        {
                            Console.SetCursorPosition(1, x);
                            ClearLine();
                        }
                    }
                    else
                    {
                        Console.SetCursorPosition(1, 4);
                    }

                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write(
                        $"{httpTrace.ClientIPAddress} - - [{httpTrace.RequestDate:dd/MM/yyyy:H:mm:ss zzz}] {httpTrace.Method} {httpTrace.RemoteUri.AbsolutePath} {httpTrace.StatusCode ?? "-"} {httpTrace.ContentSize}");
                }

                // Display most hit requests and interesting facts.
                if (!String.IsNullOrWhiteSpace(model.MostRequestedHost))
                {
                    Console.SetCursorPosition(1, 10);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"The most actively requested site is: {model.MostRequestedHost}\n");
                    Console.Write($" {model.MostRequestedHost} makes up {model.MostRequestedPercentage} of your traffic for this current session. \n");

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
        /// Helper method to clear a line in the console
        /// </summary>
        private void ClearLine()
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(1, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(1, currentLineCursor);
        }
    }

}
