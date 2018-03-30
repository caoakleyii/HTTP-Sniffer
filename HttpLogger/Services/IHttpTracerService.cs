using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Models;

namespace HttpLogger.Services
{
    public interface IHttpTracerService : IDisposable
    {
        void TraceProxyRequest(ProxyRequest request);

        void MonitorMostActiveRequest();

        void MonitorHighTraffic();
        
    }
}
