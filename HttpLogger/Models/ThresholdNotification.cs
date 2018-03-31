using System;

namespace HttpLogger.Models
{
    /// <summary>
    /// Defines a <see cref="ThresholdNotification"/> object to encapsulate a notification of being over or under the threshold.
    /// </summary>
    public class ThresholdNotification
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not this network requests over the defined threshold
        /// </summary>
        public bool IsOverThreshold { get; set; }

        /// <summary>
        /// Gets or sets a count on the amount of request within the timeframe.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// Gets or sets a DateTime to detail when this notification happened.
        /// </summary>
        public DateTime NotificationDateTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is new.
        /// </summary>
        public bool  IsNotificationNew { get; set; }

        /// <summary>
        /// Gets a formated string of the notification.
        /// </summary>
        public string Notification => IsOverThreshold
            ? $"| High traffic generated an alert - hits = {this.RequestCount}, triggered at {this.NotificationDateTime} |"
            : $"| Traffic throughput is not over the threshold, hits = {this.RequestCount}, triggered at {this.NotificationDateTime} |";
    }
}
