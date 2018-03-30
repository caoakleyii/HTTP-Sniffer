using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLogger.Models
{
    public class TraceView
    {
        public HttpTrace CurrentTrace { get; set; }

        public string MostRequestedHost { get; set; }

        public List<HttpTrace> MostRequestedHostTraces { get; set; }

        public int TrafficVolume { get; set; }
    }
}
