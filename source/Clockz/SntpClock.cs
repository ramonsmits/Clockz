using System;
using System.Configuration;

namespace Clockz
{
    public class SntpClock : BaseClock
    {
        private static readonly string DefaultNtpServer = ConfigurationManager.AppSettings["NtpServer"] ?? "pool.ntp.org";
        readonly InternetTime.SntpClient Client;
        readonly string Host;

        public SntpClock() : this(DefaultNtpServer)
        {
        }

        public SntpClock(string host, bool jit = false)
        {
            Host = host;
            Client = new InternetTime.SntpClient(host);
            if (jit)
            {
                var warmup = UtcNow;
            }
        }

        public override DateTime UtcNow
        {
            get
            {
                Client.Connect();

                TimeSpan span = (Client.ReceiveTimestamp - Client.OriginateTimestamp) + (Client.TransmitTimestamp - Client.DestinationTimestamp);
                return DateTime.UtcNow.AddMilliseconds(span.TotalMilliseconds / 2);
            }
        }

        public override string ToString()
        {
            return string.Format("SntpClock(host:{0})", Host);
        }
    }
}
