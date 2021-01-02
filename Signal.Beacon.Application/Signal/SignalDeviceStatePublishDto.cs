using System;

namespace Signal.Beacon.Application.Signal
{
    public class SignalDeviceStatePublishDto
    {
        public string DeviceIdentifier { get; set; }
        
        public string ChannelName { get; set; }
        
        public string ContactName { get; set; }
        
        public string? ValueSerialized { get; set; }
        
        public DateTime TimeStamp { get; set; }
    }
}