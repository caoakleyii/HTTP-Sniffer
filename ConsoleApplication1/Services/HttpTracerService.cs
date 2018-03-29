using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Models;
using HttpLogger.Repositories;

namespace HttpLogger.Services
{
    public class HttpTracerService
    {
		public IHttpTraceRepository HttpTraceRepository { get; set; }

	    public HttpTracerService(IHttpTraceRepository traceRepository)
	    {
		    this.HttpTraceRepository = traceRepository;
	    }

	    public void TraceProxyRequest(ProxyRequest request)
	    {
		    var httpTrace = new HttpTrace
		    {
			    ClientIPAddress = request.IPAddress,
			    ContentSize = request.ContentLength,
			    HttpCommand = $"{request.Method} {request.RemoteUri} HTTP/{request.Version.ToString(2)}",
				RemoteUri = new Uri(request.RemoteUri),
			    RequestDate = request.RequestDateTime,
			    StatusCode = request.StatusCode
		    };

			this.HttpTraceRepository.CreateTrace(httpTrace);
			this.HttpTraceRepository.SaveChanges();
	    }

	    public void DisplayMostActiveRequests()
	    {
		    var traces = this.HttpTraceRepository.ReadTraces();
		    var websitesVisited = new Dictionary<string, int>();
		    var mostVisitedDnsSafeHost = string.Empty;
		    var mostVisitedCount = 0;

			traces.Values.ToList().ForEach(trace =>
			{
				var dnsSafeHost = trace.RemoteUri.DnsSafeHost;

				var count = websitesVisited[dnsSafeHost];
			    websitesVisited[dnsSafeHost] = ++count;

				if (count <= mostVisitedCount)
				{
					return;
				}
				mostVisitedCount = count;
				mostVisitedDnsSafeHost = dnsSafeHost;
			});

			Console.WriteLine(mostVisitedDnsSafeHost);
	    }
	    
    }
}
