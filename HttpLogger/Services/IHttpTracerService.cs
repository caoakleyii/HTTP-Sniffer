using System;
using HttpLogger.HttpMonitors;
using HttpLogger.Models;

namespace HttpLogger.Services
{
    public interface IHttpTracerService : IDisposable
    {
        /// <summary>
        /// Logs and saves an HTTP <see cref="ProxyRequest"/>
        /// </summary>
        /// <param name="request">The <see cref="ProxyRequest"/> to be traced, and logged.</param>
        void TraceProxyRequest(ProxyRequest request);

        /// <summary>
        /// Logs and saves an HTTP <see cref="SocketRequest"/>
        /// </summary>
        /// <param name="request">The <see cref="SocketRequest"/> to be traced, and logged.</param>
        void TraceSocketRequest(SocketRequest request);

        /// <summary>
        /// Monitors the most active requests during the lifecycle of the application.
        /// </summary>
        void MonitorMostActiveRequest();

        /// <summary>
        /// Monitors for high traffic past the provided threshold.
        /// </summary>
        void MonitorHighTraffic(ThreadObject threshold);
        
    }
}
