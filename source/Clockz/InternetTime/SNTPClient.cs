/*
 * A C# SNTP Client
 * 
 * Copyright (C)2001-2011 Valer BOCAN, PhD <valer@bocan.ro>
 * Last modified: November 20, 2011
 *  
 * Comments, bugs and suggestions are welcome.
 *
 * Update history:
 * 
 * November 20, 2011
 * - Added the SNTP_WindowsMobile compilation directive for discrimination between Windows Desktop and Windows Mobile
 * 
 * - Altered Connect() method to provide a socket timeout (Jason Garrett - jason.garrett@hotmail.com)
 *    - Credit goes to Kyle Jones who posted this improved Connect() method on
 *      http://objectmix.com/dotnet/98919-socket-receive-timeout-compact-framework.html
 *      on 10 Mar 2009.
 * - Added <summary> tags to class methods and attributes
 * 
 * May 2, 2011
 * - RoundTripDelay and LocalClockOffset now return a double instead of an integer to avoid overflows
 *   when the computer clock is way off.
 * - Added the DllImport directive for Windows Mobile 6.0
 *   Thanks to André Rippstein <andre@rippstein.net>
 * 
 * September 20, 2003
 * - Renamed the class from NTPClient to SNTPClient.
 * - Fixed the RoundTripDelay and LocalClockOffset properties.
 *   Thanks go to DNH <dnharris@csrlink.net>.
 * - Fixed the PollInterval property.
 *   Thanks go to Jim Hollenhorst <hollenho@attbi.com>.
 * - Changed the ReceptionTimestamp variable to DestinationTimestamp to follow the standard
 *   more closely.
 * - Precision property is now shown is seconds rather than milliseconds in the
 *   ToString method.
 * 
 * May 28, 2002
 * - Fixed a bug in the Precision property and the SetTime function.
 *   Thanks go to Jim Hollenhorst <hollenho@attbi.com>.
 * 
 * March 14, 2001
 * - First public release.
 */

// Uncomment the line below if compiling for Windows Mobile
//#define SNTP_WindowsMobile

namespace InternetTime
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Leap indicator field values
    /// </summary>
    public enum _LeapIndicator
    {
        NoWarning,		// 0 - No warning
        LastMinute61,	// 1 - Last minute has 61 seconds
        LastMinute59,	// 2 - Last minute has 59 seconds
        Alarm			// 3 - Alarm condition (clock not synchronized)
    }

    /// <summary>
    /// Mode field values
    /// </summary>
    public enum _Mode
    {
        SymmetricActive,	// 1 - Symmetric active
        SymmetricPassive,	// 2 - Symmetric pasive
        Client,				// 3 - Client
        Server,				// 4 - Server
        Broadcast,			// 5 - Broadcast
        Unknown				// 0, 6, 7 - Reserved
    }

    /// <summary>
    /// Stratum field values
    /// </summary>
    public enum _Stratum
    {
        Unspecified,			// 0 - unspecified or unavailable
        PrimaryReference,		// 1 - primary reference (e.g. radio-clock)
        SecondaryReference,		// 2-15 - secondary reference (via NTP or SNTP)
        Reserved				// 16-255 - reserved
    }

    /// <summary>
    /// SNTPClient is a C# class designed to connect to time servers on the Internet and
    /// fetch the current date and time. Optionally, it may update the time of the local system.
    /// The implementation of the protocol is based on the RFC 2030.
    /// 
    /// Public class members:
    ///
    /// LeapIndicator - Warns of an impending leap second to be inserted/deleted in the last
    /// minute of the current day. (See the _LeapIndicator enum)
    /// 
    /// VersionNumber - Version number of the protocol (3 or 4).
    /// 
    /// Mode - Returns mode. (See the _Mode enum)
    /// 
    /// Stratum - Stratum of the clock. (See the _Stratum enum)
    /// 
    /// PollInterval - Maximum interval between successive messages
    /// 
    /// Precision - Precision of the clock
    /// 
    /// RootDelay - Round trip time to the primary reference source.
    /// 
    /// RootDispersion - Nominal error relative to the primary reference source.
    /// 
    /// ReferenceID - Reference identifier (either a 4 character string or an IP address).
    /// 
    /// ReferenceTimestamp - The time at which the clock was last set or corrected.
    /// 
    /// OriginateTimestamp - The time at which the request departed the client for the server.
    /// 
    /// ReceiveTimestamp - The time at which the request arrived at the server.
    /// 
    /// Transmit Timestamp - The time at which the reply departed the server for client.
    /// 
    /// RoundTripDelay - The time between the departure of request and arrival of reply.
    /// 
    /// LocalClockOffset - The offset of the local clock relative to the primary reference
    /// source.
    /// 
    /// Initialize - Sets up data structure and prepares for connection.
    /// 
    /// Connect - Connects to the time server and populates the data structure.
    ///	It can also update the system time.
    /// 
    /// IsResponseValid - Returns true if received data is valid and if comes from
    /// a NTP-compliant time server.
    /// 
    /// ToString - Returns a string representation of the object.
    /// 
    /// -----------------------------------------------------------------------------
    /// Structure of the standard NTP header (as described in RFC 2030)
    ///                       1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                          Root Delay                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                       Root Dispersion                         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                     Reference Identifier                      |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Reference Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Originate Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Receive Timestamp (64)                     |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Transmit Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                 Key Identifier (optional) (32)                |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                                                               |
    ///  |                 Message Digest (optional) (128)               |
    ///  |                                                               |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// -----------------------------------------------------------------------------
    /// 
    /// SNTP Timestamp Format (as described in RFC 2030)
    ///                         1                   2                   3
    ///     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           Seconds                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                  Seconds Fraction (0-padded)                  |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// 
    /// </summary>

    public class SNTPClient
    {
        // SNTP Data Structure Length
        private const byte SNTPDataLength = 48;

        // SNTP Data Structure (as described in RFC 2030)
        byte[] SNTPData = new byte[SNTPDataLength];

        // Offset constants for timestamps in the data structure
        private const byte offReferenceID = 12;
        private const byte offReferenceTimestamp = 16;
        private const byte offOriginateTimestamp = 24;
        private const byte offReceiveTimestamp = 32;
        private const byte offTransmitTimestamp = 40;

        /// <summary>
        /// Warns of an impending leap second to be inserted/deleted in the last
        /// minute of the current day. (See the _LeapIndicator enum)
        /// </summary>
        public _LeapIndicator LeapIndicator
        {
            get
            {
                // Isolate the two most significant bits
                byte val = (byte)(SNTPData[0] >> 6);
                switch (val)
                {
                    case 0: return _LeapIndicator.NoWarning;
                    case 1: return _LeapIndicator.LastMinute61;
                    case 2: return _LeapIndicator.LastMinute59;
                    case 3: goto default;
                    default:
                        return _LeapIndicator.Alarm;
                }
            }
        }

        /// <summary>
        /// Version number of the protocol (3 or 4).
        /// </summary>
        public byte VersionNumber
        {
            get
            {
                // Isolate bits 3 - 5
                byte val = (byte)((SNTPData[0] & 0x38) >> 3);
                return val;
            }
        }

        /// <summary>
        /// Returns mode. (See the _Mode enum)
        /// </summary>
        public _Mode Mode
        {
            get
            {
                // Isolate bits 0 - 3
                byte val = (byte)(SNTPData[0] & 0x7);
                switch (val)
                {
                    case 0: goto default;
                    case 6: goto default;
                    case 7: goto default;
                    default:
                        return _Mode.Unknown;
                    case 1:
                        return _Mode.SymmetricActive;
                    case 2:
                        return _Mode.SymmetricPassive;
                    case 3:
                        return _Mode.Client;
                    case 4:
                        return _Mode.Server;
                    case 5:
                        return _Mode.Broadcast;
                }
            }
        }

        /// <summary>
        /// Stratum of the clock. (See the _Stratum enum)
        /// </summary>
        public _Stratum Stratum
        {
            get
            {
                byte val = (byte)SNTPData[1];
                if (val == 0) return _Stratum.Unspecified;
                else
                    if (val == 1) return _Stratum.PrimaryReference;
                    else
                        if (val <= 15) return _Stratum.SecondaryReference;
                        else
                            return _Stratum.Reserved;
            }
        }

        /// <summary>
        /// Maximum interval (seconds) between successive messages
        /// </summary>
        public uint PollInterval
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (uint)(Math.Pow(2, (sbyte)SNTPData[2]));
            }
        }

        /// <summary>
        /// Precision (in seconds) of the clock
        /// </summary>
        public double Precision
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (Math.Pow(2, (sbyte)SNTPData[3]));
            }
        }

        /// <summary>
        /// Round trip time (in milliseconds) to the primary reference source.
        /// </summary>
        public double RootDelay
        {
            get
            {
                int temp = 0;
                temp = 256 * (256 * (256 * SNTPData[4] + SNTPData[5]) + SNTPData[6]) + SNTPData[7];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        /// <summary>
        /// Nominal error (in milliseconds) relative to the primary reference source.
        /// </summary>
        public double RootDispersion
        {
            get
            {
                int temp = 0;
                temp = 256 * (256 * (256 * SNTPData[8] + SNTPData[9]) + SNTPData[10]) + SNTPData[11];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        /// <summary>
        /// Reference identifier (either a 4 character string or an IP address)
        /// </summary>
        public string ReferenceID
        {
            get
            {
                string val = "";
                switch (Stratum)
                {
                    case _Stratum.Unspecified:
                        goto case _Stratum.PrimaryReference;
                    case _Stratum.PrimaryReference:
                        val += (char)SNTPData[offReferenceID + 0];
                        val += (char)SNTPData[offReferenceID + 1];
                        val += (char)SNTPData[offReferenceID + 2];
                        val += (char)SNTPData[offReferenceID + 3];
                        break;
                    case _Stratum.SecondaryReference:
                        switch (VersionNumber)
                        {
                            case 3:	// Version 3, Reference ID is an IPv4 address
                                string Address = SNTPData[offReferenceID + 0].ToString() + "." +
                                                 SNTPData[offReferenceID + 1].ToString() + "." +
                                                 SNTPData[offReferenceID + 2].ToString() + "." +
                                                 SNTPData[offReferenceID + 3].ToString();
                                try
                                {
                                    IPHostEntry Host = Dns.GetHostEntry(Address);
                                    val = Host.HostName + " (" + Address + ")";
                                }
                                catch (Exception)
                                {
                                    val = "N/A";
                                }
                                break;
                            case 4: // Version 4, Reference ID is the timestamp of last update
                                DateTime time = ComputeDate(GetMilliSeconds(offReferenceID));
                                // Take care of the time zone
                                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                                val = (time + offspan).ToString();
                                break;
                            default:
                                val = "N/A";
                                break;
                        }
                        break;
                }

                return val;
            }
        }

        /// <summary>
        /// The time at which the clock was last set or corrected
        /// </summary>
        public DateTime ReferenceTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offReferenceTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                return time + offspan;
            }
        }

        /// <summary>
        /// The time (T1) at which the request departed the client for the server
        /// </summary>
        public DateTime OriginateTimestamp
        {
            get
            {
                return ComputeDate(GetMilliSeconds(offOriginateTimestamp));
            }
        }

        /// <summary>
        /// The time (T2) at which the request arrived at the server
        /// </summary>
        public DateTime ReceiveTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offReceiveTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                return time + offspan;
            }
        }

        /// <summary>
        /// The time (T3) at which the reply departed the server for client
        /// </summary>
        public DateTime TransmitTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliSeconds(offTransmitTimestamp));
                // Take care of the time zone
                TimeSpan offspan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                return time + offspan;
            }
            set
            {
                SetDate(offTransmitTimestamp, value);
            }
        }

        /// <summary>
        /// Destination Timestamp (T4)
        /// </summary>
        public DateTime DestinationTimestamp;

        /// <summary>
        /// The time (in milliseconds) between the departure of request and arrival of reply 
        /// </summary>
        public double RoundTripDelay
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (DestinationTimestamp - OriginateTimestamp) - (ReceiveTimestamp - TransmitTimestamp);
                return span.TotalMilliseconds;
            }
        }

        /// <summary>
        /// The offset (in milliseconds) of the local clock relative to the primary reference source
        /// </summary>
        public double LocalClockOffset
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) + (TransmitTimestamp - DestinationTimestamp);
                return (span.TotalMilliseconds / 2);
            }
        }

        /// <summary>
        /// Compute date, given the number of milliseconds since January 1, 1900
        /// </summary>
        private DateTime ComputeDate(ulong milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds((double)milliseconds);
            DateTime time = new DateTime(1900, 1, 1);
            time += span;
            return time;
        }

        /// <summary>
        /// Compute the number of milliseconds, given the offset of a 8-byte array
        /// </summary>
        private ulong GetMilliSeconds(byte offset)
        {
            ulong intpart = 0, fractpart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intpart = 256 * intpart + SNTPData[offset + i];
            }
            for (int i = 4; i <= 7; i++)
            {
                fractpart = 256 * fractpart + SNTPData[offset + i];
            }
            ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
            return milliseconds;
        }

        /// <summary>
        /// Set the date part of the SNTP data
        /// </summary>
        /// <param name="offset">Offset at which the date part of the SNTP data is</param>
        /// <param name="date">The date</param>
        private void SetDate(byte offset, DateTime date)
        {
            ulong intpart = 0, fractpart = 0;
            DateTime StartOfCentury = new DateTime(1900, 1, 1, 0, 0, 0);	// January 1, 1900 12:00 AM

            ulong milliseconds = (ulong)(date - StartOfCentury).TotalMilliseconds;
            intpart = milliseconds / 1000;
            fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000;

            ulong temp = intpart;
            for (int i = 3; i >= 0; i--)
            {
                SNTPData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }

            temp = fractpart;
            for (int i = 7; i >= 4; i--)
            {
                SNTPData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }
        }

        /// <summary>
        /// Initialize the SNTP client data. Sets up data structure and prepares for connection.
        /// </summary>
        private void Initialize()
        {
            // Set version number to 4 and Mode to 3 (client)
            SNTPData[0] = 0x1B;
            // Initialize all other fields with 0
            for (int i = 1; i < 48; i++)
            {
                SNTPData[i] = 0;
            }
            // Initialize the transmit timestamp
            TransmitTimestamp = DateTime.Now;
        }

        public SNTPClient(string host)
        {
            TimeServer = host;
        }

        /// <summary>
        /// Connects to the time server and populates the data structure.
        ///	It can also update the system time.
        /// </summary>
        /// <param name="TimeOutInterval">Time in milliseconds after which the method returns.</param>
        /// <param name="UpdateSystemTime">TRUE if the local time should be updated.</param>
        public void Connect(int TimeOutInterval, bool UpdateSystemTime)
        {
            try
            {
                IPEndPoint listenEP = new IPEndPoint(IPAddress.Any, 123);
                Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPHostEntry hostEntry = Dns.GetHostEntry(TimeServer);
                IPEndPoint sendEP = new IPEndPoint(hostEntry.AddressList[0], 123);
                EndPoint epSendEP = (EndPoint)sendEP;

                int messageLength = 0;
                try
                {
                    sendSocket.Bind(listenEP);
                    Initialize();

                    bool messageReceived = false;
                    int elapsedTime = 0;

                    // Timeout code
                    while (!messageReceived && (elapsedTime < TimeOutInterval))
                    {
                        sendSocket.SendTo(SNTPData, SNTPData.Length, SocketFlags.None, sendEP);
                        // Check if data has been received by the listening socket and is available to be read
                        if (sendSocket.Available > 0)
                        {
                            messageLength = sendSocket.ReceiveFrom(SNTPData, ref epSendEP);
                            if (!IsResponseValid())
                            {
                                throw new Exception("Invalid response from " + TimeServer);
                            }
                            messageReceived = true;
                            break;
                        }
                        // Wait a second
                        Thread.Sleep(1000);
                        elapsedTime += 1000;
                    }
                    if (!messageReceived)
                    {
                        throw new TimeoutException("Timeout whilst trying to connect to " + TimeServer);
                    }
                }
                catch (SocketException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    sendSocket.Close();
                }

                DestinationTimestamp = DateTime.Now;
            }
            catch (SocketException e)
            {
                throw new Exception(e.Message);
            }

            // Update system time
            if (UpdateSystemTime)
            {
                SetTime();
            }
        }

        /// <summary>
        /// Returns true if received data is valid and if comes from a NTP-compliant time server.
        /// </summary>
        public bool IsResponseValid()
        {
            if (SNTPData.Length < SNTPDataLength || Mode != _Mode.Server)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        public override string ToString()
        {
            string str;

            str = "Leap Indicator: ";
            switch (LeapIndicator)
            {
                case _LeapIndicator.NoWarning:
                    str += "No warning";
                    break;
                case _LeapIndicator.LastMinute61:
                    str += "Last minute has 61 seconds";
                    break;
                case _LeapIndicator.LastMinute59:
                    str += "Last minute has 59 seconds";
                    break;
                case _LeapIndicator.Alarm:
                    str += "Alarm Condition (clock not synchronized)";
                    break;
            }
            str += "\r\nVersion number: " + VersionNumber.ToString() + "\r\n";
            str += "Mode: ";
            switch (Mode)
            {
                case _Mode.Unknown:
                    str += "Unknown";
                    break;
                case _Mode.SymmetricActive:
                    str += "Symmetric Active";
                    break;
                case _Mode.SymmetricPassive:
                    str += "Symmetric Pasive";
                    break;
                case _Mode.Client:
                    str += "Client";
                    break;
                case _Mode.Server:
                    str += "Server";
                    break;
                case _Mode.Broadcast:
                    str += "Broadcast";
                    break;
            }
            str += "\r\nStratum: ";
            switch (Stratum)
            {
                case _Stratum.Unspecified:
                case _Stratum.Reserved:
                    str += "Unspecified";
                    break;
                case _Stratum.PrimaryReference:
                    str += "Primary Reference";
                    break;
                case _Stratum.SecondaryReference:
                    str += "Secondary Reference";
                    break;
            }
            str += "\r\nLocal time: " + TransmitTimestamp.ToString();
            str += "\r\nPrecision: " + Precision.ToString() + " s";
            str += "\r\nPoll Interval: " + PollInterval.ToString() + " s";
            str += "\r\nReference ID: " + ReferenceID.ToString();
            str += "\r\nRoot Delay: " + RootDelay.ToString() + " ms";
            str += "\r\nRoot Dispersion: " + RootDispersion.ToString() + " ms";
            str += "\r\nRound Trip Delay: " + RoundTripDelay.ToString() + " ms";
            str += "\r\nLocal Clock Offset: " + LocalClockOffset.ToString() + " ms";
            str += "\r\n";

            return str;
        }

        /// <summary>
        /// SYSTEMTIME structure used by SetSystemTime
        /// </summary>
        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public short year;
            public short month;
            public short dayOfWeek;
            public short day;
            public short hour;
            public short minute;
            public short second;
            public short milliseconds;
        }

        /// <summary>
        /// Sets the Local Time. 
        /// Note that the Local Time is different from the System Time, and DateTime.Now.
        /// They are different by definition.  DateTime.Now will take the TimeZone and 
        /// Daylight Savings into account.
        /// </summary>
#if !SNTP_WindowsMobile
        // Compiling for Windows
        [DllImport("kernel32.dll")]
#else
        // Compiling for Windows Mobile
        [DllImport("coredll.dll")]
#endif
        static extern bool SetLocalTime(ref SYSTEMTIME time);

        /// <summary>
        /// Set the LocalTime according to transmit timestamp
        /// </summary>
        private void SetTime()
        {
            SYSTEMTIME st;

            // Thanks to Jim Hollenhorst <hollenho@attbi.com>
            DateTime trts = DateTime.Now.AddMilliseconds(LocalClockOffset);

            st.year = (short)trts.Year;
            st.month = (short)trts.Month;
            st.dayOfWeek = (short)trts.DayOfWeek;
            st.day = (short)trts.Day;
            st.hour = (short)trts.Hour;
            st.minute = (short)trts.Minute;
            st.second = (short)trts.Second;
            st.milliseconds = (short)trts.Millisecond;

            SetLocalTime(ref st);
        }

        /// <summary>
        /// The URL of the time server we're connecting to
        /// </summary>
        private string TimeServer;
    }
}
