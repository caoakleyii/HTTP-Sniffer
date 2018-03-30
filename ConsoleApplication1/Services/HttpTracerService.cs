using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HttpLogger.Models;
using HttpLogger.Repositories;
using Timer = System.Timers.Timer;

namespace HttpLogger.Services
{
    /// <summary>
    /// Defines the <see cref="HttpTracerService"/> class which is used to handle operations for logging and tracing HTTP calls.
    /// </summary>
    public class HttpTracerService : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="IHttpTraceRepository"/> service implemenation, used for handling database operations regarding the trace logs.
        /// </summary>
		private IHttpTraceRepository HttpTraceRepository { get; }

        /// <summary>
        /// Gets or sets the <see cref="System.Timers.Timer"/> object.
        /// Used to handle monitoring the most active requests and displaying it periodically.
        /// </summary>
        private Timer ActiveRequestTimer { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="HttpTracerService"/>
        /// </summary>
        /// <param name="traceRepository"></param>
	    public HttpTracerService(IHttpTraceRepository traceRepository)
	    {
		    this.HttpTraceRepository = traceRepository;
	    }
        
        /// <summary>
        /// Logs and saves an HTTP <see cref="ProxyRequest"/>
        /// </summary>
        /// <param name="request">The <see cref="ProxyRequest"/> to be traced, and logged.</param>
	    public void TraceProxyRequest(ProxyRequest request)
	    {
		    var httpTrace = new HttpTrace
		    {
			    ClientIPAddress = request.IPAddress,
			    ContentSize = request.ContentLength,
			    HttpCommand = $"{request.Method} {request.RemoteUri} HTTP/{request.HttpVersion.ToString(2)}",
				RemoteUri = new Uri(request.RemoteUri),
			    RequestDate = request.RequestDateTime,
			    StatusCode = request.StatusCode
		    };

			this.HttpTraceRepository.CreateTrace(httpTrace);
			this.HttpTraceRepository.SaveChanges();
	    }

        /// <summary>
        /// Monitors the most active requests during the lifecycle of the application.
        /// </summary>
        public void MonitorMostActiveRequest()
        {
            this.ActiveRequestTimer = new Timer(10 * 1000);
            this.ActiveRequestTimer.Elapsed += ActiveRequestTimer_Elapsed;
            this.ActiveRequestTimer.Enabled = true;
        }

        public void MonitorHighTraffic()
        {
            var thread = new Thread(CalculateTrafficThroughput);
            thread.Start();
        }

        private void CalculateTrafficThroughput()
        {
            try
            {
                
                while (true)
                {
                    var traces = this.HttpTraceRepository.ReadTraces();
                    var requests = 0;
                    for (var i = traces.Count - 1; i >= 0; i--)
                    {
                        if (!(traces[i] is HttpTrace trace))
                            continue;

                        if (trace.RequestDate - DateTime.Now > TimeSpan.FromMinutes(2))
                        {
                            break;
                        }
                        requests++;
                    }

                    if (requests > 25)
                    {
                        Console.Write($"High traffic generated an alert - hits = {requests}, triggered at {DateTime.Now}");
                    }
                    else
                    {
                        Console.Write("Traffic throughput is no longer over the ");
                    }
                }
            }
            catch(ThreadAbortException) { }
        }

        /// <summary>
        /// Event handler for the Active Request Timer.
        /// Determines the most actively requested website and displays the domain and the 
        /// sections requested for this domain.
        /// </summary>
        /// <param name="sender">Sender <see cref="object"/></param>
        /// <param name="e">The <see cref="EventArgs"/></param>
	    private void ActiveRequestTimer_Elapsed(object sender, EventArgs e)
	    {
            // retrieve all traces from our http trace repository
		    var traces = this.HttpTraceRepository.ReadTraces();
            
	        // define a data structure to contain and condense all websites visited by their DnsSafeHost domain name
            // within this data structure we have key/value pair that holds each http trace requests and maintain a count 
		    var websitesVisited = new Dictionary<string, Tuple<List<HttpTrace>, int>>();

            // The DnsSafeHost string of the most visited domain, so we can easily look it up through index.
	        // the count for evaluating 
            var mostVisitedDnsSafeHost = string.Empty;
		    var mostVisitedCount = 0; 

			foreach(var traceObj in traces.Values)
			{
			    if (!(traceObj is HttpTrace trace))
			        continue;

				var dnsSafeHost = trace.RemoteUri.DnsSafeHost;
			    var count = 0;

                // if this is our first record of this website, create a new key/value pair
			    if (!websitesVisited.ContainsKey(dnsSafeHost))
			    {
                    websitesVisited.Add(dnsSafeHost, new Tuple<List<HttpTrace>, int>(new[] {trace}.ToList(), count));
			    }
			    else
			    {
                    // else update the existing
			        var listOfTraces = websitesVisited[dnsSafeHost].Item1;

			        count = websitesVisited[dnsSafeHost].Item2;
			        listOfTraces.Add(trace);

                    websitesVisited[dnsSafeHost] = new Tuple<List<HttpTrace>, int>(listOfTraces, ++count);
			    }

			    if (count <= mostVisitedCount)
				{
					return;
				}

				mostVisitedCount = count;
				mostVisitedDnsSafeHost = dnsSafeHost;
			}

            if (string.IsNullOrWhiteSpace(mostVisitedDnsSafeHost))
            {
                return;
            }

			Console.WriteLine($" The most actively requested site is: {mostVisitedDnsSafeHost}");
            Console.WriteLine(" -----------------------------------------------------------");
	        foreach (var trace in websitesVisited[mostVisitedDnsSafeHost].Item1)
	        {
	            var section = trace.RemoteUri.Segments.Length > 1
	                ? $"{trace.RemoteUri.Segments[0]}{trace.RemoteUri.Segments[1]}"
	                : trace.RemoteUri.Segments[0];

	            Console.WriteLine($"{mostVisitedDnsSafeHost}{section}");
	        }
            
	    }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
