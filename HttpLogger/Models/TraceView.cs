using System.Collections.Generic;

namespace HttpLogger.Models
{
    public class TraceView
    {        
        public HttpTrace CurrentTrace { get; set; }

        public string MostRequestedHost { get; set; }

        public List<HttpTrace> MostRequestedHostTraces { get; set; }

        public ThresholdNotification CurrentNotifaction { get; set; }
        
        public Stack<ThresholdNotification> NotificationHistory { get; set; }

        public string MostRequestedPercentage { get; set; }
    }
}
