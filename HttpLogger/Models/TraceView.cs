using System.Collections.Generic;

namespace HttpLogger.Models
{
    /// <summary>
    /// Defines a <see cref="TraceView"/> a model to define the values to be displayed within the console.
    /// </summary>
    public class TraceView
    {        
        /// <summary>
        /// Gets or sets the current <see cref="HttpTrace"/> object.
        /// </summary>
        public HttpTrace CurrentTrace { get; set; }

        /// <summary>
        /// Gets or set a string of the most requested host.
        /// </summary>
        public string MostRequestedHost { get; set; }

        /// <summary>
        /// Gets or sets a list of <see cref="HttpTrace"/> objects assocaited with the <see cref="MostRequestedHost"/>.
        /// </summary>
        public List<HttpTrace> MostRequestedHostTraces { get; set; }

        /// <summary>
        /// Gets or sets the current <see cref="ThresholdNotification"/>.
        /// </summary>
        public ThresholdNotification CurrentNotifaction { get; set; }
        
        /// <summary>
        /// Gets or set a stack of <see cref="ThresholdNotification"/> history.
        /// </summary>
        public Stack<ThresholdNotification> NotificationHistory { get; set; }
        
        /// <summary>
        /// Gets or sets a value that indicates the percentage of requests were made to <see cref="MostRequestedHost"/>.
        /// </summary>
        public string MostRequestedPercentage { get; set; }
    }
}
