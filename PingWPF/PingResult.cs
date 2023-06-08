using System;

namespace PingWPF
{
    public class PingResult
    {
        public DateTime Timestamp { get; }
        public long RoundtripTime { get; }

        public PingResult( DateTime timestamp, long roundtripTime )
        {
            Timestamp = timestamp;
            RoundtripTime = roundtripTime;
        }
    }
}