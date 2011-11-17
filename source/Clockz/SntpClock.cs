using System;

namespace Clockz
{
    public class SntpClock : BaseClock
    {
        readonly InternetTime.SNTPClient Client;
        readonly string Host;

        public SntpClock(string host)
        {
            Host = host;
            Client = new InternetTime.SNTPClient(host);
        }

        public override DateTime UtcNow
        {
            get
            {
                Client.Connect(false);
                TimeSpan span = (Client.ReceiveTimestamp - Client.OriginateTimestamp) + (Client.TransmitTimestamp - Client.DestinationTimestamp);
                return DateTime.Now.AddMilliseconds(span.TotalMilliseconds / 2).ToUniversalTime();
            }
        }

        public override string ToString()
        {
            return string.Format("SntpClock(host:{0})", Host);
        }
    }
}
