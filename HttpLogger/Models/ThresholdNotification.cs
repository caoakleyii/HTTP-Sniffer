using System;

namespace HttpLogger.Models
{
    public class ThresholdNotification
    {
        public bool IsOverThreshold { get; set; }

        public int RequestCount { get; set; }

        public DateTime NotificationDateTime { get; set; }

        public bool  IsNotificationNew { get; set; }

        public string Notification
        {
            get
            {
                if (IsOverThreshold)
                {
                    return $"High traffic generated an alert - hits = {this.RequestCount}, triggered at {this.NotificationDateTime}";
                }
                else
                {
                    return $"Traffic throughput is no longer over the threshold, hits = { this.RequestCount}, triggered at { this.NotificationDateTime}";
                }
                
            }
        }
    }
}
