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
    public class HttpTracerService : IHttpTracerService
    {
        private readonly object _lock = new object();

        private IHttpTraceRepository _httpTraceRepository;
        
        /// <summary>
        /// Gets the <see cref="IHttpTraceRepository"/> service implemenation, used for handling database operations regarding the trace logs.
        /// </summary>
        public IHttpTraceRepository HttpTraceRepository
        {
            get
            {
                lock (_lock)
                {
                    return _httpTraceRepository;
                }
            }
            set
            {
                lock (_lock)
                {
                    _httpTraceRepository = value;
                }
            }
        }
        
        public IUIService GUI { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Timers.Timer"/> object.
        /// Used to handle monitoring the most active requests and displaying it periodically.
        /// </summary>
        private Timer ActiveRequestTimer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Thread"/> object that is used to monitor 
        /// traffic volume.
        /// </summary>
        private Thread TrafficThread { get; set; }                

        /// <summary>
        /// Creates a new instance of <see cref="HttpTracerService"/>
        /// </summary>
        /// <param name="traceRepository">The HttpTraceRepository implementation</param>
        public HttpTracerService(IHttpTraceRepository traceRepository, IUIService gui)
        {
            this.HttpTraceRepository = traceRepository;
            this.GUI = gui;
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
			    StatusCode = request.StatusCode,
                Method = request.Method
		    };

			this.HttpTraceRepository.CreateTrace(httpTrace);
			this.HttpTraceRepository.SaveChanges();

	        this.GUI.TraceViewModel.CurrentTrace = httpTrace;
	    }

        /// <summary>
        /// Logs and saves an HTTP <see cref="SocketRequest"/>
        /// </summary>
        /// <param name="request">The <see cref="SocketRequest"/> to be traced, and logged.</param>
        public void TraceSocketRequest(SocketRequest request)
        {
            var httpTrace = new HttpTrace
            {
                ClientIPAddress = request.IPAddress,
                ContentSize = request.ContentLength,
                HttpCommand = request.HttpCommand,
                RemoteUri = request.RemoteUri,
                RequestDate = request.RequestDateTime,
                StatusCode = request.StatusCode,
                Method = request.Method
            };

            this.HttpTraceRepository.CreateTrace(httpTrace);
            this.HttpTraceRepository.SaveChanges();

            this.GUI.TraceViewModel.CurrentTrace = httpTrace;
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

        /// <summary>
        /// Monitors for high traffic past the provided threshold.
        /// </summary>
        public void MonitorHighTraffic(ThreadObject threshold)
        {
           this.TrafficThread = new Thread(CalculateTrafficThroughput);
           this.TrafficThread.Start(threshold);            
        }

        /// <summary>
        /// Calculate the traffic, to monitor for high volumes.
        /// </summary>
        private void CalculateTrafficThroughput(object objThreshold)
        {
            var threadRequest = (ThreadObject)objThreshold;
            var threshold = (int)threadRequest.ThreadStartObject;
            
            try
            {
                var overThreshold = false;
                this.GUI.TraceViewModel.NotificationHistory = new Stack<ThresholdNotification>();

                while (true)
                {
                    var traces = this.HttpTraceRepository.ReadTraces();
                    var requests = 0;
                    for (var i = traces.Count - 1; i >= 0; i--)
                    {
                        if (!(traces[i] is HttpTrace trace))
                            continue;

                        if (DateTime.Now - trace.RequestDate > TimeSpan.FromMinutes(2))
                        {
                            break;
                        }
                        requests++;
                    }

                    if (requests > threshold && !overThreshold)
                    {
                        overThreshold = true;
                        var newNotifiaction = new ThresholdNotification()
                        {
                            IsOverThreshold = true,
                            NotificationDateTime = DateTime.Now,
                            RequestCount = requests,
                            IsNotificationNew = true
                        };

                        if (this.GUI.TraceViewModel.CurrentNotifaction != null)
                        {
                            this.GUI.TraceViewModel.NotificationHistory.Push(this.GUI.TraceViewModel.CurrentNotifaction);
                        }
                        this.GUI.TraceViewModel.CurrentNotifaction = newNotifiaction;
                        
                    }
                    else if (requests < threshold && overThreshold)
                    {
                        overThreshold = false;
                        var newNotifiaction = new ThresholdNotification()
                        {
                            IsOverThreshold = false,
                            NotificationDateTime = DateTime.Now,
                            RequestCount = requests,
                            IsNotificationNew = true
                        };

                        if (this.GUI.TraceViewModel.CurrentNotifaction != null)
                        {
                            this.GUI.TraceViewModel.NotificationHistory.Push(this.GUI.TraceViewModel.CurrentNotifaction);
                        }
                        this.GUI.TraceViewModel.CurrentNotifaction = newNotifiaction;
                        
                    }
                    else
                    {
                        if (this.GUI.TraceViewModel.CurrentNotifaction == null)
                        {
                            this.GUI.TraceViewModel.CurrentNotifaction = new ThresholdNotification();
                        }
                        this.GUI.TraceViewModel.CurrentNotifaction.RequestCount = requests;
                        this.GUI.TraceViewModel.CurrentNotifaction.NotificationDateTime = DateTime.Now;
                    }

                    threadRequest.ThreadCallback(true);
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
					continue;
				}

				mostVisitedCount = count;
				mostVisitedDnsSafeHost = dnsSafeHost;
			}

            if (string.IsNullOrWhiteSpace(mostVisitedDnsSafeHost))
            {
                return;
            }

            var requestPercentage = $"{((decimal)mostVisitedCount / (decimal)traces.Count * 100.0m).ToString("0.##")}%";

            this.GUI.TraceViewModel.MostRequestedPercentage = requestPercentage;
	        this.GUI.TraceViewModel.MostRequestedHost = mostVisitedDnsSafeHost;
	        this.GUI.TraceViewModel.MostRequestedHostTraces = websitesVisited[mostVisitedDnsSafeHost].Item1.ToList();
	    }

        /// <summary>
        /// Close and clean up resources being used by this instance of <see cref="HttpTracerService"/>
        /// </summary>
        public void Dispose()
        {
            if (this.TrafficThread == null || !this.TrafficThread.IsAlive) return;

            this.TrafficThread.Abort();
            this.TrafficThread.Join();
        }
    }
}
