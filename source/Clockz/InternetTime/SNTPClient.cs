/*
 * A C# SNTP Client
 * 
 * Copyright (C)2001-2003 Valer BOCAN <vbocan@dataman.ro>
 * All Rights Reserved
 * 
 * You may download the latest version from http://www.dataman.ro
 * Last modified: September 20, 2003
 *  
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, provided that the above
 * copyright notice(s) and this permission notice appear in all copies of
 * the Software and that both the above copyright notice(s) and this
 * permission notice appear in supporting documentation.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
 * OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL
 * INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING
 * FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
 * NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
 * WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 *
 * Disclaimer
 * ----------
 * Although reasonable care has been taken to ensure the correctness of this
 * implementation, this code should never be used in any application without
 * proper verification and testing. I disclaim all liability and responsibility
 * to any person or entity with respect to any loss or damage caused, or alleged
 * to be caused, directly or indirectly, by the use of this SNTPClient class.
 *
 * Comments, bugs and suggestions are welcome.
 *
 * Update history:
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

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace InternetTime
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    // Leap indicator field values
    public enum LeapIndicator
    {
        NoWarning,		// 0 - No warning
        LastMinute61,	// 1 - Last minute has 61 seconds
        LastMinute59,	// 2 - Last minute has 59 seconds
        Alarm			// 3 - Alarm condition (clock not synchronized)
    }

    //Mode field values
    public enum Mode
    {
        SymmetricActive,	// 1 - Symmetric active
        SymmetricPassive,	// 2 - Symmetric pasive
        Client,				// 3 - Client
        Server,				// 4 - Server
        Broadcast,			// 5 - Broadcast
        Unknown				// 0, 6, 7 - Reserved
    }

    // Stratum field values
    public enum Stratum
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

    public class SntpClient
    {
        // SNTP Data Structure Length
        private const byte SntpDataLength = 48;
        // SNTP Data Structure (as described in RFC 2030)
        byte[] _sntpData = new byte[SntpDataLength];

        // Offset constants for timestamps in the data structure
        private const byte OffReferenceId = 12;
        private const byte OffReferenceTimestamp = 16;
        private const byte OffOriginateTimestamp = 24;
        private const byte OffReceiveTimestamp = 32;
        private const byte OffTransmitTimestamp = 40;

        // Leap Indicator
        public LeapIndicator LeapIndicator
        {
            get
            {
                // Isolate the two most significant bits
                byte val = (byte)(_sntpData[0] >> 6);
                switch (val)
                {
                    case 0: return LeapIndicator.NoWarning;
                    case 1: return LeapIndicator.LastMinute61;
                    case 2: return LeapIndicator.LastMinute59;
                    case 3: goto default;
                    default:
                        return LeapIndicator.Alarm;
                }
            }
        }

        // Version Number
        public byte VersionNumber
        {
            get
            {
                // Isolate bits 3 - 5
                byte val = (byte)((_sntpData[0] & 0x38) >> 3);
                return val;
            }
        }

        // Mode
        public Mode Mode
        {
            get
            {
                // Isolate bits 0 - 3
                byte val = (byte)(_sntpData[0] & 0x7);
                switch (val)
                {
                    case 0: goto default;
                    case 6: goto default;
                    case 7: goto default;
                    default:
                        return Mode.Unknown;
                    case 1:
                        return Mode.SymmetricActive;
                    case 2:
                        return Mode.SymmetricPassive;
                    case 3:
                        return Mode.Client;
                    case 4:
                        return Mode.Server;
                    case 5:
                        return Mode.Broadcast;
                }
            }
        }

        // Stratum
        public Stratum Stratum
        {
            get
            {
                byte val = _sntpData[1];
                if (val == 0) return Stratum.Unspecified;
                if (val == 1) return Stratum.PrimaryReference;
                if (val <= 15) return Stratum.SecondaryReference;
                return Stratum.Reserved;
            }
        }

        // Poll Interval (in seconds)
        public uint PollIntervalSeconds
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (uint)(Math.Pow(2, (sbyte)_sntpData[2]));
            }
        }

        // Precision (in seconds)
        public double PrecisionSeconds
        {
            get
            {
                // Thanks to Jim Hollenhorst <hollenho@attbi.com>
                return (Math.Pow(2, (sbyte)_sntpData[3]));
            }
        }

        // Root Delay (in milliseconds)
        public double RootDelayMilliseconds
        {
            get
            {
                int temp = 256 * (256 * (256 * _sntpData[4] + _sntpData[5]) + _sntpData[6]) + _sntpData[7];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        // Root Dispersion (in milliseconds)
        public double RootDispersionMilliseconds
        {
            get
            {
                int temp = 256 * (256 * (256 * _sntpData[8] + _sntpData[9]) + _sntpData[10]) + _sntpData[11];
                return 1000 * (((double)temp) / 0x10000);
            }
        }

        // Reference Identifier
        public string ReferenceId
        {
            get
            {
                string val = "";
                switch (Stratum)
                {
                    case Stratum.Unspecified:
                        goto case Stratum.PrimaryReference;
                    case Stratum.PrimaryReference:
                        val += (char)_sntpData[OffReferenceId + 0];
                        val += (char)_sntpData[OffReferenceId + 1];
                        val += (char)_sntpData[OffReferenceId + 2];
                        val += (char)_sntpData[OffReferenceId + 3];
                        break;
                    case Stratum.SecondaryReference:
                        switch (VersionNumber)
                        {
                            case 3:	// Version 3, Reference ID is an IPv4 address
                                string address = _sntpData[OffReferenceId + 0] + "." +
                                                 _sntpData[OffReferenceId + 1] + "." +
                                                 _sntpData[OffReferenceId + 2] + "." +
                                                 _sntpData[OffReferenceId + 3];
                                try
                                {
                                    IPHostEntry host = Dns.GetHostEntry(address);
                                    val = host.HostName + " (" + address + ")";
                                }
                                catch (SocketException)
                                {
                                    val = "N/A";
                                }
                                break;
                            case 4: // Version 4, Reference ID is the timestamp of last update
                                DateTime time = ComputeDate(GetMilliseconds(OffReferenceId));
                                val = (time).ToString(CultureInfo.InvariantCulture);
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

        // Reference Timestamp
        public DateTime ReferenceTimestampUtc
        {
            get
            {
                DateTime time = ComputeDate(GetMilliseconds(OffReferenceTimestamp));
                return time;
            }
        }

        // Originate Timestamp (T1)
        public DateTime OriginateTimestamp
        {
            get
            {
                return ComputeDate(GetMilliseconds(OffOriginateTimestamp));
            }
        }

        // Receive Timestamp (T2)
        /// <summary>
        /// Server time at which NTP response was received locally
        /// </summary>
        public DateTime ReceiveTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliseconds(OffReceiveTimestamp));
                return time;
            }
        }

        // Transmit Timestamp (T3)
        /// <summary>
        /// Server time at which NTP request was send locally
        /// </summary>
        public DateTime TransmitTimestamp
        {
            get
            {
                DateTime time = ComputeDate(GetMilliseconds(OffTransmitTimestamp));
                return time;
            }
            set
            {
                SetDate(OffTransmitTimestamp, value);
            }
        }

        // Destination Timestamp (T4)
        /// <summary>
        /// Local UTC time, set when sync completes.
        /// </summary>
        public DateTime DestinationTimestamp { get; private set; }

        // Round trip delay (in milliseconds)
        public double RoundTripDelayMilliseconds
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (DestinationTimestamp - OriginateTimestamp) - (ReceiveTimestamp - TransmitTimestamp);
                return span.TotalMilliseconds;
            }
        }

        // Local clock offset (in milliseconds)
        public double LocalClockOffsetMilliseconds
        {
            get
            {
                // Thanks to DNH <dnharris@csrlink.net>
                TimeSpan span = (ReceiveTimestamp - OriginateTimestamp) + (TransmitTimestamp - DestinationTimestamp);
                return span.TotalMilliseconds / 2;
            }
        }

        // Compute date, given the number of milliseconds since January 1, 1900
        private DateTime ComputeDate(ulong milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
            return _startOfCentury + span;
        }

        // Compute the number of milliseconds, given the offset of a 8-byte array
        private ulong GetMilliseconds(byte offset)
        {
            ulong intpart = 0, fractpart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intpart = 256 * intpart + _sntpData[offset + i];
            }
            for (int i = 4; i <= 7; i++)
            {
                fractpart = 256 * fractpart + _sntpData[offset + i];
            }
            ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
            return milliseconds;
        }

        readonly DateTime _startOfCentury = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);	// January 1, 1900 12:00 AM

        // Compute the 8-byte array, given the date
        private void SetDate(byte offset, DateTime date)
        {
            ulong intPart, fractPart;

            ulong milliseconds = (ulong)(date - _startOfCentury).TotalMilliseconds;
            intPart = milliseconds / 1000;
            fractPart = ((milliseconds % 1000) * 0x100000000L) / 1000;

            ulong temp = intPart;
            for (int i = 3; i >= 0; i--)
            {
                _sntpData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }

            temp = fractPart;
            for (int i = 7; i >= 4; i--)
            {
                _sntpData[offset + i] = (byte)(temp % 256);
                temp = temp / 256;
            }
        }

        // Initialize the NTPClient data
        private void Initialize()
        {
            // Set version number to 4 and Mode to 3 (client)
            _sntpData[0] = 0x1B;
            // Initialize all other fields with 0
            for (int i = 1; i < 48; i++)
            {
                _sntpData[i] = 0;
            }
        }

        public SntpClient(string host, int port = NtpPort, bool updateAddress= false)
        {
            _port = port;
            _timeServer = host;
            _updateAddress = updateAddress;
            // Resolve server address
            ResolveAddress();
        }

        public SntpClient(IPAddress address, int port = NtpPort)
        {
            _port = port;
            _timeServer = "NA";
            _epHost = new IPEndPoint(address, _port);
        }

        void ResolveAddress()
        {
            IPHostEntry hostadd = Dns.GetHostEntry(_timeServer);
            _epHost = new IPEndPoint(hostadd.AddressList[0], _port);
        }

        private const int NtpPort = 123;
        private const int SocketTimeout = 5000;
        private readonly int _port;
        private IPEndPoint _epHost;
        private readonly bool _updateAddress;

        // Connect to the time server and update system time
        public void Connect()
        {
            try
            {
                if (_updateAddress)
                {
                    ResolveAddress();
                }

                //Connect the time server
                using (UdpClient timeSocket = new UdpClient())
                {
                    timeSocket.Client.ReceiveTimeout = SocketTimeout;
                    timeSocket.Client.SendTimeout = SocketTimeout;

                    timeSocket.Connect(_epHost);
                    // Initialize data structure
                    Initialize();
                    // Initialize the transmit timestamp
                    TransmitTimestamp = DateTime.UtcNow;
                    timeSocket.Send(_sntpData, _sntpData.Length);
                    _sntpData = timeSocket.Receive(ref _epHost);
                    if (!IsResponseValid())
                    {
                        throw new Exception("Invalid response from " + _timeServer);
                    }

                    DestinationTimestamp = DateTime.UtcNow;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error retrieving NTP timestamp.", e);
            }
        }
        // Check if the response from server is valid
        public bool IsResponseValid()
        {
            return _sntpData.Length >= SntpDataLength && Mode == Mode.Server;
        }

        // Converts the object to string
        public override string ToString()
        {
            var str = new StringBuilder();

            str.Append("Leap Indicator: ");
            switch (LeapIndicator)
            {
                case LeapIndicator.NoWarning:
                    str.Append("No warning");
                    break;
                case LeapIndicator.LastMinute61:
                    str.Append("Last minute has 61 seconds");
                    break;
                case LeapIndicator.LastMinute59:
                    str.Append("Last minute has 59 seconds");
                    break;
                case LeapIndicator.Alarm:
                    str.Append("Alarm Condition (clock not synchronized)");
                    break;
            }
            str.AppendLine();
            str.AppendFormat("Server            : {0}:{1} ({2})", _timeServer, _port, _epHost).AppendLine();
            str.AppendFormat("Version number    : {0}", VersionNumber).AppendLine();
            str.AppendFormat("Mode              : {0}", Mode).AppendLine();
            str.AppendFormat("Stratum           : {0}", Stratum).AppendLine();
            str.AppendFormat("Local time        : " + TransmitTimestamp).AppendLine();
            str.AppendFormat("Precision         : {0:N} µs", PrecisionSeconds * 1000 * 1000).AppendLine();
            str.AppendFormat("Poll Interval     : {0} s", PollIntervalSeconds).AppendLine();
            str.AppendFormat("Reference ID      : {0}", ReferenceId).AppendLine();
            str.AppendFormat("Root Delay        : {0} ms", RootDelayMilliseconds).AppendLine();
            str.AppendFormat("Root Dispersion   : {0} ms", RootDispersionMilliseconds).AppendLine();
            str.AppendFormat("Round Trip Delay  : {0} ms", RoundTripDelayMilliseconds).AppendLine();
            str.AppendFormat("Local Clock Offset: {0} µs", LocalClockOffsetMilliseconds * 1000).AppendLine();
            str.AppendLine();
            str.AppendFormat("ReceiveTimestamp    : {0:O} ({1})", ReceiveTimestamp, ReceiveTimestamp.Kind).AppendLine();
            str.AppendFormat("OriginateTimestamp  : {0:O} ({1})", OriginateTimestamp, OriginateTimestamp.Kind).AppendLine();
            str.AppendFormat("TransmitTimestamp   : {0:O} ({1})", TransmitTimestamp, TransmitTimestamp.Kind).AppendLine();
            str.AppendFormat("DestinationTimestamp: {0:O} ({1})", DestinationTimestamp, DestinationTimestamp.Kind).AppendLine();

            return str.ToString();
        }
        // The URL of the time server we're connecting to
        private readonly string _timeServer;
    }
}
